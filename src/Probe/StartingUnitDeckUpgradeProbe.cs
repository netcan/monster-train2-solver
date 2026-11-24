using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StartingUnitDeckUpgradeProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static bool Matches(RelicState owner) =>
            CardBranchMaskProbe.Enabled && typeof(SaveManager).Module.ModuleVersionId == Module &&
            !owner.DisallowedInPlacementPhase && (owner.GetRelicDataID(), owner.GetAssetName(), owner.GetEffects()) is
                (StartingUnitDeckUpgradeModel.DominionId, StartingUnitDeckUpgradeModel.DominionKey,
                    [RelicEffectAddStartingUpgradeToUnitCards, RelicEffectAddStartingUpgradeToCardDrafts]) or
                (StartingUnitDeckUpgradeModel.SavageryId, StartingUnitDeckUpgradeModel.SavageryKey,
                    [RelicEffectAddStartingUpgradeToUnitCards]);

        internal static StartingUnitDeckUpgradeState? Capture(SaveManager save, string ownerId)
        {
            RelicState? active = save.GetMutators().FirstOrDefault(owner => owner.GetRelicDataID() == ownerId && Matches(owner));
            if (active == null) return null;
            bool dominion = ownerId == StartingUnitDeckUpgradeModel.DominionId;
            var listener = (RelicEffectAddStartingUpgradeToUnitCards)active.GetEffects()[0];
            RelicEffectData unitUpgrade = active.GetSourceRelicData().GetEffects()[0];
            if (unitUpgrade.GetEffectConditions().Count != 0 || unitUpgrade.GetParamInt2() !=
                (int)(dominion ? UnitEnhancerPoolSelection.SelectRandom : UnitEnhancerPoolSelection.SelectAll) ||
                unitUpgrade.GetParamCardFilter() is not { } filter || filter.name != (dominion ? "ExcludeChampions" : "OnlyChampions") ||
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
            UnityRng? rewards = null;
            if (dominion)
            {
                var words = RngCalibration.Words(RandomManager.GetState(RngId.Rewards));
                rewards = new UnityRng(words[0], words[1], words[2], words[3]);
            }
            return new StartingUnitDeckUpgradeState(rewards, upgrades, CardUpgradeMaskProbe.Rule(filter), listener.CanApplyInPreviewMode);
        }
    }
}
