using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class ScoreBreakdown
{
    public string PlayerId { get; set; } = default!;
    public int CandyPoints { get; set; }
    public int SecretQuestBonusPoints { get; set; }
    public int HauntedHouseBonusPoints { get; set; }
    public int GlowStickPoints { get; set; }
    public int Total { get; set; }
}

public static class ScoringService
{
    // section 39
    public static ScoreBreakdown CalculateScore(GameState state, Player player)
    {
        var breakdown = new ScoreBreakdown { PlayerId = player.Id };

        foreach (var kv in player.Candy)
        {
            var type = kv.Key;
            var count = kv.Value;

            if (type == player.SecretCandyQuest)
            {
                // section 19: 2 points instead of 1 for matching secret quest candy
                breakdown.SecretQuestBonusPoints += count * 2;
            }
            else
            {
                breakdown.CandyPoints += count;
            }

            if (type == state.HauntedHouseSecretCandy)
            {
                // section 20: +1 point per 2 matching candies, 1 leftover candy gives no bonus
                breakdown.HauntedHouseBonusPoints += count / 2;
            }
        }

        // section 39: unused Glow Sticks = +1 point each
        breakdown.GlowStickPoints = player.GlowSticks;

        breakdown.Total =
            breakdown.CandyPoints +
            breakdown.SecretQuestBonusPoints +
            breakdown.HauntedHouseBonusPoints +
            breakdown.GlowStickPoints;

        player.Score = breakdown.Total;

        return breakdown;
    }

    public static List<ScoreBreakdown> CalculateAllScores(GameState state)
        => state.Players.Select(p => CalculateScore(state, p)).ToList();

    // section 39: tie -> player who triggered BEDTIME first wins
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