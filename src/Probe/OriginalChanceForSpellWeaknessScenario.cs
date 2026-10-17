using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalChanceForSpellWeaknessScenario
    {
        internal const string Name = "original-chance-for-spell-weakness";
        internal const string DataId = "98989cbb-c729-4f87-8240-08f448db96d8";
        internal const string AssetKey = "ChanceForSpellWeakness";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static DefinitionEvidence? Definition { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("ChanceForSpellWeakness must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("ChanceForSpellWeakness is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-CHANCE-SPELL-WEAKNESS-ACQUIRED unchanged owner; 50% AfterSpawnEnchant on Heroes adds one Spell Weakness to self.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("ChanceForSpellWeakness must be prepared on deployment turn.");
            CollectableRelicData relic = managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            RelicState[] owners = managers.GetSaveManager().GetCollectedRelics()
                .Where(item => item.GetRelicDataID() == DataId).ToArray();
            if (owners.Length != 1)
                throw new InvalidOperationException("ChanceForSpellWeakness is missing or duplicated at battle setup.");
            Definition!.CanApplyInPreviewMode = owners[0].GetEffects().Single().CanApplyInPreviewMode;
            if (Definition.CanApplyInPreviewMode)
                throw new InvalidOperationException("The 50% after-spawn effect unexpectedly became preview-enabled.");
            Prepared = true;
            log.LogInfo("ORIGINAL-CHANCE-SPELL-WEAKNESS-PREPARED unchanged 50% owner; natural enemy Heroes provide eligible AfterSpawnEnchant rolls.");
        }

        private static DefinitionEvidence CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            SubtypeData? subtype = effect.GetParamCharacterSubtype();
            SubtypeData[] excluded = effect.GetParamExcludeCharacterSubtypes();
            CardEffectData[] cardEffects = effect.GetParamCardEffectData().ToArray();
            if (relic.name != AssetKey || relic.GetID() != DataId || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicAfterSpawnChanceStatusModel.Effect || effect.GetParamInt() != 50 ||
                effect.GetParamInt2() != 0 || effect.GetParamSourceTeam() != Team.Type.Heroes ||
                subtype == null || !subtype.IsNone || excluded.Length != 0 || effect.GetEffectConditions().Count != 0 ||
                cardEffects.Length != 1)
                throw new InvalidOperationException("Original ChanceForSpellWeakness owner metadata changed.");
            CardEffectData statusEffect = cardEffects[0];
            StatusEffectStackData[] statuses = statusEffect.GetParamStatusEffects();
            if (statusEffect.GetEffectStateName() != "CardEffectAddStatusEffect" ||
                statusEffect.GetTargetMode() != TargetMode.Self || statusEffect.GetTargetTeamType() != Team.Type.Heroes ||
                statusEffect.GetParamInt() != 0 || statusEffect.GetUseIntRange() || statusEffect.GetParamBool() ||
                statuses.Length != 1 || statuses[0].statusId != "spell weakness" || statuses[0].count != 1)
                throw new InvalidOperationException("Original ChanceForSpellWeakness card-effect payload changed.");
            return new DefinitionEvidence
            {
                OwnerId = relic.GetID(), AssetKey = relic.name, Effect = effect.GetEffectClassName(),
                ChancePercent = effect.GetParamInt(), RoomRestriction = effect.GetParamInt2(),
                TargetTeam = effect.GetParamSourceTeam().ToString(), RequiredSubtype = subtype.Key,
                ExcludedSubtypes = excluded.Select(item => item.Key).ToArray(), ConditionCount = effect.GetEffectConditions().Count,
                CardEffect = statusEffect.GetEffectStateName(), CardEffectTarget = statusEffect.GetTargetMode().ToString(),
                CardEffectTeam = statusEffect.GetTargetTeamType().ToString(), CardEffectChance = statusEffect.GetParamInt(),
                CardEffectUsesRange = statusEffect.GetUseIntRange(), StatusId = statuses[0].statusId, StatusStacks = statuses[0].count
            };
        }

        internal sealed class DefinitionEvidence
        {
            public string OwnerId { get; set; } = string.Empty;
            public string AssetKey { get; set; } = string.Empty;
            public string Effect { get; set; } = string.Empty;
            public int ChancePercent { get; set; }
            public int RoomRestriction { get; set; }
            public string TargetTeam { get; set; } = string.Empty;
            public string RequiredSubtype { get; set; } = string.Empty;
            public string[] ExcludedSubtypes { get; set; } = Array.Empty<string>();
            public int ConditionCount { get; set; }
            public bool CanApplyInPreviewMode { get; set; }
            public string CardEffect { get; set; } = string.Empty;
            public string CardEffectTarget { get; set; } = string.Empty;
            public string CardEffectTeam { get; set; } = string.Empty;
            public int CardEffectChance { get; set; }
            public bool CardEffectUsesRange { get; set; }
            public string StatusId { get; set; } = string.Empty;
            public int StatusStacks { get; set; }
        }
    }
}
