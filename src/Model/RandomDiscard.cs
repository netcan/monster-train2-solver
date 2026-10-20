using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RandomDiscardModel
    {
        public static int? EligibleCount(CombatContext source, int sourceCardId, out string? error)
        {
            error = null;
            if (sourceCardId <= 0) { error = "Random discard requires the played card identity."; return null; }
            if (source.Cards.ExternalInteractions.Count > 0)
            { error = string.Join("; ", source.Cards.ExternalInteractions); return null; }
            if (source.CardInstances == null)
            { error = "Random discard requires captured card instances and traits."; return null; }

            int count = 0;
            foreach (CardToken token in source.Cards.Hand)
            {
                if (token.InstanceId == sourceCardId) continue;
                CardInstanceState? instance = source.CardInstances.FirstOrDefault(card => card.InstanceId == token.InstanceId);
                if (instance == null || instance.DataId != token.DataId)
                { error = "Random discard is missing a matching hand card instance."; return null; }
                if (instance.MaskDescriptor == null)
                { error = "Random discard requires branch-owned hand card trait metadata."; return null; }
                string? modifierError = CardModifierModel.UnsupportedReason(instance);
                if (modifierError != null) { error = modifierError; return null; }
                CardUpgradeMaskCard mask;
                try { mask = CardBranchMaskModel.Resolve(instance); }
                catch (InvalidOperationException exception) { error = exception.Message; return null; }
                if (mask.Traits.Contains("CardTraitInfusion", StringComparer.Ordinal) ||
                    mask.Traits.Contains("CardTraitPersistent", StringComparer.Ordinal)) continue;
                count++;
            }
            return count;
        }

        public static HandRemovalResult Apply(CombatContext source, int discardCount, int sourceCardId, int effectIndex,
            BattlePlayRules? definitions, IReadOnlyList<CardPileState>? otherPiles = null,
            bool placement = false, bool preview = false,
            Func<CombatContext, int, HandRemovalResult>? onTreasure = null)
        {
            if (discardCount < 0) return Unsupported("Negative random discard count is not modeled.");
            int? eligibleCount = EligibleCount(source, sourceCardId, out string? error);
            if (!eligibleCount.HasValue) return Unsupported(error!);

            CardToken[] remaining = source.Cards.Hand.Where(token => token.InstanceId != sourceCardId).Where(token =>
            {
                CardInstanceState instance = source.CardInstances!.Single(card => card.InstanceId == token.InstanceId);
                return !CardBranchMaskModel.Resolve(instance).Traits.Contains("CardTraitInfusion", StringComparer.Ordinal) &&
                    !CardBranchMaskModel.Resolve(instance).Traits.Contains("CardTraitPersistent", StringComparer.Ordinal);
            }).ToArray();
            int randomRemovals = Math.Max(0, remaining.Length - discardCount);
            UnityRng rng = source.BattleRng;
            var candidates = remaining.ToList();
            for (int index = 0; index < randomRemovals; index++)
            {
                RngDraw draw = rng.Range(0, candidates.Count);
                rng = draw.State;
                candidates.RemoveAt(draw.Value);
            }

            CombatContext withRng = source.WithBattleRng(rng);
            if (candidates.Count == 0)
                return new HandRemovalResult(withRng, otherPiles ?? source.OtherPiles, Array.Empty<CardToken>(), 0);
            return HandRemovalModel.Apply(withRng, 0, sourceCardId, effectIndex, definitions, otherPiles,
                selectedCardIds: candidates.Select(card => card.InstanceId).ToArray(), effectCounterType: null,
                triggeredByCard: true, placement: placement, preview: preview, onTreasure: onTreasure);
        }

        private static HandRemovalResult Unsupported(string error) =>
            new HandRemovalResult(null, null, Array.Empty<CardToken>(), 0, error);
    }
}
