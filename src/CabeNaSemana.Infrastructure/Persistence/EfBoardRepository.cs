using CabeNaSemana.Application.Board;
using CabeNaSemana.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CabeNaSemana.Infrastructure.Persistence;

public sealed class EfBoardRepository(PlannerDbContext dbContext) : IBoardRepository
{
    private const decimal DefaultWeeklyCapacity = 10m;

    public async Task<IReadOnlyList<StudyTask>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.StudyTasks
            .AsNoTracking()
            .OrderBy(task => task.Status)
            .ThenBy(task => task.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<StudyTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.StudyTasks.FindAsync([id], cancellationToken);

    public async Task AddAsync(StudyTask task, CancellationToken cancellationToken) =>
        await dbContext.StudyTasks.AddAsync(task, cancellationToken);

    public void Remove(StudyTask task) => dbContext.StudyTasks.Remove(task);

    public async Task<decimal> GetWeeklyCapacityAsync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.PlannerSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PlannerSetting.SingletonId, cancellationToken);

        return setting?.WeeklyCapacityHours ?? DefaultWeeklyCapacity;
    }

    public async Task SetWeeklyCapacityAsync(decimal hours, CancellationToken cancellationToken)
    {
        var setting = await dbContext.PlannerSettings
            .SingleOrDefaultAsync(item => item.Id == PlannerSetting.SingletonId, cancellationToken);

        if (setting is null)
        {
            await dbContext.PlannerSettings.AddAsync(
                new PlannerSetting
                {
                    Id = PlannerSetting.SingletonId,
                    WeeklyCapacityHours = hours
                },
                cancellationToken);
            return;
        }

        setting.WeeklyCapacityHours = hours;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
