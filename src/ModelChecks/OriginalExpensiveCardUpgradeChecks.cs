using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalExpensiveCardUpgradeChecks
{
    private static RelicExpensiveCardUpgradeRule Rule(int minimum = 3)
    {
        var stats = new CardStatModifier();
        var upgrade = new CardUpgradeModifier(RelicExpensiveCardUpgradeModel.Effect, "ApplyDazedOnAscendOrDescend_Upgrade", stats, [], false, true, false, 0, 0, [],
            maskMetadata: new([], false, false, false, false), lifecycle: new(new(RelicExpensiveCardUpgradeModel.Effect, stats, [], false, false, false, false),
                "ApplyDazedOnAscendOrDescend_Upgrade", true, false, [new(RelicExpensiveCardUpgradeModel.Trait, RelicExpensiveCardUpgradeModel.Trait, 0, 0, true, 0)], [], false, [], [], []));
        return new("Invalid", minimum, true, upgrade, [new("CostGreaterThan3", minCost: 3)], []);
    }
    private static CombatRelicState Owner(RelicExpensiveCardUpgradeRule rule) => new(RelicExpensiveCardUpgradeModel.DataId, RelicExpensiveCardUpgradeModel.AssetKey,
        [RelicExpensiveCardUpgradeModel.Effect], isCovenant: false, disallowedInPlacementPhase: false,
        cardModifiers: [new(0, null, null, false, expensiveCardUpgrade: rule)]);
    private static CardInstanceState Card(int cost, bool x = false, string type = "Spell", int temporary = 0) => new(1, "expensive-card",
        CardModifiers.Empty(), new(new(cost: temporary), [], 0, []), 0, 0, 0, [], maskDescriptor: new(new("expensive-card", type, "Common", false, false, false,
            [], [], [], [], null, x, 0, 0, false, false, false), cost, [], []));
    internal static void Run()
    {
        var rule = Rule(); Require(RelicModel.Validate([Owner(rule)]) == null && RelicModel.Validate([Owner(Rule(2))]) != null, "Original expensive-card definition gate failed.");
        foreach (var spec in new[] { (2, false, "Spell", 0, false), (3, false, "Spell", 0, true), (0, true, "Spell", 0, true),
            (99, false, "Monster", 0, true), (3, false, "Equipment", -1, false), (2, false, "Junk", 1, true), (99, false, "Spell", -99, false) })
        {
            CardInstanceState source = Card(spec.Item1, spec.Item2, spec.Item3, spec.Item4); string parent = JsonSerializer.Serialize(source, ModelJson.Options);
            var changed = RelicExpensiveCardUpgradeModel.Refresh(source, rule, true, out bool returned);
            Require(returned == spec.Item5 && RelicExpensiveCardUpgradeModel.HasUpgrade(changed) == returned &&
                CardBranchMaskModel.Resolve(changed).Traits.Contains("CardTraitFreeze") == returned, "Threshold/X/type/full-reduction or trait-addition behavior differs.");
            var repeated = RelicExpensiveCardUpgradeModel.Refresh(changed, rule, true, out bool again);
            Equal(repeated, changed, "idempotent refresh"); Require(again == returned, "Idempotent return differs.");
            var preview = RelicExpensiveCardUpgradeModel.Refresh(source, rule, false, out _);
            Require(!CardBranchMaskModel.Resolve(preview).Traits.Contains("CardTraitFreeze"), "Preview must not notify Permafrost OnTraitAdded.");
            Parallel.For(0, 16, branch => Equal(RelicExpensiveCardUpgradeModel.Refresh(source, rule, true, out _), changed, "parallel refresh"));
            Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Expensive-card refresh mutated its parent.");
        }
        foreach (bool running in new[] { false, true })
        {
            CardInstanceState initial = Card(3);
            CombatContext manager = new(new([new(1, initial.DataId)], [], [], UnityRng.Seed(91), 0, []), UnityRng.Seed(92), 0, 2, 10, [],
                cardInstances: [initial], relics: [Owner(rule)], queryFrame: new(runningCombat: running));
            var applied = RelicCardModifierModel.Apply(initial, manager.Relics, false, manager);
            var added = RelicExpensiveCardUpgradeModel.Added(manager, 1);
            Require(applied.Supported && added.Supported && RelicExpensiveCardUpgradeModel.HasUpgrade(applied.Card!) &&
                CardBranchMaskModel.Resolve(applied.Card!).Traits.Contains("CardTraitFreeze") == running &&
                CardBranchMaskModel.Resolve(added.Context.FindCard(1)!).Traits.Contains("CardTraitFreeze") == running,
                "Manager and hand-addition trait notifications must honor the native running-combat gate.");
            CombatContext missing = manager.WithQueryFrame(null);
            Require(!RelicCardModifierModel.Apply(initial, missing.Relics, false, missing).Supported &&
                !RelicExpensiveCardUpgradeModel.Added(missing, 1).Supported, "Missing running-combat input must be rejected.");
        }
        CardInstanceState expensive = RelicExpensiveCardUpgradeModel.Refresh(Card(3), rule, true, out _);
        CardInstanceState cheaper = expensive.WithModifiers(new(new(cost: -1), [], 0, []), expensive.Temporary);
        cheaper = RelicExpensiveCardUpgradeModel.Refresh(cheaper, rule, true, out bool removalReturned);
        Require(removalReturned && !RelicExpensiveCardUpgradeModel.HasUpgrade(cheaper) && CardBranchMaskModel.Resolve(cheaper).Traits.Contains("CardTraitFreeze"),
            "Cost decrease must remove the source upgrade while retaining temporary Freeze.");
        var played = RelicFreezeHandUpgradeModel.Played(expensive);
        Require(!CardBranchMaskModel.Resolve(played).Traits.Contains("CardTraitFreeze") && RelicExpensiveCardUpgradeModel.HasUpgrade(played), "Playing must clear Freeze while retaining Permafrost.");
        CombatContext context = new(new([new(1, played.DataId)], [], [], UnityRng.Seed(91), 0, []), UnityRng.Seed(92), 0, 2, 10, [], cardInstances: [played], relics: [Owner(rule)]);
        var redrawn = RelicExpensiveCardUpgradeModel.Drawn(context, [1], out string? error);
        Require(error == null && CardBranchMaskModel.Resolve(redrawn.FindCard(1)!).Traits.Contains("CardTraitFreeze") && redrawn.BattleRng.Equals(context.BattleRng), "Redraw must restore Freeze without RNG.");
        Console.WriteLine("EXPENSIVE-CARD-UPGRADE-PURE PASS: exact 2/3 threshold, X/full/temporary costs, all card types, unique refresh, preview, running-combat and missing-input gates, residual Freeze after removal, play/redraw and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 235 && root.GetProperty("OriginalExpensiveCardUpgradePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Permafrost battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalExpensiveCardUpgradeCaptureFailures", "OriginalExpensiveCardUpgradeMismatches",
            "OriginalExpensiveCardUpgradeUnsupported", "OriginalExpensiveCardUpgradePending" }) Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int additions = 0, removals = 0, redraws = 0, queries = 0, previews = 0; var kinds = new HashSet<string>();
        foreach (var row in root.GetProperty("RelicExpensiveCardUpgradeRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            string kind = row.GetProperty("Kind").GetString()!; kinds.Add(kind); int id = row.GetProperty("CardId").GetInt32();
            CardInstanceState card = before.Context!.FindCard(id)!; CardInstanceState after = actual.Context!.FindCard(id)!;
            CombatRelicState owner = before.Context.Relics!.Single(owner => owner.DataId == RelicExpensiveCardUpgradeModel.DataId);
            Require(RelicModel.Validate([owner]) == null, "Changed Permafrost owner."); var rule = owner.CardModifiers!.Single().ExpensiveCardUpgrade!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                CardInstanceState predicted; bool? returned = null;
                if (kind == "Has") { predicted = card; returned = RelicExpensiveCardUpgradeModel.Interested(card, rule) && RelicExpensiveCardUpgradeModel.HasUpgrade(card); }
                else if (kind == "TraitDrawn") predicted = RelicExpensiveCardUpgradeModel.Freeze(card);
                else { predicted = RelicExpensiveCardUpgradeModel.Refresh(card, rule, kind != "Preview", out bool value); if (kind != "Permanent") returned = value; }
                Require(returned == row.GetProperty("Returned").Deserialize<bool?>(), "Native return differs.");
                Equal(CardSpellModel.WithContext(before, before.Context.WithCard(predicted)), actual, "native " + kind);
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native replay mutated its parent.");
            bool had = RelicExpensiveCardUpgradeModel.HasUpgrade(card), has = RelicExpensiveCardUpgradeModel.HasUpgrade(after);
            if (!had && has) additions++;
            if (had && !has) { removals++; Require(CardBranchMaskModel.Resolve(after).Traits.Contains("CardTraitFreeze"), "Removal unexpectedly cleared residual Freeze."); }
            if (kind == "TraitDrawn" && !CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitFreeze")) redraws++;
            if (kind == "Has") queries++; if (kind == "Preview") previews++;
        }
        Require(additions >= 3 && removals >= 1 && redraws >= 1 && queries >= 6 && previews == 2 &&
            new[] { "Apply", "Permanent", "Added", "Has", "Preview", "TraitDrawn" }.All(kinds.Contains), "Missing threshold/reset, callbacks, preview or natural redraw evidence.");
        int[] carriers = root.GetProperty("OriginalExpensiveCardUpgrade").GetProperty("Carriers").Deserialize<int[]>()!;
        int played = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32(); if (id != carriers[0] && id != carriers[2]) continue;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!; var card = actual.Spawn.Train.Context!.FindCard(id)!;
            Require(card.PlayCount == 1 && RelicExpensiveCardUpgradeModel.HasUpgrade(card) && !CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitFreeze"), "Played Permafrost card did not clear Freeze independently."); played++;
        }
        Require(played == 2 && root.GetProperty("Actions").GetArrayLength() == 4, "Missing ordinary and X natural plays.");
        int retainedTurns = 0;
        foreach (var row in root.GetProperty("Turns").EnumerateArray())
        {
            if ((RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() == RoomOutcome.BattleWon) continue;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = actual.Spawn.Train.Context!;
            Require(context.Cards.Hand.Any(card => card.InstanceId == carriers[3]) &&
                CardBranchMaskModel.Resolve(context.FindCard(carriers[3])!).Traits.Contains("CardTraitFreeze"), "Unplayed four-cost Permafrost carrier was not retained.");
            retainedTurns++;
        }
        Require(retainedTurns == 4, "Missing four natural retained-hand endpoints.");
        RelicCardModifierChecks.Native(root, required: true);
        Console.WriteLine($"ORIGINAL-EXPENSIVE-CARD-UPGRADE-NATIVE PASS: {additions} additions, {removals} cost removals with residual Freeze, {redraws} natural re-freezes, {queries} queries, two previews, all six callback kinds and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string name)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, name + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
