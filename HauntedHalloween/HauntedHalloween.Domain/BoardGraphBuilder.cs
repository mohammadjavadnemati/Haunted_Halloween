namespace HauntedHalloween.Domain;

// می‌سازه گراف بورد از روی مختصات grid موجود در index.html.
// دو نود با فاصله ۱ در row یا column (و برابر بودن مختصات دیگر) همسایه محسوب می‌شن.
public static class BoardGraphBuilder
{
    private record TileDef(string Id, int Col, int Row, TileType Type, int? HouseNumber = null);

    private static readonly List<TileDef> Defs = new()
    {
        // Right vertical path
        new("start-A", 15, 2, TileType.Start),
        new("path-15-3", 15, 3, TileType.Path),
        new("path-15-4", 15, 4, TileType.Path),
        new("path-15-5", 15, 5, TileType.Path),
        new("path-15-6", 15, 6, TileType.Path),
        new("path-15-7", 15, 7, TileType.Path),
        new("path-15-8", 15, 8, TileType.Path),
        new("path-15-9", 15, 9, TileType.Path),
        new("path-15-10", 15, 10, TileType.Path),
        new("path-15-11", 15, 11, TileType.Path),
        new("start-B", 15, 12, TileType.Start),

        new("house-1", 16, 5, TileType.House, 1),
        new("house-2", 16, 9, TileType.House, 2),

        // Entrance 1 + connectors to cemetery
        new("gate-10-7", 10, 7, TileType.Path),
        new("conn-11-7", 11, 7, TileType.Path),
        new("conn-12-7", 12, 7, TileType.Path),
        new("conn-13-7", 13, 7, TileType.Path),
        new("conn-14-7", 14, 7, TileType.Path),

        // Cemetery row
        new("cem-C1", 9, 7, TileType.Cemetery),
        new("cem-C2", 8, 7, TileType.Cemetery),
        new("cem-C3", 7, 7, TileType.Cemetery),
        new("cem-C4", 6, 7, TileType.Cemetery),
        new("cem-C5", 5, 7, TileType.Cemetery),
        new("cem-C6", 4, 7, TileType.Cemetery),
        new("cem-C7", 3, 7, TileType.Cemetery),

        new("gate-2-7", 2, 7, TileType.Path), // entrance 2

        // Central branch
        new("gate-6-3", 6, 3, TileType.Path),
        new("path-6-4", 6, 4, TileType.Path),
        new("path-6-5", 6, 5, TileType.Path),
        new("path-6-6", 6, 6, TileType.Path),
        new("path-6-8", 6, 8, TileType.Path),
        new("path-6-9", 6, 9, TileType.Path),
        new("path-6-10", 6, 10, TileType.Path),
        new("gate-6-11", 6, 11, TileType.Path),

        new("ghost-tile-6-12", 6, 12, TileType.Ghost),

        // Right chain from ghost tile
        new("path-7-12", 7, 12, TileType.Path),
        new("path-8-12", 8, 12, TileType.Path),
        new("orange-9-12", 9, 12, TileType.Orange),
        new("green-10-12", 10, 12, TileType.Green),
        new("path-11-12", 11, 12, TileType.Path),

        new("house-3", 8, 13, TileType.House, 3),
        new("house-4", 11, 13, TileType.House, 4),

        // Left chain from ghost tile
        new("path-5-12", 5, 12, TileType.Path),
        new("path-4-12", 4, 12, TileType.Path),
        new("purple-3-12", 3, 12, TileType.Purple),
        new("path-2-12", 2, 12, TileType.Path),

        new("house-5", 4, 13, TileType.House, 5),
        new("house-6", 1, 13, TileType.House, 6),

        // Upward chain (col 2)
        new("green-2-11", 2, 11, TileType.Green),
        new("path-2-10", 2, 10, TileType.Path),
        new("orange-2-9", 2, 9, TileType.Orange),
        new("path-2-8", 2, 8, TileType.Path),
        new("path-2-6", 2, 6, TileType.Path),
        new("path-2-5", 2, 5, TileType.Path),
        new("path-2-4", 2, 4, TileType.Path),
        new("path-2-3", 2, 3, TileType.Path),
        new("green-2-2", 2, 2, TileType.Green),

        new("house-7", 1, 10, TileType.House, 7),
        new("house-8", 1, 8, TileType.House, 8),
        new("house-9", 1, 5, TileType.House, 9),

        // Top chain
        new("path-3-2", 3, 2, TileType.Path),
        new("purple-4-2", 4, 2, TileType.Purple),
        new("path-5-2", 5, 2, TileType.Path),
        new("orange-6-2", 6, 2, TileType.Orange),
        new("green-7-2", 7, 2, TileType.Green),
        new("path-8-2", 8, 2, TileType.Path),
        new("path-9-2", 9, 2, TileType.Path),

        new("house-10", 3, 1, TileType.House, 10), // Haunted House — طبق section21 شماره 10 باید Haunted باشه
        new("house-11", 5, 1, TileType.House, 11),
        new("house-12", 9, 1, TileType.House, 12),
        new("grave-center", 6, 7, TileType.Cemetery),
new("grave-safe-N", 6, 6, TileType.Cemetery),   // = Ghost2 start
new("grave-safe-S", 6, 8, TileType.Cemetery),
new("grave-safe-E", 7, 7, TileType.Cemetery),   // = Ghost3 start
new("grave-safe-W", 5, 7, TileType.Cemetery),

new("ghost-start", 4, 9, TileType.GhostStart),
new("candy-coffin", 3, 9, TileType.CandyCoffin),
new("ghost-diag-1", 5, 8, TileType.Path),
new("ghost-diag-2", 4, 8, TileType.Path),

new("portal-1", 1, 1, TileType.GhostPortal),
new("portal-2", 16, 13, TileType.GhostPortal),

new("banshee-start", 3, 0, TileType.Path),
    };

    public static Dictionary<string, BoardTile> Build()
    {
        var tiles = Defs.ToDictionary(d => d.Id, d => new BoardTile
        {
            Id = d.Id,
            Type = d.Type,
            HouseNumber = d.HouseNumber
        });

        foreach (var a in Defs)
        {
            foreach (var b in Defs)
            {
                if (a.Id == b.Id) continue;
                bool adjacent =
                    (a.Col == b.Col && Math.Abs(a.Row - b.Row) == 1) ||
                    (a.Row == b.Row && Math.Abs(a.Col - b.Col) == 1);
                if (adjacent)
                    tiles[a.Id].ConnectedTileIds.Add(b.Id);
            }
        }
        // بعد از foreach adjacency loop در Build():
        tiles["grave-center"].ConnectedTileIds.AddRange(new[] { "grave-safe-N", "grave-safe-S", "grave-safe-E", "grave-safe-W" });
        tiles["grave-safe-N"].ConnectedTileIds.Add("grave-center");
        tiles["grave-safe-S"].ConnectedTileIds.Add("grave-center");
        tiles["grave-safe-E"].ConnectedTileIds.Add("grave-center");
        tiles["grave-safe-W"].ConnectedTileIds.Add("grave-center");

        tiles["ghost-diag-1"].ConnectedTileIds.AddRange(new[] { "ghost-start", "grave-safe-S" });
        tiles["ghost-diag-2"].ConnectedTileIds.AddRange(new[] { "ghost-diag-1", "grave-center" });
        tiles["ghost-start"].ConnectedTileIds.Add("ghost-diag-1");
        tiles["candy-coffin"].ConnectedTileIds.Add("ghost-start");

        tiles["grave-safe-N"].IsGhostSafe = true;
        tiles["grave-safe-S"].IsGhostSafe = true;
        tiles["grave-safe-E"].IsGhostSafe = true;
        tiles["grave-safe-W"].IsGhostSafe = true;
        tiles["grave-center"].IsGhostSafe = true;
        tiles["ghost-diag-1"].IsGhostSafe = true;
        tiles["ghost-diag-2"].IsGhostSafe = true;

        tiles["portal-1"].IsGhostPortal = true;
        tiles["portal-2"].IsGhostPortal = true;
        tiles["ghost-diag-1"].IsGhostExtraRoll = true; // اولین تایل خروج از در پایین قبرستان
        return tiles;
    }
}