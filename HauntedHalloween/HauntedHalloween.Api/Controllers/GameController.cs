using HauntedHalloween.Domain;
using HauntedHalloween.Engine;
using Microsoft.AspNetCore.Mvc;

namespace HauntedHalloween.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    // فعلاً in-memory تا فاز 10 (multiplayer sync) که به‌درستی persist/broadcast میشه
    private static readonly Dictionary<string, GameState> Games = new();

    public record CreatePlayerRequest(string Id, string Name, string StartTileId);
    public record CreateGameRequest(List<CreatePlayerRequest> Players);

    [HttpPost("create")]
    public ActionResult<GameState> Create([FromBody] CreateGameRequest request)
    {
        try
        {
            var gameId = Guid.NewGuid().ToString();
            var state = GameSetupService.CreateNewGame(
                gameId,
                request.Players.Select(p => (p.Id, p.Name, p.StartTileId)).ToList());

            Games[gameId] = state;
            return Ok(state);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{gameId}")]
    public ActionResult<GameState> Get(string gameId)
    {
        if (!Games.TryGetValue(gameId, out var state))
            return NotFound();
        return Ok(state);
    }

    [HttpPost("{gameId}/begin-turn")]
    public ActionResult<TurnResult> BeginTurn(string gameId)
    {
        if (!Games.TryGetValue(gameId, out var state))
            return NotFound();
        return Ok(TurnService.BeginTurn(state));
    }

    [HttpPost("{gameId}/end-turn")]
    public ActionResult<TurnResult> EndTurn(string gameId)
    {
        if (!Games.TryGetValue(gameId, out var state))
            return NotFound();
        return Ok(TurnService.EndTurn(state));
    }

    public record BedtimeRequest(string PlayerId);

    [HttpPost("{gameId}/bedtime")]
    public ActionResult<TurnResult> Bedtime(string gameId, [FromBody] BedtimeRequest request)
    {
        if (!Games.TryGetValue(gameId, out var state))
            return NotFound();
        return Ok(TurnService.TriggerBedtime(state, request.PlayerId));
    }
    [HttpGet("{gameId}/reachable-tiles")]
    public ActionResult<List<ReachableTile>> ReachableTiles(string gameId, [FromQuery] string playerId, [FromQuery] int maxDistance)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) return BadRequest("Player not found.");
        return Ok(MovementService.GetReachableTiles(state, player.CurrentTileId, maxDistance));
    }

    public record MoveRequest(string PlayerId, string DestinationTileId, int MaxDistance);

    [HttpPost("{gameId}/move")]
    public ActionResult<MoveResult> Move(string gameId, [FromBody] MoveRequest request)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        return Ok(MovementService.ExecuteMove(state, request.PlayerId, request.DestinationTileId, request.MaxDistance));
    }
}