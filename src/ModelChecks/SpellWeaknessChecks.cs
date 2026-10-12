using MonsterTrain2Poju.Model;

internal static class SpellWeaknessChecks
{
    internal static void Run()
    {
        CardMaskDefinition spellDefinition = Definition("spell", "Spell");
        CardMaskDefinition monsterDefinition = Definition("monster", "Monster");
        CardInstanceState spell = Card(7, spellDefinition);
        CardInstanceState monster = Card(8, monsterDefinition);
        CombatStatus weakness = new("spell weakness", 2);

        RoomCombatResult spellHit = Room(spell, Unit(weakness)).ApplyDamage(1, 3, spell.InstanceId);
        Require(spellHit.Supported && spellHit.State!.Units.Single().Health == 41 &&
            spellHit.State.Units.Single().Status("spell weakness")?.Stacks == 2,
            "Spell Weakness did not multiply spell damage by stacks plus one while retaining stacks.");

        RoomCombatResult monsterHit = Room(monster, Unit(weakness)).ApplyDamage(1, 3, monster.InstanceId);
        Require(monsterHit.Supported && monsterHit.State!.Units.Single().Health == 47,
            "Spell Weakness modified damage from a non-spell source card.");

        CombatStatus shield = new("damage shield", 1, removeWhenTriggered: true);
        RoomCombatResult shielded = Room(spell, Unit(weakness, shield)).ApplyDamage(1, 3, spell.InstanceId);
        Require(shielded.Supported && shielded.State!.Units.Single().Health == 50 &&
            shielded.State.Units.Single().Status("damage shield") == null &&
            shielded.State.Units.Single().Status("spell weakness")?.Stacks == 2,
            "Spell Weakness amplified damage through a non-piercing shield.");

        CombatRelicState piercing = new("piercing", "piercing", [RelicModel.AllDamageIsPiercing]);
        RoomCombatResult pierced = Room(spell, Unit(weakness, shield), [piercing]).ApplyDamage(1, 3, spell.InstanceId);
        Require(pierced.Supported && pierced.State!.Units.Single().Health == 41 &&
            pierced.State.Units.Single().Status("damage shield")?.Stacks == 1,
            "Piercing spell damage did not pass the Spell Weakness shield gate.");

        CombatStatus afterCard = new("spell weakness", 2, removeWhenTriggeredAfterCardPlayed: true);
        RoomCombatResult marked = Room(spell, Unit(afterCard)).ApplyDamage(1, 3, spell.InstanceId);
        Require(marked.Supported && marked.State!.Units.Single().Status("spell weakness")?.MarkedForRemovalAfterCardPlayed == true,
            "Spell Weakness did not retain its after-card removal marker after triggering.");
        RoomCombatState markedState = marked.State ?? throw new InvalidOperationException("Marked Spell Weakness room state is missing.");
        TrainCombatState markedTrain = new([markedState], [], 0, markedState.Context);
        TrainCombatResult afterCardRemoval = TrainCombatModel.ProcessDelayedStatusRemovalsAfterCardPlayed(markedTrain);
        CombatUnit? removedWeakness = afterCardRemoval.State?.Rooms.Single().Units.Single();
        Require(afterCardRemoval.Supported && removedWeakness!.Status("spell weakness") == null &&
            removedWeakness.RegisteredStatus("spell weakness") is CombatStatus retainedWeakness &&
            retainedWeakness.Stacks == 0 && !retainedWeakness.MarkedForRemovalAfterCardPlayed,
            "Marked Spell Weakness did not clear after card completion in both active and retained status state.");

        CombatStatus atEndOfTurn = new("spell weakness", 2, removeAtEndOfTurnIfTriggered: true);
        RoomCombatResult scheduled = Room(spell, Unit(atEndOfTurn)).ApplyDamage(1, 3, spell.InstanceId);
        Require(scheduled.Supported && scheduled.State!.Units.Single().Status("spell weakness")?.RemoveAllAtEnd == true,
            "Spell Weakness did not schedule its end-of-turn removal when triggered.");

        CardInstanceState untypedSource = CardInstanceState.Empty(99, "unknown-source");
        RoomCombatState missingSource = new(0, false, [Unit(weakness)], [], Context(untypedSource));
        RoomCombatResult unknown = RoomCombatModel.ApplyCardDamage(missingSource, 1, 3, 99);
        Require(!unknown.Supported && unknown.UnsupportedReason!.Contains("source card type", StringComparison.Ordinal),
            "Spell Weakness guessed when the native source card type was missing: " + unknown.UnsupportedReason);

        Console.WriteLine("SPELL-WEAKNESS-CHECKS PASS: spell source, multiplier, persistence, shield and piercing gates, delayed card/end-turn removal, missing card type.");
    }

    private static CardMaskDefinition Definition(string id, string type) =>
        new(id, type, "Common", type == "Monster", type == "Monster", type == "Monster",
            [], [], [], [], null, false, 0, 0, false, false, false);

    private static CardInstanceState Card(int id, CardMaskDefinition definition) =>
        new(id, definition.DataId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: new CardMaskDescriptor(definition, 0, [], []));

    private static CombatUnit Unit(params CombatStatus[] statuses) =>
        new(1, "target", CombatTeam.Enemy, 0, 50, 50, false, false, false, statuses, statusRegistry: statuses);

    private static CombatContext Context(CardInstanceState? card = null, IReadOnlyList<CombatRelicState>? relics = null)
    {
        UnityRng rng = UnityRng.Seed(37);
        return new(new([], [], [], rng, 0, []), rng, 0, 100, 10,
            cardInstances: card == null ? [] : [card], cardRegistry: card == null ? [] : [card], relics: relics);
    }

    private static RoomCombatState Room(CardInstanceState card, CombatUnit target,
        IReadOnlyList<CombatRelicState>? relics = null) => new(0, false, [target], [], Context(card, relics));

    private static RoomCombatResult ApplyDamage(this RoomCombatState room, int targetId, int damage, int sourceCardId) =>
        RoomCombatModel.ApplyCardDamage(room, targetId, damage, sourceCardId);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
