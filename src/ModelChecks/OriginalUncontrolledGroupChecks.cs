using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUncontrolledGroupChecks
{
    internal const string RelicId = "4c737e1e-7e5c-40ee-87a3-944859784283";
    internal const string RelicKey = "UncontrolledGroup";
    private const string Scenario = "original-uncontrolled-group";

    internal static void Run()
    {
        RelicUnstableRebalanceModifier Rule(int growth = 5, int team = 255, int proc = 0,
            int explosion = 0, bool onlyOne = false) => new(0, team, proc, explosion, growth,
                onlyOne, "unstable", 1);
        CombatRelicState Relic(RelicUnstableRebalanceModifier rule, string id = RelicId,
            string key = RelicKey, string[]? effects = null) => new(id, key,
                effects ?? [RelicUnstableRebalanceModel.Effect, RelicModel.Null],
                unstableRebalances: [rule]);
        CombatContext Context(params CombatRelicState[] relics)
        {
            UnityRng rng = UnityRng.Seed(4417);
            return new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 0, 10,
                relics: relics);
        }
        CombatUnit Unit(int id, CombatTeam team, int hp, int unstable, bool blastResistant = false,
            int attack = 0, bool relentless = false)
        {
            var statuses = new List<CombatStatus> { new("unstable", unstable, hidden: false, displayCategory: "Negative") };
            if (blastResistant) statuses.Add(new CombatStatus("blastresistant", 1));
            if (relentless) statuses.Add(new CombatStatus("relentless", 1));
            return new CombatUnit(id, "unstable-growth-" + id, team, attack, hp, hp, attack > 0, false, false,
                statuses, statusRegistry: statuses);
        }
        TrainCombatState Train(CombatContext context, params CombatUnit[] units) => new(
            [new RoomCombatState(0, false, units, [], context)], [], 0, context);
        void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        CombatRelicState owner = Relic(Rule());
        Require(RelicModel.Validate([owner]) == null &&
            RelicUnstableRebalanceModel.GetAdditionalGrowthRate(Context(owner), Unit(1, CombatTeam.Player, 30, 5),
                [Unit(1, CombatTeam.Player, 30, 5)]) == 5,
            "The unchanged UncontrolledGroup payload did not provide five Unstable growth.");

        UnstableGrowthResult grown = UnstableGrowthModel.StartTurn(Train(Context(owner),
            Unit(1, CombatTeam.Player, 30, 5), Unit(2, CombatTeam.Enemy, 30, 5)));
        Require(grown.Supported && grown.Outcome == RoomOutcome.Exchanged &&
            Stacks(grown.State!.Rooms.Single().Units.Single(unit => unit.Id == 1), "unstable") == 10 &&
            Stacks(grown.State.Rooms.Single().Units.Single(unit => unit.Id == 2), "unstable") == 10,
            "UncontrolledGroup did not grow both native teams by five at start of turn.");

        CombatContext upperFloorContext = Context(owner);
        UnstableGrowthResult upperFloor = UnstableGrowthModel.StartTurn(new TrainCombatState(
            [new RoomCombatState(2, false, [Unit(8, CombatTeam.Player, 30, 5)], [], upperFloorContext)],
            [], 0, upperFloorContext));
        Require(upperFloor.Supported && Stacks(upperFloor.State!.Rooms.Single().Units.Single(), "unstable") == 10,
            "Queued UncontrolledGroup growth failed when the active room used a nonzero RoomIndex.");

        RoomCombatResult roomTurn = RoomCombatModel.Resolve(Train(Context(owner),
            Unit(6, CombatTeam.Player, 120, 5, attack: 30), Unit(7, CombatTeam.Enemy, 60, 0, relentless: true)).Rooms.Single());
        Require(roomTurn.Supported && roomTurn.State!.Units.All(unit => unit.Id != 7) &&
            Stacks(roomTurn.State.Units.Single(unit => unit.Id == 6), "unstable") == 15,
            "The room combat model did not apply UncontrolledGroup between repeated team exchanges.");

        UnstableGrowthResult resistant = UnstableGrowthModel.StartTurn(Train(Context(owner),
            Unit(3, CombatTeam.Player, 5, 5, blastResistant: true)));
        Require(resistant.Supported && Stacks(resistant.State!.Rooms.Single().Units.Single(), "unstable") == 5,
            "Blast Resistant did not suppress the native Unstable start-of-turn callback.");

        UnstableGrowthResult lethal = UnstableGrowthModel.StartTurn(Train(Context(owner),
            Unit(4, CombatTeam.Player, 5, 5)));
        Require(!lethal.Supported && lethal.UnsupportedReason!.Contains("Lethal Unstable", StringComparison.Ordinal),
            "Lethal pre-growth Unstable was not kept behind the explicit unsupported boundary.");

        CombatRelicState noGrowth = Relic(Rule(growth: 0, explosion: 1),
            id: "b0575799-1511-489d-a05d-b5b2e6bf84c2", key: "NefariousOverflow",
            effects: [RelicUnstableRebalanceModel.Effect]);
        TrainCombatState untouched = Train(Context(noGrowth), Unit(5, CombatTeam.Player, 5, 5));
        UnstableGrowthResult noGrowthResult = UnstableGrowthModel.StartTurn(untouched);
        Require(noGrowthResult.Supported && ReferenceEquals(noGrowthResult.State, untouched),
            "A relic with no growth changed the existing Unstable turn flow.");

        Require(RelicModel.Validate([Relic(Rule(growth: 4))]) != null &&
            RelicModel.Validate([Relic(Rule(proc: 1))]) != null &&
            RelicModel.Validate([Relic(Rule(explosion: 1))]) != null &&
            RelicModel.Validate([Relic(Rule(onlyOne: true))]) != null &&
            RelicModel.Validate([Relic(Rule(), effects: [RelicUnstableRebalanceModel.Effect])]) != null &&
            RelicModel.Validate([Relic(Rule(), id: "00000000-0000-0000-0000-000000000001")]) != null,
            "An altered owner, effect order, or unsupported Unstable hook was accepted.");

        Console.WriteLine("ORIGINAL-UNCONTROLLED-GROUP-CHECKS PASS: exact owner payload, both-team +5 growth, nonzero room-index queue, Blast Resistant control, lethal boundary, and altered-payload rejection.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 191 &&
            root.GetProperty("OriginalUncontrolledGroupPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == Scenario &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The original UncontrolledGroup battle is incomplete or has native/model failures.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length >= 3, "The UncontrolledGroup witness lacks Mixology, carrier, or infusion plays.");
        BattleTurnState firstBefore = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState relic = firstBefore.Spawn.Train.Context!.Relics!.Single(item => item.DataId == RelicId);
        RelicUnstableRebalanceModifier rule = relic.UnstableRebalances!.Single();
        Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual(
                [RelicUnstableRebalanceModel.Effect, RelicModel.Null]) && rule.EffectIndex == 0 &&
            rule.SourceTeam == 255 && rule.AdditionalProcDamage == 0 && rule.AdditionalExplosionDamage == 0 &&
            rule.AdditionalGrowthRate == 5 && !rule.RequireOnlyOneUnitWithUnstable &&
            rule.StatusId == "unstable" && rule.StatusStacks == 1 && RelicModel.Validate([relic]) == null,
            "The runtime owner differs from the unchanged UncontrolledGroup payload.");

        FixtureValue[] callbacks = root.GetProperty("UncontrolledGroupGrowthCallbacks").EnumerateArray().ToArray();
        FixtureValue witness = callbacks.FirstOrDefault(callback => callback.GetProperty("Stage").GetString() ==
            "OnMonsterTeamTurnBegin" && callback.GetProperty("UnstableBefore").GetInt32() == 5 &&
            callback.GetProperty("UnstableAfter").GetInt32() == 10 && callback.GetProperty("HealthBefore").GetInt32() > 5);
        Require(witness.ValueKind != FixtureKind.Undefined,
            "No native OnMonsterTeamTurnBegin callback grew the carrier from five to ten stacks.");
        int unitId = witness.GetProperty("UnitId").GetInt32();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        bool replayed = turns.Any(turn =>
        {
            if (!turn.GetProperty("Predicted").GetProperty("Supported").GetBoolean() ||
                turn.GetProperty("Difference").ValueKind != FixtureKind.Null) return false;
            BattleTurnState actual = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleTurnState predicted = turn.GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
            return actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == unitId &&
                    Stacks(unit, "unstable") == 10) &&
                predicted.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == unitId &&
                    Stacks(unit, "unstable") == 10);
        });
        Require(replayed, "The whole-turn model did not replay the native five-to-ten growth state.");
        Console.WriteLine("ORIGINAL-UNCONTROLLED-GROUP NATIVE PASS: unchanged two-effect payload, direct +5 callback, and exact EndTurn replay.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static int Stacks(CombatUnit unit, string id) =>
        unit.Statuses.SingleOrDefault(status => status.Id == id)?.Stacks ?? 0;
}
