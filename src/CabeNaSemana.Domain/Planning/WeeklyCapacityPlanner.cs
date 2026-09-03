using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Domain.Planning;

public static class WeeklyCapacityPlanner
{
    public const decimal MinimumWeeklyHours = 1m;
    public const decimal MaximumWeeklyHours = 80m;

    public static WeeklyPlan Analyze(
        IEnumerable<StudyTask> tasks,
        decimal weeklyCapacity,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ValidateCapacity(weeklyCapacity);

        var prioritized = tasks
            .Select(task => new
            {
                Task = task,
                Priority = PriorityEngine.Assess(task, today)
            })
            .OrderByDescending(item => item.Priority.Score)
            .ThenBy(item => item.Task.DueDate)
            .ThenBy(item => item.Task.CreatedAtUtc)
            .ToList();

        var usedHours = prioritized
            .Where(item => IsScheduled(item.Task.Status))
            .Sum(item => item.Task.EstimatedHours);
        var allocatedHours = 0m;
        var plannedTasks = new List<PlannedTask>(prioritized.Count);

        foreach (var item in prioritized)
        {
            plannedTasks.Add(new PlannedTask(
                item.Task,
                item.Priority,
                GetCapacityFit(item.Task, weeklyCapacity, ref allocatedHours)));
        }

        var overbookedHours = Math.Max(usedHours - weeklyCapacity, 0m);
        var availableHours = Math.Max(weeklyCapacity - usedHours, 0m);
        var percentage = Math.Round(usedHours / weeklyCapacity * 100m, 1);

        return new WeeklyPlan(
            plannedTasks,
            new CapacitySnapshot(
                weeklyCapacity,
                usedHours,
                availableHours,
                overbookedHours,
                percentage,
                overbookedHours > 0m));
    }

    public static void ValidateCapacity(decimal weeklyCapacity)
    {
        if (weeklyCapacity is < MinimumWeeklyHours or > MaximumWeeklyHours)
        {
            throw DomainValidationException.For(
                "WeeklyCapacity",
                $"A capacidade deve ficar entre {MinimumWeeklyHours:0} e {MaximumWeeklyHours:0} horas.");
        }
    }

    private static CapacityFit GetCapacityFit(
        StudyTask task,
        decimal weeklyCapacity,
        ref decimal allocatedHours)
    {
        if (task.Status == KanbanColumn.Completed)
        {
            return CapacityFit.Completed;
        }

        if (!IsScheduled(task.Status))
        {
            return CapacityFit.NotScheduled;
        }

        allocatedHours += task.EstimatedHours;
        if (allocatedHours > weeklyCapacity)
        {
            return CapacityFit.Overflow;
        }

        return allocatedHours >= weeklyCapacity * 0.8m
            ? CapacityFit.NearLimit
            : CapacityFit.Fits;
    }

    private static bool IsScheduled(KanbanColumn status) =>
        status is KanbanColumn.ThisWeek or KanbanColumn.InProgress;
}
