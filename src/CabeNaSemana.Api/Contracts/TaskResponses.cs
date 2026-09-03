using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Api.Contracts;

public sealed record CreatedTaskResponse(Guid Id);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    DateOnly DueDate,
    Importance Importance,
    decimal EstimatedHours,
    KanbanColumn Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static TaskResponse From(StudyTask task) => new(
        task.Id,
        task.Title,
        task.DueDate,
        task.Importance,
        task.EstimatedHours,
        task.Status,
        task.CreatedAtUtc,
        task.UpdatedAtUtc);
}
