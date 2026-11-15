using System;

namespace MonsterTrain2Poju.Model
{
    public static class MissingHealthUnitUpgradeModel
    {
        public const string NativeEffect = "CardEffectAddCardUpgradeToUnitPerMissingHp";
        public const string Effect = "UnitUpgradePerMissingHealth";

        internal static string? Validate(CardActionEffect action)
        {
            CardUpgradeModifier? upgrade = action.Upgrade;
            if (action.Target != "Self" || action.Value < 1 || upgrade == null || !action.ScaleUpgrade ||
                action.Lifetime is not ("TemporaryUntilEndOfBattle" or "TemporaryUntilUnitDeath"))
                return "Missing-health upgrades require a positive divisor, one self target and a temporary accumulated upgrade.";
            // The native class bypasses AddCardUpgradeToUnits, including its source-card
            // scaling traits. Broader payloads need their own accumulation evidence.
            if (upgrade.DataId.Length == 0 || upgrade.Unique || upgrade.ExcludeFromClones ||
                upgrade.RestrictSizeToRoomCapacity || upgrade.Stats.Damage < 0 || upgrade.Stats.Health < 0 ||
                upgrade.Stats.Cost != 0 || upgrade.Stats.Heal != 0 || upgrade.Stats.Size != 0 ||
                upgrade.Stats.XCost != 0 || upgrade.Stats.EquipmentLimit != 0 || upgrade.Stats.UpgradeSlotCount != 0 ||
                upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0 || upgrade.Statuses.Count != 0 ||
                upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null ||
                upgrade.Lifecycle is CardLifecycleUpgrade lifecycle && (lifecycle.AddedTraits.Count != 0 ||
                    lifecycle.RemovedRuntimeTypes.Count != 0 || lifecycle.TraitsModified.Count != 0 ||
                    lifecycle.ReplacedAssets.Count != 0 || lifecycle.Triggers.Count != 0))
                return "Missing-health upgrade accumulation currently requires a plain nonnegative attack/health payload.";
            return null;
        }

        internal static CardUpgradeModifier Scale(CardUpgradeModifier upgrade, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            // Validation limits the payload to numeric attack/health. The native
            // repeated additions then reduce to one multiplication during search.
            return upgrade.WithScaledStats(checked(upgrade.Stats.Damage * count), checked(upgrade.Stats.Health * count));
        }
    }
}
