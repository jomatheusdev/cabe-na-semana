using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Domain.Planning;

public enum CapacityFit
{
    NotScheduled = 0,
    Fits = 1,
    NearLimit = 2,
    Overflow = 3,
    Completed = 4
}

public sealed record PlannedTask(
    StudyTask Task,
    PriorityAssessment Priority,
    CapacityFit CapacityFit);

public sealed record CapacitySnapshot(
    decimal WeeklyHours,
    decimal UsedHours,
    decimal AvailableHours,
    decimal OverbookedHours,
    decimal UsagePercentage,
    bool IsOverloaded);

public sealed record WeeklyPlan(
    IReadOnlyList<PlannedTask> Items,
    CapacitySnapshot Capacity);
