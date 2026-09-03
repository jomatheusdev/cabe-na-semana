using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CabeNaSemana.Application.Board;
using CabeNaSemana.Application.Common;
using CabeNaSemana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace CabeNaSemana.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(
                        JsonNamingPolicy.CamelCase,
                        allowIntegerValues: false));
            });
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                if (status >= StatusCodes.Status500InternalServerError)
                {
                    context.ProblemDetails.Title = "Ocorreu um erro interno.";
                    context.ProblemDetails.Detail = null;
                }

                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            };
        });
        var permitLimit = Math.Max(
            1,
            builder.Configuration.GetValue("RateLimiting:PermitLimit", 120));
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                var problemDetailsService = context.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Muitas requisições.",
                        Detail = "Tente novamente em instantes."
                    }
                });
            };
        });
        builder.Services.AddScoped<BoardService>();
        builder.Services.AddScoped<IBoardRepository, EfBoardRepository>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(ResolvePlanningTimeZone(builder));
        builder.Services.AddSingleton<IAppClock, SystemAppClock>();

        var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = ResolveConnectionString(builder, databaseProvider);
        builder.Services.AddPlannerDatabase(databaseProvider, connectionString);

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseRateLimiter();
        app.MapControllers();
        app.MapGet("/health", () => Results.Text("healthy", "text/plain"));

        await InitializeDatabaseAsync(app);
        await app.RunAsync();
    }

    private static string ResolveConnectionString(
        WebApplicationBuilder builder,
        string databaseProvider)
    {
        var connectionString = builder.Configuration.GetConnectionString("Planner");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (databaseProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return PostgreSqlConnectionStringFactory.Create(
                GetRequiredConfiguration(builder, "Database:Host"),
                builder.Configuration.GetValue("Database:Port", 5432),
                GetRequiredConfiguration(builder, "Database:Name"),
                GetRequiredConfiguration(builder, "Database:Username"),
                GetRequiredConfiguration(builder, "Database:Password"));
        }

        var dataDirectory = builder.Environment.IsEnvironment("Testing")
            ? Path.Combine(Path.GetTempPath(), "cabe-na-semana-tests", "api")
            : Path.Combine(builder.Environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);
        var databaseName = builder.Environment.IsEnvironment("Testing")
            ? $"planner-api-{Environment.ProcessId}.db"
            : "cabe-na-semana.db";
        return $"Data Source={Path.Combine(dataDirectory, databaseName)}";
    }

    private static string GetRequiredConfiguration(
        WebApplicationBuilder builder,
        string key)
    {
        var value = builder.Configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"A configuração '{key}' é obrigatória.");
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IAppClock>();
        await DatabaseInitializer.InitializeAsync(
            dbContext,
            seedDemoData: !app.Environment.IsEnvironment("Testing"),
            clock);
    }

    private static TimeZoneInfo ResolvePlanningTimeZone(WebApplicationBuilder builder)
    {
        var timeZoneId = builder.Configuration["Planning:TimeZone"]
            ?? "America/Fortaleza";

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Planning:TimeZone '{timeZoneId}' não é um fuso horário válido.",
                exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException(
                $"Planning:TimeZone '{timeZoneId}' contém dados inválidos.",
                exception);
        }
    }
}
