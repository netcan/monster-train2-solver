using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DeathSignalProbe
    {
        private sealed class Invocation
        {
            internal CharacterState Actor = null!;
            internal CardState? Source;
            internal bool StatisticsStarted;
        }
        private static readonly List<Invocation> Invocations = new List<Invocation>();
        internal static UnitDeathState Capture(CharacterState actor)
        {
            object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(actor);
            object signal = AccessTools.Field(state.GetType(), "deathSignal").GetValue(state);
            bool Contains(string name) => (AccessTools.Field(signal.GetType(), name).GetValue(signal) as Delegate)?.GetInvocationList()
                .Any(callback => callback.Method.DeclaringType == typeof(CardStatistics) && callback.Method.Name == nameof(CardStatistics.OnCharacterDeath)) == true;
            bool permanent = Contains("Listener"), once = Contains("OnceListener"), listener = permanent || once;
            Invocation? pending = !listener ? null : Invocations.LastOrDefault(item => item.Actor == actor && !item.StatisticsStarted);
            return new UnitDeathState(actor.HasFinishedDying, actor.IsBeingRemoved(), listener,
                pending == null ? null : pending.Source == null ? 0 : FullBattleTrace.Active!.CardId(pending.Source),
                actor.IsSacrifice, once && !permanent,
                HordeMergeScenario.Enabled || BumpScenario.Enabled || EnchantmentBattleScenario.Prepared ? (bool)AccessTools.Property(typeof(CharacterState), "FiredDespawnEvent").GetValue(actor) : (bool?)null,
                HordeMergeScenario.Enabled || BumpScenario.Enabled || EnchantmentBattleScenario.Prepared ? actor.IsDestroyed : (bool?)null);
        }
        [HarmonyPatch(typeof(CharacterState), "CheckForDeath")]
        private static class CheckPatch
        {
            private static void Postfix(CharacterState __instance, CardState damageSourceCard, ref IEnumerator __result)
            {
                if (FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, __instance, damageSourceCard);
            }
            private static IEnumerator Observe(IEnumerator native, CharacterState actor, CardState? source)
            {
                var invocation = new Invocation { Actor = actor, Source = source }; Invocations.Add(invocation);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); Invocations.Remove(invocation); }
            }
        }
        [HarmonyPatch(typeof(CardStatistics), nameof(CardStatistics.OnCharacterDeath))]
        private static class StatisticsPatch
        {
            private static void Postfix(CharacterDeathParams deathParams, ref IEnumerator __result)
            { if (FullBattleTrace.Active != null && deathParams?.deadCharacter != null) __result = Observe(__result, deathParams.deadCharacter); }
            private static IEnumerator Observe(IEnumerator native, CharacterState actor)
            {
                Invocation? pending = Invocations.LastOrDefault(item => item.Actor == actor && !item.StatisticsStarted);
                if (pending != null) pending.StatisticsStarted = true;
                while (native.MoveNext()) yield return native.Current;
                (native as IDisposable)?.Dispose();
            }
        }
    }
}
