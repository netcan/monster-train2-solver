using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFirstRoomAttachmentScenario
    {
        internal const string Name = "original-first-room-attachment";
        internal const string RelicId = "e55235f7-1d84-43f0-b8f5-037b64b8f582";
        internal const string RelicKey = "FirstRoomCardOnAllFloors";
        internal const string AttachmentCardId = "66a6a723-6f60-47ef-8256-454de1c32475";
        private static int pendingStep;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static CoverageRecord Evidence { get; } = new CoverageRecord();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("FirstRoomCardOnAllFloors must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("FirstRoomCardOnAllFloors is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-FIRST-ROOM-ATTACHMENT-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() +
                " attachmentType=" + relic.GetEffects().Single().GetParamInt2() + " conditionCount=" + relic.GetEffects().Single().GetEffectConditions().Count + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("FirstRoomCardOnAllFloors coverage must start on deployment turn after relic acquisition.");
            SaveManager save = managers.GetSaveManager();
            CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId));
            AllGameData data = save.GetAllGameData();
            CardData attachment = data.FindCardData(AttachmentCardId)
                ?? throw new InvalidOperationException("Native IncreaseRoomCapacity card is missing.");
            CardEffectData nativeEffect = attachment.GetEffects().Single();
            if (attachment.name != "IncreaseRoomCapacity" || attachment.GetCardType() != CardType.TrainRoomAttachment ||
                nativeEffect.GetEffectStateName() != "CardEffectAttachTrainRoomAttachment" ||
                nativeEffect.GetParamCardUpgradeData()?.GetRoomModifierUpgrades().Single().GetParamInt() != 4)
                throw new InvalidOperationException("Native IncreaseRoomCapacity payload changed.");

            RoomManager rooms = managers.GetRoomManager()!;
            RoomState[] openFloors = Enumerable.Range(0, rooms.GetNumRooms()).Select(rooms.GetRoom)
                .Where(room => !room.GetIsPyreRoom() && room.GetNumLivingCharacters(Team.Type.Monsters) == 0 &&
                    room.GetNumLivingCharacters(Team.Type.Heroes) == 0).ToArray();
            if (openFloors.Length < 2)
                throw new InvalidOperationException("FirstRoomCardOnAllFloors controls require two empty non-Pyre floors.");
            RoomState controlFloor = openFloors[0];
            RoomState selectedFloor = openFloors[openFloors.Length - 1];
            CardManager cards = managers.GetCardManager()!;
            CardState[] attachments = Enumerable.Range(0, 2)
                .Select(_ => cards.AddNewCard(attachment, CardPile.HandPile, false, false, animate: false))
                .Where(card => card != null).Cast<CardState>().ToArray();
            if (attachments.Length != 2 || attachments.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Could not add both native IncreaseRoomCapacity cards to hand.");

            Evidence.Prepared = true;
            Evidence.AttachmentCardDataId = attachment.GetID();
            Evidence.AttachmentCardInstanceIds = attachments.Select(FullBattleTrace.Active!.CardId).ToArray();
            Evidence.ConditionTriggerLimit = (int)AccessTools.Field(typeof(RelicEffectCondition), "paramInt")
                .GetValue(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId)
                    .GetEffects().Single().GetEffectConditions().Single());
            Evidence.ConditionTriggerCountAtSetup = CaptureConditionState(managers).TriggerCount;
            Evidence.RoomIndex = selectedFloor.GetRoomIndex();
            Evidence.ControlRoomIndex = controlFloor.GetRoomIndex();
            Evidence.PyreRoomIndexes = Enumerable.Range(0, rooms.GetNumRooms()).Select(rooms.GetRoom)
                .Where(room => room.GetIsPyreRoom()).Select(room => room.GetRoomIndex()).ToArray();
            Evidence.InitialCapacities = CaptureCapacities(rooms);
            Evidence.InitialAttachmentCounts = CaptureAttachmentCounts(rooms);
            if (Evidence.InitialAttachmentCounts.Where((_, index) => !Evidence.PyreRoomIndexes.Contains(index)).Any(count => count != 0))
                throw new InvalidOperationException("The first-room attachment scenario requires clean room attachment counters.");
            managers.GetPlayerManager().AddEnergy(99);
            Evidence.EnergyAtSetup = managers.GetPlayerManager().GetEnergy();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-FIRST-ROOM-ATTACHMENT-PREPARED card=IncreaseRoomCapacity instanceIds=" +
                string.Join(",", Evidence.AttachmentCardInstanceIds) + " conditionTriggerLimit=" + Evidence.ConditionTriggerLimit +
                " conditionCountAtSetup=" + Evidence.ConditionTriggerCountAtSetup + " room=" + Evidence.RoomIndex + " floors=" +
                rooms.GetNumRooms() + " energy=" + Evidence.EnergyAtSetup + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || state.PlayRules == null || state.Spawn.Train.Context == null || pendingStep != 0) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
            if (!Evidence.NormalCompleted)
            {
                PlayCardAction? normal = plays.FirstOrDefault(action => action.RoomIndex == Evidence.ControlRoomIndex &&
                    state.Spawn.Train.Context.FindCard(action.CardInstanceId) is CardInstanceState card &&
                    state.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId) is CardPlayRule rule &&
                    rule.CardType == "Monster" && rule.Effect == "SpawnMonster" && rule.ExternalInteractions.Count == 0);
                if (normal == null) throw new InvalidOperationException("No supported ordinary monster card was available for the negative control.");
                Evidence.NormalCardInstanceId = normal.CardInstanceId;
                Evidence.NormalCardDataId = state.Spawn.Train.Context.FindCard(normal.CardInstanceId)!.DataId;
                pendingStep = 1;
                return normal;
            }
            if (!Evidence.FirstAttachmentCompleted)
            {
                PlayCardAction? first = plays.FirstOrDefault(action => action.CardInstanceId == Evidence.AttachmentCardInstanceIds[0] &&
                    action.RoomIndex == Evidence.RoomIndex);
                if (first == null) throw new InvalidOperationException("The first native IncreaseRoomCapacity card was not a supported play on the selected floor.");
                pendingStep = 2;
                return first;
            }
            if (!Evidence.SecondAttachmentCompleted)
            {
                PlayCardAction? second = plays.FirstOrDefault(action => action.CardInstanceId == Evidence.AttachmentCardInstanceIds[1] &&
                    action.RoomIndex == Evidence.RoomIndex);
                if (second == null)
                {
                    string rejected = string.Join(";", state.PlayRules.Rooms.Select(room =>
                    {
                        BattleActionResult result = BattleActionModel.PlayCard(state,
                            new PlayCardAction(Evidence.AttachmentCardInstanceIds[1], room.RoomIndex));
                        return room.RoomIndex + ":" + result.Rejection + ":" + result.Reason;
                    }));
                    throw new InvalidOperationException("The second native IncreaseRoomCapacity card was not a supported play on the selected floor. " + rejected);
                }
                pendingStep = 3;
                return second;
            }
            return null;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled &&
            (action.CardInstanceId == Evidence.NormalCardInstanceId && !Evidence.NormalCompleted ||
             action.CardInstanceId == Evidence.AttachmentCardInstanceIds.ElementAtOrDefault(0) && !Evidence.FirstAttachmentCompleted ||
             action.CardInstanceId == Evidence.AttachmentCardInstanceIds.ElementAtOrDefault(1) && !Evidence.SecondAttachmentCompleted);

        internal static void CompleteCardPlay(AllGameManagers managers)
        {
            if (!Prepared || pendingStep == 0) return;
            RoomManager rooms = managers.GetRoomManager()!;
            (bool Triggered, int TriggerCount, int ValueAtLastTrigger) condition = CaptureConditionState(managers);
            if (pendingStep == 1)
            {
                Evidence.NormalAfterCapacities = CaptureCapacities(rooms);
                Evidence.NormalAfterAttachmentCounts = CaptureAttachmentCounts(rooms);
                Evidence.ConditionTriggeredAfterControl = condition.Triggered;
                Evidence.NormalCompleted = true;
                if (!Evidence.NormalAfterCapacities.SequenceEqual(Evidence.InitialCapacities) ||
                    !Evidence.NormalAfterAttachmentCounts.SequenceEqual(Evidence.InitialAttachmentCounts) || condition.Triggered)
                    Evidence.Error = "The ordinary-card negative control changed room attachments or triggered the relic.";
            }
            else if (pendingStep == 2)
            {
                Evidence.FirstAttachmentAfterCapacities = CaptureCapacities(rooms);
                Evidence.FirstAttachmentAfterCounts = CaptureAttachmentCounts(rooms);
                Evidence.ConditionTriggeredAfterFirstAttachment = condition.Triggered;
                Evidence.ConditionTriggerCountAfterFirstAttachment = condition.TriggerCount;
                Evidence.ConditionValueAtFirstTrigger = condition.ValueAtLastTrigger;
                Evidence.FirstAttachmentCompleted = true;
                for (int index = 0; index < Evidence.FirstAttachmentAfterCapacities.Length; index++)
                {
                    int expected = Evidence.InitialCapacities[index] + (Evidence.PyreRoomIndexes.Contains(index) ? 0 : 4);
                    if (Evidence.FirstAttachmentAfterCapacities[index] != expected ||
                        Evidence.FirstAttachmentAfterCounts[index] != (Evidence.PyreRoomIndexes.Contains(index) ? 0 : 1))
                        Evidence.Error = "The first capacity attachment did not replicate exactly once to every non-Pyre floor.";
                }
                if (!condition.Triggered || condition.TriggerCount != 1 || condition.ValueAtLastTrigger != 0)
                    Evidence.Error = "The native relic condition did not record its first successful trigger.";
            }
            else
            {
                Evidence.SecondAttachmentAfterCapacities = CaptureCapacities(rooms);
                Evidence.SecondAttachmentAfterCounts = CaptureAttachmentCounts(rooms);
                Evidence.ConditionTriggeredAfterSecondAttachment = condition.Triggered;
                Evidence.ConditionTriggerCountAfterSecondAttachment = condition.TriggerCount;
                Evidence.SecondAttachmentCompleted = true;
                if (!Evidence.SecondAttachmentAfterCapacities.SequenceEqual(Evidence.FirstAttachmentAfterCapacities) ||
                    Evidence.SecondAttachmentAfterCounts[Evidence.RoomIndex] != 2 ||
                    Evidence.SecondAttachmentAfterCounts.Where((_, index) => index != Evidence.RoomIndex && !Evidence.PyreRoomIndexes.Contains(index)).Any(count => count != 1) ||
                    !condition.Triggered || condition.TriggerCount != 1)
                    Evidence.Error = "The later attachment changed replicated floors or retriggered the relic.";
            }
            pendingStep = 0;
        }

        private static int[] CaptureCapacities(RoomManager rooms) => Enumerable.Range(0, rooms.GetNumRooms())
            .Select(index => rooms.GetRoom(index).GetCapacityInfo(Team.Type.Monsters).max).ToArray();

        private static int[] CaptureAttachmentCounts(RoomManager rooms) => Enumerable.Range(0, rooms.GetNumRooms())
            .Select(index => rooms.GetRoom(index).NumRoomAttachmentsAddedFromCard).ToArray();

        private static (bool Triggered, int TriggerCount, int ValueAtLastTrigger) CaptureConditionState(AllGameManagers managers)
        {
            RelicEffectFirstRoomCardIsPlayedOnAllFloors effect = managers.GetRelicManager()
                .GetRelicEffect<RelicEffectFirstRoomCardIsPlayedOnAllFloors>()
                ?? throw new InvalidOperationException("The acquired original room-attachment relic was not initialized.");
            var conditions = (IEnumerable<RelicEffectCondition>)AccessTools.Field(typeof(RelicEffectBase), "_effectConditions").GetValue(effect);
            RelicEffectCondition condition = conditions.Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            return ((bool)Field("triggered"), (int)Field("durationTriggerCount"), (int)Field("valueAtLastTrigger"));
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            string observed = "id=" + relic.GetID() + " key=" + relic.name + " class=" + effect.GetEffectClassName() +
                " int2=" + effect.GetParamInt2() + " tracked=" + Field("paramTrackedValue") +
                " duration=" + Field("paramEntryDuration") + " cardType=" + Field("paramCardType") +
                " subtype=" + Field("paramSubtype") + " comparator=" + Field("paramComparator") +
                " value=" + Field("paramInt") + " trackCount=" + Field("paramTrackTriggerCount") +
                " multiple=" + Field("allowMultipleTriggersPerDuration");
            if (relic.GetID() != RelicId || relic.name != RelicKey ||
                effect.GetEffectClassName() != "RelicEffectFirstRoomCardIsPlayedOnAllFloors" || effect.GetParamInt2() != 0 ||
                Field("paramTrackedValue").ToString() != "SubtypeInDeck" ||
                Field("paramEntryDuration").ToString() != "ThisBattle" || Field("paramCardType").ToString() != "Any" ||
                Field("paramSubtype").ToString() != "SubtypesData_None" || Convert.ToInt32(Field("paramComparator")) != 2 ||
                (int)Field("paramInt") != 1 || !(bool)Field("paramTrackTriggerCount") ||
                (bool)Field("allowMultipleTriggersPerDuration"))
                throw new InvalidOperationException("Original FirstRoomCardOnAllFloors definition or condition changed: " + observed + ".");
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        internal sealed class CoverageRecord
        {
            public bool Prepared { get; set; }
            public string AttachmentCardDataId { get; set; } = string.Empty;
            public int[] AttachmentCardInstanceIds { get; set; } = Array.Empty<int>();
            public int ConditionTriggerLimit { get; set; }
            public int ConditionTriggerCountAtSetup { get; set; }
            public int RoomIndex { get; set; } = -1;
            public int ControlRoomIndex { get; set; } = -1;
            public int[] PyreRoomIndexes { get; set; } = Array.Empty<int>();
            public int[] InitialCapacities { get; set; } = Array.Empty<int>();
            public int[] InitialAttachmentCounts { get; set; } = Array.Empty<int>();
            public int EnergyAtSetup { get; set; }
            public int[] NormalAfterCapacities { get; set; } = Array.Empty<int>();
            public int[] NormalAfterAttachmentCounts { get; set; } = Array.Empty<int>();
            public int[] FirstAttachmentAfterCapacities { get; set; } = Array.Empty<int>();
            public int[] FirstAttachmentAfterCounts { get; set; } = Array.Empty<int>();
            public int[] SecondAttachmentAfterCapacities { get; set; } = Array.Empty<int>();
            public int[] SecondAttachmentAfterCounts { get; set; } = Array.Empty<int>();
            public bool ConditionTriggeredAfterControl { get; set; }
            public bool ConditionTriggeredAfterFirstAttachment { get; set; }
            public int ConditionTriggerCountAfterFirstAttachment { get; set; }
            public int ConditionValueAtFirstTrigger { get; set; }
            public bool ConditionTriggeredAfterSecondAttachment { get; set; }
            public int ConditionTriggerCountAfterSecondAttachment { get; set; }
            public int NormalCardInstanceId { get; set; }
            public string NormalCardDataId { get; set; } = string.Empty;
            public bool NormalCompleted { get; set; }
            public bool FirstAttachmentCompleted { get; set; }
            public bool SecondAttachmentCompleted { get; set; }
            public string? Error { get; set; }
        }
    }
}
