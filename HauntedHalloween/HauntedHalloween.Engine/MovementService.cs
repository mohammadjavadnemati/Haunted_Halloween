using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public record ReachableTile(string TileId, int Distance, List<string> Path);

public class MoveResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? LandedTileId { get; set; }
    public TileType? LandedTileType { get; set; }
    public bool GrantsExtraRoll { get; set; }      // Orange (section 5)
    public bool GrantsForcedMove2 { get; set; }     // Purple (section 5)
}

public static class MovementService
{
    // BFS: تمام تایل‌های قابل دسترس در فاصله <= maxDistance از یک تایل
    public static List<ReachableTile> GetReachableTiles(GameState state, string fromTileId, int maxDistance)
    {
        var result = new List<ReachableTile>();
        var visited = new Dictionary<string, int> { [fromTileId] = 0 };
        var paths = new Dictionary<string, List<string>> { [fromTileId] = new() { fromTileId } };
        var queue = new Queue<string>();
        queue.Enqueue(fromTileId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var dist = visited[current];
            if (dist >= maxDistance) continue;

            if (!state.Tiles.TryGetValue(current, out var tile)) continue;

            foreach (var neighborId in tile.ConnectedTileIds)
            {
                if (visited.ContainsKey(neighborId)) continue;
                visited[neighborId] = dist + 1;
                paths[neighborId] = new List<string>(paths[current]) { neighborId };
                queue.Enqueue(neighborId);
            }
        }

        foreach (var kv in visited)
        {
            if (kv.Key == fromTileId) continue;
            result.Add(new ReachableTile(kv.Key, kv.Value, paths[kv.Key]));
        }
        return result;
    }

    // section 3: مقصد معتبر = هر تایلی که distance <= maxDistance داره (چه خانه باشه چه مسیر عادی)
    // section 5: purple/orange resolve میشه بعد از قرارگیری
    public static MoveResult ExecuteMove(GameState state, string playerId, string destinationTileId, int maxDistance)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        var result = new MoveResult();

        if (player == null) { result.Error = "Player not found."; return result; }
        if (!state.Tiles.TryGetValue(destinationTileId, out var destTile))
        {
            result.Error = "Invalid tile."; return result;
        }

        var reachable = GetReachableTiles(state, player.CurrentTileId, maxDistance);
        var target = reachable.FirstOrDefault(r => r.TileId == destinationTileId);
        if (target == null)
        {
            result.Error = "Destination not reachable within movement.";
            return result;
        }

        player.CurrentTileId = destinationTileId;
        player.LastMovementPath = target.Path;

        result.Success = true;
        result.LandedTileId = destinationTileId;
        result.LandedTileType = destTile.Type;
        result.GrantsExtraRoll = destTile.Type == TileType.Orange;
        result.GrantsForcedMove2 = destTile.Type == TileType.Purple;

        return result;
    }
    // اضافه به کلاس MovementService:

    public static bool PlayerLandedOnPortal(GameState state, string tileId) =>
        state.Tiles.TryGetValue(tileId, out var t) && t.IsGhostPortal;

    // section 22: BOOst #1 owner may teleport to another portal; reusable, never consumed
    public static MoveResult ResolvePortalChoice(GameState state, string playerId, bool teleport, string? chosenPortalTileId)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        var result = new MoveResult();
        if (player == null) { result.Error = "Player not found."; return result; }
        if (!player.Boosts.Contains(BoostType.AnyGhostPortal)) { result.Error = "Player does not own BOOst #1."; return result; }

        if (!teleport || chosenPortalTileId == null)
        {
            result.Success = true;
            result.LandedTileId = player.CurrentTileId;
            return result;
        }

        if (!state.Tiles.TryGetValue(chosenPortalTileId, out var tile) || !tile.IsGhostPortal)
        {
            result.Error = "Not a valid portal."; return result;
        }

        player.CurrentTileId = chosenPortalTileId;
        result.Success = true;
        result.LandedTileId = chosenPortalTileId;
        return result;
    }
}