using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardCorruptStateModel
    {
        public const string Trait = "CardTraitCorruptState";

        internal static RoomCorruptionResult BeforePlay(TrainCombatState source, CardInstanceState? card,
            CardPlayRule rule, int roomIndex)
        {
            if (card?.MaskDescriptor == null) return new RoomCorruptionResult(source.Context);
            CardTraitValue[] traits = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>())
                .Where(trait => trait.RuntimeType == Trait).ToArray();
            if (traits.Length == 0) return new RoomCorruptionResult(source.Context);
            if (rule.CardType is not ("Spell" or "Monster") || rule.IsAnyAbility == true || traits.Any(trait => trait.DeclaredName != Trait))
                return new RoomCorruptionResult(null, "Corrupt State requires an ordinary spell or Monster with captured trait provenance.");
            if (source.Context?.Resources?.CorruptionPerCorruptTrait is not int amount || amount < 0)
                return new RoomCorruptionResult(null, "Corrupt State requires the captured native mutable corruption rule.");
            CombatContext context = source.Context;
            // Native iterates live combined trait objects, before all Corrupt Restricted callbacks.
            foreach (CardTraitValue trait in traits)
            {
                var train = CardSpellModel.WithContext(source, context);
                RoomCorruptionResult added = RoomCorruptionModel.Apply(train, roomIndex,
                    new CardActionEffect("AdjustCorruption", "Room", amount, false, false,
                        Array.Empty<CombatStatus>(), corruptionAdd: true));
                if (!added.Supported) return added;
                context = added.Context!;
            }
            return new RoomCorruptionResult(context);
        }
    }
}
