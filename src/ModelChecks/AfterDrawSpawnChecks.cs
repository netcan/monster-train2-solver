using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AfterDrawSpawnChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        var definition = root.GetProperty("OriginalAfterDrawSpawnDefinition");
        string? owner = definition.GetProperty("Id").GetString();
        string[] expected = owner switch
        {
            "84e8fb5f-ebfa-4ebd-99d5-607d5ff45d3b" =>
            ["854f3636-2b50-45eb-80af-cef27a99d8d6", "9d4e443d-77fb-485b-b5e4-24baa2f8957f", "cb9baba5-9b89-491c-b774-6bd7b00d4b35"],
            "80ccdba4-3fa5-487a-8fdf-2dfea52f9541" => ["e8b1f88f-8628-48ea-84fd-0191ce2e88a0"],
            _ => throw new InvalidOperationException("Unknown original after-draw spawn owner.")
        };
        Require(root.GetProperty("OriginalAfterDrawSpawnPrepared").GetBoolean() &&
            definition.GetProperty("Type").GetString() == "RelicEffectSpawnMonsterOnTurnStart" &&
            !definition.GetProperty("PlacementExcluded").GetBoolean() && definition.GetProperty("Conditions").GetInt32() == 0 &&
            definition.GetProperty("Roster").EnumerateArray().Select(unit => unit.GetString()).SequenceEqual(expected) &&
            definition.ContentEquals(root.GetProperty("OriginalAfterDrawSpawnDefinitionAfter")),
            "Missing unchanged original after-draw roster.");

        var callbacks = root.GetProperty("OriginalAfterDrawSpawnCallbacks").EnumerateArray().ToArray();
        Require(callbacks.Length >= 2 && callbacks[0].GetProperty("Deployment").GetBoolean() &&
            callbacks[0].GetProperty("Turn").GetInt32() == 0 && callbacks.Skip(1).All(row => !row.GetProperty("Deployment").GetBoolean()),
            "Expected both deployment and regular after-draw callbacks.");
        int births = 0;
        bool fullFloorRetry = false, disabledFloorSkip = false;
        bool lonelyRooms = callbacks[0].GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.Relics!
            .Any(RelicModel.IsOriginalLonelyRooms);
        foreach (var callback in callbacks)
        {
            var before = callback.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = callback.GetProperty("After").Deserialize<TrainCombatState>()!;
            var roster = before.Context!.Relics!.Single(relic => relic.DataId == owner).ResourceEffects!
                .Single(effect => effect.Type == "RelicEffectSpawnMonsterOnTurnStart").OpeningSpawn!.Units;
            var beforeIds = before.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var born = after.Rooms.Where(room => room.RoomIndex is >= 0 and < 3).SelectMany(room => room.Units)
                .Where(unit => !beforeIds.Contains(unit.Id)).OrderBy(unit => unit.Id).ToArray();
            var expectedOrder = roster.Select(unit => unit.Unit.AssetKey).ToList();
            int next = 0;
            foreach (CombatUnit unit in born)
            {
                int position = expectedOrder.FindIndex(next, name => name == unit.AssetKey);
                Require(position >= 0 && unit.Team == CombatTeam.Player && unit.SpawnerCardId == 0 &&
                    unit.Statuses.Any(status => status.Id == "cardless"), "The native birth lost its roster order or a cardless flag.");
                next = position + 1;
            }
            Require(before.Context.SpawnPoints != null && after.Context!.SpawnPoints != null &&
                !before.Context.BattleRng.Equals(after.Context.BattleRng), "The native callback missed its physical slots or battle RNG samples.");
            if (callback.GetProperty("Deployment").GetBoolean())
                Require(born.Length == expected.Length, "The original complete roster must appear in deployment.");
            if (lonelyRooms)
            {
                int turn = callback.GetProperty("Turn").GetInt32();
                UnityRng rng = before.Context.BattleRng;
                if (turn == 1)
                {
                    RngDraw firstFloor = rng.Range(0, 3);
                    RngDraw secondFloor = firstFloor.State.Next().Range(0, 2);
                    Require(firstFloor.Value == 2 && secondFloor.Value == 1 &&
                        before.Context.SpawnPoints!.FirstEmpty(2, CombatTeam.Player) < 0 &&
                        before.Context.SpawnPoints.FirstEmpty(1, CombatTeam.Player) < 0 && born.Length == 1 &&
                        after.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Id == born[0].Id) &&
                        after.Context!.BattleRng.Equals(Advance(rng, 6)),
                        "The first regular native callback must retry both full floors and spawn after six Battle RNG draws.");
                    fullFloorRetry = true;
                }
                if (turn == 5)
                {
                    var settled = root.GetProperty("Turns").EnumerateArray().Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                        .First(state => state.Spawn.Turn == turn);
                    RngDraw firstFloor = rng.Range(0, 3);
                    Require(firstFloor.Value == 2 && firstFloor.State.Next().Range(0, 2).Value == 0 &&
                        before.Context.SpawnPoints!.FirstEmpty(2, CombatTeam.Player) < 0 &&
                        before.Context.SpawnPoints.FirstEmpty(0, CombatTeam.Player) >= 0 &&
                        before.Context.SpawnPoints.FirstEmpty(1, CombatTeam.Player) < 0 &&
                        settled.PlayRules!.Rooms.Any(room => room.RoomIndex == 0 && !room.Enabled) && born.Length == 0 &&
                        after.Context!.BattleRng.Equals(Advance(rng, 5)),
                        "The native callback must skip the empty disabled floor and consume five Battle RNG draws.");
                    disabledFloorSkip = true;
                }
            }
            births += born.Length;
        }
        Require(births > expected.Length, "No later after-draw summon actually appeared.");
        Require(!lonelyRooms || fullFloorRetry && disabledFloorSkip, "The Lonely Rooms archive must witness both retry and disabled-floor paths.");
        SolverStageChecks.Native(path);
        Console.WriteLine($"AFTER-DRAW-SPAWN PASS owner={definition.GetProperty("Key").GetString()} callbacks={callbacks.Length} births={births}");
        if (lonelyRooms) Console.WriteLine("AFTER-DRAW-FLOOR-BOUNDARIES PASS: two full floors retried and empty disabled floor skipped.");
    }

    private static UnityRng Advance(UnityRng rng, int draws)
    {
        for (int draw = 0; draw < draws; draw++) rng = rng.Next();
        return rng;
    }

    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
