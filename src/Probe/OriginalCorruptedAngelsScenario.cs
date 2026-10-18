using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCorruptedAngelsScenario
    {
        internal const string Name = "original-corrupted-angels";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static string? CostOrder => Environment.GetEnvironmentVariable("MT2_PROBE_ANGELS_COST_ORDER");
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<Record> Records = new();
        private static readonly List<Record> active = new();
        private static CardData carrier = null!;
        private static string role = "natural";
        private static int depth;
        internal sealed class Record
        {
            public string Role { get; set; } = "";
            public int Depth { get; set; }
            public int CardId { get; set; }
            public bool Returned { get; set; }
            public bool Expected { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public BattlePlayRules Definitions { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
            public List<DrawAttempt> Attempts { get; } = new();
        }
        internal sealed class DrawAttempt
        {
            public int CardId { get; set; }
            public bool Returned { get; set; }
            public bool Completed { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected pre-battle Corrupted Angels acquisition.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(unit, "health", 100); Set(unit, "attackDamage", 50); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!); Set(steward, "cost", 0); Set(steward, "traits", new List<CardTraitData>());
            carrier = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            carrier.name = "PojuCorruptedAngelsCarrier"; Set(carrier, "id", "ca224000-0000-4000-8000-000000000001");
            Set(carrier, "cost", 0); Set(carrier, "costType", CardData.CostType.Default);
            Set(carrier, "targetless", true); Set(carrier, "targetsRoom", true); Set(carrier, "isUnitAbility", false); Set(carrier, "isRoomAbility", false);
            Set(carrier, "traits", new List<CardTraitData>()); Set(carrier, "effects", new List<CardEffectData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>()); Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            save.GetDeckState().Clear();
            foreach (CardData card in new[] { steward, steward, steward, carrier, carrier, carrier,
                data.FindCardData(RelicPerkeoModel.SourceCardId), data.FindCardData(RelicPerkeoModel.SourceCardId),
                data.FindCardData("2785cc8e-d0e8-46c0-84ab-9c1d602525d4"), data.FindCardData("2785cc8e-d0e8-46c0-84ab-9c1d602525d4") })
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (CostOrder == "cost-first") save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicRandomDrawCostModel.DataId));
            save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicCorruptedAngelsModel.DataId));
            if (CostOrder == "cost-last") save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicRandomDrawCostModel.DataId));
            string? error = RelicModel.Validate(RelicProbe.Capture(managers)); if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-CORRUPTED-ANGELS-ACQUIRED unchanged original masks, energy and extra-draw definition.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (!Acquired || Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment.");
            CardManager cards = managers.GetCardManager()!; AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardState source = cards.AddNewCard(data.FindCardData(RelicPerkeoModel.SourceCardId), CardPile.DeckPile, false, false, animate: false)!;
            CardState junk = cards.AddNewCard(data.FindCardData("2785cc8e-d0e8-46c0-84ab-9c1d602525d4"), CardPile.DeckPile, false, false, animate: false)!;
            CardState[] normal = Enumerable.Range(0, 5).Select(_ => cards.AddNewCard(carrier, CardPile.DeckPile, false, false, animate: false)!).ToArray();
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules();
            var effect = managers.GetSaveManager().GetCollectedRelics().Single(item => item.GetRelicDataID() == RelicCorruptedAngelsModel.DataId)
                .GetEffects().OfType<RelicEffectDrawCardAndGainEmberOnDrawType>().Single();
            void Invoke(CardState? card) => effect.ApplyEffectOnCardDrawn(new CardDrawnRelicEffectParams(new RelicEffectParams
                { relicManager = managers.GetRelicManager() }) { cardState = card!, cardStatistics = managers.GetCardStatistics() }, managers.GetCoreManagers());
            void Control(string name, CardState[] draw, CardState[] discard, CardState? card)
            {
                List<CardState> deck = cards.GetDrawPile(), bin = cards.GetDiscardPile(), exhausted = cards.GetExhaustedPile();
                CardState[] savedDeck = deck.ToArray(), savedDiscard = bin.ToArray();
                CardState[] parked = savedDeck.Concat(savedDiscard).Where(item => !draw.Contains(item) && !discard.Contains(item)).ToArray();
                exhausted.AddRange(parked);
                deck.Clear(); deck.AddRange(draw); bin.Clear(); bin.AddRange(discard); role = name;
                try { Invoke(card); }
                finally
                {
                    role = "natural"; var hand = cards.GetHand();
                    foreach (CardState item in parked) exhausted.Remove(item);
                    deck.Clear(); deck.AddRange(savedDeck.Where(item => !hand.Contains(item)));
                    bin.Clear(); bin.AddRange(savedDiscard.Where(item => !hand.Contains(item)));
                }
            }
            if (CostOrder != null)
            {
                CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuAngelsPending", "ca224000-0000-4000-8000-000000000003", 0, 0, -1, 0, "armor", 0);
                upgrade.GetStatusEffectUpgrades().Clear();
                var state = new CardUpgradeState(); state.Setup(upgrade);
                cards.AddTempCardUpgradeToNextDrawnCard(state);
            }
            Control("draw-priority", new[] { junk, normal[0] }, new[] { normal[1] }, source);
            Control("discard-fallback", new[] { junk }, new[] { normal[1] }, junk);
            Control("no-eligible", new[] { source }, new[] { junk }, source);
            Control("non-primary", new[] { normal[2] }, new[] { normal[3] }, normal[0]);
            CardData recursive = UnityEngine.Object.Instantiate(carrier);
            recursive.name = "PojuCorruptedAngelsRecursiveJunk";
            Set(recursive, "id", "ca224000-0000-4000-8000-000000000002"); Set(recursive, "cardType", CardType.Junk);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(recursive);
            CardState[] chain = Enumerable.Range(0, 2).Select(_ => cards.AddNewCard(recursive, CardPile.DeckPile, false, false, animate: false)!).ToArray();
            FullBattleTrace.Active.InvalidateCardRules();
            Control("recursive-candidate", new[] { normal[2], chain[0], chain[1] }, Array.Empty<CardState>(), source);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
            Control("full-hand", new[] { normal[3] }, new[] { normal[4] }, source);
            managers.GetPlayerManager().AddEnergy(100);
            Control("energy-cap", new[] { normal[3] }, new[] { normal[4] }, junk);
            Control("null-filter", new[] { normal[3] }, Array.Empty<CardState>(), null);
            if (CostOrder != null) ((List<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextDrawnTempCardUpgrades").GetValue(cards)).Clear();
            FullBattleTrace.Active.InvalidateCardRules();
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        [HarmonyPatch(typeof(RelicEffectDrawCardAndGainEmberOnDrawType), nameof(RelicEffectDrawCardAndGainEmberOnDrawType.ApplyEffectOnCardDrawn))]
        private static class ApplyPatch
        {
            private static void Prefix(RelicEffectDrawCardAndGainEmberOnDrawType __instance, CardDrawnRelicEffectParams relicEffectParams,
                out Record? __state)
            {
                __state = null;
                if (!Prepared || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    __instance.SourceRelicState?.GetRelicDataID() != RelicCorruptedAngelsModel.DataId) return;
                var row = new Record { Role = role, Depth = depth++ }; Records.Add(row); active.Add(row); __state = row;
                try
                {
                    FullBattleTrace trace = FullBattleTrace.Active;
                    row.CardId = relicEffectParams.cardState == null ? 0 : trace.CardId(relicEffectParams.cardState);
                    row.Before = trace.CaptureTrain(); row.Definitions = trace.CaptureDecision().PlayRules!;
                    var result = RelicCorruptedAngelsModel.Apply(row.Before.Context!, row.CardId, out bool expected, row.Definitions);
                    row.Expected = expected; row.UnsupportedReason = result.UnsupportedReason;
                    if (result.Supported) row.Predicted = CardSpellModel.WithContext(row.Before, result.Context);
                }
                catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, Record? __state)
            {
                if (__state == null) return;
                depth--; active.RemoveAt(active.Count - 1);
                try
                {
                    __state.Returned = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain(); __state.Completed = true;
                    if (__result != __state.Expected) __state.Difference = "Corrupted Angels callback result differs.";
                    if (__state.Predicted != null && !JToken.DeepEquals(JToken.FromObject(__state.Predicted), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Corrupted Angels complete state differs.";
                }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.DrawSpecificCard))]
        private static class DrawPatch
        {
            private static void Prefix(CardState drawCard, out DrawAttempt? __state)
            {
                __state = null;
                if (active.Count == 0) return;
                __state = new DrawAttempt { CardId = FullBattleTrace.Active!.CardId(drawCard) };
                active[active.Count - 1].Attempts.Add(__state);
            }
            private static void Postfix(bool __result, DrawAttempt? __state)
            { if (__state != null) { __state.Returned = __result; __state.Completed = true; } }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
