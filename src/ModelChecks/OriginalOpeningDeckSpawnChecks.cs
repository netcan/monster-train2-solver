using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOpeningDeckSpawnChecks
{
    internal static void Run()
    {
        CombatRelicState Owner(string id) => new(id, RelicOpeningDeckSpawnModel.AssetKey,
            ["RelicEffectNull", RelicOpeningDeckSpawnModel.Effect, RelicOpeningDeckSpawnModel.Effect, RelicOpeningDeckSpawnModel.Effect],
            isCovenant: false, disallowedInPlacementPhase: true,
            resourceEffects: Enumerable.Range(1, 3).Select(index => new RelicResourceEffect(index, RelicOpeningDeckSpawnModel.Effect,
                index - 1, 0, 0, false, deckSpawn: new(0, []))).ToArray());
        Require(RelicModel.Validate([Owner(RelicOpeningDeckSpawnModel.DataId)]) == null &&
            RelicModel.Validate([Owner("changed-owner")]) != null, "Original opening deck-spawn identity guard failed.");
        Console.WriteLine("OPENING-DECK-SPAWN-PURE PASS: exact original effect ordering, room parameters, empty initial cache and changed-owner rejection.");
    }
    internal static void Native(FixtureValue root)
    {
        Run();
        Require(root.GetProperty("Schema").GetInt32() == 244 && root.GetProperty("OriginalOpeningDeckSpawnPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete native opening deck-spawn.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalOpeningDeckSpawnCaptureFailures",
            "OriginalOpeningDeckSpawnMismatches", "OriginalOpeningDeckSpawnUnsupported", "OriginalOpeningDeckSpawnPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int originalTests = 0, resetControls = 0;
        foreach (FixtureValue row in root.GetProperty("RelicOpeningDeckSpawnTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int index = row.GetProperty("EffectIndex").GetInt32(); bool isolated = row.GetProperty("Isolated").GetBoolean();
            TrainCombatResult result = RelicOpeningDeckSpawnModel.Test(before, RelicOpeningDeckSpawnModel.DataId, index, out bool passed);
            Require(result.Supported && passed == row.GetProperty("Returned").GetBoolean(), result.UnsupportedReason ?? "Wrong Test result.");
            int[] cached = result.State!.Context!.Relics!.Single(owner => owner.DataId == RelicOpeningDeckSpawnModel.DataId)
                .ResourceEffects!.Single(effect => effect.EffectIndex == index).DeckSpawn!.CachedCardIds.ToArray();
            Equal(cached, row.GetProperty("CacheAfter").Deserialize<int[]>(), "native Test candidate cache");
            Equal(isolated ? before : result.State, actual, "Test complete train");
            if (isolated)
            {
                Require(!passed && row.GetProperty("CacheBefore").EnumerateArray().Count() > 0 && cached.Length == 0,
                    "Missing seeded-cache reset with no remaining unit cards.");
                resetControls++;
            }
            else
            {
                Require(passed && cached.Length == 5 - index && before.Context!.Cards.Draw.Count > cached.Length,
                    "Missing 4/3/2 unit pools or non-unit exclusion controls.");
                originalTests++;
            }
        }
        Require(originalTests == 3 && resetControls == 1, "Missing native original tests/reset control.");
        FixtureValue[] applications = root.GetProperty("RelicOpeningDeckSpawnApplications").EnumerateArray().ToArray();
        Require(applications.Select(row => row.GetProperty("EffectIndex").GetInt32()).SequenceEqual([1, 2, 3]), "Wrong native effect/floor order.");
        foreach (FixtureValue row in applications)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            BattlePlayRules definitions = row.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
            int index = row.GetProperty("EffectIndex").GetInt32();
            CombatRelicState owner = before.Context!.Relics!.Single(item => item.DataId == RelicOpeningDeckSpawnModel.DataId);
            Require(RelicModel.Validate([owner]) == null, "Changed native original owner.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatResult result = RelicOpeningDeckSpawnModel.Apply(before, definitions, owner.DataId, index);
                Require(result.Supported, result.UnsupportedReason ?? "Deck spawn unsupported.");
                Equal(result.State, actual, "native deck-spawn complete train");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Deck spawn mutated parent.");
            RelicResourceEffect effect = owner.ResourceEffects!.Single(item => item.EffectIndex == index);
            var shuffled = before.Context.Cards.Rng.Shuffle(effect.DeckSpawn!.CachedCardIds);
            int selected = shuffled.Items[0];
            CombatUnit unit = actual.Rooms.Single(room => room.RoomIndex == index - 1).Units.Single(actor => actor.SpawnerCardId == selected);
            CardPileState standby = actual.Context!.OtherPiles!.Single(pile => pile.Name == "Standby");
            Require(unit.Modifiers?.IsClone == false && unit.Status("cardless") == null &&
                standby.Cards.Any(card => card.InstanceId == selected) && standby.UnitConditions!.Single(binding => binding.CardId == selected).HostUnitId == unit.Id &&
                !actual.Context.Cards.Draw.Any(card => card.InstanceId == selected) && actual.Context.Cards.Draw.Count == before.Context.Cards.Draw.Count - 1 &&
                actual.Context.Cards.Rng.Equals(shuffled.State) && actual.Context.BattleRng.Equals(before.Context.BattleRng) &&
                row.GetProperty("Notifications").GetInt32() == 0 && row.GetProperty("NonDrawableAfter").GetInt32() == 0,
                "Wrong shuffle stream, source-card/standby binding, birth flags or restored eligibility/notification state.");
            Require(!RelicOpeningDeckSpawnModel.Apply(before, definitions, "absent-owner", index).Supported &&
                !RelicOpeningDeckSpawnModel.Apply(before, definitions, owner.DataId, 0).Supported, "Missing owner/effect accepted.");
        }
        Console.WriteLine("ORIGINAL-OPENING-DECK-SPAWN-NATIVE PASS: unchanged owner, three 4/3/2 unit pools, non-Monster controls, cached CardDraw shuffles, card-backed non-clone births on floors 0/1/2, exact standby/statistics/eligibility state, no notification, seeded-cache reset and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
