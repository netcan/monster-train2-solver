using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class EnchanterRemovalProbe
    {
        private static ManualLogSource? log;
        private static readonly Type EffectType = typeof(CardEffectEnchant);
        private static readonly HashSet<int> trackedInstances = new HashSet<int>();
        private static readonly HashSet<int> mappedInstances = new HashSet<int>();

        internal static void Configure(ManualLogSource logger) => log = logger;

        internal static void TrackSource(int instanceId, int unitId)
        {
            if (!Enabled || instanceId == 0) return;
            trackedInstances.Add(instanceId);
            if (unitId > 0 && mappedInstances.Add(instanceId))
                log?.LogInfo("AURA-SOURCE-MAPPED instance=" + instanceId + " unit=" + unitId);
        }

        internal static void MapUnit(int instanceId, int unitId)
        {
            if (Enabled && trackedInstances.Contains(instanceId) && mappedInstances.Add(instanceId))
                log?.LogInfo("AURA-SOURCE-MAPPED instance=" + instanceId + " unit=" + unitId);
        }

        private static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ENCHANTER_REMOVAL_TRACE") == "1";

        private readonly struct Observation
        {
            internal readonly bool Enabled;
            internal readonly bool Preview;
            internal readonly int SourceUnitId;
            internal readonly int SourceInstance;
            internal readonly string EffectOwners;
            internal readonly bool SourcePreviewBorn;
            internal readonly bool SourceDestroyed;
            internal readonly string Status;
            internal readonly uint[] Battle;
            internal readonly uint[] Test;
            internal readonly uint[] Unity;
            internal readonly string Caller;

            internal Observation(bool preview, int sourceUnitId, int sourceInstance, string effectOwners, bool sourcePreviewBorn, bool sourceDestroyed,
                string status, uint[] battle, uint[] test, uint[] unity, string caller)
            {
                Enabled = true;
                Preview = preview;
                SourceUnitId = sourceUnitId;
                SourceInstance = sourceInstance;
                EffectOwners = effectOwners;
                SourcePreviewBorn = sourcePreviewBorn;
                SourceDestroyed = sourceDestroyed;
                Status = status;
                Battle = battle;
                Test = test;
                Unity = unity;
                Caller = caller;
            }

            internal string Format() => $"preview={Preview} sourceUnit={SourceUnitId} sourceInstance={SourceInstance} owners={EffectOwners} sourcePreview={SourcePreviewBorn} " +
                $"sourceDestroyed={SourceDestroyed} status={Status} battle={string.Join(",", Battle)} " +
                $"test={string.Join(",", Test)} unity={string.Join(",", Unity)} caller={Caller}";
        }

        [HarmonyPatch]
        private static class ApplyEffectInternalPatch
        {
            private static System.Reflection.MethodBase? TargetMethod() =>
                AccessTools.Method(EffectType, "ApplyEffectInternal");

            private static void Prefix(CardEffectEnchant __instance, out Observation __state)
            {
                __state = default;
                if (!Enabled) return;
                try { __state = Capture(__instance); }
                catch (Exception error) { log?.LogWarning("AURA-RECOMPUTE capture failed: " + error.Message); }
            }

            private static void Postfix(CardEffectEnchant __instance, Observation __state)
            {
                if (!__state.Enabled) return;
                try
                {
                    Observation after = Capture(__instance);
                    if (__state.Preview || after.Preview || __state.SourcePreviewBorn || after.SourcePreviewBorn ||
                        __state.Status != after.Status || !__state.Battle.SequenceEqual(after.Battle) ||
                        !__state.Test.SequenceEqual(after.Test) || !__state.Unity.SequenceEqual(after.Unity))
                    {
                        log?.LogInfo("AURA-RECOMPUTE before{" + __state.Format() + "} after{" + after.Format() + "}");
                    }
                }
                catch (Exception error) { log?.LogWarning("AURA-RECOMPUTE after capture failed: " + error.Message); }
            }

            private static Observation Capture(CardEffectEnchant effect)
            {
                var source = (CharacterState?)AccessTools.Field(EffectType, "enchanterCharacter").GetValue(effect);
                int sourceInstance = source == null ? 0 : source.GetInstanceID();
                int sourceUnitId = source != null && FullBattleTrace.Active?.TryGetUnitId(source, out int knownId) == true ? knownId : 0;
                TrackSource(sourceInstance, sourceUnitId);
                string effectOwners = Owners(effect);
                bool sourcePreviewBorn = false;
                bool sourceDestroyed = false;
                if (source != null)
                {
                    using (new CharacterState.SetAllowDestroyedAccessHelper(source, true))
                    {
                        sourcePreviewBorn = source.SpawnedInPreviewMode;
                        sourceDestroyed = source.IsDestroyed;
                    }
                }
                var cached = (StatusEffectStackData?)AccessTools.Field(EffectType, "statusEffect").GetValue(effect);
                string status = cached == null ? "none" : cached.statusId + ":" + cached.count;
                bool preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode;
                uint[] battle = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
                uint[] test = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest));
                uint[] unity = RngCalibration.Words(UnityEngine.Random.state);
                string caller = string.Join(">", new StackTrace(2, false).GetFrames().Take(5)
                    .Select(frame => frame.GetMethod()?.DeclaringType?.Name + "." + frame.GetMethod()?.Name));
                return new Observation(preview, sourceUnitId, sourceInstance, effectOwners, sourcePreviewBorn, sourceDestroyed,
                    status, battle, test, unity, caller);
            }

            private static string Owners(CardEffectEnchant effect)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (trace == null) return string.Empty;
                var owners = new List<int>();
                foreach (CharacterState unit in trace.KnownUnits)
                {
                    try
                    {
                        using (new CharacterState.SetAllowDestroyedAccessHelper(unit, true))
                            foreach (CharacterTriggerState trigger in unit.GetTriggers())
                                foreach (CardEffectState state in trigger.GetEffectStates())
                                    if (ReferenceEquals(state.GetCardEffect(), effect) && trace.TryGetUnitId(unit, out int id)) owners.Add(id);
                    }
                    catch { }
                }
                return string.Join(",", owners.Distinct().OrderBy(id => id));
            }
        }
    }
}
