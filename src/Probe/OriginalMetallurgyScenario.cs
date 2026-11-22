using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if DEVELOPMENT_PROBE
using System.IO;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;
using UnityEngine;
#endif
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMetallurgyScenario
    {
        internal const string Name = "original-metallurgy";
        internal const string CardId = "bdfec381-a336-4b7d-b08a-c10c7ce231c0";
        private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static int sourceId;
        private static int selectedId;
        private static bool chosenOnce;
        private static ManualLogSource? logger;
#if DEVELOPMENT_PROBE
        private const string MakingOfAMorsel = "89605a1e-342e-401d-8f54-396ec12a7ede";
        private static int growRoomId;
        internal static bool GrowRoomObservation => Enabled &&
            Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_GROW_ROOM_SMELT_OBSERVATION") == "1";
#endif

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData metallurgy = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Metallurgy CardData lookup failed.");
            CardEffectData[] effects = metallurgy.GetEffects().ToArray();
            if (effects.Length != 2) throw new InvalidOperationException("Original Metallurgy effect count changed.");
            StatusEffectStackData[] armor = effects[0].GetParamStatusEffects();
            if (metallurgy.name != "Metallurgy" || metallurgy.GetCardType() != CardType.Spell || metallurgy.GetCost() != 1 ||
                metallurgy.GetTraits().Count != 0 || metallurgy.GetCardTriggers().Count != 0 ||
                effects[0].GetEffectStateName() != "CardEffectAddStatusEffect" || effects[0].GetTargetMode() != TargetMode.Room ||
                effects[0].GetTargetTeamType() != Team.Type.Monsters || armor.Length != 1 || armor[0].statusId != "armor" || armor[0].count != 4 ||
                effects[1].GetEffectStateName() != "CardEffectChooseDiscard" || effects[1].GetTargetMode() != TargetMode.Hand ||
                effects[1].GetParamInt() != 3 || effects[1].GetAdditionalParamInt() != 5 || effects[1].GetParamBool() ||
                !effects[1].GetParamBool2() || effects[1].GetParamCardPool() != null)
                throw new InvalidOperationException("Original Metallurgy definition changed.");

            string oneCostId = OneCostId;
#if DEVELOPMENT_PROBE
            if (GrowRoomObservation)
            {
                if (typeof(CardState).Assembly.ManifestModule.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d"))
                    throw new InvalidOperationException("Original GrowRoom smelt observation requires the pinned 2.3.1 module.");
                oneCostId = MakingOfAMorsel;
            }
#endif
            CardData oneCost = all.FindCardData(oneCostId) ?? throw new InvalidOperationException("Original one-cost choice card lookup failed.");
            CardData twoCost = all.FindCardData(TwoCostId) ?? throw new InvalidOperationException("Original two-cost choice card lookup failed.");
            if (oneCost.GetCost() != 1 || twoCost.GetCost() != 2 || oneCost.GetCardType() != CardType.Spell || twoCost.GetCardType() != CardType.Spell)
                throw new InvalidOperationException("Metallurgy choice control card costs changed.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 0);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState host = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(metallurgy, CardPile.HandPile, false, false, animate: false);
            CardState retained = cards.AddNewCard(oneCost, CardPile.HandPile, false, false, animate: false);
            CardState consumed = cards.AddNewCard(twoCost, CardPile.HandPile, false, false, animate: false);
            if (host == null || spell == null || retained == null || consumed == null ||
                new[] { host, spell, retained, consumed }.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Metallurgy scenario could not add its controlled cards.");

            CardIds = new[] { host, spell, retained, consumed }.Select(FullBattleTrace.Active!.CardId).ToArray();
#if DEVELOPMENT_PROBE
            if (GrowRoomObservation)
            {
                CardData definition = all.FindCardData(GrowRoomScenario.DataId) ??
                    throw new InvalidOperationException("Original GrowRoom lookup failed.");
                CardState room = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                if (room == null || !cards.GetHand().Contains(room))
                    throw new InvalidOperationException("Could not add the original GrowRoom for the observation.");
                growRoomId = FullBattleTrace.Active!.CardId(room);
            }
#endif
            sourceId = CardIds[1];
            selectedId = CardIds[3];
            chosenOnce = false;
            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-METALLURGY-PREPARED unchanged armor-4 spell; native ConsumeAndForge choice consumes cost 2 and retains cost 1.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
#if DEVELOPMENT_PROBE
            if (GrowRoomObservation)
            {
                if (context.Statistics.Value(growRoomId, "TimesPlayed", "ThisBattle") != 0) return null;
                var attach = new PlayCardAction(growRoomId, 0);
                BattleActionResult attachment = BattleActionModel.PlayCard(source, attach);
                if (!attachment.Supported)
                    throw new InvalidOperationException("Native GrowRoom preparation is unsupported: " + attachment.Reason);
                return attach;
            }
#endif
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
            {
                var cast = new PlayCardAction(ids[1], 0,
                    recursionSelections: [new CardRecursionSelection(1, [ids[3]])]);
                BattleActionResult result = BattleActionModel.PlayCard(source, cast);
                logger?.LogInfo("ORIGINAL-METALLURGY-CANDIDATE supported=" + result.Supported + " reason=" + result.Reason);
                if (result.Supported) return cast;
            }
            return null;
        }

#if DEVELOPMENT_PROBE
        internal static void CaptureGrowRoomSmelt(AllGameManagers managers)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            var rooms = managers.GetRoomManager()!;
            if (!Prepared || save.PreviewMode || combat.IsPlacementPhase || combat.GetTurnCount() != 1)
                throw new InvalidOperationException("Expected the settled first ordinary turn of the original GrowRoom observation.");

            RoomState room = rooms.GetRoom(0), control = rooms.GetRoom(1);
            TrainRoomAttachmentState attachment = room.Attachments.Single();
            RoomModifierData modifier = attachment.UpgradeStates.Single().Item2.GetRoomModifierUpgrades().Single();
            CardState queried = managers.GetCardManager()!.GetHand().Single(card => trace.CardId(card) == CardIds[2]);
            if (!attachment.IsActive || attachment.CardState == null || trace.CardId(attachment.CardState) != growRoomId ||
                attachment.CardState.GetCardDataID() != GrowRoomScenario.DataId || control.Attachments.Count != 0 ||
                modifier.GetRoomStateModifierClassName() != "RoomStateAddEffectPostCombatModifier" ||
                queried.GetCardDataID() != MakingOfAMorsel)
                throw new InvalidOperationException("Unexpected original room, empty attachment control or queried card.");

            JToken before = JToken.FromObject(trace.CaptureDecision());
            JObject Rng() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));
            JObject rngBefore = Rng();
            int frame = Time.frameCount;
            var core = managers.GetCoreManagers();
            var rows = new[] { room, control }.SelectMany(floor => new[] { 0, 1, 2 }.Select(input => new
            {
                Room = floor.GetRoomIndex(), Input = input,
                Forge = floor.GetRoomStateModifiedForgePointsForCostStat(input, queried, core),
                Ember = floor.GetRoomStateModifiedEmberForCostStat(input, queried, core)
            })).ToArray();
            JObject rngAfter = Rng();
            JToken after = JToken.FromObject(trace.CaptureDecision());
            if (!JToken.DeepEquals(before, after) || !JToken.DeepEquals(rngBefore, rngAfter) || frame != Time.frameCount)
                throw new InvalidOperationException("Original smelt queries changed decision, RNG or frame.");

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativeOriginalGrowRoomSmeltCosts", GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Turn = combat.GetTurnCount(), CardId = queried.GetCardDataID(), CardInstanceId = trace.CardId(queried),
                AttachmentId = GrowRoomScenario.DataId, AttachmentInstanceId = growRoomId,
                Modifier = modifier.GetRoomStateModifierClassName(), FrameBefore = frame, FrameAfter = Time.frameCount,
                DecisionUnchanged = true, RngBefore = rngBefore, RngAfter = rngAfter, Decision = before, Rows = rows
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "grow-room-smelt-costs.mt2f"));
            document.Write(output);
        }
#endif

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class AutoSelectPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!Enabled || chosenOnce || sourceId <= 0 || selectedId <= 0) return true;
                __result = Select(__instance, parameters);
                return false;
            }
        }

        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters)
        {
            CardState selected = cards.GetHand().Single(card => FullBattleTrace.Active!.CardId(card) == selectedId);
            if (parameters.filterCallback(selected))
                throw new InvalidOperationException("The original Metallurgy selection policy was rejected by its native filter.");
            chosenOnce = true;
            logger!.LogInfo("ORIGINAL-METALLURGY-AUTOSELECT kept=" + selectedId + " cost=" + selected.GetCostWithoutAnyModifications());
            yield return parameters.cardChosenCallback(selected, cards);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
