using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class TurnResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Log { get; set; } = new();
}

public static class TurnService
{
    public static Player CurrentPlayer(GameState state) => state.Players[state.CurrentPlayerIndex];

    // بخش 40 - شروع نوبت: ابتدا فاز Ghost (اگه ghost فعالی هست)، بعد فاز Player
    public static TurnResult BeginTurn(GameState state)
    {
        var result = new TurnResult { Success = true };

        if (state.Phase == GamePhase.Ended)
        {
            result.Success = false;
            result.Error = "Game has ended.";
            return result;
        }

        bool anyGhostActive = state.Ghosts.Any(g => g.IsActive) || state.Banshee.IsReleased;

        if (anyGhostActive)
        {
            state.Phase = GamePhase.GhostPhase;
            result.Log.Add("Ghost phase pending."); // فاز 7: اینجا Ghost Die رول میشه
            // TODO فاز 7: پیاده‌سازی رول Ghost Die و حرکت Ghost/Banshee
        }
        else
        {
            state.Phase = GamePhase.PlayerPhase;
            result.Log.Add($"{CurrentPlayer(state).Name}'s player phase begins.");
        }

        return result;
    }

    // فراخوانی بعد از این‌که فاز Player کامل شد (حرکت، خانه، BOOst و ...)
    public static TurnResult EndTurn(GameState state)
    {
        var result = new TurnResult { Success = true };

        if (state.Phase == GamePhase.FinalRound)
        {
            var current = CurrentPlayer(state);
            current.HasTakenFinalTurn = true;

            if (state.Players.All(p => p.HasReachedBedtime || p.HasTakenFinalTurn))
            {
                state.Phase = GamePhase.Ended;
                result.Log.Add("Final round complete. Game ended.");
                return result;
            }
        }

        AdvanceToNextPlayer(state);
        state.TurnNumber++;
        result.Log.Add($"Turn passed to {CurrentPlayer(state).Name}.");
        return result;
    }

    // بخش 38 - وقتی یک بازیکن BEDTIME! رو فعال می‌کنه
    public static TurnResult TriggerBedtime(GameState state, string playerId)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        var result = new TurnResult();

        if (player == null)
        {
            result.Success = false;
            result.Error = "Player not found.";
            return result;
        }

        if (player.VisitedHouseNumbers.Count < 6)
        {
            result.Success = false;
            result.Error = "Player must visit at least 6 houses before triggering BEDTIME.";
            return result;
        }

        if (player.CurrentTileId != player.StartTileId)
        {
            result.Success = false;
            result.Error = "Player must be on their original START tile to trigger BEDTIME.";
            return result;
        }

        player.HasReachedBedtime = true;
        state.BedtimeTriggered = true;
        state.BedtimeTriggeredByPlayerId = playerId;
        state.Phase = GamePhase.FinalRound;

        result.Success = true;
        result.Log.Add($"{player.Name} triggered BEDTIME! Final round begins.");
        return result;
    }

    private static void AdvanceToNextPlayer(GameState state)
    {
        int count = state.Players.Count;
        int next = state.CurrentPlayerIndex;
        do
        {
            next = (next + 1) % count;
            var candidate = state.Players[next];

            if (candidate.HasReachedBedtime && state.Phase != GamePhase.FinalRound)
                continue;

            if (candidate.SkipNextTurn)
            {
                candidate.SkipNextTurn = false; // section 33: skip exactly one turn
                continue;
            }

            break;
        } while (true);

        state.CurrentPlayerIndex = next;
    }
}