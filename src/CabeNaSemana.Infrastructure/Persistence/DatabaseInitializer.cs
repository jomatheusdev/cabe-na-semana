using CabeNaSemana.Application.Common;
using CabeNaSemana.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CabeNaSemana.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const string CreateInitializationTableSql = """
        CREATE TABLE IF NOT EXISTS "AppInitialization" (
            "Id" INTEGER NOT NULL PRIMARY KEY,
            "DemoDataSeeded" BOOLEAN NOT NULL
        );
        """;

    private const string ClaimDemoSeedSql = """
        INSERT INTO "AppInitialization" ("Id", "DemoDataSeeded")
        VALUES (1, TRUE)
        ON CONFLICT ("Id") DO NOTHING;
        """;

    private const string EnsurePlannerSettingsSql = """
        INSERT INTO "PlannerSettings" ("Id", "WeeklyCapacityHours")
        VALUES (1, 10.00)
        ON CONFLICT ("Id") DO NOTHING;
        """;

    public static async Task InitializeAsync(
        PlannerDbContext dbContext,
        bool seedDemoData,
        IAppClock clock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clock);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!seedDemoData)
        {
            return;
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                CreateInitializationTableSql,
                cancellationToken);
            var claimedDemoSeed = await dbContext.Database.ExecuteSqlRawAsync(
                ClaimDemoSeedSql,
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                EnsurePlannerSettingsSql,
                cancellationToken);

            if (claimedDemoSeed == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var hasTasks = await dbContext.StudyTasks.AnyAsync(cancellationToken);
            var today = clock.Today;
            var now = clock.UtcNow;
            var demoTasks = new[]
            {
                StudyTask.Create(
                    "Revisar funções para a prova",
                    today.AddDays(2),
                    Importance.Critical,
                    4m,
                    KanbanColumn.InProgress,
                    now),
                StudyTask.Create(
                    "Finalizar resumo de História",
                    today.AddDays(4),
                    Importance.High,
                    3m,
                    KanbanColumn.ThisWeek,
                    now.AddMinutes(1)),
                StudyTask.Create(
                    "Resolver lista de Física",
                    today.AddDays(6),
                    Importance.Medium,
                    5m,
                    KanbanColumn.ThisWeek,
                    now.AddMinutes(2)),
                StudyTask.Create(
                    "Organizar referências do seminário",
                    today.AddDays(10),
                    Importance.Low,
                    2m,
                    KanbanColumn.Planning,
                    now.AddMinutes(3)),
                StudyTask.Create(
                    "Enviar redação argumentativa",
                    today.AddDays(-1),
                    Importance.High,
                    2m,
                    KanbanColumn.Completed,
                    now.AddMinutes(4))
            };

            if (!hasTasks)
            {
                await dbContext.StudyTasks.AddRangeAsync(demoTasks, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
