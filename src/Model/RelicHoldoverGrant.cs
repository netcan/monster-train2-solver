using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicHandTraitRule
    {
        public string CardType { get; }
        public string SourceTeam { get; }
        public IReadOnlyList<CardTraitValue> Traits { get; }
        public IReadOnlyList<string> ExcludedTraits { get; }

        public RelicHandTraitRule(string cardType, string sourceTeam, IReadOnlyList<CardTraitValue> traits,
            IReadOnlyList<string>? excludedTraits)
        {
            CardType = cardType;
            SourceTeam = sourceTeam;
            Traits = Array.AsReadOnly(traits.ToArray());
            ExcludedTraits = Array.AsReadOnly((excludedTraits ?? Array.Empty<string>()).ToArray());
        }
    }

    internal static class RelicHoldoverGrantModel
    {
        internal const string Effect = "RelicEffectAddTraitToHand";
        internal const string OwnerId = "023d3df2-3402-454f-adf8-114cd9ba4918";
        internal const string OwnerKey = "AddHoldoverToSpells";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            if (owner.DataId != OwnerId || owner.AssetKey != OwnerKey || owner.IsCovenant != false ||
                owner.DisallowedInPlacementPhase != false || owner.EffectTypes.Count != 1 || effect.EffectIndex != 0 ||
                effect.Type != Effect || effect.Conditions.Count != 0 || effect.CanApplyInPreviewMode ||
                effect.RequiredMutatorId != null || effect.Amount != 0 ||
                effect.HandTrait is not { CardType: "Spell", SourceTeam: "Monsters" } rule ||
                rule.ExcludedTraits.Count != 0 || rule.Traits.Count != 1 ||
                rule.Traits[0] is not { RuntimeType: CardRetainModel.Trait, DeclaredName: CardRetainModel.Trait,
                    HoldoverPending: false, TemporaryReplacement: false })
                return "Missing or changed original Holdover hand-trait rule.";
            return null;
        }

        internal static RelicResourceResult CardAdded(CombatContext source, int cardId)
        {
            CombatContext context = source;
            if (source.Relics == null) return new RelicResourceResult(source, 0);
            foreach (CombatRelicState owner in source.Relics)
            {
                if (!owner.EffectTypes.Contains(Effect)) continue;
                var rule = owner.ResourceEffects?.SingleOrDefault(effect => effect.Type == Effect);
                if (rule == null) return new RelicResourceResult(null, 0, "Missing original Holdover hand-trait rule.");
                string? error = Validate(owner, rule);
                if (error != null) return new RelicResourceResult(null, 0, error);
                var result = Grant(context, cardId, rule.HandTrait!);
                if (!result.Supported) return result;
                context = result.Context!;
            }
            return new RelicResourceResult(context, 0);
        }

        internal static RelicResourceResult AfterDraw(CombatContext source, CombatRelicState owner,
            RelicResourceEffect effect, bool preview)
        {
            string? error = Validate(owner, effect);
            if (error != null) return new RelicResourceResult(null, 0, error);
            if (preview) return new RelicResourceResult(source, 0);
            CombatContext context = source;
            foreach (CardToken card in source.Cards.Hand)
            {
                var result = Grant(context, card.InstanceId, effect.HandTrait!);
                if (!result.Supported) return result;
                context = result.Context!;
            }
            return new RelicResourceResult(context, 0);
        }

        private static RelicResourceResult Grant(CombatContext source, int cardId, RelicHandTraitRule rule)
        {
            CardInstanceState? card = source.FindCard(cardId);
            CardMaskDescriptor? mask = card?.MaskDescriptor;
            if (card == null || mask == null || mask.Definition.DataId != card.DataId)
                return new RelicResourceResult(null, 0, "Holdover grant requires the native card definition and trait instances.");
            if (mask.Definition.CardType != rule.CardType || mask.Definition.IsChampionCard == true)
                return new RelicResourceResult(source, 0);
            if (!mask.Definition.IsChampionCard.HasValue)
                return new RelicResourceResult(null, 0, "Holdover grant requires the native Champion card flag.");
            var traits = CardExhaustModel.RefreshTraits(card);
            if (traits.CombinedTraits!.Any(trait => trait.RuntimeType == CardRetainModel.Trait))
                return new RelicResourceResult(source, 0);
            var temporary = mask.TemporaryTraits.Concat(rule.Traits).ToArray();
            var granted = card.WithMaskDescriptor(mask.WithHoldover(mask.BaseTraits, temporary, null));
            return new RelicResourceResult(source.WithCard(granted), 1);
        }
    }
}
