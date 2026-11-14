using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChainedRemoteQueueChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "chained-remote-queue" && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Incomplete native remote Chained battle.");
        FixtureValue evidence = root.GetProperty("ChainedBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(),
            requests = evidence.GetProperty("Requests").EnumerateArray().ToArray(),
            propagations = evidence.GetProperty("Propagations").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("RemoteQueue").GetBoolean() &&
            evidence.GetProperty("CompletedControlPlays").GetInt32() == 5 && evidence.GetProperty("RemoteActivations").GetInt32() == 3 &&
            ids.Length == 5 && actions.Length == 8 && requests.Length == 23 && propagations.Length == 136 &&
            requests.Concat(propagations).All(row => row.GetProperty("Completed").GetBoolean()), "Missing complete remote controls/observations.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatRelicState owner = Before(0).Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == "d0294a77-7204-4e9e-9b22-7f38e0d59ad0");
        Require(owner.AssetKey == "ChainedDamageIncrease" && owner.EffectTypes.SequenceEqual([RelicModel.AddStatusOnSpawn]) &&
            owner.SpawnStatuses is { Count: 1 } && owner.SpawnStatuses[0] is { TargetPlayer: false, TargetEnemy: true } &&
            owner.SpawnStatuses[0].Statuses.Single() is { Id: "chained", Stacks: 1 }, "Original Chained spawn owner differs.");
        int victim = After(0).Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id,
            activator = After(3).Spawn.Train.Rooms[2].Units.Single(unit => unit.SpawnerCardId == ids[3]).Id;
        foreach (int index in new[] { 5, 6, 7 })
        {
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(action.RoomIndex == 0 && (action.ActivatorRoomIndex == null || action.ActivatorRoomIndex == 2) &&
                Before(index).Spawn.Train.Rooms[2].Units.Any(unit => unit.Id == activator) && action.ActivatorUnitId == activator && action.TargetUnitId == victim &&
                Before(index).Spawn.Turn == index - 4 && After(index).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == victim).Health == 4,
                "Remote ability source, target, timing or post-Bloodshed health differs.");
        }
        foreach (FixtureValue row in requests)
            Require(row.GetProperty("DamageType").GetInt32() == ChainedStatusModel.DamageType &&
                row.GetProperty("NullCard").GetBoolean() && row.GetProperty("NullRelic").GetBoolean() &&
                !row.GetProperty("QueueRunning").GetBoolean() && Unit(row, "Before").Team == CombatTeam.Enemy && Unit(row, "Source").Team == CombatTeam.Player,
                "Native damage attribution or outer queue protocol differs.");
        Require(requests.Any(row => !Preview(row) && row.GetProperty("Damage").GetInt32() == 3 && Unit(row, "Source").Id == victim && Unit(row, "Source").Health == 38) &&
            requests.Any(row => !Preview(row) && row.GetProperty("Damage").GetInt32() == 50 && Unit(row, "Source").Id == victim && Unit(row, "Source").Health == 4),
            "Native Chained ran before Bloodshed healing or lost pre-Tenacious input.");
        FixtureValue[] timeline = evidence.GetProperty("Timeline").EnumerateArray().Where(row => !Preview(row)).ToArray();
        Require(timeline.Any(row => row.GetProperty("Kind").GetString() == "OnBloodshed" && row.GetProperty("QueueRunning").GetBoolean()) &&
            timeline.Any(row => row.GetProperty("Kind").GetString() == "Propagation" && !row.GetProperty("QueueRunning").GetBoolean()),
            "Missing native queue dispatch and post-queue propagation evidence.");

        // Independently enter just the first damage effect with a real outer
        // train queue. Its borrowed attacker and every remote room must survive
        // the callback checkpoint; this is separate from full card activation.
        TrainCombatState initial = Before(5).Spawn.Train; string unchanged = JsonSerializer.Serialize(initial, ModelJson.Options);
        PlayCardAction first = actions[5].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var pending = new List<RoomCombatModel.QueuedCharacterTrigger>(); int checkpoints = 0;
        TrainSpellResult single = CardSpellModel.ApplyNativeDamageStep(initial, 0, victim, 5, first.CardInstanceId,
            Array.Empty<RoomCombatModel.QueuedCharacterTrigger>(), activator, enqueueCharacterTrigger: pending.Add,
            drainQueuedTriggers: frame => {
                checkpoints++; var queue = pending.ToList(); pending.Clear();
                TrainCombatResult result = TrainCombatModel.ApplyCharacterQueue(frame, queue); return result.State;
            });
        Require(single.Supported && checkpoints >= 2 && pending.Count == 0 &&
            single.State!.Rooms[0].Units.Single(unit => unit.Id == victim).Health == 38 &&
            single.State.Rooms[2].Units.Single(unit => unit.Id == activator).Health == initial.Rooms[2].Units.Single(unit => unit.Id == activator).Health &&
            JsonSerializer.Serialize(initial, ModelJson.Options) == unchanged, "Independent damage checkpoint lost healing, borrowed actor or input immutability.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine("CHAINED-REMOTE-QUEUE PASS: original owner, three two-hit remote abilities, native Bloodshed healing before Chained, 136 propagations/23 requests, borrowed actor checkpoint and immutable source, unfinished/removed deaths, full successor/RNG replay and 16 independent battle branches. Uncaptured movement/removal continuations remain guarded.");
    }
    private static CombatUnit Unit(FixtureValue row, string key) => row.GetProperty(key).Deserialize<CombatUnit>()!;
    private static bool Preview(FixtureValue row) => row.GetProperty("Preview").GetBoolean();
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
