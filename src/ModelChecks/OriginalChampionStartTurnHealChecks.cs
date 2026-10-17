using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalChampionStartTurnHealChecks
{
    private const string OwnerId = "dbd5aec6-5046-4df2-ae61-4b646b8d1702";
    private const int Schema = 208;

    private sealed class Operation
    {
        public string OwnerDataId { get; set; } = "";
        public int EffectIndex { get; set; }
        public int Turn { get; set; }
        public int SelectedRoom { get; set; }
        public int CounterBefore { get; set; }
        public int CounterAfter { get; set; }
        public TrainCombatState Before { get; set; } = null!;
        public TrainCombatState Actual { get; set; } = null!;
        public bool Completed { get; set; }
        public bool ModelSupported { get; set; }
        public string? UnsupportedReason { get; set; }
        public string? Difference { get; set; }
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalChampionStartTurnHealSchema").GetInt32() == Schema &&
            root.GetProperty("ModifierScenario").GetString() == "original-champion-start-turn-heal" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Original champion start-turn healing battle is incomplete or from an unexpected build.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending",
            "RelicStartTurnHealCaptureFailures", "RelicStartTurnHealMismatches", "RelicStartTurnHealUnsupported",
            "RelicStartTurnHealPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Champion start-turn heal capture has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("RelicStartTurnHealCoverage");
        int[] cards = evidence.GetProperty("CardIds").Deserialize<int[]>()!;
        Require(evidence.GetProperty("OwnerId").GetString() == OwnerId && cards.Length == 6,
            "Champion start-turn heal control setup changed.");
        PlayCardAction[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(action => action.GetProperty("ScenarioAction").GetBoolean())
            .Select(action => action.GetProperty("Action").Deserialize<PlayCardAction>()!).ToArray();
        Require(actions.Select(action => action.CardInstanceId).SequenceEqual(
                [cards[0], cards[2], cards[1], cards[3], cards[4], cards[5]]),
            "Native battle did not play both-floor Champions, the non-Champ control and all three wounds in order.");

        var resets = root.GetProperty("RelicStartTurnHealResets").EnumerateArray().ToArray();
        Require(resets.Length == 1 && resets[0].GetProperty("OwnerDataId").GetString() == OwnerId &&
            resets[0].GetProperty("Completed").GetBoolean() && resets[0].GetProperty("BeforeCounter").GetInt32() == 4 &&
            resets[0].GetProperty("AfterCounter").GetInt32() == 0,
            "Native StartOfBattle did not reset a seeded healing counter from four to zero exactly once.");
        var tests = root.GetProperty("RelicStartTurnHealTests").EnumerateArray().ToArray();
        Require(tests.Any(test => test.GetProperty("Phase").GetString() == "TestEffectStartOfBattle" &&
                test.GetProperty("Returned").GetBoolean()) &&
            tests.Any(test => test.GetProperty("Phase").GetString() == "TestEffectTurnPhaseTiming" &&
                test.GetProperty("Turn").GetInt32() >= 1 && test.GetProperty("Returned").GetBoolean()),
            "Native healing effect did not pass its battle and timed-turn eligibility tests.");
        foreach (FixtureValue test in tests.Where(test => test.GetProperty("Phase").GetString() == "TestEffectTurnPhaseTiming"))
            Require(test.GetProperty("OwnerDataId").GetString() == OwnerId &&
                test.GetProperty("Returned").GetBoolean() == (test.GetProperty("Counter").GetInt32() < test.GetProperty("MaxTurns").GetInt32()),
                "Native turn eligibility differs from healingTurnCount < healingTurnCountMax.");

        Operation[] operations = root.GetProperty("RelicStartTurnHealOperations").EnumerateArray()
            .Select(ReadOperation).ToArray();
        Require(operations.Length >= 1 && operations.All(operation => operation.OwnerDataId == OwnerId &&
            operation.EffectIndex == 0 && operation.Completed && operation.ModelSupported &&
            operation.UnsupportedReason == null && operation.Difference == null &&
            operation.CounterAfter == operation.CounterBefore + 1),
            "Native timed healing callbacks are incomplete or diverged from the independent model.");
        foreach (Operation operation in operations)
        {
            ValidateOwner(operation.Before.Context!);
            RelicStartTurnHealResult predicted = RelicStartTurnHealModel.Apply(operation.Before,
                operation.Turn, operation.SelectedRoom);
            Require(predicted.Supported && Same(predicted.State!, operation.Actual),
                "Independent start-turn heal replay differs on turn " + operation.Turn + ".");
        }

        Operation positive = operations.First(operation => operation.Turn == 1);
        CombatUnit champZeroBefore = CardUnit(positive.Before, cards[0]);
        CombatUnit champOneBefore = CardUnit(positive.Before, cards[2]);
        CombatUnit peerBefore = CardUnit(positive.Before, cards[1]);
        CombatUnit champZeroAfter = CardUnit(positive.Actual, cards[0]);
        CombatUnit champOneAfter = CardUnit(positive.Actual, cards[2]);
        CombatUnit peerAfter = CardUnit(positive.Actual, cards[1]);
        Require(positive.SelectedRoom == 0 && champZeroBefore.Health < champZeroBefore.MaxHealth &&
            champZeroAfter.Health == champZeroAfter.MaxHealth && champOneBefore.Health < champOneBefore.MaxHealth &&
            champOneAfter.Health == champOneAfter.MaxHealth &&
            peerBefore.Health < peerBefore.MaxHealth && peerAfter.Health == peerBefore.Health,
            "Native Tower targeting did not heal wounded Champions across floors while excluding a wounded non-Champion.");

        RelicStartTurnHealResult otherSelection = RelicStartTurnHealModel.Apply(positive.Before, positive.Turn, 1);
        Require(otherSelection.Supported && Same(otherSelection.State!, positive.Actual),
            "Tower target collection changed when the selected room changed.");
        RelicStartTurnHealResult placement = RelicStartTurnHealModel.Apply(positive.Before, positive.Turn,
            positive.SelectedRoom, placement: true);
        Require(placement.Supported && Same(placement.State!, positive.Before),
            "Placement-phase exclusion changed the healing state.");

        TrainCombatState capped = WithMaxTurns(positive.Before, 1);
        RelicStartTurnHealResult afterCap = RelicStartTurnHealModel.Apply(capped, positive.Turn + 1, positive.SelectedRoom);
        Require(afterCap.Supported && Same(afterCap.State!, capped),
            "The max-turn counter allowed healing after its cap.");
        TrainCombatState withoutOwner = WithoutOwner(positive.Before);
        RelicStartTurnHealResult ablated = RelicStartTurnHealModel.Apply(withoutOwner, positive.Turn, positive.SelectedRoom);
        Require(ablated.Supported && Same(ablated.State!, withoutOwner),
            "Removing the relic owner did not remove the healing callback.");

        BattleTurnChecks.Native(root);
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-CHAMPION-START-TURN-HEAL-NATIVE PASS: {operations.Length} timed callbacks, Tower-wide Champ-only full heal, non-Champ control, reset/cap and six action replays.");
    }

    private static CombatRelicState ValidateOwner(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicStartTurnHealRule rule = owner.ResourceEffects!.Single().StartTurnHeal!;
        Require(owner.AssetKey == "ChampsHealBeginningOfTurn" && owner.DisallowedInPlacementPhase == true &&
            owner.EffectTypes.SequenceEqual([RelicStartTurnHealModel.Effect]) && rule.EffectIndex == 0 &&
            rule.MaxTurns == 999 && rule.Behavior == 0 && rule.HealingAmount == 0 && rule.SourceTeam == "Monsters" &&
            rule.TargetMode == "Tower" && rule.RequiredSubtype == RelicStartTurnHealModel.ChampionSubtype &&
            rule.ConditionCount == 0, "Original ChampsHealBeginningOfTurn owner or payload changed.");
        return owner;
    }

    private static TrainCombatState WithMaxTurns(TrainCombatState source, int maxTurns)
    {
        CombatContext context = source.Context!;
        CombatRelicState owner = ValidateOwner(context);
        RelicResourceEffect effect = owner.ResourceEffects!.Single();
        RelicStartTurnHealRule current = effect.StartTurnHeal!;
        var replacementRule = new RelicStartTurnHealRule(current.EffectIndex, maxTurns, current.Behavior,
            current.HealingAmount, current.SourceTeam, current.TargetMode, current.RequiredSubtype, current.ConditionCount);
        var replacement = new RelicResourceEffect(effect.EffectIndex, effect.Type, effect.ParamInt, effect.ParamInt2,
            effect.Amount, effect.CanApplyInPreviewMode, effect.RequiredMutatorId, effect.Conditions, effect.DrawValue,
            effect.DrawMutationOrder, effect.CapacityValue, effect.CapacityMutationOrder, effect.ParamMinInt,
            effect.ParamFloat, effect.CardGeneration, effect.UnitTrigger, effect.OpeningDraw, effect.PileStartTurn,
            effect.MoonPhaseStatus, replacementRule);
        CombatRelicState[] relics = context.Relics!.Select(item => item.DataId == OwnerId
            ? item.WithResourceEffects([replacement]) : item).ToArray();
        return WithContext(source, context.WithRelics(relics));
    }

    private static TrainCombatState WithoutOwner(TrainCombatState source)
    {
        CombatContext context = source.Context!;
        return WithContext(source, context.WithRelics(context.Relics!.Where(item => item.DataId != OwnerId).ToArray()));
    }

    private static TrainCombatState WithContext(TrainCombatState source, CombatContext context) =>
        new(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
            room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
            source.Movement, source.EnemySlotsPerRoom, context);

    private static CombatUnit CardUnit(TrainCombatState source, int cardId) => source.Rooms.SelectMany(room => room.Units)
        .Single(unit => unit.SpawnerCardId == cardId);

    private static bool Same(TrainCombatState expected, TrainCombatState actual) =>
        ModelJson.Difference(Comparable(expected), Comparable(actual)) == null;

    private static string Comparable(TrainCombatState state) => JsonSerializer.Serialize(new
    {
        Rooms = state.Rooms.Select(room => new { room.RoomIndex, room.Deployment, room.Units }).ToArray(),
        Context = BattleTurnChecks.ComparableContext(state.Context)
    });

    private static Operation ReadOperation(FixtureValue value) => new()
    {
        OwnerDataId = value.GetProperty("OwnerDataId").GetString()!,
        EffectIndex = value.GetProperty("EffectIndex").GetInt32(),
        Turn = value.GetProperty("Turn").GetInt32(),
        SelectedRoom = value.GetProperty("SelectedRoom").GetInt32(),
        CounterBefore = value.GetProperty("CounterBefore").GetInt32(),
        CounterAfter = value.GetProperty("CounterAfter").GetInt32(),
        Before = value.GetProperty("Before").Deserialize<TrainCombatState>()!,
        Actual = value.GetProperty("Actual").Deserialize<TrainCombatState>()!,
        Completed = value.GetProperty("Completed").GetBoolean(),
        ModelSupported = value.GetProperty("ModelSupported").GetBoolean(),
        UnsupportedReason = value.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null
            ? null : value.GetProperty("UnsupportedReason").GetString(),
        Difference = value.GetProperty("Difference").ValueKind == FixtureKind.Null
            ? null : value.GetProperty("Difference").GetString()
    };

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
