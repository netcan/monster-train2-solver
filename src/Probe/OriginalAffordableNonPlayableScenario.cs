#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAffordableNonPlayableScenario
    {
        internal const string Name = "original-affordable-nonplayable";
        internal const string Boundary = "NativeOriginalAffordableNonPlayable";
        private const string CardDataId = "0127e3c2-b60d-4ac3-acf7-6395d44eb5ef";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static CardState? card;
        private static object? unavailable;
        private static BattleTurnState? affordable;
        private static bool accepted;
        private static string? selectionError;

        internal static void Tick(AllGameManagers managers, ManualLogSource log)
        {
            if (Completed || Error != null) return;
            try
            {
                CardManager cards = managers.GetCardManager()!;
                PlayerManager player = managers.GetPlayerManager();
                SaveManager save = managers.GetSaveManager();
                FullBattleTrace trace = FullBattleTrace.Active!;
                const int floor = 0;
                SpawnPoint point = managers.GetRoomManager()!.GetRoom(floor).GetMonsterPoint(0);
                CardData original = save.GetAllGameData().FindCardData(CardDataId);
                if (original.GetAssetKey() != "ShatteringDarkshard" || original.GetCostType() != CardData.CostType.NonPlayable ||
                    original.GetCost() != 1 || managers.GetCombatManager()!.GetTurnCount() != 0)
                    throw new InvalidOperationException("Expected the unchanged original Shattering Darkshard at deployment.");
                if (card == null)
                {
                    save.SetForgePoints(0);
                    player.RemoveEnergy(player.GetEnergy());
                    card = cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false)
                        ?? throw new InvalidOperationException("Shattering Darkshard failed to enter the original native hand.");
                    trace.InvalidateCardRules();
                    return;
                }

                int cardId = trace.CardId(card);
                int handIndex = cards.GetHand().IndexOf(card);
                if (unavailable == null)
                {
                    BattleTurnState before = trace.CaptureDecision();
                    bool canPlay = cards.CanPlayHandCard(card, floor, point, null, null, out var error);
                    unavailable = new { Before = before, CanPlay = canPlay, SelectionError = error.ToString(), Actual = trace.CaptureDecision() };
                    player.AddEnergy(1);
                    return;
                }
                if (affordable == null)
                {
                    affordable = trace.CaptureDecision();
                    if (cards.CanPlayHandCard(card, floor, point, null, null, out var error))
                        accepted = cards.PlayCard(handIndex, point, ref error);
                    selectionError = error.ToString();
                    return;
                }

                using var document = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary, GameVersion = UnityEngine.Application.version,
                    GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                    CardId = cardId, DataId = CardDataId, OriginalCost = original.GetCost(), CostType = original.GetCostType().ToString(),
                    Unavailable = unavailable, Affordable = new { Before = affordable, Accepted = accepted, SelectionError = selectionError,
                        Action = new PlayCardAction(cardId, floor, 0), Actual = trace.CaptureDecision() }
                });
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "affordable-nonplayable.mt2f"));
                document.Write(stream);
                Completed = true;
                log.LogInfo("ORIGINAL-AFFORDABLE-NONPLAYABLE accepted=" + accepted + " selection=" + selectionError);
            }
            catch (Exception error)
            {
                Error = error.ToString();
                log.LogError(Error);
            }
        }
    }
}
#endif
