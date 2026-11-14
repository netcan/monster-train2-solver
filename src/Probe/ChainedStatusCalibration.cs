using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ObfuscatedNumber = ShinyShoe.ObfuscatedNumber;

namespace MonsterTrain2Poju.Probe
{
    internal static class ChainedStatusCalibration
    {
        private static bool intercept;
        private static Request? request;
        internal sealed class Request
        {
            public int Damage { get; set; }
            public int TargetId { get; set; }
            public int AttackerId { get; set; }
            public int DamageType { get; set; }
            public bool NullCard { get; set; }
            public bool NullRelic { get; set; }
        }
        internal static object Run(AllGameManagers managers)
        {
            var trace = FullBattleTrace.Active!;
            var player = managers.GetMonsterManager().GetCharactersReadOnly().First(actor => actor.IsAlive && !actor.IsPyreHeart());
            var enemy = managers.GetHeroManager().GetCharactersReadOnly().First(actor => actor.IsAlive);
            CharacterState[] actors = { player, enemy };
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object[] originals = actors.Select(primary.GetValue).ToArray();
            TrainCombatState before = trace.CaptureTrain(); object rng = HalfHealthLifecycleCalibration.Rng();
            var tests = new List<object>(); var requests = new List<object>();
            try
            {
                foreach (int type in Enumerable.Range(0, 13))
                foreach (int damage in new[] { -1, 0, 3 }) Test(1, 0, 10, 0, 1, damage, type);
                foreach (int holder in new[] { 0, 1 })
                foreach (int hp in new[] { 0, 10 })
                foreach (int undying in new[] { 0, 1 })
                foreach (int stacks in new[] { 0, 1, 3 })
                foreach (int attacked in new[] { -1, 0, 1 }) Test(holder, attacked, hp, undying, stacks, 3, 0);
                foreach (int damage in new[] { int.MinValue, -1, 0, 1, 2, 3, int.MaxValue })
                foreach (int stacks in new[] { -1, 0, 1, 2, int.MaxValue })
                {
                    StatusEffectState status = Shadow(1, 10, 0, stacks);
                    var input = new StatusEffectState.InputTriggerParams { attacked = player, damage = damage, damageType = Damage.Type.Default };
                    int observed = enemy.GetStatusEffectStacks("chained"); request = null; intercept = true;
                    try { Drain((IEnumerator)AccessTools.Method(status.GetType(), "OnTriggered").Invoke(status,
                        new object[] { input, new StatusEffectState.OutputTriggerParams(), managers.GetCoreManagers() })); }
                    finally { intercept = false; }
                    int magnitude = status.GetEffectMagnitude(stacks);
                    string? error = request == null ? "Missing native secondary-damage request." :
                        request.Damage != ChainedStatusModel.Damage(damage, observed) || request.TargetId != trace.UnitId(enemy) ||
                        request.AttackerId != trace.UnitId(player) || request.DamageType != ChainedStatusModel.DamageType ||
                        !request.NullCard || !request.NullRelic || magnitude != ChainedStatusModel.Magnitude(stacks) ? "Native request differs." : null;
                    requests.Add(new { Damage = damage, Stacks = stacks, ObservedStacks = observed, Magnitude = magnitude,
                        Request = request, Difference = error, Completed = true });
                    if (error != null) throw new InvalidOperationException(error);
                }
            }
            finally
            {
                intercept = false; request = null;
                for (int i = 0; i < actors.Length; i++) primary.SetValue(actors[i], originals[i]);
            }
            TrainCombatState after = trace.CaptureTrain(); object rngAfter = HalfHealthLifecycleCalibration.Rng();
            string? restoration = HalfHealthLifecycleCalibration.Difference(before, after) ?? HalfHealthLifecycleCalibration.Difference(rng, rngAfter);
            if (restoration != null) throw new InvalidOperationException("Chained restoration: " + restoration);
            return new { Completed = true, LiveContextUnchanged = true, RestorationDifference = restoration,
                PlayerId = trace.UnitId(player), EnemyId = trace.UnitId(enemy), Before = before, Actual = after, RngBefore = rng,
                RngAfter = rngAfter, Tests = tests, Requests = requests, Boundary = "NativeAdmissionAndInterceptedRawDamageRequests" };

            StatusEffectState Shadow(int holder, int hp, int undying, int stacks)
            {
                for (int i = 0; i < actors.Length; i++) primary.SetValue(actors[i], originals[i]);
                var actor = actors[holder]; object live = originals[holder];
                object shadow = AccessTools.Method(live.GetType(), "DeepCopyForPreview").Invoke(live, null);
                primary.SetValue(actor, shadow); AccessTools.Field(shadow.GetType(), "hp").SetValue(shadow, new ObfuscatedNumber(hp));
                StatusEffectState status = managers.GetStatusEffectManager().Create("chained", actor)!;
                var statuses = new Dictionary<string, CharacterState.StatusEffectStack> {
                    ["chained"] = new CharacterState.StatusEffectStack(status, stacks) };
                if (undying > 0) statuses.Add("undying", new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create("undying", actor)!, undying));
                AccessTools.Field(shadow.GetType(), "statusEffects").SetValue(shadow, statuses);
                return status;
            }
            void Test(int holder, int attacked, int hp, int undying, int stacks, int damage, int type)
            {
                StatusEffectState status = Shadow(holder, hp, undying, stacks);
                var input = new StatusEffectState.InputTriggerParams { attacked = attacked < 0 ? null! : actors[attacked],
                    damage = damage, damageType = (Damage.Type)type };
                bool native = status.TestTrigger(input, new StatusEffectState.OutputTriggerParams(), managers.GetCoreManagers());
                bool dead = actors[holder].IsDeadAndUnrevivable;
                bool model = ChainedStatusModel.Test(holder == 0 ? CombatTeam.Player : CombatTeam.Enemy, dead, stacks,
                    attacked < 0 ? (CombatTeam?)null : attacked == 0 ? CombatTeam.Player : CombatTeam.Enemy, damage, type);
                tests.Add(new { Holder = holder, Attacked = attacked, Health = hp, Undying = undying, Stacks = stacks, Damage = damage,
                    DamageType = type, DeadAndUnrevivable = dead, NativeTest = native, Completed = true });
                if (model != native) throw new InvalidOperationException("Native chained admission differs.");
            }
        }
        private static void Drain(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root); int steps = 0;
            try
            {
                while (stack.Count > 0)
                {
                    if (++steps > 32) throw new InvalidOperationException("Unexpected native chained coroutine depth.");
                    IEnumerator current = stack.Peek();
                    if (!current.MoveNext()) { (current as IDisposable)?.Dispose(); stack.Pop(); }
                    else if (current.Current is IEnumerator child) stack.Push(child);
                    else throw new InvalidOperationException("Unexpected native chained yield before damage request interception.");
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }
        private static IEnumerator Empty() { yield break; }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class RequestPatch
        {
            private static bool Prefix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters, ref IEnumerator __result)
            {
                if (!intercept) return true;
                if (request != null) throw new InvalidOperationException("Repeated native chained request.");
                var trace = FullBattleTrace.Active!;
                request = new Request { Damage = damage, TargetId = trace.UnitId(target),
                    AttackerId = parameters.selfTarget == null ? 0 : trace.UnitId(parameters.selfTarget), DamageType = (int)parameters.damageType,
                    NullCard = parameters.playedCard == null, NullRelic = parameters.relicState == null };
                __result = Empty(); return false;
            }
        }
    }
}
