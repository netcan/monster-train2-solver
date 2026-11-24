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
                    .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal) &&
                        trait.RuntimeType != CardMoonUpgradeModel.Trait && trait.RuntimeType != CardMoonAdvanceScalingModel.Trait) == true) ||
                    card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits
                        .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true)) == true)
                return new MoonPhaseTransitionResult(null, "Moon resource refresh trait callbacks require native calibration.");
            foreach (CardInstanceState card in context.CardInstances ?? Array.Empty<CardInstanceState>())
                if (card.MaskDescriptor?.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits)
                    .Any(trait => trait.RuntimeType == CardMoonAdvanceScalingModel.Trait) == true)
                {
                    // Original Lunacy inherits the empty resource-refresh callback;
                    // its paid-cost scaling only runs when advancing the phase.
                    StatisticQueryResult validated = CardMoonAdvanceScalingModel.Adjust(context, card.InstanceId, 0);
                    if (!validated.Supported) return new MoonPhaseTransitionResult(null, validated.UnsupportedReason);
                }
            context = CardMoonUpgradeModel.RefreshHand(context, out string? cardError);
            if (cardError != null) return new MoonPhaseTransitionResult(null, cardError);
            source = CardSpellModel.WithContext(source, context);
            // Refresh only changes player-resource traits and character phase understanding;
            // it does not shift the global phase or fire phase-shift relic callbacks.
            if (!characters) return new MoonPhaseTransitionResult(source);
            if (context.QueryFrame.MoonPhaseTrackingActive is not (true or false))
                return new MoonPhaseTransitionResult(null, "Moon refresh requires captured tracking state.");
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health > 0 && !unit.MoonPhaseMask.HasValue))
                return new MoonPhaseTransitionResult(null, "Room phase refresh requires captured character understanding masks.");
            string? callbackError = ValidateCallbacks(source, includePhaseShift: false);
            if (callbackError != null) return new MoonPhaseTransitionResult(null, callbackError);
            var understood = new TrainCombatState(source.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                unit.Health <= 0 ? unit : unit.WithMoonPhaseMask(Effective(context, room.RoomIndex, context.QueryFrame.MoonPhase.Value))).ToArray())).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, source.Context);
            return ApplyUnderstanding(source, understood, phaseShift: false);
        }

        internal static bool HasMask(TrainCombatState state) => state.Context?.RoomCapacities?.Any(room =>
            room.Attachments?.Any(attachment => attachment.MoonPhase != null) == true) == true;

        // CardEffectBump updates just this actor after movement and shift/sentry
        // callbacks. Its phase triggers stay queued until the ordinary queue drain.
        internal static MoonPhaseTransitionResult RefreshMovedCharacter(TrainCombatState source, int id,
            ICollection<RoomCombatModel.QueuedCharacterTrigger> pending)
        {
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == id));
            CombatUnit? unit = room?.Units.FirstOrDefault(item => item.Id == id);
            if (unit == null || unit.Health <= 0 || source.Context?.QueryFrame?.MoonPhaseTrackingActive is not (true or false) ||
                source.Context.QueryFrame.MoonPhase is not (1 or 2))
                return new MoonPhaseTransitionResult(source);
            if (!unit.MoonPhaseMask.HasValue)
                return new MoonPhaseTransitionResult(null, "Movement moon understanding requires a captured prior mask.");
            if (source.Context.Relics?.Any(relic => relic.EffectTypes.Contains("RelicEffectMoonPhaseAlwaysActive")) == true)
                return new MoonPhaseTransitionResult(null, "Movement with relic moon-phase override requires native calibration.");
            CardInstanceState? spawner = source.Context.FindCard(unit.SpawnerCardId);
            if (spawner != null && (spawner.MaskDescriptor?.BaseTraits.Concat(spawner.MaskDescriptor.TemporaryTraits)
                .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true ||
                spawner.Permanent.Upgrades.Concat(spawner.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits
                    .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true)))
                return new MoonPhaseTransitionResult(null, "Moving units with moon resource traits requires native calibration.");
            string? error = ValidateCallbacks(source, includePhaseShift: false);
            if (error != null) return new MoonPhaseTransitionResult(null, error);
            int prior = unit.MoonPhaseMask.Value;
            int mask = (prior & ~3) | Effective(source.Context, room!.RoomIndex, source.Context.QueryFrame.MoonPhase.Value);
            if ((prior & 3) == (mask & 3)) return new MoonPhaseTransitionResult(source);
            if (room.Preview && !(source.Context.QueryFrame.MoonPhaseTrackingActive == false && spawner == null &&
                !HasMask(source) && unit.Triggers.Count == 2 && unit.Triggers.Any(trigger =>
                    RoomCombatModel.IsRailforgedAttackerKillBump(unit, trigger)) && unit.Triggers.Any(trigger => trigger.Kind == "OnShift")))
                return new MoonPhaseTransitionResult(null, "Preview movement moon understanding requires native calibration.");
            CombatUnit changed = unit.WithMoonPhaseMask(mask);
            void Queue(int bit, string gained, string lost)
            {
                if ((prior & bit) == (mask & bit)) return;
                string kind = (mask & bit) != 0 ? gained : lost;
                if (changed.Triggers.Any(trigger => trigger.Kind == kind))
                    pending.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, changed, kind));
            }
            Queue(2, "OnMoonLit", "OnMoonlitLost"); Queue(1, "OnMoonShade", "OnMoonshadeLost");
            RoomCombatState refreshed = EnchantmentWorldModel.Sync(room.WithUnits(room.Units.Select(actor => actor.Id == id ? changed : actor).ToArray()));
            var state = new TrainCombatState(source.Rooms.Select(item => item.RoomIndex == room.RoomIndex ? refreshed : item).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, refreshed.Context);
            return new MoonPhaseTransitionResult(refreshed.Context == null ? state : CardSpellModel.WithContext(state, refreshed.Context));
        }
        internal static string? ValidateCallbacks(TrainCombatState source, bool includePhaseShift = true)
        {
            foreach (RoomMoonPhaseRule? rule in source.Context?.RoomCapacities?.SelectMany(room =>
                room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()).Select(attachment => attachment.MoonPhase)
                ?? Enumerable.Empty<RoomMoonPhaseRule?>())
                if (rule != null && Validate(rule) is string ruleError) return ruleError;
            var callbacks = source.Rooms.SelectMany(room => room.Units)
                .SelectMany(unit => unit.Triggers.Select(trigger => (Unit: unit, Trigger: trigger)))
                .Where(item => item.Trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" ||
                    includePhaseShift && item.Trigger.Kind == "OnMoonPhaseShift");
            if (callbacks.Any(item => item.Trigger.Effects.Any(effect => effect.Type is not
                    ("CardEffectAddTempCardUpgradeToUnits" or "CardEffectRemoveTempUpgradeFromUnit")) &&
                !CalibratedCyclestaffShift(source.Context, item.Unit, item.Trigger)))
                return "Room moon callback payload requires independent native calibration.";
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health > 0 && !unit.IsPyre && !unit.MoonPhaseMask.HasValue))
                return "Room moon callbacks require captured prior understanding masks.";
            return callbacks.Any() ? TrainCombatModel.Validate(source) : null;
        }

        private static bool CalibratedCyclestaffShift(CombatContext? context, CombatUnit unit, CombatTrigger trigger)
        {
            const string staffId = "f8b6af14-e819-4bac-94c3-51babb939be9";
            return trigger is { Kind: "OnMoonPhaseShift", Once: false, IgnoreSilence: false, FireCount: 1,
                Origin: { UpgradeId: staffId, EquipmentCardId: > 0, IsFromEquipment: true, OnlyIfEquipped: false } origin,
                Effects: [{ Type: "CardEffectBuffEquipment", UnitUpgrade: { Type: "BuffEquipment", Target: "Self", Value: 0,
                    Range: null, Lifetime: "TemporaryUntilUnitDeath", ScaleUpgrade: false,
                    Upgrade: { DataId: "674d8fc6-81b8-4f3a-b0c1-0392df7f0ced", AssetKey: "LunaCyclestaff_EquipUpgrade",
                        Stats: { Damage: 1, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                        Statuses.Count: 0, TriggerUpgrades: null, AbilityUpgrade: null, ExternalInteractions.Count: 0 } } }] } &&
                unit.EquipmentCards?.Contains(origin.EquipmentCardId) == true &&
                context?.FindCard(origin.EquipmentCardId) is { DataId: staffId } card && card.EquippedUnitId == unit.Id;
        }

        internal static MoonPhaseTransitionResult ApplyUnderstanding(TrainCombatState before, TrainCombatState state, bool phaseShift)
        {
            var previous = before.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            {
                foreach (var member in state.Rooms.SelectMany(room => room.Units.Select(unit => (room, unit)))
                    .Where(member => member.unit.Team == team && member.unit.Health > 0).OrderBy(member => member.unit.Id))
                {
                    CombatUnit unit = member.unit;
                    int oldMask = previous[unit.Id].MoonPhaseMask ?? 0, mask = unit.MoonPhaseMask ?? 0;
                    void Add(string kind)
                    {
                        if (unit.Triggers.Any(trigger => trigger.Kind == kind))
                            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(member.room.RoomIndex, unit, kind));
                    }
                    // Native understanding compares Full first and New second.
                    // A room can keep New across a global phase transition.
                    if ((oldMask & 2) != (mask & 2)) Add((mask & 2) != 0 ? "OnMoonLit" : "OnMoonlitLost");
                    if ((oldMask & 1) != (mask & 1)) Add((mask & 1) != 0 ? "OnMoonShade" : "OnMoonshadeLost");
                    if (phaseShift) Add("OnMoonPhaseShift");
                }
                // Global shifts drain the monster queue before visiting heroes;
                // resource refresh queues both teams before draining once.
                if (phaseShift && queue.Count > 0)
                {
                    TrainCombatResult triggered = TrainCombatModel.ApplyCharacterQueue(state, queue);
                    if (!triggered.Supported) return new MoonPhaseTransitionResult(null, triggered.UnsupportedReason);
                    state = triggered.State!; queue.Clear();
                }
            }
            if (queue.Count > 0)
            {
                TrainCombatResult triggered = TrainCombatModel.ApplyCharacterQueue(state, queue);
                return new MoonPhaseTransitionResult(triggered.State, triggered.UnsupportedReason);
            }
            return new MoonPhaseTransitionResult(state);
        }
    }
}
