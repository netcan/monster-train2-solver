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
    internal static class PileSpawnUpgradeMechanismBatch
    {
        private static int? controlledCount;
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!PileSpawnUpgradeScenario.Enabled) return;
            var save = managers.GetSaveManager(); var combat = managers.GetCombatManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 ||
                typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Pile upgrade batch requires settled pinned native state.");
            var sources = new List<(RelicData Owner, int Index, RelicEffectData Data)>();
            foreach (string collection in new[] { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas", "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" })
                foreach (RelicData owner in ((IEnumerable)AccessTools.Field(typeof(AllGameData), collection).GetValue(save.GetAllGameData())).Cast<RelicData>())
                    for (int i = 0; i < owner.GetEffects().Count; i++)
                        if (owner.GetEffects()[i].GetEffectClassName() == RelicPileSpawnUpgradeModel.Effect) sources.Add((owner, i, owner.GetEffects()[i]));
            if (sources.Count != 4) throw new InvalidOperationException("Original pile upgrade denominator changed.");
            var definitions = sources.Select(source => (Id: source.Owner.GetID(), source.Owner, source.Data)).ToList();
            foreach (int threshold in new[] { int.MinValue, -1, 0, 1, 10, int.MaxValue })
                foreach (Team.Type team in new[] { Team.Type.None, Team.Type.Monsters, Team.Type.Heroes, Team.Type.Monsters | Team.Type.Heroes })
                    foreach (bool subtype in new[] { false, true })
                    {
                        var data = new RelicEffectData(); Set(data, "paramMaxInt", threshold); Set(data, "paramSourceTeam", team);
                        Set(data, "paramMinInt", 0); Set(data, "paramCharacterSubtype", subtype ? OriginalFirstImpBonusScenario.ImpSubtype : SubtypeData.None.Key);
                        Set(data, "paramCardUpgradeData", sources[0].Data.GetParamCardUpgradeData()); Set(data, "effectConditions", new List<RelicEffectCondition>());
                        definitions.Add(("control:" + definitions.Count, sources[0].Owner, data));
                    }
            RelicEffectAddUnitUpgradePerPile Effect(RelicData owner, RelicEffectData data)
            { var effect = new RelicEffectAddUnitUpgradePerPile(); effect.Initialize(null!, owner, data); return effect; }
            object Originals() => sources.Select(source => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Rule = RelicResourceProbe.Definition(Effect(source.Owner, source.Data), source.Index, managers).PileSpawnUpgrade,
                Conditions = source.Data.GetEffectConditions().Count }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Originals()); var rng = RngStates(); int frame = Time.frameCount;
            var counts = Enum.GetValues(typeof(CardPile)).Cast<CardPile>().Select(pile => new { Pile = pile.ToString(),
                Actual = CardPileHelper.GetVisibleCardCount(pile, managers.GetCardManager(), save) }).ToArray();
            var samples = new List<object>(); var rules = new List<object>(); var timer = Stopwatch.StartNew();
            var obj = new GameObject("Poju detached pile upgrade callbacks"); obj.SetActive(false);
            var character = ScriptableObject.CreateInstance<CharacterData>();
            try
            {
                var actor = obj.AddComponent<CharacterState>(); Set(actor, "allGameManagers", managers); Set(actor, "combatManager", combat);
                Set(actor, "characterData", character); Set(actor, "characterManager", managers.GetHeroManager());
                var destroyed = AccessTools.Field(typeof(CharacterState), "destroyedState"); destroyed.SetValue(actor, Enum.Parse(destroyed.FieldType, "None"));
                object state = Activator.CreateInstance(AccessTools.Inner(typeof(CharacterState), "StateInformation"), true);
                Set(actor, "_primaryStateInformation", state); Set(actor, "_previewStateInformation", state);
                foreach (var definition in definitions)
                {
                    var effect = Effect(definition.Owner, definition.Data);
                    var rule = RelicResourceProbe.Definition(effect, 0, managers).PileSpawnUpgrade!;
                    rules.Add(new { definition.Id, Rule = rule });
                    foreach (int count in new[] { -1, 0, 1, 9, 10, 11, 19, 20, 99 })
                        foreach (Team.Type team in new[] { Team.Type.None, Team.Type.Monsters, Team.Type.Heroes, Team.Type.Monsters | Team.Type.Heroes })
                            foreach (bool matching in new[] { false, true })
                            {
                                var subtypes = matching && rule.Subtype.Length > 0 ? new List<string> { rule.Subtype } : new List<string>();
                                Set(character, "subtypeKeys", subtypes); Set(actor, "subtypes", character.GetSubtypes()); Set(actor, "teamType", team);
                                controlledCount = count; int yielded = 0;
                                var callback = effect.OnCharacterAdded(actor, CardState.None, managers.GetCoreManagers());
                                while (callback.MoveNext())
                                {
                                    // Count native ApplyCardUpgrade requests without executing their children.
                                    if (!(callback.Current is IEnumerator) || ++yielded > 100) throw new InvalidOperationException("Unexpected pile upgrade callback request.");
                                }
                                samples.Add(new { Definition = definition.Id, Count = count, Team = RelicPercentageStatProbe.TeamName(team), Subtypes = subtypes, Actual = yielded });
                            }
                }
            }
            finally { controlledCount = null; UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(character); }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Originals()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) || frame != Time.frameCount || queue.Count != 0)
                throw new InvalidOperationException("Pile upgrade batch changed source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenPileSpawnUpgrade", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after, Original = original, OriginalAfter = originalAfter,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Definitions = rules, Counts = counts, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "pile-spawn-upgrade-batch.mt2f"))) document.Write(output);
        }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        [HarmonyPatch(typeof(CardPileHelper), nameof(CardPileHelper.GetVisibleCardCount))]
        private static class CountControl
        {
            private static bool Prefix(ref int __result)
            { if (!controlledCount.HasValue) return true; __result = controlledCount.Value; return false; }
        }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter).ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
