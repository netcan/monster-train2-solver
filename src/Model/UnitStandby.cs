using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitStandbyCondition
    {
        public int CardId { get; }
        public int HostUnitId { get; }
        public bool Ready { get; }
        public UnitStandbyCondition(int cardId, int hostUnitId, bool ready = false)
        { CardId = cardId; HostUnitId = hostUnitId; Ready = ready; }
    }
    public static class UnitStandbyModel
    {
        internal static CombatContext MarkDead(CombatContext context, IReadOnlyList<CombatUnit> dead)
        {
            var ready = new HashSet<int>(dead.Where(unit => unit.DeathState?.HasFinishedDying == true).Select(unit => unit.Id));
            if (ready.Count == 0 || context.OtherPiles == null) return context;
            return context.WithOtherPiles(context.OtherPiles.Select(pile => pile.UnitConditions == null ? pile :
                new CardPileState(pile.Name, pile.Cards, pile.EntrySlots, pile.FreeSlots, pile.EquipmentConditions,
                    pile.UnitConditions.Select(binding => new UnitStandbyCondition(binding.CardId, binding.HostUnitId,
                        binding.Ready || ready.Contains(binding.HostUnitId))).ToArray(), pile.ExhaustConditions)).ToArray());
        }
        // Global checks occur during DiscardCard and DrawHand. A death-local check only
        // checks the character's own source and does not return a differently bound card.
        public static CombatContext ReturnReady(CombatContext context)
        {
            CardPileState? standby = context.OtherPiles?.FirstOrDefault(pile => pile.Name == "Standby");
            if (standby?.UnitConditions?.Any(binding => binding.Ready) == true &&
                context.OtherPiles!.All(pile => pile.Name != "Exhausted"))
                throw new InvalidOperationException("Ready unit cards require the Exhausted pile.");
            foreach (UnitStandbyCondition binding in standby?.UnitConditions ?? Array.Empty<UnitStandbyCondition>())
            {
                if (!binding.Ready) continue;
                CardToken card = standby!.Cards.Single(item => item.InstanceId == binding.CardId);
                context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby"
                    ? CardPileModel.Remove(pile, card.InstanceId) : pile.Name == "Exhausted" ? CardPileModel.Add(CardPileModel.Remove(pile, card.InstanceId), card) : pile).ToArray())
                    .WithStatistics(context.LiveStatistics?.Increment(card.InstanceId, "TimesExhausted", requireTrackedCard: context.CardInstances?.Count == 0));
            }
            return context;
        }

        internal static CombatContext ReturnEndless(CombatContext context, int cardId)
        {
            if (context.OtherPiles == null) throw new InvalidOperationException("Endless requires captured card piles.");
            CardPileState? standby = context.OtherPiles.FirstOrDefault(pile => pile.Name == "Standby");
            CardToken? card = standby?.Cards.FirstOrDefault(item => item.InstanceId == cardId);
            if (card == null) return context;
            CardCycleState cards = new CardCycleState(
                context.Cards.Hand,
                context.Cards.Draw.Concat(new[] { card }).ToArray(),
                context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier, context.Cards.ExternalInteractions,
                context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId);
            var piles = context.OtherPiles.Select(pile => pile.Name is "Standby" or "Exhausted" or "Eaten"
                ? CardPileModel.Remove(pile, cardId) : pile).ToArray();
            return context.WithCards(cards).WithOtherPiles(piles);
        }
    }
}
