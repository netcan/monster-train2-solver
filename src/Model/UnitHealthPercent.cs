using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class UnitHealthPercentModel
    {
        public const string NativeEffect = "CardEffectSetHealthPercent";
        public const string Effect = "SetHealthPercent";

        // SetHealth writes the primary HP field directly. It does not run the
        // damage, healing, Horde, half-health or death lifecycle APIs.
        public static CombatUnit Apply(CombatUnit target, int percent)
        {
            float health = (percent / 100f) * target.MaxHealth;
            int rounded = unchecked((int)Math.Ceiling(health));
            return target.WithHealth(Math.Min(99999, rounded));
        }

        public static RoomCombatResult Apply(RoomCombatState source, int targetId, int percent)
        {
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null || target.IsPyre || source.Context?.LifeLinks?.Actors.Any(actor => actor.Id == targetId) == true)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    "Raw linked/Pyre percentage health setters require native calibration.");
            CombatUnit changed = Apply(target, percent);
            // The live-room projection omits nonpositive actors even while the
            // engine retains their physical references. Do not lose those
            // references or fabricate their later lifecycle from this projection.
            if (changed.Health <= 0)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    "Nonpositive raw HP needs retained physical actor lifecycle calibration.");
            CombatContext? context = source.Context == null ? null : ConduitModel.Alive(source.Context, source.RoomIndex, changed);
            var room = new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(),
                source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds);
            return new RoomCombatResult(EnchantmentWorldModel.Sync(room), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }
    }
}
