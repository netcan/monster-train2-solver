using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFirstImpBonusChecks
{
    internal static void Diagnose(FixtureValue root)
    {
        foreach (FixtureValue record in root.GetProperty("Spawns").EnumerateArray())
        {
            if (record.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            int index = record.GetProperty("Index").GetInt32();
            EnemySpawnState before = record.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            EnemySpawnState actual = record.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            EnemySpawnResult predicted = EnemySpawningModel.Spawn(before, record.GetProperty("IncludeTreasure").GetBoolean());
            if (!predicted.Supported)
            {
                Console.WriteLine("FIRST-IMP-SPAWN-DIAG index=" + index + " unsupported=" + predicted.UnsupportedReason);
                continue;
            }
            string left = JsonSerializer.Serialize(Comparable(predicted.State!), ModelJson.Options);
            string right = JsonSerializer.Serialize(Comparable(actual), ModelJson.Options);
            Console.WriteLine("FIRST-IMP-SPAWN-DIAG index=" + index + " difference=" + ModelJson.Difference(left, right));
            Console.WriteLine("FIRST-IMP-SPAWN-CACHE index=" + index + " predicted=" +
                string.Join(",", predicted.State!.Train.Context!.Statistics!.StoredCards ?? Array.Empty<int>()) + " actual=" +
                string.Join(",", actual.Train.Context!.Statistics!.StoredCards ?? Array.Empty<int>()));
        }
        foreach (FixtureValue record in root.GetProperty("Stages").EnumerateArray())
        {
            if (record.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            int index = record.GetProperty("Index").GetInt32();
            RoomCombatState before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = record.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatResult predicted = record.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            if (!predicted.Supported)
            {
                Console.WriteLine("FIRST-IMP-ROOM-DIAG index=" + index + " unsupported=" + predicted.UnsupportedReason);
                continue;
            }
            Console.WriteLine("FIRST-IMP-ROOM-DIAG index=" + index + " units=" + Difference(predicted.State!.Units, actual.Units) +
                " context=" + Difference(ComparableContext(predicted.State.Context), ComparableContext(actual.Context)) +
                " before-cache=" + CacheSummary(before.Context) + " predicted-cache=" + CacheSummary(predicted.State.Context) +
                " actual-cache=" + CacheSummary(actual.Context));
        }
        foreach (FixtureValue record in root.GetProperty("TrainPhases").EnumerateArray())
        {
            if (record.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            int index = record.GetProperty("Index").GetInt32();
            TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = record.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            TrainCombatResult predicted = record.GetProperty("Kind").GetString() == "Combat"
                ? TrainCombatModel.ResolveCombat(before) : TrainCombatModel.Ascend(before);
            if (!predicted.Supported)
            {
                Console.WriteLine("FIRST-IMP-TRAIN-DIAG index=" + index + " unsupported=" + predicted.UnsupportedReason);
                continue;
            }
            Console.WriteLine("FIRST-IMP-TRAIN-DIAG index=" + index + " rooms=" + Difference(
                predicted.State!.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
                actual.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray()) + " context=" +
                Difference(ComparableContext(predicted.State.Context), ComparableContext(actual.Context)));
        }

        static object Comparable(EnemySpawnState state) => new
        {
            Rooms = state.Train.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
            Movement = state.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(),
            state.Train.Context,
            state.Phase,
            state.SelectedGroups,
            state.Rng,
            state.NextUnitId,
            state.TreasuresRemaining,
            state.CanonicalDecisionReferences
        };
        static CombatContext? ComparableContext(CombatContext? context)
        {
            if (context?.QueryFrame is StatisticQueryFrame frame &&
                (frame.SelectedRoomIndex.HasValue || frame.MoonPhaseTrackingActive.HasValue ||
                    frame.ForgeTrackingActive.HasValue || frame.ForgeToggleActive.HasValue))
                context = context.WithQueryFrame(new StatisticQueryFrame(frame.Energy, frame.RunningCombat, frame.Turn,
                    frame.ForgePoints, frame.DragonsHoard, frame.MoonPhase, frame.PyreResurrectionCount, frame.ActiveBattle,
                    forgeTrackingActive: frame.ForgeTrackingActive, forgeToggleActive: frame.ForgeToggleActive,
                    grosMichelCombatRoundCount: frame.GrosMichelCombatRoundCount));
            if (context?.SpawnPoints is BattleSpawnPoints points && points.NextPreviewCopyId.HasValue)
                context = context.WithSpawnPoints(new BattleSpawnPoints(points.Groups, points.Units));
            return context;
        }
        static string? Difference<T>(T expected, T actual) => ModelJson.Difference(
            JsonSerializer.Serialize(expected, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        static string CacheSummary(CombatContext? context) => context?.Statistics == null ? "none" :
            "stored=" + string.Join(",", context.Statistics.StoredCards ?? Array.Empty<int>()) +
            ";tracked=" + string.Join(",", context.Statistics.TrackedCards) +
            ";uncached=" + string.Join(",", (context.CardInstances ?? Array.Empty<CardInstanceState>())
                .Where(card => !context.Statistics.StoredCards?.Contains(card.InstanceId) == true)
                .Select(card => card.InstanceId + ":" + card.DataId));
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 213 &&
            root.GetProperty("OriginalFirstImpBonusSchema").GetInt32() == 213 &&
            root.GetProperty("ModifierScenario").GetString() == "original-first-imp-bonus" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "FirstImpBonus battle is incomplete or from an unexpected capture schema.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "FirstImpBonus battle has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("OriginalFirstImpBonus").GetProperty("SyntheticWitness");
        int cardId = evidence.GetProperty("CardInstanceId").GetInt32();
        Require(evidence.GetProperty("IsSynthetic").GetBoolean() &&
            evidence.GetProperty("CardDefinitionId").GetString() == OriginalFirstImpBonusScenarioCardId &&
            evidence.GetProperty("Subtype").GetString() == ImpSubtype && evidence.GetProperty("Cost").GetInt32() == 0 &&
            evidence.GetProperty("PreparedTurn").GetInt32() > 0 &&
            evidence.GetProperty("SourceCardAssetKey").GetString() != null,
            "FirstImpBonus positive control is not an explicitly synthetic Imp outside deployment phase.");
        FixtureValue definition = root.GetProperty("OriginalFirstImpBonus").GetProperty("Definition");
        Require(definition.GetProperty("OwnerId").GetString() == RelicModelDataId &&
            definition.GetProperty("AssetKey").GetString() == "FirstImpBonus" &&
            definition.GetProperty("EffectType").GetString() == "RelicEffectEnergyAndCardDrawOnUnitSpawned" &&
            definition.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            definition.GetProperty("ParamInt").GetInt32() == 1 &&
            definition.GetProperty("ParamSourceTeam").GetString() == "Monsters" &&
            !definition.GetProperty("CanApplyInPreviewMode").GetBoolean(),
            "Unchanged FirstImpBonus definition or native placement/preview gates changed.");

        FixtureValue[] positive = root.GetProperty("RelicSpawnStatuses").EnumerateArray()
            .Where(record => record.GetProperty("FromCardId").GetInt32() == cardId).ToArray();
        Require(positive.Length == 1, "Expected exactly one synthetic Imp birth callback.");
        FixtureValue record = positive[0];
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Synthetic Imp birth callback was incomplete or divergent.");
        {
            int unitId = record.GetProperty("UnitId").GetInt32();
            TrainCombatState before = record.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = record.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            CombatContext beforeContext = before.Context!;
            CombatContext afterContext = actual.Context!;
            CombatUnit actor = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == unitId);
            int conditionValue = beforeContext.Statistics!.SubtypesSpawnedThisTurn
                .Single(item => item.Key == ImpSubtype).Value;
            Require(actor.Team == CombatTeam.Player && actor.Subtypes.Contains(ImpSubtype) && conditionValue == 1,
                "Synthetic witness did not produce a player Imp with MonsterSubtypePlayed exactly one.");
            RelicConditionState beforeCondition = beforeContext.Relics!.Single(relic => relic.DataId == RelicModelDataId)
                .SpawnStatuses!.Single().Conditions.Single();
            RelicConditionState afterCondition = afterContext.Relics!.Single(relic => relic.DataId == RelicModelDataId)
                .SpawnStatuses!.Single().Conditions.Single();
            Require(!beforeCondition.Triggered && afterCondition.Triggered && afterCondition.ValueAtLastTrigger == 1 &&
                afterCondition.DurationTriggerCount == 1 && beforeContext.QueryFrame!.Energy.HasValue &&
                afterContext.QueryFrame!.Energy == beforeContext.QueryFrame.Energy + 1 &&
                afterContext.Cards.Hand.Count == beforeContext.Cards.Hand.Count + 1 &&
                afterContext.Cards.Draw.Count == beforeContext.Cards.Draw.Count - 1,
                "FirstImpBonus did not record the one-shot condition, +1 energy and one-card draw on its positive callback.");
            var predicted = RelicBirthModel.CharacterAdded(before, unitId, cardId,
                record.GetProperty("OnlyCovenants").GetBoolean(), record.GetProperty("DeferCallbacks").GetBoolean(),
                record.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(predicted.Supported && ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options)) == null &&
                JsonSerializer.Serialize(predicted.Queued, ModelJson.Options) ==
                    JsonSerializer.Serialize(record.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>()!, ModelJson.Options) &&
                JsonSerializer.Serialize(predicted.Dispatched, ModelJson.Options) ==
                    JsonSerializer.Serialize(record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>()!, ModelJson.Options),
                "Independent FirstImpBonus train/queue replay differs from native callback state.");
        }

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == cardId).ToArray();
        Require(actions.Length == 1, "The synthetic Imp witness was not played exactly once.");
        PlayCardAction play = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleTurnState decision = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actualAction = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleActionResult actionResult = BattleActionModel.PlayCard(decision, play);
        bool terminal = actionResult.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
        Require(actionResult.Supported && ModelJson.Difference(
            BattleTurnChecks.Comparable(actionResult.State!, terminal, BattleTurnChecks.ShouldCompareRoomEnabled(root)),
            BattleTurnChecks.Comparable(actualAction, terminal, BattleTurnChecks.ShouldCompareRoomEnabled(root))) == null,
            "Independent synthetic Imp card-play replay differs from native.");

        Console.WriteLine("ORIGINAL-FIRST-IMP-BONUS-NATIVE PASS: unchanged owner, placement gate, synthetic Imp exact-one positive, condition record, +1 energy, one draw and action/birth replay.");
    }

    private const string OriginalFirstImpBonusScenarioCardId = "fc119000-0000-4000-8000-000000000513";
    private const string RelicModelDataId = "0a6cff92-aefa-4d7f-ae71-615c8e30739b";
    private const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
