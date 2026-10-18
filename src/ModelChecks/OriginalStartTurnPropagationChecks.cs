using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStartTurnPropagationChecks
{
    private static RelicResourceEffect Resource(int adjustment = 1)
    {
        var action = new CardActionEffect("AdjustAllStatusEffects", "Room", adjustment, false, true, [],
            tests: new(true, false, false, false, true), filters: new("Both", [], [], false, "", []), statusAdjustmentFilter: 1);
        var rule = new RelicStartTurnRoomEffectRule(0, 1, 0, 0, true, false, false, false, action, [], false, false, false, false, []);
        return new(0, RelicStartTurnPropagationModel.Effect, 1, 0, 1, true, startTurnRoomEffect: rule);
    }
    private static CombatRelicState Owner(int adjustment = 1) => new(RelicStartTurnPropagationModel.DataId, RelicStartTurnPropagationModel.AssetKey,
        [RelicStartTurnPropagationModel.Effect], isCovenant: false, disallowedInPlacementPhase: false, resourceEffects: [Resource(adjustment)]);
    private static RoomPlayRule[] Rooms() => Enumerable.Range(0, 4).Select(index => new RoomPlayRule(index, 5, 7, true, false, index == 3)).ToArray();
    private static CombatStatus Status(string id, int stacks, bool propagatable, bool excludeMonster = false) => new(id, stacks,
        stackable: true, hidden: false, propagatable: propagatable, excludeHeroPropagation: false, excludeMonsterPropagation: excludeMonster);
    internal static void Run()
    {
        CombatStatus armor = Status("armor", 2, false), spikes = Status("spikes", 3, true);
        CombatStatus weakness = Status("spell weakness", 2, true, true), immobile = Status("immobile", 1, false);
        var rng = UnityRng.Seed(231);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 10, 10,
            statusRules: [armor.WithStacks(0), spikes.WithStacks(0), weakness.WithStacks(0), immobile.WithStacks(0)], relics: [Owner()],
            statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [], queryFrame: new(energy: 0, runningCombat: true, turn: 1),
            energyState: new(10, 0, 0, "MonsterTurn", true), resources: new(3, 1, 5, 10, 10, "pyre", 0, 20, 0, 0, [], [], []));
        CombatUnit Unit(int id, CombatTeam team, CombatStatus[] statuses, bool pyre = false) => new(id, "propagation-unit-" + id,
            team, 0, 20, 20, false, pyre, false, statuses, statusRegistry: statuses);
        var player = Unit(1, CombatTeam.Player, [armor, spikes, weakness, immobile]);
        var enemy = Unit(2, CombatTeam.Enemy, [armor]); var other = Unit(3, CombatTeam.Player, [spikes]);
        var pyre = Unit(4, CombatTeam.Player, [armor], true);
        var train = new TrainCombatState([new(0, false, [player, enemy], [], context), new(1, false, [other], [], context),
            new(2, false, [], [], context), new(3, false, [pyre], [], context)], [], 7, context);
        Require(RelicModel.Validate(context.Relics) == null && RelicModel.Validate([Owner(2)]) != null, "Original propagation payload validation failed.");
        string parent = JsonSerializer.Serialize(train, ModelJson.Options);
        var result = RelicStartTurnStatusModel.Apply(train, Rooms(), 1);
        Require(result.Supported, "Pure start-turn propagation failed: " + result.UnsupportedReason);
        CombatUnit changed = result.State!.Rooms[0].Units.Single(unit => unit.Id == 1);
        Require(changed.Status("armor")!.Stacks == 2 && changed.Status("spikes")!.Stacks == 4 && changed.Status("spell weakness")!.Stacks == 2 &&
            changed.Status("immobile")!.Stacks == 1 && result.State.Rooms[0].Units.Single(unit => unit.Id == 2).Status("armor")!.Stacks == 2 &&
            result.State.Rooms[1].Units.Single().Status("spikes")!.Stacks == 4 && result.State.Rooms[3].Units.Single().Status("armor")!.Stacks == 2 &&
            result.State.Context!.BattleRng.Equals(rng) && result.CandidateRooms.SequenceEqual([0, 1, 2]),
            "Propagation must affect existing permitted friendly statuses on all three floors and preserve enemies/Pyre/exclusions/RNG.");
        foreach (var gate in new[] { (true, false), (false, true) })
        {
            var allowed = RelicStartTurnStatusModel.Apply(train, Rooms(), 1, gate.Item1, gate.Item2);
            Require(allowed.Supported && JsonSerializer.Serialize(allowed.State, ModelJson.Options) == JsonSerializer.Serialize(result.State, ModelJson.Options),
                "The original propagation owner permits placement and preview.");
        }
        var emptyTrain = new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, false,
            room.RoomIndex == 3 ? [pyre] : [], [], context)).ToArray(), [], 7, context);
        var empty = RelicStartTurnStatusModel.Apply(emptyTrain, Rooms(), 1);
        Require(empty.Supported && JsonSerializer.Serialize(empty.State, ModelJson.Options) == JsonSerializer.Serialize(emptyTrain, ModelJson.Options), "Empty-room propagation changed state/RNG.");
        var twice = RelicStartTurnStatusModel.Apply(result.State, Rooms(), 2);
        Require(twice.Supported && twice.State!.Rooms[0].Units.Single(unit => unit.Id == 1).Status("spikes")!.Stacks == 5, "Propagation did not repeat on the next turn.");
        var absent = CardSpellModel.WithContext(train, context.WithRelics([]));
        Require(ReferenceEquals(RelicStartTurnStatusModel.Apply(absent, null, 1).State, absent), "Absent propagation owner changed state.");
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(RelicStartTurnStatusModel.Apply(train, Rooms(), 1).State, ModelJson.Options) ==
            JsonSerializer.Serialize(result.State, ModelJson.Options), "Parallel propagation branch diverged."));
        Require(JsonSerializer.Serialize(train, ModelJson.Options) == parent, "Propagation mutated its parent.");
        Console.WriteLine("START-TURN-PROPAGATION-PURE PASS: all-floor friendly targeting, propagatable/team-excluded/persistent controls, enemy/Pyre/empty exclusions, placement/preview permissions, repeat turns, strict payload and immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 231 && root.GetProperty("OriginalStartTurnPropagationSchema").GetInt32() == 231 &&
            root.GetProperty("ModifierScenario").GetString() == "original-start-turn-propagation" && root.GetProperty("OriginalStartTurnPropagationPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalStartTurnPropagationCaptureFailures",
                "OriginalStartTurnPropagationMismatches", "OriginalStartTurnPropagationUnsupported", "OriginalStartTurnPropagationPending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Native start-turn propagation acceptance failed.");
        Require(root.GetProperty("Actions").GetArrayLength() == 2 && root.GetProperty("Actions").EnumerateArray().All(row => row.GetProperty("ScenarioAction").GetBoolean()), "Missing complete deployment policy.");
        int emptyControls = 0, tests = 0;
        foreach (var row in root.GetProperty("RelicStartTurnPropagationTests").EnumerateArray())
        {
            Require(row.GetProperty("Result").GetBoolean(), "Non-strict propagation Test must pass even on empty floors.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test state/RNG");
            if (row.GetProperty("EmptyControl").GetBoolean())
            {
                emptyControls++; Require(row.GetProperty("Before").Deserialize<TrainCombatState>()!.Rooms.Where(room => room.RoomIndex < 3).All(room => room.Units.Count == 0), "Empty Test control has actors.");
            }
            tests++;
        }
        int applications = 0, changes = 0, excluded = 0; var floors = new HashSet<int>();
        foreach (var row in root.GetProperty("RelicStartTurnPropagationApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            BattlePlayRules rules = row.GetProperty("Rules").Deserialize<BattlePlayRules>()!;
            var owner = before.Context!.Relics!.Single(item => item.DataId == RelicStartTurnPropagationModel.DataId);
            Require(row.GetProperty("Completed").GetBoolean() && RelicModel.Validate([owner]) == null, "Incomplete native propagation coroutine or original payload.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = RelicStartTurnPropagationModel.Apply(before, rules.Rooms, owner.ResourceEffects!.Single());
                Require(result.Supported, "Unsupported native propagation: " + result.UnsupportedReason);
                Equal(result.State, actual, "all rooms and complete state");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(before.Context.BattleRng.Equals(actual.Context!.BattleRng), "Native all-room propagation consumed Battle RNG.");
            foreach (RoomCombatState room in before.Rooms)
                foreach (CombatUnit unit in room.Units)
                {
                    CombatUnit after = actual.Rooms.SelectMany(item => item.Units).Single(item => item.Id == unit.Id);
                    if (room.RoomIndex == 3 || unit.Team != CombatTeam.Player) { Equal(unit, after, "enemy/Pyre control"); continue; }
                    foreach (CombatStatus status in unit.Statuses.Where(status => status.Stacks > 0))
                    {
                        bool eligible = status.Propagatable == true && status.ExcludeMonsterPropagation == false;
                        Require((after.Status(status.Id)?.Stacks ?? 0) == status.Stacks + (eligible ? 1 : 0), "Native status propagation/exclusion differs for " + status.Id);
                        if (eligible) { changes++; floors.Add(room.RoomIndex); } else excluded++;
                    }
                }
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native propagation replay mutated its parent."); applications++;
        }
        Require(emptyControls == 1 && applications >= 2 && tests >= applications + 1 && changes >= 6 && excluded >= 4 && floors.SetEquals([0, 1]), "Missing repeated multi-floor propagation, empty Test or excluded status controls.");
        Console.WriteLine($"ORIGINAL-START-TURN-PROPAGATION-NATIVE PASS: {applications} all-room applications, {changes} exact +1 status changes, {excluded} excluded statuses, empty-floor Test, unchanged RNG/enemies/Pyre and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string phase)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Start-turn propagation " + phase + " differs: " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
