using CabeNaSemana.Domain.Planning;
using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Tests.Domain;

public sealed class PriorityEngineTests
{
    private static readonly DateOnly Today = new(2026, 9, 3);
    private static readonly DateTime Now = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Assess_GivesHigherScoreToEarlierDeadline_WhenOtherFactorsMatch()
    {
        var dueTomorrow = CreateTask("Prova amanhã", Today.AddDays(1), Importance.High, 3m);
        var dueNextMonth = CreateTask("Prova no mês que vem", Today.AddDays(30), Importance.High, 3m);

        var urgent = PriorityEngine.Assess(dueTomorrow, Today);
        var later = PriorityEngine.Assess(dueNextMonth, Today);

        Assert.True(urgent.Score > later.Score);
        Assert.Equal(PriorityLevel.Urgent, urgent.Level);
    }

    [Fact]
    public void Assess_GivesHigherScoreToCriticalImportance()
    {
        var critical = CreateTask("Seminário", Today.AddDays(5), Importance.Critical, 4m);
        var low = CreateTask("Leitura opcional", Today.AddDays(5), Importance.Low, 4m);

        var criticalAssessment = PriorityEngine.Assess(critical, Today);
        var lowAssessment = PriorityEngine.Assess(low, Today);

        Assert.True(criticalAssessment.Score > lowAssessment.Score);
        Assert.Contains("crítica", criticalAssessment.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assess_ExplainsOverdueDeadline()
    {
        var overdue = CreateTask("Trabalho atrasado", Today.AddDays(-2), Importance.Medium, 2m);

        var assessment = PriorityEngine.Assess(overdue, Today);

        Assert.Contains("atrasada", assessment.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(-2, assessment.DaysUntilDue);
    }

    [Fact]
    public void Assess_CompletedTaskHasNoActivePriority()
    {
        var completed = CreateTask(
            "Lista entregue",
            Today,
            Importance.Critical,
            1m,
            KanbanColumn.Completed);

        var assessment = PriorityEngine.Assess(completed, Today);

        Assert.Equal(0, assessment.Score);
        Assert.Equal(PriorityLevel.None, assessment.Level);
    }

    private static StudyTask CreateTask(
        string title,
        DateOnly dueDate,
        Importance importance,
        decimal hours,
        KanbanColumn status = KanbanColumn.ThisWeek) =>
        StudyTask.Create(title, dueDate, importance, hours, status, Now);
}
