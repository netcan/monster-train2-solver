using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomPreCombatRule
    {
        public IReadOnlyList<CardActionEffect> Effects { get; }
        public bool DisallowedInDeployment { get; }
        public bool FocusRoom { get; }
        public int? ConsumedSpellDrawCount { get; }
        public RoomPreCombatRule(IReadOnlyList<CardActionEffect> effects, bool disallowedInDeployment, bool focusRoom,
            int? consumedSpellDrawCount = null)
        { Effects = Array.AsReadOnly(effects.ToArray()); DisallowedInDeployment = disallowedInDeployment; FocusRoom = focusRoom; ConsumedSpellDrawCount = consumedSpellDrawCount; }
    }
    public static class RoomPreCombatModel
    {
        public static string? Validate(RoomPreCombatRule rule) => rule.ConsumedSpellDrawCount.HasValue ?
            rule.ConsumedSpellDrawCount == 1 && rule.DisallowedInDeployment && !rule.FocusRoom && rule.Effects.Count == 0
                ? null : "Uncalibrated consumed-spell room draw." :
            !rule.DisallowedInDeployment || !rule.FocusRoom || rule.Effects.Count != 1 || rule.Effects.Any(effect =>
            effect.Type != "Damage" || effect.Target != "Room" || !effect.AllowPlayer || !effect.AllowEnemy || effect.Value != 50 ||
            effect.Statuses.Count > 0 || effect.Range != null || effect.Upgrade != null || effect.Filters != null &&
                (effect.Filters.Health != "Both" || effect.Filters.RequiredStatuses.Count > 0 || effect.Filters.ExcludedStatuses.Count > 0 ||
                 effect.Filters.IgnoreBosses || effect.Filters.Subtype.Length > 0 || effect.Filters.ExcludedSubtypes.Count > 0))
            ? "Uncalibrated room pre-combat effect composition." : null;

        internal static RoomCombatResult Apply(RoomCombatState source, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre)
        {
            RoomCombatState current = source;
            var events = new List<CombatEvent>();
            // Native DoRoomCombat calls the room hook in previews as well.
            // The nested Damage effect supports preview application even though
            // the room modifier advertises CanApplyInPreviewMode = false.
            foreach (RoomCapacityAttachmentState attachment in source.Context?.RoomCapacities?
                .FirstOrDefault(room => room.RoomIndex == source.RoomIndex)?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
            {
                RoomPreCombatRule? rule = attachment.PreCombat;
                if (rule == null || source.Deployment && rule.DisallowedInDeployment) continue;
                string? error = Validate(rule);
                if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, error);
                if (rule.ConsumedSpellDrawCount.HasValue)
                {
                    if (current.Preview) continue;
                    TypedDrawResult drawn = RoomConsumedSpellDrawModel.Apply(current.Context!);
                    if (!drawn.Supported) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, drawn.UnsupportedReason);
                    current = current.WithContext(drawn.Context!);
                    continue;
                }
                // Native attachment VFX selects the room before running effects.
                if (!current.Preview && rule.FocusRoom && current.Context?.QueryFrame != null)
                    current = current.WithContext(current.Context.WithQueryFrame(current.Context.QueryFrame.With(selectedRoomIndex: source.RoomIndex)));
                RoomCombatResult result = CardSpellModel.Apply(current, rule.Effects, 0, refreshPyre: refreshPyre);
                if (!result.Supported) return result;
                events.AddRange(result.Events);
                current = result.State!;
                if (result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
                    return new RoomCombatResult(current, result.Outcome, 0, events);
            }
            return new RoomCombatResult(current, RoomOutcome.Exchanged, 0, events);
        }
    }
}
