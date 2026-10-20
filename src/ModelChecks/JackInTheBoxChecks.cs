using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JackInTheBoxChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        const string boxId = "5fdc790d-961b-4c95-ad0c-bbb945da4e39", spearId = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
        if (!originals.Contains(boxId)) return;
        BattleTurnState initial = root.GetProperty("Actions").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardInstanceState original = initial.Spawn.Train.Context!.CardRegistry!.Single(card => card.DataId == boxId);
        bool empty = originals.Contains("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b");
        Require(original.Permanent.BoxedCards?.Count == (empty ? 0 : 3), "Missing independent initial empty/filled boxed templates.");
        if (!empty) Require(original.Permanent.BoxedCards!.Count(card => card.Creation.DataId == spearId) == 2,
            "Filled box must contain two distinct saved unit templates and a filtered spell.");
        var children = new Dictionary<int, int>();
        bool initializedEmpty = false, sawRemaining = false;
        int exhaustedObservations = 0;
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext context = after.Spawn.Train.Context!;
            CombatUnit? box = after.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == original.InstanceId);
            if (box == null) continue;
            CombatEffect effect = box.Triggers.SelectMany(trigger => trigger.Effects).Single(effect => effect.Type == "CardEffectUnboxJackInTheBoxUnitCard");
            Require(effect.BoxedPool != null && effect.BoxedPool.All(index => index is 0 or 1), "Jack pool must initialize, filter nonunits and retain saved-template indices.");
            sawRemaining |= effect.BoxedPool!.Count == 1;
            if (effect.BoxedPool.Count == 0) { initializedEmpty = true; exhaustedObservations++; }
            foreach (CombatUnit child in after.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => context.CardRegistry!.Any(card =>
                card.InstanceId == unit.SpawnerCardId && card.DataId == spearId)))
            {
                CardInstanceState card = context.CardRegistry!.Single(card => card.InstanceId == child.SpawnerCardId);
                Require(child.Status("cardless")?.Stacks == 1 && context.CardInstances!.All(owned => owned.InstanceId != card.InstanceId) &&
                    card.Permanent.Upgrades.Single().Stats.Health == 50 && card.Temporary.Upgrades.Count == 0,
                    "Unboxed children must retain their saved upgrade, independent detached card identity and native cardless status.");
                children[child.Id] = card.Permanent.Upgrades.Single().Stats.Damage;
            }
        }
        Require(initializedEmpty && exhaustedObservations >= 2 && children.Count == (empty ? 0 : 2) &&
            (empty || sawRemaining && children.Values.Order().SequenceEqual(new[] { 3, 7 })),
            "Missing repeated exhausted/empty controls, two distinct upgraded births or nonunit filtering.");
        Console.WriteLine($"ORIGINAL-JACK-IN-THE-BOX PASS: {(empty ? "empty" : "filled")} original, {children.Count} saved-template births, {exhaustedObservations} exhausted controls; preview exclusion and battle RNG replay.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
