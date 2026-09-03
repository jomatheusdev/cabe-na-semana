using CabeNaSemana.Domain.Planning;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Tests.Domain;

public sealed class WeeklyCapacityPlannerTests
{
    private static readonly DateOnly Today = new(2026, 9, 3);
    private static readonly DateTime Now = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Analyze_MarksLowerPriorityWorkAsOverflow_WhenCapacityIsExceeded()
    {
        var urgent = CreateTask("Prova crítica", Today.AddDays(1), Importance.Critical, 4m);
        var high = CreateTask("Trabalho importante", Today.AddDays(3), Importance.High, 4m);
        var low = CreateTask("Leitura complementar", Today.AddDays(6), Importance.Low, 4m);

        var plan = WeeklyCapacityPlanner.Analyze([low, high, urgent], 8m, Today);

        Assert.Equal(12m, plan.Capacity.UsedHours);
        Assert.Equal(4m, plan.Capacity.OverbookedHours);
        Assert.True(plan.Capacity.IsOverloaded);
        Assert.Equal(CapacityFit.Overflow, plan.Items.Single(item => item.Task.Id == low.Id).CapacityFit);
        Assert.NotEqual(CapacityFit.Overflow, plan.Items.Single(item => item.Task.Id == urgent.Id).CapacityFit);
    }

    [Fact]
    public void Analyze_IgnoresPlanningAndCompletedWorkInWeeklyUsage()
    {
        var thisWeek = CreateTask("Revisão", Today.AddDays(2), Importance.High, 3m);
        var backlog = CreateTask("Pesquisa futura", Today.AddDays(20), Importance.Medium, 8m, KanbanColumn.Planning);
        var completed = CreateTask("Exercícios", Today, Importance.High, 5m, KanbanColumn.Completed);

        var plan = WeeklyCapacityPlanner.Analyze([thisWeek, backlog, completed], 10m, Today);

        Assert.Equal(3m, plan.Capacity.UsedHours);
        Assert.Equal(7m, plan.Capacity.AvailableHours);
        Assert.Equal(CapacityFit.NotScheduled, plan.Items.Single(item => item.Task.Id == backlog.Id).CapacityFit);
        Assert.Equal(CapacityFit.Completed, plan.Items.Single(item => item.Task.Id == completed.Id).CapacityFit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(81)]
    public void Analyze_RejectsCapacityOutsideSupportedRange(decimal capacity)
    {
        Assert.Throws<DomainValidationException>(
            () => WeeklyCapacityPlanner.Analyze([], capacity, Today));
    }

    private static StudyTask CreateTask(
        string title,
        DateOnly dueDate,
        Importance importance,
        decimal hours,
        KanbanColumn status = KanbanColumn.ThisWeek) =>
        StudyTask.Create(title, dueDate, importance, hours, status, Now);
}
