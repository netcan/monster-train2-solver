using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicPersistentMonsterHealthModel
    {
        internal const string Effect = "RelicEffectPersistMonsterHp";
        internal const string OwnerId = "4d5b56f1-65b8-4206-ace7-3629d623e0ec";
        internal const string OwnerKey = "Echoes_FriendlyUnitHealthPersistsBetweenBattles";

        internal static bool IsOriginal(CombatRelicState relic) => relic.DataId == OwnerId && relic.AssetKey == OwnerKey &&
            relic.EffectTypes.Count == 1 && relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false;

        internal static RoomCombatResult RestoreAtBirth(RoomCombatState room, CombatUnit unit)
        {
            int saved = Spawner(room.Context!, unit)?.Permanent.PersistentHealth ?? 0;
            if (saved <= 0 || saved == unit.Health) return new RoomCombatResult(room, RoomOutcome.Exchanged, 0, new());
            if (saved < unit.Health)
            {
                return new RoomCombatResult(room.WithUnits(room.Units.Select(other => other.Id == unit.Id
                    ? unit.WithHealthAndMaxHealth(saved, unit.MaxHealth) : other).ToArray()), RoomOutcome.Exchanged, 0, new());
            }

            return UnitHealthModel.ApplyNative(room, unit.Id, saved - unit.Health);
        }

        internal static CombatContext Record(CombatContext context, CombatUnit unit, int health)
        {
            CardInstanceState? card = Spawner(context, unit);
            return card == null || card.Permanent.PersistentHealth == health ? context :
                context.WithCard(card.WithModifiers(card.Permanent.WithPersistentHealth(health), card.Temporary, refreshTraits: false));
        }

        private static CardInstanceState? Spawner(CombatContext context, CombatUnit unit) =>
            unit.Team == CombatTeam.Player && !unit.IsPyre && unit.SpawnerCardId > 0 ? context.FindCard(unit.SpawnerCardId) : null;
    }
}
