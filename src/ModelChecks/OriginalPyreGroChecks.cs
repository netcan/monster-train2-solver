using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPyreGroChecks
{
    private const string CardId = "2ba0f9eb-744e-4b3c-af4e-3ad39ae8a74f";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalPyreGroScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 259 &&
            root.GetProperty("ModifierScenario").GetString() == "original-pyre-gro" &&
            root.GetProperty("OriginalPyreGroPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original PyreGro witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalPyreGroCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its Train Steward before playing PyreGro.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        Require(rule.Cost == 0 && rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "DrawNextTurn", "GainEnergyEveryTurn" }) &&
            rule.Effects[0].Target == "Room" && rule.Effects[0].Value == -2 &&
            rule.Effects[1].Target == "Room" && rule.Effects[1].Value == 1 &&
            afterContext.Cards.DrawModifier == beforeContext.Cards.DrawModifier - 2 &&
            afterContext.EnergyState!.EveryTurn == beforeContext.EnergyState!.EveryTurn + 1 &&
            after.Energy == before.Energy &&
            afterContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[1]) &&
            !afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]),
            "PyreGro must schedule -2 draw and +1 recurring energy without spending energy, then Consume itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-PYRE-GRO PASS: unchanged zero-cost play schedules two fewer cards and one recurring energy, then exhausts itself.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalPyreGroScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(item => item.InstanceId == ids[1]))
        {
            var cast = new PlayCardAction(ids[1], 0);
            return BattleActionModel.PlayCard(source, cast).Supported ? cast : null;
        }
        return null;
    }
}
