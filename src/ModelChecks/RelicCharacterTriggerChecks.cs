using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicCharacterTriggerChecks
{
    internal static void Run()
    {
        var baseTrigger = Gold("OnEquipmentAdded", 5).WithStateId(0);
        var actor = new CombatUnit(1, "carrier", CombatTeam.Player, 2, 20, 20, true, false, false, [],
            [baseTrigger], subtypes: ["angel"], equipmentCards: [], nextTriggerId: 1);
        var limit = new RelicConditionState(new("SubtypeInDeck"), true, 6, 1, false);
        var rule = new RelicCharacterTriggers(0, true, false, [Gold("OnEquipmentAdded", 11), Gold("OnShift", 17)],
            true, "angel", false, conditions: [limit]);
        var relic = new CombatRelicState("relic", "AddedTriggerRelic", [RelicModel.AddTrigger],
            isCovenant: false, disallowedInPlacementPhase: false, triggerAdditions: [rule]);
        var cache = new TriggerCountState([new("OnEquipmentAdded", 1), new("OnShift", 1)], [], [], []);
        var root = State(actor, relic, cache);
        string parent = Serialize(root);
        var applied = Add(root);
        Require(applied.Supported, applied.UnsupportedReason ?? "Installation failed.");
        var result = applied.State!.Units.Single();
        Require(result.Triggers.Select(trigger => trigger.StateId).SequenceEqual(new int?[] { 2, 1, 0 }) &&
            result.NextTriggerId == 3 && result.Triggers.Take(2).All(trigger => trigger.FireCount == 1 &&
                trigger.NoCountModifiersAllowed == true && trigger.Origin!.UpgradeId == "" && !trigger.Origin.IsFromEquipment) &&
            result.Triggers[2].FireCount == 2 && applied.PendingCallbacks.Count == 0 && applied.Events.Count == 0,
            "Prepend ordering, fresh IDs, count admission or installation side effects differ.");
        Require(applied.State.Context!.BattleRng.Equals(root.Context!.BattleRng) &&
            !applied.State.Context.Relics![0].TriggerAdditions![0].Conditions[0].Triggered,
            "Installation consumed RNG or notified a condition that native does not record.");
        var repeated = Add(applied.State);
        Require(repeated.Supported && repeated.State!.Units[0].NextTriggerId == 5 &&
            repeated.State.Units[0].Triggers.Select(trigger => trigger.StateId).SequenceEqual(new int?[] { 4, 3, 2, 1, 0 }),
            "A second birth callback reused an installed trigger or spent an unnotified condition.");
        var admitted = new RelicCharacterTriggers(0, true, false, [Gold("OnEquipmentAdded", 11)], false);
        var admittedRelic = new CombatRelicState("admitted", "Admitted", [RelicModel.AddTrigger], isCovenant: false,
            disallowedInPlacementPhase: false, triggerAdditions: [admitted]);
        Require(Add(State(actor, admittedRelic, cache)).State!.Units[0].Triggers[0].FireCount == 2,
            "An admitted relic trigger did not receive the native count modifier.");
        foreach (var excluded in new[]
        {
            new RelicCharacterTriggers(0, false, true, rule.Triggers, true),
            new RelicCharacterTriggers(0, true, false, rule.Triggers, true, "missing", false)
        })
        {
            var filtered = State(actor, new("filtered", "Filtered", [RelicModel.AddTrigger], isCovenant: false,
                disallowedInPlacementPhase: false, triggerAdditions: [excluded]), cache);
            Require(Serialize(Add(filtered).State) == Serialize(filtered), "A rejected team/subtype installed triggers or changed RNG.");
        }
        Require(Serialize(RelicSpawnStatusModel.CharacterAdded(root, 1, 0, true).State) == parent,
            "A non-covenant relic ran in the covenant birth phase.");
        var preview = new RoomCombatState(0, false, root.Units, [], root.Context, preview: true);
        Require(Serialize(Add(preview).State) == Serialize(preview), "Preview installed live relic triggers.");
        Require(!Add(State(actor, relic, null)).Supported, "Missing count metadata was guessed.");
        Parallel.For(0, 32, _ => Require(Serialize(Add(root).State) == Serialize(applied.State), "Parallel relic branches differ."));
        Require(Serialize(root) == parent, "Relic installation mutated the source branch.");
        Console.WriteLine("RELIC-CHARACTER-TRIGGER-CHECKS PASS: ordered fresh installs, count gates, unnotified conditions, team/subtype/covenant/preview filters and 32 immutable branches.");
    }
    private static CombatTrigger Gold(string kind, int amount) => new(kind, false, false, true, 1,
        [new("CardEffectRewardGold", amount, 0, "", 0, [], false)], false, noCountModifiersAllowed: false);
    private static RoomCombatState State(CombatUnit actor, CombatRelicState relic, TriggerCountState? cache) => new(0, false, [actor], [],
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: [relic], triggerCounts: cache));
    private static RoomCombatResult Add(RoomCombatState root) => RelicSpawnStatusModel.CharacterAdded(root, 1, 0, false);
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
}
