#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CorruptRestrictedStateCalibration
    {
        internal static object? Evidence { get; private set; }

        internal static void Capture(PlayCardAction? action)
        {
            if (action == null || Evidence != null) return;
            FullBattleTrace trace = FullBattleTrace.Active!;
            CardState? card = trace.KnownCards.SingleOrDefault(value => trace.CardId(value) == action.CardInstanceId);
            if (card?.GetCardDataID() != "ae114dc4-d08f-4909-a2ad-d99215661463" ||
                !card.GetTraitStates().Any(trait => trait is CardTraitCorruptState)) return;
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomManager rooms = managers.GetRoomManager()!;
            RoomState room = rooms.GetRoom(action.RoomIndex);
            object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(room);
            object preview = AccessTools.Field(typeof(RoomState), "previewCorruptionStats").GetValue(room);
            var currentField = AccessTools.Field(stats.GetType(), "currentCorruption");
            int current = (int)currentField.GetValue(stats), previewCurrent = (int)currentField.GetValue(preview);
            var selectedField = AccessTools.Field(typeof(RoomUI), "selectedRoom");
            RoomUI ui = rooms.GetRoomUI(); int selected = rooms.GetSelectedRoom();
            JToken before = JToken.FromObject(trace.CaptureDecision());
            int frame = UnityEngine.Time.frameCount;
            CardTraitCorruptRestricted restricted = card.GetTraitStates().OfType<CardTraitCorruptRestricted>().Single();
            var queries = new List<object>();
            try
            {
                selectedField.SetValue(ui, action.RoomIndex);
                foreach (int value in new[] { 0, 1, 4, int.MaxValue })
                {
                    currentField.SetValue(stats, value); currentField.SetValue(preview, value);
                    BattleTurnState query = trace.CaptureDecision();
                    bool hand = restricted.GetIsPlayableFromHand(managers.GetCardManager()!, rooms, 1, out var handError);
                    bool play = restricted.GetIsPlayableFromPlay(managers.GetCardManager()!, rooms, 1, out var playError);
                    queries.Add(new { Before = query, RoomIndex = action.RoomIndex, SourceRoomIndex = 1,
                        CardId = action.CardInstanceId, Corruption = value, FromHand = hand, FromPlay = play,
                        HandError = handError.ToString(), PlayError = playError.ToString() });
                }
            }
            finally
            {
                currentField.SetValue(stats, current); currentField.SetValue(preview, previewCurrent);
                selectedField.SetValue(ui, selected);
            }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            Evidence = new { Queries = queries, QueriesUnchanged = unchanged };
            if (!unchanged) throw new InvalidOperationException("Corrupt plus restriction queries changed the native decision/RNG/frame.");
        }
    }
}
#endif
