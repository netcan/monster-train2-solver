using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class HealingTriggerChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(19);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 9, 10,
            statistics: BattleStatistics.Empty().TrackCards([8]), cardInstances: [CardInstanceState.Empty(8, "healer")]);
        CombatEffect Gold(int value) => new("CardEffectRewardGold", value, 0, "", 0, [], false);
        CombatTrigger Trigger(CombatEffect effect, bool once = false, bool ignored = false, bool skip = false, int count = 1) =>
            new("OnHeal", once, false, ignored, count, [effect], skip);
        CombatTrigger[] rewards = [Trigger(Gold(2)), Trigger(Gold(3), once: true), Trigger(Gold(1), ignored: true)];
        CombatUnit Unit(int hp = 20, bool healable = true, CombatStatus[]? statuses = null, CombatTrigger[]? triggers = null) =>
            new(1, "healer", CombatTeam.Player, 3, hp, 20, true, false, false, statuses ?? [], triggers ?? rewards,
                spawnerCardId: 8, size: 1, modifiers: new(3, 0, 0, 1, 1, healable, false, []));
        RoomCombatState Room(CombatUnit unit, bool deployment = false) => new(0, deployment, [unit], [], context);
        var root = Room(Unit());
        string parent = JsonSerializer.Serialize(root);
        var first = RoomCombatModel.ApplyCardHeal(root, 1, 5);
        Require(first.Supported && first.State!.Units.Single().Health == 20 && first.State.Context!.Gold == 15 &&
            first.State.Units.Single().Triggers.All(trigger => trigger.HasTriggered), "Full-health healing skipped OnHeal.");
        var second = RoomCombatModel.ApplyCardHeal(first.State!, 1, 0);
        Require(second.Supported && second.State!.Context!.Gold == 25, "Zero healing or once-only trigger state differs.");
        var immune = RoomCombatModel.ApplyCardHeal(Room(Unit(5, statuses: [new("heal immunity", 1)])), 1, 9);
        Require(immune.Supported && immune.State!.Units.Single().Health == 5 && immune.State.Context!.Gold == 15,
            "Healing immunity suppressed OnHeal.");
        var silent = RoomCombatModel.ApplyCardHeal(Room(Unit(statuses: [new("silenced", 1)])), 1, 0);
        Require(silent.Supported && silent.State!.Context!.Gold == 5 &&
            silent.State.Units.Single().Triggers.Take(2).All(trigger => !trigger.HasTriggered),
            "Silence or ignored-silence handling differs.");
        var skipped = RoomCombatModel.ApplyCardHeal(Room(Unit(triggers: [Trigger(Gold(2), skip: true)]), true), 1, 0);
        Require(skipped.Supported && skipped.State!.Context!.Gold == 0 && !skipped.State.Units.Single().Triggers.Single().HasTriggered,
            "Deployment restriction fired a trigger or consumed its once state.");
        Require(RoomCombatModel.ApplyCardHeal(Room(Unit(triggers: [Trigger(Gold(2), count: 2)])), 1, 0).State!.Context!.Gold == 10,
            "OnHeal fire count was lost.");
        var unhealable = RoomCombatModel.ApplyCardHeal(Room(Unit(5, healable: false)), 1, 8);
        var negative = RoomCombatModel.ApplyCardHeal(Room(Unit(5, statuses: [new("heal multiplier", 1, -1)])), 1, 8);
        Require(unhealable.State!.Context!.Gold == 0 && negative.State!.Context!.Gold == 0,
            "An ineligible heal fired triggers.");
        var upgrade = new CardUpgradeModifier("hp", "hp", new(health: 3), [], false, false, false, 0, 0, []);
        var maxHealth = UnitModifierModel.Apply(Room(Unit(5)), 1, upgrade, "TemporaryUntilUnitDeath");
        Require(maxHealth.Supported && maxHealth.State!.Units.Single().Health == 8 && maxHealth.State.Context!.Gold == 0,
            "Max-health upgrade healing fired OnHeal.");
        var consumed = new CombatTrigger("OnHeal", true, true, false, 1,
            [new("CardEffectDespawnCharacter", 1, 1, "", 0, [], false)], false);
        var consumedUnit = Unit(5, statuses: [new("regen", 2, 1)], triggers: [consumed]);
        Require(RoomCombatModel.Resolve(Room(consumedUnit)).State!.Units.Count == 1 &&
            RoomCombatModel.Resolve(new(0, false, [consumedUnit], [], context, preview: true)).State!.Units.Count == 0 && consumed.HasTriggered,
            "Preview once-only flags did not reset independently of real trigger state.");

        var generate = new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false);
        var generated = RoomCombatModel.ApplyCardHeal(Room(Unit(triggers: [Trigger(generate)])), 1, 0);
        Require(generated.Supported && generated.State!.Context!.Cards.Hand.Single().InstanceId == 9 &&
            generated.State.Context.NextCardId == 10 && !generated.State.Context.BattleRng.Equals(rng) &&
            generated.State.Context.CardInstances!.Any(card => card.InstanceId == 9), "OnHeal generation lost identity or RNG.");
        var despawn = new CombatEffect("CardEffectDespawnCharacter", 1, 1, "", 0, [], false);
        var gone = CardSpellModel.Apply(Room(Unit(triggers: [Trigger(despawn)])),
            [new("Heal", "DropTargetCharacter", 0, false, true, []), new("Damage", "LastTargetedCharacters", 4, false, true, [])], 1);
        Require(gone.Supported && gone.State!.Units.Count == 0 && gone.State.Context!.Statistics!.MonstersDeadThisBattle == 0 &&
            gone.State.Context.Statistics.Value(8, "TimesExhausted", "ThisBattle") == 1,
            "OnHeal despawn ran death callbacks or retargeted a later effect.");
        var legacy = new CombatTrigger("OnHeal", false, false, false, 1, [Gold(1)]);
        Require(!RoomCombatModel.ApplyCardHeal(Room(Unit(triggers: [legacy])), 1, 0).Supported &&
            !RoomCombatModel.ApplyCardHeal(Room(Unit(triggers: [Trigger(new("Unknown", 0, 0, "", 0, [], false))])), 1, 0).Supported,
            "Incomplete healing triggers returned a search child.");
        string expected = JsonSerializer.Serialize(first.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyCardHeal(root, 1, 5).State) == expected,
            "Parallel OnHeal branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "OnHeal mutated the parent.");
        Console.WriteLine("ONHEAL-CHECKS PASS: zero/full/immune heals, once/silence/deployment/fire count, generation/RNG, despawn, max-health exclusion and isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
