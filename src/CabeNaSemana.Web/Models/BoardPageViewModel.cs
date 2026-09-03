using CabeNaSemana.Application.Board;

namespace CabeNaSemana.Web.Models;

public sealed record BoardPageViewModel(BoardSnapshot Board, TaskFormModel NewTask);

public sealed record EditTaskViewModel(Guid Id, TaskFormModel Task);

public sealed class CapacityFormModel
{
    [System.ComponentModel.DataAnnotations.Range(
        1,
        80,
        ErrorMessage = "Informe entre 1 e 80 horas.")]
    public decimal WeeklyCapacity { get; init; }
}
