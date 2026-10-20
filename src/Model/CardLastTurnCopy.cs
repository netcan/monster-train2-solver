using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardLastTurnCopyModel
    {
        public static IReadOnlyList<int> EligibleCards(CombatContext source, CardActionEffect effect, out string? error)
        {
            error = null;
            if (source.Statistics?.HandCardsPlayedLastTurn == null)
            { error = "Last-turn copying requires captured CardManager hand-play history."; return Array.Empty<int>(); }
            var result = new List<int>();
            if (effect.CardFilter == null) return result;
            foreach (int id in source.Statistics.HandCardsPlayedLastTurn)
            {
                CardInstanceState? card = source.FindCard(id);
                if (card?.MaskDescriptor?.Definition.IsChampionCard == null || !card.CanBeCopied.HasValue)
                { error = "Last-turn copying requires captured source metadata and copyability."; return Array.Empty<int>(); }
                try
                {
                    if (!card.MaskDescriptor.Definition.IsChampionCard.Value && card.CanBeCopied.Value &&
                        CardUpgradeMaskModel.FilterCard(effect.CardFilter, CardBranchMaskModel.Resolve(card))) result.Add(id);
                }
                catch (InvalidOperationException failure) { error = failure.Message; return Array.Empty<int>(); }
            }
            return result;
        }
        public static CardGenerationResult Apply(CombatContext source, CardActionEffect effect)
        {
            IReadOnlyList<int> candidates = EligibleCards(source, effect, out string? error);
            if (error != null) return new(null, Array.Empty<CardToken>(), error);
            if (candidates.Count == 0) return new(source, Array.Empty<CardToken>());
            RngDraw draw = source.Cards.Rng.Range(0, candidates.Count);
            CardInstanceState chosen = source.FindCard(candidates[draw.Value])!;
            CardCreationRule? creation = effect.Generation?.Pool.FirstOrDefault(value => value.DataId == chosen.DataId);
            if (creation == null) return new(null, Array.Empty<CardToken>(), "Missing original creation template for last-turn copy.");
            CardCycleState cards = source.Cards;
            CombatContext context = source.WithCards(new CardCycleState(cards.Hand, cards.Draw, cards.Discard, draw.State,
                cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn));
            return CardGenerationModel.Apply(context, new CardGenerationRule("HandPile", 1, new[] { creation },
                copyModifiers: true, upgrade: effect.Upgrade, deterministicPoolSelection: true, requireCopyableSource: true,
                permanent: false, optionalUpgradeFilters: effect.UpgradeFilters), chosen.InstanceId);
        }
    }
}
