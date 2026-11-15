using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardEffectApplicationsModel
    {
        public const string Trait = "CardTraitScalingAddEffectApplications";
        public static StatisticQueryResult Count(CombatContext context, int sourceCardId, CardActionEffect effect)
        {
            StatisticQueryResult Reject(string reason) => new StatisticQueryResult(null, 0, reason);
            CardInstanceState? card = context.FindCard(sourceCardId);
            CardMaskDescriptor? descriptor = card?.MaskDescriptor;
            if (card == null || descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return new StatisticQueryResult(context, 1);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new StatisticQueryResult(context, 1);
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null ||
                descriptor.SourceTraits.Count != descriptor.BaseTraits.Count && !CardCopyOnPlayModel.SupportedMoonbeamSource(descriptor) ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Effect applications require one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait || source.Int != 1 ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Scaling ||
                source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisTurn" ||
                descriptor.BaseCost != 0 || !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Effect applications require captured unchanged X-cost statistic parameters.");
            // Native AddStatus does not consult the damage-only application getter.
            // Punished Spike grants its scaled zeal once, then repeats only damage.
            bool spike = source.Float == 1 && descriptor.BaseTraits.Count == 3 &&
                descriptor.SourceTraits!.Select(trait => trait.Type).SequenceEqual(new[] {
                    "CardTraitExhaustState", Trait, "CardTraitScalingAddStatusEffect" }) &&
                descriptor.SourceTraits[2] is { Int: 8, TrackedValue: "PlayedCost", EntryDuration: "ThisTurn" } status &&
                status.StatusEffectIds?.SequenceEqual(new[] { "zeal" }) == true;
            if (spike && effect.Type == "AddStatus" && effect.Target == "DropTargetCharacter" &&
                effect.AllowEnemy && effect.AllowPlayer && effect.Statuses.Count == 1 && effect.Statuses[0] is { Id: "zeal", Stacks: 0 })
                return new StatisticQueryResult(context, 1);
            bool repeatedSpike = spike && effect.Type == "Damage" && effect.Target == "LastTargetedCharacters" &&
                effect.AllowEnemy && effect.AllowPlayer && effect.Range == null &&
                effect.Value == CardModifierModel.UpgradedStat(CardModifierModel.UpgradedStat(1, "Damage", true, card.Permanent),
                    "Damage", true, card.Temporary);
            bool moonbeam = source.Float == 0 && effect.Type == "Damage" && effect.Target == "FrontInRoom" &&
                effect.AllowEnemy && !effect.AllowPlayer && effect.Range == null &&
                effect.Value == CardModifierModel.UpgradedStat(CardModifierModel.UpgradedStat(20, "Damage", true, card.Permanent),
                    "Damage", true, card.Temporary);
            if (!repeatedSpike && !moonbeam)
                return Reject("Only captured Moonbeam front damage and Punished Spike status-then-targeted-damage applications are calibrated.");
            return StatisticQueryModel.Evaluate(context, new CardStatisticQuery("PlayedCost", "ThisTurn",
                sourceRawCost: descriptor.BaseCost, variableCost: true, currentCostSourceIsSpell: true), sourceCardId);
        }
    }
}
