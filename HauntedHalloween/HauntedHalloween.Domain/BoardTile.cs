namespace HauntedHalloween.Domain;

// یک نود از گراف بورد. مسیرها با یال بین Id ها مشخص می‌شن (فاز ۴ تکمیل میشه).
public class BoardTile
{
    public string Id { get; set; } = default!;      // مثلا "path-15-3" یا "house-1"
    public TileType Type { get; set; }
    public List<string> ConnectedTileIds { get; set; } = new();

    // فقط برای Type == House / HauntedHouse
    public int? HouseNumber { get; set; }

    // فقط برای Type == GhostPortal
    public bool IsGhostPortal { get; set; }

    // فقط برای Cemetery: آیا جزو ۵ خانه محافظت‌شده است
    public bool IsGhostSafe { get; set; }
}