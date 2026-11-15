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
    internal static class SpawnStatusScalingMechanismBatch
    {
        private static CharacterState? intercepted;
        private static List<object>? requests;
        internal static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        internal static RelicData[] Owners(AllGameData all) => new[] { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas", "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" }
            .SelectMany(collection => ((IEnumerable)AccessTools.Field(typeof(AllGameData), collection).GetValue(all)).Cast<RelicData>()).ToArray();
        internal static CardEffectData StatusEffect(string type, int count)
        {
            var data = new CardEffectData(type, null!, Team.Type.Heroes); data.Cheat_SetTargetMode(TargetMode.Self);
            Set(data, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "buff", count = count },
                new StatusEffectStackData { statusId = "armor", count = count }, new StatusEffectStackData { statusId = "spikes", count = count } });
            return data;
        }
        internal static CharacterTriggerData Trigger(int count, bool enchant)
        {
            var data = HealingScenario.HealGold(0, false, false);
            Set(data, "trigger", CharacterTriggerData.Trigger.PreCombat);
            Set(data, "effects", (enchant ? new[] { "CardEffectAddStatusEffect", "CardEffectEnchant", "CardEffectAddStatusEffectNextMonster" } : new[] { "CardEffectAddStatusEffect" })
                .Select(type => StatusEffect(type, count)).ToList());
            return data;
        }
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (!SpawnStatusScalingScenario.Enabled) return;
            var save = managers.GetSaveManager(); var combat = managers.GetCombatManager();
            var queue = (ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
            if (save.PreviewMode || combat.IsRunningTriggerQueue || queue.Count != 0 || typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("Birth status scaling requires settled pinned state.");
            var originals = Owners(save.GetAllGameData()).SelectMany(owner => owner.GetEffects().Select((data, index) => (Owner: owner, Data: data, Index: index)))
                .Where(source => source.Data.GetEffectClassName() == RelicSpawnStatusScalingModel.Effect).ToArray();
            if (originals.Length != 4) throw new InvalidOperationException("Original spawn scaling denominator changed.");
            var definitions = originals.Select(source => (Id: source.Owner.GetID(), source.Owner, source.Data)).ToList();
            foreach (float multiplier in new[] { -1f, 0f, 0.5f, 1f, 1.2f, 1.5f })
                for (int flags = 0; flags < 8; flags++)
                {
                    var data = new RelicEffectData(); Set(data, "paramFloat", multiplier); Set(data, "paramBool", (flags & 1) != 0);
                    Set(data, "paramBool2", (flags & 2) != 0); Set(data, "paramBool3", (flags & 4) != 0);
                    Set(data, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "buff", count = 999 } });
                    Set(data, "paramExcludeCharacterSubtypes", new[] { OriginalFirstImpBonusScenario.ImpSubtype });
                    Set(data, "effectConditions", new List<RelicEffectCondition>());
                    definitions.Add(("control:" + definitions.Count, originals[0].Owner, data));
                }
            // Actual callback's inverted required-mutator gate: present and absent.
            foreach (var required in new[] { (MutatorData)save.GetMutators()[0].GetSourceRelicData(), Owners(save.GetAllGameData()).OfType<MutatorData>().First(value => value.GetID() != save.GetMutators()[0].GetRelicDataID()) })
            {
                var data = new RelicEffectData(); Set(data, "paramFloat", 1.5f); Set(data, "paramBool", true); Set(data, "paramBool2", true);
                Set(data, "paramExcludeCharacterSubtypes", Array.Empty<string>()); Set(data, "paramStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(data, "paramMutatorData", required); Set(data, "effectConditions", new List<RelicEffectCondition>());
                definitions.Add(("control:" + definitions.Count, originals[0].Owner, data));
            }
            RelicEffectScaleStatusApplicationsOnSpawn Effect(RelicData owner, RelicEffectData data)
            { var effect = new RelicEffectScaleStatusApplicationsOnSpawn(); effect.Initialize(null!, owner, data); return effect; }
            object Describe() => originals.Select(source => new { Id = source.Owner.GetID(), Key = source.Owner.name, source.Index,
                Rule = RelicResourceProbe.Definition(Effect(source.Owner, source.Data), source.Index, managers).SpawnStatusScaling }).ToArray();
            var before = JToken.FromObject(trace.CaptureDecision()); var original = JToken.FromObject(Describe()); var rng = RngStates(); int frame = Time.frameCount;
            var samples = new List<object>(); var rules = new List<object>(); var timer = Stopwatch.StartNew();
            var obj = new GameObject("Poju detached spawn status scaling"); obj.SetActive(false); var character = ScriptableObject.CreateInstance<CharacterData>();
            try
            {
                var actor = obj.AddComponent<CharacterState>(); Set(actor, "allGameManagers", managers); Set(actor, "combatManager", combat);
                Set(actor, "characterData", character); Set(actor, "characterManager", managers.GetHeroManager());
                var destroyed = AccessTools.Field(typeof(CharacterState), "destroyedState"); destroyed.SetValue(actor, Enum.Parse(destroyed.FieldType, "None"));
                object state = Activator.CreateInstance(AccessTools.Inner(typeof(CharacterState), "StateInformation"), true);
                Set(actor, "_primaryStateInformation", state); Set(actor, "_previewStateInformation", state);
                foreach (var definition in definitions)
                {
                    var effect = Effect(definition.Owner, definition.Data); var rule = RelicResourceProbe.Definition(effect, 0, managers).SpawnStatusScaling!;
                    rules.Add(new { definition.Id, Rule = rule });
                    foreach (int count in new[] { -3, 0, 1, 3, 5, 17 })
                        foreach (Team.Type team in new[] { Team.Type.Monsters, Team.Type.Heroes })
                            foreach (bool excluded in new[] { false, true })
                            {
                                var subtypes = excluded ? new[] { OriginalFirstImpBonusScenario.ImpSubtype } : Array.Empty<string>();
                                Set(character, "subtypeKeys", subtypes.ToList()); Set(actor, "subtypes", character.GetSubtypes()); Set(actor, "teamType", team);
                                Set(character, "startingStatusEffects", StatusEffect("CardEffectAddStatusEffect", count).GetParamStatusEffects());
                                var trigger = new CharacterTriggerState(); trigger.Setup(Trigger(count, true), team, save);
                                Set(actor, "triggers", new List<CharacterTriggerState> { trigger });
                                var source = JToken.FromObject(trigger.GetEffects().Select(value => value.GetParamStatusEffectStackData()).ToArray());
                                for (int repeat = 0; repeat < 2; repeat++)
                                {
                                    requests = new List<object>(); intercepted = actor;
                                    var callback = effect.OnCharacterAdded(actor, CardState.None, managers.GetCoreManagers());
                                    if (callback.MoveNext()) throw new InvalidOperationException("Synchronous spawn scaling unexpectedly yielded.");
                                    intercepted = null;
                                    var runtime = trigger.GetEffects().Select(value => new { Type = value.GetCardEffect().GetType().Name, Statuses = value.GetParamStatusEffectStackData() }).ToArray();
                                    var capturedEnchant = EnchantmentCombatProbe.Capture(trigger.GetEffects()[1]);
                                    samples.Add(new { Definition = definition.Id, Count = count, Team = RelicPercentageStatProbe.TeamName(team), Subtypes = subtypes,
                                        Repeat = repeat, Requests = requests.ToArray(), Runtime = runtime, CapturedEnchant = capturedEnchant!.StatusPool,
                                        Source = source, SourceAfter = JToken.FromObject(trigger.GetEffects().Select(value => value.GetSourceCardEffectData().GetParamStatusEffects()).ToArray()) });
                                }
                            }
                }
            }
            finally { intercepted = null; requests = null; UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(character); }
            timer.Stop(); var after = JToken.FromObject(trace.CaptureDecision()); var originalAfter = JToken.FromObject(Describe()); var rngAfter = RngStates();
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(original, originalAfter) || !JToken.DeepEquals(rng, rngAfter) || frame != Time.frameCount || queue.Count != 0)
                throw new InvalidOperationException("Spawn scaling component batch changed source state.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, Scope = "DefinitionDrivenSpawnStatusScaling", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after, Original = original, OriginalAfter = originalAfter,
                RngBefore = rng, RngAfter = rngAfter, FrameBefore = frame, FrameAfter = Time.frameCount, QueueBefore = 0, QueueAfter = queue.Count,
                Seconds = timer.Elapsed.TotalSeconds, Definitions = rules, Samples = samples }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "spawn-status-scaling-batch.mt2f"))) document.Write(output);
        }
        [HarmonyPatch]
        private static class StatusRequests
        {
            private static System.Reflection.MethodBase TargetMethod() => typeof(CharacterState).GetMethods().Single(method => method.Name == "AddStatusEffect" && method.GetParameters().Length == 9);
            private static bool Prefix(CharacterState __instance, string statusId, int numStacks, CharacterState.AddStatusEffectParams addStatusEffectParams)
            {
                if (__instance != intercepted) return true;
                requests!.Add(new { Id = statusId, Count = numStacks, SpawnEffect = addStatusEffectParams.spawnEffect }); return false;
            }
        }
        private static JObject RngStates() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
    }
}
#endif
