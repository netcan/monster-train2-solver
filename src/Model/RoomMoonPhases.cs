using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomMoonPhaseRule
    {
        public int AdditionalMask { get; }
        public string SupportCardId { get; }
        public IReadOnlyList<CardActionEffect> PostCombatEffects { get; }
        public bool DisallowedInDeployment { get; }
        public RoomMoonPhaseRule(int additionalMask, string supportCardId, IReadOnlyList<CardActionEffect> postCombatEffects,
            bool disallowedInDeployment)
        { AdditionalMask = additionalMask; SupportCardId = supportCardId; PostCombatEffects = Array.AsReadOnly(postCombatEffects.ToArray());
            DisallowedInDeployment = disallowedInDeployment; }
    }
    public static class RoomMoonPhaseModel
    {
        internal static string? Validate(RoomMoonPhaseRule rule) => rule.AdditionalMask != 1 ||
            rule.SupportCardId != "9bee2809-87c3-481a-a72b-aa2e29d8d1a5" || rule.PostCombatEffects.Count != 1 ||
            rule.PostCombatEffects.Any(effect => effect.Type != "UnitUpgrade" || effect.Target != "Room" ||
                !effect.AllowEnemy || !effect.AllowPlayer || effect.Upgrade == null || effect.Range != null ||
                effect.Lifetime != "TemporaryUntilEndOfBattle" || effect.Filters?.Subtype != "SubtypesData_Silent" ||
                effect.Upgrade.Stats.Damage != 15 || effect.Upgrade.Stats.Health != 0 || effect.Upgrade.Stats.Size != 0 ||
                effect.Upgrade.Statuses.Count > 0 || effect.Upgrade.ExternalInteractions.Count > 0 || effect.Upgrade.TriggerUpgrades?.Count > 0 || effect.Upgrade.AbilityUpgrade != null)
            ? "Uncalibrated room moon-phase mask, support card or post-combat payload." : null;

        internal static int Effective(CombatContext? context, int roomIndex, int phase) => phase |
            (context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == roomIndex)?.Attachments?
                .Aggregate(0, (mask, attachment) => mask | (attachment.MoonPhase?.AdditionalMask ?? 0)) ?? 0);

        public static MoonPhaseTransitionResult Refresh(TrainCombatState source, bool characters)
        {
            CombatContext? context = source.Context;
            if (context?.QueryFrame?.MoonPhase is not (1 or 2))
                return new MoonPhaseTransitionResult(null, "Moon refresh requires a captured phase.");
            if (source.Rooms.Any(room => room.Preview))
                return new MoonPhaseTransitionResult(null, "Room moon-phase refresh previews require separate native calibration.");
            if (context.CardInstances?.Any(card => (card.MaskDescriptor?.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits)
                    .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal) && trait.RuntimeType != CardMoonUpgradeModel.Trait) == true) ||
                    card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits
                        .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true)) == true)
                return new MoonPhaseTransitionResult(null, "Moon resource refresh trait callbacks require native calibration.");
            context = CardMoonUpgradeModel.RefreshHand(context, out string? cardError);
            if (cardError != null) return new MoonPhaseTransitionResult(null, cardError);
            source = CardSpellModel.WithContext(source, context);
            // Refresh only changes player-resource traits and character phase understanding;
            // it does not shift the global phase or fire phase-shift relic callbacks.
            if (!characters) return new MoonPhaseTransitionResult(source);
            if (context.QueryFrame.MoonPhaseTrackingActive != true)
                return new MoonPhaseTransitionResult(null, "Inactive moon tracking refresh requires separate native calibration.");
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger =>
                    trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost")))
                return new MoonPhaseTransitionResult(null, "Room mask refresh character callbacks require native calibration.");
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health > 0 && !unit.MoonPhaseMask.HasValue))
                return new MoonPhaseTransitionResult(null, "Room phase refresh requires captured character understanding masks.");
            return new MoonPhaseTransitionResult(new TrainCombatState(source.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                unit.Health <= 0 ? unit : unit.WithMoonPhaseMask(Effective(context, room.RoomIndex, context.QueryFrame.MoonPhase.Value))).ToArray())).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, source.Context));
        }
    }
}
