using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicConsumeDamageModel
    {
        internal const string Effect = "RelicEffectDamageOnExhaust";
        internal const string DataId = "2d2450a5-4ed5-4d9a-bba2-15c9eb0eee6d";
        internal const string AssetKey = "DamageOnConsume";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicCardPlayDamageRule? rule = effect.CardPlayDamage;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) && effect.EffectIndex == 0 &&
                effect.ParamInt == 30 && effect.Amount == 30 && effect.ParamInt2 == 0 && effect.Conditions.Count == 0 &&
                effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && rule != null &&
                rule.CardType == "Spell" && rule.TargetMode == "FrontInRoom" && rule.TargetTeam == "Heroes" &&
                rule.RequiredTraits.Count == 0 ? null : "Missing or changed original DamageOnConsume payload.";
        }

        internal static RelicCardPlayDamageSelection Select(TrainCombatState source, int cardId,
            int roomIndex, int relicIndex, int effectIndex)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return new(null, error: error);
            if (source.Context?.Relics == null || relicIndex < 0 || relicIndex >= source.Context.Relics.Count)
                return new(null, error: "Missing Consume damage owner.");
            RelicResourceEffect? effect = source.Context.Relics[relicIndex].ResourceEffects?
                .SingleOrDefault(rule => rule.EffectIndex == effectIndex && rule.Type == Effect);
            if (effect == null) return new(null, error: "Missing ordered Consume damage effect.");
            CardInstanceState? card = source.Context.FindCard(cardId);
            if (card?.MaskDescriptor == null || card.ExhaustState == null)
                return new(null, error: "Consume damage requires the played card's captured traits and private state.");
            error = CardExhaustModel.Validate(card);
            if (error != null) return new(null, error: error);
            // The native effect ignores ParamCardType and checks the actual trait's
            // next-play threshold. A Consume Monster is eligible too.
            if (!CardExhaustModel.ShouldExhaustOnPlayedDiscard(card)) return new(source);
            // Native CollectTargets explicitly ignores dead retained characters.
            var living = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => unit.Health > 0).ToArray(), room.ExternalInteractions,
                room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, source.Context);
            CardTargets targets = CardTargetModel.Collect(living, roomIndex,
                new CardActionEffect("Damage", "FrontInRoom", 30, true, false, Array.Empty<CombatStatus>()),
                Array.Empty<int>(), isTesting: false);
            return targets.Supported ? new(source, targets.UnitIds) : new(null, error: targets.UnsupportedReason);
        }

        internal static TrainCombatResult ApplyOne(TrainCombatState source, int cardId,
            int roomIndex, int relicIndex, int effectIndex)
        {
            RelicCardPlayDamageSelection selected = Select(source, cardId, roomIndex, relicIndex, effectIndex);
            return selected.Supported ? ApplySelected(selected.State!, selected.Targets) :
                new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), selected.UnsupportedReason);
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source,
            System.Collections.Generic.IReadOnlyList<int> targets, bool deferPhysicalRemoval = false) =>
            RelicCardPlayDamageModel.ApplySelected(source, targets, deferPhysicalRemoval, damageAmount: 30, selectTargetRoom: false);
    }
}
