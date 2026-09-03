namespace CabeNaSemana.Domain.Tasks;

public sealed class StudyTask
{
    public const int MaximumTitleLength = 120;
    public const decimal MinimumEstimatedHours = 0.25m;
    public const decimal MaximumEstimatedHours = 100m;

    private StudyTask()
    {
        Title = string.Empty;
    }

    private StudyTask(
        Guid id,
        string title,
        DateOnly dueDate,
        Importance importance,
        decimal estimatedHours,
        KanbanColumn status,
        DateTime createdAtUtc)
    {
        Id = id;
        Title = title;
        DueDate = dueDate;
        Importance = importance;
        EstimatedHours = estimatedHours;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Importance Importance { get; private set; }
    public decimal EstimatedHours { get; private set; }
    public KanbanColumn Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static StudyTask Create(
        string title,
        DateOnly dueDate,
        Importance importance,
        decimal estimatedHours,
        KanbanColumn status,
        DateTime createdAtUtc)
    {
        var normalizedTitle = Validate(title, importance, estimatedHours, status);

        return new StudyTask(
            Guid.NewGuid(),
            normalizedTitle,
            dueDate,
            importance,
            estimatedHours,
            status,
            createdAtUtc);
    }

    public void Update(
        string title,
        DateOnly dueDate,
        Importance importance,
        decimal estimatedHours,
        KanbanColumn status,
        DateTime updatedAtUtc)
    {
        var normalizedTitle = Validate(title, importance, estimatedHours, status);

        Title = normalizedTitle;
        DueDate = dueDate;
        Importance = importance;
        EstimatedHours = estimatedHours;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void MoveTo(KanbanColumn status, DateTime updatedAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            throw DomainValidationException.For(nameof(Status), "Selecione uma coluna válida.");
        }

        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string Validate(
        string title,
        Importance importance,
        decimal estimatedHours,
        KanbanColumn status)
    {
        var errors = new Dictionary<string, string[]>();
        var normalizedTitle = title?.Trim() ?? string.Empty;

        if (normalizedTitle.Length is 0 or > MaximumTitleLength)
        {
            errors[nameof(Title)] = [
                $"Informe um título entre 1 e {MaximumTitleLength} caracteres."
            ];
        }

        if (estimatedHours is < MinimumEstimatedHours or > MaximumEstimatedHours)
        {
            errors[nameof(EstimatedHours)] = [
                $"O esforço deve ficar entre {MinimumEstimatedHours:0.##} e {MaximumEstimatedHours:0.##} horas."
            ];
        }

        if (!Enum.IsDefined(importance))
        {
            errors[nameof(Importance)] = ["Selecione uma importância válida."];
        }

        if (!Enum.IsDefined(status))
        {
            errors[nameof(Status)] = ["Selecione uma coluna válida."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        return normalizedTitle;
    }
}
