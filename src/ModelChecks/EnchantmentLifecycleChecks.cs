using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentLifecycleChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        FixtureValue fixture = document.RootElement;
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Boundary").GetString() == "StatusApiRequests" &&
            fixture.GetProperty("StatusMutationsSuppressed").GetBoolean() &&
            fixture.GetProperty("IgnoredScalingAndChanceFieldsConfigured").GetBoolean() &&
            fixture.GetProperty("LiveContextUnchanged").GetBoolean() && fixture.GetProperty("Mismatches").GetInt32() == 0,
            "Native enchantment calibration version/boundary/restoration gate failed.");
        var records = fixture.GetProperty("Samples").EnumerateArray().ToArray();
        Require(records.Length >= 1000, "Native enchantment lifecycle matrix is incomplete.");
        foreach (var record in records) Verify(record);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        var samples = records.Select(record => new
        {
            Label = record.GetProperty("Label").GetString()!,
            Before = record.GetProperty("Before").Deserialize<EnchantmentState>()!,
            Input = record.GetProperty("Input").Deserialize<EnchantmentInput>()!,
            After = record.GetProperty("After").Deserialize<EnchantmentTransition>()!
        }).ToArray();
        Require(samples.Count(sample => sample.Label.StartsWith("gates:", StringComparison.Ordinal)) == 1152 &&
            samples.Count(sample => sample.Label.StartsWith("counts:", StringComparison.Ordinal)) == 192 &&
            samples.Count(sample => sample.Label.StartsWith("random-update-", StringComparison.Ordinal)) == 64,
            "Native source/recipient/preview/pending-action or status-count matrix is incomplete.");
        var firstCollection = samples.First(sample => sample.Label == "collect-2");
        Require(firstCollection.Input.CollectedTargetIds.SequenceEqual(new[] { 2 }) && firstCollection.Before.PrimaryTargets.Count == 0 &&
            firstCollection.After.State.PrimaryTargets.Single().IsEnchanted &&
            samples.Last(sample => sample.Label == "collect-2").After.Requests.Count == 0 &&
            samples.Single(sample => sample.Label == "collect-1").After.State.PrimaryTargets.Select(target => target.UnitId).SequenceEqual(new[] { 2, 3, 4 }),
            "Native first collection, duplicate update, insertion order or source exclusion is missing.");
        Require(samples.Any(sample => sample.After.Requests.Count > 0 && sample.After.Requests.Any(request => request.Operation == "Remove")) &&
            samples.Any(sample => sample.After.Requests.Any(request => request.SourceIsHero == true)) &&
            samples.Any(sample => sample.After.Requests.Any(request => request.SourceIsHero == false)),
            "Native addition/removal or recipient-team attribution is missing.");
        Require(samples.Any(sample => sample.After.State.PrimaryTargets.Any(target => target.IsEnchanted && target.NextAction == 2)) &&
            samples.Any(sample => sample.After.State.PreviewTargets.Any(target => target.IsEnchanted && target.NextAction == 2)) &&
            samples.Any(sample => sample.After.State.PrimaryTargets.Any(target => !target.IsEnchanted && target.NextAction == 1)),
            "Native skipped target states/pending actions were not recorded.");
        Require(samples.Any(sample => sample.Label == "duality-changes-remove" && sample.After.Requests.Single().Count == 4) &&
            samples.Any(sample => sample.After.Requests.Any(request => request.Count == 0)) &&
            samples.Any(sample => sample.After.Requests.Any(request => request.Count < 0)) &&
            samples.Any(sample => sample.After.Requests.Any(request => request.Count == int.MinValue)) &&
            samples.Any(sample => sample.After.Requests.Any(request => request.Count == int.MaxValue)),
            "Native current-duality removal or signed/zero counts are missing.");
        var stale = samples.Single(sample => sample.Label == "sync-retains-preview-only-keys");
        Require(stale.After.State.PreviewTargets.Select(target => target.UnitId).SequenceEqual(new[] { 4, 2, 3 }) &&
            stale.After.State.PrimaryTargets.Select(target => target.UnitId).SequenceEqual(new[] { 2, 3 }) &&
            !stale.After.State.PreviewRequiresSync, "Native preview merge discarded stale keys or reordered existing keys.");
        var setup = samples.Single(sample => sample.Label == "setup-preserves-sync-and-cache");
        Require(setup.After.State.PrimaryTargets.Count == 0 && setup.After.State.PreviewTargets.Count == 0 &&
            setup.After.State.PreviewRequiresSync && setup.After.State.CachedStatus != null,
            "Native Setup's retained cache/sync flag was not exercised.");
        Require(samples.Any(sample => sample.Label == "primary-unaffected" && sample.After.Requests.Count == 0) &&
            samples.Any(sample => sample.Label.StartsWith("random-update-", StringComparison.Ordinal) && sample.Input.Preview &&
                sample.After.BattleRng.Equals(sample.Input.BattleRng) && !sample.After.TestRng.Equals(sample.Input.TestRng)) &&
            samples.Where(sample => sample.Label.StartsWith("random-update-", StringComparison.Ordinal))
                .Select(sample => sample.After.State.CachedStatus!.Id).Distinct().Count() == 3 &&
            samples.Count(sample => sample.Label.StartsWith("unbound-", StringComparison.Ordinal)) == 3,
            "Native primary isolation, preview Battle RNG, random pool or early binding gates are incomplete.");
        foreach (var sample in samples)
        {
            foreach (var request in sample.After.Requests)
            {
                var map = sample.Input.Preview ? request.StateAtCall.PreviewTargets : request.StateAtCall.PrimaryTargets;
                var entry = map.Single(target => target.UnitId == request.UnitId);
                Require(entry.IsEnchanted == (request.Operation == "Add") && entry.NextAction == (request.Operation == "Add" ? 1 : 2),
                    "Native map was not updated before the status API call.");
            }
        }
        VerifyDefensiveCopies(samples[0].Input);
        VerifyIntegrationBoundary();
        var initialChain = records.TakeWhile(record => record.GetProperty("Label").GetString() != "prepare-stale-preview").ToArray();
        // Two explicit Seed calls precede prepare-stale-preview; stop before that external map reset.
        var randomChain = records.Where(record => record.GetProperty("Label").GetString()!.StartsWith("random-", StringComparison.Ordinal)).ToArray();
        VerifyChain(initialChain); VerifyChain(randomChain);
        Parallel.For(0, 32, _ => { VerifyChain(initialChain); VerifyChain(randomChain); });
        int requests = samples.Sum(sample => sample.After.Requests.Count);
        Console.WriteLine($"NATIVE-ENCHANTMENT-LIFECYCLE-CHECKS PASS: {samples.Length} exact native lifecycle transitions, {requests} status API requests, " +
            $"ordered retained/preview maps, current duality, signed counts, exact Battle/BattleTest streams, binding gates, " +
            $"{initialChain.Length}/{randomChain.Length} carried chain steps, live restoration and 32 branches; status mutation/automatic lifecycle integration remains open.");
    }
    private static void Verify(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete or mismatched native enchantment sample.");
        var before = record.GetProperty("Before").Deserialize<EnchantmentState>()!;
        var input = record.GetProperty("Input").Deserialize<EnchantmentInput>()!;
        string root = JsonSerializer.Serialize(before); string inputRoot = JsonSerializer.Serialize(input);
        EnchantmentTransition result = EnchantmentLifecycleModel.Apply(before, input);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result),
            JsonSerializer.Serialize(record.GetProperty("After").Deserialize<EnchantmentTransition>()));
        Require(difference == null, record.GetProperty("Label").GetString() + ": " + difference);
        Require(input.TestRng.Equals(record.GetProperty("TestBefore").Deserialize<UnityRng>()) &&
            result.TestRng.Equals(record.GetProperty("TestAfter").Deserialize<UnityRng>()), "Enchant BattleTest state differs.");
        Require(JsonSerializer.Serialize(before) == root && JsonSerializer.Serialize(input) == inputRoot,
            "Enchantment branch mutated its parent/input.");
    }
    private static void VerifyChain(FixtureValue[] records)
    {
        EnchantmentState state = records[0].GetProperty("Before").Deserialize<EnchantmentState>()!;
        EnchantmentState parent = state;
        var first = records[0].GetProperty("Input").Deserialize<EnchantmentInput>()!;
        UnityRng battle = first.BattleRng, test = first.TestRng;
        string root = JsonSerializer.Serialize(state);
        foreach (var record in records)
        {
            var input = record.GetProperty("Input").Deserialize<EnchantmentInput>()!;
            Require(input.BattleRng.Equals(battle) && input.TestRng.Equals(test), "Native lifecycle chain has an unexplained RNG discontinuity.");
            var carried = new EnchantmentInput(input.Operation, input.SourceId, input.CanUpdate, input.Preview, battle, test,
                input.StatusPool, input.CollectedTargetIds, input.Actors);
            var result = EnchantmentLifecycleModel.Apply(state, carried);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(result),
                JsonSerializer.Serialize(record.GetProperty("After").Deserialize<EnchantmentTransition>()));
            Require(difference == null, "Carried lifecycle chain differs at " + record.GetProperty("Label").GetString() + ": " + difference);
            state = result.State; battle = result.BattleRng; test = result.TestRng;
        }
        Require(JsonSerializer.Serialize(parent) == root,
            "Lifecycle chain mutated its root.");
    }
    private static void VerifyDefensiveCopies(EnchantmentInput sample)
    {
        var entries = new[] { new EnchantmentTarget(2, true) };
        var state = new EnchantmentState(entries, entries); entries[0] = new EnchantmentTarget(3, false);
        Require(state.PrimaryTargets[0].UnitId == 2 && state.PreviewTargets[0].UnitId == 2,
            "Enchantment state retained a caller-owned mutable map.");
        var actors = sample.Actors.ToArray(); var targets = new[] { 2 }; var pool = new[] { new EnchantmentStatus("armor", 2, "Positive") };
        var input = new EnchantmentInput("Update", 1, true, false, sample.BattleRng, sample.TestRng, pool, targets, actors);
        pool[0] = new EnchantmentStatus("poison", 9, "Negative"); targets[0] = 3; actors[0] = actors[1];
        Require(input.StatusPool[0].Id == "armor" && input.CollectedTargetIds[0] == 2 && input.Actors[0].Id == 1,
            "Enchantment input retained caller-owned lists.");
    }
    private static void VerifyIntegrationBoundary()
    {
        var aura = new CombatEffect("CardEffectEnchant", 0, 0, "", 0, [], false);
        var actor = new CombatUnit(1, "aura-host", CombatTeam.Player, 1, 10, 10, true, false, false, [],
            [new CombatTrigger("PostCombat", false, false, false, 1, [aura])]);
        var result = RoomCombatModel.Resolve(new RoomCombatState(0, false, [actor], []));
        Require(!result.Supported && result.UnsupportedReason?.Contains("CardEffectEnchant", StringComparison.Ordinal) == true,
            "The standalone aura primitive was silently accepted as complete combat integration.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
