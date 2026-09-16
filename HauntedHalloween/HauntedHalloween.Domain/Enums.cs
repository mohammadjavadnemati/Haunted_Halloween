namespace HauntedHalloween.Domain;

public enum TileType
{
    Path,
    Start,
    Orange,
    Green,
    Purple,
    Ghost,           // tile--ghost
    Cemetery,
    GhostStart,
    CandyCoffin,
    GhostPortal,
    BansheeTrack,
    HauntedHouse,
    House            // normal trick-or-treat house
}

public enum BoostType
{
    AnyGhostPortal = 1,      // #1
    ExtraGlowStick = 2,      // #2
    DoubleCandy = 3,         // #3
    RideHome = 4,            // #4
    Banish = 5,              // #5
    RevisitHouse = 6,        // #6
    SwitchWitch = 7,         // #7
    IgnoreSigns = 8,         // #8
    RaidCoffin = 9,          // #9
    FriendlyGhost = 10,      // #10
    BooBeGone = 11,          // #11
    Web = 12                 // #12
}

public enum HouseSignType { None, NoOneHome, OutOfCandy }

public enum GhostId { Ghost1, Ghost2, Ghost3, Banshee }

public enum GamePhase { GhostPhase, PlayerPhase, FinalRound, Ended }