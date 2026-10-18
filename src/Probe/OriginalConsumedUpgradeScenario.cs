using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalConsumedUpgradeScenario
    {
        internal const string Name = "original-consumed-upgrade";
        internal const string DraffId = "3b74c955-fd58-4b60-81a7-e2d6a0aaa2d4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Samples = new List<object>();
        internal static readonly List<object> Queries = new List<object>();
        private static bool querying;
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal sealed class Sample
        {
            public int CardId { get; set; }
            public int SourceCardId { get; set; }
            public int TargetCardId { get; set; }
            public CardUpgradeModifier Input { get; set; } = null!;
            public CardUpgradeModifier? Actual { get; set; }
            public bool Returned { get; set; }
            public CombatContext Context { get; set; } = null!;
            public CombatContext? After { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardConsumedUpgradeModel.CardId), Add(DraffId), Add(DraffId), Add(DraffId) };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CONSUMED-UPGRADE-PREPARED unchanged Sacrificial Ressurection and Draff, consume three then zero on the same source, fresh original upgrades and restored native counter/null-source controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int anchor = Array.FindIndex(ids.Take(2).ToArray(), id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            int cardId, room;
            if (anchor >= 0) { cardId = ids[anchor]; room = anchor; }
            else if (source.Spawn.Turn < 1) return null;
            else if (context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 0) { cardId = ids[2]; room = 0; }
            else if (source.Spawn.Turn == 2 && context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 1)
            {
                CardToken? remaining = context.Cards.Hand.FirstOrDefault(card => card.InstanceId != ids[2]);
                cardId = remaining?.InstanceId ?? ids[2]; room = 0;
            }
            else return null;
            var action = new PlayCardAction(cardId, room, playerPosition: context.FindCard(cardId)!.MaskDescriptor!.Definition.CardType == "Monster" ? 0 : -1);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-CONSUMED-UPGRADE-REJECT card=" + cardId + " reason=" + result.Reason); }
            return null;
        }
        [HarmonyPatch(typeof(CardTraitScalingByConsumedTempCardUpgradeToCard), nameof(CardTraitScalingByConsumedTempCardUpgradeToCard.OnCardBeingUpgraded))]
        private static class ScalePatch
        {
            private static void Prefix(CardTraitScalingByConsumedTempCardUpgradeToCard __instance, CardState cardState, CardState upgradingCardSource,
                CardUpgradeState cardUpgradeState, out Sample? __state)
            {
                __state = null;
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardConsumedUpgradeModel.CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                __state = new Sample { CardId = trace.CardId(__instance.GetCard()), SourceCardId = trace.CardId(upgradingCardSource), TargetCardId = trace.CardId(cardState),
                    Input = CardModifierProbe.Upgrade(cardUpgradeState), Context = trace.CaptureContext() };
                Samples.Add(__state);
            }
            private static void Postfix(CardTraitScalingByConsumedTempCardUpgradeToCard __instance, CardState cardState, CardState upgradingCardSource,
                CardUpgradeState cardUpgradeState, bool __result, Sample? __state)
            {
                if (__state == null) return;
                FullBattleTrace trace = FullBattleTrace.Active!; __state.Actual = CardModifierProbe.Upgrade(cardUpgradeState);
                __state.Returned = __result; __state.After = trace.CaptureContext();
                // Restore the private source counter after each detached fresh-upgrade query.
                if (Queries.Count != 0) return;
                CardEffectDiscardHand effect = upgradingCardSource.GetEffectStates().Select(state => state.GetCardEffect()).OfType<CardEffectDiscardHand>().Single();
                int saved = effect.GetNumCardsConsumed(); querying = true;
                try
                {
                    foreach (int consumed in new[] { 0, 1, 3, 7, -1 })
                    {
                        CombatContext original = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                        Set(effect, "numConsumed", Math.Max(0, consumed));
                        var fresh = new CardUpgradeState(); fresh.Setup(AllGameManagers.Instance!.GetSaveManager().GetAllGameData().FindCardData(CardConsumedUpgradeModel.CardId)!.GetEffects()[1].GetParamCardUpgradeData());
                        CardUpgradeModifier input = CardModifierProbe.Upgrade(fresh); CombatContext before = trace.CaptureContext();
                        bool returned = __instance.OnCardBeingUpgraded(cardState, consumed == -1 ? null : upgradingCardSource, fresh, AllGameManagers.Instance.GetCoreManagers());
                        Set(effect, "numConsumed", saved);
                        Queries.Add(new { CardId = __state.CardId, SourceCardId = consumed == -1 ? 0 : __state.SourceCardId, Consumed = Math.Max(0, consumed),
                            Input = input, Actual = CardModifierProbe.Upgrade(fresh), Returned = returned, Context = before,
                            StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(original), JToken.FromObject(trace.CaptureContext())) });
                    }
                }
                finally { Set(effect, "numConsumed", saved); querying = false; }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
