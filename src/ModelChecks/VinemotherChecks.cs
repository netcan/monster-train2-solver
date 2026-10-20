using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class VinemotherChecks
{
    internal static void Native(FixtureValue root)
    {
        const string sting = "356a00a4-010b-462b-b683-2ef296d076a8";
        const string torch = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        int positive = 0;
        bool deployment = false, hand = false, draw = false, discard = false;
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext source = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
            CardInstanceState[] owned = source.Cards.Hand.Concat(source.Cards.Draw).Concat(source.Cards.Discard)
                .Concat(source.OtherPiles!.SelectMany(pile => pile.Cards)).Select(token => source.FindCard(token.InstanceId)!)
                .DistinctBy(card => card.InstanceId).ToArray();
            CardInstanceState[] stings = owned.Where(card => card.DataId == sting).ToArray();
            if (stings.Length == 0 || changed.Cards.Hand.Count == 0) continue;
            int Bonus(CardInstanceState card) => card.Temporary.Upgrades.Where(upgrade =>
                upgrade.DataId == "40ed330b-04ae-469d-8653-9223e3c449f3").Sum(upgrade => upgrade.Stats.Damage);
            if (before.Spawn.Train.Rooms.Any(room => room.Deployment))
            {
                Require(stings.All(card => Bonus(changed.FindCard(card.InstanceId)!) == Bonus(card)), "Vinemother must skip deployment.");
                deployment = true;
                continue;
            }
            Require(stings.Length == 16 && stings.All(card => Bonus(changed.FindCard(card.InstanceId)!) == Bonus(card) + 4),
                "Vinemother must apply a distinct native +4 Sting upgrade to every owned Sting.");
            Require(owned.Where(card => card.DataId == torch).All(card =>
                Bonus(changed.FindCard(card.InstanceId)!) == 0), "Vinemother's OnlyStings filter must exclude Torch.");
            bool IsSting(CardToken card) => card.DataId == sting;
            hand |= source.Cards.Hand.Any(IsSting);
            draw |= source.Cards.Draw.Any(IsSting);
            discard |= source.Cards.Discard.Any(IsSting);
            positive++;
        }
        Require(positive >= 2 && deployment && hand && draw && discard,
            "Vinemother needs repeated upgrades across Hand/Draw/Discard and a deployment control.");
        Console.WriteLine($"ORIGINAL-VINEMOTHER PASS: {positive} native whole-deck upgrades, sixteen Stings, Hand/Draw/Discard, Torch and deployment controls.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
