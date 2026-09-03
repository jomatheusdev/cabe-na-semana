using CabeNaSemana.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CabeNaSemana.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        PlannerDbContext dbContext,
        bool seedDemoData,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!seedDemoData || await dbContext.StudyTasks.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = DateTime.UtcNow;
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

        await dbContext.StudyTasks.AddRangeAsync(demoTasks, cancellationToken);
        await dbContext.PlannerSettings.AddAsync(
            new PlannerSetting
            {
                Id = PlannerSetting.SingletonId,
                WeeklyCapacityHours = 10m
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
