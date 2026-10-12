using System;
using System.Linq;
using MonsterTrain2Poju.Model;

internal static class DamagePerHpChecks
{
    internal static void Run()
    {
        CardActionEffect configured = Effect(2, allowEnemy: false, allowPlayer: true, lastAttacker: true, includeArmor: true);
        CardPlayRule card = new("damage-per-hp", "damage-per-hp", 0, "Spell", "Discard", null, [], [configured], upgradeInteractions: []);
        CardActionEffect resolved = CardModifierModel.Resolve(card, CardInstanceState.Empty(90, card.DataId)).Effects.Single();
        Require(resolved.DamagePerHpLastAttacker && resolved.DamagePerHpIncludeArmor,
            "Card modifier resolution discarded the native DamagePerHp parameters.");

        BattleTurnState plain = UnitAbilityChecks.Root();
        TrainSpellResult multiplied = CardSpellModel.Apply(plain.Spawn.Train, 0,
            [Effect(2, allowEnemy: false, allowPlayer: true)], targetId: 10);
        Require(multiplied.Supported && Unit(multiplied.State!, 11).Health == 4,
            "Damage-per-HP did not multiply the selected unit's current HP against the opposing front.");

        BattleTurnState armored = UnitAbilityChecks.Root(actorArmor: 3);
        TrainSpellResult withArmor = CardSpellModel.Apply(armored.Spawn.Train, 0,
            [Effect(1, allowEnemy: false, allowPlayer: true, includeArmor: true)], targetId: 10);
        Require(withArmor.Supported && Unit(withArmor.State!, 11).Health == 19,
            "Damage-per-HP did not include armor stacks when requested.");

        BattleTurnState attacker = UnitAbilityChecks.Root(enemyHealth: 5, enemyLastAttackerId: 10);
        TrainSpellResult lastAttacker = CardSpellModel.Apply(attacker.Spawn.Train, 0,
            [Effect(1, allowEnemy: true, allowPlayer: false, lastAttacker: true)], targetId: 11);
        Require(lastAttacker.Supported && Unit(lastAttacker.State!, 10).Health == 13 && Unit(lastAttacker.State!, 11).Health == 5,
            "Damage-per-HP did not redirect damage to the selected unit's last attacker.");

        BattleTurnState unknownAttacker = UnitAbilityChecks.Root(actorLastAttackerId: null);
        CardActionEffect lastAttackerEffect = Effect(1, allowEnemy: false, allowPlayer: true, lastAttacker: true);
        SpellCastCheck unknownTest = CardSpellModel.TestPlay(unknownAttacker.Spawn.Train, 0, [lastAttackerEffect], 10);
        TrainSpellResult missingRelationship = CardSpellModel.Apply(unknownAttacker.Spawn.Train, 0,
            [lastAttackerEffect], targetId: 10);
        Require(!unknownTest.Supported && !missingRelationship.Supported &&
            missingRelationship.UnsupportedReason!.Contains("captured attacker reference", StringComparison.Ordinal),
            "Missing last-attacker state was offered to search or guessed during application.");

        BattleTurnState knownNone = UnitAbilityChecks.Root(actorLastAttackerId: 0);
        TrainSpellResult noAttacker = CardSpellModel.Apply(knownNone.Spawn.Train, 0,
            [lastAttackerEffect], targetId: 10);
        Require(noAttacker.Supported && Unit(noAttacker.State!, 11).Health == 40,
            "A captured absence of a last attacker did not resolve to no damage.");
        Console.WriteLine("DAMAGE-PER-HP-CHECKS PASS: HP multiplier, optional armor, last-attacker redirect and missing-reference rejection.");
    }

    private static CardActionEffect Effect(int multiplier, bool allowEnemy, bool allowPlayer,
        bool lastAttacker = false, bool includeArmor = false) =>
        new("DamagePerHp", "DropTargetCharacter", multiplier, allowEnemy, allowPlayer, [],
            damagePerHpLastAttacker: lastAttacker, damagePerHpIncludeArmor: includeArmor);

    private static CombatUnit Unit(TrainCombatState state, int id) =>
        state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
