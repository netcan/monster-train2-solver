using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityEffectsScenario
    {
        internal static bool Prepared;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            AbilityActivationScenario.Prepare(managers, log, false, false);
            SaveManager save = managers.GetSaveManager(); AllGameData game = save.GetAllGameData();
            CardData original = game.FindCardData("c2f6ed7f-18ce-4070-b65f-7dd9f5160063")!;
            CardData second = Clone("PojuSkillSelfRemove", "c2f6ed7f-18ce-4070-b65f-7dd9f5160071");
            CardData queued = Clone("PojuSkillQueuedGrant", "c2f6ed7f-18ce-4070-b65f-7dd9f5160072");
            Set(original, "effects", new List<CardEffectData> {
                Effect("Damage", 2, TargetMode.FrontInRoom, Team.Type.Heroes), Change(second, TargetMode.Self, Team.Type.Heroes),
                Effect("Heal", 1, TargetMode.Self, Team.Type.Heroes) });
            Set(second, "effects", new List<CardEffectData> {
                Effect("Damage", 2, TargetMode.FrontInRoom, Team.Type.Heroes), Remove(TargetMode.Self, Team.Type.Heroes, true),
                Effect("Heal", 1, TargetMode.Self, Team.Type.Heroes) });
            Set(queued, "effects", new List<CardEffectData> { Effect("Damage", 2, TargetMode.FrontInRoom, Team.Type.Heroes) });
            CardData steward = game.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CharacterTriggerData preOwn = unit.GetTriggers().Single(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnPreOwnAbilityActivated);
            Set(preOwn, "effects", preOwn.GetEffects().Concat(new[] { Change(queued, TargetMode.Self, Team.Type.Heroes) }).ToList());
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardData spell = game.FindCardData(owned.First(card => card.GetCardType() == CardType.Spell && card.GetEffects()
                .Any(effect => effect.GetEffectStateName() == "CardEffectDamage")).GetCardDataID())!;
            RelicData absent = UnityEngine.Object.Instantiate(game.GetAllCollectableRelicData().First()); absent.name = "PojuInactiveRemovalGate";
            AccessTools.Field(typeof(GameData), "id").SetValue(absent, "c2f6ed7f-18ce-4070-b65f-7dd9f5160073");
            CardEffectData removal = Remove(TargetMode.LastTargetedCharacters, Team.Type.Heroes, false);
            Set(removal, "paramRelicData", absent);
            Set(spell, "effects", new List<CardEffectData> { Effect("Damage", 2, TargetMode.FrontInRoom, Team.Type.Heroes),
                Change(queued, TargetMode.Room, Team.Type.Heroes), removal,
                Change(queued, TargetMode.LastTargetedCharacters, Team.Type.Heroes) });
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID() || card.GetCardDataID() == spell.GetID()))
                card.Setup(card.GetCardDataID() == steward.GetID() ? steward : spell, save);
            save.MarkOneTimeMessageComplete(OneTimeMessage.UnitAbilityReplace);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ABILITY-EFFECTS-PREPARED multi-target spells, queued pre-own grants, cached self replacement/removal and unchanged Boss/waves.");

            CardData Clone(string name, string id)
            {
                CardData card = UnityEngine.Object.Instantiate(original); card.name = name;
                AccessTools.Field(typeof(GameData), "id").SetValue(card, id);
                Set(card, "cooldownAtSpawn", 0); Set(card, "cooldownAfterActivated", 1);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(game)).Add(card); return card;
            }
        }
        private static CardEffectData Change(CardData card, TargetMode target, Team.Type team)
        {
            var effect = Effect("SetUnitAbility", 19, target, team); Set(effect, "paramCardData", card); return effect;
        }
        private static CardEffectData Remove(TargetMode target, Team.Type team, bool permanent)
        {
            var effect = Effect("RemoveAbility", 23, target, team); Set(effect, "paramBool", permanent); return effect;
        }
        private static CardEffectData Effect(string kind, int value, TargetMode target, Team.Type team)
        {
            var effect = new CardEffectData("CardEffect" + kind, null!, team); effect.Cheat_SetTargetMode(target);
            Set(effect, "paramInt", value); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
