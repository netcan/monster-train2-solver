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
                save.GetRunState().GetLoopIndex() != 1 || save.GetCurrentDistance() != 0 ||
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
            float attack = balance.GetAttackScaling(1, 0, runLength);
            float health = balance.GetHealthScaling(1, 0, runLength);
            float damage = balance.GetCardEffectDamageScaling(1, 0, runLength);
            float heal = balance.GetCardEffectHealScaling(1, 0, runLength);
            float buff = balance.GetCardEffectBuffDamageScaling(1, 0, runLength);

            return (data, raw) =>
            {
                var baseline = data.GetEndlessBaselineStats();
                if (raw.IsBoss != false || raw.Statuses.Count != 0 || !baseline.HasHealth() ||
                    !EndlessEnemyBirthStats.SupportedTrigger(raw))
                    return null;
                int baselineAttack = baseline.HasAttack() ? baseline.GetAttack() : raw.BaseAttack;
                int[] values = raw.Triggers[0].Effects.Select(entry => Mathf.CeilToInt(entry.Value *
                    (entry.Type == "CardEffectDamage" ? damage : entry.Type == "CardEffectHeal" ? heal : buff))).ToArray();
                return new EndlessEnemyBirthStats(raw.BaseAttack, raw.Health,
                    Mathf.CeilToInt(baselineAttack * attack), Mathf.CeilToInt(baseline.GetHealth() * health), values);
            };
        }
    }
}
