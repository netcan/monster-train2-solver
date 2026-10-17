using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalNimsOathChecks
{
    private const string OwnerId = "727e69a6-3a8c-4029-895c-99e9dac0bc0a";
    private const int Schema = 210;

    public sealed class RoomTest(int turn, int roomIndex, bool result)
    {
        public int Turn { get; } = turn;
        public int RoomIndex { get; } = roomIndex;
        public bool Result { get; } = result;
    }

    public sealed class TimingTest(int turn, bool result, RoomTest[] roomTests)
    {
        public int Turn { get; } = turn;
        public bool Result { get; } = result;
        public RoomTest[] RoomTests { get; } = roomTests;
    }

    public sealed class Range(int minimum, int maximum, string rng, int result)
    {
        public int Minimum { get; } = minimum;
        public int Maximum { get; } = maximum;
        public string Rng { get; } = rng;
        public int Result { get; } = result;
    }

    public sealed class Application(int turn, TrainCombatState before, TrainCombatState? after,
        Range[] ranges, RoomTest[] roomTests, int[] modelCandidateRooms, int? modelSelectedRoom,
        bool modelSupported, string? modelUnsupportedReason, string? difference, bool completed, string? error)
    {
        public int Turn { get; } = turn;
        public TrainCombatState Before { get; } = before;
        public TrainCombatState? After { get; } = after;
        public Range[] Ranges { get; } = ranges;
        public RoomTest[] RoomTests { get; } = roomTests;
        public int[] ModelCandidateRooms { get; } = modelCandidateRooms;
        public int? ModelSelectedRoom { get; } = modelSelectedRoom;
        public bool ModelSupported { get; } = modelSupported;
        public string? ModelUnsupportedReason { get; } = modelUnsupportedReason;
        public string? Difference { get; } = difference;
        public bool Completed { get; } = completed;
        public string? Error { get; } = error;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == Schema &&
            root.GetProperty("OriginalNimsOathSchema").GetInt32() == Schema &&
            root.GetProperty("ModifierScenario").GetString() == "original-nims-oath" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Nims Oath battle is incomplete or from an unexpected capture schema.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nims Oath battle has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("OriginalNimsOath");
        Require(evidence.GetProperty("OwnerId").GetString() == OwnerId &&
            evidence.GetProperty("AssetKey").GetString() == "Inkbound_NimsOath" &&
            evidence.GetProperty("RoomMode").GetString() == "ApplyToRandomRoom" &&
            evidence.GetProperty("TimingEffect").GetString() == RelicStartTurnStatusModel.Effect,
            "Original Nims Oath identity or timing payload changed.");
        if (evidence.TryGetProperty("NativeInterfaces", out FixtureValue interfaceValue))
        {
            string[] interfaces = interfaceValue.Deserialize<string[]>()!;
            Require(interfaces.Contains("ITurnPhaseStartOfPlayerTurnBeforeDrawRelicEffect") &&
                interfaces.Contains("ITurnPhaseTimingRelicEffect") && !interfaces.Contains("ICharacterKilledRelicEffect"),
                "The native effect interface evidence no longer bounds its OnKill callback behavior.");
        }
        FixtureValue trigger = evidence.GetProperty("TriggerMetadata");
        Require(trigger.GetProperty("Kind").GetString() == "OnKill" &&
            trigger.GetProperty("Effect").GetString() == "CardEffectAddStatusEffect" &&
            trigger.GetProperty("Target").GetString() == "Self" &&
            trigger.GetProperty("Team").GetString() == "Monsters" &&
            trigger.GetProperty("Status").GetString() == "conduit" &&
            trigger.GetProperty("Stacks").GetInt32() == 1 &&
            !trigger.GetProperty("RuntimeCallbackSupported").GetBoolean(),
            "The preserved but unsupported OnKill/Conduit metadata changed.");

        TimingTest[] timing = root.GetProperty("RelicNimsOathTimingTests").EnumerateArray()
            .Select(item => item.Deserialize<TimingTest>()!).ToArray();
        RoomTest[] roomTests = root.GetProperty("RelicNimsOathRoomTests").EnumerateArray()
            .Select(item => item.Deserialize<RoomTest>()!).ToArray();
        Require(timing.Any(item => item.Turn == 0 && !item.Result && item.RoomTests.Length >= 1 &&
                item.RoomTests.All(test => !test.Result)) &&
            timing.Any(item => item.Turn > 0 && item.Result) &&
            roomTests.Any(item => item.Result) && roomTests.Any(item => !item.Result),
            "Native start-turn timing lacks an empty-floor negative or eligible-room positive control.");
        Require(timing.All(item => item.Result == item.RoomTests.Any(test => test.Result)),
            "Native timing eligibility differs from its per-room TestEffects results.");

        Application[] applications = root.GetProperty("RelicNimsOathApplications").EnumerateArray()
            .Select(item => item.Deserialize<Application>()!).ToArray();
        Require(applications.Length >= 4 && applications.All(item => item.Completed && item.Error == null &&
            item.After != null && item.ModelSupported && item.ModelUnsupportedReason == null && item.Difference == null),
            "Native Nims Oath applications are incomplete, unsupported or divergent.");
        foreach (Application application in applications)
        {
            int[] nativeCandidates = application.RoomTests.Where(test => test.Result).Select(test => test.RoomIndex)
                .Distinct().Order().ToArray();
            Require(nativeCandidates.SequenceEqual(application.ModelCandidateRooms),
                "Native enabled-room candidates differ from the independent model on turn " + application.Turn + ".");
            Range[] draws = application.Ranges.Where(item => item.Rng == "Battle").ToArray();
            Require(draws.Length == 1 && draws[0].Minimum == 0 && draws[0].Maximum == nativeCandidates.Length &&
                draws[0].Result >= 0 && draws[0].Result < nativeCandidates.Length,
                "Nims Oath did not make exactly one bounded Battle RNG room selection.");
            int nativeSelectedRoom = nativeCandidates[draws[0].Result];
            Require(application.ModelSelectedRoom == nativeSelectedRoom,
                "Native Battle RNG selected a different Nims Oath room on turn " + application.Turn + ".");

            RoomPlayRule[] roomRules = RoomRules(application);
            RelicStartTurnStatusResult predicted = RelicStartTurnStatusModel.Apply(application.Before,
                roomRules, application.Turn);
            Require(predicted.Supported && predicted.CandidateRooms.SequenceEqual(nativeCandidates) &&
                predicted.SelectedRoom == nativeSelectedRoom && Same(predicted.State!, application.After!),
                "Independent Nims Oath replay differs on turn " + application.Turn + ".");
            (int Room, int UnitId, CombatTeam Team, int Delta)[] changes = DazedChanges(application.Before, application.After!);
            Require(changes.Length == 1 && changes[0].Room == nativeSelectedRoom &&
                changes[0].Team == CombatTeam.Enemy && changes[0].Delta == 1,
                "Native Nims Oath did not add exactly one Dazed stack to the selected room's front enemy.");
        }
        Require(applications.Any(item => item.ModelCandidateRooms.Length == 1) &&
            applications.Any(item => item.ModelCandidateRooms.Length >= 2),
            "Native Battle RNG did not exercise both single-room and multi-room candidate pools.");

        VerifyControls(applications);
        BattleTurnChecks.Native(root);
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-NIMS-OATH-NATIVE PASS: {applications.Length} exact start-turn applications, TestEffects room controls, Battle RNG and unsupported OnKill metadata boundary.");
    }

    private static void VerifyControls(Application[] applications)
    {
        Application first = applications[0];
        RoomPlayRule[] firstRules = RoomRules(first);
        CombatContext context = first.Before.Context!;
        CombatContext withoutOwnerContext = context.WithRelics(context.Relics!.Where(relic => relic.DataId != OwnerId).ToArray());
        TrainCombatState withoutOwner = WithContext(first.Before, withoutOwnerContext);
        RelicStartTurnStatusResult ablated = RelicStartTurnStatusModel.Apply(withoutOwner, firstRules, first.Turn);
        Require(ablated.Supported && Same(ablated.State!, withoutOwner),
            "Removing Nims Oath did not remove its start-turn effect.");

        RoomPlayRule[] disabled = firstRules.Select(rule => new RoomPlayRule(rule.RoomIndex,
            rule.PlayerCapacity, rule.PlayerSlots, false, rule.SummonBlocked, rule.IsPyre, rule.EnemyCapacity)).ToArray();
        RelicStartTurnStatusResult noCandidates = RelicStartTurnStatusModel.Apply(first.Before, disabled, first.Turn);
        Require(noCandidates.Supported && noCandidates.CandidateRooms.Count == 0 && noCandidates.SelectedRoom == null &&
            Same(noCandidates.State!, first.Before), "No eligible room consumed RNG or changed combat state.");

        RelicStartTurnStatusResult preview = RelicStartTurnStatusModel.Apply(first.Before, firstRules,
            first.Turn, preview: true);
        Require(preview.Supported && preview.CandidateRooms.Count == 0 && Same(preview.State!, first.Before),
            "Random-room Nims Oath unexpectedly ran in preview.");

        RelicStartTurnStatusResult placement = RelicStartTurnStatusModel.Apply(first.Before, firstRules,
            first.Turn, placement: true);
        RelicStartTurnStatusResult normal = RelicStartTurnStatusModel.Apply(first.Before, firstRules, first.Turn);
        Require(placement.Supported && normal.Supported && Same(placement.State!, normal.State!),
            "Nims Oath was incorrectly excluded from placement despite its native owner flag.");

        Application multiRoom = applications.First(item => item.ModelCandidateRooms.Length >= 2);
        RoomPlayRule[] multiRoomRules = RoomRules(multiRoom);
        int excluded = multiRoom.ModelCandidateRooms[0];
        RoomPlayRule[] restricted = multiRoomRules.Select(rule => rule.RoomIndex == excluded
            ? new RoomPlayRule(rule.RoomIndex, rule.PlayerCapacity, rule.PlayerSlots, false,
                rule.SummonBlocked, rule.IsPyre, rule.EnemyCapacity)
            : rule).ToArray();
        RelicStartTurnStatusResult singleRoom = RelicStartTurnStatusModel.Apply(multiRoom.Before, restricted, multiRoom.Turn);
        Require(singleRoom.Supported && singleRoom.CandidateRooms.SequenceEqual(multiRoom.ModelCandidateRooms.Skip(1)) &&
            singleRoom.SelectedRoom == multiRoom.ModelCandidateRooms[1],
            "Disabling a room did not constrain the random candidate pool.");
    }

    private static RoomPlayRule[] RoomRules(Application application)
    {
        // The native Apply callback records TestEffects only after checking IsRoomEnabled.
        var testedRooms = application.RoomTests.Select(test => test.RoomIndex).ToHashSet();
        int pyreRoom = application.Before.Rooms.Max(room => room.RoomIndex);
        return application.Before.Rooms.Select(room => new RoomPlayRule(room.RoomIndex, 0, 0,
            testedRooms.Contains(room.RoomIndex), false, room.RoomIndex == pyreRoom)).ToArray();
    }

    private static (int Room, int UnitId, CombatTeam Team, int Delta)[] DazedChanges(
        TrainCombatState before, TrainCombatState after)
    {
        var oldUnits = before.Rooms.SelectMany(room => room.Units.Select(unit =>
            (Key: (room.RoomIndex, unit.Id), Unit: unit))).ToDictionary(item => item.Key, item => item.Unit);
        var changes = new System.Collections.Generic.List<(int, int, CombatTeam, int)>();
        foreach (RoomCombatState room in after.Rooms)
        {
            foreach (CombatUnit unit in room.Units)
            {
                if (!oldUnits.TryGetValue((room.RoomIndex, unit.Id), out CombatUnit? old)) continue;
                int delta = (unit.Status("dazed")?.Stacks ?? 0) - (old.Status("dazed")?.Stacks ?? 0);
                if (delta != 0) changes.Add((room.RoomIndex, unit.Id, unit.Team, delta));
            }
        }
        return changes.ToArray();
    }

    private static TrainCombatState WithContext(TrainCombatState source, CombatContext context) => new(
        source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
            room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
        source.Movement, source.EnemySlotsPerRoom, context);

    private static bool Same(TrainCombatState expected, TrainCombatState actual) => ModelJson.Difference(
        JsonSerializer.Serialize(new { expected.Rooms, Context = BattleTurnChecks.ComparableContext(expected.Context) }, ModelJson.Options),
        JsonSerializer.Serialize(new { actual.Rooms, Context = BattleTurnChecks.ComparableContext(actual.Context) }, ModelJson.Options)) == null;

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
