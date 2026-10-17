using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSummonWithAvariceScenario
    {
        internal const string Name = "original-summon-with-avarice";
        internal const string DataId = "a9670728-2ad2-4776-be80-e1a08864f18c";
        internal const string AssetKey = "SummonWithAvarice";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static DefinitionEvidence? Definition { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("SummonWithAvarice must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("SummonWithAvarice is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-SUMMON-AVARICE-ACQUIRED unchanged owner; newly summoned Monsters receive two Avarice stacks.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("SummonWithAvarice must be prepared on deployment turn.");
            CollectableRelicData relic = managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Definition = CheckOriginal(relic);
            RelicState[] owners = managers.GetSaveManager().GetCollectedRelics()
                .Where(item => item.GetRelicDataID() == DataId).ToArray();
            if (owners.Length != 1)
                throw new InvalidOperationException("SummonWithAvarice is missing or duplicated at battle setup.");
            Prepared = true;
            log.LogInfo("ORIGINAL-SUMMON-AVARICE-PREPARED unchanged Monster-only owner; natural player-unit spawns and combat provide status and gold witnesses.");
        }

        private static DefinitionEvidence CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            SubtypeData? subtype = effect.GetParamCharacterSubtype();
            SubtypeData[] excluded = effect.GetParamExcludeCharacterSubtypes();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (relic.name != AssetKey || relic.GetID() != DataId || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn" ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || subtype == null || !subtype.IsNone ||
                excluded.Length != 0 || effect.GetParamBool() || effect.GetParamInt() != 0 || effect.GetParamInt2() != 0 ||
                effect.GetParamString() != string.Empty || effect.GetParamBool2() || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamCharacters().Count != 0 || statuses.Length != 1 ||
                statuses[0].statusId != "avarice" || statuses[0].count != 2)
                throw new InvalidOperationException("Original SummonWithAvarice owner metadata changed.");
            return new DefinitionEvidence
            {
                OwnerId = relic.GetID(), AssetKey = relic.name, Effect = effect.GetEffectClassName(),
                TargetTeam = effect.GetParamSourceTeam().ToString(), RequiredSubtype = subtype.Key,
                ExcludedSubtypeCount = excluded.Length, RestrictToRoom = effect.GetParamBool(),
                RestrictedRoom = effect.GetParamInt(), HpPercentAsStacks = effect.GetParamInt2(),
                SourceKind = effect.GetParamString(), RequireGraft = effect.GetParamBool2(),
                ConditionCount = effect.GetEffectConditions().Count, CharacterFilterCount = effect.GetParamCharacters().Count,
                StatusId = statuses[0].statusId, StatusStacks = statuses[0].count
            };
        }

        internal sealed class DefinitionEvidence
        {
            public string OwnerId { get; set; } = string.Empty;
            public string AssetKey { get; set; } = string.Empty;
            public string Effect { get; set; } = string.Empty;
            public string TargetTeam { get; set; } = string.Empty;
            public string RequiredSubtype { get; set; } = string.Empty;
            public int ExcludedSubtypeCount { get; set; }
            public bool RestrictToRoom { get; set; }
            public int RestrictedRoom { get; set; }
            public int HpPercentAsStacks { get; set; }
            public string SourceKind { get; set; } = string.Empty;
            public bool RequireGraft { get; set; }
            public int ConditionCount { get; set; }
            public int CharacterFilterCount { get; set; }
            public string StatusId { get; set; } = string.Empty;
            public int StatusStacks { get; set; }
        }
    }
}
