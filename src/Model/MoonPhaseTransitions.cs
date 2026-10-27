using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class MoonPhaseTransitionResult
    {
        public TrainCombatState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal MoonPhaseTransitionResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    public static class MoonPhaseTransitionModel
    {
        private static readonly HashSet<string> UnitCallbacks = new HashSet<string>(StringComparer.Ordinal)
        {
            "OnMoonLit", "OnMoonlitLost", "OnMoonShade", "OnMoonshadeLost", "OnMoonPhaseShift"
        };

        public static string? Validate(TrainCombatState source, CardActionEffect effect, int sourceCardId,
            bool playerActionOnly = true)
        {
            if (effect.Type != "SetMoonPhase" && effect.Type != "AdvanceMoonPhase")
                return "Unknown moon-phase effect.";
            if (effect.Target is not ("Room" or "Self") || effect.Range != null)
                return "Moon-phase effects require a fixed room or self-targeted amount.";
            if (playerActionOnly && source.Context?.EnergyState == null)
                return "Moon-phase effects require the captured combat phase.";
            if (playerActionOnly && source.Context!.EnergyState!.Phase != "MonsterTurn") return null;
            if (effect.Type == "SetMoonPhase" && effect.Value != 1 && effect.Value != 2)
                return "Unmodeled moon-phase assignment.";
            StatisticQueryFrame? frame = source.Context?.QueryFrame;
            if (frame?.MoonPhase is not (1 or 2) || !frame.MoonPhaseTrackingActive.HasValue)
                return "Missing active moon-phase snapshot.";
            int advanceCount = effect.Value;
            if (effect.Type == "AdvanceMoonPhase" && sourceCardId > 0)
            {
                CardInstanceState? card = source.Context!.FindCard(sourceCardId);
                if (card?.MaskDescriptor == null)
                    return "Moon-phase advance requires captured card traits.";
                StatisticQueryResult scaled = CardMoonAdvanceScalingModel.Adjust(source.Context, sourceCardId, advanceCount);
                if (!scaled.Supported) return scaled.UnsupportedReason;
                advanceCount = scaled.Value;
            }
            if (effect.Type == "AdvanceMoonPhase" && advanceCount <= 0) return null;
            if (frame.MoonPhaseTrackingActive != true) return null;
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger =>
                    UnitCallbacks.Contains(trigger.Kind))))
            {
                bool roomMasks = RoomMoonPhaseModel.HasMask(source);
                if (roomMasks)
                {
                    string? maskError = RoomMoonPhaseModel.ValidateCallbacks(source);
                    if (maskError != null) return maskError;
                }
                int oldPhase = frame.MoonPhase.Value;
                string oldStatus = oldPhase == 1 ? "newmoon" : "fullmoon";
                string otherStatus = oldPhase == 1 ? "fullmoon" : "newmoon";
                if (!roomMasks && source.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre && unit.Health > 0)
                    .Any(unit => unit.Status(oldStatus)?.Stacks != 1 || unit.Status(otherStatus) != null))
                    return "Moon-phase callbacks require known prior unit phase masks.";
                string? trainError = TrainCombatModel.Validate(source);
                if (trainError != null) return trainError;
            }
            if (source.Context!.Relics?.Any(relic => relic.EffectTypes.Any(type =>
                    type.Contains("MoonPhase", StringComparison.Ordinal) &&
                    type != RelicResourceModel.MoonPhaseEnergy && type != RelicMoonPhaseHealModel.Effect &&
                    type != RelicMoonPhaseStatusModel.Effect)) == true)
                return "Unmodeled moon-phase relic callback.";
            if (frame.MoonPhaseTrackingActive == true)
            {
                int phase = frame.MoonPhase.Value;
                int count = effect.Type == "SetMoonPhase" ? 1 : Math.Max(0, advanceCount);
                for (int index = 0; index < count; index++)
                {
                    phase = effect.Type == "SetMoonPhase" ? effect.Value : phase == 1 ? 2 : 1;
                    string? healError = RelicMoonPhaseHealModel.ValidateTransition(source, phase);
                    if (healError != null) return healError;
                    string? statusError = RelicMoonPhaseStatusModel.ValidateTransition(source, phase,
                        source.Rooms.Any(room => room.Preview), source.Rooms.Any(room => room.Deployment));
                    if (statusError != null) return statusError;
                }
            }
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => !unit.IsPyre) &&
                source.Context.Relics?.Any(relic => (relic.EffectTypes.Contains(RelicModel.AddStatusOnApplied) ||
                    relic.StatusStackAdditions?.Count > 0 || relic.StatusMagnitudes?.Count > 0) &&
                    !IsCalibratedBurnoutLossRelic(relic)) == true)
                return "Moon-phase status application with relic modifiers is not modeled.";
            foreach (CardInstanceState card in source.Context.CardInstances ?? Array.Empty<CardInstanceState>())
                if (CardMoonUpgradeModel.Validate(card) is string moonError) return moonError;
            if (source.Context.Relics?.Any(relic => relic.EffectTypes.Contains(RelicResourceModel.MoonPhaseEnergy)) == true &&
                source.Context.Resources == null)
                return "Moon-phase relic energy requires captured resource limits.";
            return null;
        }

        // This native callback has no status-addition behavior and only responds
        // to removed Burnout. Switching new/full Moon cannot fire it. Keep actual
        // Burnout removal on the existing ordered status-removal callback path.
        private static bool IsCalibratedBurnoutLossRelic(CombatRelicState relic) =>
            relic.DataId == "85667d60-8053-4204-8c02-ba101dab7945" && relic.AssetKey == "RageOnBurnoutStackLoss" &&
            relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
            relic.EffectTypes.SequenceEqual(new[] { RelicModel.AddStatusOnOtherStatusRemoved }) &&
            (relic.StatusMagnitudes?.Count ?? 0) == 0 && relic.StatusStackAdditions?.Count == 1 &&
            relic.StatusStackAdditions[0] is { EffectIndex: 0, SourceTeam: "Monsters", StatusId: "buff",
                TriggerStatusId: "ephemeral", AdditionalStacks: 3, AddCondition: 0, Conditions.Count: 0,
                AddedStatusDefinition: { Id: "buff", Stacks: 3, ParamInt: 2 } } &&
            RelicStatusStackModel.Validate(relic) == null;

        public static MoonPhaseTransitionResult Apply(TrainCombatState source, CardActionEffect effect, int sourceCardId = 0,
            bool playerActionOnly = true)
        {
            string? error = Validate(source, effect, sourceCardId, playerActionOnly);
            if (error != null) return new MoonPhaseTransitionResult(null, error);
            if (playerActionOnly && source.Context!.EnergyState!.Phase != "MonsterTurn") return new MoonPhaseTransitionResult(source);
            TrainCombatState state = source;
            int phase = state.Context!.QueryFrame!.MoonPhase!.Value;
            StatisticQueryResult amount = effect.Type == "AdvanceMoonPhase" ? CardMoonAdvanceScalingModel.Adjust(state.Context, sourceCardId, effect.Value) :
                new StatisticQueryResult(state.Context, effect.Value);
            if (!amount.Supported) return new MoonPhaseTransitionResult(null, amount.UnsupportedReason);
            int count = effect.Type == "SetMoonPhase" ? 1 : Math.Max(0, amount.Value);
            for (int index = 0; index < count; index++)
            {
                int oldPhase = phase;
                phase = effect.Type == "SetMoonPhase" ? effect.Value : phase == 1 ? 2 : 1;
                state = CardSpellModel.WithContext(state, state.Context!.WithQueryFrame(
                    state.Context.QueryFrame!.With(moonPhase: phase)));
                if (state.Context!.QueryFrame!.MoonPhaseTrackingActive != true) continue;
                TrainCombatState beforeUnderstanding = state;
                state = MoonPhaseStatusModel.WithPhase(state, phase);
                CombatContext hand = CardMoonUpgradeModel.RefreshHand(state.Context!, out string? cardUpgradeError);
                if (cardUpgradeError != null) return new MoonPhaseTransitionResult(null, cardUpgradeError);
                state = CardSpellModel.WithContext(state, hand);
                if (state.Context!.Relics?.Any(relic => relic.EffectTypes.Contains(RelicResourceModel.MoonPhaseEnergy)) == true)
                {
                    RelicResourceResult energy = RelicResourceModel.ApplyMoonPhaseEnergy(state.Context,
                        phase == 1 ? "New" : "Full", preview: state.Rooms.Any(room => room.Preview),
                        placement: state.Rooms.Any(room => room.Deployment));
                    if (!energy.Supported) return new MoonPhaseTransitionResult(null, energy.UnsupportedReason);
                    state = CardSpellModel.WithContext(state, energy.Context!);
                }
                MoonPhaseHealResult heal = RelicMoonPhaseHealModel.Apply(state, phase);
                if (!heal.Supported) return new MoonPhaseTransitionResult(null, heal.UnsupportedReason);
                state = heal.State!;
                RelicMoonPhaseStatusResult statuses = RelicMoonPhaseStatusModel.Apply(state, phase,
                    state.Rooms.Any(room => room.Preview), state.Rooms.Any(room => room.Deployment));
                if (!statuses.Supported) return new MoonPhaseTransitionResult(null, statuses.UnsupportedReason);
                state = statuses.State!;
                MoonPhaseTransitionResult roomEffects = RelicMooncycleRoomEffectModel.Apply(state,
                    state.Rooms.Any(room => room.Preview), state.Rooms.Any(room => room.Deployment));
                if (!roomEffects.Supported) return roomEffects;
                state = roomEffects.State!;
                if (RoomMoonPhaseModel.HasMask(state))
                {
                    MoonPhaseTransitionResult understood = RoomMoonPhaseModel.ApplyUnderstanding(beforeUnderstanding, state, phaseShift: true);
                    if (!understood.Supported) return understood;
                    state = understood.State!;
                    continue;
                }
                foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                {
                    var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
                    foreach (var member in state.Rooms.SelectMany(room => room.Units.Select(unit => (room, unit)))
                        .Where(member => member.unit.Team == team && member.unit.Health > 0)
                        .OrderBy(member => member.unit.Id))
                    {
                        RoomCombatState room = member.room;
                        CombatUnit unit = member.unit;
                        string gained = phase == 2 ? "OnMoonLit" : "OnMoonShade";
                        string lost = oldPhase == 2 ? "OnMoonlitLost" : "OnMoonshadeLost";
                        if (oldPhase != phase)
                        {
                            string first = phase == 2 ? gained : lost;
                            string second = phase == 2 ? lost : gained;
                            if (unit.Triggers.Any(trigger => trigger.Kind == first))
                                callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, unit, first));
                            if (unit.Triggers.Any(trigger => trigger.Kind == second))
                                callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, unit, second));
                        }
                        if (unit.Triggers.Any(trigger => trigger.Kind == "OnMoonPhaseShift"))
                            callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, unit, "OnMoonPhaseShift"));
                    }
                    if (callbacks.Count == 0) continue;
                    TrainCombatResult triggered = TrainCombatModel.ApplyCharacterQueue(state, callbacks);
                    if (!triggered.Supported) return new MoonPhaseTransitionResult(null, triggered.UnsupportedReason);
                    state = triggered.State!;
                }
            }
            return new MoonPhaseTransitionResult(state);
        }
    }
}
