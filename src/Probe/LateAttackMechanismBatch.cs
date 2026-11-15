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
    internal static class LateAttackMechanismBatch
    {
        private static readonly Team.Type[] Teams = { Team.Type.None, Team.Type.Monsters, Team.Type.Heroes, Team.Type.Monsters | Team.Type.Heroes };
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_LATE_ATTACK_BATCH") != "1") return;
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var combat = managers.GetCombatManager();
            var relics = managers.GetRelicManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 ||
                typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Late-attack batch requires settled pinned native state.");
            var sources = new List<(RelicData Owner, int Index, RelicEffectData Data)>();
            foreach (string collection in new[] { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas",
                "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" })
                foreach (RelicData owner in ((IEnumerable)AccessTools.Field(typeof(AllGameData), collection).GetValue(all)).Cast<RelicData>())
                    for (int i = 0; i < owner.GetEffects().Count; i++)
                        if (owner.GetEffects()[i].GetEffectClassName() == RelicLateAttackModel.Effect) sources.Add((owner, i, owner.GetEffects()[i]));
            if (sources.Count != 1) throw new InvalidOperationException("Original late-attack definition denominator changed.");
            var source = sources.Single();
            object Original() => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Multiplier = source.Data.GetParamFloat(), IgnoreChampion = source.Data.GetParamBool(),
                Team = RelicPercentageStatProbe.TeamName(source.Data.GetParamSourceTeam()), Conditions = source.Data.GetEffectConditions().Count };
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Original());
            var rng = RngStates(); int frame = Time.frameCount; var timer = Stopwatch.StartNew();
            var queries = new List<object>(); var products = new List<object>(); var attacks = new List<object>();
            RelicEffectMultiplyAttackLate Effect(float multiplier, Team.Type team, bool ignore)
            {
                var data = new RelicEffectData(); Set(data, "effectConditions", new List<RelicEffectCondition>());
                Set(data, "paramFloat", multiplier); Set(data, "paramSourceTeam", team); Set(data, "paramBool", ignore);
                var effect = new RelicEffectMultiplyAttackLate(); effect.Initialize(null!, source.Owner, data); return effect;
            }
            foreach (float multiplier in new[] { -2f, -.5f, 0f, .1f, .3f, .5f, 1f, 1.5f, 2f, 1000f })
                foreach (Team.Type team in Teams)
                    foreach (bool ignore in new[] { false, true })
                    {
                        var effect = Effect(multiplier, team, ignore);
                        foreach (Team.Type target in Teams)
                            foreach (bool champion in new[] { false, true })
                                queries.Add(new { Multiplier = multiplier, Team = RelicPercentageStatProbe.TeamName(team), IgnoreChampion = ignore,
                                    Target = RelicPercentageStatProbe.TeamName(target), Champion = champion, Actual = effect.GetMultiplier(target, champion) });
                    }
            var active = save.GetMutators().Single(owner => owner.GetRelicDataID() == source.Owner.GetID());
            var effects = active.GetEffects(); var savedEffects = effects.ToArray();
            var obj = new GameObject("Poju detached late-attack queries"); obj.SetActive(false);
            var dataObject = ScriptableObject.CreateInstance<CharacterData>();
            try
            {
                var actor = obj.AddComponent<CharacterState>(); Set(actor, "allGameManagers", managers); Set(actor, "combatManager", combat);
                Set(actor, "characterData", dataObject); Set(actor, "characterManager", managers.GetHeroManager());
                Set(actor, "relicManager", relics); Set(actor, "saveManager", save);
                var destroyed = AccessTools.Field(typeof(CharacterState), "destroyedState"); destroyed.SetValue(actor, Enum.Parse(destroyed.FieldType, "None"));
                var stateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
                object state = Activator.CreateInstance(stateType, true); Set(actor, "_primaryStateInformation", state); Set(actor, "_previewStateInformation", state);
                void Stat(string name, int value) => AccessTools.Property(stateType, name).SetValue(state, value);
                var championSubtypes = managers.GetCardManager().GetAllCards(new List<CardState>()).Select(card => card.GetSpawnCharacterData())
                    .Where(data => data != null && data.IsChampion()).First().GetSubtypes();
                foreach (float[] multipliers in new[] { new[] { .5f }, new[] { .1f, .2f, .3f }, new[] { 1.1f, .9f, 1.3f }, new[] { 1.5f, .5f }, new[] { -1f, -.5f } })
                    foreach (bool ignore in new[] { false, true })
                    {
                        effects.Clear(); effects.AddRange(multipliers.Select(multiplier => Effect(multiplier, Team.Type.Monsters, ignore)));
                        foreach (Team.Type target in Teams)
                            foreach (bool champion in new[] { false, true })
                            {
                                float actual = relics.GetLateAttackMultiplierFromRelics(target, champion);
                                products.Add(new { Multipliers = multipliers, IgnoreChampion = ignore, Target = RelicPercentageStatProbe.TeamName(target), Champion = champion, Actual = actual });
                                if (target != Team.Type.Monsters && target != Team.Type.Heroes) continue;
                                Set(actor, "teamType", target); Set(actor, "subtypes", champion ? championSubtypes : new List<SubtypeData>());
                                foreach (var input in new[] { (Raw: -7, Buff: 0, Status: 0), (0, 0, 0), (1, 0, 0), (81, 0, 0), (7, 3, -2), (7, -10, 1), (16777217, 0, 0), (int.MaxValue, 0, 0) })
                                    foreach (bool pyre in new[] { false, true })
                                    {
                                        Set(actor, "isPyreHeart", pyre); actor.SetAttackDamage(input.Item1); actor.SetHealth(100, 100);
                                        Stat("DamageBuff", input.Item2); Stat("DamageBuffFromStatusEffects", input.Item3);
                                        attacks.Add(new { Multipliers = multipliers, IgnoreChampion = ignore, Team = RelicPercentageStatProbe.TeamName(target), Champion = champion,
                                            Pyre = pyre, Raw = input.Item1, Buff = input.Item2, Status = input.Item3, PyreBase = save.GetModifiedPyreBaseAttack(),
                                            Actual = actor.GetAttackDamage(), WithoutStatuses = actor.GetAttackDamageWithoutStatusEffectBuffs() });
                                    }
                            }
                    }
            }
            finally
            {
                effects.Clear(); effects.AddRange(savedEffects);
                UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(dataObject);
            }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Original()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) ||
                frame != Time.frameCount || queue.Count != 0 || !effects.SequenceEqual(savedEffects) || combat.IsRunningTriggerQueue)
                throw new InvalidOperationException("Late-attack batch changed its source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenLateAttack", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Original = original, OriginalAfter = originalAfter,
                Before = before, After = after, RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount,
                QueueBefore = 0, QueueAfter = queue.Count, EffectsRestored = true, Seconds = timer.Elapsed.TotalSeconds, Queries = queries, Products = products, Attacks = attacks }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "late-attack-batch.mt2f"))) document.Write(output);
        }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
