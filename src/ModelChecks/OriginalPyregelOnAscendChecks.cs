using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPyregelOnAscendChecks
{
    private const string DataId = "394fb724-046f-448f-9531-f9d6caadae77";
    private const string AssetKey = "PyregelOnAscend_DH";

    internal static void Run()
    {
        RelicRoomEntryStatus rule = Rule();
        CombatRelicState relic = Relic(rule);
        Require(RelicModel.Validate([relic]) == null, "Exact PyregelOnAscend_DH owner and movement parameters failed validation.");
        Require(relic.WithSpawnStatuses([]).RoomEntryStatuses!.Single().MovementTrigger == "PostAscension",
            "Cloning another relic rule discarded movement-status parameters.");
        Require(RelicModel.Validate([new CombatRelicState(DataId, AssetKey, relic.EffectTypes)]) != null,
            "Missing native movement-status parameters were accepted.");
        Require(RelicModel.Validate([Relic(Rule(restrictions: 1))]) != null,
            "A different native movement restriction mask was accepted.");
        Require(RelicModel.Validate([Relic(Rule(excludedSubtypes: ["clan"] ))]) != null,
            "Unexpected excluded subtypes were accepted for the original relic.");
        Require(RelicModel.Validate([new CombatRelicState("other", AssetKey, relic.EffectTypes,
            roomEntryStatuses: [rule])]) != null,
            "A movement-status payload detached from its original owner was accepted.");

        CombatUnit enemy = Unit(CombatTeam.Enemy);
        RoomCombatState room = new(2, true, [enemy], [], Room(enemy, relic).Context);
        RngDraw expected = room.Context!.BattleRng.Range(0, 1);
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        RoomCombatResult ascended = RoomCombatModel.ApplyQueuedCharacterTrigger(room,
            new(2, enemy, "PostAscension"), queue.Add);
        Require(ascended.Supported, ascended.UnsupportedReason ?? "Native Pyregel ascend application failed.");
        Require(ascended.State!.Context!.BattleRng.Equals(expected.State) &&
            ascended.State.Units.Single().Status("pyregel")?.Stacks == 2,
            "PostAscension did not add two Pyregel stacks and consume one native status-selection draw.");
        Require(queue.Any(callback => callback.Kind == "OnStatusEffectChanged") &&
            queue.Count(callback => callback.Kind == "PostAscension" && callback.RelicMovementStatusApplied) == 1,
            "Movement status callback or exactly-once resumed PostAscension dispatch was not queued.");

        foreach (var skipped in new[]
        {
            (Room(enemy, relic), new RoomCombatModel.QueuedCharacterTrigger(2, enemy, "PostDescension")),
            (Room(enemy, relic), new RoomCombatModel.QueuedCharacterTrigger(2, enemy, "PostAttemptedAscension")),
            (Room(Unit(CombatTeam.Player), relic), new RoomCombatModel.QueuedCharacterTrigger(2, Unit(CombatTeam.Player), "PostAscension")),
            (Room(Unit(CombatTeam.Enemy, new CombatStatus("immune", 1)), relic), new RoomCombatModel.QueuedCharacterTrigger(2, Unit(CombatTeam.Enemy, new CombatStatus("immune", 1)), "PostAscension")),
            (Room(Unit(CombatTeam.Enemy, new CombatStatus("purify", 1)), relic), new RoomCombatModel.QueuedCharacterTrigger(2, Unit(CombatTeam.Enemy, new CombatStatus("purify", 1)), "PostAscension")),
            (new RoomCombatState(2, false, [enemy], [], Room(enemy, relic).Context, preview: true), new RoomCombatModel.QueuedCharacterTrigger(2, enemy, "PostAscension")),
        })
        {
            RoomCombatResult ignored = RoomCombatModel.ApplyQueuedCharacterTrigger(skipped.Item1, skipped.Item2, _ => { });
            Require(ignored.Supported && ignored.State!.Context!.BattleRng.Equals(skipped.Item1.Context!.BattleRng) &&
                ignored.State.Units.All(unit => unit.Status("pyregel") == null),
            "A movement direction/team/validity/preview gate added status or consumed Battle RNG.");
        }

        Console.WriteLine("ORIGINAL-PYREGEL-ON-ASCEND-CHECKS PASS: exact owner and restrictions, deployment-phase ascend, native status draw, callback ordering, fail-closed metadata, and direction/team/validity/preview controls.");
    }

    internal static void Native(FixtureValue root)
    {
        if (!root.TryGetProperty("OriginalPyregelOnAscendPrepared", out FixtureValue prepared) || !prepared.GetBoolean()) return;
        Require(root.GetProperty("Schema").GetInt32() == 185 && root.GetProperty("ModifierScenario").GetString() == "bump" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "PyregelOnAscend_DH native battle is incomplete or from another probe schema.");

        FixtureValue[] records = root.GetProperty("BumpOperations").EnumerateArray().ToArray();
        Require(records.Length == 22, "PyregelOnAscend_DH fixture did not retain all native movement boundaries.");
        foreach (FixtureValue record in records)
        {
            TrainCombatState before = Read<TrainCombatState>(record.GetProperty("Before"));
            CheckOwner(before);
            var replay = BumpModel.Apply(before, Read<int[]>(record.GetProperty("Targets")),
                record.GetProperty("Amount").GetInt32(), Read<RoomPlayRule[]>(record.GetProperty("Rooms")),
                record.GetProperty("SourceCardId").GetInt32());
            Require(replay.Supported && JsonSerializer.Serialize(replay.State, ModelJson.Options) ==
                JsonSerializer.Serialize(Read<TrainCombatState>(record.GetProperty("After")), ModelJson.Options),
                "Bump replay differs from the native operation " + record.GetProperty("Label").GetString() + ": " + replay.UnsupportedReason);
        }

        CheckStatus(Record("enemy-up"), expectStatus: true);
        CheckStatus(Record("enemy-down"), expectStatus: false);
        CheckStatus(Record("player-up"), expectStatus: false);
        CheckStatus(Record("enemy-rooted"), expectStatus: false);
        CheckStatus(Record("enemy-immobile-before-rooted"), expectStatus: false);
        Require(Record("enemy-up").GetProperty("Dispatched").EnumerateArray().Any(item =>
            item.GetProperty("Kind").GetString() == "PostAscension") &&
            Record("enemy-down").GetProperty("Dispatched").EnumerateArray().Any(item =>
                item.GetProperty("Kind").GetString() == "PostDescension"),
            "Native movement controls did not dispatch the expected post-movement callbacks.");
        Console.WriteLine("ORIGINAL-PYREGEL-ON-ASCEND PASS: native enemy ascend gains 2 Pyregel; descend, player, rooted and immobile controls; 22 exact Bump replays.");

        FixtureValue Record(string label) => records.Single(item => item.GetProperty("Label").GetString() == label);
    }

    private static void CheckOwner(TrainCombatState state)
    {
        CombatRelicState relic = state.Context!.Relics!.Single(item => item.DataId == DataId);
        RelicRoomEntryStatus rule = relic.RoomEntryStatuses!.Single();
        Require(relic.AssetKey == AssetKey && relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
            relic.EffectTypes.SequenceEqual([RelicModel.AddStatusOnUnitMovedUpDown]) && rule.EffectIndex == 0 &&
            rule.TargetRoomIndex == -1 && !rule.TargetMonsters && rule.TargetHeroes && !rule.FirstWaveOnly &&
            rule.MovementTrigger == "PostAscension" && rule.MovementRestrictions == 5 && rule.ExcludedSubtypes.Count == 0 &&
            rule.Statuses.Count == 1 && rule.Statuses[0].Id == "pyregel" && rule.Statuses[0].Stacks == 2,
            "Captured PyregelOnAscend_DH identity or native movement parameters changed.");
    }

    private static void CheckStatus(FixtureValue record, bool expectStatus)
    {
        int actorId = record.GetProperty("Targets")[0].GetInt32();
        CombatUnit actorBefore = Actor(Read<TrainCombatState>(record.GetProperty("Before")), actorId);
        CombatUnit actorAfter = Read<CombatUnit[]>(record.GetProperty("TargetsAfter")).Single(unit => unit.Id == actorId);
        int expected = actorBefore.Status("pyregel")?.Stacks ?? 0;
        if (expectStatus) expected += 2;
        Require((actorAfter.Status("pyregel")?.Stacks ?? 0) == expected,
            record.GetProperty("Label").GetString() + " applied the wrong Pyregel stack change.");
    }

    private static RelicRoomEntryStatus Rule(int restrictions = 5, IReadOnlyList<string>? excludedSubtypes = null) =>
        new(0, -1, false, true, false, [new CombatStatus("pyregel", 2)],
            restrictions == 5 ? "PostAscension" : null, restrictions, excludedSubtypes);

    private static CombatRelicState Relic(RelicRoomEntryStatus rule) => new(DataId, AssetKey,
        [RelicModel.AddStatusOnUnitMovedUpDown], isCovenant: false, disallowedInPlacementPhase: false,
        roomEntryStatuses: [rule]);

    private static CombatUnit Unit(CombatTeam team, params CombatStatus[] statuses) =>
        new(1, team == CombatTeam.Player ? "player" : "enemy", team, 2, 20, 20, true, false, false,
            statuses, statusRegistry: statuses, statusDictionary: new([], []));

    private static RoomCombatState Room(CombatUnit actor, CombatRelicState relic) =>
        new(2, false, [actor], [], new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13),
            0, 1, 10, relics: [relic]));

    private static CombatUnit Actor(TrainCombatState state, int id) => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static T Read<T>(FixtureValue value) => value.Deserialize<T>()!;
    private static void Require(bool valid, string message)
    { if (!valid) throw new System.InvalidOperationException(message); }
}
