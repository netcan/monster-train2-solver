#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStardustHealDrawWitness
    {
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private const string StardustId = CardDuplicateScenario.StardustId;
        private const string HealRelicId = OriginalMoonPhaseHealRelicScenario.DataId;
        private const string DrawRelicId = OriginalCardDrawOnHealScenario.OwnerId;
        private const string StewardId = CardDuplicateScenario.StewardId;
        private const string ExistingSpellId = "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84"; // Witchweave
        private const string DrawnSpellId = "24a256cd-715e-4423-b2e9-c0925b285fd4"; // Bloody Moon
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_STARDUST_HEAL_DRAW") == "1";
        private static CardState? stewardCard, stardustCard, existingCard, drawnCard;
        private static bool turnAdvanced, armed;
        private static int stewardUnitId, selectedId;
        private static int[] offeredIds = Array.Empty<int>();
        private static CombatContext? beforeDuplication;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static string healRelicClan = "", drawRelicClan = "", healRelicDlc = "", drawRelicDlc = "";
        private static bool modelDirectSupported, modelExistingOffered, modelDrawnOffered;
        private static string? modelDirectReason;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0 || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected the pinned original Luna opening before battle.");
            string main = save.GetStartingConditions().ClassId;
            CardData stardust = save.GetAllGameData().FindCardData(StardustId)
                ?? throw new InvalidOperationException("Original Stardust Invocation is missing.");
            if (stardust.name != "StardustInvocation" || stardust.GetLinkedClassID() != main ||
                !save.IsDlcAvailableWhenStartingRun(stardust.GetRequiredDLC()))
                throw new InvalidOperationException("Expected an available original Luna Stardust Invocation.");
            CollectableRelicData Owner(string id)
            {
                CollectableRelicData owner = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (!save.IsDlcAvailableWhenStartingRun(owner.GetRequiredDLC()))
                    throw new InvalidOperationException("Original relic DLC is unavailable: " + owner.name);
                return owner;
            }
            CollectableRelicData heal = Owner(HealRelicId), draw = Owner(DrawRelicId);
            healRelicClan = heal.GetLinkedClassID() ?? "";
            drawRelicClan = draw.GetLinkedClassID() ?? "";
            healRelicDlc = heal.GetRequiredDLC().ToString();
            drawRelicDlc = draw.GetRequiredDLC().ToString();
            OriginalMoonPhaseHealRelicScenario.AcquireBeforeBattle(managers, log);
            OriginalCardDrawOnHealScenario.AcquireBeforeBattle(managers, log);
            log.LogInfo("ORIGINAL-STARDUST-HEAL-DRAW-ACQUIRED main=" + main + " secondary=" +
                save.GetStartingConditions().SubclassId + " healClan=" + healRelicClan + " drawClan=" + drawRelicClan);
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager();
            CardManager cards = managers.GetCardManager();
            RoomManager rooms = managers.GetRoomManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0 ||
                !OriginalMoonPhaseHealRelicScenario.Acquired || !OriginalCardDrawOnHealScenario.Acquired)
                throw new InvalidOperationException("Expected both available unchanged original relics in a Luna battle.");
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            if (combat.IsPlacementPhase)
            {
                if (stewardCard == null)
                {
                    CardData steward = Definition(StewardId, "TrainStewardShield");
                    stewardCard = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false)
                        ?? throw new InvalidOperationException("Could not add the unchanged original Steward.");
                    SpawnPoint point = rooms.GetRoom(0).GetFirstEmptyMonsterPoint()
                        ?? throw new InvalidOperationException("Original Steward has no native placement point.");
                    if (!cards.CanPlayHandCard(stewardCard, 0, point, null, null, out var error) ||
                        !cards.PlayCard(cards.GetHand().IndexOf(stewardCard), point, ref error))
                        throw new InvalidOperationException("Original Steward deployment was rejected: " + error);
                }
                else if (!turnAdvanced)
                {
                    turnAdvanced = true;
                    advanceTurn();
                }
                return false;
            }
            if (combat.GetTurnCount() != 1 || stewardCard == null || !turnAdvanced)
                throw new InvalidOperationException("Expected the first paid Luna turn after Steward deployment.");
            if (before == null)
            {
                var allies = new List<CharacterState>();
                rooms.GetRoom(0).AddCharactersToList(allies, Team.Type.Monsters);
                CharacterState steward = allies.Single(unit => unit.IsAlive && ReferenceEquals(unit.GetSpawnerCard(), stewardCard));
                stewardUnitId = trace.UnitId(steward);
                int max = steward.GetMaxHP();
                if (max < 2) throw new InvalidOperationException("Original Steward cannot carry a nonlethal wound.");
                steward.SetHealth(max - 1, max);
                foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
                stardustCard = Add(StardustId, "StardustInvocation", CardPile.HandPile);
                existingCard = Add(ExistingSpellId, "Witchweave", CardPile.HandPile);
                drawnCard = Add(DrawnSpellId, "BloodyMoon", CardPile.DeckPileTop);
                AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
                before = trace.CaptureDecision();
                int sourceId = trace.CardId(stardustCard), existingId = trace.CardId(existingCard), drawnId = trace.CardId(drawnCard);
                CombatContext context = before.Spawn.Train.Context!;
                if (before.Spawn.Turn != 1 || before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == stewardUnitId).Health != max - 1 ||
                    context.Cards.Hand.Any(card => card.InstanceId == drawnId) ||
                    !context.Cards.Hand.Any(card => card.InstanceId == existingId) ||
                    !context.Cards.Draw.Any(card => card.InstanceId == drawnId) ||
                    context.Cards.Hand.Count > context.MaxHandSize - 1 ||
                    context.QueryFrame?.MoonPhaseTrackingActive != true ||
                    !context.Relics!.Any(owner => owner.DataId == HealRelicId) ||
                    !context.Relics.Any(owner => owner.DataId == DrawRelicId))
                    throw new InvalidOperationException("Original turn-one heal/draw/duplicate input is incomplete.");
                action = new PlayCardAction(sourceId, 0, recursionSelections:
                    new[] { new CardRecursionSelection(1, new[] { drawnId }) });
                PlayCardAction[] enumerated = BattleActionModel.EnumerateSupportedPlays(before)
                    .Where(play => play.CardInstanceId == sourceId).ToArray();
                modelExistingOffered = enumerated.Any(play => Selected(play, existingId));
                modelDrawnOffered = enumerated.Any(play => Selected(play, drawnId));
                BattleActionResult model = BattleActionModel.PlayCard(before, action);
                modelDirectSupported = model.Supported;
                modelDirectReason = model.Reason;
                if (!cards.CanPlayHandCard(stardustCard, 0, null, null, null, out var error))
                    throw new InvalidOperationException("Original Stardust was rejected by native card play: " + error);
                armed = true;
                if (!cards.PlayCard(cards.GetHand().IndexOf(stardustCard), null, ref error))
                    throw new InvalidOperationException("Original Stardust was rejected by native card play: " + error);
                return false;
            }

            BattleTurnState actual = trace.CaptureDecision();
            int drawnRecipientId = trace.CardId(drawnCard!);
            int existingRecipientId = trace.CardId(existingCard!);
            int copyId = actual.Spawn.Train.Context!.Cards.Hand
                .Where(card => card.DataId == DrawnSpellId && card.InstanceId != drawnRecipientId)
                .Select(card => card.InstanceId).SingleOrDefault();
            bool healed = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == stewardUnitId).Health ==
                before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == stewardUnitId).MaxHealth;
            bool originalChoice = selectedId == drawnRecipientId && offeredIds.Contains(existingRecipientId) &&
                offeredIds.Contains(drawnRecipientId) && beforeDuplication?.Cards.Hand.Any(card => card.InstanceId == drawnRecipientId) == true;
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalStardustHealDraw", GameVersion = Application.version,
                GameModuleMvid = Module, StardustId, HealRelicId, DrawRelicId, StewardId,
                ExistingSpellId, DrawnSpellId, HealRelicClan = healRelicClan, DrawRelicClan = drawRelicClan,
                HealRelicDlc = healRelicDlc, DrawRelicDlc = drawRelicDlc,
                PrimaryClan = save.GetStartingConditions().ClassId, SecondaryClan = save.GetStartingConditions().SubclassId,
                CarrierCardId = trace.CardId(stewardCard), CarrierUnitId = stewardUnitId,
                StardustCardId = trace.CardId(stardustCard!), ExistingRecipientId = existingRecipientId,
                DrawnRecipientId = drawnRecipientId, CopyCardId = copyId,
                OfferedRecipientIds = offeredIds, NativeSelectedId = selectedId, BeforeDuplication = beforeDuplication,
                Healed = healed, OriginalChoice = originalChoice, ModelDirectSupported = modelDirectSupported,
                ModelDirectReason = modelDirectReason, ModelExistingOffered = modelExistingOffered,
                ModelDrawnOffered = modelDrawnOffered, Before = before, Action = action, Actual = actual
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "stardust-heal-draw.mt2f"))) archive.Write(stream);
            if (!healed || !originalChoice || copyId <= 0)
                throw new InvalidOperationException("Native Stardust did not draw and copy the phase-healed spell.");
            log.LogInfo("ORIGINAL-STARDUST-HEAL-DRAW copied drawn=" + drawnRecipientId + " as=" + copyId +
                " modelDirect=" + modelDirectSupported + " existingOffered=" + modelExistingOffered +
                " drawnOffered=" + modelDrawnOffered);
            return true;

            CardData Definition(string id, string key)
            {
                CardData definition = save.GetAllGameData().FindCardData(id)
                    ?? throw new InvalidOperationException("Missing original card " + key);
                if (definition.name != key || !save.IsDlcAvailableWhenStartingRun(definition.GetRequiredDLC()))
                    throw new InvalidOperationException("Original card is changed or unavailable: " + key);
                return definition;
            }
            CardState Add(string id, string key, CardPile pile) => cards.AddNewCard(Definition(id, key), pile,
                false, false, animate: false) ?? throw new InvalidOperationException("Could not add original " + key);
        }

        private static bool Selected(PlayCardAction play, int id) => play.RecursionSelections.Any(selection =>
            selection.EffectIndex == 1 && selection.CardInstanceIds.SequenceEqual(new[] { id }));

        [HarmonyPatch(typeof(CardEffectDuplicateCardInHand), nameof(CardEffectDuplicateCardInHand.ApplyEffect))]
        private static class DuplicatePatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(CardEffectState cardEffectState, ref IEnumerator __result)
            {
                if (armed && ReferenceEquals(cardEffectState.GetParentCardState(), stardustCard) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result);
            }
        }

        private static IEnumerator Observe(IEnumerator native)
        {
            beforeDuplication = FullBattleTrace.Active!.CaptureContext();
            while (native.MoveNext()) yield return native.Current;
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class SelectionPatch
        {
            private static bool Prepare() => Enabled;
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!armed || parameters.selectionHandState != CardUI.CardUIState.HandDuplicateCard) return true;
                __result = Select(__instance, parameters);
                return false;
            }
        }

        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            offeredIds = cards.GetHand().Where(card => !parameters.filterCallback(card)).Select(trace.CardId).ToArray();
            CardState selected = cards.GetHand().Single(card => ReferenceEquals(card, drawnCard));
            selectedId = trace.CardId(selected);
            if (!offeredIds.Contains(selectedId))
                throw new InvalidOperationException("Original hand duplicate filter rejected the newly drawn spell.");
            yield return parameters.cardChosenCallback(selected, cards);
        }
    }
}
#endif
