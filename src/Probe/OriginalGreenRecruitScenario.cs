using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalGreenRecruitScenario
    {
        internal const string Name = "original-green-recruit";
        internal const string CardDataId = "c052c567-1e3c-4f36-a6cd-89b0648b9475";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int CardInstanceId { get; private set; }
        private static bool played;

        internal static void PrepareDeployment(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardDataId) ??
                throw new InvalidOperationException("Original Green Recruit is missing from card data.");
            CardEffectData effect = definition.GetEffects().Single();
            CharacterData spawn = definition.GetSpawnCharacterData();
            if (definition.name != "GreenRecruit" || definition.GetID() != CardDataId ||
                definition.GetCardType() != CardType.Monster || definition.GetTraits().Count != 0 ||
                effect.GetEffectStateName() != "CardEffectSpawnMonster" || spawn == null || spawn.name != "MonsterGenerousHost" ||
                !cards.GetBelowHandSize())
                throw new InvalidOperationException("Original Green Recruit definition or hand capacity changed.");

            CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (card == null || !cards.GetHand().Contains(card))
                throw new InvalidOperationException("Original Green Recruit could not be added to hand.");
            CardInstanceId = FullBattleTrace.Active!.CardId(card);
            played = false;
            Prepared = true;
            log.LogInfo("ORIGINAL-GREEN-RECRUIT-PREPARED unchanged cost=" + definition.GetCost() +
                " spawn=" + spawn.name + " traits=0 card=" + CardInstanceId);
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
