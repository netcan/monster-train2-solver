using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardSpecificDamageModel
    {
        public const string Trait = "CardTraitScalingAddDamagePerCard";
        internal static DamageScalingResult Apply(CombatContext? context, CardInstanceState card, int damageSourceCardId,
            int damage, StatisticQueryFrame? frame, RoomCombatState? selectedRoom, int attackerUnitId)
        {
            DamageScalingResult Unchanged() => new DamageScalingResult(context, damage);
            DamageScalingResult Reject(string reason) => new DamageScalingResult(null, 0, reason);
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true)) return Unchanged();
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>())
                .Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return Unchanged();
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Specific-card damage requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 10 || active[0].DefinitionParamInt != 10 ||
                source.Type != Trait || source.Int != 10 || source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" ||
                source.Float != 1 || source.Scaling || source.TrackedValue != "NumSpecificCardsInDeck" || source.EntryDuration != "ThisTurn" ||
                string.IsNullOrEmpty(source.SpecificCardDataId))
                return Reject("Only captured Dante's Inferno specific-card damage parameters are calibrated.");
            if (attackerUnitId > 0) return Reject("Specific-card damage's attacker attack mutation requires native calibration.");
            if (card.DamageScalingTraits?.Count > 0 || descriptor.BaseTraits.Any(trait => trait.RuntimeType == CardStatusDamageModel.Trait))
                return Reject("Mixed specific-card damage trait ordering requires native calibration.");
            return DamageScalingModel.ApplyTrait(context, new ScalingDamageTrait(new CardStatisticQuery(source.TrackedValue,
                source.EntryDuration, specificCardDataId: source.SpecificCardDataId), 10, 1, false), card.InstanceId,
                damageSourceCardId, damage, frame, selectedRoom);
        }
    }
}
