using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicUnitUpgradeTrigger
    {
        public int EffectIndex { get; }
        public string Trigger { get; }
        public bool SourcePlayer { get; }
        public bool SourceEnemy { get; }
        public string? RequiredSubtype { get; }
        public bool SubtypeIsNone { get; }
        public bool ShouldRemove { get; }
        public int UpgradeLifetime { get; }
        public CardUpgradeModifier? Upgrade { get; }

        public RelicUnitUpgradeTrigger(int effectIndex, string trigger, bool sourcePlayer, bool sourceEnemy,
            string? requiredSubtype, bool subtypeIsNone, bool shouldRemove, int upgradeLifetime, CardUpgradeModifier? upgrade)
        {
            EffectIndex = effectIndex; Trigger = trigger; SourcePlayer = sourcePlayer; SourceEnemy = sourceEnemy;
            RequiredSubtype = requiredSubtype; SubtypeIsNone = subtypeIsNone; ShouldRemove = shouldRemove;
            UpgradeLifetime = upgradeLifetime; Upgrade = upgrade;
        }
    }

    internal static class RelicUnitUpgradeTriggerModel
    {
        internal const int UntilEndOfBattle = 0;
        internal const int UntilUnitDeath = 1;

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddRemoveTempUpgradeOnUnitTrigger)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.UnitUpgradeTriggers?.Count > 0 ? "Unexpected relic unit-upgrade trigger definitions." : null;
            if (relic.UnitUpgradeTriggers == null || !indices.SequenceEqual(relic.UnitUpgradeTriggers.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic unit-upgrade trigger definitions.";
            foreach (RelicUnitUpgradeTrigger rule in relic.UnitUpgradeTriggers)
            {
                if (string.IsNullOrEmpty(rule.Trigger) || !rule.SubtypeIsNone && string.IsNullOrEmpty(rule.RequiredSubtype))
                    return "Malformed native relic unit-upgrade trigger definition.";
                if (rule.UpgradeLifetime != UntilEndOfBattle && rule.UpgradeLifetime != UntilUnitDeath)
                    return "Unmodeled relic unit-upgrade lifetime.";
                if (rule.Upgrade != null && (string.IsNullOrEmpty(rule.Upgrade.DataId) || rule.Upgrade.ExternalInteractions.Count > 0))
                    return "Unmodeled relic unit-upgrade payload.";
            }
            return null;
        }
    }
}
