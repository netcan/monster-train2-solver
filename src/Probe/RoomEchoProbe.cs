using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomEchoProbe
    {
        internal const string CardId = "d74c257e-d38b-44da-b3d4-0a67934803a7";
        internal static bool Known(RoomModifierData data) => data.GetRoomStateModifierClassName() == "RoomStateRecastSpellModifier";
        internal static RoomEchoState? Definition(System.Collections.Generic.IEnumerable<RoomModifierData> modifiers) =>
            modifiers.Any(Known) ? new RoomEchoState() : null;
        internal static RoomEchoState? Capture(TrainRoomAttachmentState attachment)
        {
            RoomStateRecastSpellModifier? modifier = attachment.RoomStateModifiers.OfType<RoomStateRecastSpellModifier>().SingleOrDefault();
            if (modifier == null) return null;
            object? Field(string name) => AccessTools.Field(modifier.GetType(), name).GetValue(modifier);
            CardState? original = (CardState?)Field("cardStateSpellCast"), copy = (CardState?)Field("cardStateSpellCastCopy");
            var trace = FullBattleTrace.Active!;
            return new RoomEchoState(original == null ? 0 : trace.CardId(original), copy == null ? 0 : trace.CardId(copy),
                (int)Field("roomIndexSpellCast")!, BattleSpawnPointProbe.Reference((SpawnPoint?)Field("dropLocationSpellCast")),
                (int)Field("playedCostSpellCast")!, (int)Field("roomCorruptionOnOriginalSpellCast")!,
                copy == null ? null : BattleActionProbe.Definition(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(copy.GetCardDataID())!).Effects);
        }
        internal static void Discover()
        {
            RoomManager rooms = AllGameManagers.Instance!.GetRoomManager()!;
            for (int index = 0; index < rooms.GetNumRooms(); index++)
                foreach (TrainRoomAttachmentState attachment in rooms.GetRoom(index).Attachments) Capture(attachment);
        }
    }
}
