using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class DominionDeckUpgradeState
    {
        public UnityRng RewardsRng { get; }
        public IReadOnlyList<CardUpgradeModifier> Choices { get; }
        public CardUpgradeMaskRule Filter { get; }
        public bool CanApplyInPreview { get; }

        public DominionDeckUpgradeState(UnityRng rewardsRng, IReadOnlyList<CardUpgradeModifier> choices,
            CardUpgradeMaskRule filter, bool canApplyInPreview)
        {
            RewardsRng = rewardsRng;
            Choices = Array.AsReadOnly(choices.ToArray());
            Filter = filter;
            CanApplyInPreview = canApplyInPreview;
        }

        internal DominionDeckUpgradeState WithRng(UnityRng rng) => new(rng, Choices, Filter, CanApplyInPreview);
    }

    internal static class DominionDeckUpgradeModel
    {
        internal const string OwnerId = "caa8e46f-304b-4e0b-bf09-05b6c71b162a";
        internal const string OwnerKey = "Echoes_FriendlyUnitsGetDominionBuffs";
        internal const string Effect = "RelicEffectAddStartingUpgradeToUnitCards";

        internal static bool IsOriginalOwner(CombatRelicState owner) => owner is
        {
            DataId: OwnerId, AssetKey: OwnerKey, IsCovenant: false, DisallowedInPlacementPhase: false,
            EffectTypes: [Effect, "RelicEffectAddStartingUpgradeToCardDrafts"]
        };

        internal static string? Validate(CombatContext? context)
        {
            if (context?.Relics?.Any(IsOriginalOwner) != true) return null;
            if (context.Relics.Count(IsOriginalOwner) != 1 || context.Resources?.Dominion is not { } pool ||
                !context.Resources.MutatorIds.Contains(OwnerId) || pool.Filter == null || pool.Choices.Count != 3 ||
                pool.Choices.Any(upgrade => string.IsNullOrEmpty(upgrade.DataId) || upgrade.ExternalInteractions.Count > 0) ||
                pool.Choices.Select(upgrade => upgrade.DataId).Distinct().Count() != pool.Choices.Count)
                return "Dominion requires the captured original permanent deck upgrade pool and Rewards RNG.";
            return null;
        }

        internal static string? Select(CombatContext context, CardInstanceState added,
            out CardUpgradeModifier? upgrade, out BattleResourceState? resources)
        {
            upgrade = null;
            resources = context.Resources;
            if (context.Relics?.Any(IsOriginalOwner) != true) return null;
            string? error = Validate(context);
            if (error != null) return error;

            DominionDeckUpgradeState pool = resources!.Dominion!;
            if (context.IsolatedBattlePreview == true && !pool.CanApplyInPreview) return null;
            if (added.MaskDescriptor?.Definition is not { } definition)
                return "Dominion requires captured type and champion classification for permanently added cards.";
            if (definition.CardType != "Monster") return null;
            CardUpgradeMaskCard mask;
            try { mask = CardBranchMaskModel.Resolve(added); }
            catch (InvalidOperationException invalid) { return invalid.Message; }
            bool allSubtypes = context.Relics.Any(owner => owner.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes"));
            if (!CardUpgradeMaskModel.FilterCard(pool.Filter, mask, allSubtypes)) return null;
            if (added.Permanent.Upgrades.Any(installed => pool.Choices.Any(choice => choice.DataId == installed.DataId)))
                return null;

            RngDraw draw = pool.RewardsRng.Range(0, pool.Choices.Count);
            upgrade = pool.Choices[draw.Value];
            error = CardExhaustModel.PermanentUpgradeError(added, upgrade);
            if (error != null) return error;
            resources = resources.WithDominion(pool.WithRng(draw.State));
            return null;
        }
    }
}
