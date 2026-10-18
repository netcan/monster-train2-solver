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
    internal static class MixalotScenario
    {
        internal const string Name = "mixalot";
        internal const string DataId = "a68aa4ef-ff56-4d3f-b760-ddb7b2bbe207";
        internal const string FillerId = "f15d0006-0000-4000-8000-000000000001";
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
            public ElixirCatalog Catalog { get; set; } = null!;
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
            CardData original = all.FindCardData(DataId)!;
            CardState[] spells = Enumerable.Range(0, 3).Select(index => cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false)).ToArray();
            CardIds = spells.Select(FullBattleTrace.Active!.CardId).ToArray();
            var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); cards.AddTemporaryTraitToCard(spells[2], freeze);
            cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardData filler = UnityEngine.Object.Instantiate(all.FindCardData("b776421c-915c-4c3d-87dc-0fe1a9e8584c")!);
            filler.name = "PojuMixalotFiller"; Set(filler, "id", FillerId); Set(filler, "cardType", CardType.Junk); Set(filler, "cost", 999);
            Set(filler, "startingUpgrades", new List<CardUpgradeData>()); Set(filler, "triggers", new List<CardTriggerEffectData>());
            Set(filler, "effectTriggers", new List<CharacterTriggerData>()); Set(filler, "targetless", false); Set(filler, "targetsRoom", true);
            var infusion = new CardTraitData(); infusion.Setup("CardTraitInfusion"); Set(filler, "traits", new List<CardTraitData> { infusion });
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
            Set(filler, "effects", new List<CardEffectData> { noop });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(filler);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(filler, CardPile.HandPile, false, false, animate: false);
            isolatedFullHand = true;
            try
            {
                IEnumerator refusal = new CardEffectMixRandom().ApplyEffect(spells[0].GetEffectStates()[0], new CardEffectParams { playedCard = spells[0] },
                    managers.GetCoreManagers(), managers.GetSystemManagers());
                if (refusal.MoveNext()) throw new InvalidOperationException("Full-hand random mixing unexpectedly yielded a generation operation.");
                (refusal as IDisposable)?.Dispose();
            }
            finally { isolatedFullHand = false; }
            managers.GetPlayerManager().AddEnergy(10); Prepared = true;
            log.LogInfo("MIXALOT-PREPARED three original casts; full-hand no-op, fresh/existing/fresh recipes and previous-recipe exclusion.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] cards)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            var steward = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == StewardId);
            if (steward != null) return steward;
            for (int index = 0; index < 2; index++)
                if (context.Statistics!.Value(cards[index], "TimesPlayed", "ThisBattle") == 0)
                    return plays.FirstOrDefault(play => play.CardInstanceId == cards[index] && play.RoomIndex == 0);
            bool hasElixir = context.Cards.Hand.Any(card => source.PlayRules!.Elixirs!.Recipes.Any(recipe => recipe.Creation.DataId == card.DataId));
            if (context.Statistics!.Value(cards[2], "TimesPlayed", "ThisBattle") == 0)
            {
                if (hasElixir) return plays.FirstOrDefault(play => play.RoomIndex == 0 && source.PlayRules!.Elixirs!.Recipes.Any(recipe =>
                    recipe.Creation.DataId == context.FindCard(play.CardInstanceId)!.DataId));
                return plays.FirstOrDefault(play => play.CardInstanceId == cards[2] && play.RoomIndex == 0);
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId);
        }
        [HarmonyPatch(typeof(CardEffectMixRandom), nameof(CardEffectMixRandom.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; AllGameManagers managers = AllGameManagers.Instance!;
            var row = new Record { CardId = trace.CardId(effect.GetParentCardState()), Before = trace.CaptureContext(),
                Catalog = ElixirCatalogProbe.Capture(managers), IsolatedFullHand = isolatedFullHand,
                TestReturned = new CardEffectMixRandom().TestEffect(effect, parameters, managers.GetCoreManagers()) };
            Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureContext(); }
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
