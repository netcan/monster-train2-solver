using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggeredRecursionChecks
{
    private const string Child = "55a291ba-611e-4e29-93b0-08f5154ceb89";
    private const string Infery = "4cad5d6e-3c7e-4993-9d3d-53948dae4b6d";
    internal static void Native(FixtureValue root, IReadOnlyList<string> originals)
    {
        foreach (string id in originals.Where(id => id is Child or Infery))
        {
            FixtureValue play = root.GetProperty("Actions").EnumerateArray().Single(row =>
                row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!
                    .FindCard(row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())!.DataId == id);
            int spawner = play.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            BattleTurnState before, after;
            if (id == Infery)
            {
                before = play.GetProperty("Before").Deserialize<BattleTurnState>()!;
                after = play.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == spawner)
                    .Triggers.Single(trigger => trigger.Kind == "OnSpawn").HasTriggered,
                    "Infery's unconditional native recursion test must admit its OnSpawn callback even for an empty pile.");
            }
            else
            {
                FixtureValue death = root.GetProperty("Turns").EnumerateArray().Single(row =>
                {
                    BattleTurnState old = row.GetProperty("Before").Deserialize<BattleTurnState>()!, next = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                    return old.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == spawner && unit.Health > 0) &&
                        !next.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == spawner && unit.Health > 0);
                });
                before = death.GetProperty("Before").Deserialize<BattleTurnState>()!;
                after = death.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            }
            CombatContext oldContext = before.Spawn.Train.Context!, nextContext = after.Spawn.Train.Context!;
            CardPileState oldPile = oldContext.OtherPiles!.Single(pile => pile.Name == "Exhausted"),
                nextPile = nextContext.OtherPiles!.Single(pile => pile.Name == "Exhausted");
            string cardType = id == Child ? "Monster" : "Spell";
            int[] eligible = oldPile.Cards.Where(card => oldContext.FindCard(card.InstanceId)!.MaskDescriptor!.Definition.CardType == cardType)
                .Select(card => card.InstanceId).ToArray();
            if (eligible.Length == 0)
            {
                Require(id == Infery && nextPile.Cards.Count == oldPile.Cards.Count &&
                    nextContext.Cards.Hand.Count == oldContext.Cards.Hand.Count - 1,
                    "Empty-pile OnSpawn recursion must produce no card while still completing the unit play.");
                Console.WriteLine("ORIGINAL-TRIGGERED-RECURSION PASS: native Infery empty-pile control.");
                continue;
            }
            int[] restored = eligible.Where(cardId => nextPile.Cards.All(card => card.InstanceId != cardId)).ToArray();
            Require(restored.Length == 1 && nextContext.Cards.Hand.Any(card => card.InstanceId == restored[0]) &&
                oldPile.Cards.Where(card => !eligible.Contains(card.InstanceId)).All(card => nextPile.Cards.Any(next => next.InstanceId == card.InstanceId)),
                "Triggered recursion must restore exactly one eligible card and preserve the opposite-type exhausted controls.");
            CardInstanceState selected = nextContext.FindCard(restored[0])!;
            Require(nextContext.Statistics!.Value(restored[0], "TimesDrawn", "ThisBattle") ==
                    oldContext.Statistics!.Value(restored[0], "TimesDrawn", "ThisBattle") + 1 &&
                (id == Child ? eligible.Length == 2 && selected.Temporary.Upgrades.Count(upgrade =>
                    upgrade.DataId == "828772f2-d8d3-4021-a8ae-bfa934c04679" && upgrade.Stats.Damage == 40) == 1 :
                    selected.Temporary.Upgrades.Count == 0),
                "Native recursion must record the draw and apply only Formless Child's temporary +40 Attack upgrade.");
            Console.WriteLine($"ORIGINAL-TRIGGERED-RECURSION PASS: {id}, one native {cardType} restored from {eligible.Length} eligible cards, type exclusion and temporary upgrade verified.");
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
