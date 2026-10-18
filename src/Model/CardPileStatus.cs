using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardPileStatusModel
    {
        public const string Trait = "CardTraitScalingStatusEffectPerPile";
        // Returns only the trait's bonus; native ignores the running incoming count.
        public static StatusScalingResult Bonus(CombatContext? context, int sourceCardId, string statusId)
        {
            StatusScalingResult Reject(string reason) => new StatusScalingResult(null, 0, reason);
            CardInstanceState? card = context?.FindCard(sourceCardId);
            CardMaskDescriptor? descriptor = card?.MaskDescriptor;
            if (card == null || descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return new StatusScalingResult(context, 0);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new StatusScalingResult(context, 0);
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Pile-scaled status requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait || source.Int != 1 ||
                source.Int2 != 0 || source.Int3 != 10 || !source.Bool || source.Str != "" || source.Float != 0 || source.Scaling ||
                source.TrackedValue != "SubtypeInDeck" || source.EntryDuration != "ThisTurn" || source.StatusEffectIds == null ||
                !source.StatusEffectIds.SequenceEqual(new[] { "horde" }) || descriptor.BaseCost != 2 || descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured Dig Deep visible-deck Horde parameters are calibrated.");
            if (statusId.Length > 0 && !source.StatusEffectIds.Contains(statusId)) return new StatusScalingResult(context, 0);
            if (context!.Resources == null || context.Resources.VisibleDeckCount < 0)
                return Reject("Pile-scaled status requires the captured visible permanent-deck count.");
            if (context.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.AddStatusOnApplied) || relic.StatusStackAdditions?.Count > 0) == true)
                return Reject("Pile-scaled status with relic stack modifiers requires native calibration of the extra modifier pass.");
            return new StatusScalingResult(context, context.Resources.VisibleDeckCount / source.Int3);
        }
    }
}
