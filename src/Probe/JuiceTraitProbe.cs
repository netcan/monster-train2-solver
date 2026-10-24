using System.Collections.Generic;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class JuiceTraitProbe
    {
        internal static readonly List<Sample> Samples = new List<Sample>();
        private static bool captured;
        internal static void Calibrate(CardUpgradeState upgrade)
        {
#if DEVELOPMENT_PROBE
            if (captured || !CardBranchMaskProbe.Enabled || upgrade.GetCardUpgradeDataId() != CardJuiceModel.Upgrade) return;
            foreach (CardTraitData data in upgrade.GetTraitDataUpgrades())
            {
                if (data.GetTraitStateName() != CardJuiceModel.Trait) continue;
                var trait = new CardTraitJuice(); trait.Setup(data, CardState.None);
                foreach (int input in new[] { int.MinValue, -3, 0, 1, 4, int.MaxValue })
                    Samples.Add(new Sample { UpgradeId = upgrade.GetCardUpgradeDataId(), ParamInt = data.GetParamInt(),
                        Multiplier = trait.StackMultiplier, Input = input,
                        Bonus = trait.OnStatusEffectApplied(null!, CardState.None, AllGameManagers.Instance!.GetCoreManagers(), "lifesteal", input) });
                captured = true;
            }
#endif
        }
        internal sealed class Sample
        {
            public string UpgradeId { get; set; } = "";
            public int ParamInt { get; set; }
            public int Multiplier { get; set; }
            public int Input { get; set; }
            public int Bonus { get; set; }
        }
    }
}
