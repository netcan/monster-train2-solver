using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityEffectChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(73);
        CombatStatus[] statuses = [new("unit_ability", 1, hidden: true, displayCategory: "Persistent"),
            new("cooldown", 2, stackable: true, hidden: true, displayCategory: "Persistent"),
            new("unit_ability_available", 1, hidden: true, displayCategory: "Persistent")];
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, statuses,
            cardInstances: [], cardRegistry: [], abilityCardCache: [], permanentlyDisabledAbilities: []);
        UnitAbilityDefinition Definition(string id) => new(id, true, 3, 2, new(id, CardModifiers.Empty(), [], []));
        var a = Definition("a"); var b = Definition("b"); var c = Definition("c");
        CombatTrigger[] common = new[] { "OnOwnAbilityActivated", "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" }
            .Select((kind, index) => new CombatTrigger(kind, false, false, true, 1, [], skipDuringDeployment: false,
                origin: new("UnitAbilityCommonData", 0, false, false), stateId: index)).ToArray();
        CombatUnit Unit(int id, UnitAbilityDefinition definition) => new(id, "unit", CombatTeam.Player, 1, 20, 20, true, false, false,
            statuses.Take(2).ToArray(), common, statusRegistry: statuses.Take(2).ToArray(), nextTriggerId: 3,
            ability: new(definition.DataId, 3, 2, cardCreation: definition.CardCreation, definition: definition),
            statusDictionary: new(["unit_ability", "cooldown"], []), abilityRules: new(false, []));
        var root = new RoomCombatState(0, false, [Unit(1, a), Unit(2, b)], [], context);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        CardActionEffect Change(UnitAbilityDefinition? definition, string target = "Room") => new("SetUnitAbility", target, 19, false, true, [],
            range: new(17, 29), abilityChange: new(definition, false, false, common));
        CardActionEffect Remove(bool blocked = false) => new("RemoveAbility", "Room", 23, false, true, [],
            range: new(17, 29), abilityChange: new(null, false, true, common, blocked));
        var removed = CardSpellModel.Apply(root, [Remove()], 0);
        Require(removed.Supported && removed.State!.Units.All(unit => unit.Ability == null) &&
            removed.State.Context!.PermanentlyDisabledAbilities!.SequenceEqual(["b", "a"]),
            "Multi-target removal lost reverse target order or permanent IDs.");
        Require(removed.State!.Context!.BattleRng.Equals(rng), "Unused lifecycle quantities consumed RNG.");
        var blocked = CardSpellModel.Apply(root, [Remove(true)], 0);
        Require(blocked.Supported && JsonSerializer.Serialize(blocked.State, ModelJson.Options) == parent,
            "Active relic gate changed an ability or consumed RNG.");
        var empty = new RoomCombatState(0, false, [], [], context);
        Require(CardSpellModel.TestPlay(empty, [Change(c)], 0).CanPlay && CardSpellModel.Apply(empty, [Change(c)], 0).Supported,
            "The native base test must allow empty lifecycle target sets.");
        var nullAssignment = CardSpellModel.Apply(root, [Change(null)], 0);
        Require(nullAssignment.Supported && JsonSerializer.Serialize(nullAssignment.State, ModelJson.Options) == parent,
            "Null ability assignment changed state.");
        var assigned = CardSpellModel.Apply(root, [Change(c)], 0);
        Require(assigned.Supported && assigned.State!.Units.All(unit => unit.Ability?.DataId == "c") &&
            assigned.State.Context!.AbilityCardCache!.Count == 1 && assigned.State.Context.BattleRng.Equals(rng),
            "Composed assignment lost the shared cache or sampled an unused quantity.");
        var bad = new CardActionEffect("SetUnitAbility", "Room", 0, false, true, []);
        Require(!CardSpellModel.Apply(root, [bad], 0).Supported, "Missing lifecycle definitions were silently accepted.");
        var effect = new CombatEffect("CardEffectSetUnitAbility", 0, 0, "", 0, [], false, action: Change(c, "Self"));
        var trigger = new CombatTrigger("OnPreOwnAbilityActivated", false, false, true, 1, [effect], skipDuringDeployment: false, stateId: 3);
        CombatUnit actor = Unit(1, a);
        actor = new CombatUnit(actor.Id, actor.AssetKey, actor.Team, actor.Attack, actor.Health, actor.MaxHealth, true, false, false,
            actor.Statuses, actor.Triggers.Concat([trigger]).ToArray(), statusRegistry: actor.StatusRegistry,
            nextTriggerId: 4, ability: actor.Ability, statusDictionary: actor.StatusDictionary, abilityRules: actor.AbilityRules);
        var queuedRoot = new RoomCombatState(0, false, [actor], [], context);
        var callback = new RoomCombatModel.QueuedCharacterTrigger(0, actor, trigger.Kind);
        var generated = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var queuedResult = RoomCombatModel.ApplyQueuedCharacterTrigger(queuedRoot, callback, generated.Add);
        Require(queuedResult.Supported && callback.Unit.Ability?.DataId == "c" && generated.Any(item => item.Kind == "OnUnitAbilityAvailable") &&
            callback.Unit.Triggers.Where(item => item.Origin?.UpgradeId == "UnitAbilityCommonData").All(item => !item.HasTriggered),
            "Running-queue assignment eagerly dispatched availability callbacks.");
        string expected = JsonSerializer.Serialize(assigned.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, [Change(c)], 0).State,
            ModelJson.Options) == expected, "Parallel assignment branches differ."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Ability effects mutated the parent.");
        Console.WriteLine("ABILITY-EFFECT-CHECKS PASS: reverse removal, empty/null/relic gates, shared cache, unused quantities and deferred queued assignment in 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var modifier) || modifier.GetString() != "ability-effects") return;
        var effects = fixture.GetProperty("AbilityEffectOperations").EnumerateArray().ToArray();
        Require(effects.Length > 0 && effects.All(item => item.GetProperty("Completed").GetBoolean()), "Incomplete native ability effects.");
        foreach (var record in effects) VerifyEffect(record);
        Require(effects.Any(item => item.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type == "SetUnitAbility" &&
            item.GetProperty("Targets").GetArrayLength() >= 2) &&
            effects.Any(item => item.GetProperty("RunningTriggerQueue").GetBoolean()) &&
            effects.Any(item => item.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type == "RemoveAbility" &&
                item.GetProperty("HasRelicGate").GetBoolean()), "Native multi-target, queued grant or inactive relic gate coverage missing.");
        var callbacks = fixture.GetProperty("CharacterCallbackFires").EnumerateArray().ToArray();
        foreach (var record in callbacks) VerifyCallback(record);
        Require(new[] { "OnPreOwnAbilityActivated", "OnOwnAbilityActivated", "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" }
            .All(kind => callbacks.Any(item => item.GetProperty("Kind").GetString() == kind)), "Native ability callback kinds missing.");
        const string original = "c2f6ed7f-18ce-4070-b65f-7dd9f5160063";
        const string replacement = "c2f6ed7f-18ce-4070-b65f-7dd9f5160071";
        const string queued = "c2f6ed7f-18ce-4070-b65f-7dd9f5160072";
        var activations = fixture.GetProperty("Actions").EnumerateArray().Where(item =>
            item.GetProperty("Action").Deserialize<PlayCardAction>()!.ActivatorUnitId > 0).ToArray();
        Require(activations.Length == 4, "Native cached replacement/removal activations missing.");
        foreach (var record in activations)
        {
            var action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
            CombatUnit actual = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
            Require(before.PlayRules!.Cards.Any(card => card.DataId == replacement) && before.PlayRules.Cards.Any(card => card.DataId == queued),
                "Newly assigned ability definitions are absent from the root rule catalog.");
            string played = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
            Require(played == prior.Ability!.DataId && played is original or replacement,
                "Pre-own replacement changed the cached card chosen for activation.");
            Require(played == original ? actual.Ability is { DataId: replacement, Cooldown: 1 } : actual.Ability == null,
                "Self replacement/removal produced an incorrect post-own ability.");
            Require(after.Spawn.Train.Context!.Cards.Hand.Concat(after.Spawn.Train.Context.Cards.Draw)
                .Concat(after.Spawn.Train.Context.Cards.Discard).All(card => card.InstanceId != action.CardInstanceId),
                "Detached activated ability entered an owned pile.");
        }
        var last = activations[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(last.Spawn.Train.Context!.PermanentlyDisabledAbilities!.SequenceEqual([queued, queued]),
            "Self removal must permanently disable the current queued replacement, preserving duplicates.");
        Parallel.For(0, 32, _ => { foreach (var record in effects) VerifyEffect(record); foreach (var record in callbacks) VerifyCallback(record); });
        Console.WriteLine($"NATIVE-ABILITY-EFFECT-CHECKS PASS: {effects.Length} complete effect states, {callbacks.Length} queued dispatches, cached self replacement/removal and 32 branches.");
    }

    private static void VerifyEffect(FixtureValue record)
    {
        var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var effect = record.GetProperty("Effect").Deserialize<CardActionEffect>()!;
        RoomCombatState state = before;
        foreach (int id in record.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).Reverse())
        {
            var result = AbilityLifecycleModel.Apply(state, id, effect.AbilityChange!, effect.Type == "RemoveAbility", deferCallbacks: true);
            Require(result.Supported, "Native ability effect unsupported: " + result.UnsupportedReason);
            state = result.State!;
        }
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(state, ModelJson.Options),
            JsonSerializer.Serialize(record.GetProperty("After").Deserialize<RoomCombatState>(), ModelJson.Options));
        Require(difference == null, "Native ability effect " + effect.Type + " differs: " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Ability effect mutated its native root.");
    }

    private static void VerifyCallback(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0,
            "Incomplete native ability callback.");
        var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var actor = record.GetProperty("BeforeUnit").Deserialize<CombatUnit>()!;
        var callback = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, record.GetProperty("Kind").GetString()!,
            paramInt: record.GetProperty("ParamInt").GetInt32(), paramInt2: record.GetProperty("ParamInt2").GetInt32(),
            paramString: record.GetProperty("ParamString").GetString(), overrideTarget: record.GetProperty("OverrideTarget").Deserialize<CombatUnit>(),
            canFireTriggers: record.GetProperty("CanFire").GetBoolean());
        var generated = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, callback, generated.Add);
        Require(result.Supported, "Native ability callback unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(record.GetProperty("Actual").Deserialize<RoomCombatState>(), ModelJson.Options));
        difference ??= ModelJson.Difference(JsonSerializer.Serialize(callback.Unit, ModelJson.Options),
            JsonSerializer.Serialize(record.GetProperty("ActualUnit").Deserialize<CombatUnit>(), ModelJson.Options));
        Require(difference == null, "Native ability callback " + callback.Kind + " differs: " + difference);
        var nativeGenerated = record.GetProperty("Generated").EnumerateArray().ToArray();
        Require(generated.Count == nativeGenerated.Length && generated.Select(item => item.Kind)
            .SequenceEqual(nativeGenerated.Select(item => item.GetProperty("Kind").GetString())), "Ability callback queue payload order differs.");
        for (int index = 0; index < generated.Count; index++)
        {
            var native = nativeGenerated[index]; var predicted = generated[index];
            Require(predicted.Unit.Id == native.GetProperty("ActorId").GetInt32() && predicted.ParamInt == native.GetProperty("ParamInt").GetInt32() &&
                predicted.ParamInt2 == native.GetProperty("ParamInt2").GetInt32() && predicted.ParamString == native.GetProperty("ParamString").GetString(),
                "Ability callback generated incorrect actor or payload.");
        }
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Ability callback mutated its native root.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
