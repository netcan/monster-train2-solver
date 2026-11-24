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
            if (!save.IsEndlessRun || typeof(CharacterState).Module.ModuleVersionId != Module ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                return null;
            var run = save.GetRunState();
            int loop = run.GetLoopIndex();
            int distance = run.GetCurrentDistance();
            int runLength = save.GetRunLength();
            if (loop is < 1 or > 3 || runLength != 8 || distance < 0 || distance >= runLength)
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
            float attack = balance.GetAttackScaling(loop, distance, runLength);
            float health = balance.GetHealthScaling(loop, distance, runLength);
            float damage = balance.GetCardEffectDamageScaling(loop, distance, runLength);
            float heal = balance.GetCardEffectHealScaling(loop, distance, runLength);
            float buff = balance.GetCardEffectBuffDamageScaling(loop, distance, runLength);
            float status = balance.GetStatusScaling(loop, distance, runLength);
            if (new[] { attack, health, damage, heal, buff, status }.Any(factor =>
                factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor)))
                return null;
            string[] scalable = new[] { "armor", "titanskin", "buff" }.Where(id =>
                managers.GetStatusEffectManager().GetStatusEffectDataById(id)?.ScalesInEndless() == true).ToArray();
            var statusScaling = new RelicSpawnStatusScalingRule(status,
                Array.Empty<string>(), scaleStarting: true, scaleApplied: true, onlyListed: true, scalable, scalable);

            return (data, raw) =>
            {
                var baseline = data.GetEndlessBaselineStats();
                bool statusShape = EndlessEnemyBirthStats.IsStatusShape(raw);
                if (!baseline.HasHealth() || data.IsOuterTrainBoss() || !EndlessEnemyBirthStats.SupportedShape(raw) ||
                    statusShape && scalable.Length != 3 ||
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
