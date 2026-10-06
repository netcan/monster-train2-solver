using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardCreationRule
    {
        public string DataId { get; }
        public CardModifiers StartingModifiers { get; }
        public IReadOnlyList<CardEffectCounter>? EffectCounters { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public IReadOnlyList<ScalingDamageTrait>? DamageScalingTraits { get; }
        public CardCreationRule(string dataId, CardModifiers startingModifiers, IReadOnlyList<CardEffectCounter>? effectCounters,
            IReadOnlyList<string> externalInteractions, IReadOnlyList<ScalingDamageTrait>? damageScalingTraits = null)
        { DataId = dataId; StartingModifiers = startingModifiers; EffectCounters = effectCounters == null ? null : Array.AsReadOnly(effectCounters.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            DamageScalingTraits = damageScalingTraits == null ? null : Array.AsReadOnly(damageScalingTraits.ToArray()); }
    }

    public sealed class DiscardGenerationUpgrade
    {
        public string RequiredUpgradeId { get; }
        public CardUpgradeModifier Upgrade { get; }
        public DiscardGenerationUpgrade(string requiredUpgradeId, CardUpgradeModifier upgrade)
        { RequiredUpgradeId = requiredUpgradeId; Upgrade = upgrade; }
    }

    public sealed class CardGenerationRule
    {
        public string Destination { get; }
        public int Count { get; }
        public IReadOnlyList<CardCreationRule> Pool { get; }
        public bool SkipDuplicateInHand { get; }
        public bool RequireHandSpace { get; }
        public bool CopyModifiers { get; }
        public bool IgnoreTemporaryModifiers { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public IReadOnlyList<DiscardGenerationUpgrade> DiscardCopyUpgrades { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public CardGenerationRule(string destination, int count, IReadOnlyList<CardCreationRule> pool,
            bool skipDuplicateInHand = false, bool requireHandSpace = false, bool copyModifiers = false,
            bool ignoreTemporaryModifiers = false, CardUpgradeModifier? upgrade = null,
            IReadOnlyList<DiscardGenerationUpgrade>? discardCopyUpgrades = null, IReadOnlyList<string>? externalInteractions = null)
        { Destination = destination; Count = count; Pool = Array.AsReadOnly(pool.ToArray()); SkipDuplicateInHand = skipDuplicateInHand;
            RequireHandSpace = requireHandSpace; CopyModifiers = copyModifiers; IgnoreTemporaryModifiers = ignoreTemporaryModifiers; Upgrade = upgrade;
            DiscardCopyUpgrades = Array.AsReadOnly((discardCopyUpgrades ?? Array.Empty<DiscardGenerationUpgrade>()).ToArray());
            ExternalInteractions = Array.AsReadOnly((externalInteractions ?? Array.Empty<string>()).ToArray()); }
    }

    public sealed class CardGenerationResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<CardToken> AddedCards { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal CardGenerationResult(CombatContext? context, IReadOnlyList<CardToken> cards, string? error = null)
        { Context = context; AddedCards = Array.AsReadOnly(cards.ToArray()); UnsupportedReason = error; }
    }

    public static class CardGenerationModel
    {
        public static CardGenerationResult Apply(CombatContext source, CardGenerationRule rule, int sourceCardId = 0)
        {
            if (source.Cards.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", source.Cards.ExternalInteractions));
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (!new[] { "HandPile", "DiscardPile", "DeckPile", "DeckPileTop", "DeckPileRandom" }.Contains(rule.Destination))
                return Unsupported("Unmodeled generated card destination " + rule.Destination);
            CombatContext context = source;
            var added = new List<CardToken>();
            for (int index = 0; index < Math.Max(1, rule.Count); index++)
            {
                if (rule.Pool.Count == 0) continue; // Native pool filtering failed: no selection or allocation.
                RngDraw selected = context.BattleRng.Range(0, rule.Pool.Count);
                context = context.WithBattleRng(selected.State);
                CardCreationRule creation = rule.Pool[selected.Value];
                if (rule.Destination == "HandPile" && (context.Cards.Hand.Count >= context.MaxHandSize ||
                    rule.SkipDuplicateInHand && context.Cards.Hand.Any(card => card.DataId == creation.DataId))) continue;
                if (context.CardInstances == null && (HasModifiers(creation.StartingModifiers) || creation.EffectCounters?.Count > 0 || creation.DamageScalingTraits?.Count > 0 ||
                    rule.Upgrade != null || context.NextAddedTemporaryUpgrades?.Count > 0 || rule.CopyModifiers && context.FindCard(sourceCardId) != null))
                    return Unsupported("Modified generation requires complete card instance state.");
                CardModifiers permanent = creation.StartingModifiers, temporary = CardModifiers.Empty();
                CardInstanceState candidate = new(context.NextCardId, creation.DataId, permanent, temporary, 0, 0, 0,
                    creation.ExternalInteractions, creation.EffectCounters, creation.DamageScalingTraits);
                string? error = CardModifierModel.UnsupportedReason(candidate);
                if (error != null) return Unsupported(error);
                if (rule.Upgrade != null)
                {
                    if (rule.Upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.Upgrade.ExternalInteractions));
                    temporary = UnitModifierModel.Add(temporary, rule.Upgrade);
                }
                CardInstanceState? copying = rule.CopyModifiers ? context.FindCard(sourceCardId) : null;
                if (copying != null)
                {
                    error = CardModifierModel.UnsupportedReason(copying);
                    if (error != null) return Unsupported(error);
                    if (rule.Destination == "DiscardPile")
                        foreach (DiscardGenerationUpgrade conditional in rule.DiscardCopyUpgrades.Where(item => item.RequiredUpgradeId.Length == 0 ||
                            copying.Permanent.Upgrades.Concat(copying.Temporary.Upgrades).Any(upgrade => upgrade.DataId == item.RequiredUpgradeId)))
                        {
                            CardUpgradeModifier upgrade = conditional.Upgrade;
                            if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
                            temporary = UnitModifierModel.Add(temporary, upgrade);
                        }
                    // Same-card clones retain their own starting upgrades, rather than copying a second copy.
                    var ignored = new HashSet<string>(copying.DataId == creation.DataId ? permanent.Upgrades.Select(upgrade => upgrade.DataId) :
                        Array.Empty<string>(), StringComparer.Ordinal);
                    foreach (CardUpgradeModifier upgrade in copying.Permanent.Upgrades.Where(upgrade => upgrade.ExcludeFromClones)) ignored.Add(upgrade.DataId);
                    permanent = Copy(permanent, copying.Permanent, ignored);
                    if (!rule.IgnoreTemporaryModifiers)
                    {
                        foreach (CardUpgradeModifier upgrade in copying.Temporary.Upgrades.Where(upgrade => upgrade.ExcludeFromClones)) ignored.Add(upgrade.DataId);
                        temporary = Copy(temporary, copying.Temporary, ignored);
                    }
                }
                foreach (CardUpgradeModifier upgrade in context.NextAddedTemporaryUpgrades ?? Array.Empty<CardUpgradeModifier>())
                {
                    if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
                    temporary = UnitModifierModel.Add(temporary, upgrade);
                }
                var card = new CardToken(context.NextCardId, creation.DataId);
                var hand = context.Cards.Hand.ToList(); var draw = context.Cards.Draw.ToList(); var discard = context.Cards.Discard.ToList();
                UnityRng rng = context.BattleRng;
                switch (rule.Destination)
                {
                    case "HandPile": hand.Insert(0, card); break;
                    case "DiscardPile": discard.Insert(0, card); break;
                    case "DeckPileTop": draw.Add(card); break;
                    case "DeckPileRandom":
                        RngDraw position = rng.Range(0, draw.Count); rng = position.State;
                        draw.Insert(position.Value, card); break;
                    default: draw.Insert(0, card); break;
                }
                candidate = new CardInstanceState(card.InstanceId, card.DataId, permanent, temporary, 0, 0, 0,
                    creation.ExternalInteractions, creation.EffectCounters, creation.DamageScalingTraits);
                context = new CombatContext(new CardCycleState(hand, draw, discard, context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions), rng, context.Gold, checked(context.NextCardId + 1), context.MaxHandSize,
                    context.StatusRules, context.Statistics?.TrackCards(new[] { card.InstanceId }),
                    context.CardInstances?.Concat(new[] { candidate }).ToArray(), context.CardRegistry, context.AllScenarioBossesDead,
                    context.NextAddedTemporaryUpgrades == null ? null : Array.Empty<CardUpgradeModifier>(), context.OtherPiles);
                added.Add(card);
            }
            return new CardGenerationResult(context, added);
        }
        private static CardModifiers Copy(CardModifiers destination, CardModifiers source, HashSet<string> ignored)
        {
            CardModifiers result = new CardModifiers(source.Offsets, destination.Upgrades, source.PersistentHealth, source.ExternalInteractions);
            foreach (CardUpgradeModifier upgrade in source.Upgrades.Where(upgrade => !ignored.Contains(upgrade.DataId)))
                result = UnitModifierModel.Add(result, upgrade);
            return result;
        }
        private static bool HasModifiers(CardModifiers modifiers) => modifiers.Upgrades.Count > 0 || modifiers.PersistentHealth != 0 ||
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .Any(stat => modifiers.Offsets.Value(stat) != 0);
        private static CardGenerationResult Unsupported(string error) => new CardGenerationResult(null, Array.Empty<CardToken>(), error);
    }
}
