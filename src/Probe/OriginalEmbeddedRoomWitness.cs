#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEmbeddedRoomWitness
    {
        private const string FileName = "original-embedded-room.mt2f";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_EMBEDDED_ROOM_WITNESS") == "1";
        internal static bool Acquired { get; private set; }
        private static CardState? first, second;
        private static object? definition;
        private static BattleTurnState? before;
        private static PlayCardAction? action;

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original rank-zero Ring 1 before battle.");
            MutatorData owner = save.GetAllGameData().FindMutatorData(RelicModel.AlwaysEmbeddedRoomsOwnerId)
                ?? throw new InvalidOperationException("The original embedded room mutator was missing.");
            RelicEffectData effect = owner.GetEffects().Single();
            if (owner.name != RelicModel.AlwaysEmbeddedRoomsOwnerKey || owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectTrainRoomAttachmentsAlwaysEmbedded) ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("The original embedded room mutator changed.");
            save.AddMutator(owner);
            if (save.GetMutators().Single().GetEffects().Single() is not RelicEffectTrainRoomAttachmentsAlwaysEmbedded installed ||
                !installed.GetIsAlwaysEmbedded())
                throw new InvalidOperationException("The native embedded room rule was not installed.");
            Acquired = true;
            definition = new { Id = owner.GetID(), AssetKey = owner.name, Effect = effect.GetEffectClassName(),
                owner.DisallowInDeploymentPhase };
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (!managers.GetCombatManager().IsPlacementPhase || !managers.GetRelicManager().GetTrainRoomAttachmentsAlwaysEmbedded())
                throw new InvalidOperationException("Expected original embedded-room placement.");
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            if (first == null)
            {
                CardData card = managers.GetAllGameData().FindCardData(OriginalFirstRoomAttachmentScenario.AttachmentCardId);
                if (card?.name != "IncreaseRoomCapacity" || card.GetCardType() != CardType.TrainRoomAttachment ||
                    card.GetEffects() is not [var effect] || effect.GetAdditionalParamInt() != 0 ||
                    effect.GetParamCardUpgradeData()?.GetRoomModifierUpgrades().Single().GetParamInt() != 4)
                    throw new InvalidOperationException("The stock capacity attachment changed.");
                first = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
                second = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
                if (first == null || second == null || !cards.CanPlayHandCard(first, 0, null, null, null, out _))
                    throw new InvalidOperationException("The original capacity attachment was unavailable.");
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(first), 0);
                CommonSelectionBehavior.SelectionError error = CommonSelectionBehavior.SelectionError.None;
                if (!cards.PlayCard(cards.GetHand().IndexOf(first), null, ref error))
                    throw new InvalidOperationException("Original attachment failed: " + error);
                return false;
            }

            var after = trace.CaptureDecision();
            var replacement = second!.GetEffectStates().Single();
            int[] floors = Enumerable.Range(0, rooms.GetNumRooms()).Where(index => !rooms.GetRoom(index).GetIsPyreRoom()).ToArray();
            var permissions = floors.Select(index => new
            {
                Floor = index,
                Allowed = GameEffectHelper.TestEffect(replacement,
                    new CardEffectParams { playedCard = second, selectedRoom = index }, managers.GetCoreManagers(), false),
                Affordable = cards.CanPlayHandCard(second, index, null, null, null, out _)
            }).ToArray();
            var installed = floors.Select(index => new
            {
                Floor = index,
                Capacity = rooms.GetRoom(index).GetCapacityInfo(Team.Type.Monsters).max,
                Count = rooms.GetRoom(index).NumRoomAttachmentsAddedFromCard,
                Types = rooms.GetRoom(index).Attachments.Where(item => item.IsActive).Select(item => item.AttachType.ToString()).ToArray()
            }).ToArray();
            int cost = first.GetLastPlayedCost();
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalEmbeddedRoom", GameVersion = Application.version, GameModuleMvid = Module,
                Definition = definition, Before = before, Action = action, After = after,
                ReplacementCardId = trace.CardId(second!), Permissions = permissions, Installed = installed, PaidCost = cost
            });
            using (Stream stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName)))
                document.Write(stream);
            if (before!.Energy - after.Energy != cost || floors.Length != 3 || permissions.Any(item => item.Allowed || !item.Affordable) ||
                installed.Any(item => item.Count != 1 || item.Types is not ["Primary_Embedded"] ||
                    item.Capacity != before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == item.Floor).PlayerMaximum + 4))
                throw new InvalidOperationException("The original attachment, replicated copies or embedded play gates did not settle.");
            return true;
        }
    }
}
#endif
