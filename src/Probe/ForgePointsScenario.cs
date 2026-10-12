using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class ForgePointsScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData gameData = save.GetAllGameData();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            if (stewards.Length == 0) throw new InvalidOperationException("Forge Points fixture requires an owned Steward.");
            CardData data = gameData.FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger(Adjust(3), Adjust(-2), Adjust(-2, 4)));
            Set(unit, "triggers", triggers);
            save.SetForgePoints(3);
            CollectableRelicData forgeRelic = gameData.GetAllCollectableRelicData()
                .Where(relic => relic.GetEffects().Any(effect => effect.GetEffectClassName() == "RelicEffectAdditionalForgePoints"))
                .OrderBy(relic => relic.GetID(), StringComparer.Ordinal).FirstOrDefault() ??
                throw new InvalidOperationException("Forge Points fixture needs an original Additional Forge Points relic.");
            save.AddRelic(forgeRelic);
            foreach (CardState card in stewards) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("FORGE-POINTS-PREPARED original Additional Forge Points relic; OnSpawn signed fixed and ranged adjustments from a nonzero starting value: " + forgeRelic.name + "/" + forgeRelic.GetID());
        }

        private static CardEffectData Adjust(int amount, int? max = null)
        {
            var effect = new CardEffectData("CardEffectAdjustForgePoints", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", amount);
            if (max.HasValue)
            {
                Set(effect, "useIntRange", true); Set(effect, "paramMinInt", amount); Set(effect, "paramMaxInt", max.Value);
            }
            return effect;
        }

        private static CharacterTriggerData Trigger(params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), "OnSpawn"));
            Set(trigger, "effects", effects.ToList());
            return trigger;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
