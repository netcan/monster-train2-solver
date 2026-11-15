using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
internal static class SpawnStatusScalingMechanismChecks
{
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "DefinitionDrivenSpawnStatusScaling" && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Wrong native spawn scaling version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("Original", "OriginalAfter"), ("RngBefore", "RngAfter"), ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Spawn scaling batch changed " + pair.Item1);
        Require(root.GetProperty("QueueBefore").GetInt32() == 0 && root.GetProperty("Original").GetArrayLength() == 4, "Missing four original definitions/empty queue.");
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Rule").Deserialize<RelicSpawnStatusScalingRule>()!);
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(definitions.Count == 54 && samples.Length == 2592, "Missing native scaling flag/count/team/subtype/repeat controls.");
        var context = root.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        string parent = JsonSerializer.Serialize(context, ModelJson.Options);
        void Verify()
        {
            foreach (var row in samples)
            {
                var rule = definitions[row.GetProperty("Definition").GetString()!];
                int count = row.GetProperty("Count").GetInt32(), repeat = row.GetProperty("Repeat").GetInt32();
                string[] subtypes = row.GetProperty("Subtypes").Deserialize<string[]>()!;
                bool eligible = row.GetProperty("Team").GetString() == "Heroes" && !subtypes.Any(rule.ExcludedSubtypes.Contains) &&
                    (rule.RequiredMutatorId == null || !context.Resources!.MutatorIds.Contains(rule.RequiredMutatorId));
                var expectedRequests = new List<(string Id, int Count)>();
                foreach (string id in new[] { "buff", "armor", "spikes" })
                    if (eligible && rule.ScaleStarting && rule.StartingDelta(id, count) > 0) expectedRequests.Add((id, rule.StartingDelta(id, count)));
                var actualRequests = row.GetProperty("Requests").EnumerateArray().Select(request =>
                { Require(request.GetProperty("SpawnEffect").GetBoolean(), "Starting delta lost native spawnEffect."); return (request.GetProperty("Id").GetString()!, request.GetProperty("Count").GetInt32()); }).ToArray();
                Require(expectedRequests.SequenceEqual(actualRequests), "Native starting addition requests differ.");
                Require(row.GetProperty("Source").ContentEquals(row.GetProperty("SourceAfter")), "Runtime rewrite changed source definition.");
                foreach (var effect in row.GetProperty("Runtime").EnumerateArray())
                    foreach (var status in effect.GetProperty("Statuses").EnumerateArray())
                    {
                        string id = status.GetProperty("statusId").GetString()!; int expected = count;
                        if (eligible && rule.ScaleApplied && effect.GetProperty("Type").GetString() is "CardEffectAddStatusEffect" or "CardEffectEnchant")
                            for (int i = 0; i <= repeat; i++) expected = rule.Count(id, expected);
                        Require(status.GetProperty("count").GetInt32() == expected, "Native repeated runtime trigger parameter differs.");
                    }
                var rawEnchant = row.GetProperty("Runtime")[1].GetProperty("Statuses").EnumerateArray().ToArray();
                var capturedEnchant = row.GetProperty("CapturedEnchant").Deserialize<CombatStatus[]>()!;
                Require(capturedEnchant.Length == rawEnchant.Length && capturedEnchant.Select(status => (status.Id, status.Stacks)).SequenceEqual(
                    rawEnchant.Select(status => (status.GetProperty("statusId").GetString()!, status.GetProperty("count").GetInt32()))), "Enchantment capture ignored runtime status parameters.");
            }
        }
        Verify(); Parallel.For(0, 16, _ => Verify());
        foreach (var row in samples.Where(value => value.GetProperty("Count").GetInt32() == 3 && value.GetProperty("Repeat").GetInt32() == 0))
        {
            var rule = definitions[row.GetProperty("Definition").GetString()!];
            if (rule.ScaleStarting) continue; // Real status application is covered by the full native battle.
            var statuses = row.GetProperty("CapturedEnchant").Deserialize<CombatStatus[]>()!.Select(status => status.WithStacks(3)).ToArray();
            var targeting = new CardActionEffect("AddStatus", "Self", 19, true, false, statuses,
                tests: new CardEffectTests(true, true, true, false, true), disallowStatusStackModifiers: true);
            var effects = new[] { new CombatEffect("CardEffectAddStatusEffect", 17, 7, "", 13, new[] { "preserve" }, true, action: targeting),
                new CombatEffect("CardEffectEnchant", 17, 7, "", 13, Array.Empty<string>(), false, enchantment: new EnchantmentRule(targeting, statuses)),
                new CombatEffect("CardEffectAddStatusEffectNextMonster", 17, 7, "", 13, Array.Empty<string>(), false, action: targeting) };
            var trigger = new CombatTrigger("PreCombat", true, false, true, 0, effects);
            var unit = new CombatUnit(98765, "any-character", row.GetProperty("Team").GetString() == "Heroes" ? CombatTeam.Enemy : CombatTeam.Player,
                1, 10, 10, true, false, false, Array.Empty<CombatStatus>(), new[] { trigger }, subtypes: row.GetProperty("Subtypes").Deserialize<string[]>()!);
            var state = new RoomCombatState(0, false, new[] { unit }, Array.Empty<string>(), context);
            string unchanged = JsonSerializer.Serialize(state, ModelJson.Options);
            var descriptor = new RelicResourceEffect(0, RelicSpawnStatusScalingModel.Effect, 0, 0, 0, false, spawnStatusScaling: rule);
            var result = RelicSpawnStatusScalingModel.Apply(state, unit.Id, descriptor);
            Require(result.Supported, result.UnsupportedReason ?? "Shared trigger scaling failed.");
            var actual = result.State!.Units.Single().Triggers.Single().Effects;
            for (int e = 0; e < actual.Count; e++)
            {
                var observed = e == 1 ? actual[e].Enchantment!.StatusPool : actual[e].Action!.Statuses;
                var expected = row.GetProperty("Runtime")[e].GetProperty("Statuses").EnumerateArray().ToArray();
                Require(observed.Select(status => (status.Id, status.Stacks)).SequenceEqual(expected.Select(status => (status.GetProperty("statusId").GetString()!, status.GetProperty("count").GetInt32()))),
                    "Shared immutable trigger transition differs from native callback.");
                Require(actual[e].Value == 17 && actual[e].Counter == 7 && actual[e].Count == 13, "Trigger rewrite lost unrelated effect parameters.");
            }
            var preserved = actual[0].Action!;
            Require(preserved.Value == 19 && ReferenceEquals(preserved.Tests, targeting.Tests) && preserved.DisallowStatusStackModifiers &&
                unchanged == JsonSerializer.Serialize(state, ModelJson.Options), "Trigger rewrite lost target metadata or changed its parent.");
        }
        var payload = definitions.Values.First();
        var effect = new RelicResourceEffect(0, RelicSpawnStatusScalingModel.Effect, 0, 0, 0, false, spawnStatusScaling: payload);
        var owner = new CombatRelicState("any-id", "any-key", new[] { effect.Type }, isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: new[] { effect });
        Require(RelicModel.Validate(new[] { owner }) == null && ReferenceEquals(effect.SpawnStatusScaling, effect.WithConditions(Array.Empty<RelicConditionState>()).SpawnStatusScaling),
            "Shared scaling uses an owner allowlist or loses its immutable payload.");
        Require(RelicModel.Validate(new[] { new CombatRelicState("missing", "missing", new[] { effect.Type }) }) != null && parent == JsonSerializer.Serialize(context, ModelJson.Options), "Missing scaling fields accepted or parent changed.");
        Console.WriteLine($"SPAWN-STATUS-SCALING-BATCH PASS originals=4 definitions=54 callbacks=2592 parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3}; starting requests, runtime rewrites/exact types/enchant capture and complete source purity.");
    }
    private static IEnumerable<FixtureValue> Objects(FixtureValue value)
    {
        if (value.ValueKind == FixtureKind.Object) { yield return value; foreach (var child in value.EnumerateObject()) foreach (var item in Objects(child.Value)) yield return item; }
        else if (value.ValueKind == FixtureKind.Array) foreach (var child in value.EnumerateArray()) foreach (var item in Objects(child)) yield return item;
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("SpawnStatusScalingPrepared").GetBoolean() && root.GetProperty("SpawnStatusScalingDefinition").ContentEquals(root.GetProperty("SpawnStatusScalingDefinitionAfter")), "Original scaling owner changed.");
        var units = Objects(root).Where(item => item.TryGetProperty("AbilityRules", out var rules) && rules.ValueKind == FixtureKind.Object && item.TryGetProperty("Team", out var team) && team.GetInt32() == (int)CombatTeam.Enemy)
            .Select(item => item.Deserialize<CombatUnit>()!).Where(unit => unit.AbilityRules!.AuthoredStartingStatuses.Any(status => status.Id == "buff" && status.Stacks == 3)).ToArray();
        Require(units.Any(unit => unit.Statuses.Any(status => status.Id == "buff" && status.Stacks == 4)) && units.Any(unit => unit.Triggers.Any(trigger => trigger.Effects.Any(effect =>
            effect.Type == "CardEffectAddStatusEffect" && effect.Action!.Statuses.Any(status => status.Id == "buff" && status.Stacks == 4)))), "No actual scaled starting status/runtime trigger in native battle.");
        CardCycleChecks.Native(root); SolverStageChecks.Native(path);
        Console.WriteLine("SPAWN-STATUS-SCALING-BATTLE PASS unchanged original endless owner; authored enemy starting/trigger controls, complete successors/16 branches/frontiers/serial-parallel root search.");
    }
}
