using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusRegistryCalibration
    {
        private static readonly HashSet<string> seen = new HashSet<string>();
        internal static readonly List<Sample> Samples = new List<Sample>();
        internal sealed class Sample
        {
            public int UnitId { get; set; }
            public CombatStatus[] Registry { get; set; } = null!;
            public int NativeTotal { get; set; }
            public int NativeVisible { get; set; }
        }
        internal static void Capture(CharacterState unit, int id, IReadOnlyList<CombatStatus> registry)
        {
            string key = id + ";" + string.Join("|", registry.Select(status => status.Id + ":" + status.Stacks + ":" + status.Hidden + ":" + status.DisplayCategory));
            if (!seen.Add(key)) return;
            Samples.Add(new Sample
            {
                UnitId = id,
                Registry = registry.ToArray(),
                NativeTotal = unit.GetNumberUniqueStatusEffects(),
                NativeVisible = unit.GetNumberUniqueStatusEffectsInCategory(StatusEffectData.DisplayCategory.Positive, true) +
                    unit.GetNumberUniqueStatusEffectsInCategory(StatusEffectData.DisplayCategory.Negative, true)
            });
        }
    }
}
