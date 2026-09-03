using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CabeNaSemana.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddPlannerDatabase(
        this IServiceCollection services,
        string provider,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddDbContext<PlannerDbContext>(
                options => options.UseSqlite(connectionString));
        }

        if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddDbContext<PlannerDbContext>(
                options => options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null)));
        }

        throw new InvalidOperationException(
            $"Database:Provider '{provider}' não é suportado. Use 'Sqlite' ou 'PostgreSql'.");
    }
}
