using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossHealthChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "NativeLifeLinkAndNonterminalHealthUpdates" &&
            root.GetProperty("NativeStateReferencesRestored").GetBoolean() && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("CharacterPreviewModesOff").GetBoolean() && root.GetProperty("AllScenarioBossesDeadUnchanged").GetBoolean() &&
            root.GetProperty("WorldBefore").ContentEquals(root.GetProperty("WorldAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) &&
            root.GetProperty("RngBefore").GetProperty("Streams").GetArrayLength() == 16 &&
            root.GetProperty("RngBefore").GetProperty("Unity").GetArrayLength() == 4 &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("WorldBefore").GetProperty("Characters").EnumerateArray()
                .Select(actor => actor.GetProperty("AssetKey").GetString()).SequenceEqual(TrueFinalBossChecks.Assets),
            "Health-update controls changed original references, world, preview controls, terminal flag, frame or RNG.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 200, "Native health-update matrix or dependent chains are incomplete.");
        var matrix = new HashSet<(int Mode, int Target, string Operation, int Value)>();
        var chain = new Dictionary<int, string>();
        var chainCounts = new Dictionary<int, int>();
        TrueFinalBossHealthState[] shared = rows.Select(row => row.GetProperty("Before").Deserialize<TrueFinalBossHealthState>()!).ToArray();
        string[] immutable = shared.Select(state => Json(state)).ToArray();
        int sharedSignals = 0, noOps = 0, capped = 0, recovered = 0, protectedHits = 0;
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index]; TrueFinalBossHealthState before = shared[index];
            int mode = row.GetProperty("Mode").GetInt32(), target = row.GetProperty("TargetId").GetInt32(), value = row.GetProperty("Value").GetInt32();
            string operation = row.GetProperty("Operation").GetString()!, label = row.GetProperty("Label").GetString()!;
            TrueFinalBossHealthResult actual = row.GetProperty("Actual").Deserialize<TrueFinalBossHealthResult>()!;
            TrueFinalBossHealthResult result = TrueFinalBossHealthModel.Mutate(before, target, operation, value);
            string? difference = ModelJson.Difference(Json(actual), Json(result));
            Require(result.Supported && difference == null && Json(before) == immutable[index],
                $"Native health update differs or source mutated: {index} {label} {target}/{operation}/{value}: {result.UnsupportedReason ?? difference}");
            Require(before.LifeLink.Actors.Select(actor => actor.Id).SequenceEqual(new[] { 1, 2, 3, 4, 5 }) &&
                before.LifeLink.Actors.Select(actor => actor.LinkedId).SequenceEqual(new[] { 0, 0, 0, 3, 3 }) &&
                before.LifeLink.Actors.All(actor => actor.Initialized && actor.HasLifeLink && actor.TrueFinalBoss && actor.OuterBoss == (actor.Id >= 3)) &&
                before.LifeLink.CalculatingPreview == (mode == 1), "Native identity, health link, status or preview-mode control differs.");
            if (label == "matrix")
                Require(matrix.Add((mode, target, operation, value)) && before.LifeLink.SharedPreviewHpChange == 0 &&
                    before.LifeLink.Actors.Select(actor => actor.Health).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }) &&
                    before.Health.Actors.All(actor => !actor.Dirty), "Native matrix baseline differs or case duplicated.");
            else
            {
                if (chain.TryGetValue(mode, out string? previous)) Require(previous == Json(before), "Native dependent health chain lost previous successor.");
                chain[mode] = Json(actual.State!);
                chainCounts[mode] = chainCounts.GetValueOrDefault(mode) + 1;
            }
            Require(actual.Events.Count <= 1 && actual.Events.All(signal => signal.Id == target &&
                signal.Health == actual.State!.LifeLink.Actors.Single(actor => actor.Id == target).Health),
                "Native HP signal was broadcast to linked peers instead of only the updated actor.");
            if (actual.Events.Count == 0) noOps++;
            if (target >= 4 && actual.Events.Count == 1)
            {
                Require(actual.State!.LifeLink.Actors.Skip(2).Select(actor => actor.Health).Distinct().Count() == 1,
                    "Linked body health failed to synchronize across three rooms.");
                sharedSignals++;
            }
            if (operation == "UpdateHp" && value > 99999)
            {
                Require(actual.State!.LifeLink.Actors[target - 1].Health == 99999, "Native upper-only cap differs."); capped++;
            }
            if (label.EndsWith("-recover", StringComparison.Ordinal)) recovered++;
            if (actual.DamageStage?.Triggered == true) protectedHits++;
            if (label == "chain-all-groups-one") Require(actual.State!.LifeLink.Actors.All(actor => actor.Health == 1), "Missing native all-groups-at-one chain witness.");
        }
        var expected = (from mode in new[] { 0, 1 } from target in Enumerable.Range(1, 5)
            from operation in new[] { "DebuffHP", "UpdateHp", "LifeLinkAndDebuffHP" }
            from value in operation == "DebuffHP" ? new[] { int.MinValue, -1, 0, 1, 9 } : operation == "UpdateHp" ?
                new[] { 1, 2, 17, 99999, 100000, int.MaxValue } : new[] { int.MinValue, -1, 0, 1, 9, 4000, int.MaxValue }
            select (mode, target, operation, value)).ToHashSet();
        Require(matrix.SetEquals(expected) && chainCounts.Count == 2 && chainCounts.Values.All(count => count == 10) &&
            sharedSignals > 0 && noOps > 0 && capped == 22 && recovered == 4 && protectedHits > 0,
            "Missing native no-op, single-target signal/shared health, upper cap, recovery or protected-lethal witnesses.");
        Parallel.For(0, 256, index =>
        {
            int selected = index % rows.Length; FixtureValue row = rows[selected];
            TrueFinalBossHealthResult result = TrueFinalBossHealthModel.Mutate(shared[selected], row.GetProperty("TargetId").GetInt32(),
                row.GetProperty("Operation").GetString()!, row.GetProperty("Value").GetInt32());
            Require(ModelJson.Difference(Json(result), Json(row.GetProperty("Actual").Deserialize<TrueFinalBossHealthResult>()!)) == null,
                "Concurrent shared-health update differs from native successor.");
        });
        TrueFinalBossHealthState seed = shared[0];
        Require(shared.Select(state => Json(state)).SequenceEqual(immutable) &&
            !TrueFinalBossHealthModel.Mutate(seed, 999, "DebuffHP", 1).Supported &&
            !TrueFinalBossHealthModel.Mutate(seed, 1, "Unknown", 1).Supported &&
            !TrueFinalBossHealthModel.Mutate(seed, 1, "DebuffHP", 4000).Supported &&
            !TrueFinalBossHealthModel.Mutate(seed, 1, "UpdateHp", 0).Supported &&
            TrueFinalBossChecks.Assets.All(asset => !OuterBossDefinitions.Supports(asset)),
            "Missing immutable parent, malformed-operation/terminal rejection or Level9 full-battle guard.");
        Console.WriteLine("TRUE-FINAL-BOSS-HEALTH-UPDATES PASS: 180 native controls + 20 dependent updates; cross-room linked HP, life-link + DebuffHP, target-only HP signals, preview delta, recovery, integer caps, 256 concurrent branches and unchanged original state/frame/RNG. Full turn/death remains guarded.");
    }
}
