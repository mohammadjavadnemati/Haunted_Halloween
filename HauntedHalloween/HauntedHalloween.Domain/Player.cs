namespace HauntedHalloween.Domain;

public class Player
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CurrentTileId { get; set; } = default!;
    public string StartTileId { get; set; } = default!;

    public int PlayerTokensRemaining { get; set; } = 12;
    public HashSet<int> VisitedHouseNumbers { get; set; } = new();

    public Dictionary<CandyType, int> Candy { get; set; } = new();
    public CandyType SecretCandyQuest { get; set; }

    public List<BoostType> Boosts { get; set; } = new();
    public int NormalHouseBoostsUsed { get; set; }   // حداکثر ۲
    public bool HasHauntedHouseBoost { get; set; }   // اجازه رسیدن به ۳

    public int GlowSticks { get; set; }
    public bool SkipNextTurn { get; set; }           // اثر BOOst #12
    public bool HasReachedBedtime { get; set; }
    public bool HasTakenFinalTurn { get; set; }
    public int Score { get; set; }
    public List<string> LastMovementPath { get; set; } = new();
}

public enum CandyType
{
    Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Type9
}