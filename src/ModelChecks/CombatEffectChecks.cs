using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class CombatEffectChecks
{
    internal static void Run()
    {
        var context = new CombatContext(new CardCycleState([], [new CardToken(1, "old")], [],
            UnityRng.Seed(12), 0, []), UnityRng.Seed(42), 10, 2, 10);
        var add = new CombatEffect("CardEffectAddBattleCard", 0, 0, "DeckPileRandom", 1, ["junk"], false);
        var junker = Unit(1, 1, 4, Trigger("PostCombat", add));
        var source = new RoomCombatState(0, false, [junker], [], context);
        RoomCombatResult added = RoomCombatModel.Resolve(source);
        CombatContext after = added.State!.Context!;
        Require(after.Cards.Draw.Select(card => card.DataId).SequenceEqual(["junk", "old"]) &&
            after.NextCardId == 3 && after.BattleRng.Equals(context.BattleRng.Range(0, 1).State.Range(0, 1).State),
            "Generating a card did not advance both selection and placement RNG.");
        Require(context.Cards.Draw.Count == 1 && !junker.Triggers[0].HasTriggered,
            "A unit trigger mutated its parent context or trigger state.");
        var gold = new CombatEffect("CardEffectRewardGold", 50, 0, "", 0, [], false);
        var despawn = new CombatEffect("CardEffectDespawnCharacter", 0, 2, "", 0, [], false);
        var treasure = Unit(1, 0, 1, Trigger("OnDeath", gold), Trigger("PostCombat", despawn));
        var escaped = RoomCombatModel.Resolve(new RoomCombatState(0, false, [treasure], [], context));
        Require(escaped.State!.Units[0].Triggers[1].Effects[0].Counter == 1 && escaped.State.Context!.Gold == 10,
            "The despawn counter was not carried across turns.");
        escaped = RoomCombatModel.Resolve(escaped.State);
        Require(escaped.State!.Units.Count == 0 && escaped.State.Context!.Gold == 10,
            "An escaped treasure fired its death reward.");
        var player = new CombatUnit(2, "player", CombatTeam.Player, 5, 10, 10, true, false, false, []);
        var killed = RoomCombatModel.Resolve(new RoomCombatState(0, false, [treasure, player], [], context));
        Require(killed.State!.Context!.Gold == 60 && killed.State.Units.Count == 1,
            "A killed treasure failed to reward gold exactly once.");
        var once = new CombatTrigger("PostCombat", true, false, false, 1, [add]);
        var onceUnit = Unit(1, 0, 1, once);
        var onceRoom = new RoomCombatState(0, false, [onceUnit], [], context);
        var first = RoomCombatModel.Resolve(onceRoom).State!;
        Require(RoomCombatModel.Resolve(first).State!.Context!.NextCardId == 3,
            "A once-only trigger fired in a later turn.");
        var train = new TrainCombatState([source,
            new RoomCombatState(1, false, [Unit(3, 0, 3, Trigger("PostCombat", add))], [], context)],
            [new EnemyMovement(1, 1, true, false), new EnemyMovement(3, 1, true, false)], 7, context);
        Require(TrainCombatModel.ResolveCombat(train).State!.Context!.NextCardId == 4,
            "A later room lost an earlier room's generated card state.");
        string expected = JsonSerializer.Serialize(added.State);
        Parallel.For(0, 64, _ => Require(JsonSerializer.Serialize(RoomCombatModel.Resolve(source).State) == expected,
            "Parallel trigger execution differed."));
        Console.WriteLine("EFFECT-CHECKS PASS: generated cards and RNG, gold, despawn, once triggers, train context and isolation.");
    }
    private static CombatTrigger Trigger(string kind, CombatEffect effect) => new(kind, false, false, false, 1, [effect]);
    private static CombatUnit Unit(int id, int attack, int health, params CombatTrigger[] triggers) =>
        new(id, "enemy", CombatTeam.Enemy, attack, health, health, true, false, false, [], triggers);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
