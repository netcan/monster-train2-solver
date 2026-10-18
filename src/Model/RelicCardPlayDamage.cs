using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardPlayDamageRule
    {
        public string CardType { get; }
        public string TargetMode { get; }
        public string TargetTeam { get; }
        public IReadOnlyList<string> RequiredTraits { get; }
        public RelicCardPlayDamageRule(string cardType, string targetMode, string targetTeam, IReadOnlyList<string> requiredTraits)
        { CardType = cardType; TargetMode = targetMode; TargetTeam = targetTeam; RequiredTraits = Array.AsReadOnly(requiredTraits.ToArray()); }
    }

    internal sealed class RelicCardPlayDamageSelection
    {
        internal TrainCombatState? State { get; }
        internal IReadOnlyList<int> Targets { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => State != null;
        internal RelicCardPlayDamageSelection(TrainCombatState? state, IReadOnlyList<int>? targets = null, string? error = null)
        { State = state; Targets = targets ?? Array.Empty<int>(); UnsupportedReason = error; }
    }

    internal static class RelicCardPlayDamageModel
    {
        internal const string Effect = "RelicEffectDamageOnCardPlayed";
        internal const string DataId = "b6e80269-efdd-4e28-97b3-8b19e8236b2e";
        internal const string AssetKey = "DamageOnCardPlayed";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicCardPlayDamageRule? rule = effect.CardPlayDamage;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) && effect.EffectIndex == 0 &&
                effect.ParamInt == 3 && effect.Amount == 3 && effect.ParamInt2 == 0 && effect.Conditions.Count == 0 &&
                !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && rule != null &&
                rule.CardType == "Spell" && rule.TargetMode == "RandomInRoom" && rule.TargetTeam == "Heroes" &&
                rule.RequiredTraits.Count == 0 ? null : "Missing or changed original DamageOnCardPlayed payload.";
        }

        internal static RelicCardPlayDamageSelection Select(TrainCombatState source, CardPlayRule card,
            int roomIndex, int relicIndex, int effectIndex, bool preview = false)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return new(null, error: error);
            if (source.Context?.Relics == null || relicIndex < 0 || relicIndex >= source.Context.Relics.Count)
                return new(null, error: "Missing card-play damage owner.");
            CombatRelicState owner = source.Context.Relics[relicIndex];
            RelicResourceEffect? effect = owner.ResourceEffects?.SingleOrDefault(rule => rule.EffectIndex == effectIndex && rule.Type == Effect);
            if (effect == null) return new(null, error: "Missing ordered card-play damage effect.");
            if (preview) return new(source);
            if (card.CardType == null || card.CardType == "Spell" && !card.IsAnyAbility.HasValue)
                return new(null, error: "Card-play damage requires captured card-type and ability metadata.");
            if (card.CardType != "Spell" || card.IsAnyAbility == true) return new(source);
            CardTargets targets = CardTargetModel.Collect(source, roomIndex,
                new CardActionEffect("Damage", "RandomInRoom", 3, true, false, Array.Empty<CombatStatus>()),
                Array.Empty<int>(), isTesting: false);
            if (!targets.Supported) return new(null, error: targets.UnsupportedReason);
            // Native Test uses isTesting=false and commits the Battle RNG choice.
            TrainCombatState selected = targets.BattleRng.HasValue
                ? CardSpellModel.WithContext(source, source.Context.WithBattleRng(targets.BattleRng.Value)) : source;
            return new(selected, targets.UnitIds);
        }

        internal static TrainCombatResult ApplyOne(TrainCombatState source, CardPlayRule card,
            int roomIndex, int relicIndex, int effectIndex, bool preview = false)
        {
            RelicCardPlayDamageSelection selected = Select(source, card, roomIndex, relicIndex, effectIndex, preview);
            return selected.Supported ? ApplySelected(selected.State!, selected.Targets) : Fail(selected.UnsupportedReason!);
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source, IReadOnlyList<int> targets,
            bool deferPhysicalRemoval = false)
        {
            if (targets.Count == 0) return new(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            if (targets.Count != 1) return Fail("Original card-play damage requires one native random target.");
            RoomCombatState? room = source.Rooms.SingleOrDefault(item => item.Units.Any(unit => unit.Id == targets[0]));
            if (room == null || source.Context == null) return Fail("Missing selected card-play damage target.");
            TrainCombatState state = CardSpellModel.WithContext(source, source.Context.WithQueryFrame(
                source.Context.QueryFrame?.With(selectedRoomIndex: room.RoomIndex)));
            room = state.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
            var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomCombatResult damaged = RoomCombatModel.ApplyRelicDamage(room, targets[0], 3,
                callback => pending.Add(callback.WithRelicDamageRemoval()));
            if (!damaged.Supported) return Fail(damaged.UnsupportedReason!);
            RelicDamageQueueModel.Import(ref state, damaged.State!);
            pending.AddRange(damaged.PendingCallbacks);
            var events = damaged.Events.ToList();
            var dispatches = damaged.Dispatches.ToList();
            var dispatched = new List<UnitCloneCallback>();
            RoomOutcome outcome = damaged.Outcome;
            if (!RelicDamageQueueModel.Settle(ref state, pending, events, dispatches, dispatched, ref outcome, out string? error, deferPhysicalRemoval))
                return Fail(error!);
            return new(state, outcome, new[] { damaged });
        }

        private static TrainCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
