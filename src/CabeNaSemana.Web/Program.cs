using CabeNaSemana.Application.Board;
using CabeNaSemana.Application.Common;
using CabeNaSemana.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<BoardService>();
builder.Services.AddScoped<IBoardRepository, EfBoardRepository>();
builder.Services.AddSingleton<IAppClock, SystemAppClock>();

var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Planner");
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (databaseProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Planner é obrigatória quando Database:Provider é PostgreSql.");
    }

    var dataDirectory = builder.Environment.IsEnvironment("Testing")
        ? Path.Combine(Path.GetTempPath(), "cabe-na-semana-tests")
        : Path.Combine(builder.Environment.ContentRootPath, "Data");
    Directory.CreateDirectory(dataDirectory);
    var databaseName = builder.Environment.IsEnvironment("Testing")
        ? $"planner-{Environment.ProcessId}.db"
        : "cabe-na-semana.db";
    connectionString = $"Data Source={Path.Combine(dataDirectory, databaseName)}";
}

builder.Services.AddPlannerDatabase(databaseProvider, connectionString);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Board}/{action=Index}/{id?}");
app.MapGet("/health", () => Results.Text("healthy", "text/plain"));
app.MapFallbackToController("NotFoundPage", "Board");

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
    await DatabaseInitializer.InitializeAsync(
        dbContext,
        seedDemoData: !app.Environment.IsEnvironment("Testing"));
}

await app.RunAsync();

public partial class Program;
