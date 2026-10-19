using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPrimordiumScenario
    {
        internal const string Name = "original-primordium";
        internal const string CardDataId = "2e1b29f2-fbe8-47b9-8ef8-1bac4c1f1f65";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int CardInstanceId { get; private set; }
        private static bool played;

        internal static void PrepareDeployment(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardDataId) ??
                throw new InvalidOperationException("Original Champion Primordium is missing from card data.");
            CardEffectData effect = definition.GetEffects().Single();
            CharacterData spawn = definition.GetSpawnCharacterData();
            if (definition.name != "ChampionPrimordium" || definition.GetID() != CardDataId ||
                definition.GetCardType() != CardType.Monster || definition.GetCost() != 0 || definition.GetTraits().Count != 0 ||
                effect.GetEffectStateName() != "CardEffectSpawnMonster" || spawn == null || spawn.name != "MonsterChampionUmbraPrimordium" ||
                !cards.GetBelowHandSize())
                throw new InvalidOperationException("Original Champion Primordium definition or hand capacity changed.");

            CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (card == null || !cards.GetHand().Contains(card))
                throw new InvalidOperationException("Original Champion Primordium could not be added to hand.");
            CardInstanceId = FullBattleTrace.Active!.CardId(card);
            played = false;
            Prepared = true;
            log.LogInfo("ORIGINAL-PRIMORDIUM-PREPARED unchanged cost=0 spawn=" + spawn.name + " traits=0 card=" + CardInstanceId);
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!played && state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == CardInstanceId))
            {
                PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state)
                    .FirstOrDefault(candidate => candidate.CardInstanceId == CardInstanceId);
                if (action != null)
                {
                    played = true;
                    return action;
                }
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => card.InstanceId != CardInstanceId);
        }
    }
}
