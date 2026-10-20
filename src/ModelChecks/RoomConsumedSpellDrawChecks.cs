using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomConsumedSpellDrawChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("93fd40da-054d-4cc2-a8b3-da77e619a1d0")) return;
        int positive = 0, empty = 0;
        foreach (FixtureValue row in root.GetProperty("RoomPreCombatRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("RoomIndex").GetInt32() == 0,
                "Total Recall needs complete selected-room pre-combat observations.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            CombatContext prior = before.Context!, next = after.Context!;
            CardToken[] eligible = prior.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards
                .Where(token => prior.FindCard(token.InstanceId)!.MaskDescriptor!.Definition.CardType == "Spell").ToArray();
            int[] drawn = next.Cards.Hand.Where(token => prior.Cards.Hand.All(old => old.InstanceId != token.InstanceId)).Select(token => token.InstanceId).ToArray();
            if (eligible.Length == 0)
            {
                Require(drawn.Length == 0 && next.BattleRng.Equals(prior.BattleRng), "Empty Total Recall must preserve its hand and Battle RNG.");
                empty++;
            }
            else
            {
                RngDraw roll = prior.BattleRng.Range(0, eligible.Length);
                int id = eligible[roll.Value].InstanceId;
                Require(next.BattleRng.Equals(roll.State) && drawn.SequenceEqual(new[] { id }) &&
                    next.Statistics!.Value(id, "TimesDrawn", "ThisBattle") == prior.Statistics!.Value(id, "TimesDrawn", "ThisBattle") + 1 &&
                    next.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.All(token => token.InstanceId != id),
                    "Total Recall must consume exactly one Range draw and return that consumed spell once.");
                positive++;
            }
            foreach (CardToken monster in prior.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Where(token =>
                prior.FindCard(token.InstanceId)!.MaskDescriptor!.Definition.CardType == "Monster"))
                Require(next.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == monster.InstanceId),
                    "Total Recall must leave exhausted monsters unchanged.");
            TypedDrawResult predicted = RoomConsumedSpellDrawModel.Apply(prior);
            Require(predicted.Supported, predicted.UnsupportedReason ?? "Unsupported consumed-spell draw.");
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.Context, ModelJson.Options), JsonSerializer.Serialize(next, ModelJson.Options));
            Require(difference == null, "Isolated native consumed-spell callback differs: " + difference);
        }
        Require(empty > 0 && (!originals.Contains("63a48bad-07da-4687-bfef-2f7c6653af5e") || positive == 2),
            "Total Recall needs empty controls and both seeded consumed spell returns.");
        Console.WriteLine($"ORIGINAL-CONSUMED-ROOM-DRAW PASS: {positive} positive and {empty} empty callbacks, exact native RNG/card ownership/statistics and monster exclusions.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
