using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicCorruptRestrictedCardPlayModel
    {
        internal static string? ValidateCardTraits(CardInstanceState card, out bool hasCorruptRestricted)
        {
            hasCorruptRestricted = false;
            if (card.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId)
                return "Corrupt Restricted card-play relics require matching played-card mask metadata.";
            IReadOnlyList<CardTraitValue>? traits;
            try
            {
                traits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(card).Traits)
                    .Composition.CombinedTraits;
            }
            catch (InvalidOperationException error) { return error.Message; }
            if (traits == null) return "Corrupt Restricted played-card traits were not captured.";
            CardTraitValue[] active = traits.Where(trait => trait.DeclaredName != null).ToArray();
            CardTraitValue[] restricted = active.Where(trait => trait.RuntimeType == "CardTraitCorruptRestricted").ToArray();
            if (restricted.Length == 0) return null;
            hasCorruptRestricted = true;
            if (active.Length != restricted.Length)
                return "Corrupt Restricted relic coverage requires a card with no other active traits.";
            if (restricted.Any(trait => trait.ParamInt != 0))
                return "Positive Corrupt Restricted pre-play corruption is not modeled.";
            return null;
        }

        internal static TrainCombatResult ApplyOne(TrainCombatState source, int playedCardId, int selectedRoomIndex,
            int relicIndex, int effectIndex, bool placement, bool preview)
        {
            TrainCombatResult Fail(string reason) => new(null, RoomOutcome.Unsupported,
                Array.Empty<RoomCombatResult>(), reason);
            TrainCombatResult Unchanged() => new(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());

            CombatContext? context = source.Context;
            if (context?.Relics == null || relicIndex < 0 || relicIndex >= context.Relics.Count)
                return Fail("Missing Corrupt Restricted card-play relic state.");
            if (effectIndex < 0 || effectIndex >= context.Relics[relicIndex].EffectTypes.Count)
                return Fail("Corrupt Restricted card-play effect order changed.");
            CombatRelicState owner = context.Relics[relicIndex];
            string type = owner.EffectTypes[effectIndex];
            if (type != RelicResourceModel.EnergyOnCorruptRestrictedPlayed &&
                type != RelicResourceModel.CorruptionOnCorruptRestrictedPlayed)
                return Fail("Unexpected Corrupt Restricted card-play effect type.");
            string? validation = RelicResourceModel.Validate(owner);
            if (validation != null) return Fail(validation);
            RelicResourceEffect? rule = owner.ResourceEffects?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (rule == null || rule.Type != type) return Fail("Missing ordered Corrupt Restricted card-play resource definition.");
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !rule.CanApplyInPreviewMode)
                return Unchanged();

            CardInstanceState? card = context.CardInstances?.SingleOrDefault(item => item.InstanceId == playedCardId);
            if (card == null) return Fail("Corrupt Restricted relic callback requires the played owned card.");
            string? traitError = ValidateCardTraits(card, out bool hasCorruptRestricted);
            if (traitError != null) return Fail(traitError);
            if (!hasCorruptRestricted) return Unchanged();
            if (rule.RequiredMutatorId != null || rule.Conditions.Count != 0)
                return Fail("Conditional Corrupt Restricted card-play relic effects are not modeled.");
            if (rule.Amount <= 0) return Fail("Non-positive Corrupt Restricted relic resource changes are not modeled.");

            if (type == RelicResourceModel.EnergyOnCorruptRestrictedPlayed)
            {
                string? energyError = EnergyModel.Validate(context);
                if (energyError != null) return Fail(energyError);
                return new TrainCombatResult(CardSpellModel.WithContext(source,
                    EnergyModel.Apply(context, "GainEnergy", rule.Amount)), RoomOutcome.Exchanged,
                    Array.Empty<RoomCombatResult>());
            }

            if (selectedRoomIndex < 0) return Fail("Corrupt Restricted relic callback requires the selected room.");
            RoomCorruptionResult corruption = RoomCorruptionModel.Apply(source, selectedRoomIndex,
                new CardActionEffect("AdjustCorruption", "Room", rule.Amount, false, false,
                    Array.Empty<CombatStatus>(), corruptionAdd: true));
            if (!corruption.Supported) return Fail(corruption.UnsupportedReason!);
            return new TrainCombatResult(CardSpellModel.WithContext(source, corruption.Context!), RoomOutcome.Exchanged,
                Array.Empty<RoomCombatResult>());
        }
    }
}
