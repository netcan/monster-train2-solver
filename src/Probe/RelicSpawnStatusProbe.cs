using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicSpawnStatusProbe
    {
        internal sealed class Record
        {
            public int UnitId { get; set; }
            public int FromCardId { get; set; }
            public bool OnlyCovenants { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public TrainCombatState? BeforeTrain { get; set; }
            public TrainCombatState? AfterTrain { get; set; }
            public bool DeferCallbacks { get; set; } = true;
            public bool QueueRunningBefore { get; set; }
            public bool StandaloneScenarioActive { get; set; }
            public bool ReplayCardPlayingBefore { get; set; }
            public string? SettlementProtocol { get; set; }
            public List<UnitCloneCallback> QueuedBefore { get; set; } = new List<UnitCloneCallback>();
            public List<UnitCloneCallback> QueuedAfter { get; set; } = new List<UnitCloneCallback>();
            public List<UnitCloneCallback> Dispatched { get; set; } = new List<UnitCloneCallback>();
            public string? UnsupportedQueueReason { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly List<Record> active = new List<Record>();
        private static int observingBirth;
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddTrigger))]
        private static class TriggerIdentityPatch
        {
            private static void Postfix(CharacterState __instance)
            {
                // Register after each prepend so IDs reflect creation order, even
                // when a relic installs several triggers before the next snapshot.
                if (observingBirth > 0 && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    FullBattleTrace.Active?.TriggerStates(__instance);
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.CharacterAdded), new[] { typeof(CharacterState), typeof(CardState), typeof(bool) })]
        private static class AddedPatch
        {
            private static void Postfix(CharacterState character, CardState fromCard, bool onlyCovenants, ref IEnumerator __result)
            {
                bool bigDamageBirthCapture = OriginalBigDamageScenario.Prepared && !onlyCovenants &&
                    character.GetTeamType() == Team.Type.Monsters && !character.IsPyreHeart();
                bool ordinaryBirthCapture = !onlyCovenants && (OriginalHealthChoiceScenario.Prepared || OriginalEquipmentGrowthScenario.Prepared || OriginalConduitScenario.Prepared || OriginalCombatTriggerBatchScenario.Prepared || OriginalStealthSlayScenario.Prepared || SpawnStatusRelicScenario.Prepared || SpawnStatusRelicScenario.OriginalCombatRelicsAcquired || OriginalRelicBatchScenario.Prepared ||
                    RelicCardModifierScenario.Prepared && RelicCardModifierScenario.AllUnitsMultistrikeMutator || bigDamageBirthCapture);
                bool covenantBirthCapture = onlyCovenants && CovenantCharacterFilterScenario.Prepared;
                bool completeTrainCapture = SpawnStatusRelicScenario.Clones || OriginalLifecycleRelicBatchScenario.Enabled || OriginalCombatTriggerBatchScenario.Enabled || OriginalConduitScenario.Enabled || OriginalEquipmentGrowthScenario.Enabled || OriginalStealthSlayScenario.Enabled || OriginalHealthChoiceScenario.Enabled || bigDamageBirthCapture;
                if ((!ordinaryBirthCapture && !covenantBirthCapture) || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                __result = Observe(__result, character, fromCard, onlyCovenants, completeTrainCapture);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState character, CardState fromCard, bool covenants, bool completeTrainCapture)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = character.GetCurrentRoom();
            var record = new Record { UnitId = trace.UnitId(character), FromCardId = fromCard == null ? 0 : trace.CardId(fromCard),
                OnlyCovenants = covenants, Before = trace.Capture(room) };
            Records.Add(record);
            RelicBirthResult? world = null;
            RoomCombatResult? predicted = null;
            if (completeTrainCapture)
            {
                record.BeforeTrain = trace.CaptureTrain();
                record.QueueRunningBefore = AllGameManagers.Instance!.GetCombatManager()!.IsRunningTriggerQueue;
                record.StandaloneScenarioActive = UnitCloneScenario.Started && !UnitCloneScenario.Completed;
                record.ReplayCardPlayingBefore = AllGameManagers.Instance!.GetReplayManager()!.IsCardPlaying();
                record.SettlementProtocol = UnitCloneScenario.StandaloneBirth ? "standalone-scenario-birth" : "enclosing-native-coroutine";
                record.DeferCallbacks = !UnitCloneScenario.StandaloneBirth || record.QueueRunningBefore;
                record.QueuedBefore = Queue(record);
                world = RelicBirthModel.CharacterAdded(record.BeforeTrain, record.UnitId, record.FromCardId, covenants,
                    record.DeferCallbacks, record.QueuedBefore);
                active.Add(record);
            }
            else predicted = RelicSpawnStatusModel.CharacterAdded(record.Before, record.UnitId, record.FromCardId, covenants);
            observingBirth++;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                observingBirth--;
                (native as IDisposable)?.Dispose(); record.After = trace.Capture(room);
                if (completeTrainCapture)
                {
                    active.Remove(record); record.AfterTrain = trace.CaptureTrain(); record.QueuedAfter = Queue(record);
                    IEnumerable<UnitCloneCallback> predictedDispatches = world!.Dispatched;
                    IEnumerable<UnitCloneCallback> nativeDispatches = record.Dispatched;
                    if (OriginalBigDamageScenario.Prepared)
                    {
                        predictedDispatches = BehavioralDispatches(record.BeforeTrain!, predictedDispatches);
                        nativeDispatches = BehavioralDispatches(record.BeforeTrain!, nativeDispatches);
                    }
                    record.Difference = record.UnsupportedQueueReason ?? (!world!.Supported ? world.UnsupportedReason :
                        JToken.DeepEquals(JToken.FromObject(world.State!), JToken.FromObject(record.AfterTrain)) &&
                        JToken.DeepEquals(JToken.FromObject(world.Queued), JToken.FromObject(record.QueuedAfter)) &&
                        JToken.DeepEquals(JToken.FromObject(predictedDispatches.ToArray()), JToken.FromObject(nativeDispatches.ToArray())) ? null :
                        "Relic CharacterAdded train/queue/dispatch differs.");
                }
                else record.Difference = !predicted!.Supported ? predicted.UnsupportedReason :
                    JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) ? null :
                    "Relic CharacterAdded room/context differs.";
            }
        }
        private static List<UnitCloneCallback> Queue(Record record)
        {
            var queue = ((IEnumerable)AccessTools.Property(typeof(CombatManager), "TriggerQueue")
                .GetValue(AllGameManagers.Instance!.GetCombatManager())).Cast<CombatManager.TriggerQueueData>().ToArray();
            if (queue.Any(item => !item.canAttackOrHeal || !item.canFireTriggers))
                record.UnsupportedQueueReason = "Relic birth requires captured disabled native queue flags.";
            return queue.Select(item => Payload(item.character, item.trigger, item.dyingCharacter, item.fireTriggersData, item.triggerCount, item.exclusiveTrigger)).ToList();
        }
        private static UnitCloneCallback Payload(CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState? dying,
            CharacterState.FireTriggersData? data, int count, CharacterTriggerState? exclusive)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            return new UnitCloneCallback(trace.UnitId(actor), kind.ToString(), data?.paramInt ?? 0, data?.paramInt2 ?? 0, data?.paramString,
                count, dying == null ? 0 : trace.UnitId(dying), data?.overrideTargetCharacter == null ? 0 : trace.UnitId(data.overrideTargetCharacter),
                data?.overrideLastSpawnedCharacter == null ? 0 : trace.UnitId(data.overrideLastSpawnedCharacter),
                exclusive == null ? (int?)null : trace.TriggerStateId(actor, exclusive));
        }
        private static IEnumerable<UnitCloneCallback> BehavioralDispatches(TrainCombatState before,
            IEnumerable<UnitCloneCallback> callbacks)
        {
            return callbacks.Where(callback => before.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == callback.ActorId) is CombatUnit actor &&
                (actor.Statuses.Count > 0 || actor.Triggers.Any(trigger => trigger.Kind == callback.Kind)));
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue, CharacterTriggerState exclusiveTrigger)
            {
                if (!fromRunningTriggerQueue || active.Count == 0 || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                UnitCloneCallback item = Payload(__instance, trigger, dyingCharacter, fireTriggersData, triggerCount, exclusiveTrigger);
                foreach (Record record in active) record.Dispatched.Add(item);
            }
        }
    }
}
