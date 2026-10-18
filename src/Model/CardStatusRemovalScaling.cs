using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardStatusRemovalScalingModel
    {
        public const string Trait = "CardTraitScalingRemoveStatusEffect";
        public static StatusScalingResult Apply(CombatContext? context, int sourceCardId, string statusId, int sourceStacks)
        {
            StatusScalingResult Unchanged() => new StatusScalingResult(context, sourceStacks);
            StatusScalingResult Reject(string reason) => new StatusScalingResult(null, 0, reason);
            if (sourceCardId <= 0 || sourceStacks == -1) return Unchanged(); // Native remove-all bypasses trait modifiers.
            CardInstanceState? card = context?.FindCard(sourceCardId);
            CardMaskDescriptor? descriptor = card?.MaskDescriptor;
            if (card == null || descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true)) return Unchanged();
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return Unchanged();
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Status-removal scaling requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait || source.Int != 1 ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Float != 0 || source.Scaling ||
                source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisBattle" || source.StatusEffectIds?.SequenceEqual(new[] { "cooldown" }) != true ||
                descriptor.BaseCost != 0 || !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured Pyreborne Brainspike cooldown/played-cost removal parameters are calibrated.");
            if (!source.StatusEffectIds.Contains(statusId)) return Unchanged();
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context!, new CardStatisticQuery("PlayedCost", "ThisBattle",
                sourceRawCost: descriptor.BaseCost, variableCost: true, currentCostSourceIsSpell: true), sourceCardId);
            return query.Supported ? new StatusScalingResult(query.Context, unchecked(sourceStacks + active[0].ParamInt * query.Value)) : Reject(query.UnsupportedReason!);
        }
    }
}
