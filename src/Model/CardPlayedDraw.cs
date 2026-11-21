using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardPlayedDrawModel
    {
        internal const string Trait = "CardTraitDrawOnPlay";

        internal static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            CardExhaustModel.RefreshTraits(card).CombinedTraits?.Any(trait => trait.RuntimeType == Trait) == true;

        internal static TypedDrawResult Discarded(CombatContext source, CardInstanceState card, bool wasPlayed,
            BattlePlayRules? definitions = null)
        {
            if (!wasPlayed || card.MaskDescriptor == null) return new(source, Array.Empty<int>());
            var effects = CardExhaustModel.RefreshTraits(card).CombinedTraits!.Where(trait => trait.RuntimeType == Trait);
            var context = source;
            var drawn = new System.Collections.Generic.List<int>();
            foreach (CardTraitValue trait in effects)
            {
                if (trait.DrawOnPlayUpgrade != null)
                {
                    if (trait.DrawOnPlayUpgrade.Upgrade.ExternalInteractions.Count != 0)
                        return new(null, Array.Empty<int>(), "Draw-on-play next-draw upgrade has unmodeled interactions.");
                    try { context = NextDrawUpgradeModel.Schedule(context, trait.DrawOnPlayUpgrade); }
                    catch (ArgumentException error) { return new(null, Array.Empty<int>(), error.Message); }
                }
                TypedDrawResult result = NextDrawUpgradeModel.DrawCards(context, trait.ParamInt, card.InstanceId, definitions);
                if (!result.Supported) return result;
                context = result.Context!;
                drawn.AddRange(result.DrawnCardIds);
            }
            return new(context, drawn);
        }
    }
}
