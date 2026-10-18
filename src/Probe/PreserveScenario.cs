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
    internal static class PreserveScenario
    {
        internal const string Name = "preserve";
        internal const string DataId = "3ca50b9d-9c52-49c8-884f-f4409a4489c9";
        internal const string JunkId = "f15d0003-0000-4000-8000-000000000001";
        internal const string SpellId = "f15d0003-0000-4000-8000-000000000002";
        internal const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int JunkCardId { get; private set; }
        internal static int SpellCardId { get; private set; }
        internal static CombatContext FilterContext { get; private set; } = null!;
        internal static readonly List<FilterTest> Filters = new List<FilterTest>();
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class FilterTest
        {
            public string Name { get; set; } = "";
            public int CardId { get; set; }
            public int SourceId { get; set; }
            public bool Rejected { get; set; }
        }
        internal sealed class Record
        {
            public int SourceId { get; set; }
            public int? SelectedId { get; set; }
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
            cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardData template = all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility());
            CardState Control(string id, string name, CardType type, int cost, bool purge)
            {
                CardData data = UnityEngine.Object.Instantiate(template); data.name = name; Set(data, "id", id);
                Set(data, "cardType", type); Set(data, "cost", cost); Set(data, "costType", CardData.CostType.Default);
                Set(data, "targetless", false); Set(data, "targetsRoom", true);
                var traits = new List<CardTraitData>();
                if (purge) { var trait = new CardTraitData(); trait.Setup("CardTraitSelfPurge"); traits.Add(trait); }
                Set(data, "traits", traits); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None);
                noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
                Set(data, "effects", new List<CardEffectData> { noop });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
                return cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            }
            CardState junk = Control(JunkId, "PojuPreserveJunk", CardType.Junk, 999, false);
            CardState spell = Control(SpellId, "PojuPreservePlayedSpell", CardType.Spell, 0, true);
            JunkCardId = FullBattleTrace.Active!.CardId(junk); SpellCardId = FullBattleTrace.Active.CardId(spell);
            CardState[] originals = Enumerable.Range(0, 3).Select(_ => cards.AddNewCard(all.FindCardData(DataId),
                CardPile.HandPile, false, false, animate: false)).ToArray();
            CardIds = originals.Select(FullBattleTrace.Active.CardId).ToArray();
            var native = new CardEffectFreezeCard();
            void Filter(string name, CardState card, CardState source)
            {
                Filters.Add(new FilterTest { Name = name, CardId = FullBattleTrace.Active.CardId(card),
                    SourceId = FullBattleTrace.Active.CardId(source), Rejected = (bool)AccessTools.Method(typeof(CardEffectFreezeCard), "CardFilterFunc")
                        .Invoke(native, new object[] { card, source }) });
            }
            Filter("ordinary", spell, originals[0]); Filter("self", spell, spell);
            Filter("junk", junk, originals[0]); Filter("already-frozen", originals[1], originals[0]);
            FilterContext = FullBattleTrace.Active.CaptureContext();
            Prepared = true;
            log.LogInfo("PRESERVE-PREPARED three unchanged originals; explicit Junk/spell selections, empty eligible hand, self/frozen filters, retained frozen Junk and thaw-on-play controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CombatContext context = source.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            var unit = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == StewardId);
            if (unit != null) return unit;
            for (int index = 0; index < 2; index++)
                if (context.Statistics!.Value(CardIds[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int selected = index == 0 ? JunkCardId : SpellCardId;
                    return plays.FirstOrDefault(play => play.CardInstanceId == CardIds[index] && play.RoomIndex == 0 &&
                        play.RecursionSelections.Any(selection => selection.EffectIndex == 0 && selection.CardInstanceIds.SequenceEqual(new[] { selected })));
                }
            var spell = plays.FirstOrDefault(play => play.CardInstanceId == SpellCardId && play.RoomIndex == 0);
            if (spell != null) return spell;
            if (context.Statistics!.Value(CardIds[2], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == CardIds[2] && play.RoomIndex == 0 && play.RecursionSelections.Count == 0);
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId);
        }
        [HarmonyPatch(typeof(CardEffectFreezeCard), nameof(CardEffectFreezeCard.ApplyEffect))]
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
            var row = new Record { SourceId = trace.CardId(state.GetParentCardState()), Before = trace.CaptureContext() };
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
                int chosen = current.SourceId == CardIds[0] ? JunkCardId : current.SourceId == CardIds[1] ? SpellCardId : 0;
                if (chosen == 0) throw new InvalidOperationException("The empty-hand Preserve control unexpectedly opened selection UI.");
                __result = Select(__instance, parameters, chosen, current); return false;
            }
        }
        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters, int id, Record row)
        {
            CardState card = cards.GetHand().Single(item => FullBattleTrace.Active!.CardId(item) == id);
            if (parameters.filterCallback(card)) throw new InvalidOperationException("Original Preserve rejected its selected card.");
            row.SelectedId = id;
            yield return parameters.cardChosenCallback(card, cards);
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
