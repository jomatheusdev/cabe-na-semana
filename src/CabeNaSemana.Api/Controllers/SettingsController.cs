using CabeNaSemana.Api.Contracts;
using CabeNaSemana.Application.Board;
using Microsoft.AspNetCore.Mvc;

namespace CabeNaSemana.Api.Controllers;

[Route("api/settings")]
public sealed class SettingsController(BoardService boardService) : ApiControllerBase
{
    [HttpPut("weekly-capacity")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateWeeklyCapacity(
        UpdateWeeklyCapacityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await boardService.SetCapacityAsync(
            request.WeeklyCapacityHours!.Value,
            cancellationToken);
        return result.IsSuccess ? NoContent() : Failure(result);
    }
}
