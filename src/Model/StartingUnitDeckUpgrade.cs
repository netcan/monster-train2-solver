using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class StartingUnitDeckUpgradeState
    {
        public UnityRng? RewardsRng { get; }
        public bool ShouldSerializeRewardsRng() => RewardsRng.HasValue;
        public IReadOnlyList<CardUpgradeModifier> Choices { get; }
        public CardUpgradeMaskRule Filter { get; }
        public bool CanApplyInPreview { get; }

        public StartingUnitDeckUpgradeState(UnityRng? rewardsRng, IReadOnlyList<CardUpgradeModifier> choices,
            CardUpgradeMaskRule filter, bool canApplyInPreview)
        {
            RewardsRng = rewardsRng;
            Choices = Array.AsReadOnly(choices.ToArray());
            Filter = filter;
            CanApplyInPreview = canApplyInPreview;
        }

        internal StartingUnitDeckUpgradeState WithRng(UnityRng rng) => new(rng, Choices, Filter, CanApplyInPreview);
    }

    internal static class StartingUnitDeckUpgradeModel
    {
        internal const string DominionId = "caa8e46f-304b-4e0b-bf09-05b6c71b162a";
        internal const string DominionKey = "Echoes_FriendlyUnitsGetDominionBuffs";
        internal const string SavageryId = "8a0237aa-438f-4c76-8391-949c5e369d4b";
        internal const string SavageryKey = "Echoes_ChampionUnitsGetSavageryBuffs";
        internal const string Effect = "RelicEffectAddStartingUpgradeToUnitCards";
        internal const string DraftEffect = "RelicEffectAddStartingUpgradeToCardDrafts";

        internal static bool IsOriginalOwner(CombatRelicState owner) =>
            owner is { IsCovenant: false, DisallowedInPlacementPhase: false } &&
            (owner.DataId, owner.AssetKey, owner.EffectTypes) is (DominionId, DominionKey, [Effect, DraftEffect]) or
                (SavageryId, SavageryKey, [Effect]);

        internal static string? Validate(CombatContext? context)
        {
            if (context?.Relics == null) return null;
            foreach (CombatRelicState owner in context.Relics.Where(IsOriginalOwner))
            {
                bool dominion = owner.DataId == DominionId;
                StartingUnitDeckUpgradeState? pool = dominion ? context.Resources?.Dominion : context.Resources?.Savagery;
                if (context.Relics.Count(item => item.DataId == owner.DataId) != 1 || pool == null ||
                    !context.Resources!.MutatorIds.Contains(owner.DataId) || pool.Filter == null || pool.Choices.Count != 3 ||
                    pool.RewardsRng.HasValue != dominion ||
                    pool.Choices.Any(upgrade => string.IsNullOrEmpty(upgrade.DataId) || upgrade.ExternalInteractions.Count > 0) ||
                    pool.Choices.Select(upgrade => upgrade.DataId).Distinct().Count() != pool.Choices.Count)
                    return "The permanent unit upgrade effect requires its captured original pool and selection state.";
            }
            return null;
        }

        internal static string? Apply(CombatContext context, CardInstanceState added,
            out CardInstanceState upgraded, out CombatContext updated)
        {
            upgraded = added;
            updated = context;
            if (context.Relics?.Any(IsOriginalOwner) != true) return null;
            string? error = Validate(context);
            if (error != null) return error;
            if (added.MaskDescriptor?.Definition is not { } definition)
                return "Permanent unit upgrade effects require the captured card mask.";
            if (definition.CardType != "Monster") return null;
            bool allSubtypes = context.Relics.Any(owner => owner.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes"));
            foreach (CombatRelicState owner in context.Relics.Where(IsOriginalOwner))
            {
                BattleResourceState resources = updated.Resources!;
                StartingUnitDeckUpgradeState pool = (owner.DataId == DominionId ? resources.Dominion : resources.Savagery)!;
                if (context.IsolatedBattlePreview == true && !pool.CanApplyInPreview) continue;
                CardUpgradeMaskCard mask;
                try { mask = CardBranchMaskModel.Resolve(upgraded); }
                catch (InvalidOperationException invalid) { return invalid.Message; }
                if (!CardUpgradeMaskModel.FilterCard(pool.Filter, mask, allSubtypes)) continue;

                IReadOnlyList<CardUpgradeModifier> choices = pool.Choices;
                if (pool.RewardsRng is UnityRng rewards)
                {
                    CardInstanceState incumbent = upgraded;
                    if (incumbent.Permanent.Upgrades.Any(installed => choices.Any(choice => choice.DataId == installed.DataId))) continue;
                    RngDraw draw = rewards.Range(0, choices.Count);
                    choices = new[] { choices[draw.Value] };
                    updated = updated.WithResources(resources.WithDominion(pool.WithRng(draw.State)));
                }
                foreach (CardUpgradeModifier upgrade in choices)
                {
                    error = CardExhaustModel.PermanentUpgradeError(upgraded, upgrade);
                    if (error != null) return error;
                    upgraded = upgraded.WithPermanentUpgrade(upgrade, false);
                }
            }
            return null;
        }
    }
}
