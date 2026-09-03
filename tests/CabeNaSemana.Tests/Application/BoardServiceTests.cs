using CabeNaSemana.Application.Board;
using CabeNaSemana.Application.Common;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Tests.Application;

public sealed class BoardServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);
    private readonly FakeClock _clock = new(Now);

    [Fact]
    public async Task CreateAsync_PersistsAValidTask()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);
        var command = new SaveTaskCommand(
            "Preparar seminário",
            new DateOnly(2026, 9, 8),
            Importance.Critical,
            4.5m,
            KanbanColumn.ThisWeek);

        var result = await service.CreateAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Tasks);
        Assert.Equal("Preparar seminário", repository.Tasks[0].Title);
        Assert.Equal(repository.Tasks[0].Id, result.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationErrorsWithoutPersisting()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);
        var command = new SaveTaskCommand(
            string.Empty,
            new DateOnly(2026, 9, 8),
            Importance.High,
            0m,
            KanbanColumn.Planning);

        var result = await service.CreateAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(repository.Tasks);
    }

    [Fact]
    public async Task MoveAsync_MovesExistingTaskBetweenKanbanColumns()
    {
        var task = StudyTask.Create(
            "Resolver exercícios",
            new DateOnly(2026, 9, 5),
            Importance.High,
            3m,
            KanbanColumn.ThisWeek,
            Now);
        var repository = new FakeBoardRepository([task]);
        var service = new BoardService(repository, _clock);

        var result = await service.MoveAsync(task.Id, KanbanColumn.InProgress, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(KanbanColumn.InProgress, task.Status);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheEditableTaskFields()
    {
        var task = StudyTask.Create(
            "Rascunho",
            new DateOnly(2026, 9, 12),
            Importance.Low,
            1m,
            KanbanColumn.Planning,
            Now);
        var repository = new FakeBoardRepository([task]);
        var service = new BoardService(repository, _clock);
        var command = new SaveTaskCommand(
            "Trabalho final",
            new DateOnly(2026, 9, 7),
            Importance.Critical,
            6m,
            KanbanColumn.ThisWeek);

        var result = await service.UpdateAsync(task.Id, command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Trabalho final", task.Title);
        Assert.Equal(Importance.Critical, task.Importance);
        Assert.Equal(6m, task.EstimatedHours);
        Assert.Equal(KanbanColumn.ThisWeek, task.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingTask()
    {
        var task = StudyTask.Create(
            "Descartar rascunho",
            new DateOnly(2026, 9, 12),
            Importance.Low,
            1m,
            KanbanColumn.Planning,
            Now);
        var repository = new FakeBoardRepository([task]);
        var service = new BoardService(repository, _clock);

        var result = await service.DeleteAsync(task.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Tasks);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task SetCapacityAsync_RejectsInvalidCapacityWithoutSaving()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);

        var result = await service.SetCapacityAsync(0m, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(10m, repository.WeeklyCapacity);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SetCapacityAsync_PersistsAValidCapacity()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);

        var result = await service.SetCapacityAsync(18m, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(18m, repository.WeeklyCapacity);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailureForMissingTask()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);
        var command = new SaveTaskCommand(
            "Atividade",
            new DateOnly(2026, 9, 8),
            Importance.Medium,
            2m,
            KanbanColumn.Planning);

        var result = await service.UpdateAsync(Guid.NewGuid(), command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationFailureKind.NotFound, result.FailureKind);
        Assert.Contains(result.Errors.Values.SelectMany(errors => errors), error => error.Contains("não encontrada"));
    }

    [Fact]
    public async Task MoveAsync_ReturnsFailureForUnknownColumn()
    {
        var task = StudyTask.Create(
            "Atividade",
            new DateOnly(2026, 9, 8),
            Importance.Medium,
            2m,
            KanbanColumn.Planning,
            Now);
        var repository = new FakeBoardRepository([task]);
        var service = new BoardService(repository, _clock);

        var result = await service.MoveAsync(task.Id, (KanbanColumn)99, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(KanbanColumn.Planning, task.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailureForMissingTask()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);

        var result = await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task FindAsync_ReturnsExistingTask()
    {
        var task = StudyTask.Create(
            "Atividade",
            new DateOnly(2026, 9, 8),
            Importance.Medium,
            2m,
            KanbanColumn.Planning,
            Now);
        var service = new BoardService(new FakeBoardRepository([task]), _clock);

        var found = await service.FindAsync(task.Id, CancellationToken.None);

        Assert.Same(task, found);
    }

    [Fact]
    public async Task GetAsync_MapsPriorityAndCapacityIntoCardView()
    {
        var task = StudyTask.Create(
            "Prova importante",
            new DateOnly(2026, 9, 4),
            Importance.Critical,
            12m,
            KanbanColumn.ThisWeek,
            Now);
        var service = new BoardService(new FakeBoardRepository([task]), _clock);

        var board = await service.GetAsync(CancellationToken.None);
        var card = Assert.Single(board.Columns.Single(column => column.Status == KanbanColumn.ThisWeek).Tasks);

        Assert.Equal(task.Id, card.Id);
        Assert.Equal("Prova importante", card.Title);
        Assert.Equal(12m, card.EstimatedHours);
        Assert.Equal(CabeNaSemana.Domain.Planning.PriorityLevel.Urgent, card.PriorityLevel);
        Assert.Equal(CabeNaSemana.Domain.Planning.CapacityFit.Overflow, card.CapacityFit);
        Assert.True(board.Capacity.IsOverloaded);
    }

    [Fact]
    public async Task GetAsync_AlwaysReturnsTheFourKanbanColumns()
    {
        var repository = new FakeBoardRepository();
        var service = new BoardService(repository, _clock);

        var board = await service.GetAsync(CancellationToken.None);

        Assert.Collection(
            board.Columns,
            column => Assert.Equal(KanbanColumn.Planning, column.Status),
            column => Assert.Equal(KanbanColumn.ThisWeek, column.Status),
            column => Assert.Equal(KanbanColumn.InProgress, column.Status),
            column => Assert.Equal(KanbanColumn.Completed, column.Status));
    }

    private sealed class FakeClock(DateTime utcNow) : IAppClock
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateOnly Today { get; } = DateOnly.FromDateTime(utcNow);
    }

    private sealed class FakeBoardRepository(IEnumerable<StudyTask>? seed = null) : IBoardRepository
    {
        public List<StudyTask> Tasks { get; } = seed?.ToList() ?? [];
        public int SaveCount { get; private set; }
        public decimal WeeklyCapacity { get; private set; } = 10m;

        public Task<IReadOnlyList<StudyTask>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudyTask>>(Tasks);

        public Task<StudyTask?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Tasks.SingleOrDefault(task => task.Id == id));

        public Task AddAsync(StudyTask task, CancellationToken cancellationToken)
        {
            Tasks.Add(task);
            return Task.CompletedTask;
        }

        public void Remove(StudyTask task) => Tasks.Remove(task);

        public Task<decimal> GetWeeklyCapacityAsync(CancellationToken cancellationToken) =>
            Task.FromResult(WeeklyCapacity);

        public Task SetWeeklyCapacityAsync(decimal hours, CancellationToken cancellationToken)
        {
            WeeklyCapacity = hours;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
