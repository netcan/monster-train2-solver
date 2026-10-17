using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalChanceToCleanseScenario
    {
        internal const string Name = "original-chance-to-cleanse";
        internal const string DataId = "1e972d81-fc2f-4d4a-ade9-46edeb246dbd";
        internal const string AssetKey = "ChanceToCleanse";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static DefinitionEvidence? Definition { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("ChanceToCleanse must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("ChanceToCleanse is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-CHANCE-TO-CLEANSE-ACQUIRED unchanged owner; 50% AfterSpawnEnchant removes visible Positive statuses from Heroes.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("ChanceToCleanse must be prepared on deployment turn.");
            CollectableRelicData relic = managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            RelicState[] owners = managers.GetSaveManager().GetCollectedRelics()
                .Where(item => item.GetRelicDataID() == DataId).ToArray();
            if (owners.Length != 1)
                throw new InvalidOperationException("ChanceToCleanse is missing or duplicated at battle setup.");
            Definition!.CanApplyInPreviewMode = owners[0].GetEffects().Single().CanApplyInPreviewMode;
            if (Definition.CanApplyInPreviewMode)
                throw new InvalidOperationException("ChanceToCleanse unexpectedly became preview-enabled.");
            Prepared = true;
            log.LogInfo("ORIGINAL-CHANCE-TO-CLEANSE-PREPARED unchanged 50% owner; natural enemy Heroes provide eligible AfterSpawnEnchant tests.");
        }

        private static DefinitionEvidence CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            SubtypeData? subtype = effect.GetParamCharacterSubtype();
            SubtypeData[] excluded = effect.GetParamExcludeCharacterSubtypes();
            CardEffectData[] cardEffects = effect.GetParamCardEffectData().ToArray();
            if (relic.name != AssetKey || relic.GetID() != DataId || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicAfterSpawnChanceStatusModel.Effect || effect.GetParamInt() != 50 ||
                effect.GetParamInt2() != 0 || effect.GetParamSourceTeam() != Team.Type.Heroes || subtype == null ||
                !subtype.IsNone || excluded.Length != 0 || effect.GetEffectConditions().Count != 0 || cardEffects.Length != 1)
                throw new InvalidOperationException("Original ChanceToCleanse owner metadata changed.");
            CardEffectData cleanse = cardEffects[0];
            if (cleanse.GetEffectStateName() != "CardEffectRemoveAllStatusEffects" ||
                cleanse.GetTargetMode() != TargetMode.Self || cleanse.GetTargetTeamType() != Team.Type.Heroes ||
                cleanse.GetParamInt() != 0 || cleanse.GetParamBool() || cleanse.GetParamBool2() ||
                cleanse.GetUseIntRange() || cleanse.GetParamStatusEffects().Length != 0)
                throw new InvalidOperationException("Original ChanceToCleanse nested effect payload changed.");
            return new DefinitionEvidence
            {
                OwnerId = relic.GetID(), AssetKey = relic.name, Effect = effect.GetEffectClassName(),
                ChancePercent = effect.GetParamInt(), RoomRestriction = effect.GetParamInt2(),
                TargetTeam = effect.GetParamSourceTeam().ToString(), RequiredSubtype = subtype.Key,
                ExcludedSubtypes = excluded.Select(item => item.Key).ToArray(), ConditionCount = effect.GetEffectConditions().Count,
                CardEffect = cleanse.GetEffectStateName(), CardEffectTarget = cleanse.GetTargetMode().ToString(),
                CardEffectTeam = cleanse.GetTargetTeamType().ToString(), CardEffectCategory = cleanse.GetParamInt(),
                RemoveHalf = cleanse.GetParamBool()
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
            public int CardEffectCategory { get; set; }
            public bool RemoveHalf { get; set; }
        }
    }
}
