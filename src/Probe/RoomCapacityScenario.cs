using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomCapacityScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] rooms = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData room = save.GetAllGameData().FindCardData(rooms[0].GetCardDataID())!;
            room.GetEffects().Clear();
            CardEffectData ignoredRange = Capacity(4, Team.Type.Monsters); Set(ignoredRange, "useIntRange", true);
            Set(ignoredRange, "paramMinInt", -10); Set(ignoredRange, "paramMaxInt", 10); Set(ignoredRange, "paramMultiplier", .75f);
            room.GetEffects().AddRange(new[] { Capacity(-100, Team.Type.Monsters), Capacity(-1, Team.Type.Monsters),
                Capacity(100, Team.Type.Monsters), Capacity(1, Team.Type.Monsters), Capacity(-25, Team.Type.Monsters),
                Capacity(0, Team.Type.None), Capacity(int.MaxValue, Team.Type.Monsters), ignoredRange,
                Capacity(-100, Team.Type.Heroes), Capacity(100, Team.Type.Heroes), Capacity(-25, Team.Type.Heroes),
                Capacity(2, Team.Type.Heroes | Team.Type.Monsters), Capacity(3, Team.Type.Heroes, true) });
            Set(room.GetEffects().Last(), "shouldCancelSubsequentEffectsIfTestFails", true);
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room);
            Set(gain, "paramInt", 1); room.GetEffects().Add(gain);
            Set(room, "targetless", false); Set(room, "targetsRoom", true);
            foreach (CardState card in rooms) card.Setup(room, save);
            CardState[] targeted = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" &&
                effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            CardData damage = save.GetAllGameData().FindCardData(targeted[0].GetCardDataID())!;
            if (lethal) Set(damage.GetEffects()[0], "paramInt", 9999);
            damage.GetTraits().Add(Trait("UnmodifiedPlayedCost", 3)); damage.GetTraits().Add(Trait("PlayedCost", -1));
            damage.GetEffects().Add(Capacity(0, Team.Type.Monsters));
            damage.GetEffects().Add(Capacity(-1, Team.Type.Heroes, true));
            foreach (CardState card in targeted) card.Setup(damage, save);
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var trigger = HealingScenario.HealGold(0, false, true); Set(trigger, "trigger", CharacterTriggerData.Trigger.PreCombat);
            CardUpgradeData size = DynamicUpgradeScenario.Upgrade("PojuCapacitySize", "01c79100-5400-4000-8000-000000000001", 0, 0, 0, 1, "armor", 0);
            Set(size, "restrictSizeToRoomCapacity", true);
            var sizeEffect = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            sizeEffect.Cheat_SetTargetMode(TargetMode.Self); Set(sizeEffect, "paramCardUpgradeData", size);
            Set(sizeEffect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            Set(trigger, "effects", new List<CardEffectData> { Capacity(1, Team.Type.Monsters), sizeEffect, Capacity(-1, Team.Type.Heroes, true) });
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { trigger }).ToList());
            foreach (CardState card in stewards)
            {
                card.Setup(steward, save); var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHP(20);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("CAPACITY-PREPARED occupied-room shrink, bounds, signed wrap, ignored ranges, both teams/combined flags, enemy-presence cancellation, ordered paid-cost scaling, unit PreCombat and original Boss/waves; lethal=" + lethal);
        }
        private static CardTraitData Trait(string type, int amount)
        {
            var data = new CardTraitData(); data.Setup("CardTraitScalingAdjustCapacity"); data.SetParamInt(amount);
            Set(data, "paramTrackedValue", Enum.Parse(typeof(CardStatistics.TrackedValueType), type));
            Set(data, "paramEntryDuration", CardStatistics.EntryDuration.ThisTurn); return data;
        }
        private static CardEffectData Capacity(int amount, Team.Type team, bool onlyIfNoEnemies = false)
        {
            var effect = new CardEffectData("CardEffectAdjustRoomCapacity", null!, team); effect.Cheat_SetTargetMode(TargetMode.Room);
            Set(effect, "paramInt", amount); Set(effect, "paramBool", onlyIfNoEnemies); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
