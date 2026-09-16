namespace HauntedHalloween.Engine;

public enum HauntedHouseFace { Teleport, Booo, OneCandyRollAgain, Boost, ThreeCandy, TwoCandy }

public static class HauntedHouseDie
{
    // section 35: faces 1-6
    private static readonly HauntedHouseFace[] Faces =
    {
        HauntedHouseFace.Teleport,
        HauntedHouseFace.Booo,
        HauntedHouseFace.OneCandyRollAgain,
        HauntedHouseFace.Boost,
        HauntedHouseFace.ThreeCandy,
        HauntedHouseFace.TwoCandy
    };

    public static HauntedHouseFace Roll(Random rng) => Faces[rng.Next(Faces.Length)];
}