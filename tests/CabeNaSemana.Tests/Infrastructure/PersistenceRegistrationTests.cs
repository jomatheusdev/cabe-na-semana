using CabeNaSemana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CabeNaSemana.Tests.Infrastructure;

public sealed class PersistenceRegistrationTests
{
    [Fact]
    public void AddPlannerDatabase_WithSqlite_RegistersSqliteProvider()
    {
        var services = new ServiceCollection();

        services.AddPlannerDatabase("Sqlite", "Data Source=:memory:");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }

    [Fact]
    public void AddPlannerDatabase_WithPostgreSql_RegistersNpgsqlProvider()
    {
        var services = new ServiceCollection();

        services.AddPlannerDatabase(
            "PostgreSql",
            "Host=postgres;Port=5432;Database=cabe_na_semana;Username=app;Password=test");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [Fact]
    public void AddPlannerDatabase_WithUnknownProvider_RejectsConfiguration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddPlannerDatabase("Unknown", "unused"));

        Assert.Contains("Database:Provider", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("forte;com=separadores\"e'aspas")]
    [InlineData("espaços e símbolos !@#$%&*()")]
    public void PostgreSqlConnectionStringFactory_PreservesSpecialCharactersInPassword(
        string password)
    {
        var connectionString = PostgreSqlConnectionStringFactory.Create(
            "postgres",
            5432,
            "cabe_na_semana",
            "cabe_runtime",
            password);

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("postgres", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("cabe_na_semana", parsed.Database);
        Assert.Equal("cabe_runtime", parsed.Username);
        Assert.Equal(password, parsed.Password);
        Assert.True(parsed.Pooling);
    }
}
