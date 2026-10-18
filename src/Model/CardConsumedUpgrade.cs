using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardConsumedUpgradeModel
    {
        public const string Trait = "CardTraitScalingByConsumedTempCardUpgradeToCard";
        public const string CardId = "a27a5954-d63a-4b7c-9cff-308aa6a91314";
        public const string UpgradeId = "78bda620-962d-48b5-a9e8-22a659951850";
        public static UnitUpgradeScalingResult Apply(CombatContext context, int ownerCardId, int upgradingSourceCardId, CardUpgradeModifier upgrade)
        {
            UnitUpgradeScalingResult Reject(string reason) => new UnitUpgradeScalingResult(null, null, reason);
            CardInstanceState? owner = context.FindCard(ownerCardId);
            CardMaskDescriptor? descriptor = owner?.MaskDescriptor;
            if (descriptor == null || !Present(owner)) return new UnitUpgradeScalingResult(context, upgrade);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(owner!).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new UnitUpgradeScalingResult(context, upgrade);
            if (owner!.DataId != CardId || active.Length != 1 || descriptor.BaseTraits.Count != 1 || descriptor.BaseTraits[0].RuntimeType != Trait ||
                descriptor.SourceTraits?.Count != 1 || descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                owner.Permanent.Upgrades.Concat(owner.Temporary.Upgrades).Any(item => item.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Consumed-card upgrade scaling requires the unchanged original base trait and source metadata.");
            CardSourceTrait source = descriptor.SourceTraits[0];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait || source.Int != 1 ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Float != 0 || !source.Scaling ||
                source.TrackedValue != "SubtypeInDeck" || source.EntryDuration != "ThisTurn" || descriptor.BaseCost != 0 ||
                descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only original Sacrificial Ressurection's consumed-hand scaling parameters are calibrated.");
            CardStatModifier stats = upgrade.Stats;
            if (upgrade.DataId != UpgradeId || upgrade.AssetKey != "SacrificialRessurection" || stats.Damage != 10 || stats.Health != 0 || stats.Cost != 0 ||
                stats.Heal != 0 || stats.Size != 0 || stats.XCost != 0 || stats.EquipmentLimit != 0 || stats.UpgradeSlotCount != 0 ||
                upgrade.Statuses.Count != 0 || upgrade.ExternalInteractions.Count != 0 || upgrade.RemoveOnDiscard || upgrade.Unique || upgrade.ExcludeFromClones ||
                upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0 || upgrade.CloneDamageBase.HasValue || upgrade.CloneHealBase.HasValue ||
                upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null || upgrade.Lifecycle?.AddedTraits.Count > 0 ||
                upgrade.Lifecycle?.RemovedRuntimeTypes.Count > 0 || upgrade.Lifecycle?.ReplacedAssets.Count > 0 || upgrade.Lifecycle?.Triggers.Count > 0)
                return Reject("Only the original damage-ten generation upgrade is calibrated; other stats and status formulas remain guarded.");
            int consumed = 0;
            if (upgradingSourceCardId != 0)
            {
                // This is the private counter of this resolving effect, not a card-statistics total.
                CardInstanceState? parent = context.FindCard(upgradingSourceCardId);
                if (parent == null || parent.InstanceId != ownerCardId || parent.EffectCounters?.Count != 1 ||
                    parent.EffectCounters[0].EffectIndex != 0 || parent.EffectCounters[0].Type != "CardEffectDiscardHand" || parent.EffectCounters[0].Value < 0)
                    return Reject("Consumed-card scaling requires the captured original source effect counter.");
                consumed = parent.EffectCounters[0].Value;
            }
            return new UnitUpgradeScalingResult(context, upgrade.WithStats(new CardStatModifier(damage: unchecked(stats.Damage * consumed))));
        }
        internal static bool Present(CardInstanceState? card) => card?.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             card.MaskDescriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true));
    }
}
