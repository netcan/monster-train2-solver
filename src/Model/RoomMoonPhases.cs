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
            if (context.QueryFrame.MoonPhaseTrackingActive != true)
                return new MoonPhaseTransitionResult(null, "Inactive moon tracking refresh requires separate native calibration.");
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health > 0 && !unit.MoonPhaseMask.HasValue))
                return new MoonPhaseTransitionResult(null, "Room phase refresh requires captured character understanding masks.");
            string? callbackError = ValidateCallbacks(source);
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
            string? error = ValidateCallbacks(source);
            if (error != null) return new MoonPhaseTransitionResult(null, error);
            int prior = unit.MoonPhaseMask.Value;
            int mask = (prior & ~3) | Effective(source.Context, room!.RoomIndex, source.Context.QueryFrame.MoonPhase.Value);
            if ((prior & 3) == (mask & 3)) return new MoonPhaseTransitionResult(source);
            if (source.Context.QueryFrame.MoonPhaseTrackingActive == false &&
                !(unit is { Team: CombatTeam.Player, AssetKey: "MonsterChampionHellhornedHornbreaker",
                    Health: > 0, Size: 2, Modifiers.IsChampion: true } &&
                    (unit.MoonPhaseMask == 5 && source.Context.QueryFrame.MoonPhase == 2 ||
                     unit.MoonPhaseMask == 6 && source.Context.QueryFrame.MoonPhase == 1)) &&
                !(unit is { Team: CombatTeam.Enemy, AssetKey: "HeavyT2_Zealot", MoonPhaseMask: 6,
                    BaseAttack: 3, Attack: 3, Health: 110, MaxHealth: 110, Size: 2, IsBoss: false,
                    Modifiers.IsChampion: false, Modifiers.IsClone: false } && source.Context.QueryFrame.MoonPhase == 1) &&
                !(unit is { Team: CombatTeam.Enemy, AssetKey: "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack", MoonPhaseMask: 6,
                    BaseAttack: 13, Attack: 13, Health: 1025, MaxHealth: 1100, Size: 6, IsBoss: true,
                    Modifiers.IsChampion: false, Modifiers.IsClone: false } && source.Context.QueryFrame.MoonPhase == 1) &&
                !(unit is { Team: CombatTeam.Player, AssetKey: "MonsterBasher", MoonPhaseMask: 5,
                    Health: > 0, Size: 3, IsBoss: false,
                    Modifiers.IsChampion: false, Modifiers.IsClone: false } && source.Context.QueryFrame.MoonPhase == 2) &&
                !(unit is { Team: CombatTeam.Enemy, AssetKey: "HordeT3_PureSouls", MoonPhaseMask: 6,
                    Health: > 0, Size: 1, IsBoss: false,
                    Modifiers.IsChampion: false, Modifiers.IsClone: false } && source.Context.QueryFrame.MoonPhase == 1))
                return new MoonPhaseTransitionResult(null, "Inactive movement moon understanding requires native calibration.");
            if (room.Preview)
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
        internal static string? ValidateCallbacks(TrainCombatState source)
        {
            foreach (RoomMoonPhaseRule? rule in source.Context?.RoomCapacities?.SelectMany(room =>
                room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()).Select(attachment => attachment.MoonPhase)
                ?? Enumerable.Empty<RoomMoonPhaseRule?>())
                if (rule != null && Validate(rule) is string ruleError) return ruleError;
            if (source.Rooms.SelectMany(room => room.Units).SelectMany(unit => unit.Triggers)
                .Where(trigger => trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift")
                .SelectMany(trigger => trigger.Effects).Any(effect => effect.Type is not
                    ("CardEffectAddTempCardUpgradeToUnits" or "CardEffectRemoveTempUpgradeFromUnit")))
                return "Room moon callback payload requires independent native calibration.";
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health > 0 && !unit.IsPyre && !unit.MoonPhaseMask.HasValue))
                return "Room moon callbacks require captured prior understanding masks.";
            return source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger =>
                trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift"))
                ? TrainCombatModel.Validate(source) : null;
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
