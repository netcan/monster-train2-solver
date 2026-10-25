using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardCorruptStateModel
    {
        public const string Trait = "CardTraitCorruptState";

        internal static TrainSpellResult BeforePlay(TrainCombatState source, CardInstanceState? card,
            CardPlayRule rule, int roomIndex, BattlePlayRules definitions)
        {
            if (card?.MaskDescriptor == null) return new TrainSpellResult(source, RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
            CardTraitValue[] traits = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>())
                .Where(trait => trait.RuntimeType == Trait).ToArray();
            if (traits.Length == 0) return new TrainSpellResult(source, RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
            if (rule.CardType is not ("Spell" or "Monster") || rule.IsAnyAbility == true || traits.Any(trait => trait.DeclaredName != Trait))
                return new TrainSpellResult(null, RoomOutcome.Unsupported, Array.Empty<CombatEvent>(),
                    "Corrupt State requires an ordinary spell or Monster with captured trait provenance.");
            if (source.Context?.Resources?.CorruptionPerCorruptTrait is not int amount || amount < 0)
                return new TrainSpellResult(null, RoomOutcome.Unsupported, Array.Empty<CombatEvent>(),
                    "Corrupt State requires the captured native mutable corruption rule.");
            // Each trait queues unit/relic callbacks before committing corruption,
            // before the card's summon/effects and Corrupt Restricted callbacks.
            return CardSpellModel.ApplyBeforePlayCorruption(source, roomIndex, traits.Select(_ =>
                new CardActionEffect("AdjustCorruption", "Room", amount, false, false,
                    Array.Empty<CombatStatus>(), corruptionAdd: true)).ToArray(), card.InstanceId, definitions);
        }
    }
}
