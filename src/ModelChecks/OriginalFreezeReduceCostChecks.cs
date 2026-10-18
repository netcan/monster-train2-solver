using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFreezeReduceCostChecks
{
    private static CardUpgradeModifier Upgrade(int cost = -99) => new(RelicFreezeHandUpgradeModel.UpgradeId,
        "ReduceCostOnFreeze", new(cost: cost), [], true, false, false, 0, 0, [],
        maskMetadata: new([], false, false, false, false));
    private static CombatRelicState Owner(int cost = -99, string[]? traits = null) => new(RelicFreezeHandUpgradeModel.DataId,
        RelicFreezeHandUpgradeModel.AssetKey, [RelicFreezeHandUpgradeModel.Effect], isCovenant: false,
        disallowedInPlacementPhase: true, cardModifiers: [new(0, null, null, false,
            freezeHandUpgrade: new(traits ?? ["CardTraitFreeze"], true, false, 0, 0, Upgrade(cost)))]);
    private static CardInstanceState Card(int id, bool frozen) => new(id, "freeze-" + id, CardModifiers.Empty(), CardModifiers.Empty(),
        0, 0, 0, [], maskDescriptor: new(new("freeze-" + id, "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false), 3, [], [],
            temporaryTraits: frozen ? [new("CardTraitFreeze", "CardTraitFreeze", 0, 0, true, 0)] : []));
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(2771);
        CombatContext source = new(new([new(1, "freeze-1"), new(2, "freeze-2")], [new(3, "freeze-3")], [], rng, 0, []),
            rng, 0, 4, 10, cardInstances: [Card(1, true), Card(2, false), Card(3, true)], relics: [Owner()]);
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        foreach (var gate in new[] { (1, false, false), (0, true, false), (0, false, true) })
        {
            var skipped = RelicFreezeHandUpgradeModel.Apply(source, gate.Item1, gate.Item2, gate.Item3);
            Require(skipped.Supported && JsonSerializer.Serialize(skipped.Context, ModelJson.Options) == parent,
                "Wrong-room, deployment or preview gate changed the hand.");
        }
        var applied = RelicFreezeHandUpgradeModel.Apply(source, 0, false, false);
        Require(applied.Supported && applied.Context.FindCard(1)!.Temporary.Upgrades.Count == 1 &&
            CardBranchMaskModel.Resolve(applied.Context.FindCard(1)!).CostWithoutTraits == 0 &&
            applied.Context.FindCard(2)!.Temporary.Upgrades.Count == 0 && applied.Context.FindCard(3)!.Temporary.Upgrades.Count == 0 &&
            applied.Context.BattleRng.Equals(rng), "Freeze filter, hand scope, cost clamp or RNG differs.");
        var repeated = RelicFreezeHandUpgradeModel.Apply(applied.Context, 0, false, false);
        Require(repeated.Supported && repeated.Context.FindCard(1)!.Temporary.Upgrades.Count == 2 &&
            CardBranchMaskModel.Resolve(repeated.Context.FindCard(1)!).CostWithoutTraits == 0, "Non-unique retained upgrades must accumulate.");
        CardInstanceState discarded = repeated.Context.FindCard(1)!.OnDiscard(false);
        Require(discarded.Temporary.Upgrades.Count == 0 && CardBranchMaskModel.Resolve(discarded).Traits.Contains("CardTraitFreeze"),
            "An ordinary discard must clear removable upgrades without consuming the frozen trait.");
        CardInstanceState played = RelicFreezeHandUpgradeModel.Played(repeated.Context.FindCard(1)!).OnDiscard(true, 0);
        Require(played.MaskDescriptor!.TemporaryTraits.Count == 0 && played.Temporary.Upgrades.Count == 0 &&
            CardBranchMaskModel.Resolve(played).CostWithoutTraits == 3, "Playing must thaw the temporary trait and clear removable upgrades on discard.");
        Require(RelicModel.Validate([Owner(cost: -98)]) != null && RelicModel.Validate([Owner(traits: [])]) != null,
            "Changed original payload was accepted.");
        Parallel.For(0, 16, _ => Require(RelicFreezeHandUpgradeModel.Apply(source, 0, false, false).Context.FindCard(1)!.Temporary.Upgrades.Count == 1,
            "Parallel freeze upgrade diverged."));
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Freeze hand upgrade mutated its parent.");
        Console.WriteLine("FREEZE-COST-PURE PASS: room/deployment/preview gates, frozen hand filtering, non-unique stacking, zero cost clamp, discard cleanup, thawing and immutable RNG branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 228 && root.GetProperty("OriginalFreezeCostSchema").GetInt32() == 228 &&
            root.GetProperty("ModifierScenario").GetString() == "original-freeze-reduce-cost" && root.GetProperty("OriginalFreezeCostPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalFreezeCostCaptureFailures",
                "OriginalFreezeCostMismatches", "OriginalFreezeCostUnsupported", "OriginalFreezeCostPending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Native freeze-cost acceptance failed.");
        int[] spells = root.GetProperty("OriginalFreezeCost").GetProperty("SpellCards").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(spells.Length == 4 && actions.Length == 3 && actions.All(row => row.GetProperty("ScenarioAction").GetBoolean()) &&
            actions[^1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == spells[0], "Missing deployments or discounted frozen-card play.");
        int tests = 0;
        foreach (FixtureValue row in root.GetProperty("RelicFreezeCostTests").EnumerateArray())
        {
            Require(row.GetProperty("Result").GetBoolean(), "Native end-of-turn Test must always return true.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test state/RNG"); tests++;
        }
        int positiveRooms = 0, gatedRooms = 0, added = 0, largest = 0;
        foreach (FixtureValue row in root.GetProperty("RelicFreezeCostApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int room = row.GetProperty("Room").GetInt32();
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native freeze-cost coroutine.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatResult result = RelicFreezeHandUpgradeModel.ApplySelected(before, room, false);
                Require(result.Supported, "Freeze hand upgrade unsupported: " + result.UnsupportedReason);
                Equal(result.State, actual, "raw application");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(actual.Context!.BattleRng.Equals(before.Context!.BattleRng) && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Native freeze upgrade changed RNG or mutated its replay root.");
            if (room != 0) { gatedRooms++; Equal(before, actual, "nonzero floor"); continue; }
            positiveRooms++;
            foreach (CardToken token in before.Context.Cards.Hand)
            {
                CardInstanceState old = before.Context.FindCard(token.InstanceId)!, changed = actual.Context.FindCard(token.InstanceId)!;
                bool frozen = CardBranchMaskModel.Resolve(old).Traits.Contains("CardTraitFreeze");
                Require(changed.Temporary.Upgrades.Count == old.Temporary.Upgrades.Count + (frozen ? 1 : 0), "Frozen trait filtering differs.");
                if (frozen) { added++; largest = Math.Max(largest, changed.Temporary.Upgrades.Count);
                    Require(CardBranchMaskModel.Resolve(changed).CostWithoutTraits == 0, "Frozen card did not become free."); }
            }
            Require(actual.Context.FindCard(spells[2])!.Temporary.Upgrades.Count == 0 &&
                actual.Context.FindCard(spells[3])!.Temporary.Upgrades.Count == 0, "Unfrozen controls received a discount.");
        }
        BattleTurnState afterPlay = actions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardInstanceState thawed = afterPlay.Spawn.Train.Context!.FindCard(spells[0])!;
        Require(thawed.Temporary.Upgrades.Count == 0 && !CardBranchMaskModel.Resolve(thawed).Traits.Contains("CardTraitFreeze") &&
            tests == positiveRooms + gatedRooms && positiveRooms >= 2 && gatedRooms > positiveRooms && added >= 3 && largest >= 2,
            "Missing repeated accumulation, floor controls or played-card thaw/discard cleanup.");
        Console.WriteLine($"ORIGINAL-FREEZE-COST-NATIVE PASS: {tests} unchanged Tests, {positiveRooms} active floor-zero callbacks, {gatedRooms} gated floors, {added} upgrades, maximum stack {largest}, thaw/discard cleanup and immutable replay.");
    }
    private static void Equal<T>(T a, T b, string label)
    {
        string x = JsonSerializer.Serialize(a, ModelJson.Options), y = JsonSerializer.Serialize(b, ModelJson.Options);
        Require(x == y, "Freeze-cost " + label + " differs: " + ModelJson.Difference(x, y));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
