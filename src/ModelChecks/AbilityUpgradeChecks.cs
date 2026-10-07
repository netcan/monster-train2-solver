using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityUpgradeChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(85);
        CombatStatus[] statuses = [new("unit_ability", 1, hidden: true, displayCategory: "Persistent"),
            new("cooldown", 2, stackable: true, hidden: true, displayCategory: "Persistent"),
            new("unit_ability_available", 1, hidden: true, displayCategory: "Persistent")];
        var card = new CardInstanceState(1, "unit", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, []);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 2, 10, statuses,
            cardInstances: [card], cardRegistry: [card], abilityCardCache: [], permanentlyDisabledAbilities: []);
        UnitAbilityDefinition Definition(string id, int cooldown) => new(id, true, cooldown, 2, new(id, CardModifiers.Empty(), [], []));
        var a = Definition("a", 3); var b = Definition("b", 5); var c = Definition("c", 7);
        CombatTrigger[] common = new[] { "OnOwnAbilityActivated", "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" }
            .Select((kind, index) => new CombatTrigger(kind, false, false, true, 1, [], skipDuringDeployment: false,
                origin: new("UnitAbilityCommonData", 0, false, false), stateId: index)).ToArray();
        CardUpgradeModifier Upgrade(string id, UnitAbilityDefinition definition, bool keep = false, IReadOnlyList<CombatTrigger>? triggers = null) =>
            new(id, "", new(), [], false, false, false, 0, 0, [], triggerUpgrades: triggers,
                abilityUpgrade: new(definition, false, false, common), doNotReplaceExistingAbility: keep);
        var unit = new CombatUnit(1, "unit", CombatTeam.Player, 1, 20, 20, true, false, false,
            statuses.Take(2).ToArray(), common, spawnerCardId: 1, modifiers: new(1, 0, 0, 1, 2, true, false, []),
            statusRegistry: statuses.Take(2).ToArray(), nextTriggerId: 3,
            ability: new("a", 9, 2, cardCreation: a.CardCreation, definition: a),
            statusDictionary: new(["unit_ability", "cooldown"], []), abilityRules: new(false, []));
        var root = new RoomCombatState(0, false, [unit], [], context);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        CardInstanceState SpawnCard(IReadOnlyList<CardUpgradeModifier> permanent, IReadOnlyList<CardUpgradeModifier> temporary) =>
            new(1, "unit", new(new(), permanent, 0, []), new(new(), temporary, 0, []), 0, 0, 0, []);
        CombatUnit Initial(CardInstanceState source, CombatContext? ctx = null)
        {
            var initialized = AbilityLifecycleModel.InitialAtSpawn(unit, source, ctx ?? context, out string? error);
            Require(error == null, "Initial ability unsupported: " + error); return initialized;
        }
        Require(Initial(SpawnCard([Upgrade("b", b)], [Upgrade("c", c)])).Ability?.DataId == "c", "Temporary ability did not override permanent ability.");
        Require(Initial(SpawnCard([Upgrade("b", b, true)], [])).Ability!.Cooldown == 9,
            "Do-not-replace discarded the existing initial ability.");
        var disabled = context.WithPermanentlyDisabledAbilities(["b"]);
        Require(Initial(SpawnCard([Upgrade("b", b)], []), disabled).Ability == null &&
            Initial(SpawnCard([Upgrade("b", b)], []), disabled).Triggers.All(trigger => trigger.Origin?.UpgradeId != "UnitAbilityCommonData"),
            "Disabled selected initial ability fell back to the base ability.");
        Require(Initial(SpawnCard([Upgrade("b", b, true)], []), context.WithPermanentlyDisabledAbilities(["a"])).Ability == null,
            "Permanent suppression ran before do-not-replace selection.");
        var overlayResult = AbilityLifecycleModel.Apply(root, 1, new(b, true, false, common), remove: false);
        Require(overlayResult.Supported, "Equipment overlay unsupported: " + overlayResult.UnsupportedReason);
        var overlay = overlayResult.State!;
        var direct = UnitModifierModel.ApplyDirect(overlay, 1, Upgrade("direct", c));
        Require(direct.Supported && direct.State!.Units[0].Ability is { DataId: "c", FromEquipment: false, PreviousDataId: null },
            "A direct ability upgrade retained equipment restoration history.");
        var removed = UnitModifierModel.ApplyDirect(direct.State!, 1, Upgrade("direct", c), true);
        Require(removed.Supported && removed.State!.Units[0].Ability == null, "Matching ability upgrade removal did not remove the current skill.");
        var keep = UnitModifierModel.ApplyDirect(overlay, 1, Upgrade("keep", c, true));
        var unmatch = UnitModifierModel.ApplyDirect(keep.State!, 1, Upgrade("keep", c, true), true);
        Require(keep.Supported && unmatch.Supported && unmatch.State!.Units[0].Ability is { DataId: "b", FromEquipment: true, PreviousDataId: "a" },
            "Do-not-replace or nonmatching removal lost the equipment ability.");
        var gold = new CombatTrigger("OnUnitAbilityAvailable", false, false, true, 1,
            [new CombatEffect("CardEffectRewardGold", 5, 0, "", 0, [], false)], skipDuringDeployment: false);
        var granted = UnitModifierModel.ApplyDirect(root, 1, Upgrade("with-trigger", b, triggers: [gold]));
        Require(granted.Supported && granted.State!.Context!.Gold == 5 && granted.State.Units[0].Triggers
            .Any(trigger => trigger.Origin?.UpgradeId == "" && trigger.Kind == gold.Kind && trigger.HasTriggered),
            "New upgrade triggers were unavailable during quiet lifecycle removal callbacks.");
        var descriptor = Upgrade("copies", c, true);
        Require(descriptor.WithEquipmentSource(8, 1).AbilityUpgrade == descriptor.AbilityUpgrade &&
            descriptor.WithScaledStats(2, 3).DoNotReplaceExistingAbility && descriptor.RefreshCloneMagicPower().AbilityUpgrade == descriptor.AbilityUpgrade,
            "Equipment, scaling or clone copies dropped ability upgrade metadata.");
        string expected = JsonSerializer.Serialize(direct.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(UnitModifierModel.ApplyDirect(overlay, 1, Upgrade("direct", c)).State,
            ModelJson.Options) == expected, "Parallel ability-upgrade branches differ."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Ability upgrade mutated its parent.");
        Console.WriteLine("ABILITY-UPGRADE-CHECKS PASS: ordered initial selection, post-selection suppression, upgrade trigger ordering, live matching/keep gates, metadata copies and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var modifier) || modifier.GetString() != "equipment-abilities") return;
        const string b = "c2f6ed7f-18ce-4070-b65f-7dd9f5160074", c = "c2f6ed7f-18ce-4070-b65f-7dd9f5160075";
        var initial = fixture.GetProperty("InitialAbilitySpawns").EnumerateArray().Single();
        VerifyInitial(initial);
        var born = initial.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var initialAction = initial.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var initialSource = initial.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.FindCard(initialAction.CardInstanceId)!;
        Require(initialSource.Permanent.Upgrades.Any(upgrade => upgrade.AbilityUpgrade?.Definition?.DataId == b) &&
            initialSource.Temporary.Upgrades.Any(upgrade => upgrade.AbilityUpgrade?.Definition?.DataId == c),
            "Native initial spawn lacks both the permanent and overriding temporary ability upgrades.");
        Require(born.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == initialAction.CardInstanceId)
            .Ability is { DataId: c, Cooldown: 6, CooldownAtSpawn: 2, FromEquipment: false }, "Native temporary initial skill selection missing.");
        var equipment = fixture.GetProperty("EquipmentOperations").EnumerateArray().ToArray();
        CombatUnit Host(FixtureValue item, string phase) => item.GetProperty(phase).Deserialize<RoomCombatState>()!.Units
            .Single(unit => unit.Id == item.GetProperty("UnitId").GetInt32());
        Require(equipment.Any(item => !item.GetProperty("Remove").GetBoolean() && Host(item, "After").Ability is
            { DataId: b, FromEquipment: true, PreviousDataId: c }) &&
            equipment.Any(item => !item.GetProperty("Remove").GetBoolean() && Host(item, "After").Ability is
            { DataId: c, FromEquipment: true, PreviousDataId: c }), "Native repeated equipment replacement lost the initial raw ability.");
        Require(equipment.Any(item => item.GetProperty("DeferAbilityCallbacks").GetBoolean()) &&
            equipment.Any(item => !item.GetProperty("DeferAbilityCallbacks").GetBoolean()), "Quiet and played-card equipment boundaries missing.");
        Require(equipment.Any(item => item.GetProperty("Remove").GetBoolean() && Host(item, "Before").Ability is
            { DataId: b, FromEquipment: true, PreviousDataId: c } && Host(item, "After").Ability is
            { DataId: c, Cooldown: 6, FromEquipment: false, PreviousDataId: null } && Host(item, "After").Status("cooldown")?.Stacks == 6),
            "Equipment removal never restored the original raw activation cooldown.");
        var direct = fixture.GetProperty("DirectUnitUpgrades").EnumerateArray().ToDictionary(item => item.GetProperty("Label").GetString()!);
        Require(direct.Count == 7 && Host(direct["ability-keep-existing"], "After").Ability is { DataId: b, FromEquipment: true } &&
            Host(direct["ability-direct-clears-equipment-history"], "After").Ability is { DataId: c, FromEquipment: false, PreviousDataId: null } &&
            Host(direct["ability-remove-current"], "After").Ability == null &&
            Host(direct["ability-explicit-reassign-disabled"], "After").Ability?.DataId == c,
            "Native direct upgrade lifecycle coverage differs.");
        var actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Any(item =>
        {
            var before = item.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = item.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int cardId = item.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var source = before.Spawn.Train.Context!.FindCard(cardId);
            var original = before.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == source?.DataId)?.SpawnUnit?.Ability;
            return original?.HasAbility == true && source!.Permanent.Upgrades.Any(upgrade => upgrade.DoNotReplaceExistingAbility &&
                upgrade.AbilityUpgrade?.Definition?.DataId == b) && after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id >= before.Spawn.NextUnitId && unit.SpawnerCardId == cardId && unit.Ability?.DataId == original.DataId);
        }), "No native do-not-replace initial upgrade retained the base skill.");
        Require(actions.Any(item =>
        {
            var before = item.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = item.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int cardId = item.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var source = before.Spawn.Train.Context!.FindCard(cardId);
            return source != null && before.PlayRules!.Cards.Any(rule => rule.DataId == source.DataId && rule.SpawnUnit?.Ability?.HasAbility == true) &&
                source.Permanent.Upgrades.Any(upgrade => upgrade.AbilityUpgrade?.Definition?.DataId == c) &&
                before.Spawn.Train.Context.PermanentlyDisabledAbilities!.Contains(c) && after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id >= before.Spawn.NextUnitId && unit.SpawnerCardId == cardId && unit.Ability == null &&
                        unit.Triggers.All(trigger => trigger.Origin?.UpgradeId != "UnitAbilityCommonData"));
        }), "No native upgraded skill birth was suppressed without falling back to the base ability.");
        Require(actions.Any(item =>
        {
            var action = item.GetProperty("Action").Deserialize<PlayCardAction>()!;
            return action.ActivatorUnitId > 0 && item.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms
                .SelectMany(room => room.Units).Any(unit => unit.Id == action.ActivatorUnitId && unit.Ability?.FromEquipment == true);
        }), "The equipment skill was never activated through a real native action.");
        var callbacks = fixture.GetProperty("CharacterCallbackFires").EnumerateArray().ToArray();
        Require(new[] { "OnPreOwnAbilityActivated", "OnOwnAbilityActivated", "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" }
            .All(kind => callbacks.Any(item => item.GetProperty("Kind").GetString() == kind)), "Native ability-upgrade callback kinds missing.");
        foreach (var callback in callbacks) AbilityEffectChecks.VerifyCallback(callback);
        Parallel.For(0, 32, _ => { VerifyInitial(initial); foreach (var callback in callbacks) AbilityEffectChecks.VerifyCallback(callback); });
        Console.WriteLine($"NATIVE-ABILITY-UPGRADE-CHECKS PASS: initial temporary selection, disabled upgraded births, equipment skill activation and {callbacks.Length} exact queued callbacks in 32 branches.");
    }
    private static void VerifyInitial(FixtureValue initial)
    {
        var before = initial.GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = BattleActionModel.PlayCard(before, initial.GetProperty("Action").Deserialize<PlayCardAction>()!);
        Require(result.Supported, "Initial upgraded spawn unsupported: " + result.Reason);
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!),
            BattleTurnChecks.Comparable(initial.GetProperty("Actual").Deserialize<BattleTurnState>()!));
        Require(difference == null, "Initial upgraded spawn differs: " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Initial spawn mutated its native root.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
