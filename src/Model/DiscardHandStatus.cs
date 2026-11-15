using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class DiscardHandStatusModel
    {
        public const string NativeEffect = "CardEffectDiscardHandAddStatusEffectPerCard";
        public const string Effect = "DiscardHandAddStatus";
        public static int[]? EligibleCards(CombatContext context, int sourceCardId, out string? error)
        {
            // Devote uses the same native Infusion/Persistent exclusions. Freeze
            // is deliberately eligible; the resolving card is never discarded.
            int[]? eligible = DevoteModel.EligibleCards(context, out error);
            return eligible?.Where(id => id != sourceCardId).ToArray();
        }
    }
}
