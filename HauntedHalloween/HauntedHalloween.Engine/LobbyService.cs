using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class LobbyResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LobbyPlayer? Player { get; set; }
}

public static class LobbyService
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 5;      // same limit as GameSetupService
    public const int CharacterCount = 5;

    private static readonly List<string> StartTiles = BoardGraphBuilder.Build().Values
        .Where(t => t.Type == TileType.Start).Select(t => t.Id).OrderBy(id => id).ToList();

    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I

    public static string NewCode(Random rng) =>
        new(Enumerable.Range(0, 5).Select(_ => CodeAlphabet[rng.Next(CodeAlphabet.Length)]).ToArray());

    private static LobbyResult Fail(string error) => new() { Error = error };
    private static LobbyResult Ok(LobbyPlayer player) => new() { Success = true, Player = player };

    private static LobbyResult Find(Lobby lobby, string playerId, bool mustBeUnready)
    {
        if (lobby.Started) return Fail("Game already started.");
        var player = lobby.Players.FirstOrDefault(x => x.Id == playerId);
        if (player == null) return Fail("Player not in this room.");
        if (mustBeUnready && player.IsReady) return Fail("Un-ready first.");
        return Ok(player);
    }

    public static LobbyResult Join(Lobby lobby, string? name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0 || name.Length > 20) return Fail("Name must be 1-20 characters.");
        if (lobby.Started) return Fail("Game already started.");
        if (lobby.Players.Count >= MaxPlayers) return Fail("Room is full.");

        var taken = lobby.Players.Select(x => x.Character).ToHashSet();
        var player = new LobbyPlayer
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Character = Enumerable.Range(1, CharacterCount).First(c => !taken.Contains(c)),
            StartTileId = StartTiles[0]
        };
        lobby.Players.Add(player);
        return Ok(player);
    }

    public static LobbyResult SetCharacter(Lobby lobby, string playerId, int character)
    {
        var r = Find(lobby, playerId, true);
        if (!r.Success) return r;
        if (character < 1 || character > CharacterCount) return Fail("Invalid character.");
        if (lobby.Players.Any(x => x.Id != playerId && x.Character == character)) return Fail("Character already taken.");
        r.Player!.Character = character;
        return r;
    }

    public static LobbyResult SetStartTile(Lobby lobby, string playerId, string startTileId)
    {
        var r = Find(lobby, playerId, true);
        if (!r.Success) return r;
        if (!StartTiles.Contains(startTileId)) return Fail("Invalid START tile.");
        r.Player!.StartTileId = startTileId;
        return r;
    }

    public static LobbyResult SetReady(Lobby lobby, string playerId, bool ready)
    {
        var r = Find(lobby, playerId, false);
        if (!r.Success) return r;
        r.Player!.IsReady = ready;
        return r;
    }

    public static LobbyResult Leave(Lobby lobby, string playerId)
    {
        var r = Find(lobby, playerId, false);
        if (!r.Success) return r;
        lobby.Players.Remove(r.Player!);
        return r;
    }

    public static bool ShouldStart(Lobby lobby) =>
        !lobby.Started && lobby.Players.Count >= MinPlayers && lobby.Players.All(x => x.IsReady);

    public static GameState StartGame(Lobby lobby, Random? rng = null)
    {
        var state = GameSetupService.CreateNewGame(
            lobby.Code,   // room code doubles as gameId -> all /api/Game/{gameId} routes keep working
            lobby.Players.Select(x => (x.Id, x.Name, x.StartTileId)).ToList(),
            rng);
        foreach (var lp in lobby.Players)
            state.Players.First(x => x.Id == lp.Id).CharacterIndex = lp.Character;
        lobby.Started = true;
        return state;
    }
}