using CabeNaSemana.Application.Common;
using CabeNaSemana.Domain.Tasks;
using CabeNaSemana.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CabeNaSemana.Tests.Infrastructure;

public sealed class DatabaseInitializerTests
{
    private static readonly IAppClock Clock = new FixedClock();

    [Fact]
    public async Task InitializeAsync_SeedsTasksWithoutReplacingExistingCapacity()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        await context.PlannerSettings.AddAsync(new PlannerSetting
        {
            Id = PlannerSetting.SingletonId,
            WeeklyCapacityHours = 15m
        });
        await context.SaveChangesAsync();

        await DatabaseInitializer.InitializeAsync(context, seedDemoData: true, Clock);

        Assert.Equal(5, await context.StudyTasks.CountAsync());
        Assert.Equal(
            15m,
            await context.PlannerSettings
                .Where(setting => setting.Id == PlannerSetting.SingletonId)
                .Select(setting => setting.WeeklyCapacityHours)
                .SingleAsync());
    }

    [Fact]
    public async Task InitializeAsync_AddsMissingCapacityWithoutDuplicatingExistingTasks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        await context.StudyTasks.AddAsync(StudyTask.Create(
            "Atividade preservada",
            new DateOnly(2026, 9, 10),
            Importance.High,
            2m,
            KanbanColumn.Planning,
            new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        await DatabaseInitializer.InitializeAsync(context, seedDemoData: true, Clock);

        Assert.Single(await context.StudyTasks.ToListAsync());
        Assert.Equal(
            10m,
            await context.PlannerSettings
                .Where(setting => setting.Id == PlannerSetting.SingletonId)
                .Select(setting => setting.WeeklyCapacityHours)
                .SingleAsync());
    }

    [Fact]
    public async Task InitializeAsync_DoesNotReseedTasksAfterUserDeletesEveryTask()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        await DatabaseInitializer.InitializeAsync(context, seedDemoData: true, Clock);
        Assert.Equal(5, await context.StudyTasks.CountAsync());

        context.StudyTasks.RemoveRange(context.StudyTasks);
        await context.SaveChangesAsync();

        await DatabaseInitializer.InitializeAsync(context, seedDemoData: true, Clock);

        Assert.Empty(await context.StudyTasks.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_ClaimsDemoSeedOnceAcrossConcurrentContexts()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"cabe-na-semana-initializer-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=10;Pooling=False";

        try
        {
            await using (var setupContext = CreateContext(connectionString))
            {
                await setupContext.Database.EnsureCreatedAsync();
            }

            await using var firstContext = CreateContext(connectionString);
            await using var secondContext = CreateContext(connectionString);

            await Task.WhenAll(
                DatabaseInitializer.InitializeAsync(firstContext, seedDemoData: true, Clock),
                DatabaseInitializer.InitializeAsync(secondContext, seedDemoData: true, Clock));

            await using var verificationContext = CreateContext(connectionString);
            Assert.Equal(5, await verificationContext.StudyTasks.CountAsync());
            Assert.Single(await verificationContext.PlannerSettings.ToListAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static PlannerDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite(connection)
            .Options;
        return new PlannerDbContext(options);
    }

    private static PlannerDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new PlannerDbContext(options);
    }

    private sealed class FixedClock : IAppClock
    {
        public DateTime UtcNow { get; } =
            new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

        public DateOnly Today { get; } = new(2026, 9, 3);
    }
}
