using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceUpgradeMechanismChecks
{
    private static void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
    private static string Snapshot(CardUpgradeModifier upgrade) => JsonSerializer.Serialize(upgrade, ModelJson.Options);
    internal static void Batch(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Scope").GetString() == "DefinitionDrivenJuiceUpgrades" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Wrong Juice upgrade matrix version/scope.");
        foreach (var pair in new[] { ("Before", "After"), ("Definitions", "DefinitionsAfter"), ("RngBefore", "RngAfter"),
            ("FrameBefore", "FrameAfter"), ("QueueBefore", "QueueAfter") })
            Require(root.GetProperty(pair.Item1).ContentEquals(root.GetProperty(pair.Item2)), "Juice upgrade matrix mutated " + pair.Item1);
        Require(root.GetProperty("QueueBefore").GetInt32() == 0, "Juice upgrade matrix was not settled.");
        var definitions = root.GetProperty("Definitions").EnumerateArray().ToArray();
        Require(definitions.Length == 5 && definitions.Select(row => row.GetProperty("Id").GetString()).Distinct().Count() == 5,
            "Incomplete original Juice upgrade discovery.");
        var traits = definitions.ToDictionary(row => row.GetProperty("Id").GetString()!, row => row.GetProperty("Trait").Deserialize<CardTraitValue>()!);
        Require(traits.Values.Select(trait => trait.RuntimeType).Order().Distinct().SequenceEqual(new[] { "CardTraitJuice", "CardTraitJuice3", "CardTraitJuice4" }),
            "Original Juice definitions lack a multiplier family.");
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray(); var sequences = root.GetProperty("Sequences").EnumerateArray().ToArray();
        Require(samples.Length == 540 && sequences.Length == 300 &&
            samples.Select(row => row.GetProperty("Source").GetInt32()).Distinct().Order().SequenceEqual(new[] { 0, 1, 2 }) &&
            samples.Select(row => row.GetProperty("Target").GetInt32()).Distinct().Order().SequenceEqual(new[] { 0, 1, 2 }), "Incomplete Juice relation/order matrix.");
        foreach (var row in samples)
        {
            var input = row.GetProperty("Input").Deserialize<CardUpgradeModifier>()!; string frozen = Snapshot(input);
            var trait = traits[row.GetProperty("Id").GetString()!]; int source = row.GetProperty("Source").GetInt32();
            var actual = row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!;
            var expected = CardJuiceModel.OnCardBeingUpgraded(new[] { trait }, 1, source, input);
            var repeat = CardJuiceModel.OnCardBeingUpgraded(new[] { trait }, 1, source, expected);
            Require(row.GetProperty("Returned").GetBoolean() && row.GetProperty("RepeatReturned").GetBoolean() &&
                Snapshot(expected) == Snapshot(actual) && Snapshot(repeat) == Snapshot(row.GetProperty("Repeated").Deserialize<CardUpgradeModifier>()!) &&
                frozen == Snapshot(input), "Native Juice source callback/full upgrade differs: " + trait.RuntimeType + "/" + source + "/" + row.GetProperty("Count").GetInt32());
            Require(row.GetProperty("CloneStatuses").EnumerateArray().Select(value => (value.GetProperty("Id").GetString(), value.GetProperty("Stacks").GetInt32()))
                .SequenceEqual(actual.Statuses.Select(status => ((string?)status.Id, status.Stacks))), "Native copied upgrade statuses changed on the repeated source callback.");
        }
        foreach (var row in sequences)
        {
            var ordered = row.GetProperty("Ids").EnumerateArray().Select(id => traits[id.GetString()!]).ToArray();
            var input = row.GetProperty("Input").Deserialize<CardUpgradeModifier>()!;
            var actual = CardJuiceModel.OnCardBeingUpgraded(ordered, 1, 1, input);
            Require(row.GetProperty("Returned").GetBoolean() && Snapshot(actual) == Snapshot(row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!),
                "Native ordered Juice upgrade callbacks differ.");
        }
        Parallel.For(0, 16, _ => {
            foreach (var row in samples)
            {
                var input = row.GetProperty("Input").Deserialize<CardUpgradeModifier>()!;
                var expected = CardJuiceModel.OnCardBeingUpgraded(new[] { traits[row.GetProperty("Id").GetString()!] }, 1, row.GetProperty("Source").GetInt32(), input);
                Require(expected.Statuses.Select(status => status.Stacks).SequenceEqual(row.GetProperty("Actual").GetProperty("Statuses").EnumerateArray()
                    .Select(status => status.GetProperty("Stacks").GetInt32())), "Parallel Juice upgrade callback differs.");
            }
        });
        Console.WriteLine($"JUICE-UPGRADE-BATCH PASS originals=5 relations={samples.Length} repeated={samples.Length} orderedPairs={sequences.Length} parallel=16 nativeSeconds={root.GetProperty("Seconds").GetDouble():F3} state/definitions/RNG/frame/queue unchanged.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("JuiceUpgradePrepared").GetBoolean() && root.GetProperty("JuiceUpgradeDefinitions").ContentEquals(root.GetProperty("JuiceUpgradeDefinitionsAfter")),
            "Original Juice upgrade definitions changed or scenario was not prepared.");
        var ids = root.GetProperty("JuiceUpgradeIds").Deserialize<int[]>()!;
        var rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && rows.Length == 8 && rows.Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids),
            "Missing six ordered two-card draw casts.");
        var owners = new Dictionary<int, CardInstanceState>();
        for (int i = 2; i < rows.Length; i++)
        {
            var before = rows[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var source = before.Spawn.Train.Context!.FindCard(ids[i])!; owners.Add(source.InstanceId, source);
            Require(CardJuiceModel.Validate(source) == null, "Juice draw source did not use the shared structural contract.");
            var traits = CardExhaustModel.RefreshTraits(source).CombinedTraits!.Where(trait => CardJuiceModel.IsTrait(trait.RuntimeType)).ToArray();
            Require(traits.Length == 1, "Unexpected Juice source trait composition.");
            int multiplier = CardJuiceModel.Multiplier(traits.Single().RuntimeType);
            var hand = before.Spawn.Train.Context.Cards.Hand.Select(card => card.InstanceId).ToHashSet();
            var drawn = after.Spawn.Train.Context!.Cards.Hand.Where(card => !hand.Contains(card.InstanceId)).Reverse()
                .Select(card => after.Spawn.Train.Context.FindCard(card.InstanceId)!).ToArray();
            Require(drawn.Length == 2 && after.Spawn.Train.Context.Cards.NextDrawUpgrades is { Count: 0 }, "Draw/queue-clear lifecycle differs.");
            var payloads = drawn.Select(card => card.Temporary.Upgrades.Single(upgrade => upgrade.AssetKey == "PojuJuiceDrawValor")).ToArray();
            int[] expected = i == 7 ? new[] { multiplier * multiplier, multiplier * multiplier } : new[] { multiplier, multiplier * multiplier };
            Require(payloads.Select(upgrade => upgrade.Statuses.Single().Stacks).SequenceEqual(expected), "Juice queue mutation/deep-copy order differs.");
            Require(i == 7 ? payloads[0].SharedReferenceId.HasValue && payloads[0].SharedReferenceId == payloads[1].SharedReferenceId &&
                drawn.All(card => card.Temporary.Upgrades.All(upgrade => upgrade.AssetKey != "PojuJuiceRejectedArmor" && upgrade.AssetKey != "PojuJuiceSkippedScorch"))
                : payloads.All(upgrade => upgrade.SharedReferenceId == null), "Native original/clone alias lifetime differs.");
        }
        var callbacks = root.GetProperty("JuiceUpgradeCallbacks").EnumerateArray().ToArray();
        Require(callbacks.Length == 14 && callbacks.All(row => row.GetProperty("TargetId").GetInt32() != row.GetProperty("SourceId").GetInt32()),
            "Missing positive source-to-other-card callbacks.");
        foreach (var row in callbacks)
        {
            int id = row.GetProperty("SourceId").GetInt32(); var owner = owners[id];
            var input = row.GetProperty("Input").Deserialize<CardUpgradeModifier>()!;
            var expected = CardJuiceModel.OnCardBeingUpgraded(CardExhaustModel.RefreshTraits(owner).CombinedTraits!, id, id, input);
            Require(row.GetProperty("Returned").GetBoolean() && Snapshot(expected) == Snapshot(row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!),
                "Actual source callback/full upgrade differs.");
            if (id == ids[7])
            {
                var queue = row.GetProperty("QueueAfter").Deserialize<NextDrawUpgradeRule[]>()!;
                Require(queue.Length == 3 && queue[2].Upgrade.Statuses.Single().Stacks == 3 && input.AssetKey != "PojuJuiceSkippedScorch",
                    "Filter rejection did not short-circuit later source callbacks.");
            }
        }
        // A filter may encounter a card whose behavior is outside the captured
        // contract. Report an unsupported branch and preserve its parent.
        var context = rows[2].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        var target = context.FindCard(ids[3])!;
        var unsupported = new CardUpgradeModifier("unknown", "unknown", new CardStatModifier(damage: 1), Array.Empty<CombatStatus>(),
            false, false, false, 0, 0, Array.Empty<string>());
        context = context.WithCard(target.WithModifiers(target.Permanent, UnitModifierModel.Add(target.Temporary, unsupported), refreshTraits: false));
        var filtered = callbacks.Last().GetProperty("QueueAfter").Deserialize<NextDrawUpgradeRule[]>()![1];
        context = NextDrawUpgradeModel.Schedule(context, filtered);
        string parent = JsonSerializer.Serialize(context, ModelJson.Options);
        var rejected = NextDrawUpgradeModel.Apply(context, new[] { target.InstanceId }, ids[2], null, out string? error);
        Require(error != null && ReferenceEquals(context, rejected) && parent == JsonSerializer.Serialize(context, ModelJson.Options),
            "Unsupported filter input escaped as an exception or mutated its parent.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine("JUICE-UPGRADE-BATTLE PASS five original upgrades, six two-card draws, 14 source callbacks, deep-copy/filtered shared-alias/short-circuit lifetimes, complete native chain/16 branches/frontiers and serial/parallel root search.");
    }
}
