using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardGenerationProbe
    {
        [ThreadStatic] private static int projectionSetupDepth;
        [ThreadStatic] private static int contextCaptureDepth;
        [ThreadStatic] private static Dictionary<CardData, CardCreationRule>? contextCreations;
        internal static bool ProjectingSetup => projectionSetupDepth > 0;
        internal static void BeginContextCapture()
        {
            if (contextCaptureDepth == 0)
                contextCreations = new Dictionary<CardData, CardCreationRule>(CardDataReferenceComparer.Instance);
            contextCaptureDepth++;
        }
        internal static void EndContextCapture()
        {
            if (--contextCaptureDepth == 0) contextCreations = null;
        }

        private sealed class CardDataReferenceComparer : IEqualityComparer<CardData>
        {
            internal static readonly CardDataReferenceComparer Instance = new CardDataReferenceComparer();
            public bool Equals(CardData? left, CardData? right) => ReferenceEquals(left, right);
            public int GetHashCode(CardData value) => RuntimeHelpers.GetHashCode(value);
        }

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
        internal static CardCreationRule Creation(CardData data)
        {
            // Creation rules contain definition values, not live card identities.
            // A synchronous context snapshot can request the same definition many
            // times. Never reuse it across snapshots, where definitions or query
            // conditions may have changed, or suppress a real CardState.Setup.
            Dictionary<CardData, CardCreationRule>? cache = contextCreations;
            if (cache != null && cache.TryGetValue(data, out CardCreationRule cached)) return cached;
            CardCreationRule result = CreateDefinition(data);
            if (cache != null) cache[data] = result;
            return result;
        }
        private static CardCreationRule CreateDefinition(CardData data)
        {
            var interactions = new List<string>();
            foreach (CardTraitData trait in data.GetTraits())
                if (!DamageScalingProbe.Known(trait.GetTraitStateName()) &&
                    !(data.IsUnitAbility() && trait.GetTraitStateName() == "CardTraitCorruptRestricted"))
                    interactions.Add("Generated card trait setup/callback " + trait.GetTraitStateName());
            CardState instance = new CardState();
            // This temporary instance supplies native trait defaults to the projection.
            // It must not enter live fallback identity/creation observation, including
            // when another observer captures card definitions inside ApplyEffect.
            projectionSetupDepth++;
            try { instance.Setup(data, AllGameManagers.Instance!.GetSaveManager()); }
            finally { projectionSetupDepth--; }
            CardModifiers modifiers = CardModifiers.Empty();
            CardMaskDescriptor? descriptor = CardBranchMaskProbe.Creation(data, instance.CanScaleWithMagicPower());
            int multiplier = CardMagicPowerModel.Multiplier(new CardInstanceState(0, data.GetID(), modifiers, modifiers, 0, 0, 0,
                Array.Empty<string>(), maskDescriptor: descriptor));
            foreach (CardUpgradeData upgrade in data.GetUpgradeData())
            {
                CardUpgradeModifier projected = CardMagicPowerModel.Scale(Upgrade(upgrade), multiplier);
                // SetupStartingUpgrades follows native unique-upgrade insertion order.
                if (!projected.Unique || projected.DataId.Length == 0 || !modifiers.Upgrades.Any(item => item.DataId == projected.DataId))
                    modifiers = new CardModifiers(modifiers.Offsets, modifiers.Upgrades.Concat(new[] { projected }).ToArray(), 0, modifiers.ExternalInteractions);
            }
            CardEffectCounter[] counters = data.GetEffects().Select((effect, index) => new { effect, index })
                .Where(item => item.effect.GetEffectStateName() == "CardEffectDiscardHand")
                .Select(item => new CardEffectCounter(item.index, "CardEffectDiscardHand", 0)).ToArray();
            return new CardCreationRule(data.GetID(), modifiers, counters.Length == 0 ? null : counters, interactions,
                DamageScalingProbe.Creation(data), StatusScalingProbe.Creation(data), UnitUpgradeScalingProbe.Creation(data),
                RoomCapacityProbe.Creation(data), 0, descriptor, CardModifierProbe.ForgeCountScalingTraits(instance), instance.CanBeCopied(),
                CardModifierProbe.TrainHealScalingTraits(instance));
        }
    }
}
