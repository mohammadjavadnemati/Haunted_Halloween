using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public enum SpinnerResult
{
    OutOfCandy,
    TwoCandy,
    SpinAgain,
    NiceCostume,       // 3 candy
    NoOneHome,
    Take2Candy,
    Take1CandyRollAgain
}

public static class SpinnerService
{
    // section 15: 16 equal sections
    private static readonly SpinnerResult[] Sections =
    {
        SpinnerResult.OutOfCandy,
        SpinnerResult.TwoCandy, SpinnerResult.TwoCandy, SpinnerResult.TwoCandy,
        SpinnerResult.SpinAgain,
        SpinnerResult.NiceCostume, SpinnerResult.NiceCostume, SpinnerResult.NiceCostume,
        SpinnerResult.NoOneHome,
        SpinnerResult.Take2Candy, SpinnerResult.Take2Candy, SpinnerResult.Take2Candy,
        SpinnerResult.SpinAgain,
        SpinnerResult.Take1CandyRollAgain, SpinnerResult.Take1CandyRollAgain, SpinnerResult.Take1CandyRollAgain
    };

    public static SpinnerResult Spin(Random rng) => Sections[rng.Next(Sections.Length)];
}