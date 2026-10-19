using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEnthusiasticFanScenario
    {
        internal const string Name = "original-enthusiastic-fan";
        internal const string CardDataId = "adaaaa9d-56a9-4a5f-a347-ee71d4767df5";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int CardInstanceId { get; private set; }
        private static bool played;

        internal static void PrepareDeployment(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardDataId) ??
                throw new InvalidOperationException("Original Enthusiastic Fan is missing from card data.");
            CharacterData spawn = definition.GetSpawnCharacterData();
            if (definition.name != "EnthusiasticFan" || definition.GetID() != CardDataId ||
                definition.GetCardType() != CardType.Monster || definition.GetCost() != 1 || definition.GetTraits().Count != 0 ||
                definition.GetEffects().Count != 1 || definition.GetEffects()[0].GetEffectStateName() != "CardEffectSpawnMonster" ||
                spawn == null || spawn.name != "MonsterEnthusiasticFan" || !cards.GetBelowHandSize())
                throw new InvalidOperationException("Original Enthusiastic Fan definition or hand capacity changed.");

            CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (card == null || !cards.GetHand().Contains(card))
                throw new InvalidOperationException("Original Enthusiastic Fan could not be added to hand.");
            CardInstanceId = FullBattleTrace.Active!.CardId(card);
            played = false;
            Prepared = true;
            log.LogInfo("ORIGINAL-ENTHUSIASTIC-FAN-PREPARED unchanged cost=1 spawn=" + spawn.name + " card=" + CardInstanceId);
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!played && state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == CardInstanceId))
            {
                PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(candidate =>
                    candidate.CardInstanceId == CardInstanceId && state.Spawn.Train.Rooms
                        .Single(room => room.RoomIndex == candidate.RoomIndex).Units.Any(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre));
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
