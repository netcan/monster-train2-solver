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
                    .FirstOrDefault(play =>
                    {
                        if (!cardIds.Contains(play.CardInstanceId) || play.TargetUnitId <= 0) return false;
                        CombatUnit? unit = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                            .FirstOrDefault(item => item.Id == play.TargetUnitId && item.Team == CombatTeam.Player);
                        if (unit == null) return false;
                        string dataId = source.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == play.CardInstanceId).DataId;
                        CardActionEffect effect = source.PlayRules!.Cards.Single(rule => rule.DataId == dataId)
                            .Effects.Single(item => item.Type == "DamagePerHp");
                        return (!effect.DamagePerHpLastAttacker || unit.LastAttackerId is > 0) &&
                            (!effect.DamagePerHpIncludeArmor || unit.Statuses.Any(status => status.Id == "armor" && status.Stacks > 0));
                    });
                if (action != null) return action;
            }
        }
        bool hasCapturedTargetRequirement = source.PlayRules != null && source.Spawn.Train.Context != null &&
            source.Spawn.Train.Context.Cards.Hand.Any(card => source.PlayRules.Cards.Any(rule => rule.DataId == card.DataId &&
                rule.Effects.Any(effect => effect.Type == "DamagePerHp" &&
                    (effect.DamagePerHpLastAttacker || effect.DamagePerHpIncludeArmor))));
        return hasCapturedTargetRequirement
            ? BattleActionModel.ChooseUnitAndJunkPlay(source)
            : BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static PlayCardAction? ChooseAbility(BattleTurnState source)
    {
        foreach (PlayCardAction action in UnitAbilityModel.EnumerateSupportedActivations(source))
        {
            RoomCombatState? actorRoom = source.Spawn.Train.Rooms.FirstOrDefault(room =>
                room.Units.Any(unit => unit.Id == action.ActivatorUnitId));
            CombatUnit? actor = actorRoom?.Units.FirstOrDefault(unit => unit.Id == action.ActivatorUnitId);
            if (actor == null || actor.LastAttackerId is not > 0) continue;
            CombatUnit? attacker = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == actor.LastAttackerId.Value && unit.Team == CombatTeam.Enemy);
            if (attacker == null) continue;
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (!result.Supported) continue;
            CombatUnit? after = result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == attacker.Id);
            if (after == null || after.Health < attacker.Health) return action;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Native(FixtureValue fixture)
    {
        int plays = 0;
        int opposingDamage = 0;
        int lastAttackerPlays = 0;
        int armorPlays = 0;
        string scenario = fixture.GetProperty("ModifierScenario").GetString()!;
        FixtureValue actions = fixture.GetProperty("Actions");
        if (scenario == "damage-per-hp-ability")
        {
            NativeAbility(actions);
            return;
        }
        foreach (FixtureValue entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardToken card = before.Spawn.Train.Context!.Cards.Hand.Single(item => item.InstanceId == action.CardInstanceId);
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            CardActionEffect? effect = rule.Effects.SingleOrDefault(item => item.Type == "DamagePerHp");
            if (effect == null) continue;

            plays++;
            Require(action.TargetUnitId > 0 && effect.Target == "DropTargetCharacter" && effect.Value == 1,
                "Native DamagePerHp action lost its selected-target multiplier parameters.");
            CombatUnit selected = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
            Require(selected.Team == CombatTeam.Player, "Native DamagePerHp did not select a friendly source unit.");
            RoomCombatState room = before.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            if (effect.DamagePerHpIncludeArmor)
            {
                armorPlays++;
                Require(selected.Statuses.Any(status => status.Id == "armor" && status.Stacks > 0),
                    "Native DamagePerHp Armor flag was not exercised on a unit with Armor.");
            }
            int expectedTargetId;
            if (effect.DamagePerHpLastAttacker)
            {
                lastAttackerPlays++;
                if (action.ActivatorUnitId <= 0) expectedTargetId = 0;
                else
                {
                    CombatUnit source = before.Spawn.Train.Rooms.SelectMany(item => item.Units)
                        .Single(unit => unit.Id == action.ActivatorUnitId);
                    expectedTargetId = source.LastAttackerId.GetValueOrDefault();
                }
            }
            else expectedTargetId = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy)?.Id ?? 0;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (effect.DamagePerHpLastAttacker && expectedTargetId == 0)
            {
                Require(selected.LastAttackerId is > 0,
                    "Native null-selfTarget fixture did not preserve a selected unit with a captured last attacker.");
                var beforeHealth = before.Spawn.Train.Rooms.SelectMany(item => item.Units)
                    .ToDictionary(unit => unit.Id, unit => unit.Health);
                var afterHealth = actual.Spawn.Train.Rooms.SelectMany(item => item.Units)
                    .ToDictionary(unit => unit.Id, unit => unit.Health);
                Require(beforeHealth.All(item => afterHealth.TryGetValue(item.Key, out int health) && health == item.Value),
                    "Native DamagePerHp with null selfTarget changed a unit's health.");
                continue;
            }
            CombatUnit? expectedTarget = before.Spawn.Train.Rooms.SelectMany(item => item.Units)
                .FirstOrDefault(unit => unit.Id == expectedTargetId);
            if (expectedTarget == null) continue;
            bool changed = !actual.Spawn.Train.Rooms.SelectMany(item => item.Units).Any(unit => unit.Id == expectedTarget.Id) ||
                actual.Spawn.Train.Rooms.SelectMany(item => item.Units)
                    .Any(unit => unit.Id == expectedTarget.Id && unit.Health < expectedTarget.Health);
            if (changed)
            {
                opposingDamage++;
            }
        }
        Require(plays > 0 && (scenario == "damage-per-hp-last-attacker" ? lastAttackerPlays > 0 : opposingDamage > 0),
            "Native DamagePerHp did not exercise its expected target behavior.");
        if (scenario == "damage-per-hp-armor")
            Require(armorPlays > 0, "Native DamagePerHp Armor scenario did not exercise its parameter.");
        if (scenario == "damage-per-hp-last-attacker")
            Require(lastAttackerPlays > 0, "Native DamagePerHp last-attacker scenario did not exercise its parameter.");
            Console.WriteLine($"NATIVE-DAMAGE-PER-HP-CHECKS PASS: {plays} effect plays, {opposingDamage} expected targets damaged, {lastAttackerPlays} last-attacker plays.");
    }

    private static void NativeAbility(FixtureValue actions)
    {
        int plays = 0, redirectedHits = 0;
        foreach (FixtureValue entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (action.ActivatorUnitId <= 0) continue;
            CombatUnit actor = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.Id == action.ActivatorUnitId);
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == actor.Ability!.DataId);
            CardActionEffect? effect = rule.Effects.SingleOrDefault(item => item.Type == "DamagePerHp");
            if (effect == null) continue;
            plays++;
            RoomCombatState actorRoom = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == actor.Id));
            RoomCombatState targetRoom = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            Require(effect.Target == "FrontInRoom" && effect.Value == 1 && effect.DamagePerHpLastAttacker &&
                !effect.DamagePerHpIncludeArmor && actor.LastAttackerId is > 0 && targetRoom.Units.Any(unit =>
                    unit.Team == CombatTeam.Enemy && unit.Health > 0),
                "Native unit ability did not provide a live source actor, a last attacker and a legal target room.");
            CombatUnit attacker = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.Id == actor.LastAttackerId!.Value);
            Require(attacker.Team == CombatTeam.Enemy, "Native ability source's last attacker was not an enemy.");
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit? afterAttacker = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == attacker.Id);
            if (afterAttacker == null || afterAttacker.Health < attacker.Health) redirectedHits++;
        }
        Require(plays > 0 && redirectedHits > 0,
            "Native unit ability never dealt positive DamagePerHp to its source actor's last attacker.");
        Console.WriteLine($"NATIVE-DAMAGE-PER-HP-ABILITY-CHECKS PASS: {plays} ability plays, {redirectedHits} positive last-attacker hits.");
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

        CardActionEffect enemyLastAttackerEffect = Effect(1, allowEnemy: true, allowPlayer: false, lastAttacker: true);
        BattleTurnState selectedLastAttacker = UnitAbilityChecks.Root(enemyHealth: 5, enemyLastAttackerId: 10);
        SpellCastCheck noSourceTest = CardSpellModel.TestPlay(selectedLastAttacker.Spawn.Train, 0,
            [enemyLastAttackerEffect], 11);
        TrainSpellResult noSource = CardSpellModel.Apply(selectedLastAttacker.Spawn.Train, 0,
            [enemyLastAttackerEffect], targetId: 11);
        Require(noSourceTest.Supported && noSource.Supported && Unit(noSource.State!, 10).Health == 18 &&
            Unit(noSource.State!, 11).Health == 5,
            "A normal spell used the selected target's LastAttackerId despite native selfTarget being null.");

        CardActionEffect actorLastAttackerEffect = Effect(1, allowEnemy: false, allowPlayer: true, lastAttacker: true);
        BattleTurnState sameRoomRoot = UnitAbilityChecks.Root(actorLastAttackerId: 11);
        SpellCastCheck sameRoomTest = CardSpellModel.TestPlay(sameRoomRoot.Spawn.Train, 0,
            [actorLastAttackerEffect], 10, selfUnitId: 10);
        TrainSpellResult sameRoom = CardSpellModel.Apply(sameRoomRoot.Spawn.Train, 0,
            [actorLastAttackerEffect], targetId: 10, selfUnitId: 10);
        Require(sameRoomTest.Supported && sameRoom.Supported && Unit(sameRoom.State!, 11).Health == 22 &&
            Unit(sameRoom.State!, 10).Health == 18,
            "Damage-per-HP did not redirect unit-ability damage to its source's last attacker.");

        CardActionEffect enemyTargetAbilityEffect = new("DamagePerHp", "FrontInRoom", 1,
            allowEnemy: true, allowPlayer: false, [], damagePerHpLastAttacker: true);
        BattleTurnState enemyTargetSource = UnitAbilityChecks.Root(enemyHealth: 5, actorLastAttackerId: 11);
        TrainCombatState remoteActorTrain = MoveUnitToOtherRoom(enemyTargetSource.Spawn.Train, 10, 1);
        SpellCastCheck enemyTargetTest = CardSpellModel.TestPlay(remoteActorTrain, 0,
            [enemyTargetAbilityEffect], 11, selfUnitId: 10);
        TrainSpellResult enemyTargetRedirect = CardSpellModel.Apply(remoteActorTrain, 0,
            [enemyTargetAbilityEffect], targetId: 11, selfUnitId: 10);
        Require(enemyTargetTest.Supported && enemyTargetRedirect.Supported &&
            !enemyTargetRedirect.State!.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 11),
            "Damage-per-HP ability did not damage its enemy last attacker when the selected target was also an enemy.");

        BattleTurnState crossRoomRoot = UnitAbilityChecks.Root(actorLastAttackerId: 11);
        TrainCombatState crossRoomTrain = MoveUnitToOtherRoom(crossRoomRoot.Spawn.Train, 10, 1);
        SpellCastCheck crossRoomTest = CardSpellModel.TestPlay(crossRoomTrain, 1,
            [actorLastAttackerEffect], 10, selfUnitId: 10);
        TrainSpellResult crossRoom = CardSpellModel.Apply(crossRoomTrain, 1,
            [actorLastAttackerEffect], targetId: 10, selfUnitId: 10);
        Require(crossRoomTest.Supported && crossRoom.Supported && Unit(crossRoom.State!, 11).Health == 22 &&
            Unit(crossRoom.State!, 10).Health == 18,
            "Damage-per-HP did not find a live source's last attacker on another floor: test=" +
            crossRoomTest.UnsupportedReason + ", apply=" + crossRoom.UnsupportedReason + ", enemy=" +
            (crossRoom.State == null ? -1 : Unit(crossRoom.State, 11).Health) + ", source=" +
            (crossRoom.State == null ? -1 : Unit(crossRoom.State, 10).Health) + ".");

        BattleTurnState crossRoomKillRoot = UnitAbilityChecks.Root(enemyHealth: 10, actorLastAttackerId: 11);
        TrainCombatState crossRoomKillTrain = MoveUnitToOtherRoom(crossRoomKillRoot.Spawn.Train, 10, 1);
        RoomCombatState actorRoom = crossRoomKillTrain.Rooms.Single(room => room.Units.Any(unit => unit.Id == 10));
        var slay = new CombatTrigger("OnKill", once: true, hasTriggered: false, ignoreSilence: false, fireCount: 1,
            [new CombatEffect("CardEffectRewardGold", 5, 0, "", 0, [], false)], skipDuringDeployment: false);
        CombatUnit actorWithSlay = Unit(crossRoomKillTrain, 10).WithTriggers(actorRoom.Units.Single(unit => unit.Id == 10).Triggers.Append(slay).ToArray());
        RoomCombatState[] killRooms = crossRoomKillTrain.Rooms.Select(room => room.RoomIndex == actorRoom.RoomIndex
            ? new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => unit.Id == actorWithSlay.Id ? actorWithSlay : unit).ToArray(),
                room.ExternalInteractions, room.Context, room.Preview, room.ActiveCompanionBossIds)
            : room).ToArray();
        crossRoomKillTrain = new TrainCombatState(killRooms, crossRoomKillTrain.Movement,
            crossRoomKillTrain.EnemySlotsPerRoom, crossRoomKillTrain.Context);
        int goldBeforeCrossRoomKill = crossRoomKillTrain.Context!.Gold;
        TrainSpellResult crossRoomKill = CardSpellModel.Apply(crossRoomKillTrain, 1,
            [actorLastAttackerEffect], targetId: 10, selfUnitId: 10);
        Require(crossRoomKill.Supported && !crossRoomKill.State!.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 11) &&
            crossRoomKill.State.Context!.Gold == goldBeforeCrossRoomKill + 5 &&
            Unit(crossRoomKill.State, 10).Triggers.Single(trigger => trigger.Kind == "OnKill").HasTriggered,
            "Cross-room spell damage failed to route the source actor's lethal OnKill callback to its owning floor.");

        BattleTurnState unknownAttacker = UnitAbilityChecks.Root(actorLastAttackerId: null);
        SpellCastCheck unknownTest = CardSpellModel.TestPlay(unknownAttacker.Spawn.Train, 0,
            [actorLastAttackerEffect], 10, selfUnitId: 10);
        TrainSpellResult missingRelationship = CardSpellModel.Apply(unknownAttacker.Spawn.Train, 0,
            [actorLastAttackerEffect], targetId: 10, selfUnitId: 10);
        Require(!unknownTest.Supported && !missingRelationship.Supported &&
            missingRelationship.UnsupportedReason!.Contains("captured source attacker reference", StringComparison.Ordinal),
            "Missing last-attacker state was offered to search or guessed during application.");

        BattleTurnState knownNone = UnitAbilityChecks.Root(actorLastAttackerId: 0);
        SpellCastCheck knownNoneTest = CardSpellModel.TestPlay(knownNone.Spawn.Train, 0,
            [actorLastAttackerEffect], 10, selfUnitId: 10);
        TrainSpellResult noAttacker = CardSpellModel.Apply(knownNone.Spawn.Train, 0,
            [actorLastAttackerEffect], targetId: 10, selfUnitId: 10);
        Require(knownNoneTest.Supported && noAttacker.Supported && Unit(noAttacker.State!, 11).Health == 40,
            "A captured absence of the source unit's last attacker did not resolve to no damage.");
        Console.WriteLine("DAMAGE-PER-HP-CHECKS PASS: HP multiplier, optional armor, null-source behavior, source last-attacker redirection, cross-room lookup and missing-reference rejection.");
    }

    private static CardActionEffect Effect(int multiplier, bool allowEnemy, bool allowPlayer,
        bool lastAttacker = false, bool includeArmor = false) =>
        new("DamagePerHp", "DropTargetCharacter", multiplier, allowEnemy, allowPlayer, [],
            damagePerHpLastAttacker: lastAttacker, damagePerHpIncludeArmor: includeArmor);

    private static CombatUnit Unit(TrainCombatState state, int id) =>
        state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);

    private static TrainCombatState MoveUnitToOtherRoom(TrainCombatState source, int unitId, int destinationRoomIndex)
    {
        RoomCombatState sourceRoom = source.Rooms.Single(room => room.Units.Any(unit => unit.Id == unitId));
        CombatUnit moving = sourceRoom.Units.Single(unit => unit.Id == unitId);
        RoomCombatState[] rooms = source.Rooms.Select(room =>
        {
            var units = room.Units.Where(unit => unit.Id != unitId).ToList();
            if (room.RoomIndex == destinationRoomIndex) units.Add(moving);
            return new RoomCombatState(room.RoomIndex, room.Deployment, units, room.ExternalInteractions,
                room.Context, room.Preview, room.ActiveCompanionBossIds);
        }).ToArray();
        return new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, source.Context);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
