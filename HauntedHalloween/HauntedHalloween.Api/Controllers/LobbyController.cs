using System.Collections.Concurrent;
using HauntedHalloween.Domain;
using HauntedHalloween.Engine;
using Microsoft.AspNetCore.Mvc;

namespace HauntedHalloween.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LobbyController : ControllerBase
{
    
    private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();
    private readonly GameBroadcaster _broadcaster;
    public LobbyController(GameBroadcaster broadcaster) => _broadcaster = broadcaster;

    public record NameRequest(string? Name);
    public record JoinResponse(string Code, string PlayerId, Lobby Lobby);
    public record CharacterRequest(string PlayerId, int Character);
    public record StartTileRequest(string PlayerId, string StartTileId);
    public record ReadyRequest(string PlayerId, bool Ready);
    public record LeaveRequest(string PlayerId);

    private static Lobby? Find(string code) => Lobbies.GetValueOrDefault(code.Trim().ToUpperInvariant());

    [HttpPost("create")]
    public ActionResult<JoinResponse> Create([FromBody] NameRequest req)
    {
        var rng = new Random();
        var lobby = new Lobby();
        do { lobby.Code = LobbyService.NewCode(rng); } while (!Lobbies.TryAdd(lobby.Code, lobby));

        LobbyResult r;
        lock (lobby) r = LobbyService.Join(lobby, req.Name);
        if (!r.Success) { Lobbies.TryRemove(lobby.Code, out _); return BadRequest(r.Error); }
        return Ok(new JoinResponse(lobby.Code, r.Player!.Id, lobby));
    }

    [HttpPost("{code}/join")]
    public async Task<ActionResult<JoinResponse>> Join(string code, [FromBody] NameRequest req)
    {
        var lobby = Find(code);
        if (lobby == null) return NotFound("Room not found.");
        LobbyResult r;
        lock (lobby) r = LobbyService.Join(lobby, req.Name);
        if (!r.Success) return BadRequest(r.Error);
        await _broadcaster.BroadcastLobby(lobby);
        return Ok(new JoinResponse(lobby.Code, r.Player!.Id, lobby));
    }

    [HttpGet("{code}")]
    public ActionResult<Lobby> Get(string code)
    {
        var lobby = Find(code);
        return lobby == null ? NotFound("Room not found.") : Ok(lobby);
    }

    [HttpPost("{code}/character")]
    public Task<ActionResult<Lobby>> SetCharacter(string code, [FromBody] CharacterRequest r) =>
        Mutate(code, l => LobbyService.SetCharacter(l, r.PlayerId, r.Character));

    [HttpPost("{code}/start-tile")]
    public Task<ActionResult<Lobby>> SetStartTile(string code, [FromBody] StartTileRequest r) =>
        Mutate(code, l => LobbyService.SetStartTile(l, r.PlayerId, r.StartTileId));

    [HttpPost("{code}/ready")]
    public Task<ActionResult<Lobby>> SetReady(string code, [FromBody] ReadyRequest r) =>
        Mutate(code, l => LobbyService.SetReady(l, r.PlayerId, r.Ready), tryStart: true);

    [HttpPost("{code}/leave")]
    public Task<ActionResult<Lobby>> Leave(string code, [FromBody] LeaveRequest r) =>
        Mutate(code, l => LobbyService.Leave(l, r.PlayerId));

    // Runs a lobby change under lock; starts the game once everyone is ready.
    private async Task<ActionResult<Lobby>> Mutate(string code, Func<Lobby, LobbyResult> action, bool tryStart = false)
    {
        var lobby = Find(code);
        if (lobby == null) return NotFound("Room not found.");

        LobbyResult result;
        GameState? started = null;
        lock (lobby)
        {
            result = action(lobby);
            if (result.Success && tryStart && LobbyService.ShouldStart(lobby))
            {
                
                started = LobbyService.StartGame(lobby);
                GameController.Games[lobby.Code] = started;
            }
        }
        if (!result.Success) return BadRequest(result.Error);

        if (started != null) await _broadcaster.BroadcastState(started);
        await _broadcaster.BroadcastLobby(lobby);
        return Ok(lobby);
    }
}