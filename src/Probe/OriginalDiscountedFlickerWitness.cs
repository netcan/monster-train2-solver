#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDiscountedFlickerWitness
    {
        private const string FlickerId = "3d1a2e1b-7751-4b2c-a549-77d03db715c9";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static CardState? flicker;
        private static CardState[]? handBefore;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static int originalCost, paidCost, visibleDeck;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DISCOUNTED_FLICKER") == "1";

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module || save.PreviewMode ||
                save.GetCurrentDistance() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Discounted Flicker requires the original 2.3.1 first battle.");
            OriginalFirstCardCostScenario.AcquireBeforeBattle(managers, log);
            var torch = save.GetAllGameData().FindCardData(TorchId);
            if (torch == null || torch.GetCardType() != CardType.Spell)
                throw new InvalidOperationException("Missing the original Torch definition.");
            int additional = Math.Max(0, 23 - save.GetVisibleDeckCount());
            for (int index = 0; index < additional; index++) save.AddCardToDeck(torch, showAnimation: false);
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager()!;
            var rooms = managers.GetRoomManager()!;
            var player = managers.GetPlayerManager();
            var stats = managers.GetCardStatistics();
            var monsters = managers.GetMonsterManager();
            var relics = managers.GetRelicManager();
            if (save.PreviewMode || !OriginalFirstCardCostScenario.Acquired || managers.GetCombatManager()!.GetTurnCount() != 1)
                throw new InvalidOperationException("Expected the first settled native spell turn.");
            if (flicker == null)
            {
                var data = save.GetAllGameData();
                var spell = data.FindCardData(FlickerId);
                var upgrade = data.FindCardUpgradeData(CardCopyOnPlayModel.UpgradeId);
                if (spell?.name != "Flicker" || upgrade?.name != "CreateSpellCopyWithPurge" ||
                    upgrade.GetTraitDataUpgrades().Single().GetTraitStateName() != CardCopyOnPlayModel.Trait)
                    throw new InvalidOperationException("The authored Flicker or merchant upgrade changed.");
                flicker = cards.AddNewCard(spell, CardPile.DeckPileTop, false, false, animate: false);
                if (flicker == null || !CardUpgradeHelper.TryApplyUpgradeToCard(flicker, null, upgrade, false, false,
                    managers.GetCoreManagers(), out _))
                    throw new InvalidOperationException("Native Flicker did not accept its original merchant upgrade.");
                originalCost = flicker.GetCost(stats, monsters, relics);
                if (!cards.DrawSpecificCard(flicker)) throw new InvalidOperationException("Could not draw original Flicker.");
                return false;
            }

            if (action == null)
            {
                int room = Enumerable.Range(0, 3).Where(index =>
                    cards.CanPlayHandCard(flicker, index, null, null, null, out _)).DefaultIfEmpty(-1).First();
                if (room < 0) throw new InvalidOperationException("Original Flicker is not natively playable.");
                if (rooms.GetSelectedRoom() != room)
                {
                    save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(room));
                    return false;
                }
                var temporary = flicker.GetTemporaryCardStateModifiers().GetCardUpgrades().Single(item =>
                    item.GetCardUpgradeDataId() == RelicFirstCardCostModel.Effect);
                visibleDeck = save.GetVisibleDeckCount();
                int reduction = temporary.GetCostReduction();
                paidCost = flicker.GetCost(stats, monsters, relics, rooms.GetRoom(room));
                if (visibleDeck < 15 || cards.GetDrawPile().Count < 15 || reduction != visibleDeck / 15 ||
                    !temporary.GetRemoveOnDiscard() || !temporary.GetExcludeFromClones() ||
                    paidCost != Math.Max(0, originalCost - reduction) || player.GetEnergy() < paidCost)
                    throw new InvalidOperationException("Original first-spell discount or payment was not ready.");
                handBefore = cards.GetHand().ToArray();
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(flicker), room);
                CommonSelectionBehavior.SelectionError error = default;
                if (!cards.PlayCard(cards.GetHand().IndexOf(flicker), null, ref error))
                    throw new InvalidOperationException("Original discounted Flicker was rejected: " + error);
                return false;
            }

            var copy = cards.GetHand().Except(handBefore!).Single();
            var actual = trace.CaptureDecision();
            if (cards.GetHand().Contains(flicker) || copy.GetCardDataID() != FlickerId || !copy.HasTrait<CardTraitEphemeral>() ||
                copy.HasTrait<CardTraitCopyOnPlay>() || before!.Energy - actual.Energy != paidCost ||
                new[] { flicker, copy }.Any(card => card.GetTemporaryCardStateModifiers().GetCardUpgrades().Any(item =>
                    item.GetCardUpgradeDataId() == RelicFirstCardCostModel.Effect)))
                throw new InvalidOperationException("Native copy or first-spell discount did not settle.");
            using var archive = NativeFixtureCapture.Capture(new { Schema = 1, Boundary = "OriginalDiscountedFlicker",
                GameVersion = Application.version, GameModuleMvid = Module, VisibleDeck = visibleDeck, OriginalCost = originalCost,
                PaidCost = paidCost, CopyId = trace.CardId(copy), Action = action, Before = before, Actual = actual });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "discounted-flicker.mt2f"));
            archive.Write(stream);
            return true;
        }
    }
}
#endif
