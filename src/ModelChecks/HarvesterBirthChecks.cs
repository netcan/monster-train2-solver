using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HarvesterBirthChecks
{
    internal static void Native(FixtureValue fixture)
    {
        BattleTurnState initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        PlayerWorld(initial);
        Movement(fixture);
        EnemySpawningChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
    }

    private static void PlayerWorld(BattleTurnState initial)
    {
        Require(initial.PlayRules!.Cards.Any(card => card.SpawnUnit?.AssetKey == "MonsterUpbeatWarbler" &&
            card.SpawnUnit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Enchantment != null))) &&
            initial.Spawn.Train.Context?.Enchantments?.AutomaticLifecycle == true,
            "The natural player enchanter must enable the automatic world before it is played.");
    }

    internal static void Component(FixtureValue observation, FixtureValue fixture)
    {
        foreach (string counter in new[] { "Mismatches", "Unsupported", "CaptureFailures", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Native component counter failed: " + counter);
        Require(observation.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            observation.GetProperty("NativeCompleted").GetBoolean() &&
            observation.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Missing fresh native birth boundary observation.");
        FixtureValue request = observation.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == "Tower trigger targeting requires complete-train unit snapshots." &&
            request.GetProperty("IsEndTurn").GetBoolean(), "Historical tower rejection was not preserved.");
        FixtureValue[] prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        FixtureValue[] rows = observation.GetProperty("Rows").EnumerateArray().ToArray();
        Require(prefix.Length == 26 && rows.Length == 27, "Incomplete original native prefix.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        PlayerWorld(current);
        for (int i = 0; i < rows.Length; i++)
        {
            FixtureValue row = rows[i], step = i == prefix.Length ? request : prefix[i];
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(BattleTurnChecks.Comparable(current) == BattleTurnChecks.Comparable(before) &&
                row.GetProperty("Boundary").GetBoolean() == (i == prefix.Length) &&
                row.GetProperty("Action").ContentEquals(step.GetProperty("Action")) && row.GetProperty("IsEndTurn").GetBoolean() == end,
                "Native component root/input chain differs at " + i);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleTurnState predicted = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(end ? null : step.GetProperty("Action").Deserialize<PlayCardAction>(), end)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && BattleTurnChecks.Comparable(predicted) == BattleTurnChecks.Comparable(actual) &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                BattleTurnChecks.Comparable(row.GetProperty("Expected").Deserialize<BattleTurnState>()!) == BattleTurnChecks.Comparable(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Native component prediction differs at " + i);
            if (i == prefix.Length)
                Require(BattleTurnChecks.Comparable(before) == BattleTurnChecks.Comparable(request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!) &&
                    actual.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).Last().AssetKey == "Boss_Level4_Harvester",
                    "Native birth did not finish with the original self/back rearrangement.");
            current = actual;
        }
        FixtureValue frozen = observation.GetProperty("PlayerAdvisor").GetProperty("FrozenObservation");
        Require(frozen.GetProperty("Paused").GetBoolean() && frozen.GetProperty("SearchesAfterPause").GetInt64() == 0 &&
            frozen.GetProperty("FinalAdviceAbsent").GetBoolean(), "Component replay performed background planning.");
        Movement(fixture, requireCrossRoom: true);
        EnemySpawningChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        Console.WriteLine("HARVESTER-COMPONENT PASS: all27 native roots/inputs/predictions/successors/RNG, automatic player aura world, complete birth/rearrangement and zero searches after pausing.");
    }

    internal static void Movement(FixtureValue fixture, bool requireCrossRoom = false)
    {
        FixtureValue record = fixture.GetProperty("BumpOperations").EnumerateArray().Single();
        Require(record.GetProperty("Label").GetString() == "OriginalHarvesterBirth" &&
            record.GetProperty("Amount").GetInt32() == -2 && record.GetProperty("SourceCardId").GetInt32() == 0 &&
            record.GetProperty("AuthoredUpgradeId").GetString() == "b778dc25-2f26-41ad-8379-26e97aa71837",
            "Missing original Harvester birth observation, including its unused authored upgrade field.");
        TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = record.GetProperty("After").Deserialize<TrainCombatState>()!;
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        CombatUnit actor = before.Rooms[0].Units.Single(unit => unit.AssetKey == "Boss_Level4_Harvester");
        CombatTrigger trigger = actor.Triggers.Single(item => item.Kind == "OnSpawnNotFromCard");
        Require(HarvesterBirthModel.IsTrigger(actor, trigger) && trigger.HasTriggered, "Wrong native marked birth trigger.");
        CardActionEffect effect = record.GetProperty("EffectDefinition").Deserialize<CardActionEffect>()!;
        Require(JsonSerializer.Serialize(effect) == JsonSerializer.Serialize(trigger.Effects[0].Action), "Native live/definition movement payload differs.");
        int[] targets = record.GetProperty("Targets").Deserialize<int[]>()!;
        bool crossRoom = before.Rooms.Skip(1).Take(2).Any(room => room.Units.Any(unit => targets.Contains(unit.Id)));
        Require(CardTargetModel.Collect(before, 0, effect, [], selfUnitId: actor.Id).UnitIds.SequenceEqual(targets) &&
            (!requireCrossRoom || crossRoom) &&
            targets.All(id => before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id).Team == CombatTeam.Enemy),
            "Harvester did not exercise ordered enemy tower targeting across floors.");
        BumpResult result = BumpModel.Apply(before, targets, -2, record.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!,
            0, null, deferCharacterCallbacks: true);
        Require(result.Supported, "Native Harvester Bump unsupported: " + result.UnsupportedReason);
        string predicted = JsonSerializer.Serialize(result.State, ModelJson.Options), expected = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(predicted == expected, "Native Harvester effect return differs: " + ModelJson.Difference(predicted, expected));
        Require(targets.All(id => actual.Rooms[0].Units.Any(unit => unit.Id == id)) &&
            record.GetProperty("QueueAfter").GetInt32() == result.PendingCallbacks.Count && result.Dispatched.Count == 0 &&
            record.GetProperty("Dispatched").GetArrayLength() == 0 &&
            immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Movement lost targets, left callbacks or mutated its source.");
        foreach ((string field, JsonNode? value) in new (string, JsonNode?)[] {
            ("Target", JsonValue.Create("Self")), ("Value", JsonValue.Create(-1)), ("AllowPlayer", JsonValue.Create(true)) })
        {
            JsonObject changed = JsonSerializer.SerializeToNode(actor, ModelJson.Options)!.AsObject();
            changed["Triggers"]![0]!["Effects"]![0]!["Action"]![field] = value;
            CombatUnit invalid = changed.Deserialize<CombatUnit>(ModelJson.Options)!;
            Require(!HarvesterBirthModel.IsTrigger(invalid, invalid.Triggers[0]) && RoomCombatModel.Validate(new RoomCombatState(0,
                before.Rooms[0].Deployment, before.Rooms[0].Units.Select(unit => unit.Id == invalid.Id ? invalid : unit).ToArray(),
                before.Rooms[0].ExternalInteractions, before.Context)) != null, "Uncalibrated birth signature was admitted: " + field);
        }
        Console.WriteLine($"HARVESTER-BIRTH PASS: {targets.Length} ordered enemy tower targets, crossRoom={crossRoom}, original -2 movement, full native effect state/RNG and narrow signature guards.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
