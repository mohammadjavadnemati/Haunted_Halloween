namespace HauntedHalloween.Domain;

public class House
{
    public int Number { get; set; }            // 1..10 (10 = Haunted House)
    public bool IsHauntedHouse { get; set; }
    public BoostType? HiddenBoost { get; set; } // BOOst زیرش تا کشف نشه معلوم نیست
    public bool BoostCollected { get; set; }
    public HouseSignType Sign { get; set; } = HouseSignType.None;
    public int CandyDroppedHere { get; set; }   // برای Haunted House Face 2 (BOOO!)
}