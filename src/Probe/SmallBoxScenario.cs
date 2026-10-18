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
    internal static class SmallBoxScenario
    {
        internal const string Name = "small-box";
        internal const string DataId = "f0ff6411-5c58-4b69-b025-0f6e4a52fca7";
        internal const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int CardId { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
            public bool TestReturned { get; set; }
            public bool IsolatedFullHand { get; set; }
        }
        private static bool isolatedFullHand;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData all = save.GetAllGameData(); CardData steward = all.FindCardData(StewardId)!;
            Set(steward.GetSpawnCharacterData(), "attackDamage", 60); Set(steward.GetSpawnCharacterData(), "health", 100);
            Set(steward.GetSpawnCharacterData(), "size", 1); Set(steward, "cost", 0);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == StewardId)) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState[] boxes = Enumerable.Range(0, 2).Select(_ => cards.AddNewCard(all.FindCardData(DataId)!, CardPile.HandPile, false, false, animate: false)).ToArray();
            CardIds = boxes.Select(FullBattleTrace.Active!.CardId).ToArray();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            for (int index = 0; index < 3; index++)
            {
                CardData unit = UnityEngine.Object.Instantiate(steward); unit.name = "PojuBoxedUnit" + index;
                Set(unit, "id", "f15d0007-0000-4000-8000-00000000000" + (index + 1));
                var starting = DynamicUpgradeScenario.Upgrade("PojuBoxedStarting" + index,
                    "f15d0007-0000-4000-8000-00000000001" + (index + 1), 3 + index, 4 + index, 0, 0, "armor", 0);
                Set(starting, "statusEffectUpgrades", new List<StatusEffectStackData>());
                Set(starting, "excludeFromClones", true);
                Set(unit, "startingUpgrades", new List<CardUpgradeData> { starting }); definitions.Add(unit);
                var boxed = new CardState(unit, save);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2);
                boxed.ApplyTemporaryUpgrade(temporary, save);
                boxes[1].GetCardStateModifiers().BoxCardState(save.AddBoxedCardState(boxed), save);
            }
            // Keep the full hand until after both casts: the filled box has only
            // two spaces, so all three templates shuffle but only two are copied.
            CardData filler = UnityEngine.Object.Instantiate(all.FindCardData("b776421c-915c-4c3d-87dc-0fe1a9e8584c")!);
            filler.name = "PojuBoxFiller"; Set(filler, "id", "f15d0007-0000-4000-8000-000000000020");
            Set(filler, "cardType", CardType.Junk); Set(filler, "cost", 999);
            Set(filler, "startingUpgrades", new List<CardUpgradeData>()); Set(filler, "triggers", new List<CardTriggerEffectData>());
            Set(filler, "effectTriggers", new List<CharacterTriggerData>()); Set(filler, "targetless", false); Set(filler, "targetsRoom", true);
            var infusion = new CardTraitData(); infusion.Setup("CardTraitInfusion"); Set(filler, "traits", new List<CardTraitData> { infusion });
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
            Set(filler, "effects", new List<CardEffectData> { noop }); definitions.Add(filler);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(filler, CardPile.HandPile, false, false, animate: false);
            isolatedFullHand = true;
            try
            {
                IEnumerator noSpace = new CardEffectUnboxSmallBoxUnitCard().ApplyEffect(boxes[1].GetEffectStates()[0],
                    new CardEffectParams { playedCard = boxes[1] }, managers.GetCoreManagers(), managers.GetSystemManagers());
                if (noSpace.MoveNext()) throw new InvalidOperationException("Full-hand unboxing unexpectedly yielded.");
                (noSpace as IDisposable)?.Dispose();
            }
            finally { isolatedFullHand = false; }
            managers.GetPlayerManager().AddEnergy(10); Prepared = true;
            log.LogInfo("SMALL-BOX-PREPARED original empty/filled boxes, full-hand shuffle, capacity truncation and starting/temporary upgrade inheritance.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] cards)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (int id in cards)
                if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                    return plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0);
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId);
        }
        [HarmonyPatch(typeof(CardEffectUnboxSmallBoxUnitCard), nameof(CardEffectUnboxSmallBoxUnitCard.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.TryAddCachedTemporaryCardToHand))]
        private static class CachedCardPatch
        {
            private static void Prefix(CardState cardState)
            {
                // Observe each actual copied card before the next front insertion.
                // Saved boxed templates and definition projections get no battle ID.
                if (Enabled && FullBattleTrace.Active != null) FullBattleTrace.Active.CardId(cardState);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; AllGameManagers managers = AllGameManagers.Instance!;
            var row = new Record { CardId = trace.CardId(effect.GetParentCardState()), Before = trace.CaptureContext(),
                IsolatedFullHand = isolatedFullHand, TestReturned = new CardEffectUnboxSmallBoxUnitCard().TestEffect(effect, parameters, managers.GetCoreManagers()) };
            Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureContext(); }
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
