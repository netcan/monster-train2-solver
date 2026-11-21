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
            births += born.Length;
        }
        Require(births > expected.Length, "No later after-draw summon actually appeared.");
        SolverStageChecks.Native(path);
        Console.WriteLine($"AFTER-DRAW-SPAWN PASS owner={definition.GetProperty("Key").GetString()} callbacks={callbacks.Length} births={births}");
    }

    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
