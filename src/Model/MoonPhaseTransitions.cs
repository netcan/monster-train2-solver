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

        public static string? Validate(TrainCombatState source, CardActionEffect effect, int sourceCardId)
        {
            if (effect.Type != "SetMoonPhase" && effect.Type != "AdvanceMoonPhase")
                return "Unknown moon-phase effect.";
            if (effect.Target is not ("Room" or "Self") || effect.Range != null)
                return "Moon-phase effects require a fixed room or self-targeted amount.";
            if (source.Context?.EnergyState == null)
                return "Moon-phase effects require the captured combat phase.";
            if (source.Context.EnergyState.Phase != "MonsterTurn") return null;
            if (effect.Type == "SetMoonPhase" && effect.Value != 1 && effect.Value != 2)
                return "Unmodeled moon-phase assignment.";
            StatisticQueryFrame? frame = source.Context?.QueryFrame;
            if (frame?.MoonPhase is not (1 or 2) || !frame.MoonPhaseTrackingActive.HasValue)
                return "Missing active moon-phase snapshot.";
            if (effect.Type == "AdvanceMoonPhase" && sourceCardId > 0)
            {
                CardInstanceState? card = source.Context!.FindCard(sourceCardId);
                if (card?.MaskDescriptor == null)
                    return "Moon-phase advance requires captured card traits.";
                if (CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitScalingAdvanceMoonPhase"))
                    return "Moon-phase advance scaling requires native trait calibration.";
            }
            if (effect.Type == "AdvanceMoonPhase" && effect.Value <= 0) return null;
            if (frame.MoonPhaseTrackingActive != true) return null;
            if (source.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Triggers.Any(trigger => UnitCallbacks.Contains(trigger.Kind))))
                return "Moon-phase unit callbacks require native ordering calibration.";
            if (source.Context!.Relics?.Any(relic => relic.EffectTypes.Any(type =>
                    type.Contains("MoonPhase", StringComparison.Ordinal) &&
                    type != RelicResourceModel.MoonPhaseEnergy)) == true)
                return "Unmodeled moon-phase relic callback.";
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => !unit.IsPyre) &&
                source.Context.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.AddStatusOnApplied) ||
                    relic.StatusStackAdditions?.Count > 0 || relic.StatusMagnitudes?.Count > 0) == true)
                return "Moon-phase status application with relic modifiers is not modeled.";
            if (source.Context.CardInstances?.Any(card =>
                    (card.MaskDescriptor?.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait =>
                        trait.RuntimeType == "CardTraitAddCardUpgradeOnMoonPhase") == true) ||
                    card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                        upgrade.Lifecycle?.AddedTraits.Any(trait =>
                            trait.RuntimeType == "CardTraitAddCardUpgradeOnMoonPhase") == true)) == true)
                return "Moon-phase card-upgrade trait callbacks are not modeled.";
            if (source.Context.Relics?.Any(relic => relic.EffectTypes.Contains(RelicResourceModel.MoonPhaseEnergy)) == true &&
                source.Context.Resources == null)
                return "Moon-phase relic energy requires captured resource limits.";
            return null;
        }

        public static MoonPhaseTransitionResult Apply(TrainCombatState source, CardActionEffect effect, int sourceCardId = 0)
        {
            string? error = Validate(source, effect, sourceCardId);
            if (error != null) return new MoonPhaseTransitionResult(null, error);
            if (source.Context!.EnergyState!.Phase != "MonsterTurn") return new MoonPhaseTransitionResult(source);
            TrainCombatState state = source;
            int phase = state.Context!.QueryFrame!.MoonPhase!.Value;
            int count = effect.Type == "SetMoonPhase" ? 1 : Math.Max(0, effect.Value);
            for (int index = 0; index < count; index++)
            {
                phase = effect.Type == "SetMoonPhase" ? effect.Value : phase == 1 ? 2 : 1;
                state = CardSpellModel.WithContext(state, state.Context!.WithQueryFrame(
                    state.Context.QueryFrame!.With(moonPhase: phase)));
                if (state.Context!.QueryFrame!.MoonPhaseTrackingActive != true) continue;
                state = MoonPhaseStatusModel.WithPhase(state, phase);
                if (state.Context!.Relics?.Any(relic => relic.EffectTypes.Contains(RelicResourceModel.MoonPhaseEnergy)) != true)
                    continue;
                RelicResourceResult energy = RelicResourceModel.ApplyMoonPhaseEnergy(state.Context,
                    phase == 1 ? "New" : "Full", preview: state.Rooms.Any(room => room.Preview),
                    placement: state.Rooms.Any(room => room.Deployment));
                if (!energy.Supported) return new MoonPhaseTransitionResult(null, energy.UnsupportedReason);
                state = CardSpellModel.WithContext(state, energy.Context!);
            }
            return new MoonPhaseTransitionResult(state);
        }
    }
}
