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

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicUnitResourceRule? trigger = effect.UnitTrigger;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) && effect.EffectIndex == 0 &&
                effect.ParamInt == 5 && effect.Amount == 5 && effect.ParamInt2 == 0 && effect.Conditions.Count == 0 &&
                effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && trigger != null &&
                trigger.Trigger == "OnEaten" && trigger.Team == "Monsters" && trigger.Subtype == null &&
                trigger.RequiredStatuses.Count == 0 && effect.UnitDamage?.TargetMode == "Weakest"
                ? null : "Missing or changed original DamageOnEaten payload.";
        }

        internal static RelicCardPlayDamageSelection Select(TrainCombatState source, CombatUnit actor,
            string trigger, int roomIndex, int relicIndex, int effectIndex)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return new(null, error: error);
            CombatRelicState? owner = source.Context?.Relics?.ElementAtOrDefault(relicIndex);
            RelicResourceEffect? effect = owner?.ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect && rule.EffectIndex == effectIndex);
            if (effect == null) return new(null, error: "Missing friendly-trigger damage owner/effect.");
            if (trigger != "OnEaten" || actor.Team != CombatTeam.Player || actor.IsPyre) return new(source);
            var living = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => unit.Health > 0).ToArray(), room.ExternalInteractions, room.Context,
                room.Preview, room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, source.Context);
            CardTargets targets = CardTargetModel.Collect(living, roomIndex,
                new CardActionEffect("Damage", "Weakest", 5, true, false, Array.Empty<CombatStatus>()), [], isTesting: true);
            return targets.Supported ? new(source, targets.UnitIds) : new(null, error: targets.UnsupportedReason);
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source, IReadOnlyList<int> targets,
            bool deferPhysicalRemoval = false) =>
            RelicCardPlayDamageModel.ApplySelected(source, targets, deferPhysicalRemoval, damageAmount: 5, selectTargetRoom: false);
    }
}
