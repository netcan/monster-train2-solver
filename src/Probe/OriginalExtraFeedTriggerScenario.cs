using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalExtraFeedTriggerScenario
    {
        internal const string Name = "original-extra-feed-trigger";
        internal const string DataId = "b6613fe4-13c6-4462-8a0b-9af42850a851";
        internal const string AssetKey = "ExtraFeedTrigger";
        private const string Effect = "RelicEffectModifyTriggerCount";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static DefinitionEvidence? Evidence { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("ExtraFeedTrigger must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Evidence = CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("ExtraFeedTrigger is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-EXTRA-FEED-TRIGGER-ACQUIRED unchanged owner; Monsters OnFeed count +1.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("ExtraFeedTrigger must be prepared on deployment turn.");
            SaveManager save = managers.GetSaveManager();
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            Evidence = CheckOriginal(relic);
            RelicState[] owners = save.GetCollectedRelics().Where(item => item.GetRelicDataID() == DataId).ToArray();
            if (owners.Length != 1 || managers.GetRelicManager()!.GetTriggerFireCountModifier(CharacterTriggerData.Trigger.OnFeed) != 1)
                throw new InvalidOperationException("ExtraFeedTrigger registration did not add exactly one OnFeed count.");
            CharacterState pyre = managers.GetRoomManager()!.GetPyreRoom().GetPyreHeart();
            if (pyre == null || pyre.IsDestroyed || pyre.GetTeamType() != Team.Type.Monsters)
                throw new InvalidOperationException("ExtraFeedTrigger needs the live Monsters Pyre for native count queries.");
            Evidence.PlayerUnitId = pyre.GetInstanceID();
            Evidence.PlayerOnFeedCount = pyre.GetTriggerFireCount(CharacterTriggerData.Trigger.OnFeed);
            Evidence.PlayerOnDeathCount = pyre.GetTriggerFireCount(CharacterTriggerData.Trigger.OnDeath);
            if (Evidence.PlayerOnFeedCount != 2)
                throw new InvalidOperationException("Native Monsters OnFeed query did not include exactly one relic count.");
            Prepared = true;
            log.LogInfo($"ORIGINAL-EXTRA-FEED-TRIGGER-PREPARED owner unchanged; native Monsters OnFeed={Evidence.PlayerOnFeedCount}, OnDeath={Evidence.PlayerOnDeathCount}.");
        }

        private static DefinitionEvidence CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            SubtypeData subtype = effect.GetParamCharacterSubtype();
            if (relic.name != AssetKey || relic.GetID() != DataId || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != Effect || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                !subtype.IsNone || effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnFeed ||
                effect.GetParamInt() != 1 || effect.GetParamInt2() != 0 || effect.GetParamInt3() != 0 ||
                effect.GetParamBool() || effect.GetParamBool2() || effect.GetParamBool3() ||
                effect.GetParamMinInt() != 0 || effect.GetParamMaxInt() != 0 ||
                effect.GetCardTriggers().Count != 0 || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamCharacters().Count != 0)
                throw new InvalidOperationException("Original ExtraFeedTrigger owner metadata changed.");
            return new DefinitionEvidence
            {
                OwnerId = relic.GetID(), AssetKey = relic.name, Effect = effect.GetEffectClassName(),
                SourceTeam = effect.GetParamSourceTeam().ToString(), RequiredSubtype = subtype.Key,
                Trigger = effect.GetParamTrigger().ToString(), CountModifier = effect.GetParamInt(),
                AllTriggers = effect.GetParamBool(), AppliesToHeroes = effect.GetParamBool2(),
                ExcludedCardTriggers = effect.GetCardTriggers().Select(item => item.ToString()).ToArray(),
                ConditionCount = effect.GetEffectConditions().Count
            };
        }

        internal sealed class DefinitionEvidence
        {
            public string OwnerId { get; set; } = string.Empty;
            public string AssetKey { get; set; } = string.Empty;
            public string Effect { get; set; } = string.Empty;
            public string SourceTeam { get; set; } = string.Empty;
            public string RequiredSubtype { get; set; } = string.Empty;
            public string Trigger { get; set; } = string.Empty;
            public int CountModifier { get; set; }
            public bool AllTriggers { get; set; }
            public bool AppliesToHeroes { get; set; }
            public string[] ExcludedCardTriggers { get; set; } = Array.Empty<string>();
            public int ConditionCount { get; set; }
            public int PlayerUnitId { get; set; }
            public int PlayerOnFeedCount { get; set; }
            public int PlayerOnDeathCount { get; set; }
        }
    }
}
