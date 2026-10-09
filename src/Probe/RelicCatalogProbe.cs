using System;
using System.Collections;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    // Definitions only: no relic is acquired, initialized or fired by this capture.
    internal static class RelicCatalogProbe
    {
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CATALOG") != "1") return;
            JObject before = RngStates();
            int frame = UnityEngine.Time.frameCount;
            AllGameData gameData = managers.GetSaveManager().GetAllGameData();
            string[] collections = { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas",
                "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" };
            var relics = collections.SelectMany(collection => ((IEnumerable)Field(gameData, collection)).Cast<RelicData>()
                .Select(data => new { Collection = collection, Data = data })).Select(entry => new
            {
                entry.Collection, Id = entry.Data.GetID(), AssetKey = entry.Data.name, DisallowedInPlacementPhase = entry.Data.DisallowInDeploymentPhase,
                Effects = entry.Data.GetEffects().Select(effect => new
                {
                    ClassName = effect.GetEffectClassName(), RuntimeType = TypeNameCache.GetType(effect.GetEffectClassName())?.Name,
                    SourceTeam = effect.GetParamSourceTeam().ToString(), Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                    Bool = effect.GetParamBool(), Bool2 = effect.GetParamBool2(), Str = effect.GetParamString(),
                    Trigger = effect.GetParamTrigger().ToString(), ExcludedCardTriggers = effect.GetCardTriggers().Select(trigger => trigger.ToString()).ToArray(),
                    Statuses = (effect.GetParamStatusEffects() ?? Array.Empty<StatusEffectStackData>()).Select(status => new { status.statusId, status.count }).ToArray(),
                    Conditions = (effect.GetEffectConditions() ?? new System.Collections.Generic.List<RelicEffectCondition>()).Select(condition => new
                    {
                        TrackedValue = Field(condition, "paramTrackedValue").ToString(), CardType = Field(condition, "paramCardType").ToString(),
                        TrackTriggerCount = (bool)Field(condition, "paramTrackTriggerCount"), Duration = Field(condition, "paramEntryDuration").ToString(),
                        Comparator = Convert.ToInt32(Field(condition, "paramComparator")), Int = (int)Field(condition, "paramInt"),
                        AllowMultiple = (bool)Field(condition, "allowMultipleTriggersPerDuration"), Subtype = Field(condition, "paramSubtype")
                    }).ToArray()
                }).ToArray()
            }).ToArray();
            JObject after = RngStates();
            if (UnityEngine.Time.frameCount != frame || !JToken.DeepEquals(before, after))
                throw new InvalidOperationException("Relic definition inspection changed native RNG or frame.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 3, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = before, RngAfter = after, Relics = relics
            }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "relic-catalog.mt2f")))
                document.Write(stream);
        }
        private static object Field(object source, string name) => AccessTools.Field(source.GetType(), name).GetValue(source);
        private static JObject RngStates()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter)
                    result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
    }
}
