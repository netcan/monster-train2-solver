using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class DevotionState
    {
        public int Level { get; }
        public int Points { get; }
        public int FirstThreshold { get; }
        public int ThresholdIncrease { get; }
        public int PassiveStacksPerLevel { get; }
        public DevotionState(int level, int points, int firstThreshold, int thresholdIncrease, int passiveStacksPerLevel)
        { Level = level; Points = points; FirstThreshold = firstThreshold; ThresholdIncrease = thresholdIncrease; PassiveStacksPerLevel = passiveStacksPerLevel; }
        internal DevotionState With(int level, int points) => new DevotionState(level, points, FirstThreshold, ThresholdIncrease, PassiveStacksPerLevel);
    }

    public sealed class DevotionResult
    {
        public CombatContext? Context { get; }
        public bool LeveledUp { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal DevotionResult(CombatContext? context, bool leveledUp = false, string? error = null)
        { Context = context; LeveledUp = leveledUp; UnsupportedReason = error; }
    }

    public static class DevotionModel
    {
        public const string Effect = "GainDevotion";
        public static string? Validate(CombatContext? context)
        {
            DevotionState? state = context?.QueryFrame?.Devotion;
            if (state == null) return "Devotion requires captured save and balance inputs.";
            if (state.Level < 0 || state.Points < 0 || state.FirstThreshold < 1 || state.ThresholdIncrease < 0 ||
                (long)state.FirstThreshold + (long)state.ThresholdIncrease * state.Level > int.MaxValue)
                return "Devotion threshold configuration or signed level overflow requires native calibration.";
            return null;
        }
        public static int Threshold(DevotionState state) => Math.Max(1, unchecked(state.FirstThreshold +
            Math.Max(0, state.ThresholdIncrease) * Math.Max(0, state.Level)));
        public static int PassiveStacks(DevotionState state, int multiplier = 1) => state.Level < 1 ? 0 :
            unchecked(Math.Max(0, state.PassiveStacksPerLevel) * Math.Max(1, multiplier) * state.Level);
        public static bool Test(CombatContext? context, CardActionEffect effect) => context?.QueryFrame?.Devotion is DevotionState state &&
            unchecked((effect.Range?.Max ?? effect.Value) + PerLevel(state, effect.DevotionPerLevel ?? 0)) > 0;
        private static int PerLevel(DevotionState state, int amount) => state.Level > 0 ? unchecked(amount * state.Level) : 0;
        public static DevotionResult ApplyEffect(CombatContext source, CardActionEffect effect, bool preview = false) =>
            Award(source, unchecked(effect.Value + (source.QueryFrame?.Devotion is DevotionState state ? PerLevel(state, effect.DevotionPerLevel ?? 0) : 0)), preview);

        public static DevotionResult Award(CombatContext source, int amount, bool preview = false)
        {
            string? error = Validate(source);
            if (error != null) return Fail(error);
            if (preview || amount <= 0) return new DevotionResult(source);
            DevotionState state = source.QueryFrame!.Devotion!;
            int points = unchecked(state.Points + amount);
            int level = state.Level;
            if (points >= Threshold(state))
            {
                // The native loop consumes arithmetic thresholds. Sum them in
                // Int64 and binary-search the count, preserving the signed input
                // addition without spending one iteration per gained level.
                long threshold = Threshold(state), increase = state.ThresholdIncrease;
                long low = 0, high = Math.Min((long)points / threshold, (long)int.MaxValue - level);
                if (increase > 0) high = Math.Min(high, ((long)int.MaxValue - state.FirstThreshold) / increase - level);
                while (low < high)
                {
                    long count = low + (high - low + 1) / 2;
                    long cost = count * threshold + increase * count * (count - 1) / 2;
                    if (cost <= points) low = count; else high = count - 1;
                }
                long consumed = low * threshold + increase * low * (low - 1) / 2;
                points -= (int)consumed; level += (int)low;
                var next = state.With(level, points);
                if (points >= Threshold(next)) return Fail("Devotion level or threshold overflow requires native calibration.");
            }
            bool changed = level != state.Level;
            if (changed)
            {
                // The original Monolith consumers have no mutating resource
                // callback: they query their damage/status scaling when used.
                // Reject unknown live callbacks instead of silently losing them.
                foreach (CardInstanceState card in (source.CardInstances ?? Array.Empty<CardInstanceState>()).Where(card =>
                    source.Cards.Hand.Any(token => token.InstanceId == card.InstanceId) || card.PlayedRoomUnitIds?.Count > 0))
                {
                    if (card.MaskDescriptor?.SourceTraits == null) return Fail("Devotion level notifications require captured card trait provenance.");
                    if (card.MaskDescriptor.SourceTraits.Any(trait => trait.TrackedValue == "MonolithLevel" &&
                        trait.EntryDuration != "PreviousTurn" && trait.Type != "CardTraitScalingAddDamage" && trait.Type != "CardTraitScalingAddStatusEffect"))
                        return Fail("Uncalibrated Devotion level resource callback on " + card.DataId + ".");
                }
            }
            return new DevotionResult(source.WithQueryFrame(source.QueryFrame.With(devotion: state.With(level, Math.Max(0, points)))), changed);
        }
        private static DevotionResult Fail(string error) => new DevotionResult(null, error: error);
    }
}
