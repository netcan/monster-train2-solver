using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomCapacityProbe
    {
        internal static RoomCapacityState[] Capture(RoomManager rooms) => Enumerable.Range(0, rooms.GetNumRooms()).Select(index =>
        {
            RoomState room = rooms.GetRoom(index);
            return new RoomCapacityState(index, room.GetCapacityInfo(Team.Type.Monsters).max,
                room.GetCapacityInfo(Team.Type.Heroes).max, room.GetIsPyreRoom());
        }).ToArray();
        internal static ScalingCapacityTrait[]? Traits(CardState card)
        {
            ScalingCapacityTrait[] traits = card.GetTraitStates().OfType<CardTraitScalingAdjustCapacity>().Select(trait =>
                new ScalingCapacityTrait(DamageScalingProbe.Query(trait.StatValueData,
                    (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card), card.IsConsumeRemainingEnergyCostType()), trait.GetParamInt())).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static ScalingCapacityTrait[]? Creation(CardData card)
        {
            ScalingCapacityTrait[] traits = card.GetTraits().Where(data => data.GetTraitStateName() == "CardTraitScalingAdjustCapacity").Select(data =>
            {
                var trait = new CardTraitScalingAdjustCapacity(); trait.Setup(data, CardState.None);
                return new ScalingCapacityTrait(DamageScalingProbe.Query(trait.StatValueData, card.GetCost(),
                    card.GetCostType() == CardData.CostType.ConsumeRemainingEnergy), trait.GetParamInt());
            }).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal sealed class Attempt
        {
            public int Maximum { get; set; }
            public int Requested { get; set; }
            public int Approved { get; set; }
            public bool Allowed { get; set; }
            public string Error { get; set; } = "";
        }
        internal sealed class Record
        {
            public int SourceCardId { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
            public List<Attempt> Attempts { get; } = new List<Attempt>();
        }
        internal sealed class TestRecord
        {
            public RoomCombatState Before { get; set; } = null!;
            public CardActionEffect Effect { get; set; } = null!;
            public bool Actual { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        private static Record? current;
        [HarmonyPatch(typeof(CardEffectAdjustRoomCapacity), nameof(CardEffectAdjustRoomCapacity.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState, cardEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? parent = current;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(parameters.selectedRoom);
            CardState? owner = effect.GetParentCardState();
            var record = new Record { SourceCardId = owner == null ? 0 : trace.CardId(owner),
                Effect = UnitTriggerActionProbe.Capture(effect) ?? throw new NotSupportedException("Uncaptured room-capacity effect."), Before = CaptureAllUnits(trace, room) };
            Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); current = parent; record.After = CaptureAllUnits(trace, room); }
        }
        private static RoomCombatState CaptureAllUnits(FullBattleTrace trace, RoomState room)
        {
            RoomCombatState captured = trace.Capture(room); var units = new List<CharacterState>();
            room.AddCharactersToList(units, Team.Type.Heroes); room.AddCharactersToList(units, Team.Type.Monsters);
            return new RoomCombatState(captured.RoomIndex, captured.Deployment, units.Where(unit => !unit.IsDestroyed)
                .Select(unit => trace.CaptureUnit(unit)).ToArray(), captured.ExternalInteractions, captured.Context, captured.Preview);
        }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.CanAdjustCapacity))]
        private static class BoundsPatch
        {
            private static void Prefix(RoomState __instance, Team.Type team, int adjustAmount, out Attempt? __state)
            {
                __state = current == null ? null : new Attempt { Maximum = __instance.GetCapacityInfo(team).max, Requested = adjustAmount };
            }
            private static void Postfix(int adjustAmount, SpawnPointGroup.CapacityAdjustmentError error, bool __result, Attempt? __state)
            { if (__state != null && current != null) { __state.Approved = adjustAmount; __state.Allowed = __result; __state.Error = error.ToString(); current.Attempts.Add(__state); } }
        }
        [HarmonyPatch(typeof(CardEffectAdjustRoomCapacity), nameof(CardEffectAdjustRoomCapacity.TestEffect))]
        private static class TestPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, bool __result)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (trace == null || trace.NativeWon != null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                Tests.Add(new TestRecord { Before = CaptureAllUnits(trace, AllGameManagers.Instance!.GetRoomManager()!.GetRoom(cardEffectParams.selectedRoom)),
                    Effect = UnitTriggerActionProbe.Capture(cardEffectState) ?? throw new NotSupportedException("Uncaptured room-capacity test."), Actual = __result });
            }
        }
    }
}
