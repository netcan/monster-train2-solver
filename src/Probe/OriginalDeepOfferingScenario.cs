using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeepOfferingScenario
    {
        internal const string Name = "original-deep-offering";
        internal const string CardId = "37eaa51b-62cf-4cbc-971f-6d208fd07366";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            CardData deepOffering = all.FindCardData(CardId)!;
            if (deepOffering.name != "DeepOffering" || deepOffering.GetCost() != 3 ||
                !deepOffering.GetEffects().Select(effect => effect.GetEffectStateName()).SequenceEqual(new[] { "CardEffectDiscardHand", "CardEffectDraw" }) ||
                deepOffering.GetEffects()[1].GetParamInt() != 5 ||
                !deepOffering.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitExhaustState"))
                throw new InvalidOperationException("Original Deep Offering definition changed.");

            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(string id, CardPile pile = CardPile.HandPile) => cards.AddNewCard(all.FindCardData(id)!, pile, false, false, animate: false);
            CardState anchor = Add(steward.GetID());
            CardState offering = Add(CardId);
            CardState discarded = Add(steward.GetID());
            for (int index = 0; index < 5; index++) Add(steward.GetID(), CardPile.DeckPile);
            CardIds = new[] { anchor, offering, discarded }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            managers.GetPlayerManager().AddEnergy(4);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DEEP-OFFERING-PREPARED unchanged 3-cost card, one hand discard, five controlled draws and played Exhaust.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
            {
                var cast = new PlayCardAction(ids[1], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
