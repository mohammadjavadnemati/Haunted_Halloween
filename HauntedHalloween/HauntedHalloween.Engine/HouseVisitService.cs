using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public class HouseVisitResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Log { get; set; } = new();
    public bool NeedsRollAgain { get; set; }        // Take1CandyRollAgain / Nice-house-rollagain chains
    public bool NeedsSpinAgain { get; set; }         // SpinAgain result (before Banshee-track advance handled by caller)
    public bool BlockedBySign { get; set; }
    public HouseSignType? SignHit { get; set; }
}

public static class HouseVisitService
{
    // section 4 + 17 + 40 Phase2 step6
    public static HouseVisitResult VisitHouse(GameState state, string playerId, int houseNumber, Random rng, bool ignoreSigns = false)
    {
        var result = new HouseVisitResult();
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null) { result.Error = "Player not found."; return result; }

        if (!state.Houses.TryGetValue(houseNumber, out var house))
        {
            result.Error = "House not found."; return result;
        }

        if (house.IsHauntedHouse)
        {
            result.Error = "Use the Haunted House endpoint for house 10.";
            return result;
        }

        // section 4: normally visit-once, exception is BOOst #6 (handled separately by BoostService.RevisitHouse)
        if (player.VisitedHouseNumbers.Contains(houseNumber))
        {
            result.Error = "House already visited. Use BOOst #6 to revisit.";
            return result;
        }

        // section 17: signs block visit unless ignored via BOOst #8
        if (!ignoreSigns && house.Sign != HouseSignType.None)
        {
            result.Success = true;
            result.BlockedBySign = true;
            result.SignHit = house.Sign;
            result.Log.Add($"House {houseNumber} has sign {house.Sign}. Turn wasted, no candy.");
            MarkVisited(player, houseNumber);
            return result;
        }

        MarkVisited(player, houseNumber);

        ResolveSpin(state, player, house, rng, result);

        result.Success = true;
        return result;
    }

    private static void MarkVisited(Player player, int houseNumber)
    {
        player.VisitedHouseNumbers.Add(houseNumber);
        if (player.PlayerTokensRemaining > 0) player.PlayerTokensRemaining--;
        // section 4: token placed on house = "visited" marker; token count is tracked, actual placement is a UI concern
    }

    // section 15-17 + 40 Phase2 step6: resolve spinner, chain SpinAgain internally
    private static void ResolveSpin(GameState state, Player player, House house, Random rng, HouseVisitResult result)
    {
        var spin = SpinnerService.Spin(rng);
        ApplySpinOutcome(state, player, house, spin, rng, result);

        while (spin == SpinnerResult.SpinAgain)
        {
            // section 14: SPIN AGAIN advances Banshee track if not yet released
            if (!state.Banshee.IsReleased)
            {
                AdvanceBansheeTrack(state, result);
            }
            spin = SpinnerService.Spin(rng);
            ApplySpinOutcome(state, player, house, spin, rng, result);
        }
    }

    private static void ApplySpinOutcome(GameState state, Player player, House house, SpinnerResult spin, Random rng, HouseVisitResult result)
    {
        switch (spin)
        {
            case SpinnerResult.OutOfCandy:
                MoveSign(state, house, HouseSignType.OutOfCandy);
                result.Log.Add($"Out Of Candy at house {house.Number}.");
                break;

            case SpinnerResult.TwoCandy:
                GiveCandy(player, 2, rng);
                result.Log.Add("Received 2 candy.");
                break;

            case SpinnerResult.NiceCostume:
                GiveCandy(player, 3, rng);
                result.Log.Add("Nice Costume! Received 3 candy.");
                break;

            case SpinnerResult.NoOneHome:
                MoveSign(state, house, HouseSignType.NoOneHome);
                result.Log.Add($"No One Home at house {house.Number}.");
                break;

            case SpinnerResult.Take2Candy:
                GiveCandy(player, 2, rng);
                result.Log.Add("Take 2 Candy.");
                break;

            case SpinnerResult.Take1CandyRollAgain:
                GiveCandy(player, 1, rng);
                result.NeedsRollAgain = true;
                result.Log.Add("Take 1 Candy and Roll Again.");
                break;

            case SpinnerResult.SpinAgain:
                // handled by caller loop
                break;
        }
    }

    private static void MoveSign(GameState state, House house, HouseSignType sign)
    {
        // section 17: only one house can carry each sign at a time — clear from any other house first
        foreach (var h in state.Houses.Values)
            if (h.Sign == sign) h.Sign = HouseSignType.None;

        house.Sign = sign;
    }

    // section 18-19: 9 candy types, random type per candy award, apply Secret Quest doubling at scoring time (not here)
    private static void GiveCandy(Player player, int count, Random rng)
    {
        var types = Enum.GetValues<CandyType>();
        for (int i = 0; i < count; i++)
        {
            var t = types[rng.Next(types.Length)];
            player.Candy[t]++;
        }
    }

    private static void AdvanceBansheeTrack(GameState state, HouseVisitResult result)
    {
        state.Banshee.TrackPosition++;
        result.Log.Add($"Banshee track advanced to {state.Banshee.TrackPosition}.");

        if (state.Banshee.TrackPosition == 4)
        {
            var g2 = state.Ghosts.First(g => g.Id == GhostId.Ghost2);
            g2.IsActive = true;
            // TODO fase7/8: exact tile id for "above central intersection" not yet in board graph
            result.Log.Add("Ghost 2 released.");
        }
        else if (state.Banshee.TrackPosition == 5)
        {
            var g3 = state.Ghosts.First(g => g.Id == GhostId.Ghost3);
            g3.IsActive = true;
            // TODO fase7/8: exact tile id for "right of central intersection" not yet in board graph
            result.Log.Add("Ghost 3 released.");
        }
        else if (state.Banshee.TrackPosition == 6)
        {
            state.Banshee.IsReleased = true;
            state.Banshee.CurrentTileId = "banshee-start"; // section 14
            result.Log.Add("Banshee released.");
        }
    }
}