namespace HauntedHalloween.Domain;

public class GameState
{
    public string GameId { get; set; } = default!;
    public List<Player> Players { get; set; } = new();
    public int CurrentPlayerIndex { get; set; }
    public GamePhase Phase { get; set; }

    public Dictionary<string, BoardTile> Tiles { get; set; } = new();
    public Dictionary<int, House> Houses { get; set; } = new();

    public List<GhostState> Ghosts { get; set; } = new();
    public BansheeState Banshee { get; set; } = new();

    public Dictionary<CandyType, int> CandyCoffin { get; set; } = new();
    public List<BoostType> HauntedHouseBoostPool { get; set; } = new(); // شروع: ۳ تا (متعلق به خونه ۱،۲،۱۰)

    public CandyType HauntedHouseSecretCandy { get; set; } // مخفی تا پایان بازی

    public bool BedtimeTriggered { get; set; }
    public string? BedtimeTriggeredByPlayerId { get; set; }

    public int TurnNumber { get; set; }
}