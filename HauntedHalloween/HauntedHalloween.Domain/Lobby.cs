namespace HauntedHalloween.Domain;

public class LobbyPlayer
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Character { get; set; }            // 1..5
    public string StartTileId { get; set; } = default!;
    public bool IsReady { get; set; }
}

public class Lobby
{
    public string Code { get; set; } = default!;
    public List<LobbyPlayer> Players { get; set; } = new();
    public bool Started { get; set; }
}