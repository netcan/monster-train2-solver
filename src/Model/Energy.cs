using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleEnergyState
    {
        public int Maximum { get; }
        public int NextTurn { get; }
        public int EveryTurn { get; }
        public string Phase { get; }
        public bool PyreAlive { get; }
        public BattleEnergyState(int maximum, int nextTurn, int everyTurn, string phase, bool pyreAlive)
        { Maximum = maximum; NextTurn = nextTurn; EveryTurn = everyTurn; Phase = phase; PyreAlive = pyreAlive; }
        internal BattleEnergyState With(string? phase = null, int? nextTurn = null, int? everyTurn = null, bool? pyreAlive = null)
            => new BattleEnergyState(Maximum, nextTurn ?? NextTurn, everyTurn ?? EveryTurn, phase ?? Phase, pyreAlive ?? PyreAlive);
    }

    public static class EnergyModel
    {
        public static bool IsEffect(string type) => type == "GainEnergy" || type == "GainEnergyMonsterTurn" ||
            type == "AdjustEnergy" || type == "GainEnergyNextTurn" || type == "GainEnergyEveryTurn";
        public static bool IsNativeEffect(string type) => type == "CardEffectGainEnergy" || type == "CardEffectAdjustEnergy" ||
            type == "CardEffectGainEnergyNextTurn" || type == "CardEffectGainEnergyEveryTurn";
        public static string? Validate(CombatContext? context)
        {
            if (context?.EnergyState == null || context.QueryFrame?.Energy == null || context.QueryFrame.RunningCombat == null)
                return "Energy effects require captured limits, phase, modifiers and current energy.";
            if (context.EnergyState.Maximum < 0 || !new[] { "Start", "UNUSED", "PreCombat", "MonsterTurn", "MonsterTurnQueueClear",
                "BossActionPreCombat", "Combat", "HeroTurn", "EndMonsterTurn", "BossActionPostCombat", "EndOfCombat" }.Contains(context.EnergyState.Phase))
                return "Invalid energy limit or combat phase.";
            return null;
        }
        public static bool Test(CombatContext context, string type) => type == "GainEnergyMonsterTurn"
            ? context.EnergyState!.Phase == "MonsterTurn" : type != "AdjustEnergy" ||
                context.EnergyState!.PyreAlive && context.QueryFrame!.RunningCombat == true;
        // Runtime gates are checked by the effect executor; native quantity sampling occurs only after those gates.
        public static CombatContext Apply(CombatContext context, string type, int amount)
        {
            string? error = Validate(context);
            if (error != null) throw new ArgumentException(error, nameof(context));
            BattleEnergyState energy = context.EnergyState!;
            if (type == "GainEnergyNextTurn")
                return amount > 0 ? context.WithEnergyState(energy.With(nextTurn: unchecked(energy.NextTurn + amount))) : context;
            if (type == "GainEnergyEveryTurn")
                return amount > 0 ? context.WithEnergyState(energy.With(everyTurn: unchecked(energy.EveryTurn + amount))) : context;
            if (type == "AdjustEnergy" && energy.Phase != "MonsterTurn")
                return context.WithEnergyState(energy.With(nextTurn: unchecked(energy.NextTurn + amount)));
            if (type == "GainEnergy" || type == "GainEnergyMonsterTurn")
                return amount > 0 && (type == "GainEnergy" || energy.Phase == "MonsterTurn") ? Add(context, amount) : context;
            if (type == "AdjustEnergy")
                return amount > 0 ? Add(context, amount) : context.WithQueryFrame(context.QueryFrame!.With(
                    energy: Math.Max(0, unchecked(context.QueryFrame.Energy!.Value - unchecked(-amount)))));
            throw new ArgumentException("Unknown energy effect.", nameof(type));
        }
        private static CombatContext Add(CombatContext context, int amount) => context.WithQueryFrame(context.QueryFrame!.With(
            energy: Math.Min(context.EnergyState!.Maximum, unchecked(context.QueryFrame.Energy!.Value + amount))));
        internal static CombatContext SetPhase(CombatContext context, string phase)
        {
            if (phase == "MonsterTurn" && context.LastSpawnedUnitId.HasValue) context = context.WithLastSpawned(0);
            return context.EnergyState == null ? context : context.WithEnergyState(context.EnergyState.With(phase: phase));
        }
        internal static CombatContext StartTurn(CombatContext context, int baseEnergy)
        {
            if (context.EnergyState == null) return context.WithQueryFrame(context.QueryFrame?.With(energy: baseEnergy));
            int income = unchecked(baseEnergy + context.EnergyState.NextTurn + context.EnergyState.EveryTurn);
            context = SetPhase(context, "MonsterTurn");
            if (income > 0) context = Add(context, income);
            return context.WithEnergyState(context.EnergyState!.With(nextTurn: 0));
        }
    }
}
