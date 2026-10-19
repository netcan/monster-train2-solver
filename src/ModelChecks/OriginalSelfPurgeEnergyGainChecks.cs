using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSelfPurgeEnergyGainChecks
{
    private const string CardId = "d0e1fe8a-fb2a-44e0-9d9b-a3ddd7475c34";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalSelfPurgeEnergyGainScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 257 &&
            root.GetProperty("ModifierScenario").GetString() == "original-self-purge-energy-gain" &&
            root.GetProperty("OriginalSelfPurgeEnergyGainPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Self-Purge Energy Gain witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalSelfPurgeEnergyGainCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 2, "Missing controlled Self-Purge Energy Gain card sequence.");
        Require(actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its controlled Train Steward before playing Self-Purge Energy Gain.");

        FixtureValue action = actions[1];
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == CardId);
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        Require(rule.Cost == 0 && rule.CardType == "Spell" && rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "GainEnergy", "Draw" }) &&
            rule.Effects[0].Target == "Room" && rule.Effects[0].Value == 2 &&
            rule.Effects[1].Target == "Room" && rule.Effects[1].Value == 1 &&
            beforeContext.Cards.Hand.Any(item => item.InstanceId == ids[1] && item.DataId == CardId) &&
            beforeContext.Cards.Draw.Any(item => item.InstanceId == ids[2] && item.DataId == StewardId),
            "Self-Purge Energy Gain must retain its unchanged zero-cost GainEnergy(2), Draw(1) rule and controlled draw.");
        Require(after.Energy == System.Math.Min(before.Energy + 2, beforeContext.EnergyState!.Maximum) &&
            afterContext.Cards.Hand.Any(item => item.InstanceId == ids[2] && item.DataId == StewardId) &&
            afterContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(item => item.InstanceId == ids[1]) &&
            !afterContext.Cards.Discard.Any(item => item.InstanceId == ids[1]),
            "Self-Purge Energy Gain did not add capped energy, draw the controlled Steward and exhaust itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-SELF-PURGE-ENERGY-GAIN PASS: unchanged zero-cost play gains two capped energy, draws one card, then exhausts itself.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalSelfPurgeEnergyGainScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            context.Cards.Hand.Any(item => item.InstanceId == ids[1]) && context.Cards.Draw.Any(item => item.InstanceId == ids[2]))
            return BattleActionModel.PlayCard(source, new PlayCardAction(ids[1], 0)).Supported
                ? new PlayCardAction(ids[1], 0) : null;
        return null;
    }
}
