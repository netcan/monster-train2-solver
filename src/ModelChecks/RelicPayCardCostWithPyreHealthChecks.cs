using System;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicPayCardCostWithPyreHealthChecks
{
    private static readonly int Seed = 40619;

    internal static void Run()
    {
        CombatRelicState owner = Owner(1);
        Require(RelicModel.Validate([owner]) == null, "The unchanged Pyre-payment owner was rejected.");
        TrainCombatState source = State([owner]);

        RelicPyreCostPlan paid = RelicPayCardCostWithPyreHealthModel.Plan(source, 4, 3, "Default", 1, false);
        Require(paid.Supported && paid.CanPay && paid.PaidWithPyre && paid.Callbacks.Count == 1 &&
            paid.Callbacks[0].Kind == "OnHit" && paid.Callbacks[0].ParamInt == 4 &&
            paid.State.Rooms.Single().Units.Single().Health == 76 &&
            source.Rooms.Single().Units.Single().Health == 80,
            "Unaffordable native payment did not immutably spend cost-scaled Pyre HP and queue OnHit.");

        RelicPyreCostPlan affordable = RelicPayCardCostWithPyreHealthModel.Plan(source, 3, 3, "Default", 1, false);
        RelicPyreCostPlan xCost = RelicPayCardCostWithPyreHealthModel.Plan(source, 3, 0, "ConsumeRemainingEnergy", 1, false);
        Require(affordable.Supported && affordable.CanPay && !affordable.PaidWithPyre &&
            xCost.Supported && xCost.CanPay && !xCost.PaidWithPyre &&
            affordable.State.Rooms.Single().Units.Single().Health == 80 && xCost.Callbacks.Count == 0,
            "Affordable or X-cost control incorrectly used Pyre payment.");

        RelicPyreCostPlan ablated = RelicPayCardCostWithPyreHealthModel.Plan(State([]), 4, 3, "Default", 1, false);
        RelicPyreCostPlan preview = RelicPayCardCostWithPyreHealthModel.Plan(source, 4, 3, "Default", 1, true);
        RelicPyreCostPlan deployment = RelicPayCardCostWithPyreHealthModel.Plan(source, 4, 3, "Default", 0, false);
        RelicPyreCostPlan lethal = RelicPayCardCostWithPyreHealthModel.Plan(State([owner], 4), 4, 3, "Default", 1, false);
        Require(ablated.Supported && !ablated.CanPay && !ablated.PaidWithPyre &&
            preview.Supported && !preview.CanPay &&
            !deployment.Supported && deployment.UnsupportedReason!.Contains("Deployment-phase", StringComparison.Ordinal) &&
            !lethal.Supported && lethal.UnsupportedReason!.Contains("Lethal", StringComparison.Ordinal),
            "Owner ablation, preview, deployment, or lethal-payment boundary was not explicit.");

        RelicPyreCostPlan stacked = RelicPayCardCostWithPyreHealthModel.Plan(State([Owner(1), Owner(2)]), 4, 3, "Default", 1, false);
        Require(stacked.Supported && stacked.PaidWithPyre && stacked.State.Rooms.Single().Units.Single().Health == 68 &&
            stacked.Callbacks.Select(callback => callback.ParamInt).SequenceEqual([4, 8]),
            "Ordered duplicate owners did not each charge their cost-scaled amount and queue OnHit.");

        Require(RelicModel.Validate([Owner(0)]) != null && RelicModel.Validate([
                new CombatRelicState(RelicPayCardCostWithPyreHealthModel.DataId,
                    RelicPayCardCostWithPyreHealthModel.AssetKey,
                    [RelicPayCardCostWithPyreHealthModel.Effect], disallowedInPlacementPhase: false)]) != null,
            "Invalid or uncaptured Pyre payment payload passed native relic validation.");
        Console.WriteLine("RELIC-PYRE-CARD-COST-PURE PASS: affordability/X-cost gates, scale, ordered owner stacking, immutable HP, callback payload and explicit support boundaries.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPyreCardCostSchema").GetInt32() == 209 &&
            root.GetProperty("ModifierScenario").GetString() == "original-pyre-health-card-cost" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The isolated Pyre card-cost battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalPyreCardCost");
        int initialEnergy = evidence.GetProperty("InitialEnergy").GetInt32();
        int initialPyreHp = evidence.GetProperty("InitialPyreHp").GetInt32();
        int highCost = evidence.GetProperty("HighCost").GetInt32();
        Require(evidence.GetProperty("OwnerId").GetString() == RelicPayCardCostWithPyreHealthModel.DataId &&
            initialEnergy >= 2 && highCost == initialEnergy + 1 && highCost < initialPyreHp,
            "The original owner identity or nonlethal scenario setup changed.");

        FixtureValue[] eligibility = evidence.GetProperty("Eligibility").EnumerateArray().ToArray();
        Require(eligibility.Length == 3 &&
            eligibility[0].GetProperty("Label").GetString() == "Affordable" &&
            eligibility[0].GetProperty("Affordable").GetBoolean() &&
            !eligibility[0].GetProperty("RelicTest").GetBoolean() &&
            eligibility[0].GetProperty("CanPlayHandCard").GetBoolean() &&
            eligibility[1].GetProperty("Label").GetString() == "PyrePaid" &&
            eligibility[1].GetProperty("Cost").GetInt32() == highCost &&
            !eligibility[1].GetProperty("Affordable").GetBoolean() &&
            eligibility[1].GetProperty("RelicTest").GetBoolean() &&
            eligibility[1].GetProperty("CanPlayHandCard").GetBoolean() &&
            eligibility[2].GetProperty("Label").GetString() == "XCost" &&
            eligibility[2].GetProperty("CostType").GetString() == "ConsumeRemainingEnergy" &&
            !eligibility[2].GetProperty("RelicTest").GetBoolean() &&
            eligibility[2].GetProperty("CanPlayHandCard").GetBoolean(),
            "Native card affordability, relic eligibility or CanPlayHandCard controls differ.");

        FixtureValue[] results = evidence.GetProperty("Results").EnumerateArray().ToArray();
        Require(results.Length == 3 && results.Select(item => item.GetProperty("Label").GetString())
                .SequenceEqual(["Affordable", "PyrePaid", "XCost"]),
            "The native battle did not resolve all three card-cost controls in order.");
        int affordableCard = results[0].GetProperty("CardInstanceId").GetInt32();
        int pyrePaidCard = results[1].GetProperty("CardInstanceId").GetInt32();
        int xCostCard = results[2].GetProperty("CardInstanceId").GetInt32();
        Require(results[0].GetProperty("Energy").GetInt32() == initialEnergy - 1 &&
            results[0].GetProperty("PyreHp").GetInt32() == initialPyreHp &&
            results[0].GetProperty("LastPlayedCost").GetInt32() == 1 &&
            results[1].GetProperty("Energy").GetInt32() == initialEnergy - 1 &&
            results[1].GetProperty("PyreHp").GetInt32() == initialPyreHp - highCost &&
            results[1].GetProperty("LastPlayedCost").GetInt32() == eligibility[1].GetProperty("LastPlayedCost").GetInt32() &&
            results[2].GetProperty("Energy").GetInt32() == 0 &&
            results[2].GetProperty("PyreHp").GetInt32() == initialPyreHp - highCost &&
            results[2].GetProperty("LastPlayedCost").GetInt32() == initialEnergy - 1,
            "Native payment changed the wrong resource or recorded Pyre payment as Ember cost.");

        FixtureValue[] queuedHits = evidence.GetProperty("QueuedHits").EnumerateArray().ToArray();
        Require(queuedHits.Length == 1 && queuedHits[0].GetProperty("CardInstanceId").GetInt32() == pyrePaidCard &&
            queuedHits[0].GetProperty("ActorId").GetInt32() > 0 &&
            queuedHits[0].GetProperty("Trigger").GetString() == "OnHit" &&
            queuedHits[0].GetProperty("ParamInt").GetInt32() == highCost &&
            queuedHits[0].GetProperty("ParamInt2").GetInt32() == 0,
            "Native Pyre OnHit queue payload did not match the scaled health payment.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(action => action.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 3 && actions.Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())
                .SequenceEqual([affordableCard, pyrePaidCard, xCostCard]) &&
            actions.All(action => action.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Independent action replay differed or did not play the controls in order.");
        BattleTurnChecks.Native(root);
        BattleActionChecks.Native(root);
        Console.WriteLine("ORIGINAL-PYRE-HEALTH-CARD-COST NATIVE PASS: affordable Ember, scaled nonlethal Pyre payment, exact OnHit payload, X-cost ablation and three action replays.");
    }

    private static CombatRelicState Owner(int scale)
    {
        var rule = new RelicResourceEffect(0, RelicPayCardCostWithPyreHealthModel.Effect,
            scale, 0, scale, false);
        return new CombatRelicState(RelicPayCardCostWithPyreHealthModel.DataId,
            RelicPayCardCostWithPyreHealthModel.AssetKey,
            [RelicPayCardCostWithPyreHealthModel.Effect], disallowedInPlacementPhase: false,
            resourceEffects: [rule]);
    }

    private static TrainCombatState State(CombatRelicState[] relics, int pyreHealth = 80)
    {
        UnityRng rng = UnityRng.Seed(Seed);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10,
            relics: relics, resources: new BattleResourceState(3, 0, 5, 10, 10,
                "pyre", 45, 100, 0, 45, [], [], []));
        var pyre = new CombatUnit(11, "pyre", CombatTeam.Player, 45, pyreHealth, 100,
            true, true, false, []);
        var room = new RoomCombatState(3, false, [pyre], [], context);
        return new TrainCombatState([room], [], 4, context);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
