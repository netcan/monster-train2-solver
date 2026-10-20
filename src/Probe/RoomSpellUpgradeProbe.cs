using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomSpellUpgradeProbe
    {
        internal static bool Known(RoomModifierData modifier) => modifier.GetRoomStateModifierClassName() == "RoomStateMagicalPowerModifier";
        internal static RoomSpellUpgradeState? Rule(IEnumerable<RoomModifierData> modifiers, List<string> interactions)
        {
            RoomModifierData[] matches = modifiers.Where(Known).ToArray();
            if (matches.Length == 0) return null;
            if (matches.Length != 1 || matches[0].DisallowedInDeploymentPhase || matches[0].GetParamCardUpgradeData() == null)
            { interactions.Add("Uncalibrated room magical power modifier"); return null; }
            var upgrade = new CardUpgradeState(); upgrade.Setup(matches[0].GetParamCardUpgradeData());
            var value = new RoomSpellUpgradeState(CardModifierProbe.Upgrade(upgrade, false, true), "", Array.Empty<int>());
            string? error = RoomSpellUpgradeModel.Validate(value);
            if (error != null) interactions.Add(error);
            return value;
        }
        private static object Tracker(RoomStateMagicalPowerModifier modifier) =>
            AccessTools.Field(typeof(RoomStateSpellUpgradeModifier), "_cardsWeHaveModified").GetValue(modifier);
        internal static string Normalize(string id)
        {
            var trace = FullBattleTrace.Active; var rooms = AllGameManagers.Instance?.GetRoomManager();
            if (trace == null || rooms == null) return id;
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            foreach (TrainRoomAttachmentState attachment in rooms.GetRoom(index).Attachments)
            foreach (RoomStateMagicalPowerModifier modifier in attachment.RoomStateModifiers.OfType<RoomStateMagicalPowerModifier>())
            {
                object tracker = Tracker(modifier);
                if ((string)AccessTools.Field(tracker.GetType(), "trackerId").GetValue(tracker) == id)
                    return "room-spell:" + trace.UpgradeKey(attachment.Guid);
            }
            return id;
        }
        internal static RoomSpellUpgradeState? Capture(TrainRoomAttachmentState attachment)
        {
            RoomStateMagicalPowerModifier? modifier = attachment.RoomStateModifiers.OfType<RoomStateMagicalPowerModifier>().SingleOrDefault();
            if (modifier == null) return null;
            object tracker = Tracker(modifier);
            string id = (string)AccessTools.Field(tracker.GetType(), "trackerId").GetValue(tracker);
            var cards = (IDictionary)AccessTools.Field(tracker.GetType(), "_modifiedCardSet").GetValue(tracker);
            var upgrade = new CardUpgradeState(); upgrade.Setup(modifier.GetParamCardUpgradeData());
            return new RoomSpellUpgradeState(CardModifierProbe.Upgrade(upgrade, false, true), Normalize(id),
                cards.Keys.Cast<CardState>().Select(FullBattleTrace.Active!.CardId).ToArray());
        }
    }
}
