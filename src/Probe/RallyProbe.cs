using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RallyProbe
    {
        internal sealed class Phase
        {
            public string Label { get; set; } = "";
            public CombatTeam Team { get; set; }
            public int CardId { get; set; }
            public int[] CachedUnitIds { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        internal sealed class Trigger
        {
            public string Label { get; set; } = "";
            public int TriggerCount { get; set; }
            public int ParamInt { get; set; }
            public bool CanFire { get; set; }
            public int LastSpawnedOverrideUnitId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public CombatUnit Actor { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Phase> Phases = new List<Phase>();
        internal static readonly List<Trigger> Triggers = new List<Trigger>();
        private static bool Enabled => (RallyScenario.Started || LethalRallyScenario.Prepared) && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator ObservePhase(IEnumerator native, ICharacterManager manager, CardState card)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; var units = new List<CharacterState>();
            RoomManager rooms = AllGameManagers.Instance!.GetRoomManager()!;
            for (int room = 0; room < rooms.GetNumRooms(); room++) rooms.GetRoom(room).AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
            var record = new Phase { Label = RallyScenario.Label ?? "natural", Team = manager is MonsterManager ? CombatTeam.Player : CombatTeam.Enemy,
                CardId = trace.CardId(card), CachedUnitIds = units.Where(card.CharacterInRoomAtTimeOfCardPlay).Select(trace.UnitId).ToArray(), Before = trace.CaptureTrain() };
            Phases.Add(record); TrainCombatResult predicted = CardPlayedTriggerModel.Rally(record.Before, record.Team, record.CachedUnitIds);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.After = trace.CaptureTrain();
                record.Difference = !predicted.Supported ? predicted.UnsupportedReason : JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) ? null : "Rally team phase differs";
            }
        }
        private static IEnumerator ObserveTrigger(IEnumerator native, CharacterState actor, CharacterState.FireTriggersData? data, bool canFire, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; RoomState room = actor.GetCurrentRoom();
            var record = new Trigger { Label = RallyScenario.Label ?? "natural", Before = trace.Capture(room), Actor = trace.CaptureUnit(actor), CanFire = canFire,
                TriggerCount = count, ParamInt = data?.paramInt ?? 0, LastSpawnedOverrideUnitId = data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter) };
            Triggers.Add(record);
            var queued = new RoomCombatModel.QueuedCharacterTrigger(room.GetRoomIndex(), record.Actor, "CardMonsterPlayed", paramInt: record.ParamInt,
                canFireTriggers: canFire, triggerCount: count, lastSpawnedOverrideUnitId: record.LastSpawnedOverrideUnitId);
            RoomCombatResult predicted = RoomCombatModel.ApplyQueuedCharacterTrigger(record.Before, queued, _ => { });
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.After = trace.Capture(room); record.AfterActor = trace.CaptureUnit(actor);
                record.Difference = !predicted.Supported ? predicted.UnsupportedReason : JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) &&
                    JToken.DeepEquals(JToken.FromObject(queued.Unit), JToken.FromObject(record.AfterActor)) ? null : "Rally actor/room phase differs";
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.FireUnitTriggersForCardPlayed))]
        private static class PhasePatch
        {
            private static void Postfix(ICharacterManager characterManager, CardState playedCard, ref IEnumerator __result)
            { if (Enabled && playedCard.GetCardType() == CardType.Monster && playedCard.GetSpawnCharacterData() != null) __result = ObservePhase(__result, characterManager, playedCard); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class TriggerPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState.FireTriggersData fireTriggersData,
                bool canFireTriggers, bool fromRunningTriggerQueue, int triggerCount, ref IEnumerator __result)
            { if (Enabled && fromRunningTriggerQueue && trigger == CharacterTriggerData.Trigger.CardMonsterPlayed && __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                __result = ObserveTrigger(__result, __instance, fireTriggersData, canFireTriggers, triggerCount); }
        }
    }
}
