using CabeNaSemana.Domain.Tasks;

namespace CabeNaSemana.Tests.Domain;

public sealed class StudyTaskTests
{
    private static readonly DateTime Now = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_RejectsBlankTitle()
    {
        Assert.Throws<DomainValidationException>(() =>
            StudyTask.Create(" ", new DateOnly(2026, 9, 5), Importance.High, 2m, KanbanColumn.Planning, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_RejectsEffortOutsideSupportedRange(decimal hours)
    {
        Assert.Throws<DomainValidationException>(() =>
            StudyTask.Create("Atividade", new DateOnly(2026, 9, 5), Importance.High, hours, KanbanColumn.Planning, Now));
    }

    [Fact]
    public void MoveTo_ChangesColumnAndUpdateTimestamp()
    {
        var task = StudyTask.Create(
            "Revisar matéria",
            new DateOnly(2026, 9, 5),
            Importance.High,
            2m,
            KanbanColumn.ThisWeek,
            Now);
        var movedAt = Now.AddHours(1);

        task.MoveTo(KanbanColumn.InProgress, movedAt);

        Assert.Equal(KanbanColumn.InProgress, task.Status);
        Assert.Equal(movedAt, task.UpdatedAtUtc);
    }

    [Fact]
    public void MoveTo_RejectsUnknownColumn()
    {
        var task = StudyTask.Create(
            "Revisar matéria",
            new DateOnly(2026, 9, 5),
            Importance.High,
            2m,
            KanbanColumn.ThisWeek,
            Now);

        Assert.Throws<DomainValidationException>(() => task.MoveTo((KanbanColumn)99, Now));
    }
}
