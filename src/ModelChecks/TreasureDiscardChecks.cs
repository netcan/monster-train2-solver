using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TreasureDiscardChecks
{
    private static readonly string[] Cards = ["b9ef3012-9b1c-4cca-b60b-661c79c07ddb", "690ba1bf-378e-4db3-a2ab-c37dc36df04b", "73d96e11-7665-4915-8301-e3e6ffdafb51"];
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!Cards.All(originals.Contains)) return;
        int direct = 0, indirect = 0, controls = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            string played = old.FindCard(action.CardInstanceId)!.DataId;
            if (Cards.Contains(played))
            {
                Require(next.Statistics!.Value(action.CardInstanceId, "TimesPlayed", "ThisBattle") == 1 &&
                    next.FindCard(action.CardInstanceId)!.PlayCount == 1 && before.Energy - after.Energy == 3,
                    "Direct Treasure plays must pay their original three Ember and play exactly once.");
                direct++;
            }
            if (played == "37eaa51b-62cf-4cbc-971f-6d208fd07366")
            {
                int paid = before.PlayRules!.Cards.Single(rule => rule.DataId == played).Cost;
                Require(paid == 3 && before.Energy - after.Energy == paid, "Treasure casts must spend only Deep Offering's original three Ember.");
                foreach (CardToken token in old.Cards.Hand.Where(token => Cards.Contains(token.DataId)))
                {
                    CardInstanceState card = next.FindCard(token.InstanceId)!;
                    Require(next.Statistics!.Value(token.InstanceId, "TimesPlayed", "ThisBattle") ==
                        old.Statistics!.Value(token.InstanceId, "TimesPlayed", "ThisBattle") + 1 &&
                        card.PlayCount == old.FindCard(token.InstanceId)!.PlayCount + 1 && card.LastPlayedCost == old.FindCard(token.InstanceId)!.LastPlayedCost,
                        "Card-triggered Treasure discard must increment local/global play histories once while preserving its last paid cost.");
                    indirect++;
                }
                int amulets = old.Cards.Hand.Count(token => token.DataId == Cards[1]), teeth = old.Cards.Hand.Count(token => token.DataId == Cards[2]);
                Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy &&
                    unit.Status("debuff")?.Stacks == 2 * (1 + amulets) && unit.Status("poison")?.Stacks == 10 * (1 + teeth)),
                    "Direct and indirect Guardian Amulet/Titanstooth must positively apply their original debuffs and Frostbite.");
            }
        }
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            foreach (CardToken token in before.Spawn.Train.Context!.Cards.Hand.Where(token => Cards.Contains(token.DataId)))
            {
                Require(after.Spawn.Train.Context!.Statistics!.Value(token.InstanceId, "TimesPlayed", "ThisBattle") ==
                    before.Spawn.Train.Context.Statistics!.Value(token.InstanceId, "TimesPlayed", "ThisBattle"),
                    "End-turn hand discard must never activate Treasure's indirect cast.");
                controls++;
            }
        }
        Require(direct == 3 && indirect >= 3 && controls > 0, "Missing direct, discarded or end-turn Treasure witnesses.");
        Console.WriteLine($"ORIGINAL-TREASURE-DISCARD PASS: {direct} paid original plays, {indirect} free discard casts and {controls} end-turn non-cast controls; native damage/status/order, paid-cost and local/global play histories.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
