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
            bool calibratedCorruptOwner = (rule.CardType == "Equipment" && card.DataId is
                    "75cfe90b-47b9-406c-b891-33564aac6677" or "217d467c-77c2-481d-b78f-57aaf3dcd4a6" or
                    "51eaab0c-a656-456e-8d93-7d536b31d9cf" ||
                rule.CardType == "TrainRoomAttachment" && card.DataId == "93759003-bb97-44e4-9ef6-472c55bee2d9") &&
                traits.Length == 1 && traits[0].ParamInt == 0 && traits[0].DefinitionParamInt == 0 &&
                traits[0].Removable && traits[0].DefinitionRemovable == true && traits[0].StackMode == 0 &&
                !traits[0].TemporaryReplacement && card.MaskDescriptor.BaseTraits.Count == 1 &&
                card.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] {
                    "bdb7a38c-18de-4461-beb8-c27fdbdc5f62" });
            if ((rule.CardType is not ("Spell" or "Monster") && !calibratedCorruptOwner) || rule.IsAnyAbility == true || traits.Any(trait => trait.DeclaredName != Trait))
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
