using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitBirthProbe
    {
        internal sealed class Record
        {
            public CardPlayRule Definition { get; set; } = null!;
            public int SpawnerCardId { get; set; }
            public int Position { get; set; }
            public bool IsCardless { get; set; }
            public CombatStatus CardlessStatus { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public int UnitId { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
            internal CharacterState? Unit;
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class CloneRecord
        {
            public CardCreationRule Creation { get; set; } = null!;
            public int SourceCardId { get; set; }
            public int CloneCardId { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<CloneRecord> Clones = new List<CloneRecord>();
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.CopyCardState))]
        private static class ClonePatch
        {
            private static void Prefix(CardState sourceCardState, out CloneRecord? __state)
            {
                __state = null;
                if ((!MultiSummonScenario.Prepared && !TriggeredSummonProbe.Enabled) || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                __state = new CloneRecord { SourceCardId = FullBattleTrace.Active.CardId(sourceCardState),
                    Creation = CardGenerationProbe.Creation(AllGameManagers.Instance.GetSaveManager().GetAllGameData().FindCardData(sourceCardState.GetCardDataID())!),
                    Before = FullBattleTrace.Active.CaptureContext() }; Clones.Add(__state);
            }
            private static void Postfix(CardState __result, CloneRecord? __state)
            {
                if (__state == null) return;
                __state.CloneCardId = FullBattleTrace.Active!.CardId(__result); __state.After = FullBattleTrace.Active.CaptureContext(); __state.Completed = true;
                CardGenerationResult predicted = CardGenerationModel.CloneDetached(__state.Before, __state.Creation, __state.SourceCardId);
                __state.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                    !JToken.DeepEquals(JToken.FromObject(predicted.Context!), JToken.FromObject(__state.After)) ||
                    predicted.AddedCards[0].InstanceId != __state.CloneCardId ? "Detached clone context differs" : null;
            }
        }

        private static IEnumerator Observe(IEnumerator native, Record record, int roomIndex, SpawnPoint point)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(roomIndex);
            record.Before = trace.Capture(room); record.Position = point.GetIndexInRoom(); Records.Add(record);
            UnitBirthResult predicted = UnitBirthModel.Spawn(record.Before, record.Definition, record.SpawnerCardId,
                record.Position, record.IsCardless, record.CardlessStatus);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.After = trace.Capture(room);
                record.UnitId = record.Unit == null ? 0 : trace.UnitId(record.Unit);
                record.Difference = !predicted.Supported ? predicted.Result.UnsupportedReason : predicted.UnitId != record.UnitId ||
                    !JToken.DeepEquals(JToken.FromObject(predicted.Result.State!), JToken.FromObject(record.After)) ? "Unit birth room/context differs" : null;
            }
        }
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class BirthPatch
        {
            private static void Prefix(CharacterData monsterData, CardState spawnerCard, CardState fromPlayedCard, bool isCardless, SpawnPoint spawnLocation,
                ref Action<CharacterState> afterCharacterCreated, out Record? __state)
            {
                __state = null;
                if (!MultiSummonScenario.Prepared || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    monsterData == null || spawnerCard == null && fromPlayedCard == null || spawnLocation == null) return;
                CardState definitionCard = spawnerCard ?? fromPlayedCard;
                var record = new Record { Definition = BattleActionProbe.BirthDefinition(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(definitionCard.GetCardDataID())!, monsterData, spawnerCard == null),
                    SpawnerCardId = spawnerCard == null ? 0 : FullBattleTrace.Active.CardId(spawnerCard), IsCardless = isCardless,
                    CardlessStatus = BattleActionProbe.Status("cardless", 1) };
                __state = record; Action<CharacterState> callback = afterCharacterCreated;
                afterCharacterCreated = unit => { record.Unit = unit; callback?.Invoke(unit); };
            }
            private static void Postfix(int selectedRoom, SpawnPoint spawnLocation, Record? __state, ref IEnumerator __result)
            { if (__state != null) __result = Observe(__result, __state, selectedRoom, spawnLocation); }
        }
    }
}
