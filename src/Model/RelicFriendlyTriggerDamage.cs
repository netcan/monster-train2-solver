using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicFriendlyTriggerDamageRule
    {
        public string TargetMode { get; }
        public RelicFriendlyTriggerDamageRule(string targetMode) { TargetMode = targetMode; }
    }

    internal static class RelicFriendlyTriggerDamageModel
    {
        internal const string Effect = "RelicEffectDamageEnemyOnFriendlyTrigger";
        internal const string DataId = "38990528-6639-4cb2-b310-4e14193256a0";
        internal const string AssetKey = "DamageOnEaten";
        internal const string BurnoutDataId = "cb54fe76-9b3c-47f9-972c-424ec3ee39f7";
        internal const string BurnoutAssetKey = "DamageOnBurnoutDeath";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicUnitResourceRule? trigger = effect.UnitTrigger;
            bool eaten = owner.DataId == DataId && owner.AssetKey == AssetKey &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) && effect.EffectIndex == 0 &&
                trigger?.Trigger == "OnEaten" && trigger.RequiredStatuses.Count == 0 && effect.UnitDamage?.TargetMode == "Weakest";
            bool burnout = owner.DataId == BurnoutDataId && owner.AssetKey == BurnoutAssetKey &&
                owner.EffectTypes.SequenceEqual(new[] { Effect, Effect }, StringComparer.Ordinal) &&
                (effect.EffectIndex == 0 && trigger?.Trigger == "OnDeath" && trigger.RequiredStatuses.SequenceEqual(new[] { "ephemeral" }) ||
                 effect.EffectIndex == 1 && trigger?.Trigger == "OnBurnout" && trigger.RequiredStatuses.Count == 0) &&
                effect.UnitDamage?.TargetMode == "Room";
            return (eaten || burnout) && owner.DisallowedInPlacementPhase == false &&
                effect.ParamInt == 5 && effect.Amount == 5 && effect.ParamInt2 == 0 && effect.Conditions.Count == 0 &&
                effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && trigger != null &&
                trigger.Team == "Monsters" && trigger.Subtype == null
                ? null : "Missing or changed original friendly-trigger damage payload.";
        }

        internal static RelicCardPlayDamageSelection Select(TrainCombatState source, CombatUnit actor,
            string trigger, int roomIndex, int relicIndex, int effectIndex, int paramInt = 0)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return new(null, error: error);
            CombatRelicState? owner = source.Context?.Relics?.ElementAtOrDefault(relicIndex);
            RelicResourceEffect? effect = owner?.ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect && rule.EffectIndex == effectIndex);
            if (effect == null) return new(null, error: "Missing friendly-trigger damage owner/effect.");
            RelicUnitResourceRule rule = effect.UnitTrigger!;
            if (trigger != rule.Trigger || actor.Team != CombatTeam.Player || actor.IsPyre ||
                rule.RequiredStatuses.Any(status => !(actor.Status(status)?.Stacks > 0)) ||
                trigger == "OnDeath" && actor.Health > 0 && paramInt != 1) return new(source);
            var living = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => unit.Health > 0).ToArray(), room.ExternalInteractions, room.Context,
                room.Preview, room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, source.Context);
            CardTargets targets = CardTargetModel.Collect(living, roomIndex,
                new CardActionEffect("Damage", effect.UnitDamage!.TargetMode, 5, true, false, Array.Empty<CombatStatus>()), [], isTesting: true);
            return targets.Supported ? new(source, targets.UnitIds) : new(null, error: targets.UnsupportedReason);
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source, IReadOnlyList<int> targets,
            bool deferPhysicalRemoval = false)
        {
            TrainCombatState state = source;
            var rooms = new List<RoomCombatResult>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (int target in targets)
            {
                TrainCombatResult damaged = RelicCardPlayDamageModel.ApplySelected(state, new[] { target },
                    deferPhysicalRemoval, damageAmount: 5, selectTargetRoom: false, piercing: true);
                if (!damaged.Supported) return damaged;
                state = damaged.State!; rooms.AddRange(damaged.RoomResults);
                if (damaged.Outcome != RoomOutcome.Exchanged) outcome = damaged.Outcome;
            }
            return new(state, outcome, rooms);
        }
    }
}
