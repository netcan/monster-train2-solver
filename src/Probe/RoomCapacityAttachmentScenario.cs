using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomCapacityAttachmentScenario
    {
        internal const string Name = "room-capacity-attachment";
        internal const string CardDataId = "66a6a723-6f60-47ef-8256-454de1c32475";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static CoverageRecord Evidence { get; } = new CoverageRecord();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardData definition = save.GetAllGameData().FindCardData(CardDataId) ??
                throw new InvalidOperationException("IncreaseRoomCapacity attachment card is missing.");
            CardEffectData effect = definition.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData() ??
                throw new InvalidOperationException("IncreaseRoomCapacity has no attachment upgrade.");
            var modifiers = upgrade.GetRoomModifierUpgrades();
            if (definition.name != "IncreaseRoomCapacity" || definition.GetCardType() != CardType.TrainRoomAttachment ||
                effect.GetEffectStateName() != "CardEffectAttachTrainRoomAttachment" || effect.GetAdditionalParamInt() != 0 ||
                modifiers.Count != 1 || modifiers[0].GetRoomStateModifierClassName() != "RoomStateCapacityModifier" ||
                modifiers[0].GetParamInt() != 4)
                throw new InvalidOperationException("IncreaseRoomCapacity native payload changed.");

            CardState card = cards.GetHand().FirstOrDefault() ??
                throw new InvalidOperationException("Capacity attachment scenario requires the native opening hand.");
            card.Setup(definition, save, setupStartingUpgrades: false);
            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            if (room.GetIsPyreRoom() || room.Attachments.Count != 0)
                throw new InvalidOperationException("Capacity attachment scenario requires an unattached monster floor.");

            Evidence.Prepared = true;
            Evidence.CardDataId = definition.GetID();
            Evidence.AssetKey = definition.name;
            Evidence.BeforeCapacity = room.GetCapacityInfo(Team.Type.Monsters).max;
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ROOM-CAPACITY-ATTACHMENT-PREPARED card=IncreaseRoomCapacity native opening-hand capacity=" +
                Evidence.BeforeCapacity + " capacityDelta=4");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || Evidence.Selected || state.PlayRules == null || state.Spawn.Train.Context == null) return null;
            CardToken? card = state.Spawn.Train.Context.Cards.Hand.FirstOrDefault(item => item.DataId == CardDataId);
            if (card == null) return null;
            CardPlayRule? rule = state.PlayRules.Cards.FirstOrDefault(item => item.DataId == CardDataId);
            if (rule?.Effect != "Spell" || rule.ExternalInteractions.Count != 0 ||
                rule.Effects.Count != 1 || rule.Effects[0].Type != "AttachRoomCapacity")
                throw new InvalidOperationException("IncreaseRoomCapacity was not captured as a supported room attachment card.");

            foreach (RoomCombatState room in state.Spawn.Train.Rooms.Where(item =>
                state.PlayRules.Rooms.Single(rule => rule.RoomIndex == item.RoomIndex).IsPyre == false).OrderBy(item => item.RoomIndex))
            {
                var action = new PlayCardAction(card.InstanceId, room.RoomIndex);
                if (!BattleActionModel.PlayCard(state, action).Supported) continue;
                Evidence.Selected = true;
                Evidence.RoomIndex = room.RoomIndex;
                return action;
            }
            return null;
        }

        internal static void CompleteCardPlay(AllGameManagers managers)
        {
            if (!Evidence.Selected || Evidence.Completed) return;
            RoomState room = managers.GetRoomManager()!.GetRoom(Evidence.RoomIndex);
            TrainRoomAttachmentState? attachment = room.Attachments.FirstOrDefault(item => item.IsActive &&
                item.CardState?.GetCardDataID() == CardDataId);
            Evidence.AfterCapacity = room.GetCapacityInfo(Team.Type.Monsters).max;
            Evidence.ActiveAttachmentCount = room.Attachments.Count(item => item.IsActive);
            Evidence.AttachedCardDataId = attachment?.CardState?.GetCardDataID();
            Evidence.Completed = true;
            if (attachment == null || Evidence.AfterCapacity != Evidence.BeforeCapacity + 4)
                Evidence.Error = "Native attachment did not add four capacity and remain active on its floor.";
        }

        internal sealed class CoverageRecord
        {
            public bool Prepared { get; set; }
            public string CardDataId { get; set; } = string.Empty;
            public string AssetKey { get; set; } = string.Empty;
            public int BeforeCapacity { get; set; }
            public bool Selected { get; set; }
            public int RoomIndex { get; set; } = -1;
            public bool Completed { get; set; }
            public int AfterCapacity { get; set; } = -1;
            public int ActiveAttachmentCount { get; set; }
            public string? AttachedCardDataId { get; set; }
            public string? Error { get; set; }
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
