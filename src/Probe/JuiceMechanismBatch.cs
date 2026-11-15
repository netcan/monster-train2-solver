#if DEVELOPMENT_PROBE
using System;
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
    internal static class JuiceMechanismBatch
    {
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_JUICE_MECHANISM_BATCH") != "1") return;
            if (typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Juice batch requires the pinned 2.3.1 module.");
            var combat = managers.GetCombatManager(); var save = managers.GetSaveManager();
            var queue = (System.Collections.ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0)
                throw new InvalidOperationException("Juice batch requires an empty settled queue.");
            var originals = save.GetAllGameData().GetAllCardUpgradeData().OrderBy(data => data.GetID(), StringComparer.Ordinal)
                .SelectMany(data => data.GetTraitDataUpgrades().Select((trait, index) => new { data, trait, index }))
                .Where(item => CardJuiceModel.IsTrait(item.trait.GetTraitStateName())).ToArray();
            if (originals.Length == 0 || originals.Select(item => item.trait.GetTraitStateName()).Distinct().Count() != 3)
                throw new InvalidOperationException("Original upgrades must exercise all three Juice multipliers.");
            object Definitions() => originals.Select(item => new {
                Id = item.data.GetID(), Key = item.data.name, TraitIndex = item.index,
                Trait = CardTraitCompositionProbe.Definition(item.trait),
                Upgrade = CardModifierProbe.Upgrade(Upgrade(item.data), rejectFilters: false, filtersAlreadyApplied: true),
                Triggers = item.data.GetCardTriggerUpgrades().Select(trigger => new {
                    Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetCardEffects().Select(RuleCatalogProbe.Effect).ToArray() }).ToArray()
            }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var definitionsBefore = JToken.FromObject(Definitions());
            var rng = RngStates(); int frame = Time.frameCount;
            var samples = new List<object>(); var timer = Stopwatch.StartNew();
            int[] counts = { int.MinValue, int.MinValue / 2 - 1, -3, -1, 0, 1, 4, int.MaxValue / 4,
                int.MaxValue / 3, int.MaxValue / 2, int.MaxValue / 2 + 1, int.MaxValue };
            foreach (var item in originals)
            {
                var trait = (CardTraitJuice)Activator.CreateInstance(ShinyShoe.TypeNameCache.GetType(item.trait.GetTraitStateName()));
                trait.Setup(item.trait, CardState.None);
                foreach (int count in counts)
                    foreach (string status in new[] { "valor", "scorch", "unknown-status-control" })
                        samples.Add(new { Id = item.data.GetID(), TraitIndex = item.index, Count = count, Status = status,
                            Multiplier = trait.StackMultiplier,
                            Bonus = trait.OnStatusEffectApplied(null!, CardState.None, managers.GetCoreManagers(), status, count),
                            UpgradeStacks = trait.GetModifiedStatusEffectStacks(new StatusEffectStackData { statusId = status, count = count }) });
            }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var definitionsAfter = JToken.FromObject(Definitions());
            var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(definitionsBefore, definitionsAfter) ||
                !JToken.DeepEquals(rng, rngAfter) || frame != Time.frameCount || combat.IsRunningTriggerQueue || queue.Count != 0)
                throw new InvalidOperationException("Juice callback batch changed native source state, definitions, RNG, frame or queue.");
            using (var document = NativeFixtureCapture.Capture(new {
                Schema = 1, Scope = "DefinitionDrivenJuiceCallbacks", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Definitions = definitionsBefore, DefinitionsAfter = definitionsAfter, Before = before, After = after,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount,
                QueueBefore = 0, QueueAfter = queue.Count, Seconds = timer.Elapsed.TotalSeconds, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "juice-mechanism-batch.mt2f")))
                document.Write(output);
        }
        private static CardUpgradeState Upgrade(CardUpgradeData data) { var state = new CardUpgradeState(); state.Setup(data); return state; }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
