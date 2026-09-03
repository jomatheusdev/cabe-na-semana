using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using CabeNaSemana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CabeNaSemana.Tests.Api;

using ApiProgram = CabeNaSemana.Api.Program;

public sealed class ApiContractTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync() => await factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetBoard_ReturnsTheFourColumnsAndCamelCaseStringEnums()
    {
        using var response = await _client.GetAsync("/api/board");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var root = document.RootElement;
        var columns = root.GetProperty("columns").EnumerateArray().ToArray();
        string?[] expectedStatuses = ["planning", "thisWeek", "inProgress", "completed"];
        Assert.Equal(
            expectedStatuses,
            columns.Select(column => column.GetProperty("status").GetString()).ToArray());
        Assert.All(columns, column => Assert.Empty(column.GetProperty("tasks").EnumerateArray()));
        Assert.Equal(10m, root.GetProperty("capacity").GetProperty("weeklyHours").GetDecimal());
        Assert.Equal(0, root.GetProperty("totalTasks").GetInt32());
        Assert.Equal(0, root.GetProperty("completedTasks").GetInt32());
        Assert.True(root.TryGetProperty("today", out _));
    }

    [Fact]
    public async Task TaskEndpoints_SupportCreateReadUpdateMoveAndDelete()
    {
        using var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            ValidTask(title: "Preparar apresentação"));
        using var createDocument = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var id = createDocument.RootElement.GetProperty("id").GetGuid();
        Assert.Equal($"/api/tasks/{id}", createResponse.Headers.Location?.OriginalString);

        using var getResponse = await _client.GetAsync($"/api/tasks/{id}");
        using var getDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal("Preparar apresentação", getDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal("critical", getDocument.RootElement.GetProperty("importance").GetString());
        Assert.Equal("thisWeek", getDocument.RootElement.GetProperty("status").GetString());
        Assert.True(getDocument.RootElement.TryGetProperty("createdAtUtc", out _));
        Assert.True(getDocument.RootElement.TryGetProperty("updatedAtUtc", out _));

        using var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tasks/{id}",
            ValidTask(title: "Apresentação revisada", status: "planning"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var moveResponse = await _client.PatchAsJsonAsync(
            $"/api/tasks/{id}/status",
            new { status = "inProgress" });
        Assert.Equal(HttpStatusCode.NoContent, moveResponse.StatusCode);

        using var movedResponse = await _client.GetAsync($"/api/tasks/{id}");
        using var movedDocument = JsonDocument.Parse(await movedResponse.Content.ReadAsStringAsync());
        Assert.Equal("Apresentação revisada", movedDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal("inProgress", movedDocument.RootElement.GetProperty("status").GetString());

        using var deleteResponse = await _client.DeleteAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await _client.GetAsync($"/api/tasks/{id}");
        var problem = await missingResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, problem?.Status);
        Assert.Equal("Atividade não encontrada.", problem?.Title);
    }

    [Fact]
    public async Task PutWeeklyCapacity_UpdatesTheBoardCapacity()
    {
        using var updateResponse = await _client.PutAsJsonAsync(
            "/api/settings/weekly-capacity",
            new { weeklyCapacityHours = 18.5m });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var boardResponse = await _client.GetAsync("/api/board");
        using var boardDocument = JsonDocument.Parse(await boardResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            18.5m,
            boardDocument.RootElement.GetProperty("capacity").GetProperty("weeklyHours").GetDecimal());
    }

    [Fact]
    public async Task PostTask_ReturnsValidationProblem_WhenRequiredFieldsAreInvalid()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "",
                dueDate = "2026-09-10",
                importance = "critical",
                estimatedHours = 0,
                status = "planning"
            });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status400BadRequest, problem?.Status);
        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
    }

    [Theory]
    [InlineData("importance", 4)]
    [InlineData("status", 2)]
    public async Task PostTask_RejectsNumericEnumValues(string property, int numericValue)
    {
        var body = new Dictionary<string, object?>
        {
            ["title"] = "Revisar contrato",
            ["dueDate"] = "2026-09-10",
            ["importance"] = "high",
            ["estimatedHours"] = 2.5m,
            ["status"] = "planning"
        };
        body[property] = numericValue;

        using var response = await _client.PostAsJsonAsync("/api/tasks", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("PUT", "/api/tasks/{0}")]
    [InlineData("PATCH", "/api/tasks/{0}/status")]
    [InlineData("DELETE", "/api/tasks/{0}")]
    public async Task WriteTaskEndpoints_ReturnProblemDetails_WhenTaskDoesNotExist(
        string method,
        string routeTemplate)
    {
        var id = Guid.NewGuid();
        var route = string.Format(CultureInfo.InvariantCulture, routeTemplate, id);
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(ValidTask());
        }
        else if (method == "PATCH")
        {
            request.Content = JsonContent.Create(new { status = "completed" });
        }

        using var response = await _client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status404NotFound, problem?.Status);
        Assert.Equal("Atividade não encontrada.", problem?.Title);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyText()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", await response.Content.ReadAsStringAsync());
    }

    private static object ValidTask(
        string title = "Revisar contrato",
        string status = "thisWeek") => new
        {
            title,
            dueDate = "2026-09-10",
            importance = "critical",
            estimatedHours = 2.5m,
            status
        };
}

public class ApiWebApplicationFactory : WebApplicationFactory<ApiProgram>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_connection.State == System.Data.ConnectionState.Closed)
        {
            _connection.Open();
        }
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PlannerDbContext>>();
            services.RemoveAll<PlannerDbContext>();
            services.AddDbContext<PlannerDbContext>(options => options.UseSqlite(_connection));
            ConfigureApplicationServices(services);
        });
    }

    protected virtual void ConfigureApplicationServices(IServiceCollection services)
    {
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
