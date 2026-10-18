using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignDraftEnhancerCountCalibration
    {
        internal const string Id = "9427e610-0c7b-4430-ab1f-c84049097168";
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_DRAFT_ENHANCER_COUNT") != "1") return;
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == Id);
            RelicEffectData definition = owner.GetEffects().Single();
            MutableRules rules = (MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save);
            var map = (Dictionary<MutableRules.Condition, List<IBalanceEditingRelicEffect>>)AccessTools.Field(typeof(MutableRules), "ruleMap").GetValue(rules);
            MutableRules.Condition condition = MutableRules.Condition.NumCardsToApplyRandomDraftEnhancerTo;
            if (owner.name != "DrawOnCorruption" || owner.DisallowInDeploymentPhase ||
                definition.GetEffectClassName() != "RelicEffectChangeNumCardsToApplyRandomDraftEnhancerTo" ||
                definition.GetParamInt() != 1 || definition.GetEffectConditions().Count != 0 || save.PreviewMode ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id) ||
                map.TryGetValue(condition, out var initial) && initial.Count != 0)
                throw new InvalidOperationException("Draft enhancer-count calibration requires the unchanged isolated original owner.");
            JObject Snapshot() => JObject.FromObject(new {
                Relics = save.GetCollectedRelics().Select(item => item.GetRelicDataID()).ToArray(),
                Deck = save.GetDeckState().Select(card => new { DataId = card.GetCardDataID(), Permanent = CardModifierProbe.Modifiers(card.GetCardStateModifiers()),
                    Temporary = CardModifierProbe.Modifiers(card.GetTemporaryCardStateModifiers()) }).ToArray(),
                Gold = save.GetGold(), Health = save.GetTowerHP(), MaximumHealth = save.GetMaxTowerHP(),
                Hoard = save.GetDragonsHoardAmount(), HoardCap = save.GetDragonsHoardCap(),
                Frame = UnityEngine.Time.frameCount, Preview = save.PreviewMode,
                Rng = Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                    .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray() });
            JObject before = Snapshot(); var consumers = new List<object>(); var matrix = new List<object>();
            object? runtimeDefinition = null;
            void Remove() => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == Id));
            void Queries(string phase)
            {
                bool owned = save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id);
                IBalanceEditingRelicEffect[] effects = map.TryGetValue(condition, out var registered) ? registered.ToArray() : Array.Empty<IBalanceEditingRelicEffect>();
                if (effects.Length != (owned ? 1 : 0) || effects.Any(effect => effect.GetType() != typeof(RelicEffectChangeNumCardsToApplyRandomDraftEnhancerTo) ||
                    effect.Type != MutableRules.Type.Additive || effect.Condition != condition || effect.Value != 1f))
                    throw new InvalidOperationException("Native draft-count registration changed.");
                int[] additions = effects.Select(effect => UnityEngine.Mathf.RoundToInt(effect.Value)).ToArray();
                JObject queryBefore = Snapshot(); int actual = save.GetNumCardsToApplyRandomDraftEnhancerTo(); JObject queryAfter = Snapshot();
                if (!JToken.DeepEquals(queryBefore, queryAfter)) throw new InvalidOperationException("Native count consumer changed state/frame/RNG.");
                consumers.Add(new { Phase = phase, Owned = owned, Additions = additions, Actual = actual, Before = queryBefore, After = queryAfter });
                foreach (int start in new[] { -2, 0, 1, 2, 5, int.MaxValue })
                    matrix.Add(new { Phase = phase, Owned = owned, Additions = additions, Start = start,
                        Actual = rules.GetRule(condition, start, 1) });
                if (owned) runtimeDefinition = new { Type = effects[0].Type.ToString(), Condition = effects[0].Condition.ToString(), effects[0].Value };
            }
            try
            {
                Queries("baseline"); save.AddRelic(owner); Queries("owned"); Remove(); Queries("removed");
                save.AddRelic(owner); Queries("reacquired"); Remove(); Queries("removed-again");
            }
            finally { if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id)) Remove(); }
            JObject after = Snapshot();
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Draft-count calibration did not restore recorded state/frame/RNG.");
            using (var document = NativeFixtureCapture.Capture(new {
                Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId,
                Owner = new { Id, Key = owner.name, Type = definition.GetEffectClassName(), Amount = definition.GetParamInt(),
                    DisallowedInPlacement = owner.DisallowInDeploymentPhase, Conditions = definition.GetEffectConditions().Count, Runtime = runtimeDefinition },
                Before = before, After = after, Consumers = consumers, RuleMatrix = matrix }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-draft-enhancer-count-calibration.mt2f")))
                document.Write(stream);
        }
    }
}
