using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardDuplicateScenario
    {
        internal const string Name = "card-duplicate";
        internal const string DataId = "98d0f24b-0249-463e-a4b5-5c3e3a178f49";
        internal const string RecipientId = "f15d0004-0000-4000-8000-000000000001";
        internal const string FillerId = "f15d0004-0000-4000-8000-000000000002";
        internal const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int RecipientCardId { get; private set; }
        internal static readonly List<Test> Tests = new List<Test>();
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class Test
        {
            public int SourceId { get; set; }
            public bool Returned { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public CombatContext Context { get; set; } = null!;
        }
        internal sealed class Record
        {
            public int SourceId { get; set; }
            public int SelectedId { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData all = save.GetAllGameData(); CardData steward = all.FindCardData(StewardId)!;
            Set(steward.GetSpawnCharacterData(), "attackDamage", 60); Set(steward.GetSpawnCharacterData(), "health", 100);
            Set(steward.GetSpawnCharacterData(), "size", 1); Set(steward, "cost", 0);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == StewardId)) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardData original = all.FindCardData(DataId)!;
            CardState first = cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false);
            CardActionEffect effect = BattleActionProbe.Definition(original).Effects.Single();
            void Test()
            {
                Tests.Add(new Test { SourceId = FullBattleTrace.Active!.CardId(first), Effect = effect,
                    Context = FullBattleTrace.Active.CaptureContext(), Returned = new CardEffectDuplicateCardInHand().TestEffect(
                        first.GetEffectStates()[0], new CardEffectParams { playedCard = first }, managers.GetCoreManagers()) });
            }
            Test(); // Only the played card is present, so even a matching mask cannot pass.
            CardData template = all.FindCardData("b776421c-915c-4c3d-87dc-0fe1a9e8584c")!;
            CardData recipient = UnityEngine.Object.Instantiate(template); recipient.name = "PojuDuplicateRecipient";
            Set(recipient, "id", RecipientId); Set(recipient, "cost", 0); Set(recipient, "startingUpgrades", new List<CardUpgradeData>());
            Set(recipient, "traits", new List<CardTraitData>()); Set(recipient, "triggers", new List<CardTriggerEffectData>());
            Set(recipient, "effectTriggers", new List<CharacterTriggerData>());
            if (original.GetEffects()[0].GetParamCardFilter()?.FilterCard(recipient, managers.GetRelicManager()) == false)
                throw new InvalidOperationException("Original Mass Production filter rejected the authored equipment recipient.");
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all); definitions.Add(recipient);
            CardState target = cards.AddNewCard(recipient, CardPile.HandPile, false, false, animate: false);
            RecipientCardId = FullBattleTrace.Active!.CardId(target);
            CardUpgradeData permanentData = DynamicUpgradeScenario.Upgrade("PojuDuplicatePermanent", "f15d0004-0000-4000-8000-000000000003", 3, 4, 0, 0, "armor", 0);
            Set(permanentData, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var permanent = new CardUpgradeState(); permanent.Setup(permanentData); target.ApplyPermanentUpgrade(permanent, save);
            var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2); target.ApplyTemporaryUpgrade(temporary, save);
            Test();
            CardIds = new[] { FullBattleTrace.Active.CardId(first), FullBattleTrace.Active.CardId(
                cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false)) };
            cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardData filler = UnityEngine.Object.Instantiate(recipient); filler.name = "PojuDuplicateUncopyable"; Set(filler, "id", FillerId);
            Set(filler, "cardType", CardType.Junk); Set(filler, "cost", 999);
            var infusion = new CardTraitData(); infusion.Setup("CardTraitInfusion"); Set(filler, "traits", new List<CardTraitData> { infusion });
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
            Set(filler, "effects", new List<CardEffectData> { noop }); definitions.Add(filler);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(filler, CardPile.HandPile, false, false, animate: false);
            managers.GetPlayerManager().AddEnergy(10);
            Prepared = true;
            log.LogInfo("CARD-DUPLICATE-PREPARED two unchanged Mass Productions at full hand, original mask/optional upgrade, permanent+temporary source upgrades and isolated self-only/positive Tests.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] cards, int recipient)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (int id in cards)
                if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                    return plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0 &&
                        play.RecursionSelections.Any(choice => choice.EffectIndex == 0 && choice.CardInstanceIds.SequenceEqual(new[] { recipient })));
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
        [HarmonyPatch(typeof(CardEffectDuplicateCardInHand), nameof(CardEffectDuplicateCardInHand.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState state)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new Record { SourceId = trace.CardId(state.GetParentCardState()), Before = trace.CaptureContext(),
                Effect = BattleActionProbe.DescribeTriggerEffect(state.GetSourceCardEffectData(), new List<string>()) };
            Records.Add(row); current = row;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { current = null; }
            row.After = trace.CaptureContext(); row.Completed = true;
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class AutoSelectPatch
        {
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!Enabled || current == null) return true;
                __result = Select(__instance, parameters, current); return false;
            }
        }
        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters, Record row)
        {
            CardState card = cards.GetHand().Single(item => FullBattleTrace.Active!.CardId(item) == RecipientCardId);
            if (parameters.filterCallback(card)) throw new InvalidOperationException("Original duplication rejected its selected card.");
            row.SelectedId = RecipientCardId;
            yield return parameters.cardChosenCallback(card, cards);
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
