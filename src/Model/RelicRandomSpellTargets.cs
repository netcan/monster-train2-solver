using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicRandomSpellTargetsModel
    {
        internal const string OwnerId = "a83bc048-7924-4beb-87bd-00419bda6be0";
        internal const string OwnerKey = "AllSpellsRandomTargets";
        internal const string Effect = "RelicEffectModifyTargetsRandom";
        internal const string DisableUndo = "RelicEffectDisableUndo";

        internal static bool IsOriginal(CombatRelicState relic) => relic.DataId == OwnerId && relic.AssetKey == OwnerKey &&
            relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
            relic.EffectTypes is [Effect, DisableUndo];

        internal static bool IsRemappable(string target) => target is "DropTargetCharacter" or "FrontInRoom";

        internal static CombatContext CardAdded(CombatContext context, int cardId)
        {
            if (context.Relics?.Any(IsOriginal) != true || context.FindCard(cardId) is not CardInstanceState card ||
                card.MaskDescriptor is not { RandomTargetsFromRelic: false, Definition: { CardType: "Spell", IsSpawner: false } } descriptor)
                return context;
            return context.WithCard(card.WithMaskDescriptor(descriptor.WithRandomTargets(true)));
        }
    }

    internal static class RelicHandCardModel
    {
        internal static RelicResourceResult CardAdded(CombatContext source, int cardId)
        {
            RelicResourceResult result = RelicHoldoverGrantModel.CardAdded(source, cardId);
            if (!result.Supported) return result;
            CombatContext context = result.Context!;
            CombatContext updated = RelicRandomSpellTargetsModel.CardAdded(context, cardId);
            return ReferenceEquals(updated, context) ? result : new RelicResourceResult(updated, result.Value, events: result.Events);
        }
    }
}
