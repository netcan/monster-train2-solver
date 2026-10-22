using System;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Explicit phase control in an isolated save; no claim of a natural transition.
    internal static class TrueFinalBossRelentlessScenario
    {
        internal sealed class Record
        {
            public OuterBossPhaseState Before { get; set; } = null!;
            public OuterBossPhaseState Actual { get; set; } = null!;
            public int NativeWaveCount { get; set; }
        }
        internal static Record? Evidence { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_RELENTLESS") == "1";
        internal static bool Prepare()
        {
            if (!Enabled || Evidence != null) return false;
            AllGameManagers managers = AllGameManagers.Instance;
            if (managers.GetCombatManager().GetTurnCount() != 1) return false;
            CharacterState boss = managers.GetHeroManager().GetOuterTrainBossCharacter();
            if (!TrueFinalBossTurnProbe.Enabled || !OuterBossPhaseProbe.Enabled || managers.GetSaveManager().PreviewMode ||
                boss?.GetSourceCharacterData().GetAssetKey() != "Boss_Level9_Entropy_Top_V2" ||
                boss.GetBossState().GetCurrentAttackPhase() != BossState.AttackPhase.Casting)
                throw new InvalidOperationException("Hellforged phase control requires the isolated born original top body.");
            var row = new Record { Before = OuterBossPhaseProbe.Capture() };
            HeroManager heroes = managers.GetHeroManager();
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            pattern = (SpawnPatternData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(pattern, null);
            AccessTools.Field(typeof(SpawnPatternData), "isLoopingScenario").SetValue(pattern, false);
            AccessTools.Field(typeof(HeroManager), "spawnPattern").SetValue(heroes, pattern);
            var waves = new List<SpawnGroupPoolData>();
            pattern.GetUnlockedWaves(managers.GetSaveManager(), waves);
            row.NativeWaveCount = waves.Count;
            AccessTools.Field(typeof(HeroManager), "spawnPatternPhase").SetValue(heroes, waves.Count);
            boss.GetBossState().AdvanceToAttackPhase(BossState.AttackPhase.Relentless);
            row.Actual = OuterBossPhaseProbe.Capture();
            Evidence = row;
            return true;
        }
    }
}
