using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCardDrawPotionChecks
{
    private const string CardId = "84633260-7209-4515-9582-82605ee2a36a";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalCardDrawPotionPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Card Draw Potion witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalCardDrawPotionIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 3, "Missing original Card Draw Potion play and native draw controls.");
        BattleTurnState before = actions[2].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = actions[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        Require(before.Spawn.Train.Context!.FindCard(ids[2])!.DataId == rule.DataId && rule.Cost == 1 && rule.Effect == "Spell" &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "DrawNextTurn" && rule.Effects[0].Value == 3 &&
            rule.ExternalInteractions.Count == 0 && before.Spawn.Train.Context.Cards.DrawModifier == 0 &&
            after.Spawn.Train.Context!.Cards.DrawModifier == 3 && before.Energy - after.Energy == 1 &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]),
            "Original Card Draw Potion identity, 1-cost payment, native +3 scheduling or Exhaust routing differs.");

        FixtureValue draw = root.GetProperty("CardCycles").EnumerateArray().Single(row =>
            row.GetProperty("Kind").GetString() == "Draw" &&
            row.GetProperty("Before").GetProperty("DrawModifier").GetInt32() == 3);
        CardCycleState drawn = draw.GetProperty("Actual").Deserialize<CardCycleState>()!;
        Require(drawn.Hand.Count == 8 && drawn.DrawModifier == 0,
            "Next turn must draw five base cards plus three from the original Potion and reset the modifier.");
        BonusDrawChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-CARD-DRAW-POTION PASS: unchanged identity/cost, +3 next-turn schedule, eight-card native draw, reset, Exhaust and 16 immutable full chains.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
