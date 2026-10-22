using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Observe original UI previews without changing preview settings or timing.
    internal static class TrueFinalBossPreviewProbe
    {
        internal sealed class Record
        {
            public int Turn { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
        }
        internal sealed class RoomRecord
        {
            public int Turn { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<RoomRecord> Rooms = new List<RoomRecord>();
        private static Record? active;
        private static bool waitingForPrimaryOrder;
        private static bool Enabled => TrueFinalBossTurnProbe.Enabled && FullBattleTrace.Active != null &&
            AllGameManagers.Instance?.GetHeroManager().GetOuterTrainBossCharacter()?.IsTrueFinalBoss() == true;

        private static TrainCombatState CapturePrimary()
        {
            TrainCombatState state = FullBattleTrace.Active!.CaptureCanonicalTrain();
            CombatContext context = state.Context!;
            RoomCombatState Primary(RoomCombatState room) => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, null, false, room.ActiveCompanionBossIds);
            EnchantmentWorld world = context.Enchantments!;
            context = context.WithEnchantments(new EnchantmentWorld(world.Rooms.Select(Primary).ToArray(), world.Movement,
                world.EnemySlotsPerRoom, world.RetainedUnits, world.EnchanterIds, world.AllowUpdates, world.Updating,
                false, world.TestRng, world.AutomaticLifecycle));
            return new TrainCombatState(state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, context, false, room.ActiveCompanionBossIds)).ToArray(),
                state.Movement, state.EnemySlotsPerRoom, context);
        }

        private static RoomCombatState CaptureRoom(RoomState room)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            AllGameManagers managers = AllGameManagers.Instance;
            RoomCombatState state = trace.Capture(room);
            CombatManager combat = managers.GetCombatManager();
            var links = new LifeLinkContext(managers.GetHeroManager().GetCharactersReadOnly()
                .Where(actor => actor.IsTrueFinalBoss() && !actor.IsDestroyed).Select(actor => new LifeLinkActor(trace.UnitId(actor),
                    actor.GetLinkedCharacterState() == null ? 0 : trace.UnitId(actor.GetLinkedCharacterState()),
                    actor.GetHP(), actor.PreviewHP(), actor.Initialized, actor.HasStatusEffect("lifelink"), true,
                    actor.IsOuterTrainBoss())).ToArray(), combat.CombatPreviewState == CharacterState.CombatPreviewState.Calculating,
                combat.TrueFinalBossBattlePreviewHpChange);
            return new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions,
                state.Context!.WithLifeLinks(links), state.Preview, state.ActiveCompanionBossIds);
        }

        [HarmonyPatch(typeof(CombatManager), "SetCharacterPreviewState")]
        private static class StatePatch
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(CharacterState.CombatPreviewState previewState)
            {
                if (!Enabled) return;
                try
                {
                    if (previewState == CharacterState.CombatPreviewState.Calculating)
                    {
                        if (active != null) active.Actual = CapturePrimary();
                        active = new Record { Turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount(), Before = CapturePrimary() };
                        Records.Add(active);
                    }
                    else if (previewState == CharacterState.CombatPreviewState.Off && active != null)
                    { active.Actual = CapturePrimary(); active = null; }
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(CharacterState.CombatPreviewState previewState)
            { if (Enabled && previewState == CharacterState.CombatPreviewState.On && active != null) waitingForPrimaryOrder = true; }
        }

        private static IEnumerator CompleteAfterOrder(IEnumerator native, Record record)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try { record.Actual = CapturePrimary(); if (active == record) active = null; }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(RoomManager), nameof(RoomManager.HandleRoomUnitOrderPossiblyChanged))]
        private static class PrimaryOrderPatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ref IEnumerator __result)
            {
                if (!Enabled || !waitingForPrimaryOrder || active == null || AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                waitingForPrimaryOrder = false;
                __result = CompleteAfterOrder(__result, active);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, RoomState room)
        {
            RoomRecord? record = null;
            if (Enabled && AllGameManagers.Instance.GetSaveManager().PreviewMode)
                try
                {
                    record = new RoomRecord { Turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount(), Before = CaptureRoom(room) };
                    Rooms.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                    try { record.Actual = CaptureRoom(room); }
                    catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DoRoomCombat))]
        private static class RoomPatch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            { if (Enabled) __result = Wrap(__result, room); }
        }
    }
}
