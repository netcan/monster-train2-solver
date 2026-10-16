using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCarriageJackScenario
    {
        internal const string Name = "original-carriage-jack";
        internal const string RelicId = "19e15240-37ca-460d-8230-693626fc7f7e";
        internal const string RelicKey = "CarriageJack";
        internal const string AttachmentCardId = "66a6a723-6f60-47ef-8256-454de1c32475";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static CoverageRecord Evidence { get; } = new CoverageRecord();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("CarriageJack must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("CarriageJack is already present in the native test run.");
            save.AddRelic(relic);
            Acquired = true;
            RelicEffectData effect = relic.GetEffects().Single();
            log.LogInfo("ORIGINAL-CARRIAGE-JACK-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() +
                " amount=" + effect.GetParamInt() + " cardType=" + effect.GetParamCardType() + " conditions=" + effect.GetEffectConditions().Count + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("CarriageJack coverage must be prepared on deployment turn after acquiring the relic.");
            SaveManager save = managers.GetSaveManager();
            CardData attachment = save.GetAllGameData().FindCardData(AttachmentCardId)
                ?? throw new InvalidOperationException("Native IncreaseRoomCapacity card is missing.");
            CardEffectData nativeEffect = attachment.GetEffects().Single();
            if (attachment.name != "IncreaseRoomCapacity" || attachment.GetCardType() != CardType.TrainRoomAttachment ||
                nativeEffect.GetEffectStateName() != "CardEffectAttachTrainRoomAttachment" ||
                nativeEffect.GetParamCardUpgradeData()?.GetRoomModifierUpgrades().Single().GetParamInt() != 4)
                throw new InvalidOperationException("Native IncreaseRoomCapacity payload changed.");

            CardState card = managers.GetCardManager()!.AddNewCard(attachment, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("Native IncreaseRoomCapacity card did not enter the hand.");
            RoomManager rooms = managers.GetRoomManager()!;
            RoomState[] openFloors = Enumerable.Range(0, rooms.GetNumRooms()).Select(rooms.GetRoom)
                .Where(room => !room.GetIsPyreRoom() && room.GetNumLivingCharacters(Team.Type.Monsters) == 0 &&
                    room.GetNumLivingCharacters(Team.Type.Heroes) == 0).ToArray();
            if (openFloors.Length < 2)
                throw new InvalidOperationException("CarriageJack controls require two empty non-Pyre floors.");
            RoomState controlFloor = openFloors[0];
            RoomState floor = openFloors[openFloors.Length - 1];
            Evidence.Prepared = true;
            Evidence.AttachmentCardDataId = attachment.GetID();
            Evidence.AttachmentCardInstanceId = FullBattleTrace.Active!.CardId(card);
            Evidence.RoomIndex = floor.GetRoomIndex();
            Evidence.InitialCapacity = floor.GetCapacityInfo(Team.Type.Monsters).max;
            Evidence.NormalRoomIndex = controlFloor.GetRoomIndex();
            Evidence.NormalInitialCapacity = controlFloor.GetCapacityInfo(Team.Type.Monsters).max;
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-CARRIAGE-JACK-PREPARED attachment=IncreaseRoomCapacity id=" + attachment.GetID() +
                " instance=" + Evidence.AttachmentCardInstanceId + " room=" + Evidence.RoomIndex + " capacity=" + Evidence.InitialCapacity +
                " controlRoom=" + Evidence.NormalRoomIndex + " controlCapacity=" + Evidence.NormalInitialCapacity + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state, PlayCardAction? preferred = null)
        {
            if (!Prepared || state.PlayRules == null || state.Spawn.Train.Context == null) return preferred;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
            if (!Evidence.NormalCompleted)
            {
                PlayCardAction? normal = plays.FirstOrDefault(action => action.RoomIndex == Evidence.NormalRoomIndex &&
                    state.Spawn.Train.Context.FindCard(action.CardInstanceId) is CardInstanceState card &&
                    card.DataId != AttachmentCardId &&
                    state.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId) is CardPlayRule rule &&
                    rule.CardType == "Monster" && rule.Effect == "SpawnMonster" && rule.ExternalInteractions.Count == 0);
                if (normal == null) throw new InvalidOperationException("No supported ordinary monster card was available for CarriageJack's negative control.");
                Evidence.NormalCardInstanceId = normal.CardInstanceId;
                Evidence.NormalCardDataId = state.Spawn.Train.Context.FindCard(normal.CardInstanceId)!.DataId;
                return normal;
            }

            if (!Evidence.AttachmentCompleted)
            {
                PlayCardAction? attachment = plays.FirstOrDefault(action => action.CardInstanceId == Evidence.AttachmentCardInstanceId &&
                    action.RoomIndex == Evidence.RoomIndex);
                if (attachment == null) throw new InvalidOperationException("Native IncreaseRoomCapacity was not a supported play on the selected floor.");
                Evidence.AttachmentSelected = true;
                return attachment;
            }

            if (preferred == null || preferred.RoomIndex != Evidence.RoomIndex) return preferred;
            CardInstanceState? preferredCard = state.Spawn.Train.Context.FindCard(preferred.CardInstanceId);
            CardPlayRule? preferredRule = preferredCard == null ? null :
                state.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == preferredCard.DataId);
            if (preferredRule?.Effect != "SpawnMonster") return preferred;
            PlayCardAction? redirected = plays.FirstOrDefault(action => action.CardInstanceId == preferred.CardInstanceId &&
                action.RoomIndex != Evidence.RoomIndex);
            if (redirected == null)
                redirected = plays.FirstOrDefault(action => action.RoomIndex != Evidence.RoomIndex);
            if (redirected == null) return null;
            Evidence.ReroutedDeployments++;
            return redirected;
        }

        internal static bool IsScenarioAction(PlayCardAction action) =>
            action.CardInstanceId == Evidence.NormalCardInstanceId && !Evidence.NormalCompleted ||
            action.CardInstanceId == Evidence.AttachmentCardInstanceId && Evidence.AttachmentSelected && !Evidence.AttachmentCompleted;

        internal static void CompleteCardPlay(AllGameManagers managers)
        {
            if (!Prepared || Evidence.AttachmentCompleted) return;
            if (!Evidence.NormalCompleted && Evidence.NormalCardInstanceId > 0)
            {
                Evidence.NormalAfterCapacity = managers.GetRoomManager()!.GetRoom(Evidence.NormalRoomIndex)
                    .GetCapacityInfo(Team.Type.Monsters).max;
                Evidence.NormalCompleted = true;
                return;
            }
            if (Evidence.AttachmentSelected)
            {
                int capacity = managers.GetRoomManager()!.GetRoom(Evidence.RoomIndex).GetCapacityInfo(Team.Type.Monsters).max;
                Evidence.AttachmentAfterCapacity = capacity;
                Evidence.AttachmentCompleted = true;
                if (capacity != Evidence.InitialCapacity + 5)
                    Evidence.Error = "Native attachment plus CarriageJack did not add five capacity to the selected floor.";
            }
        }

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.GetID() != RelicId || relic.name != RelicKey ||
                effect.GetEffectClassName() != "RelicEffectModifyRoomCapacityOnCardPlay" || effect.GetParamInt() != 1 ||
                effect.GetParamCardType() != CardType.TrainRoomAttachment || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original CarriageJack definition changed.");
        }

        internal sealed class CoverageRecord
        {
            public bool Prepared { get; set; }
            public string AttachmentCardDataId { get; set; } = string.Empty;
            public int AttachmentCardInstanceId { get; set; }
            public string NormalCardDataId { get; set; } = string.Empty;
            public int NormalCardInstanceId { get; set; }
            public int RoomIndex { get; set; } = -1;
            public int InitialCapacity { get; set; } = -1;
            public int NormalRoomIndex { get; set; } = -1;
            public int NormalInitialCapacity { get; set; } = -1;
            public int NormalAfterCapacity { get; set; } = -1;
            public int ReroutedDeployments { get; set; }
            public int AttachmentAfterCapacity { get; set; } = -1;
            public bool NormalCompleted { get; set; }
            public bool AttachmentSelected { get; set; }
            public bool AttachmentCompleted { get; set; }
            public string? Error { get; set; }
        }
    }
}
