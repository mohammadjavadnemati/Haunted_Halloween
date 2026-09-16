namespace HauntedHalloween.Domain;

public class GhostState
{
    public GhostId Id { get; set; }
    public bool IsActive { get; set; }
    public string? CurrentTileId { get; set; }
}

public class BansheeState
{
    public bool IsReleased { get; set; }
    public int TrackPosition { get; set; }   // 0..6 (0 = هنوز شروع نشده, 1..6 = طبق قانون)
    public string? CurrentTileId { get; set; }
}