using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicHealDamageModel
    {
        internal const string Effect = "RelicEffectDamageEnemyOnHeal";
        internal const string DataId = "ae618996-fea4-431e-8911-f9715f96c898";
        internal const string AssetKey = "HealToDamage";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicUnitResourceRule? trigger = effect.UnitTrigger;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) && effect.EffectIndex == 0 &&
                effect.ParamInt == 1 && effect.Amount == 1 && effect.ParamInt2 == 0 && effect.Conditions.Count == 0 &&
                effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && trigger != null &&
                trigger.Trigger == "OnHeal" && trigger.Team == "Monsters" && trigger.Subtype == null &&
                trigger.RequiredStatuses.Count == 0 && effect.UnitDamage?.TargetMode == "FrontInRoom"
                ? null : "Missing or changed original HealToDamage payload.";
        }

        internal static RelicCardPlayDamageSelection Select(TrainCombatState source, CombatUnit actor,
            string trigger, int relicIndex, int effectIndex)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return new(null, error: error);
            CombatRelicState? owner = source.Context?.Relics?.ElementAtOrDefault(relicIndex);
            RelicResourceEffect? effect = owner?.ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect && rule.EffectIndex == effectIndex);
            if (effect == null) return new(null, error: "Missing heal-damage owner/effect.");
            if (trigger != "OnHeal" || actor.Team != CombatTeam.Player) return new(source);
            if (source.Context?.SpawnPoints == null) return new(null, error: "Heal damage requires captured current spawn points.");
            SpawnPointReference? point = source.Context.SpawnPoints.Units.FirstOrDefault(unit => unit.UnitId == actor.Id)?.Current;
            if (point == null) return new(source);
            var living = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => unit.Health > 0).ToArray(), room.ExternalInteractions, room.Context,
                room.Preview, room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, source.Context);
            CardTargets targets = CardTargetModel.Collect(living, point.RoomIndex,
                new CardActionEffect("Damage", "FrontInRoom", 0, true, false, Array.Empty<CombatStatus>()), [], isTesting: true);
            return targets.Supported ? new(source, targets.UnitIds) : new(null, error: targets.UnsupportedReason);
        }

        internal static TrainCombatResult ApplySelected(TrainCombatState source, IReadOnlyList<int> targets,
            int actualHeal, bool deferPhysicalRemoval = false) => RelicCardPlayDamageModel.ApplySelected(source,
                targets, deferPhysicalRemoval, damageAmount: actualHeal, selectTargetRoom: false);
    }
}
