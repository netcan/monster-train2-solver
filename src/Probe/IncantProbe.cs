using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class IncantProbe
    {
        internal sealed class Phase
        {
            public int Sequence { get; set; }
            public CombatTeam Team { get; set; }
            public int CardId { get; set; }
            public string CardType { get; set; } = "";
            public bool IsAnyAbility { get; set; }
            public int ActivatorUnitId { get; set; }
            public CardPlayedQueueEntry[]? PrecedingCallbacks { get; set; }
            public int[] CachedUnitIds { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
            internal bool Unsupported { get; set; }
        }
        internal sealed class Trigger
        {
            public int Sequence { get; set; }
            public int ParamInt { get; set; }
            public int TriggerCount { get; set; }
            public bool CanFire { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public CombatUnit Actor { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
            internal bool Unsupported { get; set; }
        }
        internal static readonly List<Phase> Phases = new List<Phase>();
        internal static readonly List<Trigger> Triggers = new List<Trigger>();
        internal static int Mismatches => Phases.Count(record => record.Difference != null) + Triggers.Count(record => record.Difference != null);
        internal static int Unsupported => Phases.Count(record => record.Unsupported) + Triggers.Count(record => record.Unsupported);
        internal static int Pending => Phases.Count(record => !record.Completed || record.After == null) +
            Triggers.Count(record => !record.Completed || record.After == null || record.AfterActor == null);
        private static bool Enabled => (IncantScenario.Prepared || AbilityIncantScenario.Prepared) && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator ObservePhase(IEnumerator native, ICharacterManager manager, CardState card, CharacterState? activator)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var units = new List<CharacterState>(); RoomManager rooms = AllGameManagers.Instance!.GetRoomManager()!;
            for (int room = 0; room < rooms.GetNumRooms(); room++) rooms.GetRoom(room).AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
            var record = new Phase { Sequence = trace.NextPhaseSequence(), Team = manager is MonsterManager ? CombatTeam.Player : CombatTeam.Enemy,
                CardId = trace.CardId(card), CardType = card.GetCardType().ToString(), IsAnyAbility = card.IsAnyAbility(),
                ActivatorUnitId = activator == null ? 0 : trace.UnitId(activator),
                CachedUnitIds = units.Where(card.CharacterInRoomAtTimeOfCardPlay).Select(trace.UnitId).ToArray(), Before = trace.CaptureTrain() };
            if (AbilityIncantScenario.Prepared) record.PrecedingCallbacks = CaptureQueue(trace);
            Phases.Add(record);
            TrainCombatResult? predicted = record.CardType == "Spell" && !record.IsAnyAbility
                ? CardPlayedTriggerModel.Spell(record.Before, record.Team, record.CachedUnitIds,
                    record.PrecedingCallbacks?.Select(entry => entry.ToQueued()).ToArray()) : record.IsAnyAbility
                ? CardPlayedTriggerModel.Ability(record.Before, record.Team, record.CachedUnitIds, record.ActivatorUnitId,
                    record.PrecedingCallbacks?.Select(entry => entry.ToQueued()).ToArray()) : null;
            record.Unsupported = predicted?.Supported == false;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    record.After = trace.CaptureTrain();
                    record.Difference = predicted?.Supported == false ? predicted.UnsupportedReason :
                        JToken.DeepEquals(JToken.FromObject(predicted?.State ?? record.Before), JToken.FromObject(record.After)) ? null : "Incant team phase differs";
                }
                catch (Exception error) { trace.CaptureFailure(error); }
            }
        }
        private static CardPlayedQueueEntry[] CaptureQueue(FullBattleTrace trace)
        {
            var manager = AllGameManagers.Instance!.GetCombatManager()!;
            var queue = ((IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(manager))
                .Cast<CombatManager.TriggerQueueData>().ToArray();
            return queue.Select(entry => {
                if (entry.exclusiveTrigger != null || entry.trigger is CharacterTriggerData.Trigger.PreCombat or CharacterTriggerData.Trigger.OnDeath)
                    throw new InvalidOperationException("Unmodeled exclusive/attack/death prefix in card-play phase.");
                var data = entry.fireTriggersData;
                return new CardPlayedQueueEntry(entry.character.GetCurrentRoomIndex(), trace.CaptureUnit(entry.character), entry.trigger.ToString(),
                    data?.paramInt ?? 0, data?.paramInt2 ?? 0, data?.paramString,
                    data?.overrideTargetCharacter == null ? null : trace.CaptureUnit(data.overrideTargetCharacter),
                    entry.dyingCharacter == null ? null : trace.CaptureUnit(entry.dyingCharacter), entry.canFireTriggers, entry.triggerCount);
            }).ToArray();
        }
        private static IEnumerator ObserveTrigger(IEnumerator native, CharacterState actor, CharacterState.FireTriggersData? data, bool canFire, int count)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = actor.GetCurrentRoom(allowLastKnownRoom: true) ?? AllGameManagers.Instance!.GetRoomManager()!.GetRoom(0);
            var record = new Trigger { Sequence = trace.NextPhaseSequence(), Before = trace.Capture(room), Actor = trace.CaptureUnit(actor),
                ParamInt = data?.paramInt ?? 0, TriggerCount = count, CanFire = canFire };
            Triggers.Add(record);
            var queued = new RoomCombatModel.QueuedCharacterTrigger(room.GetRoomIndex(), record.Actor, "CardSpellPlayed",
                paramInt: record.ParamInt, canFireTriggers: canFire, triggerCount: count,
                admission: RoomCombatModel.CharacterTriggerAdmission.Accepted);
            RoomCombatResult predicted = RoomCombatModel.ApplyQueuedCharacterTrigger(record.Before, queued, _ => { });
            record.Unsupported = !predicted.Supported;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    record.After = trace.Capture(room); record.AfterActor = trace.CaptureUnit(actor);
                    record.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                        JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) &&
                        JToken.DeepEquals(JToken.FromObject(queued.Unit), JToken.FromObject(record.AfterActor)) ? null : "Incant actor/room phase differs";
                }
                catch (Exception error) { trace.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.FireUnitTriggersForCardPlayed))]
        private static class PhasePatch
        {
            private static void Postfix(ICharacterManager characterManager, CardState playedCard, CharacterState characterThatActivatedAbility, ref IEnumerator __result)
            { if (Enabled && playedCard.GetCardType() == CardType.Spell) __result = ObservePhase(__result, characterManager, playedCard, characterThatActivatedAbility); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class TriggerPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState.FireTriggersData fireTriggersData,
                bool canFireTriggers, bool fromRunningTriggerQueue, int triggerCount, ref IEnumerator __result)
            { if (Enabled && fromRunningTriggerQueue && trigger == CharacterTriggerData.Trigger.CardSpellPlayed &&
                    __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    __result = ObserveTrigger(__result, __instance, fireTriggersData, canFireTriggers, triggerCount); }
        }
    }
}
