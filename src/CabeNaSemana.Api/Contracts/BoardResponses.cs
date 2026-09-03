using CabeNaSemana.Application.Board;
using CabeNaSemana.Domain.Planning;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Api.Contracts;

public sealed record BoardResponse(
    IReadOnlyList<BoardColumnResponse> Columns,
    CapacityResponse Capacity,
    DateOnly Today,
    int TotalTasks,
    int CompletedTasks)
{
    public static BoardResponse From(BoardSnapshot board) => new(
        board.Columns.Select(BoardColumnResponse.From).ToList(),
        CapacityResponse.From(board.Capacity),
        board.Today,
        board.TotalTasks,
        board.CompletedTasks);
}

public sealed record BoardColumnResponse(
    KanbanColumn Status,
    IReadOnlyList<BoardTaskResponse> Tasks)
{
    public static BoardColumnResponse From(BoardColumn column) => new(
        column.Status,
        column.Tasks.Select(BoardTaskResponse.From).ToList());
}

public sealed record BoardTaskResponse(
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
    CapacityFit CapacityFit)
{
    public static BoardTaskResponse From(BoardTask task) => new(
        task.Id,
        task.Title,
        task.DueDate,
        task.Importance,
        task.EstimatedHours,
        task.Status,
        task.PriorityScore,
        task.PriorityLevel,
        task.PriorityExplanation,
        task.DaysUntilDue,
        task.CapacityFit);
}

public sealed record CapacityResponse(
    decimal WeeklyHours,
    decimal UsedHours,
    decimal AvailableHours,
    decimal OverbookedHours,
    decimal UsagePercentage,
    bool IsOverloaded)
{
    public static CapacityResponse From(CapacitySnapshot capacity) => new(
        capacity.WeeklyHours,
        capacity.UsedHours,
        capacity.AvailableHours,
        capacity.OverbookedHours,
        capacity.UsagePercentage,
        capacity.IsOverloaded);
}
