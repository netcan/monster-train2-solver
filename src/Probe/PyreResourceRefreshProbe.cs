using System;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PyreResourceRefreshProbe
    {
        internal sealed class Operation
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Kind { get; set; } = "";
            public int? Amount { get; set; }
            public bool IsReward { get; set; }
            public bool Preview { get; set; }
            public bool Placement { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public int Notifications { get; set; }
            public int? CachedDamageBefore { get; set; }
            public int? CachedDamageAfter { get; set; }
        }

        private sealed class HookState
        {
            internal Operation Record = null!;
            internal Operation? Parent;
            internal FullBattleTrace Trace = null!;
        }

        internal static readonly List<Operation> Operations = new List<Operation>();
        private static Operation? current;
        private static bool capturing;

        private static bool Ready()
        {
            AllGameManagers? managers = AllGameManagers.Instance;
            return !capturing && PyreResourceRefreshScenario.Enabled && PyreResourceRefreshScenario.CaptureReady &&
                FullBattleTrace.Active != null && FullBattleTrace.Active.NativeWon == null && managers != null &&
                managers.GetSaveManager().GetGameSequence() == SaveData.GameSequence.InBattle;
        }

        // This private UI cache is diagnostic evidence only. Reading it does
        // not invoke a getter, dispatch a signal or notify a relic.
        private static int? CachedDamage()
        {
            PyreRoomState? room = AllGameManagers.Instance!.GetRoomManager()?.GetPyreRoom();
            return room == null ? null : (int?)AccessTools.Field(typeof(PyreRoomState), "cachedPyreDamage").GetValue(room);
        }

        private static HookState? Begin(string kind, int? amount, bool isReward)
        {
            if (!Ready()) return null;
            FullBattleTrace trace = FullBattleTrace.Active!;
            try
            {
                capturing = true;
                AllGameManagers managers = AllGameManagers.Instance!;
                var record = new Operation
                {
                    Id = Operations.Count + 1, ParentId = current?.Id, Kind = kind, Amount = amount,
                    IsReward = isReward, Preview = managers.GetSaveManager().PreviewMode,
                    Placement = managers.GetCombatManager()!.IsPlacementPhase,
                    Before = trace.CaptureTrain(), CachedDamageBefore = CachedDamage()
                };
                var state = new HookState { Record = record, Parent = current, Trace = trace };
                Operations.Add(record);
                current = record;
                return state;
            }
            catch (Exception error) { trace.CaptureFailure(error); return null; }
            finally { capturing = false; }
        }

        private static void Complete(HookState? state)
        {
            if (state == null) return;
            try
            {
                capturing = true;
                state.Record.After = state.Trace.CaptureTrain();
                state.Record.CachedDamageAfter = CachedDamage();
            }
            catch (Exception error) { state.Trace.CaptureFailure(error); }
            finally
            {
                current = state.Parent;
                capturing = false;
            }
        }

        // AdjustGold is synchronous. Its zero-amount body is still observed;
        // native code itself decides whether to call SetGold and its signals.
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.AdjustGold), new[] { typeof(int), typeof(bool), typeof(bool) })]
        private static class GoldPatch
        {
            private static void Prefix(SaveManager __instance, int amount, bool isReward, bool applyTrialDataModifiers,
                out HookState? __state)
            {
                __state = null;
                if (!Ready() || !ReferenceEquals(__instance, AllGameManagers.Instance!.GetSaveManager())) return;
                __state = Begin("Gold", amount, isReward);
                if (__state != null && applyTrialDataModifiers)
                    __state.Trace.CaptureFailure(new InvalidOperationException("Unmodeled trial modifiers in a Pyre refresh gold operation."));
            }
            private static void Postfix(HookState? __state) => Complete(__state);
        }

        // This handler calls GetModifiedPyreDamage(notifyRelicsTriggered:true)
        // and writes the live raw attack before SetGold's later display signal.
        [HarmonyPatch(typeof(PyreRoomState), "HandleGoldChanged")]
        private static class RefreshPatch
        {
            private static void Prefix(PyreRoomState __instance, out HookState? __state)
            {
                __state = null;
                if (!Ready() || !ReferenceEquals(__instance, AllGameManagers.Instance!.GetRoomManager()?.GetPyreRoom())) return;
                __state = Begin("PyreRefresh", null, current?.IsReward ?? false);
            }
            private static void Postfix(HookState? __state) => Complete(__state);
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotificationPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            {
                if (!capturing && current?.Kind == "PyreRefresh" && triggeredEffect is RelicEffectIncreasePyreAttack)
                    current.Notifications++;
            }
        }
    }
}
