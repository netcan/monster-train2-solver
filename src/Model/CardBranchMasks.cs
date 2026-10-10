using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardUpgradeMaskMetadata
    {
        public IReadOnlyList<UpgradeMaskStatus> Statuses { get; }
        public bool HasIcon { get; }
        public bool HideIcon { get; }
        public bool RegionRun { get; }
        public bool UnitAbility { get; }
        public CardUpgradeMaskMetadata(IReadOnlyList<UpgradeMaskStatus> statuses, bool hasIcon, bool hideIcon, bool regionRun, bool unitAbility)
        { Statuses = Array.AsReadOnly(statuses.ToArray()); HasIcon = hasIcon; HideIcon = hideIcon; RegionRun = regionRun; UnitAbility = unitAbility; }
        internal CardMaskUpgrade Current(CardUpgradeModifier upgrade) =>
            new CardMaskUpgrade(upgrade.DataId, upgrade.Stats, Statuses, HasIcon, HideIcon, RegionRun, UnitAbility);
    }

    // Definition/trait inputs owned by a card instance. Mutable numeric modifiers
    // remain on the instance and are read afresh for each branch query.
    public sealed class CardMaskDescriptor
    {
        public CardMaskDefinition Definition { get; }
        public int BaseCost { get; }
        public IReadOnlyList<CardTraitValue> BaseTraits { get; }
        public IReadOnlyList<string> AuthoredCastEffects { get; }
        public bool Purified { get; }
        public bool PermanentGraft { get; }
        public CardMaskDescriptor(CardMaskDefinition definition, int baseCost, IReadOnlyList<CardTraitValue> baseTraits,
            IReadOnlyList<string> authoredCastEffects, bool purified = false, bool permanentGraft = false)
        { Definition = definition; BaseCost = baseCost; BaseTraits = Array.AsReadOnly(baseTraits.ToArray());
            AuthoredCastEffects = Array.AsReadOnly(authoredCastEffects.ToArray()); Purified = purified; PermanentGraft = permanentGraft; }
    }

    public static class CardBranchMaskModel
    {
        internal static string? ValidateCreation(CombatContext context, CardCreationRule creation) =>
            context.CardRegistry?.Any(card => card.MaskDescriptor != null) != true ? null :
            creation.MaskDescriptor == null || creation.MaskDescriptor.Definition.DataId != creation.DataId ||
                creation.StartingModifiers.Upgrades.Any(upgrade => upgrade.MaskMetadata == null)
                ? "Generated card requires its branch mask descriptor and upgrade metadata." : null;
        public static CardUpgradeMaskCard Resolve(CardInstanceState instance, bool ignoreTemporaryCost = false)
            => CardOwnedMaskModel.Resolve(OwnedState(instance), ignoreTemporaryCost).Card;
        internal static CardOwnedMaskState OwnedState(CardInstanceState instance)
        {
            var descriptor = instance.MaskDescriptor ?? throw new InvalidOperationException("Missing branch-owned card mask descriptor.");
            string? unsupported = CardModifierModel.UnsupportedReason(instance);
            if (unsupported != null) throw new InvalidOperationException(unsupported);
            if (descriptor.Definition.DataId != instance.DataId) throw new InvalidOperationException("Card mask descriptor identity differs.");
            CardMaskModifiers Modifiers(CardModifiers modifiers) => new CardMaskModifiers(modifiers.Offsets,
                modifiers.Upgrades.Select(upgrade => (upgrade.MaskMetadata ??
                    throw new InvalidOperationException("Missing branch-owned upgrade mask metadata.")).Current(upgrade)).ToArray());
            var traits = new CardTraitCompositionState(descriptor.BaseTraits, Array.Empty<CardTraitValue>(),
                Array.Empty<IReadOnlyList<CardTraitValue>>(), Array.Empty<string>(), Array.Empty<CardTraitReplacement>());
            return new CardOwnedMaskState(descriptor.Definition, descriptor.BaseCost, Modifiers(instance.Permanent), Modifiers(instance.Temporary),
                new CardTraitRefreshState(traits, true, 1, 1, 0, 0), descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft);
        }
    }
}
