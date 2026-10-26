using System.Linq;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    internal static class MoonPhaseStatusModel
    {
        internal static bool IsTracking(CombatContext? context, IEnumerable<CombatUnit> units) =>
            context?.QueryFrame?.MoonPhaseTrackingActive ?? units.Any(unit =>
                unit.Status("newmoon")?.Stacks > 0 || unit.Status("fullmoon")?.Stacks > 0);

        internal static CombatUnit WithPhase(CombatUnit unit, int phase)
        {
            if (unit.IsPyre || phase != 1 && phase != 2) return unit;
            string id = phase == 1 ? "newmoon" : "fullmoon";
            var statuses = unit.Statuses.Where(status => status.Id != "newmoon" && status.Id != "fullmoon").ToList();
            statuses.Add(new CombatStatus(id, 1, 1, triggerVfxEnemy: false, triggerVfxPlayer: false,
                stackable: false, hidden: true, displayCategory: "Persistent", propagatable: false,
                excludeHeroPropagation: false, excludeMonsterPropagation: false));
            return CardSpellModel.Copy(unit, unit.Health, statuses);
        }

        internal static TrainCombatState WithPhase(TrainCombatState source, int phase)
        {
            if (phase != 1 && phase != 2) return source;
            RoomCombatState[] rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => WithPhase(unit, phase)
                    .WithMoonPhaseMask(RoomMoonPhaseModel.Effective(source.Context, room.RoomIndex, phase))).ToArray(), room.ExternalInteractions,
                room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            // Native enchantment targeting reads the same live characters.
            // Rebase its retained world after changing understanding masks,
            // even when this phase has no character callbacks to drain.
            CombatContext context = source.Context!;
            // HeroManager retains these unplaced births in its active roster;
            // the next real moon change reaches them before any new preview births.
            bool FollowsMoon(EnchantmentRetainedUnit actor) => actor.RoomIndex == -1 &&
                actor.Unit.AssetKey is "Boss_Level3_MiniFleshweaver_Corruption" or "Boss_Level3_MiniFleshweaver_MoreMass" &&
                actor.Unit.Health > 0 && actor.Unit.DeathState?.IsDestroyed != true;
            if (context.Enchantments is EnchantmentWorld world && world.RetainedUnits.Any(FollowsMoon))
                context = context.WithEnchantments(new EnchantmentWorld(world.Rooms, world.Movement, world.EnemySlotsPerRoom,
                    world.RetainedUnits.Select(actor => FollowsMoon(actor)
                        ? new EnchantmentRetainedUnit(WithPhase(actor.Unit, phase).WithMoonPhaseMask(phase), actor.RoomIndex, actor.Preview) : actor).ToArray(),
                    world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle,
                    world.TemporaryPreviewUnitIds));
            return EnchantmentWorldModel.Rebase(CardSpellModel.WithContext(new TrainCombatState(rooms, source.Movement,
                source.EnemySlotsPerRoom, context), context));
        }
    }
}
