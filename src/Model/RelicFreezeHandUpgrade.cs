using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicFreezeHandUpgrade
    {
        public IReadOnlyList<string>? RequiredTraits { get; }
        public bool RemoveOnDiscard { get; }
        public bool CanApplyInPreviewMode { get; }
        public int ConditionCount { get; }
        public int FilterCount { get; }
        public CardUpgradeModifier Upgrade { get; }
        public RelicFreezeHandUpgrade(IReadOnlyList<string>? requiredTraits, bool removeOnDiscard,
            bool canApplyInPreviewMode, int conditionCount, int filterCount, CardUpgradeModifier upgrade)
        { RequiredTraits = requiredTraits == null ? null : Array.AsReadOnly(requiredTraits.ToArray());
            RemoveOnDiscard = removeOnDiscard; CanApplyInPreviewMode = canApplyInPreviewMode;
            ConditionCount = conditionCount; FilterCount = filterCount; Upgrade = upgrade; }
    }

    internal static class RelicFreezeHandUpgradeModel
    {
        internal const string Effect = "RelicEffectApplyCardUpgradeToHandEndOfTurn";
        internal const string DataId = "d2de397e-6fce-442f-a9e4-c3f3ee7bdbcf";
        internal const string AssetKey = "FreezeReduceCost";
        internal const string UpgradeId = "279af5a6-1a0b-4663-9e1a-6be3987f5a86";

        internal static CardInstanceState Played(CardInstanceState card)
        {
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor?.TemporaryTraits.Any(trait => trait.RuntimeType == "CardTraitFreeze") != true) return card;
            return card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                descriptor.TemporaryTraits.Where(trait => trait.RuntimeType != "CardTraitFreeze").ToArray(), descriptor.PermanentReplacements, descriptor.SourceTraits, descriptor.KillTriggers));
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source, int roomIndex, bool preview)
        {
            if (source.Context == null) return new(null, RoomOutcome.Unsupported, [], "Missing freeze hand-upgrade context.");
            RelicMoonPhaseHandUpgradeResult result = Apply(source.Context, roomIndex, false, preview);
            if (!result.Supported) return new(null, RoomOutcome.Unsupported, [], result.UnsupportedReason);
            return new(new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, room.Units, room.ExternalInteractions, result.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, result.Context), RoomOutcome.Exchanged, []);
        }

        internal static string? Validate(CombatRelicState owner, RelicCardModifier modifier)
        {
            RelicFreezeHandUpgrade? rule = modifier.FreezeHandUpgrade;
            CardUpgradeModifier? upgrade = rule?.Upgrade;
            CardStatModifier? stats = upgrade?.Stats;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == true && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
                modifier.EffectIndex == 0 && modifier.Rule == null && modifier.Upgrade == null &&
                modifier.TraitReplacement == null && modifier.NoUpgradeCost == null && modifier.MoonPhaseHandUpgrade == null && modifier.RandomFreeze == null &&
                !modifier.ApplyToCardlessSpawns && modifier.ConditionCount == 0 && modifier.Conditions.Count == 0 &&
                rule?.RequiredTraits != null && rule.RequiredTraits.SequenceEqual(new[] { "CardTraitFreeze" }) &&
                rule.RemoveOnDiscard && !rule.CanApplyInPreviewMode && rule.ConditionCount == 0 && rule.FilterCount == 0 &&
                upgrade != null && upgrade.DataId == UpgradeId && upgrade.AssetKey == "ReduceCostOnFreeze" &&
                upgrade.RemoveOnDiscard && !upgrade.Unique && !upgrade.ExcludeFromClones &&
                upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && !upgrade.RestrictSizeToRoomCapacity &&
                !upgrade.MagicPowerTraitScalingOnly && upgrade.CloneDamageBase == null && upgrade.CloneHealBase == null &&
                upgrade.EquipmentSourceCardId == null && upgrade.EquipmentSourceUpgradeIndex == null &&
                upgrade.AbilityUpgrade == null && !upgrade.DoNotReplaceExistingAbility &&
                (upgrade.TriggerUpgrades?.Count ?? 0) == 0 && upgrade.Statuses.Count == 0 &&
                upgrade.MaskMetadata != null && !CardModifierModel.UpgradeInteractions(upgrade).Any() && stats != null &&
                stats.Cost == -99 && stats.Damage == 0 && stats.Health == 0 && stats.Heal == 0 && stats.Size == 0 &&
                stats.XCost == 0 && stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0
                ? null : "Missing or changed original FreezeReduceCost definition.";
        }

        internal static RelicMoonPhaseHandUpgradeResult Apply(CombatContext source, int roomIndex, bool deployment, bool preview)
        {
            CombatContext context = source;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            {
                if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicCardModifier? modifier = owner.CardModifiers?.SingleOrDefault();
                string? error = modifier == null ? "Missing freeze hand-upgrade payload." : Validate(owner, modifier);
                if (error != null) return new(context, error);
                if (roomIndex != 0 || deployment || preview) continue;
                RelicFreezeHandUpgrade rule = modifier!.FreezeHandUpgrade!;
                foreach (CardToken token in context.Cards.Hand)
                {
                    CardInstanceState? card = context.FindCard(token.InstanceId);
                    if (card?.MaskDescriptor == null || card.DataId != token.DataId)
                        return new(context, "Freeze hand upgrade requires matching owned card traits.");
                    IReadOnlyList<string> traits;
                    try { traits = CardBranchMaskModel.Resolve(card).Traits; }
                    catch (InvalidOperationException failure) { return new(context, failure.Message); }
                    if (!rule.RequiredTraits!.All(traits.Contains)) continue;
                    CardInstanceState changed = card.WithModifiers(card.Permanent,
                        new CardModifiers(card.Temporary.Offsets, card.Temporary.Upgrades.Concat(new[] { rule.Upgrade }).ToArray(),
                            card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions));
                    try { context = CardMagicPowerModel.Refresh(context, changed, out _); }
                    catch (InvalidOperationException failure) { return new(context, failure.Message); }
                }
            }
            return new(context);
        }
    }
}
