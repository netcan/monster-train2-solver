using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using ShinyShoe.Loading;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignDraftCalibration
    {
        private static readonly string[] Ids = { "656861c9-a96d-4c99-988c-390b75e5d8d4", "8f46fde9-0c75-49ba-9f58-3063864f81bf", "fe768ab5-3f58-4f96-9afc-48f0f30c7b21" };
        private static bool selecting;
        private static string[]? selected;
        [HarmonyPatch(typeof(LoadingScreen), nameof(LoadingScreen.AddTask), new[] { typeof(LoadingTask), typeof(bool) })]
        private static class DraftOutputPatch
        {
            private static bool Prefix(LoadingTask task)
            {
                if (!selecting) return true;
                if (!(task is LoadAdditionalCards) || selected != null) throw new InvalidOperationException("Unexpected campaign draft output task.");
                selected = ((List<CardData>)AccessTools.Field(typeof(LoadAdditionalCards), "_additionalCards").GetValue(task)).Select(card => card.GetID()).ToArray();
                // Selection has completed in the unchanged GrantReward consumer.
                // Do not schedule the subsequent asset/UI/choice/grant workflow.
                return false;
            }
        }
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_DRAFTS") != "1") return;
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var relics = managers.GetRelicManager();
            var owners = Ids.Select(id => data.GetAllCollectableRelicData().Single(owner => owner.GetID() == id)).ToArray();
            if (save.PreviewMode || save.GetCollectedRelics().Any(owner => Ids.Contains(owner.GetRelicDataID())) ||
                relics.TryGetOverrideDraftClassTypeRelic(out _) || relics.TryGetDraftFloorOverride(out _))
                throw new InvalidOperationException("Campaign draft calibration requires isolated original owners.");
            var active = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            var rewardsRng = (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { RngId.Rewards });
            var originalRng = new HadesRNG(rewardsRng);
            int originalRawHealth = active.GetTowerHP();
            JObject Snapshot() => JObject.FromObject(new { Relics = save.GetCollectedRelics().Select(owner => owner.GetRelicDataID()).ToArray(),
                Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(), RawHealth = active.GetTowerHP(),
                Health = save.GetTowerHP(), Maximum = save.GetMaxTowerHP(), Frame = UnityEngine.Time.frameCount,
                Rng = Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                    .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray() });
            JObject before = Snapshot(); var drafts = new List<object>(); var health = new List<object>();
            void Remove(CollectableRelicData owner) => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == owner.GetID()));
            var pool = UnityEngine.ScriptableObject.CreateInstance<CardPool>();
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            var add = AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) });
            var descriptions = new List<CampaignDraftCard>();
            foreach (int group in new[] { 1, 2, 4 })
            foreach (int rarity in new[] { 0, 1, 2 })
            {
                var linked = group == 1 ? save.GetMainClass() : group == 2 ? save.GetSubClass() : null;
                int level = group == 1 ? save.GetMainClassLevelInRun() : group == 2 ? save.GetSubClassLevelInRun() : int.MaxValue;
                var choices = data.GetAllCardData().Where(card => !card.IsArchived && card.GetLinkedClass() == linked &&
                    (int)card.GetRarity() == rarity && card.GetUnlockLevel() <= level && save.IsDlcEnabledForCurrentRun(card.GetRequiredDLC()))
                    .OrderBy(card => card.GetID(), StringComparer.Ordinal).Take(3).ToArray();
                if (group != 4 && choices.Length < 2) throw new InvalidOperationException("Campaign draft pool lacks unlocked rarity controls.");
                foreach (var card in choices) { add.Invoke(list, new object[] { card }); descriptions.Add(new(card.GetID(), group, rarity)); }
            }
            var reward = UnityEngine.ScriptableObject.CreateInstance<DraftRewardData>();
            Set(reward, "draftPool", pool); Set(reward, "useRunRarityFloors", false); Set(reward, "flattenRarityForDraftRate", true);
            Set(reward, "rarityCeilingOverride", CollectableRarity.Champion); Set(reward, "draftOptionsCount", 3u);
            void Drafts(string phase)
            {
                foreach (int classes in Enumerable.Range(1, 7))
                foreach (int floor in new[] { 0, 1, 2 })
                foreach (bool ignore in new[] { false, true })
                foreach (bool explicitClass in new[] { false, true })
                foreach (int seed in new[] { 41, 20261015 })
                {
                    Set(reward, "classType", (RunState.ClassType)classes); Set(reward, "rarityFloorOverride", (CollectableRarity)floor);
                    Set(reward, "ignoreRelicRarityOverride", ignore); Set(reward, "classTypeOverride", explicitClass);
                    Set(reward, "classDataOverride", save.GetMainClass()); rewardsRng.Init(seed);
                    uint[] rngBefore = RngCalibration.Words(RandomManager.GetState(RngId.Rewards)); selected = null; selecting = true;
                    try { reward.GrantReward(new GrantableRewardData.GrantParams { coreGameManagers = managers.GetGameRunManagers(), source = GrantableRewardData.Source.Battle }); }
                    finally { selecting = false; }
                    if (selected == null || selected.Length == 0) throw new InvalidOperationException("Native draft selection did not produce candidates.");
                    drafts.Add(new { Phase = phase, Classes = classes, Floor = floor, IgnoreRarityOverride = ignore,
                        ExplicitClass = explicitClass, Seed = seed, BeforeRng = rngBefore,
                        AfterRng = RngCalibration.Words(RandomManager.GetState(RngId.Rewards)), Actual = selected });
                }
            }
            try
            {
                Drafts("baseline"); save.AddRelic(owners[0]); Drafts("allied"); save.AddRelic(owners[1]); Drafts("both");
                Remove(owners[0]); Drafts("rare"); Remove(owners[1]); Drafts("removed");
                foreach (int hp in new[] { 1, Math.Max(1, save.GetMaxTowerHP() - 7), save.GetMaxTowerHP() })
                {
                    active.SetTowerHP(hp); int maximum = save.GetMaxTowerHP();
                    save.AddRelic(owners[2]); int acquiredRaw = active.GetTowerHP(), acquiredMaximum = save.GetMaxTowerHP();
                    Remove(owners[2]);
                    health.Add(new { BeforeRaw = hp, BeforeMaximum = maximum, AcquiredRaw = acquiredRaw, AcquiredMaximum = acquiredMaximum,
                        RemovedRaw = active.GetTowerHP(), RemovedHealth = save.GetTowerHP(), RemovedMaximum = save.GetMaxTowerHP() });
                }
            }
            finally
            {
                selecting = false;
                foreach (var owner in owners) if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == owner.GetID())) Remove(owner);
                rewardsRng.Init(originalRng); active.SetTowerHP(originalRawHealth); save.RefreshMaxHP();
            }
            JObject after = Snapshot();
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Campaign draft recorded state/frame/RNG was not restored.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after,
                Owners = owners.Select(owner => new { DataId = owner.GetID(), AssetKey = owner.name, DisallowedInPlacementPhase = owner.DisallowInDeploymentPhase,
                    Effects = owner.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                        Rarity = (int)effect.GetParamCardRarityType(), Conditions = effect.GetEffectConditions().Count }).ToArray() }).ToArray(),
                Pool = descriptions, Drafts = drafts, Health = health }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-draft-calibration.mt2f"))) document.Write(stream);
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
