#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class PyreDamageTakenMechanismBatch
    {
        private static bool capturing;
        private static readonly int[] DamageInputs = { int.MinValue, -1, 0, 1, 2, 9, 100, 16777217, int.MaxValue - 1, int.MaxValue };
        private static readonly int[] Extras = { int.MinValue, -10, -1, 0, 1, 3, 10, int.MaxValue };
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!PyreDamageTakenScenario.Enabled) return;
            var save = managers.GetSaveManager(); var combat = managers.GetCombatManager(); var relics = managers.GetRelicManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 ||
                typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Pyre damage batch requires settled pinned native state.");
            var sources = new List<(RelicData Owner, int Index, RelicEffectData Data)>();
            foreach (string collection in new[] { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas",
                "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" })
                foreach (RelicData owner in ((IEnumerable)AccessTools.Field(typeof(AllGameData), collection).GetValue(save.GetAllGameData())).Cast<RelicData>())
                    for (int i = 0; i < owner.GetEffects().Count; i++)
                        if (owner.GetEffects()[i].GetEffectClassName() == RelicResourceModel.ExtraPyreDamage) sources.Add((owner, i, owner.GetEffects()[i]));
            if (sources.Count != 4) throw new InvalidOperationException("Extra Pyre damage original definition denominator changed.");
            object Definitions() => sources.Select(source => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Extra = source.Data.GetParamInt(), Conditions = source.Data.GetEffectConditions().Count }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Definitions()); var rng = RngStates();
            int frame = Time.frameCount; var timer = Stopwatch.StartNew();
            var samples = new List<object>(); var products = new List<object>();
            RelicEffectApplyExtraPyreDamage Effect(RelicData owner, int extra)
            {
                var data = new RelicEffectData(); Set(data, "paramInt", extra); Set(data, "effectConditions", new List<RelicEffectCondition>());
                // Detached notification state preserves native OnTrigger without a live owner.
                Set(data, "notificationSuppressed", true);
                var state = new RelicState(); Set(state, "relicActivatedKey", ""); Set(state, "assetName", "PojuDetachedPyreDamage");
                var effect = new RelicEffectApplyExtraPyreDamage(); effect.Initialize(state, owner, data); return effect;
            }
            var parameters = new RelicEffectParams { relicManager = relics, saveManager = save };
            var active = save.GetMutators().Single(owner => owner.GetRelicDataID() == PyreDamageTakenScenario.Original!.GetID());
            var effects = active.GetEffects(); var saved = effects.ToArray();
            try
            {
                capturing = true;
                foreach (var source in sources)
                    foreach (int damage in DamageInputs)
                        samples.Add(new { OriginalId = source.Owner.GetID(), source.Index, Extra = source.Data.GetParamInt(), Damage = damage,
                            Actual = Effect(source.Owner, source.Data.GetParamInt()).ApplyModifiedDamage(damage, parameters, managers.GetCoreManagers()) });
                foreach (int[] extras in new[] { Array.Empty<int>() }.Concat(Extras.SelectMany(first => Extras.Select(second => new[] { first, second }))))
                {
                    effects.Clear(); effects.AddRange(extras.Select(extra => Effect(PyreDamageTakenScenario.Original!, extra)));
                    foreach (int damage in DamageInputs)
                        products.Add(new { Extras = extras, Damage = damage,
                            Actual = relics.ApplyTowerDamageTakenModifiedRelicEffects(damage, parameters) });
                }
            }
            finally { capturing = false; effects.Clear(); effects.AddRange(saved); }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Definitions()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) ||
                frame != Time.frameCount || queue.Count != 0 || !effects.SequenceEqual(saved) || combat.IsRunningTriggerQueue)
                throw new InvalidOperationException("Pyre damage batch changed source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenPyreDamageTaken", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Original = original, OriginalAfter = originalAfter,
                Before = before, After = after, RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount,
                QueueBefore = 0, QueueAfter = queue.Count, EffectsRestored = true, Seconds = timer.Elapsed.TotalSeconds, Samples = samples, Products = products }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "pyre-damage-taken-batch.mt2f"))) document.Write(output);
        }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        // The arithmetic and manager callbacks remain native. Only detached-test
        // popup presentation is suppressed; no gameplay notification is skipped.
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ShowRelicActivated),
            new[] { typeof(RelicData), typeof(Sprite), typeof(string), typeof(float), typeof(RoomState) })]
        private static class PopupPatch
        {
            private static bool Prefix() => !capturing;
        }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
