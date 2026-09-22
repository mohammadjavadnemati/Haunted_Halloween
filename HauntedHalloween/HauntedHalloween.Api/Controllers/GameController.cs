using HauntedHalloween.Domain;
using HauntedHalloween.Engine;
using Microsoft.AspNetCore.Mvc;
using HauntedHalloween.Api;
using System.Collections.Concurrent;

namespace HauntedHalloween.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    
    // فعلاً in-memory تا فاز 10 (multiplayer sync) که به‌درستی persist/broadcast میشه
    internal static readonly ConcurrentDictionary<string, GameState> Games = new();
    private readonly GameBroadcaster _broadcaster;
    public GameController(GameBroadcaster broadcaster) => _broadcaster = broadcaster;

    public record CreatePlayerRequest(string Id, string Name, string StartTileId);
    public record CreateGameRequest(List<CreatePlayerRequest> Players);

    [HttpPost("create")]
    public async Task<ActionResult<GameState>> Create([FromBody] CreateGameRequest request)
    {
        try
        {
            var gameId = Guid.NewGuid().ToString();
            var state = GameSetupService.CreateNewGame(
                gameId,
                request.Players.Select(p => (p.Id, p.Name, p.StartTileId)).ToList());

            Games[gameId] = state;
            await _broadcaster.BroadcastState(state);
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
    public async Task<ActionResult<TurnResult>> BeginTurn(string gameId)
    {
        if (!Games.TryGetValue(gameId, out var state))
            return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(TurnService.BeginTurn(state));
    }

    [HttpPost("{gameId}/end-turn")]
    public async Task<ActionResult<TurnResult>> EndTurn(string gameId)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        var result = TurnService.EndTurn(state);
        await _broadcaster.BroadcastState(state);
        await _broadcaster.BroadcastLog(gameId, result.Log);
        return Ok(result);
    }

    public record BedtimeRequest(string PlayerId);

    [HttpPost("{gameId}/bedtime")]
    public async Task<ActionResult<TurnResult>> Bedtime(string gameId, [FromBody] BedtimeRequest request)
    {
        if (!Games.TryGetValue(gameId, out var state))
            return NotFound();
        await _broadcaster.BroadcastState(state);
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
    public async Task<ActionResult<MoveResult>> Move(string gameId, [FromBody] MoveRequest request)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        var result = MovementService.ExecuteMove(state, request.PlayerId, request.DestinationTileId, request.MaxDistance);
        await _broadcaster.BroadcastState(state);
        return Ok(result);
    }
    [HttpGet("{gameId}/scores")]
    public ActionResult<List<ScoreBreakdown>> Scores(string gameId)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        return Ok(ScoringService.CalculateAllScores(state));
    }
    [HttpPost("{gameId}/ghost/activate-check")]
    public async Task<ActionResult> ActivateGhost1(string gameId, [FromQuery] string playerLandedTileId)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        GhostService.CheckActivateGhost1(state, playerLandedTileId);
        await _broadcaster.BroadcastState(state);
        return Ok(state.Ghosts);
    }

    public record GhostMoveRequest(string GhostId, string DestinationTileId);

    [HttpPost("{gameId}/ghost/move")]
    public async Task<ActionResult<GhostMoveResult>> MoveGhost(string gameId, [FromBody] GhostMoveRequest request)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        var rng = new Random();
        var face = GhostDie.Roll(rng);
        var ghostId = Enum.Parse<GhostId>(request.GhostId);
        await _broadcaster.BroadcastState(state);
        return Ok(GhostService.MoveGhost(state, ghostId, request.DestinationTileId, face, rng));
    }
    [HttpPost("{gameId}/haunted-house/visit")]
    public async Task<ActionResult<HauntedHouseResult>> VisitHauntedHouse(string gameId, [FromQuery] string playerId)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(HauntedHouseService.Visit(state, playerId, new Random()));
    }

    public record GlowStickChoiceRequest(string PlayerId, bool UseGlowStick, string AttackerGhostId);

    [HttpPost("{gameId}/ghost/resolve-glowstick")]
    public async Task<ActionResult<GhostMoveResult>> ResolveGlowStick(string gameId, [FromBody] GlowStickChoiceRequest request)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        var ghostId = Enum.Parse<GhostId>(request.AttackerGhostId);
        await _broadcaster.BroadcastState(state);
        return Ok(GhostService.ResolveGlowStickChoice(state, request.PlayerId, request.UseGlowStick, ghostId));
    }
    public record BoostHouseRequest(string PlayerId, int HouseNumber);
    [HttpPost("{gameId}/boost/collect-house")]
    public async Task<ActionResult<BoostResult>> CollectHouseBoost(string gameId, [FromBody] BoostHouseRequest r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.CollectNormalHouseBoost(state, r.PlayerId, r.HouseNumber));
    }

    public record Boost2Request(string PlayerId);
    [HttpPost("{gameId}/boost/2")]
    public async Task<ActionResult<BoostResult>> Boost2(string gameId, [FromBody] Boost2Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost2ExtraGlowStick(state, r.PlayerId));
    }

    public record Boost3Request(string PlayerId, CandyType CandyType, int AmountReceived);
    [HttpPost("{gameId}/boost/3")]
    public async Task<ActionResult<BoostResult>> Boost3(string gameId, [FromBody] Boost3Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost3DoubleCandy(state, r.PlayerId, r.CandyType, r.AmountReceived));
    }

    public record Boost4Request(string PlayerId);
    [HttpPost("{gameId}/boost/4")]
    public async Task<ActionResult<BoostResult>> Boost4(string gameId, [FromBody] Boost4Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost4RideHome(state, r.PlayerId));
    }

    public record Boost5Request(string PlayerId, string TargetGhost);
    [HttpPost("{gameId}/boost/5")]
    public async Task<ActionResult<BoostResult>> Boost5(string gameId, [FromBody] Boost5Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost5Banish(state, r.PlayerId, Enum.Parse<GhostId>(r.TargetGhost)));
    }

    public record Boost6Request(string PlayerId, int HouseNumber);
    [HttpPost("{gameId}/boost/6")]
    public async Task<ActionResult<BoostResult>> Boost6(string gameId, [FromBody] Boost6Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost6Revisit(state, r.PlayerId, r.HouseNumber));
    }

    public record Boost7Request(string PlayerId, string TargetPlayerId);
    [HttpPost("{gameId}/boost/7")]
    public async Task<ActionResult<BoostResult>> Boost7(string gameId, [FromBody] Boost7Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost7Switch(state, r.PlayerId, r.TargetPlayerId));
    }

    public record Boost8Request(string PlayerId);
    [HttpPost("{gameId}/boost/8")]
    public async Task<ActionResult<BoostResult>> Boost8(string gameId, [FromBody] Boost8Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.ConsumeBoost8AfterSignHit(state, r.PlayerId));
    }

    public record Boost9Request(string PlayerId);
    [HttpPost("{gameId}/boost/9")]
    public async Task<ActionResult<BoostResult>> Boost9(string gameId, [FromBody] Boost9Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost9RaidCoffin(state, r.PlayerId));
    }

    public record Boost10Request(string PlayerId, CandyType StolenType, int StolenAmount, string VictimPlayerId);
    [HttpPost("{gameId}/boost/10")]
    public async Task<ActionResult<BoostResult>> Boost10(string gameId, [FromBody] Boost10Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost10FriendlyGhost(state, r.PlayerId, r.StolenType, r.StolenAmount, r.VictimPlayerId));
    }

    public record Boost11Request(string PlayerId);
    [HttpPost("{gameId}/boost/11")]
    public async Task<ActionResult<BoostResult>> Boost11(string gameId, [FromBody] Boost11Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost11BooBeGone(state, r.PlayerId));
    }

    public record Boost12Request(string PlayerId, string TargetPlayerId);
    [HttpPost("{gameId}/boost/12")]
    public async Task<ActionResult<BoostResult>> Boost12(string gameId, [FromBody] Boost12Request r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(BoostService.UseBoost12Web(state, r.PlayerId, r.TargetPlayerId));
    }

    public record PortalChoiceRequest(string PlayerId, bool Teleport, string? ChosenPortalTileId);
    [HttpPost("{gameId}/boost/1-portal-choice")]
    public async Task<ActionResult<MoveResult>> Boost1Portal(string gameId, [FromBody] PortalChoiceRequest r)
    {
        if (!Games.TryGetValue(gameId, out var state)) return NotFound();
        await _broadcaster.BroadcastState(state);
        return Ok(MovementService.ResolvePortalChoice(state, r.PlayerId, r.Teleport, r.ChosenPortalTileId));
    }




[HttpPost("{gameId}/house/visit")]
public async Task<ActionResult<HouseVisitResult>> VisitHouse(string gameId, [FromQuery] string playerId)
{
    if (!Games.TryGetValue(gameId, out var state)) return NotFound();
    var player = state.Players.FirstOrDefault(p => p.Id == playerId);
    if (player == null) return BadRequest("Player not found.");
    if (!state.Tiles.TryGetValue(player.CurrentTileId, out var tile) || tile.HouseNumber is not int houseNumber)
        return BadRequest("Player is not standing on a house.");

    var result = HouseVisitService.VisitHouse(state, playerId, houseNumber, new Random());
    await _broadcaster.BroadcastState(state);
    await _broadcaster.BroadcastLog(gameId, result.Log);
    return Ok(result);
}
    


}