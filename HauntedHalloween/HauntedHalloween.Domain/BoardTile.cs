namespace HauntedHalloween.Domain;

public class BoardTile
{
    public string Id { get; set; } = default!;      
    public TileType Type { get; set; }
    public List<string> ConnectedTileIds { get; set; } = new();

    public int? HouseNumber { get; set; }

    public bool IsGhostPortal { get; set; }

    public bool IsHauntedHouse => HouseNumber == 10;
    public bool IsGhostSafe { get; set; }

}