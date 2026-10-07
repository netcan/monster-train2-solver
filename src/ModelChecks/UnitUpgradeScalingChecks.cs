using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitUpgradeScalingChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(194);
        var traits = new ScalingUnitUpgradeTrait[] { new(new("AnyCharacter"), "Damage", 3), new(new("AnyCharacter"), "Health", 2, 1),
            new(new("MoonPhase"), "Damage", 1, 1), new(new("TurnCount"), "Health", -1, 99) };
        var owner = new CardInstanceState(1, "spell", new(new(damage: 100, health: 100), [], 0, []), CardModifiers.Empty(), 0, 0, 0, [],
            unitUpgradeScalingTraits: traits);
        var spawner = new CardInstanceState(2, "unit", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            unitUpgradeScalingTraits: [new(new("AnyCharacter"), "Health", 1)]);
        var statistics = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 2, 99], [1, 2]);
        CombatContext Context(CardInstanceState source, StatisticQueryFrame? frame) => new(new([new(1, "spell")], [], [], rng, 0, []),
            rng, 0, 3, 10, statistics: statistics, cardInstances: [source, spawner], otherPiles: [new("Standby", [new(2, "unit")]), new("Exhausted", [])],
            queryFrame: frame);
        var frame = new StatisticQueryFrame(4, true, 3, 4, 0, 2, 0);
        var context = Context(owner, frame);
        var upgrade = new CardUpgradeModifier("upgrade", "upgrade", new(damage: 4, health: 5, cost: -1, heal: 2, size: 1, equipmentLimit: 1),
            [], false, false, false, 3, 2, []);
        var unit = new CombatUnit(10, "target", CombatTeam.Player, 8, 12, 25, true, false, false, [], spawnerCardId: 2, size: 2,
            modifiers: new(8, 0, 0, 2, 1, true, false, []));
        var room = new RoomCombatState(0, false, [unit], [], context);
        string parent = JsonSerializer.Serialize(room);
        UnitUpgradeScalingResult scaled = UnitUpgradeScalingModel.Apply(context, 1, upgrade);
        Require(scaled.Supported && scaled.Upgrade!.Stats.Damage == 9 && scaled.Upgrade.Stats.Health == 4 &&
            scaled.Upgrade.Stats.Cost == -1 && scaled.Upgrade.UnhealedHealth == 3 && scaled.Context!.Statistics!.TrackedCards.SequenceEqual([1, 2]),
            "Mixed traits, signed bonuses, source numeric modifiers, untouched upgrade fields or membership refresh differ.");
        Require(UnitUpgradeScalingModel.Apply(context, 1, upgrade, "OnSpawn").Upgrade!.Stats.Health == 4 &&
            UnitUpgradeScalingModel.Apply(context, 1, upgrade, "OnHeal").Upgrade!.Stats.Damage == 7 &&
            UnitUpgradeScalingModel.Apply(context, 1, upgrade, "OnHeal").Upgrade!.Stats.Health == 2,
            "Spawn restriction failed to distinguish null/OnSpawn from other triggers, or guessed unknown restriction values.");
        var magic = new CardUpgradeModifier("magic", "magic", new(damage: 4, health: 5), [], false, false, false, 0, 0, [], magicPowerTraitScalingOnly: true);
        Require(UnitUpgradeScalingModel.ApplyTrait(null, new(new("Gold"), "Damage", 100), 1, magic).Upgrade == magic &&
            UnitUpgradeScalingModel.ApplyTrait(null, new(new("Gold"), "Damage", 100, 1), 1, upgrade, "OnHeal").Upgrade == upgrade,
            "Skipped magic-only/trigger gates queried incomplete context.");
        var wrapping = UnitUpgradeScalingModel.ApplyTrait(context, new(new("AnyCharacter"), "Damage", int.MaxValue), 1,
            new("wrap", "wrap", new(damage: 1), [], false, false, false, 0, 0, []));
        Require(wrapping.Supported && wrapping.Upgrade!.Stats.Damage == int.MinValue, "Trait bonus addition did not wrap as a native integer.");
        RoomCombatResult applied = UnitModifierModel.Apply(room, 10, upgrade, "TemporaryUntilEndOfBattle", sourceCardId: 1);
        Require(applied.Supported && applied.State!.Units.Single().BaseAttack == 19 && applied.State.Units.Single().MaxHealth == 32 &&
            applied.State.Units.Single().Health == 16 && applied.State.Units.Single().Modifiers!.Upgrades.Single().Stats.Damage == 9 &&
            applied.State.Context!.CardInstances!.Single(card => card.InstanceId == 2).Temporary.Upgrades.Single().Stats.Health == 4,
            "Scaled unit or source-card application failed to retain the modified upgrade.");
        RoomCombatResult removed = UnitModifierModel.Apply(applied.State!, 10, upgrade, "", remove: true, sourceCardId: 1);
        Require(removed.Supported && removed.State!.Units.Single().BaseAttack == 13 && removed.State.Units.Single().MaxHealth == 24 &&
            removed.State.Units.Single().Modifiers!.Upgrades.Count == 0 &&
            removed.State.Context!.CardInstances!.Single(card => card.InstanceId == 2).Temporary.Upgrades.Count == 0,
            "Definition-based removal re-scaled or deducted the stored bonus instead of the native base upgrade argument.");
        var unknown = new CardInstanceState(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            unitUpgradeScalingTraits: [new(new("Gold"), "Damage", 1)]);
        var incomplete = Context(unknown, null);
        var restricted = new CardUpgradeModifier("restricted", "restricted", new(size: 99), [], false, false, false, 0, 0, [], true);
        Require(!UnitModifierModel.Apply(new(0, false, [unit], [], incomplete), 10, restricted, "Permanent", roomCapacity: 2, sourceCardId: 1).Supported,
            "Capacity failure bypassed its preceding scaling query.");
        var unique = new CardUpgradeModifier("unique", "unique", new(damage: 4, health: 5), [], false, true, true, 0, 0, []);
        RoomCombatResult once = UnitModifierModel.Apply(room, 10, unique, "TemporaryUntilEndOfBattle", sourceCardId: 1);
        Require(once.Supported && !UnitModifierModel.Apply(new(0, false, once.State!.Units, [], incomplete), 10, unique,
            "TemporaryUntilEndOfBattle", sourceCardId: 1).Supported, "Uniqueness bypassed the native trait query.");
        var clone = new CombatUnit(10, "clone", CombatTeam.Player, 8, 12, 25, true, false, false, [], spawnerCardId: 2, size: 2,
            modifiers: new(8, 0, 0, 2, 1, true, true, []));
        var cloneRoom = new RoomCombatState(0, false, [clone], [], incomplete);
        var excludedClone = UnitModifierModel.Apply(cloneRoom, 10, unique, "Permanent", sourceCardId: 1);
        Require(excludedClone.Supported && JsonSerializer.Serialize(excludedClone.State) == JsonSerializer.Serialize(cloneRoom),
            "Clone exclusion queried a trait before the native early return.");
        Require(!UnitUpgradeScalingModel.Apply(context, 999, upgrade).Supported &&
            !UnitUpgradeScalingModel.ApplyTrait(context, new(new("AnyStatusEffectStacksRemoved"), "Damage", 1), 1, upgrade).Supported,
            "Missing ownership or incomplete removal attribution returned a usable upgrade.");
        var generated = CardGenerationModel.Apply(context, new("HandPile", 1,
            [new("generated", CardModifiers.Empty(), null, [], unitUpgradeScalingTraits: traits)]));
        Require(generated.Supported && UnitUpgradeScalingModel.Apply(generated.Context, 3, upgrade).Upgrade!.Stats.Damage == 9 &&
            !CardGenerationModel.Apply(new(context.Cards, rng, 0, 3, 10), new("HandPile", 1,
                [new("generated", CardModifiers.Empty(), null, [], unitUpgradeScalingTraits: traits)])).Supported,
            "Creation lost scaling traits or accepted incomplete card metadata.");
        Require(owner.OnDiscard(true).UnitUpgradeScalingTraits!.Count == 4, "Discard lost upgrade scaling.");
        var hand = HandUpgradeModel.Apply(room, upgrade, "TemporaryUntilEndOfBattle", new([], [new("spell", "spell", 0, "Null", "Discard", null, [])]));
        Require(hand.Supported && UnitUpgradeScalingModel.Apply(hand.State!.Context, 1, upgrade).Upgrade!.Stats.Damage == 9,
            "Hand upgrades lost traits or incorrectly added numeric spell modifiers to scaling.");
        RoomCombatResult health = UnitHealthModel.Apply(room, 10, 2);
        foreach (RoomCombatResult copied in new[] { applied, health })
            Require(copied.Supported && UnitUpgradeScalingModel.Apply(copied.State!.Context, 2, upgrade).Upgrade!.Stats.Health == 6,
                "Spawner unit/health updates lost its scaling metadata.");
        Parallel.For(0, 64, _ => Require(JsonSerializer.Serialize(UnitModifierModel.Apply(room, 10, upgrade,
            "TemporaryUntilEndOfBattle", sourceCardId: 1)) == JsonSerializer.Serialize(applied), "Parallel scaled upgrades differed."));
        Require(JsonSerializer.Serialize(room) == parent, "Scaled upgrade mutated its parent.");
        traits[0] = new(new("Gold"), "Damage", 999);
        Require(owner.UnitUpgradeScalingTraits![0].AmountPerStat == 3, "Caller mutation changed ordered trait descriptors.");
        Console.WriteLine("UNIT-UPGRADE-SCALING-CHECKS PASS: mixed traits, signed/wrapped integers, native trigger/magic gates, query ordering, base-valued removal, creation/modification and 64 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "unit-upgrade-scaling") return;
        Require(fixture.GetProperty("UnitUpgradeScalingCalibrationContextUnchanged").GetBoolean(), "Native callback calibration changed live state.");
        int calibration = 0, live = 0, signed = 0, grew = 0, magic = 0, skippedTrigger = 0, distinctOwner = 0;
        var stats = new HashSet<string>(); var queries = new HashSet<string>(); var moons = new HashSet<int>(); var turns = new HashSet<int>();
        foreach (FixtureValue sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            Require(sample.GetProperty("CaptureError").ValueKind == FixtureKind.Null && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Native upgrade callback capture is incomplete or differs.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>()!;
            ScalingUnitUpgradeTrait trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>()!;
            CardUpgradeModifier original = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>()!;
            CardUpgradeModifier actual = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>()!;
            string? kind = sample.GetProperty("TriggerKind").GetString();
            UnitUpgradeScalingResult result = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), original, kind);
            Require(result.Supported && JsonSerializer.Serialize(result.Upgrade) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(result.Context) == JsonSerializer.Serialize(after), "Independent native trait upgrade or refreshed context differs.");
            bool calibrating = sample.GetProperty("Origin").GetString() == "Calibration";
            if (calibrating) { calibration++; Require(sample.GetProperty("OwnerCardId").GetInt32() != sample.GetProperty("ThisCardId").GetInt32(),
                "Calibration failed to distinguish callback argument from trait owner."); } else live++;
            if (calibrating && trait.Query.Type == "TimesPlayed" && !original.MagicPowerTraitScalingOnly &&
                before.Statistics!.Value(sample.GetProperty("OwnerCardId").GetInt32(), "TimesPlayed", "ThisBattle") !=
                before.Statistics.Value(sample.GetProperty("ThisCardId").GetInt32(), "TimesPlayed", "ThisBattle")) distinctOwner++;
            stats.Add(trait.Stat); queries.Add(trait.Query.Type); moons.Add(before.QueryFrame!.MoonPhase!.Value); turns.Add(before.QueryFrame.Turn!.Value);
            signed += actual.Stats.Damage < original.Stats.Damage ? 1 : 0; grew += actual.Stats.Health > original.Stats.Health ? 1 : 0;
            magic += original.MagicPowerTraitScalingOnly ? 1 : 0;
            skippedTrigger += trait.Restriction == 1 && kind == "OnHeal" && JsonSerializer.Serialize(original) == JsonSerializer.Serialize(actual) ? 1 : 0;
        }
        Require(calibration == 30 && live >= 35 && stats.Count == 2 && queries.Count == 5 && moons.Count == 2 && turns.Count >= 2 &&
            signed > 0 && grew > 0 && magic > 0 && skippedTrigger > 0 && distinctOwner == 3, "Native upgrade scaling lacks meaningful owner, gate, signed or dynamic coverage.");
        Console.WriteLine($"NATIVE-UNIT-UPGRADE-SCALING-CHECKS PASS: {live} live and {calibration} isolated callbacks, {signed} negative attack bonuses, {grew} health increases, {magic} magic gates and {skippedTrigger} non-spawn skips; complete upgrades and contexts.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
