using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CabeNaSemana.Tests.Web;

public sealed class BoardPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BoardPageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task GetRoot_RendersMvcKanbanWithAllColumns()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Cabe na Semana", html, StringComparison.Ordinal);
        Assert.Contains("A planejar", html, StringComparison.Ordinal);
        Assert.Contains("Esta semana", html, StringComparison.Ordinal);
        Assert.Contains("Em andamento", html, StringComparison.Ordinal);
        Assert.Contains("Concluído", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostCapacity_RejectsRequestWithoutAntiforgeryToken()
    {
        using var client = _factory.CreateClient();
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string> { ["WeeklyCapacity"] = "12" });

        var response = await client.PostAsync("/capacidade", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyResponse()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("healthy", content);
    }
}
