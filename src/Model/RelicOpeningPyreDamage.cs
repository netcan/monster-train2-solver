using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicOpeningPyreDamageModel
    {
        internal const string Effect = "RelicEffectDamageForNumCardsStartOfBattle";
        internal const string Threshold30 = "8e9444da-bd9b-4f00-b348-bb3e21ad1d1d";
        internal const string Threshold15 = "2fd1f357-9e96-409a-8979-2f3d1f5e776c";

        internal static string? Validate(CombatRelicState relic, RelicResourceEffect rule)
        {
            bool original = relic.DataId == Threshold30 && rule.ParamInt == 30 && rule.ParamInt2 == 1 ||
                relic.DataId == Threshold15 && rule.ParamInt == 15 && rule.ParamInt2 == 2;
            return original && relic.DisallowedInPlacementPhase == false && relic.EffectTypes.Count == 1 &&
                rule.EffectIndex == 0 && rule.Amount == rule.ParamInt2 && !rule.CanApplyInPreviewMode &&
                rule.Conditions.Count == 0 && rule.RequiredMutatorId == null
                ? null : "Unverified opening small-deck damage definition.";
        }

        internal static bool Present(CombatContext context) => context.Relics!.Any(owner => owner.EffectTypes.Contains(Effect));
    }
}
