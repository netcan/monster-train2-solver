using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicEntropyDrawModel
    {
        internal const string Effect = "RelicEffectAdditionalDrawPerCardType";
        internal const string OwnerId = "87d50a3b-dade-455f-9012-04f46bac02a5";
        internal const string OwnerKey = "PyreHeartEntropy_ArtifactData";

        internal static bool Present(CombatContext context) => context.Relics?.Any(owner => owner.DataId == OwnerId) == true;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.DataId == OwnerId && owner.AssetKey == OwnerKey && owner.EffectTypes is [Effect] &&
            owner.IsCovenant == false && rule.EffectIndex == 0 && rule.ParamInt == 2 &&
            rule.DrawValue is >= 0f and < int.MaxValue && rule.DrawValue.Value == (int)rule.DrawValue.Value &&
            rule.Amount == rule.DrawValue.Value && rule.DrawMutationOrder is >= 0 &&
            rule.RequiredMutatorId == null && rule.Conditions.Count == 0
                ? null : "Missing or changed original Entropy Pyre draw configuration.";

        internal static CombatContext? Refresh(CombatContext source, out string? error)
        {
            error = null;
            if (!Present(source)) return source;
            if (source.Statistics?.DeckCards == null)
            {
                error = "Entropy Pyre draw requires captured permanent deck membership.";
                return null;
            }

            int blights = 0;
            foreach (int id in source.Statistics.DeckCards)
            {
                if (source.FindCard(id)?.MaskDescriptor?.Definition is not CardMaskDefinition definition)
                {
                    error = "Entropy Pyre draw requires the original type of every permanent deck card.";
                    return null;
                }
                if (definition.CardType == "Blight") blights++;
            }

            CombatRelicState[] owners = source.Relics!.ToArray();
            int ownerIndex = System.Array.FindIndex(owners, owner => owner.DataId == OwnerId);
            CombatRelicState owner = owners[ownerIndex];
            RelicResourceEffect rule = owner.ResourceEffects!.Single(item => item.Type == Effect);
            int draw = blights / rule.ParamInt;
            if (draw == rule.Amount) return source;
            owners[ownerIndex] = owner.WithResourceEffects([rule.WithDrawMutation(draw)]);
            return source.WithRelics(owners);
        }
    }
}
