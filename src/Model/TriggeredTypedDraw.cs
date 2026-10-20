namespace MonsterTrain2Poju.Model
{
    internal static class TriggeredTypedDrawModel
    {
        internal static string? Validate(CombatContext? context, CombatEffect effect)
        {
            if (context == null || effect.Action?.Type != "DrawType" || effect.Action.TypedDraw == null)
                return "Triggered typed draw requires captured card type/filter and shared context.";
            if (effect.Action.Target != "Self") return "Uncalibrated triggered typed-draw selector.";
            if (context.Cards.NextDrawUpgrades?.Count > 0 && context.CardRegistry == null)
                return "Triggered typed draws with queued upgrades require captured card metadata.";
            return effect.Action.Range?.Validate();
        }
    }
}
