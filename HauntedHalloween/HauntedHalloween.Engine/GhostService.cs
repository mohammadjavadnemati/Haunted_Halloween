using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class GhostMoveResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Log { get; set; } = new();
    public bool NeedsPortalChoice { get; set; }
    public bool NeedsExtraRoll { get; set; }
    public string? MovedGhostTileId { get; set; }
}

public static class GhostService
{
    // section 6-8: activate Ghost1 the moment any player enters the Graveyard
    public static void CheckActivateGhost1(GameState state, string playerLandedTileId)
    {
        var ghost1 = state.Ghosts.First(g => g.Id == GhostId.Ghost1);
        if (ghost1.IsActive) return;

        if (!state.Tiles.TryGetValue(playerLandedTileId, out var tile)) return;
        if (tile.Type != TileType.Cemetery) return;

        ghost1.IsActive = true;
        ghost1.CurrentTileId = "grave-safe-S"; // section 10: "directly below the central intersection"
    }

    // section 7-9: move chosen ghost/banshee by ghost die distance along graph
    public static GhostMoveResult MoveGhost(GameState state, GhostId ghostId, string destinationTileId, GhostDieFace face, Random rng)
    {
        var result = new GhostMoveResult();
        int distance = GhostDie.ToDistance(face);

        string? currentTileId = GetCurrentTileId(state, ghostId);
        if (currentTileId == null) { result.Error = "Ghost/Banshee not active."; return result; }

        var reachable = MovementService.GetReachableTiles(state, currentTileId, distance);
        if (destinationTileId != currentTileId && !reachable.Any(r => r.TileId == destinationTileId))
        {
            result.Error = "Destination not reachable for this ghost.";
            return result;
        }

        SetCurrentTileId(state, ghostId, destinationTileId);
        result.MovedGhostTileId = destinationTileId;
        result.Log.Add($"{ghostId} moved to {destinationTileId}.");

        if (state.Tiles.TryGetValue(destinationTileId, out var destTile) && destTile.IsGhostPortal)
        {
            // section 9: ghost's controller picks another portal, teleports immediately
            result.NeedsPortalChoice = true;
        }

        // section 8: Ghost Extra Roll space — not represented yet in board graph (missing rule/tile)
        // TODO: قانون نمی‌گه این تایل کجاست؛ نیاز به مشخص شدن موقعیت "Ghost Extra Roll" روی بورد

        ResolveAttack(state, ghostId, destinationTileId, result, rng);

        result.Success = true;
        return result;
    }

    public static void TeleportGhostToPortal(GameState state, GhostId ghostId, string chosenPortalTileId)
    {
        if (!state.Tiles.TryGetValue(chosenPortalTileId, out var tile) || !tile.IsGhostPortal)
            throw new ArgumentException("Not a valid portal.");
        SetCurrentTileId(state, ghostId, chosenPortalTileId);
    }

    // section 11-13: candy theft, safe spaces, coffin deposit, ghost returns to Ghost Start
    private static void ResolveAttack(GameState state, GhostId ghostId, string tileId, GhostMoveResult result, Random rng)
    {
        var player = state.Players.FirstOrDefault(p => p.CurrentTileId == tileId);
        if (player == null) return;

        if (state.Tiles.TryGetValue(tileId, out var tile) && tile.IsGhostSafe)
        {
            result.Log.Add($"{player.Name} is on a safe space, no theft.");
            return;
        }

        if (ghostId == GhostId.Banshee)
        {
            StealCandy(state, player, stealHalf: true, result);
            state.Banshee.CurrentTileId = "banshee-start"; // section 34/14: returns to its starting space (banshee "no coffin return" rule not specified beyond attack)
        }
        else if (ghostId == GhostId.Ghost2)
        {
            StealFixedAmount(state, player, 2, result);
            SetCurrentTileId(state, ghostId, "ghost-start");
        }
        else if (ghostId == GhostId.Ghost3)
        {
            StealFixedAmount(state, player, 3, result);
            SetCurrentTileId(state, ghostId, "ghost-start");
        }
        else if (ghostId == GhostId.Ghost1)
        {
            // section 6-13 دقیقاً مقدار سرقت Ghost1 رو مشخص نکرده — فقط Ghost2/Ghost3/Banshee مقدار دارن.
            // طبق دستورالعمل پروژه: قانون ناقص را حدس نمی‌زنیم.
            result.Log.Add("Ghost1 attack amount not specified in rules — no theft applied.");
        }
    }

    private static void StealFixedAmount(GameState state, Player player, int amount, GhostMoveResult result)
    {
        int remaining = amount;
        var types = player.Candy.Keys.ToList();
        foreach (var t in types)
        {
            while (remaining > 0 && player.Candy[t] > 0)
            {
                player.Candy[t]--;
                state.CandyCoffin[t]++;
                remaining--;
            }
            if (remaining == 0) break;
        }
        result.Log.Add($"{player.Name} lost {amount - remaining} candy to the coffin.");
    }

    private static void StealCandy(GameState state, Player player, bool stealHalf, GhostMoveResult result)
    {
        int total = player.Candy.Values.Sum();
        int toSteal = stealHalf ? total / 2 : total;
        StealFixedAmount(state, player, toSteal, result);
    }

    private static string? GetCurrentTileId(GameState state, GhostId id) =>
        id == GhostId.Banshee ? state.Banshee.CurrentTileId : state.Ghosts.First(g => g.Id == id).CurrentTileId;

    private static void SetCurrentTileId(GameState state, GhostId id, string tileId)
    {
        if (id == GhostId.Banshee) state.Banshee.CurrentTileId = tileId;
        else state.Ghosts.First(g => g.Id == id).CurrentTileId = tileId;
    }
}