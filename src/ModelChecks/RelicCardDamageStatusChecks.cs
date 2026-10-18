using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicCardDamageStatusChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(79);
        var owner = new CombatRelicState(RelicCardDamageStatusModel.DataId,
            RelicCardDamageStatusModel.AssetKey, [RelicCardDamageStatusModel.Effect]);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            [new CombatStatus("cooldown", 0, stackable: true)], relics: [owner]);
        CombatUnit friend = Unit(1, CombatTeam.Player);
        CombatUnit enemy = Unit(2, CombatTeam.Enemy);
        CombatUnit pyre = Unit(3, CombatTeam.Player, isPyre: true);
        var room = new RoomCombatState(0, true, [friend, enemy, pyre], [], context);
        var train = new TrainCombatState([room], [], 6, context);

        TrainCombatResult applied = RelicCardDamageStatusModel.ApplyOne(train, 0,
            [friend.Id, friend.Id, enemy.Id, pyre.Id], 0, 0);
        Require(applied.Supported && UnitIn(applied.State!, friend.Id).Status("cooldown")?.Stacks == 2 &&
            UnitIn(applied.State!, enemy.Id).Status("cooldown")?.Stacks == 3 &&
            UnitIn(applied.State!, pyre.Id).Status("cooldown")?.Stacks == 3,
            "CooldownOnDamage must reduce cooldown once on a damaged friendly Monster only: supported=" +
                applied.Supported + " reason=" + applied.UnsupportedReason + " friend=" +
                (applied.Supported ? UnitIn(applied.State!, friend.Id).Status("cooldown")?.Stacks : -1));

        TrainCombatResult empty = RelicCardDamageStatusModel.ApplyOne(train, 0, [], 0, 0);
        Require(empty.Supported && ReferenceEquals(empty.State, train),
            "No damaged characters must leave the battle state unchanged.");

        var lowCooldownTrain = new TrainCombatState(
            [new RoomCombatState(0, true, [Unit(4, CombatTeam.Player, cooldown: 1)], [], context)], [], 6, context);
        TrainCombatResult lowCooldown = RelicCardDamageStatusModel.ApplyOne(lowCooldownTrain, 0, [4], 0, 0);
        Require(!lowCooldown.Supported && lowCooldown.State == null &&
            lowCooldown.UnsupportedReason!.Contains("ability-ready callbacks", StringComparison.Ordinal),
            "Cooldown reaching zero must remain rejected until ability-ready callbacks are calibrated.");

        var malformed = new CombatRelicState("other-owner", RelicCardDamageStatusModel.AssetKey,
            [RelicCardDamageStatusModel.Effect]);
        Require(RelicModel.Validate([malformed]) != null,
            "An unrelated relic owner must not inherit CooldownOnDamage behavior.");
        Console.WriteLine("COOLDOWN-ON-DAMAGE-CHECKS PASS: friendly Monster only, duplicate hit deduplication, enemy/Pyre exclusion, empty event, cooldown-zero rejection and exact owner guard.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 218 &&
            root.GetProperty("OriginalCooldownOnDamageSchema").GetInt32() == 218 &&
            root.GetProperty("OriginalCooldownOnDamagePrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-cooldown-on-damage-dh" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalCooldownOnDamageCaptureFailures",
                "OriginalCooldownOnDamageMismatches", "OriginalCooldownOnDamageUnsupported", "OriginalCooldownOnDamagePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The CooldownOnDamage fixture is incomplete or contains differences.");

        FixtureValue evidence = root.GetProperty("OriginalCooldownOnDamage");
        int carrierCardId = evidence.GetProperty("CarrierCardInstanceId").GetInt32();
        int damageCardId = evidence.GetProperty("DamageCardInstanceId").GetInt32();
        int healCardId = evidence.GetProperty("HealCardInstanceId").GetInt32();
        int carrierUnitId = evidence.GetProperty("CarrierUnitId").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 3, "CooldownOnDamage must replay the carrier, damage and heal-control actions exactly once.");

        BattleTurnState FindAfter(int cardId)
        {
            FixtureValue entry = actions.Single(item => item.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == cardId);
            BattleTurnState predicted = entry.GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(ModelJson.Difference(BattleTurnChecks.Comparable(predicted), BattleTurnChecks.Comparable(actual)) == null,
                "CooldownOnDamage action prediction differs from native state.");
            return actual;
        }

        int Cooldown(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == carrierUnitId).Status("cooldown")?.Stacks ?? 0;
        BattleTurnState afterCarrier = FindAfter(carrierCardId);
        CombatStatus? carrierCooldown = afterCarrier.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == carrierUnitId).Status("cooldown");
        Require(Cooldown(afterCarrier) == 3 && carrierCooldown?.PreventRemovalDuringRelentless == true &&
            Cooldown(FindAfter(damageCardId)) == 2 &&
            Cooldown(FindAfter(healCardId)) == 2,
            "Native CooldownOnDamage did not retain its native cooldown status and change 3->2 only after the damage spell.");

        FixtureValue[] tests = root.GetProperty("RelicCooldownOnDamageTests").EnumerateArray().ToArray();
        Require(tests.Count(item => item.GetProperty("CardInstanceId").GetInt32() == damageCardId &&
                item.GetProperty("Result").GetBoolean()) == 1 &&
            tests.Count(item => item.GetProperty("CardInstanceId").GetInt32() == healCardId &&
                !item.GetProperty("Result").GetBoolean()) == 1,
            "Native TestEffectOnCardPlayed did not distinguish friendly damage from the no-damage control.");
        FixtureValue[] applications = root.GetProperty("RelicCooldownOnDamageApplications").EnumerateArray().ToArray();
        Require(applications.Length == 1 && applications[0].GetProperty("CardInstanceId").GetInt32() == damageCardId &&
            applications[0].GetProperty("TargetUnitId").GetInt32() == carrierUnitId &&
            applications[0].GetProperty("CooldownBefore").GetInt32() == 3 &&
            applications[0].GetProperty("CooldownAfter").GetInt32() == 2 &&
            applications[0].GetProperty("Completed").GetBoolean() &&
            applications[0].GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The native relic callback must apply exactly one matching 3-to-2 cooldown transition.");
        Console.WriteLine("COOLDOWN-ON-DAMAGE-NATIVE PASS: exact original payload, friendly Spell damage 3->2, empty-Spell control, callback and full-battle differential.");
    }

    private static CombatUnit Unit(int id, CombatTeam team, bool isPyre = false, int cooldown = 3) => new(id,
        isPyre ? "Pyre" : "CooldownWitness", team, 0, 30, 30, false, isPyre, false,
        [new CombatStatus("cooldown", cooldown)], []);

    private static CombatUnit UnitIn(TrainCombatState state, int id) =>
        state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
