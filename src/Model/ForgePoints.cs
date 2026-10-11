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
            if (context.Relics?.Any(relic => relic.EffectTypes.Contains(AdditionalForgeRelic)) == true)
                return "Additional Forge Points relic effects are not modeled.";
            return null;
        }

        public static bool Test(bool? pyreAlive) => pyreAlive == true;

        public static ForgePointsResult Apply(CombatContext? source, int amount, int sourceCardId = 0)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            CombatContext context = source!;
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
            int adjusted = unchecked(unchecked(context.QueryFrame!.ForgePoints!.Value + amount) + additional);
            int result = Math.Max(0, adjusted);
            return new ForgePointsResult(context.WithQueryFrame(context.QueryFrame.With(forgePoints: result)), result);
        }

        private static ForgePointsResult Unsupported(string reason) => new ForgePointsResult(null, 0, reason);
    }
}
