using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EquipmentMergeRule
    {
        public CardCreationRule Output { get; }
        public CardUpgradeMaskRule Filter { get; }
        public IReadOnlyList<IReadOnlyList<string>> DisallowedPairs { get; }
        public EquipmentMergeRule(CardCreationRule output, CardUpgradeMaskRule filter, IReadOnlyList<IReadOnlyList<string>> disallowedPairs)
        { Output = output; Filter = filter; DisallowedPairs = CardUpgradeMaskRule.CopyLists(disallowedPairs); }
    }
    public sealed class MergedEquipmentSource
    {
        public int CardId { get; }
        public CardPlayRule Definition { get; }
        public MergedEquipmentSource(int cardId, CardPlayRule definition) { CardId = cardId; Definition = definition; }
    }
    public sealed class MergedEquipmentState
    {
        public IReadOnlyList<MergedEquipmentSource> Sources { get; }
        public MergedEquipmentState(IReadOnlyList<MergedEquipmentSource> sources) { Sources = Array.AsReadOnly(sources.ToArray()); }
        internal string? Validate() => Sources.Count != 2 || Sources.Any(source => source.CardId <= 0 ||
            source.Definition.Equipment == null || source.Definition.Equipment.ReturnToHand ||
            source.Definition.Effects.Count != 1 || source.Definition.Effects[0].Type != "AttachEquipment" ||
            source.Definition.Effects[0].Target != "DropTargetCharacter" || !source.Definition.Effects[0].AllowPlayer || source.Definition.Effects[0].AllowEnemy ||
            source.Definition.ExternalInteractions.Count > 0 || source.Definition.OnCastEffects.Count > 0 ||
            source.Definition.OnUnplayedNegativeEffects.Count > 0 ||
            source.Definition.Equipment.Upgrades.Any(upgrade => upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null)) ||
            Sources.Select(source => source.CardId).Distinct().Count() != 2 ||
            Sources.Select(source => source.Definition.DataId).Distinct().Count() != 2
            ? "Uncalibrated merged equipment source payloads or identities." : null;
        internal CardPlayRule Resolve(CardPlayRule original)
        {
            bool dropSeen = false;
            var effects = new List<CardActionEffect>(original.Effects);
            foreach (CardActionEffect effect in Sources.SelectMany(source => source.Definition.Effects))
            {
                string target = effect.Target == "DropTargetCharacter" && dropSeen ? "LastTargetedCharacters" : effect.Target;
                if (effect.Target == "DropTargetCharacter") dropSeen = true;
                CardTargetFilters? f = effect.Filters;
                effects.Add(new CardActionEffect(effect.Type, target, effect.Value, effect.AllowEnemy, effect.AllowPlayer,
                    effect.Statuses, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range,
                    f == null ? null : new CardTargetFilters(f.Health, f.RequiredStatuses, f.ExcludedStatuses, true, f.Subtype, f.ExcludedSubtypes),
                    sourceStatusMultiplier: effect.SourceStatusMultiplier, suppressPyreRoomFocus: effect.SuppressPyreRoomFocus, spikeRecipe: effect.SpikeRecipe));
            }
            var equipment = new EquipmentDefinition(Sources.SelectMany(source => source.Definition.Equipment?.Upgrades ?? Array.Empty<CardUpgradeModifier>()).ToArray(),
                false, original.DataId);
            return new CardPlayRule(original.DataId, original.AssetKey, original.Cost, "Equipment", "Standby", null,
                original.ExternalInteractions.Concat(Sources.SelectMany(source => source.Definition.ExternalInteractions)).ToArray(), effects,
                original.UpgradeInteractions, original.HandDiscardInteractions, original.HandConsumeInteractions, original.CostType, equipment,
                cardType: original.CardType, isAnyAbility: original.IsAnyAbility, canPlayInEngineRoom: original.CanPlayInEngineRoom,
                onCastEffects: original.OnCastEffects, onUnplayedNegativeEffects: original.OnUnplayedNegativeEffects,
                unplayedNegativeOrder: original.UnplayedNegativeOrder, hasUnsupportedUnplayedTriggers: original.HasUnsupportedUnplayedTriggers,
                forgeEligible: original.ForgeEligible, hasKillTriggers: original.HasKillTriggers, onForgeEffects: original.OnForgeEffects);
        }
        internal CardMaskDescriptor Mask(CardMaskDescriptor descriptor)
        {
            CardMaskDefinition d = descriptor.Definition;
            var names = d.MainEffectNames.Concat(Sources.SelectMany(source => source.Definition.Effects).Select(_ => "CardEffectAttachEquipment")).ToArray();
            var statuses = d.MainEffectStatuses.Concat(Sources.SelectMany(source => source.Definition.Effects)
                .Select(effect => (IReadOnlyList<UpgradeMaskStatus>)effect.Statuses.Select(status => new UpgradeMaskStatus(status.Id, status.Stacks)).ToArray())).ToArray();
            var definition = new CardMaskDefinition(d.DataId, d.CardType, d.Rarity, d.IsSpawner, d.HasSpawnData, d.CanAttack,
                d.Subtypes, d.StartingStatuses, statuses, names, d.LinkedClan, d.XCost, d.BaseSize, 1,
                d.AuthoredUnitAbility, d.EquipmentUnitAbility, d.AuthoredGraft, d.CanScaleWithMagicPower, d.IsChampionCard,
                d.UnmodifiedSpawnAttack, d.HasStartingUpgrades);
            return new CardMaskDescriptor(definition, descriptor.BaseCost, descriptor.BaseTraits, descriptor.AuthoredCastEffects,
                descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, descriptor.SourceTraits, descriptor.KillTriggers);
        }
    }

    public static class EquipmentMergeModel
    {
        internal static EquipmentDefinition? Definition(CombatContext? context, BattlePlayRules? definitions, int cardId)
        {
            CardInstanceState? card = context?.FindCard(cardId);
            CardPlayRule? rule = definitions?.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
            return rule == null ? null : (card?.Permanent.MergedEquipment?.Resolve(rule) ?? rule).Equipment;
        }
        public static IReadOnlyList<int> Eligible(CombatContext context, EquipmentMergeRule rule, out string? error)
        {
            error = null; var ids = new List<int>();
            foreach (CardToken token in context.Cards.Hand.Concat(context.Cards.Discard).Concat(context.Cards.Draw))
            {
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card?.MaskDescriptor == null) { error = "Equipment merging requires captured branch masks."; return Array.Empty<int>(); }
                if (CardUpgradeMaskModel.FilterCard(rule.Filter, CardBranchMaskModel.Resolve(card))) ids.Add(token.InstanceId);
            }
            return ids.Distinct().ToArray();
        }
        public static IReadOnlyList<IReadOnlyList<int>> Choices(CombatContext context, EquipmentMergeRule rule, out string? error)
        {
            IReadOnlyList<int> eligible = Eligible(context, rule, out error);
            var choices = new List<IReadOnlyList<int>> { Array.Empty<int>() }; // Native UI can cancel.
            if (error != null) return choices;
            foreach (int first in eligible) foreach (int second in eligible)
            {
                string a = context.FindCard(first)!.DataId, b = context.FindCard(second)!.DataId;
                if (a == b || rule.DisallowedPairs.Any(pair => pair.Contains(a) && pair.Contains(b))) continue;
                choices.Add(new[] { first, second });
            }
            return choices;
        }
        public static CardGenerationResult Apply(CombatContext source, EquipmentMergeRule rule, IReadOnlyList<int>? selected,
            BattlePlayRules? definitions, CardUpgradeModifier? optionalUpgrade = null)
        {
            if (selected == null) return Unsupported("Equipment merge requires an explicit selection or cancellation.");
            var choices = Choices(source, rule, out string? error);
            if (error != null) return Unsupported(error);
            if (!choices.Any(choice => choice.SequenceEqual(selected))) return Unsupported("Invalid ordered equipment merge selection.");
            if (selected.Count == 0) return new CardGenerationResult(source, Array.Empty<CardToken>());
            if (source.OtherPiles?.SingleOrDefault(pile => pile.Name == "Standby") == null || source.CardInstances == null)
                return Unsupported("Equipment merge requires captured ownership and Standby.");
            if (source.NextAddedTemporaryUpgrades?.Count > 0 || rule.Output.StartingModifiers.Upgrades.Count > 0 ||
                source.Relics?.Any(relic => relic.CardModifiers?.Count > 0 || relic.CardAttackModifiers?.Count > 0 || relic.CardHealthModifiers?.Count > 0) == true)
                return Unsupported("Uncalibrated merge creation upgrades or relic ordering.");
            var sources = new List<MergedEquipmentSource>(); var upgrades = new List<CardUpgradeModifier>(); int cost = 0;
            foreach (int id in selected)
            {
                CardInstanceState card = source.FindCard(id)!;
                CardPlayRule? raw = definitions?.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (raw == null || raw.Equipment == null || raw.Effects.Count != 1 || raw.Effects[0].Type != "AttachEquipment" ||
                    raw.ExternalInteractions.Count > 0 || raw.OnCastEffects.Count > 0 || raw.OnUnplayedNegativeEffects.Count > 0 ||
                    raw.HandDiscardInteractions.Count > 0 || raw.HandConsumeInteractions.Count > 0 || raw.Equipment.ReturnToHand ||
                    raw.Equipment.Upgrades.Any(upgrade => upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null) ||
                    card.Permanent.MergedEquipment != null || card.MaskDescriptor!.BaseTraits.Count > 0 ||
                    card.MaskDescriptor.PermanentReplacements.Count > 0 || card.MaskDescriptor.TemporaryTraits.Count > 0 ||
                    card.MaskDescriptor.PermanentGraft || card.Permanent.PersistentHealth != 0 ||
                    new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                        .Any(stat => card.Permanent.Offsets.Value(stat) != 0) ||
                    card.Permanent.BoxedCards?.Count > 0 || card.Temporary.BoxedCards?.Count > 0 ||
                    card.Permanent.ExternalInteractions.Count > 0 || card.Temporary.ExternalInteractions.Count > 0 ||
                    card.Permanent.Upgrades.Any(upgrade => upgrade.Lifecycle?.Triggers.Count > 0 || upgrade.Lifecycle?.AddedTraits.Count > 0 ||
                        upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null || upgrade.ExternalInteractions.Count > 0 ||
                        upgrade.CopyRemoveOnDiscard.HasValue && upgrade.CopyRemoveOnDiscard != upgrade.RemoveOnDiscard ||
                        upgrade.CopyUnique.HasValue && upgrade.CopyUnique != upgrade.Unique ||
                        upgrade.CopyExcludeFromClones.HasValue && upgrade.CopyExcludeFromClones != upgrade.ExcludeFromClones ||
                        upgrade.SharedReferenceId.HasValue))
                    return Unsupported("Uncalibrated merged source traits, payloads or nested equipment.");
                cost = unchecked(cost + CardModifierModel.Resolve(raw, card).Cost);
                upgrades.AddRange(card.Permanent.Upgrades);
                sources.Add(new MergedEquipmentSource(id, raw.WithForgeEligibility(null)));
            }
            var merged = new MergedEquipmentState(sources);
            if (merged.Validate() is string mergedError) return Unsupported(mergedError);
            CardModifiers modifiers = new CardModifiers(new CardStatModifier(cost: cost), upgrades, 0, Array.Empty<string>(), mergedEquipment: merged);
            CardCreationRule output = rule.Output;
            if (output.MaskDescriptor == null) return Unsupported("Missing merged output mask.");
            var creation = new CardCreationRule(output.DataId, modifiers, output.EffectCounters, output.ExternalInteractions,
                output.DamageScalingTraits, output.StatusScalingTraits, output.UnitUpgradeScalingTraits, output.CapacityScalingTraits,
                output.EquippedUnitId, merged.Mask(output.MaskDescriptor), output.ForgeCountScalingTraits, output.CanBeCopied, output.TrainHealScalingTraits);
            CardGenerationResult generated = CardGenerationModel.CreateDetached(source, creation);
            if (!generated.Supported) return generated;
            CombatContext context = generated.Context!; CardToken tokenCreated = generated.AddedCards.Single();
            CardInstanceState created = context.FindCard(tokenCreated.InstanceId)!;
            if (optionalUpgrade != null) created = created.WithModifiers(created.Permanent, UnitModifierModel.Add(created.Temporary, optionalUpgrade));
            bool draw = source.Cards.Hand.Any(card => selected.Contains(card.InstanceId));
            context = context.WithCardInstances(source.CardInstances.Concat(new[] { created }).ToArray()).WithCard(created);
            context = context.WithCards(new CardCycleState(source.Cards.Hand.Where(card => !selected.Contains(card.InstanceId)).ToArray(),
                source.Cards.Draw.Where(card => !selected.Contains(card.InstanceId)).Concat(new[] { tokenCreated }).ToArray(),
                source.Cards.Discard.Where(card => !selected.Contains(card.InstanceId)).ToArray(), source.Cards.Rng,
                source.Cards.DrawModifier, source.Cards.ExternalInteractions, source.Cards.BonusDraw, source.Cards.NextDrawUpgrades, source.Cards.NextUpgradeReferenceId, source.Cards.DeckDrawDisabledThisTurn));
            context = context.WithOtherPiles(source.OtherPiles.Select(pile => pile.Name == "Standby"
                ? selected.Aggregate(pile, (current, id) => CardPileModel.Add(current, new CardToken(id, source.FindCard(id)!.DataId))) : pile).ToArray());
            if (draw)
            {
                TypedDrawResult drawn = NextDrawUpgradeModel.DrawSpecific(context, tokenCreated.InstanceId, 0, definitions);
                if (!drawn.Supported) return Unsupported(drawn.UnsupportedReason!);
                context = drawn.Context!;
            }
            return new CardGenerationResult(context, new[] { tokenCreated });
        }
        private static CardGenerationResult Unsupported(string reason) => new CardGenerationResult(null, Array.Empty<CardToken>(), reason);
    }
}
