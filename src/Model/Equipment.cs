using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EquipmentDefinition
    {
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }
        public bool ReturnToHand { get; }
        public string? UpgradeId { get; }
        public EquipmentDefinition(IReadOnlyList<CardUpgradeModifier> upgrades, bool returnToHand = false, string? upgradeId = null)
        { Upgrades = Array.AsReadOnly(upgrades.ToArray()); ReturnToHand = returnToHand; UpgradeId = upgradeId; }
    }
    public sealed class EquipmentStandbyCondition
    {
        public int CardId { get; }
        public int HostUnitId { get; }
        public bool ReturnToHand { get; }
        public EquipmentStandbyCondition(int cardId, int hostUnitId, bool returnToHand)
        { CardId = cardId; HostUnitId = hostUnitId; ReturnToHand = returnToHand; }
    }
    public static class EquipmentModel
    {
        public static CombatContext ReturnStandby(CombatContext context, IReadOnlyList<int> livingUnitIds)
            => ReturnUnattached(context, new HashSet<int>(livingUnitIds));
        internal static string UpgradeKey(int cardId) => "equipment:" + cardId;
        internal static CardUpgradeModifier TemporaryAggregate(CardInstanceState card)
        {
            int health = card.Temporary.Offsets.Health, damage = card.Temporary.Offsets.Damage;
            var statuses = new List<CombatStatus>();
            foreach (CardUpgradeModifier upgrade in card.Temporary.Upgrades)
            {
                health = unchecked(health + upgrade.Stats.Health); damage = unchecked(damage + upgrade.Stats.Damage);
                foreach (CombatStatus status in upgrade.Statuses)
                {
                    int index = statuses.FindIndex(item => item.Id == status.Id);
                    if (index < 0) statuses.Add(status);
                    else statuses[index] = statuses[index].WithStacks(unchecked(statuses[index].Stacks + status.Stacks));
                }
            }
            return new CardUpgradeModifier("", "", new CardStatModifier(damage, health), statuses, false, false, false, 0, 0, Array.Empty<string>());
        }
        internal static CombatContext ReturnCard(CombatContext context, EquipmentStandbyCondition condition)
        {
            CardPileState? standby = context.OtherPiles?.FirstOrDefault(pile => pile.Name == "Standby");
            CardToken? card = standby?.Cards.FirstOrDefault(item => item.InstanceId == condition.CardId);
            if (card == null) return context; // Terminal clearing or an earlier return.
            CardCycleState cards = context.Cards;
            CardPileState[] piles = context.OtherPiles!.Select(pile => pile == standby ? CardPileModel.Remove(pile, card.InstanceId) : pile).ToArray();
            if (condition.ReturnToHand)
                cards = new CardCycleState(cards.Hand.Count < context.MaxHandSize ? new[] { card }.Concat(cards.Hand).ToArray() : cards.Hand,
                    cards.Hand.Count < context.MaxHandSize ? cards.Draw : cards.Draw.Concat(new[] { card }).ToArray(), cards.Discard,
                    cards.Rng, cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw);
            else
            {
                piles = piles.Select(pile => pile.Name == "Exhausted" ? CardPileModel.Add(pile, card) : pile).ToArray();
                context = context.WithStatistics(context.LiveStatistics?.Increment(card.InstanceId, "TimesExhausted", requireTrackedCard: context.CardInstances?.Count == 0));
            }
            return context.WithCards(cards).WithOtherPiles(piles);
        }
        internal static CombatContext ReturnAttached(CombatContext context, CombatUnit unit)
        {
            foreach (int cardId in unit.EquipmentCards ?? Array.Empty<int>())
            {
                EquipmentStandbyCondition? condition = context.OtherPiles?.FirstOrDefault(pile => pile.Name == "Standby")?
                    .EquipmentConditions?.FirstOrDefault(item => item.CardId == cardId && item.HostUnitId == unit.Id);
                if (condition != null) context = ReturnCard(context, condition);
            }
            return context;
        }
        internal static CombatContext ReturnUnattached(CombatContext context, ISet<int> livingIds)
        {
            CardPileState? standby = context.OtherPiles?.FirstOrDefault(pile => pile.Name == "Standby");
            foreach (CardToken card in standby?.Cards ?? Array.Empty<CardToken>())
            {
                EquipmentStandbyCondition? condition = standby!.EquipmentConditions?.FirstOrDefault(item => item.CardId == card.InstanceId);
                if (condition != null && !livingIds.Contains(condition.HostUnitId)) context = ReturnCard(context, condition);
            }
            return context;
        }
    }
}
