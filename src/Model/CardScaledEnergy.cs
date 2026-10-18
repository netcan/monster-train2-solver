using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardScaledEnergyResult
    {
        public CombatContext? Context { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal CardScaledEnergyResult(CombatContext? context, string? error = null) { Context = context; UnsupportedReason = error; }
    }
    public static class CardScaledEnergyModel
    {
        public const string Trait = "CardTraitScalingAddEnergy";
        public static CardScaledEnergyResult Discarded(CombatContext context, CardInstanceState card, bool wasPlayed)
        {
            // Native returns before reading any trait or statistic when the card was not played.
            if (!wasPlayed) return new CardScaledEnergyResult(context);
            CardScaledEnergyResult Reject(string reason) => new CardScaledEnergyResult(null, reason);
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return new CardScaledEnergyResult(context);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new CardScaledEnergyResult(context);
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Scaled discard energy requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 2 || active[0].DefinitionParamInt != 2 ||
                source.Type != Trait || source.Int != 2 || source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" ||
                source.Float != 2 || !source.Scaling || source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisBattle" ||
                descriptor.BaseCost != 0 || !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured GainXEnergy current-turn played-cost parameters are calibrated.");
            if (context.Statistics?.PlayedCosts.Any(cost => cost.CardId == card.InstanceId) != true)
                return Reject("Played discard energy requires the live paid-cost statistic before discard clears it.");
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context, new CardStatisticQuery("PlayedCost", "ThisBattle",
                sourceRawCost: descriptor.BaseCost, variableCost: true, currentCostSourceIsSpell: true), card.InstanceId);
            if (!query.Supported) return Reject(query.UnsupportedReason!);
            int amount = unchecked(active[0].ParamInt * query.Value);
            if (amount <= 0) return new CardScaledEnergyResult(query.Context);
            string? energyError = EnergyModel.Validate(query.Context);
            return energyError == null ? new CardScaledEnergyResult(EnergyModel.Apply(query.Context!, "GainEnergy", amount)) : Reject(energyError);
        }
    }
}
