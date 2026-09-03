using CabeNaSemana.Domain.Planning;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Application.Board;

public sealed record SaveTaskCommand(
    string Title,
    DateOnly DueDate,
    Importance Importance,
    decimal EstimatedHours,
    KanbanColumn Status);

public sealed record BoardTask(
    Guid Id,
    string Title,
    DateOnly DueDate,
    Importance Importance,
    decimal EstimatedHours,
    KanbanColumn Status,
    int PriorityScore,
    PriorityLevel PriorityLevel,
    string PriorityExplanation,
    int DaysUntilDue,
    CapacityFit CapacityFit);

public sealed record BoardColumn(
    KanbanColumn Status,
    IReadOnlyList<BoardTask> Tasks);

public sealed record BoardSnapshot(
    IReadOnlyList<BoardColumn> Columns,
    CapacitySnapshot Capacity,
    DateOnly Today)
{
    public int TotalTasks => Columns.Sum(column => column.Tasks.Count);
    public int CompletedTasks => Columns
        .Single(column => column.Status == KanbanColumn.Completed)
        .Tasks.Count;
}
