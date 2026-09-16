using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class ScoreBreakdown
{
    public string PlayerId { get; set; } = default!;
    public int CandyPoints { get; set; }
    public int SecretQuestBonusPoints { get; set; }
    public int HauntedHouseBonusPoints { get; set; }
    public int HouseTokenPoints { get; set; }
    public int GlowStickPoints { get; set; }
    public int Total { get; set; }
}

public static class ScoringService
{
    public static ScoreBreakdown CalculateScore(GameState state, Player player)
    {
        var breakdown = new ScoreBreakdown { PlayerId = player.Id };

        breakdown.CandyPoints = player.Candy.Values.Sum(); // 1 point per candy, هر نوع

        int questCount = player.Candy.GetValueOrDefault(player.SecretCandyQuest);
        breakdown.SecretQuestBonusPoints = questCount / 2; // +1 امتیاز اضافه به ازای هر 2 کندی کوئست

        int hhCount = player.Candy.GetValueOrDefault(state.HauntedHouseSecretCandy);
        breakdown.HauntedHouseBonusPoints = hhCount / 2; // +1 امتیاز اضافه به ازای هر 2 کندی هانتد هوس کوئست

        breakdown.HouseTokenPoints = player.VisitedHouseNumbers.Count; // 1 توکن = 1 امتیاز، حداکثر یک توکن هر خونه

        breakdown.GlowStickPoints = player.GlowSticks; // گلو استیک استفاده‌نشده

        breakdown.Total =
            breakdown.CandyPoints +
            breakdown.SecretQuestBonusPoints +
            breakdown.HauntedHouseBonusPoints +
            breakdown.HouseTokenPoints +
            breakdown.GlowStickPoints;

        player.Score = breakdown.Total;
        return breakdown;
    }

    public static List<ScoreBreakdown> CalculateAllScores(GameState state)
        => state.Players.Select(p => CalculateScore(state, p)).ToList();

    public static Player DetermineWinner(GameState state)
    {
        var scores = CalculateAllScores(state);
        var maxScore = scores.Max(s => s.Total);
        var topPlayers = state.Players.Where(p => p.Score == maxScore).ToList();

        if (topPlayers.Count == 1)
            return topPlayers[0];

        var bedtimePlayer = topPlayers.FirstOrDefault(p => p.Id == state.BedtimeTriggeredByPlayerId);
        return bedtimePlayer ?? topPlayers[0];
    }
}