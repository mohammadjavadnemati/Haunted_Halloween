namespace HauntedHalloween.Engine;

public enum GhostDieFace { Move1, GhostPlus1, GhostPlus2, GhostPlus3 }

public static class GhostDie
{
    // section 7: 2x Ghost+2, 2x Ghost+3, 1x "1", 1x Ghost+1
    private static readonly GhostDieFace[] Faces =
    {
        GhostDieFace.GhostPlus2, GhostDieFace.GhostPlus2,
        GhostDieFace.GhostPlus3, GhostDieFace.GhostPlus3,
        GhostDieFace.Move1,
        GhostDieFace.GhostPlus1
    };

    public static GhostDieFace Roll(Random rng) => Faces[rng.Next(Faces.Length)];

    public static int ToDistance(GhostDieFace face) => face switch
    {
        GhostDieFace.Move1 => 1,
        GhostDieFace.GhostPlus1 => 1,
        GhostDieFace.GhostPlus2 => 2,
        GhostDieFace.GhostPlus3 => 3,
        _ => 0
    };
}