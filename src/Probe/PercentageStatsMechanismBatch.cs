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
    internal static class PercentageStatsMechanismBatch
    {
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_PERCENTAGE_STATS_BATCH") != "1") return;
            if (typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Percentage batch requires pinned 2.3.1.");
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var combat = managers.GetCombatManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0) throw new InvalidOperationException("Unsettled percentage batch.");
            var sources = new List<(string Collection, RelicData Owner, int Index, RelicEffectData Data)>();
            foreach (string collection in new[] { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas",
                "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" })
                foreach (RelicData owner in ((IEnumerable)AccessTools.Field(typeof(AllGameData), collection).GetValue(all)).Cast<RelicData>())
                    for (int i = 0; i < owner.GetEffects().Count; i++)
                        if (owner.GetEffects()[i].GetEffectClassName() == RelicPercentageStatModel.Effect)
                            sources.Add((collection, owner, i, owner.GetEffects()[i]));
            sources = sources.OrderBy(item => item.Owner.GetID(), StringComparer.Ordinal).ThenBy(item => item.Index).ToList();
            if (sources.Count != 56 || sources.Select(item => item.Owner.GetID()).Distinct().Count() != 6)
                throw new InvalidOperationException("Original percentage definition denominator changed.");
            var definitions = new List<(string Id, RelicData Owner, RelicEffectData Data, RelicPercentageStat Rule, bool Original)>();
            RelicPercentageStat Rule(RelicData owner, RelicEffectData data, int index)
            {
                var effect = new RelicEffectModifyCharacterAttackOrHealthPercentage();
                effect.Initialize(null!, owner, data);
                return RelicPercentageStatProbe.Capture(effect, index, save);
            }
            foreach (var item in sources)
                definitions.Add((item.Owner.GetID() + ":" + item.Index, item.Owner, item.Data, Rule(item.Owner, item.Data, item.Index), true));
            foreach (int percent in new[] { int.MinValue, -100, -50, -1, 0, 1, 25, 50, 100, int.MaxValue })
                foreach (bool health in new[] { false, true })
                    foreach (Team.Type team in new[] { Team.Type.None, Team.Type.Monsters, Team.Type.Heroes, Team.Type.Monsters | Team.Type.Heroes })
                    {
                        var data = new RelicEffectData(); Set(data, "effectConditions", new List<RelicEffectCondition>()); Set(data, "paramInt", percent); Set(data, "paramBool", health); Set(data, "paramSourceTeam", team);
                        definitions.Add(("control:" + definitions.Count, sources[0].Owner, data, Rule(sources[0].Owner, data, 0), false));
                    }
            foreach (MutatorData mutator in ((IEnumerable)AccessTools.Field(typeof(AllGameData), "mutatorDatas").GetValue(all)).Cast<MutatorData>()
                .GroupBy(data => save.GetHasMutator(data)).Select(group => group.First()))
            {
                var data = new RelicEffectData(); Set(data, "effectConditions", new List<RelicEffectCondition>()); Set(data, "paramInt", 50); Set(data, "paramBool", true);
                Set(data, "paramSourceTeam", Team.Type.Heroes); Set(data, "paramMutatorData", mutator);
                definitions.Add(("required:" + mutator.GetID(), sources[0].Owner, data, Rule(sources[0].Owner, data, 0), false));
            }
            object Originals() => sources.Select(item => new { item.Collection, Id = item.Owner.GetID(), Key = item.Owner.name,
                item.Index, Rule = Rule(item.Owner, item.Data, item.Index), Conditions = item.Data.GetEffectConditions().Count }).ToArray();
            var originalBefore = JToken.FromObject(Originals()); var before = JToken.FromObject(trace.CaptureDecision());
            var rng = RngStates(); int frame = Time.frameCount; var timer = Stopwatch.StartNew();
            var obj = new GameObject("Poju detached percentage-stat callbacks"); obj.SetActive(false);
            var dataObject = ScriptableObject.CreateInstance<CharacterData>(); var samples = new List<object>();
            try
            {
                var actor = obj.AddComponent<CharacterState>(); Set(actor, "allGameManagers", managers); Set(actor, "combatManager", combat);
                Set(actor, "characterData", dataObject); Set(actor, "characterManager", managers.GetHeroManager());
                var destroyed = AccessTools.Field(typeof(CharacterState), "destroyedState"); destroyed.SetValue(actor, Enum.Parse(destroyed.FieldType, "None"));
                var stateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
                object state = Activator.CreateInstance(stateType, true); Set(actor, "_primaryStateInformation", state); Set(actor, "_previewStateInformation", state);
                foreach (var definition in definitions)
                {
                    var effect = new RelicEffectModifyCharacterAttackOrHealthPercentage(); effect.Initialize(null!, definition.Owner, definition.Data);
                    foreach (var stats in new[] { (Attack: -3, Health: -5), (0, 0), (1, 1), (7, 11), (101, 99999), (int.MaxValue, int.MaxValue), (int.MinValue, int.MinValue) })
                        foreach (Team.Type team in new[] { Team.Type.None, Team.Type.Monsters, Team.Type.Heroes, Team.Type.Monsters | Team.Type.Heroes })
                            foreach (bool matching in new[] { false, true })
                            {
                                var subtypes = matching && definition.Rule.SubtypeKey.Length > 0 ? new List<string> { definition.Rule.SubtypeKey } : new List<string>();
                                Set(dataObject, "attackDamage", stats.Item1); Set(dataObject, "health", stats.Item2); Set(dataObject, "subtypeKeys", subtypes);
                                Set(actor, "subtypes", dataObject.GetSubtypes());
                                var query = effect.GetStatAdjustment(dataObject, team, managers.GetRelicManager());
                                Set(actor, "teamType", team); actor.SetHealth(stats.Item2 - (stats.Item2 > 0 ? 1 : 0), stats.Item2); actor.SetAttackDamage(stats.Item1);
                                int inputHp = actor.GetHP(), inputMax = actor.GetMaxHP(), inputRaw = actor.GetUnbuffedAttackDamage();
                                int additional = managers.GetRelicManager().GetAdditionalAttackFromRelics(actor);
                                float late = managers.GetRelicManager().GetLateAttackMultiplierFromRelics(team, actor.IsChampion());
                                Set(effect, "lastBuffValue", 0);
                                var callback = effect.OnCharacterAdded(actor, CardState.None, managers.GetCoreManagers());
                                if (callback.MoveNext()) throw new InvalidOperationException("Percentage callback unexpectedly yielded.");
                                samples.Add(new { Definition = definition.Id, Team = RelicPercentageStatProbe.TeamName(team), Subtypes = subtypes,
                                    Attack = stats.Item1, Health = stats.Item2, InputHp = inputHp, InputMaximum = inputMax, InputRaw = inputRaw, Additional = additional, Late = late,
                                    QueryAttack = query?.attackAdjustment, QueryHealth = query?.healthAdjustment,
                                    ActualHp = actor.GetHP(), ActualMaximum = actor.GetMaxHP(), ActualRaw = (int)AccessTools.Property(stateType, "AttackDamage").GetValue(state),
                                    LastBuff = (int)AccessTools.Field(effect.GetType(), "lastBuffValue").GetValue(effect) });
                            }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(dataObject); }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Originals()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(originalBefore, originalAfter) || !JToken.DeepEquals(rng, rngAfter) ||
                frame != Time.frameCount || queue.Count != 0 || combat.IsRunningTriggerQueue) throw new InvalidOperationException("Percentage batch changed its source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenPercentageStats", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, OriginalDefinitions = originalBefore, OriginalDefinitionsAfter = originalAfter,
                Definitions = definitions.Select(item => new { item.Id, item.Original, item.Rule }).ToArray(), Before = before, After = after,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "percentage-stats-batch.mt2f"))) document.Write(output);
        }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
