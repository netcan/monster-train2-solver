using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class LethalRallyScenario
    {
        internal static bool Prepared;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unit, "size", 1);
            var playerRally = HealingScenario.HealGold(3, false, true);
            Set(playerRally, "trigger", CharacterTriggerData.Trigger.CardMonsterPlayed);
            var harvest = HealingScenario.HealGold(5, false, true);
            Set(harvest, "trigger", CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor);
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { playerRally, harvest }).ToList());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData boss = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss()))
                .Distinct().Single(character => character.IsMiniboss());
            CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuLethalRallyStewardPool";
            object entries = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(entries.GetType(), "Add", new[] { typeof(CardData) }).Invoke(entries, new object[] { steward });
            var generated = Effect("CardEffectAddBattleCard", TargetMode.Room, Team.Type.None, (int)CardPile.HandPile);
            Set(generated, "paramCardPool", pool); Set(generated, "additionalParamInt", 1);
            var born = Trigger(CharacterTriggerData.Trigger.OnSpawn, generated);
            var health = DynamicUpgradeScenario.Upgrade("PojuLethalRallyPermanent", "c2f6ed7f-18ce-4070-b65f-7dd9f5190011", 3, 2, 0, 0, "armor", 0);
            var permanent = Effect("CardEffectAddCardUpgradeToUnits", TargetMode.LastSpawnedCharacter, Team.Type.Monsters, 0);
            Set(permanent, "paramCardUpgradeData", health);
            Set(permanent, "additionalParamInt1", (int)UnitUpgradeLifetime.Permanent);
            var armor = DynamicUpgradeScenario.Upgrade("PojuLethalRallyArmor", "c2f6ed7f-18ce-4070-b65f-7dd9f5190012", 0, 0, 0, 0, "armor", 1);
            var temporary = Effect("CardEffectAddTempCardUpgradeToUnits", TargetMode.LastSpawnedCharacter, Team.Type.Monsters, 0);
            Set(temporary, "paramCardUpgradeData", armor); Set(temporary, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            var lethal = Trigger(CharacterTriggerData.Trigger.CardMonsterPlayed,
                Effect("CardEffectDamage", TargetMode.Self, Team.Type.Heroes, 9999), permanent, temporary,
                Effect("CardEffectRewardGold", TargetMode.Room, Team.Type.None, 7));
            var death = HealingScenario.HealGold(17, false, true); Set(death, "trigger", CharacterTriggerData.Trigger.OnDeath);
            Set(boss, "triggers", boss.GetTriggers().Concat(new[] { born, lethal, death }).ToList());
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("LETHAL-RALLY-PREPARED ordinary one-size Stewards, original Boss/waves, Boss OnSpawn hand generation, real paid summon, post-kill last-spawned upgrades and death/Harvest children.");
        }
        internal static PlayCardAction? ChooseNativeBossSummon(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            var enemies = new List<CharacterState>(); managers.GetHeroManager()!.AddCharactersToList(enemies);
            CharacterState? boss = enemies.FirstOrDefault(unit => unit.IsMiniboss() && unit.IsAlive && !unit.IsDestroyed);
            if (boss == null) return null;
            int room = boss.GetCurrentRoomIndex();
            foreach (CardState card in cards.GetHand().Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678"))
            {
                SpawnPoint point = rooms.GetRoom(room).GetMonsterPoint(0);
                if (cards.CanPlayHandCard(card, room, point, null, null, out _))
                    return new PlayCardAction(FullBattleTrace.Active!.CardId(card), room, 0);
            }
            return null;
        }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, params CardEffectData[] effects)
        {
            var trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", kind); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int amount)
        { var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", amount); return effect; }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
