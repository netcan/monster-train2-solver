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
        var boss = new CombatUnit(2, "Boss_Level1_Basic", CombatTeam.Enemy, 2, 20, 20, true, false, false, [], [],
            equipmentCards: [], nextTriggerId: 0);
        var bossRule = new RelicCharacterTriggers(0, true, false, [Gold("OnSpawn", 13)], true,
            characterAssetKeys: ["Boss_Level1_Basic"]);
        var bossRelic = new CombatRelicState("covenant", "Covenant04_StrongerBosses", [RelicModel.AddTrigger],
            isCovenant: true, disallowedInPlacementPhase: false, triggerAdditions: [bossRule]);
        var matchedBoss = RelicSpawnStatusModel.CharacterAdded(State(boss, bossRelic, cache), 2, 0, true);
        Require(matchedBoss.Supported && matchedBoss.State!.Units.Single().Triggers.Single().Kind == "OnSpawn",
            "A character-definition filter did not override the source-team mask for its matching Boss.");
        var wrongBoss = new CombatUnit(2, "Boss_Level1_Alternate", CombatTeam.Enemy, 2, 20, 20, true, false, false, [], [],
            equipmentCards: [], nextTriggerId: 0);
        var wrongRoot = State(wrongBoss, bossRelic, cache);
        Require(Serialize(RelicSpawnStatusModel.CharacterAdded(wrongRoot, 2, 0, true).State) == Serialize(wrongRoot),
            "A nonmatching character definition passed the native filter.");
        Require(Serialize(RelicSpawnStatusModel.CharacterAdded(root, 1, 0, true).State) == parent,
            "A non-covenant relic ran in the covenant birth phase.");
        var preview = new RoomCombatState(0, false, root.Units, [], root.Context, preview: true);
        Require(Serialize(Add(preview).State) == Serialize(preview), "Preview installed live relic triggers.");
        Require(!Add(State(actor, relic, null)).Supported, "Missing count metadata was guessed.");

        var allTeams = new RelicCharacterTriggers(0, true, true, [Gold("OnDeath", 1)], true);
        var bothTeamsRelic = new CombatRelicState("both-teams", "ExtinguishSpreadPyregel", [RelicModel.AddTrigger],
            isCovenant: false, disallowedInPlacementPhase: false, triggerAdditions: [allTeams]);
        var bothTeamsCache = new TriggerCountState([new("OnDeath", 1)], [], [], []);
        foreach (var candidate in new[]
        {
            new CombatUnit(1, "monster", CombatTeam.Player, 0, 20, 20, false, false, false, [], nextTriggerId: 0),
            new CombatUnit(1, "hero", CombatTeam.Enemy, 0, 20, 20, false, false, false, [], nextTriggerId: 0)
        })
        {
            var installed = RelicSpawnStatusModel.CharacterAdded(State(candidate, bothTeamsRelic, bothTeamsCache), 1, 0, false);
            Require(installed.Supported && installed.State!.Units[0].Triggers.Single().Kind == "OnDeath",
                "The native team-union gate omitted an eligible hero or monster.");
        }
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 0, 20, 20, false, true, true, [], nextTriggerId: 0);
        var pyreRoot = State(pyre, bothTeamsRelic, bothTeamsCache);
        Require(Serialize(Add(pyreRoot).State) == Serialize(pyreRoot), "The no-subtype relic installed its trigger on the Pyre.");
        Parallel.For(0, 32, _ => Require(Serialize(Add(root).State) == Serialize(applied.State), "Parallel relic branches differ."));
        Require(Serialize(root) == parent, "Relic installation mutated the source branch.");
        Console.WriteLine("RELIC-CHARACTER-TRIGGER-CHECKS PASS: ordered fresh installs, count gates, team/subtype/definition/covenant/preview filters and 32 immutable branches.");
    }
    private static CombatTrigger Gold(string kind, int amount) => new(kind, false, false, true, 1,
        [new("CardEffectRewardGold", amount, 0, "", 0, [], false)], false, noCountModifiersAllowed: false);
    private static RoomCombatState State(CombatUnit actor, CombatRelicState relic, TriggerCountState? cache) => new(0, false, [actor], [],
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: [relic], triggerCounts: cache));
    private static RoomCombatResult Add(RoomCombatState root) => RelicSpawnStatusModel.CharacterAdded(root, 1, 0, false);
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
}
