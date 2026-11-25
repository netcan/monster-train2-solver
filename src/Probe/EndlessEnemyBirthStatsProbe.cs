using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class EndlessEnemyBirthStatsProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static bool IsPinnedRing2(SaveManager save) => save.IsEndlessRun &&
            typeof(CharacterState).Module.ModuleVersionId == Module && save.GetRunLength() == 8 &&
            save.GetRunState().GetLoopIndex() == 1 && save.GetRunState().GetCurrentDistance() == 1 &&
            save.GetCurrentScenarioData()?.name == "TestEndless_Level2Battle1" &&
            save.GetCovenants().Count == 0 && save.GetMutators().Count == 0;

        internal static Func<CharacterData, CombatUnit, EndlessEnemyBirthStats?>? Capture(AllGameManagers managers, bool outerBoss = false)
        {
            SaveManager save = managers.GetSaveManager();
            if (!save.IsEndlessRun || typeof(CharacterState).Module.ModuleVersionId != Module ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                return null;
            var run = save.GetRunState();
            int loop = run.GetLoopIndex();
            int distance = run.GetCurrentDistance();
            int runLength = save.GetRunLength();
            bool pinnedRing2 = IsPinnedRing2(save);
            if (loop is < 1 or > 3 || runLength != 8 || distance < 0 || distance >= runLength)
                return null;
            if (outerBoss && (loop != 1 || distance != 2 || save.GetCurrentScenarioData()?.name != "TestEndless_Level3Battle1"))
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
            var statusScaling = scalable.Length == 3 ? new RelicSpawnStatusScalingRule(status,
                Array.Empty<string>(), scaleStarting: true, scaleApplied: true, onlyListed: true, scalable, scalable) : null;
            string[] corruption = { "corruption" };
            var corruptionScaling = managers.GetStatusEffectManager().GetStatusEffectDataById("corruption")?.ScalesInEndless() == true
                ? new RelicSpawnStatusScalingRule(status, Array.Empty<string>(), scaleStarting: true, scaleApplied: true,
                    onlyListed: true, corruption, corruption)
                : null;

            return (data, raw) =>
            {
                if (outerBoss)
                {
                    if (!data.IsOuterTrainBoss() || !OuterBossInstantiationModel.IsOriginalArkionPushBirth(raw) ||
                        data.GetStartingStatusEffects().Length != 0 || data.GetTriggers() is not [var trigger] ||
                        trigger.GetEffects() is not [var action] || action.GetParamStatusEffectStackData() is not [{ statusId: "dazed", count: 2 }] ||
                        managers.GetStatusEffectManager().GetStatusEffectDataById("dazed")?.ScalesInEndless() != false)
                        return null;
                    var bossBaseline = data.GetEndlessBaselineStats();
                    if (!bossBaseline.HasAttack() || !bossBaseline.HasHealth()) return null;
                    return new EndlessEnemyBirthStats(raw.BaseAttack, raw.Health, Mathf.CeilToInt(bossBaseline.GetAttack() * attack),
                        Mathf.CeilToInt(bossBaseline.GetHealth() * health), Array.Empty<int>());
                }
                var baseline = data.GetEndlessBaselineStats();
                bool statusShape = EndlessEnemyBirthStats.IsStatusShape(raw);
                bool unscaledSilence = EndlessEnemyBirthStats.IsUnscaledSilenceAura(raw);
                RelicSpawnStatusScalingRule? capturedStatuses = raw.AssetKey == EndlessEnemyBirthStats.CorruptionHorde
                    ? corruptionScaling : statusScaling;
                if (!baseline.HasHealth() || data.IsOuterTrainBoss() || !EndlessEnemyBirthStats.SupportedShape(raw) ||
                    statusShape && capturedStatuses == null ||
                    unscaledSilence && (!pinnedRing2 || managers.GetStatusEffectManager()
                        .GetStatusEffectDataById("silenced")?.ScalesInEndless() != false) ||
                    !statusShape && data.GetStartingStatusEffects().Any(status =>
                        managers.GetStatusEffectManager().GetStatusEffectDataById(status.statusId)?.ScalesInEndless() != false))
                    return null;
                int baselineAttack = baseline.HasAttack() ? baseline.GetAttack() : raw.BaseAttack;
                int[] values = statusShape || unscaledSilence ? Array.Empty<int>() : raw.Triggers.SelectMany(trigger => trigger.Effects).Select(entry => Mathf.CeilToInt(entry.Value *
                    (entry.Type == "CardEffectDamage" ? damage : entry.Type == "CardEffectHeal" ? heal : buff))).ToArray();
                return new EndlessEnemyBirthStats(raw.BaseAttack, raw.Health,
                    Mathf.CeilToInt(baselineAttack * attack), Mathf.CeilToInt(baseline.GetHealth() * health), values,
                    statusShape ? capturedStatuses : null);
            };
        }
    }
}
