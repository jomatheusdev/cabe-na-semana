using CabeNaSemana.Domain.Tasks;
using CabeNaSemana.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CabeNaSemana.Tests.Infrastructure;

public sealed class EfBoardRepositoryTests
{
    [Fact]
    public async Task Repository_RoundTripsTaskAndWeeklyCapacity_InSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new PlannerDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new EfBoardRepository(context);
        var task = StudyTask.Create(
            "Revisar química",
            new DateOnly(2026, 9, 6),
            Importance.High,
            3m,
            KanbanColumn.ThisWeek,
            new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(task, CancellationToken.None);
        await repository.SetWeeklyCapacityAsync(14m, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var saved = await repository.FindAsync(task.Id, CancellationToken.None);
        var capacity = await repository.GetWeeklyCapacityAsync(CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("Revisar química", saved.Title);
        Assert.Equal(14m, capacity);
    }
}
