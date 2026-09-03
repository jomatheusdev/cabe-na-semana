using System.Net;
using System.Net.Http.Json;
using CabeNaSemana.Application.Board;
using CabeNaSemana.Domain.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CabeNaSemana.Tests.Api;

public sealed class ApiErrorHandlingTests
{
    [Fact]
    public async Task UnknownRoute_ReturnsGenericProblemDetails()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/rota-inexistente");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status404NotFound, problem?.Status);
        Assert.DoesNotContain(
            "/Users/",
            problem?.Detail ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnexpectedException_ReturnsGenericProblemDetailsWithoutTechnicalMessage()
    {
        using var factory = new ThrowingApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/board");
        var body = await response.Content.ReadAsStringAsync();
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Ocorreu um erro interno.", problem?.Title);
        Assert.DoesNotContain("detalhe técnico secreto", body, StringComparison.OrdinalIgnoreCase);
        Assert.Null(problem?.Detail);
    }

    [Fact]
    public async Task DomainFailure_ReturnsUnprocessableEntityWithValidationProblem()
    {
        using var factory = new DomainFailureApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new
            {
                title = "Preparar seminário",
                dueDate = "2026-09-10",
                importance = "high",
                estimatedHours = 2.5m,
                status = "planning"
            });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problem?.Status);
        Assert.Contains("estimatedHours", problem?.Errors.Keys ?? []);
    }

    [Fact]
    public async Task GlobalRateLimit_ReturnsClearTooManyRequestsProblem()
    {
        using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/health");
        using var limitedResponse = await client.GetAsync("/health");
        var problem = await limitedResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        Assert.Equal("application/problem+json", limitedResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Muitas requisições.", problem?.Title);
        Assert.Equal(TimeSpan.FromSeconds(60), limitedResponse.Headers.RetryAfter?.Delta);
    }
}

public sealed class ThrowingApiWebApplicationFactory : ApiWebApplicationFactory
{
    protected override void ConfigureApplicationServices(IServiceCollection services)
    {
        services.RemoveAll<IBoardRepository>();
        services.AddScoped<IBoardRepository, ThrowingBoardRepository>();
    }
}

public sealed class DomainFailureApiWebApplicationFactory : ApiWebApplicationFactory
{
    protected override void ConfigureApplicationServices(IServiceCollection services)
    {
        services.RemoveAll<IBoardRepository>();
        services.AddScoped<IBoardRepository, DomainFailureBoardRepository>();
    }
}

public sealed class RateLimitedApiWebApplicationFactory : ApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:PermitLimit", "1");
    }
}

internal abstract class BoardRepositoryStub : IBoardRepository
{
    public virtual Task<IReadOnlyList<StudyTask>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StudyTask>>([]);

    public virtual Task<StudyTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<StudyTask?>(null);

    public virtual Task AddAsync(StudyTask task, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public virtual void Remove(StudyTask task)
    {
    }

    public virtual Task<decimal> GetWeeklyCapacityAsync(CancellationToken cancellationToken) =>
        Task.FromResult(10m);

    public virtual Task SetWeeklyCapacityAsync(decimal hours, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public virtual Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class ThrowingBoardRepository : BoardRepositoryStub
{
    public override Task<IReadOnlyList<StudyTask>> ListAsync(
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("detalhe técnico secreto");
}

internal sealed class DomainFailureBoardRepository : BoardRepositoryStub
{
    public override Task AddAsync(StudyTask task, CancellationToken cancellationToken) =>
        throw DomainValidationException.For(
            nameof(StudyTask.EstimatedHours),
            "O esforço conflita com uma regra do planejamento.");
}
