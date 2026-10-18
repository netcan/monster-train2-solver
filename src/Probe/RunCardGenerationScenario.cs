using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class RunCardGenerationScenario
    {
        internal const string Name = "run-card-generation";
        internal const string FirstId = "66189a7b-2593-4b08-aafd-f1bdae7e3c2a";
        internal const string SecondId = "0130a4a0-2269-4003-97e1-58d1f5ce3bd8";
        internal const string ThirdId = "c778fab6-585a-40ea-9f26-c460b8b82eae";
        internal const string ControlId = "f15d0001-0000-4000-8000-000000000001";
        internal const string EphemeralId = "f15d0001-0000-4000-8000-000000000003";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData all = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData steward = all.FindCardData(owned.First(card => all.FindCardData(card.GetCardDataID())
                .GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).GetCardDataID())!;
            Set(steward.GetSpawnCharacterData(), "attackDamage", 50); Set(steward.GetSpawnCharacterData(), "health", 100);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardData control = UnityEngine.Object.Instantiate(all.FindCardData(FirstId));
            control.name = "PojuRunCardGenerationControl"; Set(control, "id", ControlId); Set(control, "cost", 0);
            CardPool pool = Pool("PojuRunCardGenerationPool", steward);
            CardPool empty = Pool("PojuRunCardGenerationEmpty");
            var effects = new List<CardEffectData>();
            // Playing the controller removes it from hand before effects begin.
            // Fill that vacancy so the following permanent addition really overflows.
            var fill = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None);
            fill.Cheat_SetTargetMode(TargetMode.Room); Set(fill, "paramInt", (int)CardPile.HandPile);
            Set(fill, "paramCardPool", pool); Set(fill, "additionalParamInt", 1); effects.Add(fill);
            foreach (CardPile pile in new[] { CardPile.HandPile, CardPile.DiscardPile, CardPile.DeckPile,
                CardPile.DeckPileTop, CardPile.DeckPileRandom })
            {
                var effect = new CardEffectData("CardEffectAddRunCard", null!, Team.Type.None);
                effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", (int)pile);
                Set(effect, "paramCardPool", pool); Set(effect, "copyModifiersFromSource", true);
                // These AddBattleCard parameters are ignored by AddRunCard.
                Set(effect, "additionalParamInt", 9); Set(effect, "ignoreTemporaryModifiersFromSource", true);
                effects.Add(effect);
            }
            var noPool = new CardEffectData("CardEffectAddRunCard", null!, Team.Type.None);
            noPool.Cheat_SetTargetMode(TargetMode.Room); Set(noPool, "paramInt", (int)CardPile.DeckPileRandom);
            Set(noPool, "paramCardPool", empty); effects.Add(noPool);
            Set(control, "effects", effects);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(control);
            CardState controller = cards.AddNewCard(control, CardPile.HandPile, false, false, animate: false);
            CardUpgradeData definition = DynamicUpgradeScenario.Upgrade("PojuRunCardGenerationPermanent",
                "f15d0001-0000-4000-8000-000000000002", 3, 4, 0, 0, "armor", 0);
            Set(definition, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var permanent = new CardUpgradeState(); permanent.Setup(definition); controller.ApplyPermanentUpgrade(permanent, save);
            var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2);
            controller.ApplyTemporaryUpgrade(temporary, save);
            CardData original = all.FindCardData(FirstId)!;
            cards.AddNewCard(original, CardPile.HandPile, false, false, animate: false);
            CardData ephemeral = UnityEngine.Object.Instantiate(original); ephemeral.name = "PojuRunCardEphemeralControl";
            Set(ephemeral, "id", EphemeralId);
            var trait = new CardTraitData(); trait.Setup("CardTraitEphemeral");
            Set(ephemeral, "traits", new List<CardTraitData> { trait });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(ephemeral);
            cards.AddNewCard(ephemeral, CardPile.HandPile, false, true, animate: false);
            CardData unplayed = UnityEngine.Object.Instantiate(ephemeral); unplayed.name = "PojuRunCardUnplayedEphemeral";
            Set(unplayed, "id", "f15d0001-0000-4000-8000-000000000004"); Set(unplayed, "cost", 999);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(unplayed);
            cards.AddNewCard(unplayed, CardPile.HandPile, false, true, animate: false);
            cards.AddNewCard(all.FindCardData(SecondId), CardPile.HandPile, false, false, animate: false);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            managers.GetPlayerManager().AddEnergy(10);
            Prepared = true;
            log.LogInfo("RUN-CARD-GENERATION-PREPARED unchanged Annihilate original and Ephemeral control; five permanent destinations, full-hand overflow, empty pool and source modifier copies.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CombatContext context = source.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (string dataId in new[] { ControlId, FirstId, EphemeralId, SecondId, ThirdId })
            {
                PlayCardAction? choice = plays.FirstOrDefault(play => play.RoomIndex == 2 && context.FindCard(play.CardInstanceId)!.DataId == dataId);
                if (choice != null) return choice;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
        private static CardPool Pool(string name, params CardData[] cards)
        {
            CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = name;
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            foreach (CardData card in cards) AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { card });
            return pool;
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
