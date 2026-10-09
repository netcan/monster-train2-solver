using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardUpgradeMaskProbe
    {
        // Read serialized parameters only; do not execute a filter or create a card.
        internal static Dictionary<string, object?> Definition(CardUpgradeMaskData mask)
        {
            object Field(string name) => AccessTools.Field(typeof(CardUpgradeMaskData), name).GetValue(mask);
            string[] Strings(string name) => ((IEnumerable)Field(name)).Cast<object>().Select(value => value?.ToString() ?? "<null>").ToArray();
            var result = new Dictionary<string, object?> { ["AssetKey"] = mask.name };
            foreach (string name in new[] { "cardType", "additionalCardTypes", "requiredRarities", "excludedRarities", "requiredSubtypes",
                "excludedSubtypes", "requiredCardTraits", "excludedCardTraits", "requiredCardEffects", "excludedCardEffects" })
                result[name] = name == "cardType" ? Field(name).ToString() : (object)Strings(name);
            foreach (string name in new[] { "requiredSizes", "excludedSizes" })
                result[name] = ((IEnumerable)Field(name)).Cast<int>().ToArray();
            foreach (string name in new[] { "requiredRaritiesOperator", "excludedRaritiesOperator", "requiredSubtypesOperator", "excludedSubtypesOperator",
                "requiredStatusEffectsOperator", "excludedStatusEffectsOperator", "requiredCardTraitsOperator", "excludedCardTraitsOperator",
                "requiredCardEffectsOperator", "excludedCardEffectsOperator", "requiredLinkedClansOperator", "excludedLinkedClansOperator",
                "requiredCardUpgradesOperator", "excludedCardUpgradesOperator", "cardTargetMode", "upgradeDisabledReason" })
                result[name] = Convert.ToInt32(Field(name));
            foreach (string name in new[] { "excludeNonAttackingMonsters", "requireXCost", "excludeXCost", "excludeIfHasUnitAbility",
                "excludeIfHasGraftedEquipment", "excludeIfHasAnyUpgrades", "excludeIfHasNoUpgrades" })
                result[name] = (bool)Field(name);
            foreach (string name in new[] { "requiredStatusEffects", "excludedStatusEffects" })
                result[name] = ((IEnumerable)Field(name)).Cast<StatusEffectStackData>()
                    .Select(status => new { status.statusId, status.count, status.fromPermanentUpgrade }).ToArray();
            foreach (string name in new[] { "requiredLinkedClans", "excludedLinkedClans" })
                result[name] = ((IEnumerable)Field(name)).Cast<ClassData>().Select(data => data == null ? "<null>" : data.GetID()).ToArray();
            foreach (string name in new[] { "requiredCardUpgrades", "excludedCardUpgrades" })
                result[name] = ((IEnumerable)Field(name)).Cast<CardUpgradeData>().Select(data => data == null ? "<null>" : data.GetID()).ToArray();
            foreach (string name in new[] { "allowedCardPools", "disallowedCardPools" })
                result[name] = ((IEnumerable)Field(name)).Cast<CardPool>().Select(pool => new { AssetKey = pool.name,
                    CardIds = Enumerable.Range(0, pool.GetNumCards()).Select(index => pool.GetCardAtIndex(index)?.GetID() ?? "<null>").ToArray() }).ToArray();
            var costs = (UnityEngine.Vector2)Field("costRange");
            result["costRange"] = new[] { costs.x, costs.y };
            return result;
        }
    }
}
