using System;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardEffectRange
    {
        public int Min { get; }
        public int Max { get; }
        public float Multiplier { get; }
        public CardEffectRange(int min, int max, float multiplier = 1f)
        { Min = min; Max = max; Multiplier = multiplier; }

        internal string? Validate()
        {
            if (float.IsNaN(Multiplier) || float.IsInfinity(Multiplier)) return "Nonfinite effect range multiplier.";
            // Use a float product, as Mathf.FloorToInt does in the native effect state.
            foreach (int endpoint in new[] { Min, Max })
            {
                float scaled = Multiplier * (float)endpoint;
                double floor = Math.Floor(scaled);
                if (floor < int.MinValue || floor > int.MaxValue) return "Effect range exceeds the integer result domain.";
            }
            return null;
        }

        public RngDraw Sample(UnityRng rng)
        {
            string? error = Validate();
            if (error != null) throw new ArgumentException(error);
            RngDraw draw = rng.Range(Min, Max);
            float scaled = Multiplier * (float)draw.Value;
            return new RngDraw(checked((int)Math.Floor(scaled)), draw.State);
        }
    }
}
