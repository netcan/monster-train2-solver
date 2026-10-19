using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TriggeredTypedDrawChecks
{
    internal static void Run()
    {
        CardInstanceState Card(int id, string type) => new(id, "draw-" + id, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: new(new("draw-" + id, type, "Common", false, false, true, [], [], [], [], null, false, 1, 0, false, false, false), 1, [], []));
        RoomCombatState Root(int maxHand = 10, bool preview = false, bool blocked = false, bool equipment = true)
        {
            var effect = new CombatEffect("CardEffectDrawType", 2, 0, "", 0, [], false,
                action: new("DrawType", "Self", 2, false, false, [], typedDraw: new("Equipment")));
            var actor = new CombatUnit(1, "draw-actor", CombatTeam.Player, 0, 10, 10, true, false, false, [],
                [new("OnSpawn", false, false, false, 1, [effect], false)]);
            var rng = UnityRng.Seed(119);
            var cards = new CardCycleState([new(1, "draw-1")], [new(2, "draw-2"), new(3, "draw-3")], [new(4, "draw-4")], rng, 0, [], deckDrawDisabledThisTurn: blocked);
            var context = new CombatContext(cards, rng, 0, 5, maxHand, cardInstances: [Card(1, "Spell"), Card(2, equipment ? "Equipment" : "Spell"), Card(3, "Spell"), Card(4, equipment ? "Equipment" : "Spell")]);
            return new(0, false, [actor], [], context, preview);
        }
        RoomCombatResult Fire(RoomCombatState state) => RoomCombatModel.ApplyQueuedCharacterTrigger(state, new(0, state.Units[0], "OnSpawn"), _ => { });
        var root = Root(); string parent = JsonSerializer.Serialize(root);
        var drawn = Fire(root);
        Require(drawn.Supported && drawn.State!.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([4, 2, 1]) &&
            drawn.State.Context.Cards.Draw.Single().InstanceId == 3 && drawn.State.Context.Cards.Discard.Count == 0 &&
            drawn.State.Context.BattleRng.Equals(root.Context!.BattleRng) && drawn.State.Context.Cards.Rng.Equals(root.Context.Cards.Rng),
            drawn.UnsupportedReason ?? "Triggered type draw lost deck/discard order, type filtering or RNG.");
        string expected = JsonSerializer.Serialize(drawn.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Fire(root).State) == expected, "Triggered draw branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Triggered draw mutated its parent.");
        foreach (var control in new[] { Root(maxHand: 1), Root(preview: true), Root(blocked: true), Root(equipment: false) })
        {
            var result = Fire(control);
            Require(result.Supported && JsonSerializer.Serialize(result.State!.Context!.Cards) == JsonSerializer.Serialize(control.Context!.Cards),
                result.UnsupportedReason ?? "Full-hand, preview, blocked or missing-type draw moved cards.");
        }
        TypedDrawChecks.Run();
        Console.WriteLine("TRIGGERED-TYPED-DRAW PASS: equipment filtering, deck/discard order, full hand, preview, blocked/empty matches, unchanged RNG and 32 immutable branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
