using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomCapacityState
    {
        public int RoomIndex { get; }
        public int PlayerMaximum { get; }
        public int EnemyMaximum { get; }
        public bool IsPyre { get; }
        public RoomCapacityState(int roomIndex, int playerMaximum, int enemyMaximum, bool isPyre)
        { RoomIndex = roomIndex; PlayerMaximum = playerMaximum; EnemyMaximum = enemyMaximum; IsPyre = isPyre; }
    }
    public sealed class ScalingCapacityTrait
    {
        public CardStatisticQuery Query { get; }
        public int CapacityPerStat { get; }
        public ScalingCapacityTrait(CardStatisticQuery query, int capacityPerStat)
        { Query = query; CapacityPerStat = capacityPerStat; }
    }
    public sealed class RoomCapacityResult
    {
        public CombatContext? Context { get; }
        public int Amount { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal RoomCapacityResult(CombatContext? context, int amount, string? error = null)
        { Context = context; Amount = amount; UnsupportedReason = error; }
    }
    public static class RoomCapacityModel
    {
        public static string? Validate(CombatContext? context)
        {
            if (context?.RoomCapacities == null) return "Missing captured room capacity state.";
            return context.RoomCapacities.Any(room => room.RoomIndex < 0 || room.PlayerMaximum < 0 || room.EnemyMaximum < 0) ||
                context.RoomCapacities.Select(room => room.RoomIndex).Distinct().Count() != context.RoomCapacities.Count
                ? "Invalid room capacity state." : null;
        }
        public static int? Maximum(CombatContext? context, int room, CombatTeam team) =>
            context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room) is RoomCapacityState capacity
                ? team == CombatTeam.Player ? capacity.PlayerMaximum : capacity.EnemyMaximum : (int?)null;
        public static bool Test(RoomCombatState? room, CardActionEffect effect) => room != null &&
            Test(room.Context, room.RoomIndex, room.Units.Any(unit => unit.Team == CombatTeam.Enemy), effect.OnlyIfNoEnemies);
        internal static bool Test(CombatContext? context, int room, bool enemiesPresent, bool onlyIfNoEnemies) =>
            context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room)?.IsPyre == false && (!onlyIfNoEnemies || !enemiesPresent);
        public static int AdjustedMaximum(int maximum, int amount)
        {
            if (amount < 0 && maximum <= 1 || amount > 0 && maximum >= 30) return maximum;
            return Math.Max(1, Math.Min(30, unchecked(maximum + amount)));
        }
        public static RoomCapacityResult Apply(CombatContext context, int roomIndex, CardActionEffect effect, int sourceCardId = 0)
        {
            string? error = Validate(context);
            if (error != null) return new RoomCapacityResult(null, 0, error);
            RoomCapacityState? selected = context.RoomCapacities!.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (selected == null || selected.IsPyre) return new RoomCapacityResult(null, 0, "Missing playable capacity room.");
            int amount = effect.Value;
            if (sourceCardId > 0)
            {
                CardInstanceState? owner = context.FindCard(sourceCardId);
                if (owner == null) return new RoomCapacityResult(null, 0, "Missing capacity-trait owner.");
                foreach (ScalingCapacityTrait trait in owner.CapacityScalingTraits ?? Array.Empty<ScalingCapacityTrait>())
                {
                    StatisticQueryResult statistic = StatisticQueryModel.Evaluate(context, trait.Query, sourceCardId);
                    if (!statistic.Supported) return new RoomCapacityResult(null, 0, statistic.UnsupportedReason);
                    context = statistic.Context!; amount = unchecked(amount + trait.CapacityPerStat * statistic.Value);
                }
            }
            // Native exact Heroes selects enemies; None and combined team flags select players.
            bool enemy = effect.AllowEnemy && !effect.AllowPlayer;
            var changed = new RoomCapacityState(roomIndex,
                enemy ? selected.PlayerMaximum : AdjustedMaximum(selected.PlayerMaximum, amount),
                enemy ? AdjustedMaximum(selected.EnemyMaximum, amount) : selected.EnemyMaximum, selected.IsPyre);
            return new RoomCapacityResult(context.WithRoomCapacities(context.RoomCapacities!
                .Select(room => room.RoomIndex == roomIndex ? changed : room).ToArray()), amount);
        }
    }
}
