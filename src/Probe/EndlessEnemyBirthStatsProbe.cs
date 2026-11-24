using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class EndlessEnemyBirthStatsProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static Func<CharacterData, CombatUnit, EndlessEnemyBirthStats?>? Capture(AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            int distance = save.GetCurrentDistance();
            if (!save.IsEndlessRun || typeof(CharacterState).Module.ModuleVersionId != Module ||
                save.GetRunState().GetLoopIndex() != 1 || distance is not (0 or 1) ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                return null;

            var relics = RelicProbe.Current(managers);
            if (relics.Count(owner => owner.GetRelicDataID() == RelicModel.EndlessEnemyBirthOwnerId) != 1 ||
                relics.SelectMany(owner => owner.GetEffects()).Any(effect =>
                    effect is IAddAttackRelicEffect or IMultiplyAttackLateRelicEffect))
                return null;
            var owner = save.GetEndlessScalingSins();
            if (owner == null || owner.GetID() != RelicModel.EndlessEnemyBirthOwnerId ||
                owner.GetEffects() is not [var effect] || effect.GetEffectClassName() != RelicModel.EndlessEnemyBirthEffect ||
                effect.GetParamExcludeCharacterSubtypes().Any())
                return null;

            var balance = save.GetBalanceData().GetEndlessScalingData();
            int runLength = save.GetRunLength();
            float attack = balance.GetAttackScaling(1, distance, runLength);
            float health = balance.GetHealthScaling(1, distance, runLength);
            float damage = balance.GetCardEffectDamageScaling(1, distance, runLength);
            float heal = balance.GetCardEffectHealScaling(1, distance, runLength);
            float buff = balance.GetCardEffectBuffDamageScaling(1, distance, runLength);
            string[] scalable = new[] { "armor", "titanskin", "buff" }.Where(id =>
                managers.GetStatusEffectManager().GetStatusEffectDataById(id)?.ScalesInEndless() == true).ToArray();
            var statusScaling = new RelicSpawnStatusScalingRule(balance.GetStatusScaling(1, distance, runLength),
                Array.Empty<string>(), scaleStarting: true, scaleApplied: true, onlyListed: true, scalable, scalable);

            return (data, raw) =>
            {
                var baseline = data.GetEndlessBaselineStats();
                bool statusShape = EndlessEnemyBirthStats.IsStatusShape(raw);
                if (!baseline.HasHealth() || data.IsOuterTrainBoss() || !EndlessEnemyBirthStats.SupportedShape(raw) ||
                    statusShape && (distance != 1 || scalable.Length != 3) ||
                    !statusShape && data.GetStartingStatusEffects().Any(status =>
                        managers.GetStatusEffectManager().GetStatusEffectDataById(status.statusId)?.ScalesInEndless() != false))
                    return null;
                int baselineAttack = baseline.HasAttack() ? baseline.GetAttack() : raw.BaseAttack;
                int[] values = statusShape ? Array.Empty<int>() : raw.Triggers.SelectMany(trigger => trigger.Effects).Select(entry => Mathf.CeilToInt(entry.Value *
                    (entry.Type == "CardEffectDamage" ? damage : entry.Type == "CardEffectHeal" ? heal : buff))).ToArray();
                return new EndlessEnemyBirthStats(raw.BaseAttack, raw.Health,
                    Mathf.CeilToInt(baselineAttack * attack), Mathf.CeilToInt(baseline.GetHealth() * health), values,
                    statusShape ? statusScaling : null);
            };
        }
    }
}
