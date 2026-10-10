using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicCardUpgradeCalibration
    {
        private static bool observing;
        private static readonly List<RelicCardFilterResult> filters = new List<RelicCardFilterResult>();
        private static bool Filter(CardUpgradeMaskData mask, CardState card, RelicManager manager)
        {
            bool accepted = mask.FilterCard(card, manager);
            if (observing) filters.Add(new RelicCardFilterResult(mask.name, accepted));
            RelicCardModifierProbe.ObserveFilter(mask.name, accepted);
            return accepted;
        }
        // Mono shares generic reference-type method bodies. Instrument the
        // effect's call site, preserving FilterCard<CardData> and all other users.
        [HarmonyPatch(typeof(RelicEffectAddTempUpgrade), nameof(RelicEffectAddTempUpgrade.ApplyCardStateModifiers))]
        private static class FilterPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int replaced = 0;
                foreach (var instruction in instructions)
                {
                    if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(CardUpgradeMaskData) && method.Name == nameof(CardUpgradeMaskData.FilterCard))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(RelicCardUpgradeCalibration), nameof(Filter));
                        replaced++;
                    }
                    yield return instruction;
                }
                if (replaced != 1) throw new InvalidOperationException("Unexpected native temporary relic filter call site.");
            }
        }
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_UPGRADES") != "1") return;
            var managers = AllGameManagers.Instance!; var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var relics = managers.GetRelicManager();
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return result;
            }
            var before = JToken.FromObject(trace.CaptureContext()); var rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            var objects = new List<UnityEngine.Object>(); var rows = new List<object>();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            var originals = ((IEnumerable)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(data)).Cast<RelicData>()
                .SelectMany(relic => relic.GetEffects().Where(effect => effect.GetEffectClassName() == "RelicEffectAddTempUpgrade")
                    .Select(effect => new { Relic = relic, Effect = effect })).ToArray();
            var definitions = data.GetAllCardData().Where(card => card != null).GroupBy(card => card.GetID()).Select(group => group.First()).ToArray();
            object Clone(object value) => AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null);
            RelicCardUpgradeRule Rule(string name, RelicEffectData effect)
            {
                var source = effect.GetParamCardUpgradeData(); CardLifecycleUpgrade? upgrade = null;
                if (source != null) { var instance = new CardUpgradeState(); instance.Setup(source); upgrade = CardUpgradeLifecycleProbe.Definition(instance); }
                Team.Type sourceTeam = effect.GetParamSourceTeam();
                return new RelicCardUpgradeRule(name, sourceTeam.HasFlag(Team.Type.Monsters), upgrade,
                    source?.GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray() ?? Array.Empty<CardUpgradeMaskRule>(),
                    sourceTeam.HasFlag(Team.Type.Heroes));
            }
            void Observe(string group, string operation, CardState card, CardUpgradeLifecycleProbe probe, RelicData? relic = null, RelicEffectData? effectData = null)
            {
                var input = probe.Capture(card); int nextId = probe.NextInstanceId; bool? returned = null;
                RelicCardUpgradeRule? rule = effectData == null ? null : Rule(relic!.name, effectData);
                filters.Clear();
                if (operation == "Reset") card.ResetTemporaryCardModifiers(save);
                else
                {
                    var effect = new RelicEffectAddTempUpgrade(); effect.Initialize(new RelicState(), relic!, effectData!);
                    if (!effect.AreConditionsTrue(managers.GetCardStatistics())) throw new InvalidOperationException("Unexpected original relic condition.");
                    observing = true;
                    try { returned = effect.ApplyCardStateModifiers(card, relics, save, null); } finally { observing = false; }
                }
                var actual = probe.Capture(card); int nextAfter = probe.NextInstanceId;
                bool added = actual.Temporary.Any(upgrade => upgrade.InstanceId == nextId);
                var actualFilters = filters.ToArray();
                var view = CardUpgradeMaskCalibration.Card(card, relics);
                rows.Add(new { Group = group, Operation = operation, Rule = rule, Before = input, NextInstanceId = nextId,
                    Returned = returned, UpgradeAdded = added, Filters = actualFilters, Actual = actual, NextAfter = nextAfter,
                    Card = view, AfterQuery = probe.Capture(card) });
            }
            try
            {
                foreach (var item in originals)
                {
                    if (item.Effect.GetEffectConditions().Count != 0) throw new InvalidOperationException("Original temporary relic has unsupported conditions.");
                    var source = item.Effect.GetParamCardUpgradeData();
                    var representative = definitions.First(card => source.GetFilters().All(mask => mask.FilterCard(card, relics)));
                    var card = new CardState(representative, save, setupStartingUpgrades: false); var probe = new CardUpgradeLifecycleProbe();
                    string group = "eligible/" + item.Relic.name;
                    Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                    Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                    Observe(group, "Reset", card, probe);
                    Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                    foreach (var existing in owned)
                    {
                        card = CardUpgradeLifecycleProbe.Copy(existing); probe = new CardUpgradeLifecycleProbe();
                        group = "owned/" + item.Relic.name + "/" + trace.CardId(existing);
                        Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                        Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                    }
                }
                foreach (var existing in new[] { owned.First(card => card.IsSpawnerCard()), owned.First(card => card.GetCardType() == CardType.Spell) })
                {
                    var card = CardUpgradeLifecycleProbe.Copy(existing); var probe = new CardUpgradeLifecycleProbe(); string group = "ordered/" + existing.GetCardType();
                    Observe(group, "Reset", card, probe);
                    foreach (var item in originals) Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                    Observe(group, "Reset", card, probe);
                    foreach (var item in originals.Reverse()) Observe(group, "Apply", card, probe, item.Relic, item.Effect);
                }
                var basis = originals.First(item => item.Relic.name == "ReduceStarterCost");
                foreach (string name in new[] { "heroes", "missing-upgrade", "first-reject", "second-reject", "no-filters" })
                {
                    var effect = (RelicEffectData)Clone(basis.Effect);
                    if (name == "heroes") AccessTools.Field(typeof(RelicEffectData), "paramSourceTeam").SetValue(effect, Team.Type.Heroes);
                    else if (name == "missing-upgrade") AccessTools.Field(typeof(RelicEffectData), "paramCardUpgradeData").SetValue(effect, null);
                    else
                    {
                        var upgrade = UnityEngine.Object.Instantiate(basis.Effect.GetParamCardUpgradeData()); objects.Add(upgrade);
                        var localFilters = new List<CardUpgradeMaskData>();
                        foreach (var type in name == "second-reject" ? new[] { CardType.Monster, CardType.Spell } : name == "first-reject" ? new[] { CardType.Spell, CardType.Monster } : Array.Empty<CardType>())
                        {
                            var mask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>(); objects.Add(mask); mask.name = name + "/" + type;
                            AccessTools.Field(typeof(CardUpgradeMaskData), "cardType").SetValue(mask, type); localFilters.Add(mask);
                        }
                        AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(upgrade, localFilters);
                        AccessTools.Field(typeof(RelicEffectData), "paramCardUpgradeData").SetValue(effect, upgrade);
                    }
                    var card = CardUpgradeLifecycleProbe.Copy(owned.First(value => value.IsSpawnerCard())); var probe = new CardUpgradeLifecycleProbe();
                    AccessTools.Field(typeof(CardState), "refreshCombinedTraitsDirty").SetValue(card, true);
                    Observe("controlled/" + name, "Apply", card, probe, basis.Relic, effect);
                    Observe("controlled/" + name, "Apply", card, probe, basis.Relic, effect);
                }
            }
            finally { observing = false; foreach (var item in objects) UnityEngine.Object.DestroyImmediate(item); }
            var after = JToken.FromObject(trace.CaptureContext()); var rngAfter = Rng();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(rngBefore, rngAfter) || frame != UnityEngine.Time.frameCount)
                throw new InvalidOperationException("Native relic card upgrade capture changed live context/RNG/frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, ContextUnchanged = true,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter,
                OriginalRelicAssets = originals.Select(item => item.Relic.name).ToArray(), OwnedCardCount = owned.Length, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "relic-card-upgrade-calibration.mt2f"))) document.Write(stream);
        }
    }
}
