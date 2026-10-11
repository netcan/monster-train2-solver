using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TypedDrawRule
    {
        public string CardType { get; }
        public CardUpgradeMaskRule? PrimaryFilter { get; }
        public CardUpgradeMaskRule? SecondaryFilter { get; }
        public TypedDrawRule(string cardType, CardUpgradeMaskRule? primaryFilter = null,
            CardUpgradeMaskRule? secondaryFilter = null)
        { CardType = cardType; PrimaryFilter = primaryFilter; SecondaryFilter = secondaryFilter; }
    }

    public sealed class TypedDrawResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<int> DrawnCardIds { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal TypedDrawResult(CombatContext? context, IReadOnlyList<int> drawnCardIds, string? error = null)
        { Context = context; DrawnCardIds = Array.AsReadOnly(drawnCardIds.ToArray()); UnsupportedReason = error; }
    }

    public static class TypedDrawModel
    {
        public static TypedDrawResult Draw(CombatContext source, TypedDrawRule rule, int count,
            int playedCardId, BattlePlayRules? definitions = null)
        {
            if (source.Cards.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", source.Cards.ExternalInteractions));
            if (source.MaxHandSize < 0 || source.Cards.Hand.Count > source.MaxHandSize)
                return Unsupported("Invalid typed-draw hand size.");
            CombatContext context = source;
            var drawn = new List<int>();
            var candidates = source.Cards.Discard.Concat(source.Cards.Draw).Reverse().ToArray();
            string? error = null;
            int matches = Process(rule.PrimaryFilter);
            if (error != null) return Unsupported(error);
            // A matched card counts even when DrawSpecificCard refuses it.
            // Fallback is attempted only if the primary matched no cards.
            if (matches == 0 && rule.PrimaryFilter != null && rule.SecondaryFilter != null)
                Process(rule.SecondaryFilter);
            return error == null ? new TypedDrawResult(context, drawn) : Unsupported(error);

            int Process(CardUpgradeMaskRule? filter)
            {
                int accepted = 0;
                foreach (CardToken token in candidates)
                {
                    if (accepted >= count) break;
                    if (token.InstanceId == playedCardId) continue;
                    CardInstanceState? card = context.FindCard(token.InstanceId);
                    if (filter != null)
                    {
                        if (card?.MaskDescriptor == null)
                        { error = "Filtered typed draw requires branch-owned card metadata."; break; }
                        if (!CardUpgradeMaskModel.FilterCard(filter, CardBranchMaskModel.Resolve(card))) continue;
                    }
                    else
                    {
                        string? type = card?.MaskDescriptor?.Definition.CardType ??
                            definitions?.Cards.FirstOrDefault(item => item.DataId == token.DataId)?.CardType;
                        if (type == null)
                        { error = "Typed draw requires each candidate's native card type."; break; }
                        if (type != rule.CardType) continue;
                    }
                    accepted++;
                    TypedDrawResult step = NextDrawUpgradeModel.DrawSpecific(context, token.InstanceId, playedCardId, definitions);
                    if (!step.Supported) { error = step.UnsupportedReason; break; }
                    context = step.Context!; drawn.AddRange(step.DrawnCardIds);
                }
                return accepted;
            }
        }

        private static TypedDrawResult Unsupported(string reason) => new TypedDrawResult(null, Array.Empty<int>(), reason);
    }
}
