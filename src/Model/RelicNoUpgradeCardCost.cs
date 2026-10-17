namespace MonsterTrain2Poju.Model
{
    internal static class RelicNoUpgradeCardCostModel
    {
        internal const string Effect = "RelicEffectModifyCardCostIfNoUpgrades";
        internal const string DataId = "f03c7664-0485-4bcf-8bb9-f76321e090c3";
        internal const string AssetKey = "ReduceCostUnitIfNoUpgrade";

        internal static string? Validate(CombatRelicState owner, RelicNoUpgradeCardCostModifier rule)
        {
            CardUpgradeModifier upgrade = rule.Upgrade;
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.EffectTypes.Count != 1 ||
                rule.EffectIndex != 0 || owner.EffectTypes[rule.EffectIndex] != Effect ||
                owner.IsCovenant != false || owner.DisallowedInPlacementPhase != false ||
                rule.ExclusiveType != "Monster" || rule.CostChangeAmount != -1 ||
                upgrade.DataId != Effect || !upgrade.Unique || upgrade.RemoveOnDiscard || upgrade.ExcludeFromClones ||
                upgrade.Stats.Damage != 0 || upgrade.Stats.Health != 0 || upgrade.Stats.Cost != -1 ||
                upgrade.Stats.Heal != 0 || upgrade.Stats.Size != 0 || upgrade.Stats.XCost != 1 ||
                upgrade.Stats.EquipmentLimit != 0 || upgrade.Stats.UpgradeSlotCount != 0 || upgrade.Statuses.Count != 0 ||
                upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0 || upgrade.RestrictSizeToRoomCapacity ||
                upgrade.MagicPowerTraitScalingOnly || upgrade.ExternalInteractions.Count != 0 ||
                upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null || upgrade.MaskMetadata == null ||
                upgrade.MaskMetadata.HasIcon || upgrade.MaskMetadata.HideIcon || upgrade.MaskMetadata.RegionRun ||
                upgrade.MaskMetadata.UnitAbility || upgrade.Lifecycle == null || upgrade.Lifecycle.Values.DataId != Effect ||
                upgrade.Lifecycle.Values.Stats.Cost != -1 ||
                upgrade.Lifecycle.Values.Stats.XCost != 1 || upgrade.Lifecycle.Values.HasIcon ||
                upgrade.Lifecycle.Values.HideIcon || upgrade.Lifecycle.Values.RegionRun || upgrade.Lifecycle.Values.UnitAbility ||
                upgrade.Lifecycle.AddedTraits.Count != 0 || upgrade.Lifecycle.RemovedRuntimeTypes.Count != 0 ||
                upgrade.Lifecycle.Triggers.Count != 0)
                return "Missing or changed native unupgraded-card cost modifier payload.";
            return null;
        }

        internal static bool IsCardOfInterest(CardInstanceState card, RelicNoUpgradeCardCostModifier rule,
            out string? error)
        {
            error = null;
            if (card.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId)
            { error = "Unupgraded-card cost requires matching branch-owned card masks."; return false; }
            string cardType = card.MaskDescriptor.Definition.CardType;
            if (cardType is not ("Monster" or "Spell")) return false;
            return rule.ExclusiveType == "Invalid" || rule.ExclusiveType == cardType;
        }

        internal static bool HasSlottedUpgrade(CardInstanceState card, out bool hasUpgrade, out string? error)
        {
            hasUpgrade = false;
            error = null;
            foreach (CardUpgradeModifier upgrade in card.Permanent.Upgrades)
            {
                if (upgrade.MaskMetadata == null)
                { error = "Unupgraded-card cost requires permanent upgrade icon metadata."; return false; }
                if (upgrade.MaskMetadata.HasIcon && !upgrade.MaskMetadata.HideIcon && !upgrade.MaskMetadata.RegionRun)
                { hasUpgrade = true; break; }
            }
            return true;
        }

    }
}
