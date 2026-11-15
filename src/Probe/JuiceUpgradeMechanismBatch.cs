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
    internal static class JuiceUpgradeMechanismBatch
    {
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!JuiceUpgradeScenario.Enabled) return;
            var save = managers.GetSaveManager(); var combat = managers.GetCombatManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 ||
                typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Juice upgrade batch requires settled pinned state.");
            var originals = save.GetAllGameData().GetAllCardUpgradeData().OrderBy(data => data.GetID(), StringComparer.Ordinal)
                .SelectMany(data => data.GetTraitDataUpgrades().Where(trait => CardJuiceModel.IsTrait(trait.GetTraitStateName()))
                    .Select(trait => (Data: data, Trait: trait))).ToArray();
            if (originals.Length != 5) throw new InvalidOperationException("Original Juice upgrade denominator changed.");
            object Definitions() => originals.Select(item => new { Id = item.Data.GetID(), Key = item.Data.name,
                Trait = CardTraitCompositionProbe.Definition(item.Trait) }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var definitions = JToken.FromObject(Definitions());
            var rng = RngStates(); int frame = Time.frameCount; var timer = Stopwatch.StartNew();
            var owner = new CardState(); var other = new CardState();
            var payload = DynamicUpgradeScenario.Upgrade("PojuJuiceUpgradePayload", "fc231ba2-0000-4000-8000-000000000001", 5, 7, 0, 0, "valor", 1);
            AccessTools.Field(typeof(CardUpgradeData), "statusEffectUpgrades").SetValue(payload, new List<StatusEffectStackData> {
                new StatusEffectStackData { statusId = "valor", count = 1 }, new StatusEffectStackData { statusId = "armor", count = -1 },
                new StatusEffectStackData { statusId = "scorch", count = 2 } });
            CardUpgradeState Fresh(int count) { var state = new CardUpgradeState(); state.Setup(payload); state.GetStatusEffectUpgrades()[0].count = count; return state; }
            var traits = originals.Select(item => { var trait = (CardTraitJuice)Activator.CreateInstance(ShinyShoe.TypeNameCache.GetType(item.Trait.GetTraitStateName()));
                trait.Setup(item.Trait, owner); return trait; }).ToArray();
            var samples = new List<object>(); var sequences = new List<object>();
            int[] counts = { int.MinValue, int.MinValue / 2 - 1, -3, -1, 0, 1, 4, int.MaxValue / 4,
                int.MaxValue / 3, int.MaxValue / 2, int.MaxValue / 2 + 1, int.MaxValue };
            try
            {
                for (int i = 0; i < originals.Length; i++)
                    foreach (int count in counts)
                        foreach (int source in new[] { 0, 1, 2 })
                            foreach (int target in new[] { 0, 1, 2 })
                            {
                                var upgrade = Fresh(count); var input = CardModifierProbe.Upgrade(upgrade);
                                CardState? Card(int relation) => relation == 0 ? null : relation == 1 ? owner : other;
                                bool returned = traits[i].OnCardBeingUpgraded(Card(target)!, Card(source), upgrade, managers.GetCoreManagers());
                                var actual = CardModifierProbe.Upgrade(upgrade); var clone = new CardUpgradeState(); clone.Setup(upgrade);
                                bool repeatReturned = traits[i].OnCardBeingUpgraded(Card(target)!, Card(source), upgrade, managers.GetCoreManagers());
                                samples.Add(new { Id = originals[i].Data.GetID(), Count = count, Source = source, Target = target, Input = input,
                                    Actual = actual, Returned = returned, Repeated = CardModifierProbe.Upgrade(upgrade), RepeatReturned = repeatReturned,
                                    CloneStatuses = clone.GetStatusEffectUpgrades().Select(status => new { Id = status.statusId, Stacks = status.count }).ToArray() });
                            }
                for (int i = 0; i < originals.Length; i++)
                    for (int j = 0; j < originals.Length; j++)
                        foreach (int count in counts)
                        {
                            var upgrade = Fresh(count); var input = CardModifierProbe.Upgrade(upgrade);
                            bool first = traits[i].OnCardBeingUpgraded(other, owner, upgrade, managers.GetCoreManagers());
                            bool second = traits[j].OnCardBeingUpgraded(other, owner, upgrade, managers.GetCoreManagers());
                            sequences.Add(new { Ids = new[] { originals[i].Data.GetID(), originals[j].Data.GetID() }, Count = count,
                                Input = input, Actual = CardModifierProbe.Upgrade(upgrade), Returned = first && second });
                        }
            }
            finally { UnityEngine.Object.DestroyImmediate(payload); }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var definitionsAfter = JToken.FromObject(Definitions()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(definitions, definitionsAfter) || !JToken.DeepEquals(rng, rngAfter) ||
                frame != Time.frameCount || queue.Count != 0 || combat.IsRunningTriggerQueue) throw new InvalidOperationException("Juice upgrade batch mutated its source.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenJuiceUpgrades", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Definitions = definitions, DefinitionsAfter = definitionsAfter,
                Before = before, After = after, RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount,
                QueueBefore = 0, QueueAfter = queue.Count, Seconds = timer.Elapsed.TotalSeconds, Samples = samples, Sequences = sequences }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "juice-upgrade-batch.mt2f"))) document.Write(output);
        }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
