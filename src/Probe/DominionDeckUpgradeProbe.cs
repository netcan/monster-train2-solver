using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DominionDeckUpgradeProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static bool Matches(RelicState owner) =>
            CardBranchMaskProbe.Enabled && typeof(SaveManager).Module.ModuleVersionId == Module &&
            owner.GetRelicDataID() == DominionDeckUpgradeModel.OwnerId && owner.GetAssetName() == DominionDeckUpgradeModel.OwnerKey &&
            !owner.DisallowedInPlacementPhase && owner.GetEffects() is
                [RelicEffectAddStartingUpgradeToUnitCards, RelicEffectAddStartingUpgradeToCardDrafts];

        internal static DominionDeckUpgradeState? Capture(SaveManager save)
        {
            RelicState? active = save.GetMutators().FirstOrDefault(Matches);
            if (active == null ||
                active.GetEffects() is not [RelicEffectAddStartingUpgradeToUnitCards listener, RelicEffectAddStartingUpgradeToCardDrafts])
                return null;

            MutatorData owner = save.GetAllGameData().FindMutatorData(DominionDeckUpgradeModel.OwnerId);
            if (owner == null || owner.GetEffects() is not [var unitUpgrade, _] ||
                unitUpgrade.GetEffectConditions().Count != 0 || unitUpgrade.GetParamInt2() != (int)UnitEnhancerPoolSelection.SelectRandom ||
                unitUpgrade.GetParamCardFilter() is not { name: "ExcludeChampions" } filter ||
                unitUpgrade.GetParamEnhancerPool() is not EnhancerPool pool)
                return null;

            var choices = new List<EnhancerData>();
            pool.GetAllChoices(choices);
            var upgrades = new List<CardUpgradeModifier>(choices.Count);
            foreach (EnhancerData choice in choices)
            {
                if (choice.GetEffects() is not [var effect] || effect.GetParamCardType() != CardType.Monster ||
                    effect.GetParamCardUpgradeData() is not { } upgrade || upgrade.GetFilters().Count != 0)
                    return null;
                var state = new CardUpgradeState();
                state.Setup(upgrade);
                upgrades.Add(CardModifierProbe.Upgrade(state));
            }
            var words = RngCalibration.Words(RandomManager.GetState(RngId.Rewards));
            return new DominionDeckUpgradeState(new UnityRng(words[0], words[1], words[2], words[3]), upgrades,
                CardUpgradeMaskProbe.Rule(filter), listener.CanApplyInPreviewMode);
        }
    }
}
