using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class ForgePointsResult
    {
        public CombatContext? Context { get; }
        public int Amount { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal ForgePointsResult(CombatContext? context, int amount, string? error = null)
        { Context = context; Amount = amount; UnsupportedReason = error; }
    }

    public static class ForgePointsModel
    {
        public const string Effect = "AdjustForgePoints";
        public const string AdditionalForgeRelic = "RelicEffectAdditionalForgePoints";

        public static string? Validate(CombatContext? context)
        {
            if (context?.QueryFrame?.ForgePoints == null)
                return "Forge point effects require the current Forge Points value.";
            foreach (CombatRelicState relic in context.Relics ?? Array.Empty<CombatRelicState>())
            {
                string? relicError = RelicForgePointModel.Validate(relic);
                if (relicError != null) return relicError;
            }
            return null;
        }

        public static bool Test(bool? pyreAlive) => pyreAlive == true;

        public static ForgePointsResult Apply(CombatContext? source, int amount, int sourceCardId = 0,
            bool placement = false, bool preview = false)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            CombatContext context = source!;
            RelicForgePointResult relicPoints = RelicForgePointModel.Apply(context, amount, placement, preview);
            if (!relicPoints.Supported) return Unsupported(relicPoints.UnsupportedReason!);
            context = relicPoints.Context!;
            int additional = 0;
            if (sourceCardId > 0)
            {
                CardInstanceState? card = context.FindCard(sourceCardId);
                if (card == null) return Unsupported("Missing Forge Points source card state.");
                if (card.ForgeCountScalingTraits == null)
                    return Unsupported("Missing captured Forge Points scaling traits.");
                foreach (ScalingForgeCountTrait trait in card.ForgeCountScalingTraits)
                {
                    StatisticQueryResult query = StatisticQueryModel.Evaluate(context, trait.Query, sourceCardId);
                    if (!query.Supported) return Unsupported(query.UnsupportedReason!);
                    context = query.Context!;
                    additional = unchecked(additional + unchecked(trait.ForgedCountPerStat * query.Value));
                }
            }
            int adjusted = unchecked(unchecked(unchecked(context.QueryFrame!.ForgePoints!.Value + amount) + relicPoints.AdditionalPoints) + additional);
            int result = Math.Max(0, adjusted);
            return new ForgePointsResult(context.WithQueryFrame(context.QueryFrame.With(forgePoints: result)), result);
        }

        private static ForgePointsResult Unsupported(string reason) => new ForgePointsResult(null, 0, reason);
    }
}
