using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityCooldownChecks
{
    internal static void Run()
    {
        CombatStatus cooldown = new("cooldown", 2, removeStackAtEnd: true, preventRemovalDuringRelentless: true,
            stackable: true, hidden: true, displayCategory: "Persistent");
        var rng = UnityRng.Seed(17);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [cooldown]);
        CardActionEffect Adjust(int amount, bool absolute = false) => new("AdjustAbilityCooldown", "Room", amount, true, true, [], cooldownParameter: absolute);
        CardActionEffect Reset(bool spawn = false) => new("ResetCooldown", "Room", 0, true, true, [], cooldownParameter: spawn);
        CombatUnit Unit(bool has = true, int stacks = 2) => new(1, "unit", CombatTeam.Player, 1, 20, 20, true, false, false,
            stacks > 0 ? [new("cooldown", stacks, stackable: true)] : [], statusImmunities: ["cooldown"], statusRegistry: [cooldown],
            ability: has ? new("ability", 3, 2) : null);
        RoomCombatState Root(bool has = true, int stacks = 2) => new(0, false, [Unit(has, stacks)], [], context);
        var root = Root(); string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        var dictionary = new StatusDictionaryState(["regen", "silenced", "unit_ability"], []).Remove("regen").Remove("silenced");
        CombatStatus[] added = [new("unit_ability", 1), new("armor", 1), new("valor", 1)];
        var reused = dictionary.Sync(added);
        Require(reused.Order(added).Select(status => status.Id).SequenceEqual(["valor", "armor", "unit_ability"]) &&
            dictionary.FreeSlots.SequenceEqual([1, 0]), "Removed native dictionary slots did not reuse in LIFO order.");
        var cleared = AbilityCooldownModel.Apply(root, 1, Adjust(-10));
        Require(cleared.Supported && cleared.State!.Units[0].Ability!.Cooldown == 1 && cleared.State.Units[0].Status("cooldown") == null &&
            cleared.PendingCallbacks.Select(item => item.Kind).SequenceEqual(["OnStatusEffectChanged", "OnUnitAbilityAvailable"]),
            "Negative adjustment must clamp the configured duration while clearing current cooldown and queuing availability.");
        var lengthened = AbilityCooldownModel.Apply(root, 1, Adjust(5, true));
        Require(lengthened.State!.Units[0].Ability!.Cooldown == 5 && lengthened.State.Units[0].Status("cooldown")!.Stacks == 2,
            "Increasing the duration changed the current remaining cooldown.");
        var reset = AbilityCooldownModel.Apply(cleared.State!, 1, Reset(spawn: true));
        Require(reset.Supported && reset.State!.Units[0].Status("cooldown")!.Stacks == 2 &&
            reset.PendingCallbacks.Any(item => item.Kind == "OnUnitAbilityUnavailable"), "Spawn reset lost immunity bypass or zero-crossing callbacks.");
        Require(AbilityCooldownModel.Apply(Root(stacks: 7), 1, Reset()).State!.Units[0].Status("cooldown")!.Stacks == 7,
            "Reset incorrectly shortened an existing cooldown.");
        Require(AbilityCooldownModel.Apply(Root(has: false, stacks: 0), 1, Adjust(-3)).State!.Units[0].Ability == null,
            "A nonpositive relative adjustment created a cooldown on a unit without an ability.");
        var none = AbilityCooldownModel.Apply(Root(has: false, stacks: 0), 1, Adjust(0, true));
        Require(none.State!.Units[0].Ability is { HasAbility: false, Cooldown: 1 }, "Absolute adjustment must retain native fields even without an ability.");
        var spell = CardSpellModel.Apply(root, [Adjust(-10), Reset(true)], 0);
        Require(spell.Supported && spell.State!.Units[0].Ability!.Cooldown == 1 && spell.State.Units[0].Status("cooldown")!.Stacks == 2,
            "Composed spell effects lost updated ability fields.");
        string expected = JsonSerializer.Serialize(spell.State, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, [Adjust(-10), Reset(true)], 0).State,
            ModelJson.Options) == expected, "Parallel cooldown branches differ."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Cooldown branch mutated its parent.");
        Console.WriteLine("ABILITY-COOLDOWN-CHECKS PASS: separate duration/stacks, signed/absolute gates, no-ability fields, reset/immunity and 32 branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("AbilityCooldownEffects", out var records) || records.GetArrayLength() == 0)
        {
            Require(!fixture.TryGetProperty("ModifierScenario", out var modifier) || modifier.GetString() != "ability-cooldown",
                "Requested ability cooldown scenario did not execute.");
            return;
        }
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        Require(samples.Any(item => item.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type == "ResetCooldown") &&
            samples.Any(item => item.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type == "AdjustAbilityCooldown") &&
            samples.Any(item => item.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type == "RemoveStatus"),
            "Native coverage missed reset, adjustment or availability-marker removal.");
        Require(samples.Any(item => item.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Any(unit => unit.Ability?.HasAbility == true)) &&
            samples.Any(item => item.GetProperty("After").Deserialize<RoomCombatState>()!.Units.Any(unit => unit.Ability is { HasAbility: false, Cooldown: > 0 })),
            "Native coverage missed actual unit abilities or non-ability duration fields.");
        var actualUnits = fixture.GetProperty("Stages").EnumerateArray().SelectMany(stage => stage.GetProperty("Actual")
            .Deserialize<RoomCombatState>()!.Units).Where(unit => unit.Ability?.HasAbility == true).ToArray();
        Require(new[] { "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" }.All(kind => actualUnits.Any(unit =>
            unit.Triggers.Any(trigger => trigger.Kind == kind && trigger.HasTriggered && trigger.Origin?.UpgradeId == "UnitAbilityCommonData"))),
            "Native common availability and unavailability callbacks did not both fire.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-ABILITY-COOLDOWN-CHECKS PASS: {samples.Length} complete native effect states and 32 branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var effect = sample.GetProperty("Effect").Deserialize<CardActionEffect>()!;
        var targets = sample.GetProperty("Targets").Deserialize<int[]>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatState current = before;
        foreach (int id in effect.Type == "RemoveStatus" ? targets : targets.Reverse())
        {
            var result = effect.Type == "RemoveStatus" ? AbilityCooldownModel.RemoveStatus(current, id, effect.Statuses[0].Id,
                effect.Statuses[0].Stacks, sample.GetProperty("SourceCardId").GetInt32()) :
                AbilityCooldownModel.Apply(current, id, effect, sample.GetProperty("SourceCardId").GetInt32());
            Require(result.Supported, "Native cooldown unsupported: " + result.UnsupportedReason); current = result.State!;
        }
        string? diff = ModelJson.Difference(JsonSerializer.Serialize(current, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("After").Deserialize<RoomCombatState>(), ModelJson.Options));
        Require(diff == null, "Native cooldown state differs: " + diff);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native cooldown changed its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
