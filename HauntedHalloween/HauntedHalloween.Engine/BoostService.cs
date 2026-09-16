using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class BoostResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Log { get; set; } = new();
}

public static class BoostService
{
    // section 21: max 2 boosts from normal houses, up to 3 total if a Haunted House boost was obtained
    public static bool CanCollectNormalHouseBoost(Player player)
    {
        int limit = player.HasHauntedHouseBoost ? 3 : 2;
        return player.NormalHouseBoostsUsed < limit;
    }

    public static BoostResult CollectNormalHouseBoost(GameState state, string playerId, int houseNumber)
    {
        var result = new BoostResult();
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        var house = state.Houses.GetValueOrDefault(houseNumber);

        if (player == null) { result.Error = "Player not found."; return result; }
        if (house == null || house.IsHauntedHouse) { result.Error = "Invalid house."; return result; }
        if (house.HiddenBoost == null || house.BoostCollected) { result.Error = "No boost available here."; return result; }
        if (!CanCollectNormalHouseBoost(player)) { result.Error = "Normal house BOOst limit reached."; return result; }

        player.Boosts.Add(house.HiddenBoost.Value);
        player.NormalHouseBoostsUsed++;
        house.BoostCollected = true;

        result.Success = true;
        result.Log.Add($"{player.Name} collected BOOst {house.HiddenBoost} at house {houseNumber}.");
        return result;
    }

    // section 22: BOOst #1 — unlimited, used at MovementService/GhostService portal-landing time (not consumed)
    // Handled inline where portals resolve; no separate consume-call needed since it's reusable.

    // section 23: BOOst #2 — Extra Glow Stick, consumed once (acts exactly like a Glow Stick, so we just grant +1 GlowStick)
    public static BoostResult UseBoost2ExtraGlowStick(GameState state, string playerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.ExtraGlowStick, result);
        if (player == null) return result;

        player.GlowSticks++;
        ConsumePermanently(player, BoostType.ExtraGlowStick);
        result.Success = true;
        result.Log.Add("BOOst #2 used: gained an extra Glow Stick charge.");
        return result;
    }

    // section 24: BOOst #3 — Double Candy, must be used immediately after receiving candy from a house
    public static BoostResult UseBoost3DoubleCandy(GameState state, string playerId, CandyType candyType, int amountReceived)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.DoubleCandy, result);
        if (player == null) return result;

        player.Candy[candyType] += amountReceived; // double = add the same amount again
        ReturnToHauntedHousePool(state, player, BoostType.DoubleCandy);
        result.Success = true;
        result.Log.Add($"BOOst #3 used: doubled {amountReceived} {candyType}.");
        return result;
    }

    // section 25: BOOst #4 — Ride Home, immediately go to START and trigger BEDTIME
    public static BoostResult UseBoost4RideHome(GameState state, string playerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.RideHome, result);
        if (player == null) return result;

        player.CurrentTileId = player.StartTileId;
        ReturnToHauntedHousePool(state, player, BoostType.RideHome);

        var bedtime = TurnService.TriggerBedtime(state, playerId);
        result.Success = bedtime.Success;
        result.Error = bedtime.Error;
        result.Log.Add("BOOst #4 used: returned home, BEDTIME triggered.");
        result.Log.AddRange(bedtime.Log);
        return result;
    }

    // section 26: BOOst #5 — Banish, requires >=2 active ghosts, cannot target Banshee
    public static BoostResult UseBoost5Banish(GameState state, string playerId, GhostId targetGhost)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.Banish, result);
        if (player == null) return result;

        if (targetGhost == GhostId.Banshee) { result.Error = "Cannot target Banshee with BOOst #5."; return result; }

        int activeCount = state.Ghosts.Count(g => g.IsActive);
        if (activeCount < 2) { result.Error = "At least two ghosts must be active to use BOOst #5."; return result; }

        var ghost = state.Ghosts.First(g => g.Id == targetGhost);
        ghost.CurrentTileId = "ghost-start";

        ReturnToHauntedHousePool(state, player, BoostType.Banish);
        result.Success = true;
        result.Log.Add($"BOOst #5 used: {targetGhost} returned to Ghost Start.");
        return result;
    }

    // section 27: BOOst #6 — Revisit 1 House
    public static BoostResult UseBoost6Revisit(GameState state, string playerId, int houseNumber)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.RevisitHouse, result);
        if (player == null) return result;

        if (!player.VisitedHouseNumbers.Contains(houseNumber))
        {
            result.Error = "Player has not visited that house before."; return result;
        }

        ReturnToHauntedHousePool(state, player, BoostType.RevisitHouse);
        result.Success = true;
        result.Log.Add($"BOOst #6 used: may revisit house {houseNumber} (no new token placed).");
        // actual re-trigger of spinner is done by caller via HouseVisitService with a "skip visited check" flag
        return result;
    }

    // section 28: BOOst #7 — Switch Witch
    public static BoostResult UseBoost7Switch(GameState state, string playerId, string targetPlayerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.SwitchWitch, result);
        if (player == null) return result;

        var target = state.Players.FirstOrDefault(p => p.Id == targetPlayerId);
        if (target == null) { result.Error = "Target player not found."; return result; }

        (player.CurrentTileId, target.CurrentTileId) = (target.CurrentTileId, player.CurrentTileId);

        ReturnToHauntedHousePool(state, player, BoostType.SwitchWitch);
        result.Success = true;
        result.Log.Add($"BOOst #7 used: swapped positions with {target.Name}.");
        return result;
    }

    // section 29: BOOst #8 — Ignore Signs (consumed only if the follow-up spin actually hits a sign and player chooses Yes)
    public static bool HasIgnoreSignsBoost(Player player) => player.Boosts.Contains(BoostType.IgnoreSigns);

    public static BoostResult ConsumeBoost8AfterSignHit(GameState state, string playerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.IgnoreSigns, result);
        if (player == null) return result;

        ConsumePermanently(player, BoostType.IgnoreSigns);
        result.Success = true;
        result.Log.Add("BOOst #8 consumed: sign result ignored, spin again.");
        return result;
    }

    // section 30: BOOst #9 — Raid Coffin
    public static BoostResult UseBoost9RaidCoffin(GameState state, string playerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.RaidCoffin, result);
        if (player == null) return result;

        player.CurrentTileId = "candy-coffin";
        foreach (var t in state.CandyCoffin.Keys.ToList())
        {
            player.Candy[t] += state.CandyCoffin[t];
            state.CandyCoffin[t] = 0;
        }

        ConsumePermanently(player, BoostType.RaidCoffin);
        result.Success = true;
        result.Log.Add("BOOst #9 used: raided the Candy Coffin.");
        return result;
    }

    // section 31: BOOst #10 — Friendly Ghost (redirect most recent normal-ghost theft to this player)
    public static BoostResult UseBoost10FriendlyGhost(GameState state, string playerId, CandyType stolenType, int stolenAmount, string victimPlayerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.FriendlyGhost, result);
        if (player == null) return result;

        // stolenAmount already sits in state.CandyCoffin from GhostService theft — move it to this player instead
        int toMove = Math.Min(stolenAmount, state.CandyCoffin.GetValueOrDefault(stolenType));
        state.CandyCoffin[stolenType] -= toMove;
        player.Candy[stolenType] += toMove;

        ConsumePermanently(player, BoostType.FriendlyGhost);
        result.Success = true;
        result.Log.Add($"BOOst #10 used: redirected {toMove} {stolenType} from the coffin.");
        return result;
    }

    // section 32: BOOst #11 — Boo-Be-Gone (only vs Banshee)
    public static BoostResult UseBoost11BooBeGone(GameState state, string playerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.BooBeGone, result);
        if (player == null) return result;

        state.Banshee.CurrentTileId = "banshee-start";
        ReturnToHauntedHousePool(state, player, BoostType.BooBeGone);
        result.Success = true;
        result.Log.Add("BOOst #11 used: Banshee scared away.");
        return result;
    }

    // section 33: BOOst #12 — Web
    public static BoostResult UseBoost12Web(GameState state, string playerId, string targetPlayerId)
    {
        var result = new BoostResult();
        var player = RequireBoost(state, playerId, BoostType.Web, result);
        if (player == null) return result;

        var target = state.Players.FirstOrDefault(p => p.Id == targetPlayerId);
        if (target == null) { result.Error = "Target player not found."; return result; }

        target.SkipNextTurn = true;
        ConsumePermanently(player, BoostType.Web);
        result.Success = true;
        result.Log.Add($"BOOst #12 used: {target.Name} will skip their next turn.");
        return result;
    }

    // ---------- helpers ----------

    private static Player? RequireBoost(GameState state, string playerId, BoostType type, BoostResult result)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) { result.Error = "Player not found."; return null; }
        if (!player.Boosts.Contains(type)) { result.Error = $"Player does not own BOOst {type}."; return null; }
        return player;
    }

    private static void ConsumePermanently(Player player, BoostType type) => player.Boosts.Remove(type);

    // section 34: returned boosts go back to the Haunted House pool, obtainable again via the Haunted House die
    private static void ReturnToHauntedHousePool(GameState state, Player player, BoostType type)
    {
        player.Boosts.Remove(type);
        state.HauntedHouseBoostPool.Add(type);
    }
}