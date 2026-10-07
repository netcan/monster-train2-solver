using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class BonusDrawScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardUpgradeData a = DynamicUpgradeScenario.Upgrade("PojuBonusDrawA", "01c79000-5400-4000-8000-000000000001", 1, 1, 0, 0, "armor", 0);
            CardUpgradeData b = DynamicUpgradeScenario.Upgrade("PojuBonusDrawB", "01c79000-5400-4000-8000-000000000002", 2, 0, 0, 0, "armor", 0);
            CardState[] roomSpells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData room = save.GetAllGameData().FindCardData(roomSpells[0].GetCardDataID())!;
            room.GetEffects().Clear();
            CardEffectData ranged = Next(0, b); Set(ranged, "useIntRange", true); Set(ranged, "paramMinInt", -2); Set(ranged, "paramMaxInt", 4); Set(ranged, "paramMultiplier", .75f);
            room.GetEffects().AddRange(new[] { Next(2, a), Next(0, a), Next(-1, b), ranged, Next(3, null) });
            Set(room, "targetless", false); Set(room, "targetsRoom", true);
            foreach (CardState card in roomSpells) card.Setup(room, save);
            CardState[] damageSpells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            CardData damage = save.GetAllGameData().FindCardData(damageSpells[0].GetCardDataID())!;
            if (lethal) Set(damage.GetEffects()[0], "paramInt", 9999);
            damage.GetEffects().Add(Next(2, a));
            var draw = new CardEffectData("CardEffectDraw", null!, Team.Type.None); draw.Cheat_SetTargetMode(TargetMode.Room); Set(draw, "paramInt", 0);
            damage.GetEffects().Add(draw);
            foreach (CardState card in damageSpells) card.Setup(damage, save);
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var trigger = HealingScenario.HealGold(0, false, true); Set(trigger, "trigger", CharacterTriggerData.Trigger.PreCombat);
            CardEffectData unitDraw = Next(1, a); unitDraw.Cheat_SetTargetMode(TargetMode.Self);
            Set(trigger, "effects", new List<CardEffectData> { unitDraw });
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { trigger }).ToList());
            foreach (CardState card in stewards)
            {
                card.Setup(steward, save); var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHP(20);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            // Reuse the exact native effect twice to seed shared-counter, distinct-listener state.
            CardEffectState effect = roomSpells[0].GetEffectStates()[0];
            for (int index = 0; index < 2; index++)
            {
                var parameters = new CardEffectParams { playedCard = roomSpells[0], selectedRoom = managers.GetRoomManager()!.GetSelectedRoom() };
                IEnumerator applied = effect.GetCardEffect().ApplyEffect(effect, parameters, managers.GetCoreManagers(), managers.GetSystemManagers());
                while (applied.MoveNext()) throw new InvalidOperationException("Native bonus-draw effect unexpectedly yielded during fixture setup.");
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("BONUS-DRAW-PREPARED shared duplicate listeners, signed/ranged/zero pending counts, ordinary zero-draw cancellation, unit PreCombat bonus upgrades, hand caps and original Boss/waves; lethal=" + lethal);
        }
        private static CardEffectData Next(int amount, CardUpgradeData? upgrade)
        {
            var effect = new CardEffectData("CardEffectDrawAdditionalNextTurn", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", amount); Set(effect, "paramCardUpgradeData", upgrade!); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
