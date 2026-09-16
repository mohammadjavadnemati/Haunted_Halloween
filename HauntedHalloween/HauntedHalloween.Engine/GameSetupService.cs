using HauntedHalloween.Domain;

namespace HauntedHalloween.Engine;

public static class GameSetupService
{
    public static GameState CreateNewGame(string gameId, List<(string Id, string Name)> playerInfos, Random? rng = null)
    {
        if (playerInfos.Count < 2 || playerInfos.Count > 5)
            throw new ArgumentException("Player count must be 2-5.");

        rng ??= new Random();

        var state = new GameState
        {
            GameId = gameId,
            Tiles = BoardGraphBuilder.Build(),
            Phase = GamePhase.PlayerPhase
        };

        SetupHouses(state, rng);
        SetupPlayers(state, playerInfos, rng);
        SetupGhosts(state);
        SetupBanshee(state);
        SetupCandyCoffin(state);

        state.CurrentPlayerIndex = rng.Next(state.Players.Count); // "randomly select first player"
        state.TurnNumber = 1;

        return state;
    }

    // ---- Rule 2.11-2.13: BOOst distribution ----
    private static void SetupHouses(GameState state, Random rng)
    {
        var allBoosts = Enum.GetValues<BoostType>().OrderBy(_ => rng.Next()).ToList(); // shuffle 12 types

        // خانه‌هایی که Boost نمی‌گیرن: 1, 2, 10 (طبق قانون)
        var normalHouseNumbers = Enumerable.Range(1, 12).Where(n => n != 1 && n != 2 && n != 10).ToList();

        int boostIndex = 0;
        foreach (var num in Enumerable.Range(1, 12))
        {
            var house = new House { Number = num, IsHauntedHouse = num == 10 };

            if (normalHouseNumbers.Contains(num))
            {
                house.HiddenBoost = allBoosts[boostIndex++];
            }
            state.Houses[num] = house;
        }

        // ۳ BOOst باقیمانده (متعلق به خونه 1، 2، 10) میره داخل Haunted House
        state.HauntedHouseBoostPool = allBoosts.Skip(boostIndex).ToList();
    }

    // ---- Rule 2.9-2.10 + 19-20: Secret Candy Quest ----
    private static void SetupPlayers(GameState state, List<(string Id, string Name)> playerInfos, Random rng)
    {
        var candyTypes = Enum.GetValues<CandyType>().OrderBy(_ => rng.Next()).ToList(); // 9 types shuffled
        var startTileId = state.Tiles.Values.First(t => t.Type == TileType.Start).Id; // TODO: چند START وجود داره (start-A, start-B)؛ باید مشخص کنی هر بازیکن روی کدوم START می‌ایسته

        for (int i = 0; i < playerInfos.Count; i++)
        {
            var player = new Player
            {
                Id = playerInfos[i].Id,
                Name = playerInfos[i].Name,
                CurrentTileId = startTileId,
                StartTileId = startTileId,
                SecretCandyQuest = candyTypes[i]
            };
            foreach (var c in Enum.GetValues<CandyType>())
                player.Candy[c] = 0;

            state.Players.Add(player);
        }

        // یک توکن اضافه مخفی در Haunted House (بخش 20)
        state.HauntedHouseSecretCandy = candyTypes[playerInfos.Count];
    }

    // ---- Rule 2.6-2.8: Ghosts inactive at start ----
    private static void SetupGhosts(GameState state)
    {
        state.Ghosts.Add(new GhostState { Id = GhostId.Ghost1, IsActive = false, CurrentTileId = null }); // TODO فاز 7: Ghost Start tile هنوز در گراف بورد وجود نداره
        state.Ghosts.Add(new GhostState { Id = GhostId.Ghost2, IsActive = false, CurrentTileId = null });
        state.Ghosts.Add(new GhostState { Id = GhostId.Ghost3, IsActive = false, CurrentTileId = null });
    }

    private static void SetupBanshee(GameState state)
    {
        state.Banshee = new BansheeState { IsReleased = false, TrackPosition = 1, CurrentTileId = null };
    }

    private static void SetupCandyCoffin(GameState state)
    {
        foreach (var c in Enum.GetValues<CandyType>())
            state.CandyCoffin[c] = 0;
    }
}