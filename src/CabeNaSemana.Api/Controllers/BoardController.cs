using CabeNaSemana.Api.Contracts;
using CabeNaSemana.Application.Board;
using Microsoft.AspNetCore.Mvc;

namespace CabeNaSemana.Api.Controllers;

[Route("api/board")]
public sealed class BoardController(BoardService boardService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<BoardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardResponse>> Get(CancellationToken cancellationToken)
    {
        var board = await boardService.GetAsync(cancellationToken);
        return Ok(BoardResponse.From(board));
    }
}
