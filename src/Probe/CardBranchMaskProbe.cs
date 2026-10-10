using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardBranchMaskProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BRANCH_CARD_MASKS") == "1";
        internal static readonly List<object> Records = new List<object>();
        internal static CardMaskDescriptor? Capture(CardState card)
        {
            if (!Enabled) return null;
            var state = CardOwnedMaskProbe.Capture(card);
            return new CardMaskDescriptor(state.Definition, state.BaseCost, state.Traits.Composition.BaseTraits,
                state.InstalledCastEffects, state.Purified, state.PermanentGraft,
                state.Traits.Composition.TemporaryTraits, state.Traits.Composition.PermanentReplacements);
        }
        internal static CardUpgradeMaskMetadata? Upgrade(CardUpgradeState upgrade) => !Enabled ? null :
            new CardUpgradeMaskMetadata(upgrade.GetStatusEffectUpgrades().Select(status =>
                new UpgradeMaskStatus(status.statusId, status.count, status.fromPermanentUpgrade)).ToArray(),
                upgrade.GetUpgradeIcon() != null, upgrade.GetHideUpgradeIconOnCard(), upgrade.GetIsRegionRunUpgrade(), upgrade.GetUnitAbilityUpgrade() != null);
        internal static CardMaskDescriptor? Creation(CardData data)
        {
            if (!Enabled) return null;
            var spawn = data.GetSpawnCharacterData(); var initial = new List<StatusEffectStackData>(); spawn?.GetStartingStatusEffectsCopy(initial);
            var effects = data.GetEffects();
            UpgradeMaskStatus Status(StatusEffectStackData status) => new UpgradeMaskStatus(status.statusId, status.count, status.fromPermanentUpgrade);
            var equipment = effects.FirstOrDefault(effect => effect.GetEffectStateName() == "CardEffectAttachEquipment" && effect.GetParamCardUpgradeData() != null)?.GetParamCardUpgradeData();
            var definition = new CardMaskDefinition(data.GetID(), data.GetCardType().ToString(), data.GetRarity().ToString(), data.IsSpawnerCard(), spawn != null,
                spawn != null && spawn.GetCanAttack(), spawn?.GetSubtypes().Select(subtype => subtype.Key).ToArray() ?? Array.Empty<string>(), initial.Select(Status).ToArray(),
                effects.Select(effect => (IReadOnlyList<UpgradeMaskStatus>)effect.GetParamStatusEffectStackData().Select(Status).ToArray()).ToArray(),
                effects.Select(effect => effect.GetEffectStateName()).ToArray(), data.GetLinkedClassID(), data.IsConsumeRemainingEnergyCostType(), spawn?.GetSize() ?? 0,
                (int)data.GetCardTargetMode(), spawn?.GetUnitAbilityCardData() != null, data.GetCardType() == CardType.Equipment && equipment?.GetUnitAbilityUpgrade() != null,
                spawn?.GetGraftedEquipment() != null);
            var traits = CardTraitCompositionModel.Refresh(new CardTraitCompositionState(data.GetTraits().Select(CardTraitCompositionProbe.Definition).ToArray(),
                Array.Empty<CardTraitValue>(), Array.Empty<IReadOnlyList<CardTraitValue>>(), Array.Empty<string>(), Array.Empty<CardTraitReplacement>())).BaseTraits;
            return new CardMaskDescriptor(definition, data.GetCost(), traits, data.GetCardTriggers().Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast)
                .SelectMany(trigger => trigger.GetCardEffects()).Select(effect => effect.GetEffectStateName()).ToArray());
        }
        internal static void Observe(FullBattleTrace trace, CombatContext context)
        {
            if (!Enabled || !trace.CanonicalDecisionCapture) return;
            var manager = AllGameManagers.Instance!.GetRelicManager();
            var views = trace.KnownCards.OrderBy(trace.CardId).Select(card => new { CardId = trace.CardId(card),
                IgnoreTemporaryCost = manager.HasPermanentUpgradeChangeRelicEffects(card, out bool applied) && applied,
                Actual = CardUpgradeMaskCalibration.Card(card, manager) }).ToArray();
            Records.Add(new { Context = context, Views = views });
        }
    }
}
