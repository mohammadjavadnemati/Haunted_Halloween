using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class HauntedHouseResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Log { get; set; } = new();
    public HauntedHouseFace? Face { get; set; }
    public bool NeedsPortalChoiceByRightPlayer { get; set; }
    public bool NeedsRollAgain { get; set; }
    public bool NeedsBackwardMoveChoice { get; set; }
    public BoostType? BoostGranted { get; set; }
}

public static class HauntedHouseService
{
    // section 35-36: step 1 always grant a Glow Stick, step 2 roll die
    public static HauntedHouseResult Visit(GameState state, string playerId, Random rng)
    {
        var result = new HauntedHouseResult();
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        var house = state.Houses.GetValueOrDefault(10);

        if (player == null) { result.Error = "Player not found."; return result; }
        if (house == null || !house.IsHauntedHouse) { result.Error = "Haunted House not configured."; return result; }

        // section 4: house-visit-once rule applies generally; Haunted House not explicitly exempted, so we keep it consistent
        if (player.VisitedHouseNumbers.Contains(10))
        {
            result.Error = "Haunted House already visited. Use BOOst #6 to revisit.";
            return result;
        }
        player.VisitedHouseNumbers.Add(10);
        if (player.PlayerTokensRemaining > 0) player.PlayerTokensRemaining--;

        player.GlowSticks++;
        result.Log.Add("Received a Glow Stick.");

        // dropped candy from a previous Face-2 visit can be collected here
        if (house.CandyDroppedHere > 0)
        {
            var types = Enum.GetValues<CandyType>();
            player.Candy[types[rng.Next(types.Length)]] += house.CandyDroppedHere;
            result.Log.Add($"Collected {house.CandyDroppedHere} dropped candy.");
            house.CandyDroppedHere = 0;
        }

        var face = HauntedHouseDie.Roll(rng);
        result.Face = face;
        ApplyFace(state, player, house, face, rng, result);

        result.Success = true;
        return result;
    }

    private static void ApplyFace(GameState state, Player player, House house, HauntedHouseFace face, Random rng, HauntedHouseResult result)
    {
        switch (face)
        {
            case HauntedHouseFace.Teleport:
                // section 35: player to the right of active player chooses the portal — resolved by caller/UI
                result.NeedsPortalChoiceByRightPlayer = true;
                result.Log.Add("Teleport! Player to the right chooses a Ghost Portal.");
                break;

            case HauntedHouseFace.Booo:
                if (player.GlowSticks > 0) player.GlowSticks--;
                house.CandyDroppedHere += 1;
                result.NeedsBackwardMoveChoice = true; // section 35: move 2 spaces backward — exact destination chosen by client within the movement graph
                result.Log.Add("BOOO! Dropped 1 candy, lost Glow Stick, must move 2 backward.");
                break;

            case HauntedHouseFace.OneCandyRollAgain:
                GiveCandy(player, 1, rng);
                result.NeedsRollAgain = true;
                result.Log.Add("Received 1 candy, roll movement again.");
                break;

            case HauntedHouseFace.Boost:
                if (state.HauntedHouseBoostPool.Count > 0)
                {
                    var idx = rng.Next(state.HauntedHouseBoostPool.Count);
                    var boost = state.HauntedHouseBoostPool[idx];
                    state.HauntedHouseBoostPool.RemoveAt(idx);
                    player.Boosts.Add(boost);
                    player.HasHauntedHouseBoost = true;
                    result.BoostGranted = boost;
                    result.Log.Add($"Received BOOst {boost} from the Haunted House.");
                }
                else
                {
                    result.Log.Add("Haunted House BOOst pool is empty.");
                }
                break;

            case HauntedHouseFace.ThreeCandy:
                GiveCandy(player, 3, rng);
                result.Log.Add("Received 3 candy.");
                break;

            case HauntedHouseFace.TwoCandy:
                GiveCandy(player, 2, rng);
                result.Log.Add("Received 2 candy.");
                break;
        }
    }

    private static void GiveCandy(Player player, int count, Random rng)
    {
        var types = Enum.GetValues<CandyType>();
        for (int i = 0; i < count; i++)
            player.Candy[types[rng.Next(types.Length)]]++;
    }
}