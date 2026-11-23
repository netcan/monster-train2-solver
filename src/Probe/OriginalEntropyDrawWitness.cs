#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEntropyDrawWitness
    {
        private const string OwnerId = "87d50a3b-dade-455f-9012-04f46bac02a5";
        private const string OwnerKey = "PyreHeartEntropy_ArtifactData";
        private const string BlightId = OriginalPurgeToExhaustScenario.SelfPurgeBlightId;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<DeckChanged> Callbacks = new();
        private static string? pyreId;
        private static RelicEffectAdditionalDrawPerCardType? owner;
        private static CardState[] owned = Array.Empty<CardState>();
        private static Snapshot? deployment, firstDraw, beforePurge, afterPurge;
        private static PlayCardAction? action;
        private static bool drawnForSetup;

        internal enum Step { Wait, EndTurn, Complete }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_ENTROPY_DRAW") == "1";
        internal static bool Prepared => owner != null;

        internal static void Prepare(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.BattleIntro || save.GetMutators().Count != 0 ||
                save.GetCardCount(CardType.Blight) != 0)
                throw new InvalidOperationException("Expected the original first BattleIntro with no owned Blights or mutators.");
            var character = save.GetBalanceData().GetPyreHeartCharacterDatas()
                .Single(data => data.GetPyreHeartData().GetPyreArtifact()?.GetID() == OwnerId);
            var artifact = character.GetPyreHeartData().GetPyreArtifact();
            var definition = artifact.GetEffects().Single();
            var blight = save.GetAllGameData().FindCardData(BlightId);
            pyreId = character.GetID();
            if (character.GetAssetKey() != "PyreHeartEntropy" || !save.GetMetagameSave().HasUnlockedPyreHeart(pyreId) ||
                artifact.name != OwnerKey || definition.GetEffectClassName() != nameof(RelicEffectAdditionalDrawPerCardType) ||
                definition.GetParamCardType() != CardType.Blight || definition.GetParamInt() != 2 ||
                definition.GetEffectConditions().Count != 0 || blight == null || blight.GetCardType() != CardType.Blight ||
                !blight.GetTraits().Any(trait => trait.GetTraitStateName() == nameof(CardTraitSelfPurge)))
                throw new InvalidOperationException("Expected unlocked original Entropy and its unchanged Blight draw definition.");

            save.SetActiveSavePyreCharacter(pyreId, "", loading: false);
            var active = save.GetActiveSavePyreArtifactState();
            if (active.GetRelicDataID() != OwnerId || active.GetEffects() is not [RelicEffectAdditionalDrawPerCardType live])
                throw new InvalidOperationException("The original Entropy artifact did not initialize on its actual Pyre.");
            owner = live;
            owned = new[] { save.AddCardToDeck(blight, showAnimation: false), save.AddCardToDeck(blight, showAnimation: false) };
            if (owned[0] == owned[1] || save.GetCardCount(CardType.Blight) != 2 || owned.Any(card => !save.GetDeckState().Contains(card)))
                throw new InvalidOperationException("The original Entropy setup requires two distinct owned SelfPurgeBlights.");
        }

        internal static Step Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (!Prepared || save.PreviewMode || combat == null || cards == null ||
                rooms.GetPyreRoom().GetPyreHeart().GetSourceCharacterData().GetID() != pyreId ||
                !ReferenceEquals(owner, save.GetActiveSavePyreArtifactState().GetEffects().Single()))
                throw new InvalidOperationException("Expected the original settled Entropy Pyre.");

            switch (combat.GetTurnCount())
            {
                case 0:
                    deployment = Capture();
                    if (deployment.BlightCount != 2 || deployment.Value != 1 ||
                        deployment.StartingHandSize != save.GetBalanceData().GetStartOfTurnCards() + 1 ||
                        !Callbacks.Any(call => !call.DuringPurge && call is { BlightCount: 2, Before: 0, After: 1 }))
                        throw new InvalidOperationException("The initial Entropy draw modifier did not register for two owned Blights." +
                            " blights=" + deployment.BlightCount + " value=" + deployment.Value +
                            " handSize=" + deployment.StartingHandSize + " baseHandSize=" + save.GetBalanceData().GetStartOfTurnCards() +
                            " ownedIds=" + string.Join(",", deployment.OwnedIds) +
                            " callbacks=" + string.Join(",", Callbacks.Select(call => call.BlightCount + ":" + call.Before + ">" + call.After)));
                    save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                    return Step.EndTurn;
                case 1 when action == null:
                    firstDraw ??= Capture();
                    var selected = owned.FirstOrDefault(card => cards.GetHand().Contains(card));
                    if (selected == null)
                    {
                        selected = owned.FirstOrDefault(card => cards.GetDrawPile().Contains(card));
                        if (selected == null || !cards.DrawSpecificCard(selected))
                            throw new InvalidOperationException("Could not draw the controlled original SelfPurgeBlight.");
                        drawnForSetup = true;
                        return Step.Wait;
                    }
                    int room = rooms.GetSelectedRoom();
                    int handIndex = cards.GetHand().ToList().IndexOf(selected);
                    if (!cards.CanPlayHandCard(selected, room, null, null, null, out var error))
                        throw new InvalidOperationException("The original SelfPurgeBlight is unplayable: " + error);
                    beforePurge = Capture();
                    action = new PlayCardAction(trace.CardId(selected), room);
                    if (!cards.PlayCard(handIndex, null, ref error))
                        throw new InvalidOperationException("The original SelfPurgeBlight play failed: " + error);
                    return Step.Wait;
                case 1:
                    afterPurge = Capture();
                    var changes = Callbacks.Where(row => row.DuringPurge && row.Before != row.After).ToArray();
                    if (afterPurge.BlightCount != 1 || afterPurge.Value != 0 ||
                        afterPurge.OwnedIds.Contains(action!.CardInstanceId) ||
                        changes.Length != 1 || changes[0] is not { DuringPurge: true, BlightCount: 1, Before: 1, After: 0 })
                        throw new InvalidOperationException("The original paid purge did not remove an owned Blight and refresh Entropy.");
                    return Step.EndTurn;
                case 2:
                    var secondDraw = Capture();
                    if (deployment == null || firstDraw == null || beforePurge == null || afterPurge == null ||
                        secondDraw.BlightCount != 1 || secondDraw.Value != 0 ||
                        secondDraw.StartingHandSize != save.GetBalanceData().GetStartOfTurnCards() ||
                        firstDraw.HandIds.Length != firstDraw.StartingHandSize ||
                        secondDraw.HandIds.Length != secondDraw.StartingHandSize ||
                        firstDraw.StartingHandSize != secondDraw.StartingHandSize + 1)
                        throw new InvalidOperationException("The original next-round draw did not settle one card lower after purge.");
                    using (var archive = NativeFixtureCapture.Capture(new
                    {
                        Schema = 1, Boundary = "OriginalEntropyDrawOwnedBlight", GameVersion = Application.version,
                        GameModuleMvid = Module, OwnerId, OwnerKey, PyreId = pyreId, BlightId, CardsPerDraw = 2,
                        Deployment = deployment, FirstDraw = firstDraw, DrawnForSetup = drawnForSetup,
                        BeforePurge = beforePurge, Action = action, AfterPurge = afterPurge, SecondDraw = secondDraw,
                        DeckChanged = Callbacks
                    }))
                    using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "entropy-draw-owned-blight.mt2f")))
                        archive.Write(stream);
                    return Step.Complete;
                default:
                    throw new InvalidOperationException("Missed the settled original Entropy draw boundary.");
            }

            Snapshot Capture() => new Snapshot(trace.CaptureDecision(), save.GetCardCount(CardType.Blight), owner!.Value,
                save.GetStartOfTurnCards(), owned.Where(card => save.GetDeckState().Contains(card)).Select(trace.CardId).ToArray(),
                cards.GetHand().Select(trace.CardId).ToArray());
        }

        private sealed class Snapshot(BattleTurnState decision, int blightCount, float value, int startingHandSize,
            int[] ownedIds, int[] handIds)
        {
            public BattleTurnState Decision { get; } = decision;
            public int BlightCount { get; } = blightCount;
            public float Value { get; } = value;
            public int StartingHandSize { get; } = startingHandSize;
            public int[] OwnedIds { get; } = ownedIds;
            public int[] HandIds { get; } = handIds;
        }

        private sealed class DeckChanged(bool duringPurge, int blightCount, float before, float after)
        {
            public bool DuringPurge { get; } = duringPurge;
            public int BlightCount { get; } = blightCount;
            public float Before { get; } = before;
            public float After { get; } = after;
        }

        [HarmonyPatch(typeof(RelicEffectAdditionalDrawPerCardType), nameof(RelicEffectAdditionalDrawPerCardType.ApplyDeckChanged))]
        private static class DeckChangedPatch
        {
            private static void Prefix(RelicEffectAdditionalDrawPerCardType __instance, out float __state) =>
                __state = __instance.Value;

            private static void Postfix(RelicEffectAdditionalDrawPerCardType __instance, RelicEffectParams relicEffectParams,
                float __state)
            {
                if (Enabled && ReferenceEquals(__instance, owner) && !relicEffectParams.saveManager.PreviewMode)
                    Callbacks.Add(new DeckChanged(action != null && afterPurge == null,
                        relicEffectParams.saveManager.GetCardCount(CardType.Blight), __state, __instance.Value));
            }
        }
    }
}
#endif
