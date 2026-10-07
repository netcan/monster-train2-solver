using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardGenerationProbe
    {
        internal static CardGenerationRule Definition(CardEffectData data)
        {
            var pool = new List<CardData>();
            CardEffectState.GetFilteredCardListFromPool(data.GetParamCardPool(), data.GetParamCardFilter(),
                AllGameManagers.Instance!.GetRelicManager(), ref pool);
            return Definition(data, pool);
        }
        internal static CardGenerationRule Definition(CardEffectState state)
        {
            var pool = new List<CardData>(); state.GetFilteredCardListFromPool(AllGameManagers.Instance!.GetRelicManager(), ref pool);
            return Definition(state.GetSourceCardEffectData(), pool);
        }
        private static CardGenerationRule Definition(CardEffectData data, List<CardData> pool)
        {
            var interactions = new List<string>();
            // The class filter is applied deterministically at the root; no random choice is made here.
            if (data.GetFilterBasedOnMainSubClass() && pool.Count > 0)
            {
                SaveManager save = AllGameManagers.Instance!.GetSaveManager();
                pool.RemoveAll(card => card.GetLinkedClass() != null && card.GetLinkedClass() != save.GetMainClass() && card.GetLinkedClass() != save.GetSubClass());
                if (pool.Count == 0) interactions.Add("Class filtering leaves an invalid empty generated pool");
            }
            CardUpgradeModifier? optional = data.GetParamCardUpgradeData() == null ? null : Upgrade(data.GetParamCardUpgradeData()!);
            var discard = AllGameManagers.Instance!.GetSaveManager().GetBalanceData().GetCardUpgradesOnAddCardToDiscardPile()
                .Select(item => new DiscardGenerationUpgrade(item.UpgradeToCheck == null ? "" : item.UpgradeToCheck.GetID(), Upgrade(item.UpgradeToAdd))).ToArray();
            return new CardGenerationRule(((CardPile)data.GetParamInt()).ToString(), data.GetAdditionalParamInt(),
                pool.Select(Creation).ToArray(), data.GetParamBool2(), data.GetParamBool(), data.GetCopyModifiersFromSource(),
                data.GetIgnoreTempModifiersFromSource(), optional, discard, interactions);
        }
        private static CardUpgradeModifier Upgrade(CardUpgradeData data)
        { var state = new CardUpgradeState(); state.Setup(data); return CardModifierProbe.Upgrade(state); }
        private static CardCreationRule Creation(CardData data)
        {
            var interactions = new List<string>();
            foreach (CardTraitData trait in data.GetTraits())
                if (!DamageScalingProbe.Known(trait.GetTraitStateName())) interactions.Add("Generated card trait setup/callback " + trait.GetTraitStateName());
            CardModifiers modifiers = CardModifiers.Empty();
            foreach (CardUpgradeData upgrade in data.GetUpgradeData())
            {
                CardUpgradeModifier projected = Upgrade(upgrade);
                // SetupStartingUpgrades follows native unique-upgrade insertion order.
                if (!projected.Unique || projected.DataId.Length == 0 || !modifiers.Upgrades.Any(item => item.DataId == projected.DataId))
                    modifiers = new CardModifiers(modifiers.Offsets, modifiers.Upgrades.Concat(new[] { projected }).ToArray(), 0, modifiers.ExternalInteractions);
            }
            CardEffectCounter[] counters = data.GetEffects().Select((effect, index) => new { effect, index })
                .Where(item => item.effect.GetEffectStateName() == "CardEffectDiscardHand")
                .Select(item => new CardEffectCounter(item.index, "CardEffectDiscardHand", 0)).ToArray();
            return new CardCreationRule(data.GetID(), modifiers, counters.Length == 0 ? null : counters, interactions, DamageScalingProbe.Creation(data), StatusScalingProbe.Creation(data), UnitUpgradeScalingProbe.Creation(data), RoomCapacityProbe.Creation(data));
        }
    }
}
