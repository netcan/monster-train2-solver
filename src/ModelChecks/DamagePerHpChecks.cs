using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamagePerHpChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules != null && source.Spawn.Train.Context != null)
        {
            var cardIds = source.Spawn.Train.Context.Cards.Hand
                .Where(card => source.PlayRules.Cards.Any(rule => rule.DataId == card.DataId &&
                    rule.Effects.Any(effect => effect.Type == "DamagePerHp")))
                .Select(card => card.InstanceId).ToHashSet();
            if (cardIds.Count > 0)
            {
                PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                    .FirstOrDefault(play => cardIds.Contains(play.CardInstanceId) && play.TargetUnitId > 0 &&
                        source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                            .Any(unit => unit.Id == play.TargetUnitId && unit.Team == CombatTeam.Player));
                if (action != null) return action;
            }
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Native(FixtureValue actions)
    {
        int plays = 0;
        int opposingDamage = 0;
        foreach (FixtureValue entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardToken card = before.Spawn.Train.Context!.Cards.Hand.Single(item => item.InstanceId == action.CardInstanceId);
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            CardActionEffect? effect = rule.Effects.SingleOrDefault(item => item.Type == "DamagePerHp");
            if (effect == null) continue;

            plays++;
            Require(action.TargetUnitId > 0 && effect.Target == "DropTargetCharacter" && effect.Value == 1 &&
                !effect.DamagePerHpLastAttacker && !effect.DamagePerHpIncludeArmor,
                "Native DamagePerHp action lost its selected-target multiplier parameters.");
            CombatUnit selected = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
            Require(selected.Team == CombatTeam.Player, "Native DamagePerHp did not select a friendly source unit.");
            RoomCombatState room = before.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            CombatUnit? opposingFront = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy);
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (opposingFront == null) continue;
            bool changed = !actual.Spawn.Train.Rooms.SelectMany(item => item.Units).Any(unit => unit.Id == opposingFront.Id) ||
                actual.Spawn.Train.Rooms.SelectMany(item => item.Units)
                    .Any(unit => unit.Id == opposingFront.Id && unit.Health < opposingFront.Health);
            if (changed) opposingDamage++;
        }
        Require(plays > 0 && opposingDamage > 0,
            "Native DamagePerHp was not played against a friendly unit with a visible opposing damage result.");
        Console.WriteLine($"NATIVE-DAMAGE-PER-HP-CHECKS PASS: {plays} effect plays, {opposingDamage} damaged opposing fronts.");
    }

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
