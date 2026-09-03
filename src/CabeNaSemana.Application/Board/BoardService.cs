using CabeNaSemana.Application.Common;
using CabeNaSemana.Domain.Planning;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Application.Board;

public sealed class BoardService
{
    private readonly IBoardRepository _repository;
    private readonly IAppClock _clock;

    public BoardService(IBoardRepository repository, IAppClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<BoardSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        var tasks = await _repository.ListAsync(cancellationToken);
        var weeklyCapacity = await _repository.GetWeeklyCapacityAsync(cancellationToken);
        var plan = WeeklyCapacityPlanner.Analyze(tasks, weeklyCapacity, _clock.Today);

        var items = plan.Items.Select(MapTask).ToList();
        var columns = Enum.GetValues<KanbanColumn>()
            .Select(status => new BoardColumn(
                status,
                items.Where(item => item.Status == status).ToList()))
            .ToList();

        return new BoardSnapshot(columns, plan.Capacity, _clock.Today);
    }

    public async Task<StudyTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.FindAsync(id, cancellationToken);

    public async Task<OperationResult> CreateAsync(
        SaveTaskCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var task = StudyTask.Create(
                command.Title,
                command.DueDate,
                command.Importance,
                command.EstimatedHours,
                command.Status,
                _clock.UtcNow);

            await _repository.AddAsync(task, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.From(exception);
        }
    }

    public async Task<OperationResult> UpdateAsync(
        Guid id,
        SaveTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = await _repository.FindAsync(id, cancellationToken);
        if (task is null)
        {
            return OperationResult.Failure(string.Empty, "Atividade não encontrada.");
        }

        try
        {
            task.Update(
                command.Title,
                command.DueDate,
                command.Importance,
                command.EstimatedHours,
                command.Status,
                _clock.UtcNow);
            await _repository.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.From(exception);
        }
    }

    public async Task<OperationResult> MoveAsync(
        Guid id,
        KanbanColumn status,
        CancellationToken cancellationToken)
    {
        var task = await _repository.FindAsync(id, cancellationToken);
        if (task is null)
        {
            return OperationResult.Failure(string.Empty, "Atividade não encontrada.");
        }

        try
        {
            task.MoveTo(status, _clock.UtcNow);
            await _repository.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.From(exception);
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _repository.FindAsync(id, cancellationToken);
        if (task is null)
        {
            return OperationResult.Failure(string.Empty, "Atividade não encontrada.");
        }

        _repository.Remove(task);
        await _repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetCapacityAsync(
        decimal hours,
        CancellationToken cancellationToken)
    {
        try
        {
            WeeklyCapacityPlanner.ValidateCapacity(hours);
            await _repository.SetWeeklyCapacityAsync(hours, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.From(exception);
        }
    }

    private static BoardTask MapTask(PlannedTask item) => new(
        item.Task.Id,
        item.Task.Title,
        item.Task.DueDate,
        item.Task.Importance,
        item.Task.EstimatedHours,
        item.Task.Status,
        item.Priority.Score,
        item.Priority.Level,
        item.Priority.Explanation,
        item.Priority.DaysUntilDue,
        item.CapacityFit);
}
