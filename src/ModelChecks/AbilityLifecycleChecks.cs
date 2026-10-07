using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityLifecycleChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(37);
        CombatStatus[] statuses = [new("unit_ability", 1, hidden: true, displayCategory: "Persistent"),
            new("cooldown", 2, stackable: true, hidden: true, displayCategory: "Persistent"),
            new("unit_ability_available", 1, hidden: true, displayCategory: "Persistent")];
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, statuses,
            cardInstances: [], cardRegistry: [], abilityCardCache: [], permanentlyDisabledAbilities: []);
        UnitAbilityDefinition Definition(string id, int cd, int spawn) => new(id, true, cd, spawn,
            new(id, CardModifiers.Empty(), [], []));
        var a = Definition("a", 3, 2); var b = Definition("b", 5, 0); var c = Definition("c", 7, 4);
        CombatTrigger[] common = new[] { "OnOwnAbilityActivated", "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" }
            .Select((kind, index) => new CombatTrigger(kind, false, false, true, 1, [], skipDuringDeployment: false,
                origin: new("UnitAbilityCommonData", 0, false, false), stateId: index)).ToArray();
        CombatUnit Unit(bool restricted = false, UnitAbilityState? ability = null) => new(1, "unit", CombatTeam.Player, 1, 20, 20, true, false, false,
            statuses.Take(2).ToArray(), common, statusRegistry: statuses.Take(2).ToArray(), nextTriggerId: 3,
            ability: ability ?? new("a", 9, 2, cardCreation: a.CardCreation, definition: a),
            statusDictionary: new(["unit_ability", "cooldown"], []), abilityRules: new(restricted, []));
        var root = new RoomCombatState(0, false, [Unit()], [], context);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        AbilityChangeRule Rule(UnitAbilityDefinition? data = null, bool equipment = false, bool permanent = false) => new(data, equipment, permanent, common);
        RoomCombatState Apply(RoomCombatState state, AbilityChangeRule rule, bool remove = false)
        {
            var result = AbilityLifecycleModel.Apply(state, 1, rule, remove);
            Require(result.Supported, "Lifecycle unsupported: " + result.UnsupportedReason); return result.State!;
        }
        var overlay = Apply(root, Rule(c, true));
        Require(overlay.Units[0].Ability is { FromEquipment: true, PreviousDataId: "a", Cooldown: 7 } &&
            overlay.Units[0].Ability!.PreviousDefinition!.Cooldown == 3 && overlay.Units[0].NextTriggerId == 6,
            "Equipment overlay lost raw definition or allocated trigger identity.");
        var twice = Apply(overlay, Rule(b, true));
        Require(twice.Units[0].Ability!.PreviousDataId == "a", "Replacing equipment forgot the original ability.");
        var restored = Apply(twice, Rule(), true);
        Require(restored.Units[0].Ability is { DataId: "a", Cooldown: 3, CooldownAtSpawn: 2, FromEquipment: false, PreviousDataId: null } &&
            restored.Units[0].Status("cooldown")!.Stacks == 3, "Restoration used old runtime or spawn cooldown.");
        var directlyReplaced = Apply(overlay, Rule(b));
        Require(directlyReplaced.Units[0].Ability is { DataId: "b", FromEquipment: false, PreviousDataId: null } &&
            directlyReplaced.Units[0].Status("cooldown") == null, "Direct replacement retained equipment history or zero cooldown stacks.");
        var permanentlyRemoved = Apply(overlay, Rule(permanent: true), true);
        Require(permanentlyRemoved.Context!.PermanentlyDisabledAbilities!.SequenceEqual(["c"]) &&
            permanentlyRemoved.Units[0].Ability!.DataId == "a", "Permanent equipment removal disabled the restored original.");
        var disabled = Apply(Apply(root, Rule(permanent: true), true), Rule(a));
        disabled = Apply(disabled, Rule(permanent: true), true);
        Require(disabled.Context!.PermanentlyDisabledAbilities!.SequenceEqual(["a", "a"]) && disabled.Units[0].Ability == null,
            "Explicit reassignment or duplicate permanent disable order was lost.");
        var restricted = new RoomCombatState(0, false, [Unit(true)], [], context);
        Require(JsonSerializer.Serialize(Apply(restricted, Rule(c, true)), ModelJson.Options) == JsonSerializer.Serialize(restricted, ModelJson.Options),
            "Restricted equipment changed state.");
        Require(JsonSerializer.Serialize(Apply(root, Rule()), ModelJson.Options) == parent, "Null assignment changed state.");
        var noAbility = new RoomCombatState(0, false, [Unit(ability: new("", 4, 1))], [], context);
        Require(Apply(noAbility, Rule(permanent: true), true).Units[0].Ability!.Cooldown == 4, "No-ability removal cleared raw duration fields.");
        var immune = new RoomCombatState(0, false, [new CombatUnit(1, "immune", CombatTeam.Player, 1, 20, 20, true, false, false, [],
            statusImmunities: ["unit_ability", "cooldown"], statusRegistry: [], nextTriggerId: 0,
            statusDictionary: new([], []), abilityRules: new(false, []))], [], context);
        var assignedImmune = Apply(immune, Rule(c));
        Require(assignedImmune.Units[0].Ability!.HasAbility && assignedImmune.Units[0].Status("unit_ability") == null &&
            assignedImmune.Units[0].Status("cooldown")!.Stacks == 4, "Marker immunity and cooldown immunity bypass were conflated.");
        var preview = new RoomCombatState(0, false, root.Units, [], context, true);
        Require(Apply(preview, Rule(c)).Units[0].NextTriggerId == 3 && Apply(preview, Rule(), true).Units[0].Triggers.Count == 3,
            "Preview changed the live common trigger list or allocation cursor.");
        string expected = JsonSerializer.Serialize(restored, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Apply(Apply(Apply(root, Rule(c, true)), Rule(b, true)), Rule(), true),
            ModelJson.Options) == expected, "Parallel lifecycle branches differ."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Lifecycle changed its parent.");
        Console.WriteLine("ABILITY-LIFECYCLE-CHECKS PASS: raw cooldown restoration, equipment replacement, permanent duplicate order, no-op gates and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("AbilityLifecycleOperations", out var records) || records.GetArrayLength() == 0)
        {
            Require(!fixture.TryGetProperty("ModifierScenario", out var modifier) || modifier.GetString() != "ability-lifecycle",
                "Requested native ability lifecycle did not execute."); return;
        }
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        string[] labels = ["remove-base", "remove-missing-noop", "null-definition-noop", "ordinary-card-noop", "assign-base", "reassign-base",
            "zero-spawn", "equipment-over-zero", "equipment-replace-keeps-original", "restore-zero-with-activation-cooldown", "equipment-again",
            "direct-replace-clears-original", "equipment-before-permanent", "permanent-equipment-restores-original", "equipment-restricted-noop",
            "equipment-over-modified-duration", "restore-raw-activation-cooldown", "permanent-base", "permanent-missing-noop",
            "explicit-assign-disabled", "duplicate-permanent-base"];
        Require(samples.Length == labels.Length && samples.Select(item => item.GetProperty("Label").GetString()).SequenceEqual(labels),
            "Native lifecycle operation coverage differs.");
        var byLabel = samples.ToDictionary(item => item.GetProperty("Label").GetString()!);
        CombatUnit Actor(string label, string phase = "After") => byLabel[label].GetProperty(phase).Deserialize<RoomCombatState>()!.Units
            .Single(unit => unit.Id == byLabel[label].GetProperty("UnitId").GetInt32());
        string original = Actor("remove-base", "Before").Ability!.DataId;
        var zero = Actor("zero-spawn"); var restoredZero = Actor("restore-zero-with-activation-cooldown");
        Require(zero.Ability is { Cooldown: 5, CooldownAtSpawn: 0 } && zero.Status("cooldown") == null &&
            restoredZero.Ability is { Cooldown: 5, CooldownAtSpawn: 0, FromEquipment: false, PreviousDataId: null } &&
            restoredZero.Ability.DataId == zero.Ability.DataId && restoredZero.Status("cooldown")!.Stacks == 5,
            "Native zero-spawn ability was not restored using activation cooldown.");
        var modified = Actor("equipment-over-modified-duration", "Before"); var restoredRaw = Actor("restore-raw-activation-cooldown");
        Require(modified.Ability is { Cooldown: 9 } && modified.Ability.Definition!.Cooldown == 3 &&
            restoredRaw.Ability is { Cooldown: 3, CooldownAtSpawn: 2, PreviousDataId: null } && restoredRaw.Status("cooldown")!.Stacks == 3,
            "Native restoration did not distinguish raw, current and spawn cooldowns.");
        Require(Actor("equipment-replace-keeps-original").Ability!.PreviousDataId == zero.Ability!.DataId &&
            Actor("direct-replace-clears-original").Ability!.PreviousDataId == null, "Native replacement history coverage missing.");
        Require(Actor("remove-base").Ability == null && Actor("remove-base").Status("unit_ability_available")!.Stacks == 1 &&
            Actor("reassign-base").Triggers.Any(trigger => trigger.Origin?.UpgradeId == "UnitAbilityCommonData" &&
                trigger.Kind == "OnUnitAbilityAvailable" && !trigger.HasTriggered), "Native yielding removal callback boundary missing.");
        var disabled = samples[^1].GetProperty("After").Deserialize<RoomCombatState>()!.Context!.PermanentlyDisabledAbilities!;
        Require(disabled.Count == 3 && disabled[0] != disabled[1] && disabled[1] == original && disabled[1] == disabled[2] &&
            Actor("explicit-assign-disabled").Ability!.DataId == original && Actor("duplicate-permanent-base").Ability == null,
            "Native permanent disable order/duplicates or explicit assignment exception missing.");
        var naturallyDisabled = fixture.GetProperty("Actions").EnumerateArray().Any(record =>
        {
            int card = record.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.PermanentlyDisabledAbilities!.Contains(disabled[1]) && after.Spawn.Train.Rooms
                .SelectMany(room => room.Units).Any(unit => unit.Id >= before.Spawn.NextUnitId && unit.SpawnerCardId == card &&
                    unit.AssetKey.Contains("Steward") && unit.Ability == null && unit.Triggers.All(trigger => trigger.Origin?.UpgradeId != "UnitAbilityCommonData"));
        });
        Require(naturallyDisabled, "No natural summon respected the permanent ability disable list.");
        Require(fixture.GetProperty("Spawns").EnumerateArray().Any(record =>
        {
            var before = record.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            var after = record.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            var assets = before.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units)
                .Where(definition => definition.Unit.Ability?.DataId == original).Select(definition => definition.Unit.AssetKey).ToArray();
            return before.Train.Context!.PermanentlyDisabledAbilities!.Contains(original) && after.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id >= before.NextUnitId && unit.Team == CombatTeam.Enemy && assets.Contains(unit.AssetKey) && unit.Ability == null &&
                    unit.Triggers.All(trigger => trigger.Origin?.UpgradeId != "UnitAbilityCommonData"));
        }), "No natural enemy spawn respected the permanently disabled authored skill.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-ABILITY-LIFECYCLE-CHECKS PASS: {samples.Length} complete native API states, raw restoration, disabled player/enemy spawns and 32 branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = AbilityLifecycleModel.Apply(before, sample.GetProperty("UnitId").GetInt32(),
            sample.GetProperty("Rule").Deserialize<AbilityChangeRule>()!, sample.GetProperty("Remove").GetBoolean());
        Require(result.Supported, "Native lifecycle unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("After").Deserialize<RoomCombatState>(), ModelJson.Options));
        Require(difference == null, "Native lifecycle " + sample.GetProperty("Label").GetString() + " differs: " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native lifecycle changed its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
