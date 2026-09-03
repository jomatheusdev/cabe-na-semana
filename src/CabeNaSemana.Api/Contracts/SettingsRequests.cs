using System.ComponentModel.DataAnnotations;

namespace CabeNaSemana.Api.Contracts;

public sealed class UpdateWeeklyCapacityRequest
{
    [Required(ErrorMessage = "Informe a capacidade semanal.")]
    [Range(1, 80, ErrorMessage = "A capacidade deve ficar entre 1 e 80 horas.")]
    public decimal? WeeklyCapacityHours { get; init; }
}
