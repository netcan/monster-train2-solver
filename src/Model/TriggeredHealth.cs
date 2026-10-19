using System;

namespace MonsterTrain2Poju.Model
{
    internal static class TriggeredHealthModel
    {
        internal static bool IsEffect(string type) => type is "CardEffectAdjustMaxHealth" or "CardEffectBuffMaxHealth";
        internal static string? Validate(CombatEffect effect)
        {
            CardActionEffect? action = effect.Action;
            if (action?.Type != (effect.Type == "CardEffectAdjustMaxHealth" ? "AdjustMaxHealth" : "BuffHealth"))
                return "Missing triggered maximum-health definition.";
            if (action.Type == "AdjustMaxHealth" && action.Lifetime != "")
                return "Upgrade-bound maximum-health adjustments require native attribution calibration.";
            if (action.Type == "BuffHealth" && action.Lifetime is not ("" or "TemporaryUntilEndOfBattle" or "TemporaryUntilUnitDeath"))
                return "Unmodeled triggered maximum-health buff lifetime.";
            if (action.Target is not ("Self" or "Room" or "FrontInRoom" or "BackInRoom" or "Weakest"))
                return "Uncalibrated triggered maximum-health target.";
            return (action.Target == "Self" ? null : action.Filters?.Validate()) ?? action.Range?.Validate();
        }
        internal static CombatUnit? Adjust(CombatUnit target, int amount)
        {
            int maximum = Math.Min(99999, unchecked(target.MaxHealth + amount));
            // Native SetHealth preserves HP even below a lowered maximum. That transient
            // invalid-health shape needs a separate witness before entering room combat.
            return maximum < target.Health ? null : target.WithHealthAndMaxHealth(target.Health, maximum);
        }
    }
}
