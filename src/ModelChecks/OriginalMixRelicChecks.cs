using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMixRelicChecks
{
    private const string MixologyId = "9582724d-2d5e-4349-a642-9720bbb833e9";
    private const string TinkermasterId = "db2193eb-a476-4cc4-9a26-2d0e35f46b74";
    private const string ProgressId = "4446fd0c-f7c3-4a9d-96c3-bd12e4426a76";

    public static void Native(FixtureValue root)
    {
        string scenario = root.GetProperty("ModifierScenario").GetString()!;
        bool progress = scenario == "original-progress-pacemaker-mix";
        Require(progress || scenario == "original-tinkermaster-mix", "This is not an original Mix relic battle.");
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Mismatches").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0, "The original Mix relic battle was not fully accepted.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("Action").GetProperty("MixSelections").GetArrayLength() > 0).ToArray();
        Require(actions.Length == 2 && actions[0].GetProperty("ScenarioAction").GetBoolean() &&
            actions[1].GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() >
                actions[0].GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32(),
            "The native battle did not play Mixology in two separate turns.");
        int[] expectedAmounts = progress ? [2, 2] : [3];
        string expectedSelections = progress ? "0:0:0,0:1:0,1:0:0" : "0:0:0,1:0:0";
        foreach (FixtureValue entry in actions)
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext context = before.Spawn.Train.Context!;
            Require(context.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId == MixologyId &&
                string.Join(",", action.MixSelections.Select(item =>
                    item.EffectIndex + ":" + item.MixNumber + ":" + item.ChoiceIndex)) == expectedSelections,
                "Original Mixology did not use the expected ordered draft selections.");
            CombatRelicState relic = context.Relics!.Single(item => item.DataId == (progress ? ProgressId : TinkermasterId));
            RelicResourceEffect modifier = relic.ResourceEffects!.Single();
            Require(relic.AssetKey == (progress ? "ProgressPacemaker" : "TinkermasterTools") &&
                relic.EffectTypes.SequenceEqual([RelicResourceModel.AdjustMixAmount]) &&
                modifier.EffectIndex == 0 && modifier.Type == RelicResourceModel.AdjustMixAmount &&
                modifier.ParamInt == (progress ? 0 : 1) && modifier.ParamInt2 == (progress ? 1 : 0) &&
                modifier.ParamMinInt == (progress ? 2 : 0) && modifier.Amount == modifier.ParamInt &&
                modifier.Conditions.Count == (progress ? 1 : 0),
                "Original Mix relic identity, parameters or effect order changed.");
            if (progress)
            {
                Require(!modifier.Conditions[0].Triggered &&
                    actual.Spawn.Train.Context!.Relics!.Single(item => item.DataId == ProgressId)
                        .ResourceEffects!.Single().Conditions.Single().Triggered,
                    "The once-per-turn Mix relic condition did not reset and trigger again.");
            }
            ElixirMixPlan plan = ElixirMixModel.Plan(context, 2);
            Require(plan.Supported && plan.Amounts.SequenceEqual(expectedAmounts),
                "The modeled original Mix relic candidate amounts or repetitions differ.");
            BattleActionResult replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "Original Mix relic action does not replay to native state: " + replay.Reason);
        }
        Console.WriteLine("ORIGINAL-MIX-RELIC-NATIVE PASS: " + scenario +
            " two separate turns, candidate amounts=" + string.Join(",", expectedAmounts) + ".");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
