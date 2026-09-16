using HauntedHalloween.Domain;
using Microsoft.AspNetCore.SignalR;
using HauntedHalloween.Api.Hubs;

namespace HauntedHalloween.Api;

public class GameBroadcaster
{
    private readonly IHubContext<GameHub> _hub;
    public GameBroadcaster(IHubContext<GameHub> hub) => _hub = hub;

    public Task BroadcastState(GameState state) =>
        _hub.Clients.Group(state.GameId).SendAsync("GameStateUpdated", state);

    public Task BroadcastLog(string gameId, List<string> log) =>
        log.Count > 0
            ? _hub.Clients.Group(gameId).SendAsync("GameLog", log)
            : Task.CompletedTask;
}