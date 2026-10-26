using System.Collections.Generic;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class JuiceTraitProbe
    {
        internal static readonly List<Sample> Samples = new List<Sample>();
        internal static readonly List<Sample> TripleSamples = new List<Sample>();
        private static bool captured;
        private static bool tripleCaptured;
        internal static void Calibrate(CardUpgradeState upgrade)
        {
#if DEVELOPMENT_PROBE
            if (!tripleCaptured && CardBranchMaskProbe.Enabled && upgrade.GetCardUpgradeDataId() == CardJuiceModel.TripleUpgrade)
                foreach (CardTraitData data in upgrade.GetTraitDataUpgrades())
                {
                    if (data.GetTraitStateName() != CardJuiceModel.TripleTrait) continue;
                    var trait = new CardTraitJuice3(); trait.Setup(data, CardState.None);
                    foreach (int input in new[] { int.MinValue, -3, 0, 1, 4, int.MaxValue })
                        TripleSamples.Add(new Sample { UpgradeId = upgrade.GetCardUpgradeDataId(), ParamInt = data.GetParamInt(),
                            Multiplier = trait.StackMultiplier, Input = input,
                            Bonus = trait.OnStatusEffectApplied(null!, CardState.None, AllGameManagers.Instance!.GetCoreManagers(), "valor", input) });
                    tripleCaptured = true;
                }
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
