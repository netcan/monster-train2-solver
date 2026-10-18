using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardFreezeResult
    {
        public CombatContext? Context { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal CardFreezeResult(CombatContext? context, string? error = null)
        { Context = context; UnsupportedReason = error; }
    }

    public static class CardFreezeModel
    {
        public static IReadOnlyList<int> EligibleCards(CombatContext context, int sourceCardId, out string? error)
        {
            error = null;
            var eligible = new List<int>();
            foreach (CardToken token in context.Cards.Hand)
            {
                if (token.InstanceId == sourceCardId) continue;
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card?.MaskDescriptor == null || card.DataId != token.DataId)
                { error = "Chosen freeze requires captured hand-card traits and identities."; return Array.Empty<int>(); }
                try
                {
                    // Native FreezeCard permits Junk/Blight. Its only trait exclusion
                    // is an existing Freeze; Permafrost has already added that trait.
                    if (!CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitFreeze")) eligible.Add(token.InstanceId);
                }
                catch (InvalidOperationException failure) { error = failure.Message; return Array.Empty<int>(); }
            }
            return Array.AsReadOnly(eligible.ToArray());
        }
        public static CardFreezeResult Apply(CombatContext source, int sourceCardId, int? selectedCardId)
        {
            IReadOnlyList<int> eligible = EligibleCards(source, sourceCardId, out string? error);
            if (error != null) return new(null, error);
            if (eligible.Count == 0) return selectedCardId.HasValue ? new(null, "No eligible card for chosen freeze.") : new(source);
            if (!selectedCardId.HasValue || !eligible.Contains(selectedCardId.Value))
                return new(null, "Chosen freeze requires one eligible hand-card selection.");
            CardInstanceState card = source.FindCard(selectedCardId.Value)!;
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardInstanceState frozen = card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                descriptor.TemporaryTraits.Concat(new[] { new CardTraitValue("CardTraitFreeze", "CardTraitFreeze", 0, 0, true, 0) }).ToArray(),
                descriptor.PermanentReplacements, descriptor.SourceTraits, descriptor.KillTriggers));
            try { return new(CardMagicPowerModel.Refresh(source, frozen, out _)); }
            catch (InvalidOperationException failure) { return new(null, failure.Message); }
        }
    }
}
