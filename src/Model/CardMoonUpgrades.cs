using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardMoonUpgradeState
    {
        public bool Active { get; }
        public bool Cached { get; }
        public CardUpgradeModifier Upgrade { get; }
        public CardMoonUpgradeState(bool active, bool cached, CardUpgradeModifier upgrade)
        { Active = active; Cached = cached; Upgrade = upgrade; }
    }
    public sealed class CardMoonUpgradeResult
    {
        public CardInstanceState? Card { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Card != null;
        internal CardMoonUpgradeResult(CardInstanceState? card, string? reason = null) { Card = card; UnsupportedReason = reason; }
    }
    public static class CardMoonUpgradeModel
    {
        public const string Trait = "CardTraitAddCardUpgradeOnMoonPhase";
        internal static bool Present(CardInstanceState card) => card.MaskDescriptor?.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits)
            .Any(trait => trait.RuntimeType == Trait) == true || card.MaskDescriptor?.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) == true ||
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true);
        internal static string? Validate(CardInstanceState card) => Present(card) ? Update(card, 1, preview: true).UnsupportedReason : null;
        internal static CombatContext RefreshHand(CombatContext source, out string? error, IReadOnlyList<int>? drawn = null)
        {
            error = null; CombatContext context = source;
            foreach (CardToken token in source.Cards.Hand.Where(token => drawn == null || drawn.Contains(token.InstanceId)))
            {
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card == null || !Present(card)) continue;
                // Refreshing traits does not dispatch phase-change callbacks.
                // Existing healing-power offsets stay fixed; phase healing is
                // dispatched separately by the ordered global moon pipeline.
                if (context.QueryFrame?.MoonPhase is not (1 or 2) || context.QueryFrame.SelectedRoomIndex == null ||
                    context.Relics?.Any(relic => !CompatibleRelic(relic)) == true)
                { error = "Moon spell upgrades require captured phase/selection; active relic combinations need separate witnesses."; return source; }
                if (RelicModel.Validate(context.Relics) is string relicError)
                { error = relicError; return source; }
                foreach (RoomMoonPhaseRule rule in context.RoomCapacities?.SelectMany(room => room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                    .Select(attachment => attachment.MoonPhase).OfType<RoomMoonPhaseRule>() ?? Enumerable.Empty<RoomMoonPhaseRule>())
                    if (RoomMoonPhaseModel.Validate(rule) is string reason) { error = reason; return source; }
                int roomMask = context.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == context.QueryFrame.SelectedRoomIndex)?.Attachments?
                    .Aggregate(0, (mask, attachment) => mask | (attachment.MoonPhase?.AdditionalMask ?? 0)) ?? 0;
                CardMoonUpgradeResult result = Update(card, context.QueryFrame.MoonPhase.Value, preview: context.IsolatedBattlePreview == true && drawn == null,
                    roomMask: roomMask);
                if (!result.Supported) { error = result.UnsupportedReason; return source; }
                context = context.WithCard(result.Card!);
                // Managing a moon upgrade reapplies magic-power traits with the
                // native managers, dispatching CardUpdated to selected-room
                // modifiers. Conduit must rebuild its tracked upgrade as well.
                if (!ReferenceEquals(card, result.Card)) context = ConduitModel.Updated(context, token.InstanceId);
            }
            return context;
        }
        private static bool CompatibleRelic(CombatRelicState relic) =>
            relic.EffectTypes.All(type => type is RelicModel.ModifyCardTypeCost or RelicModel.ModifyCardAttack) &&
                (relic.CardAttackModifiers == null || relic.CardAttackModifiers.All(rule =>
                    rule.CardType == "Monster" && rule.CharacterName == "" &&
                    rule.CharacterSubtypeKey == "" && rule.CardFilter == null)) ||
            relic.DataId == "03613302-e55a-4e6c-99a1-c198c8d3175c" && relic.AssetKey == "IncreaseSpellHealing" &&
                relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
                relic.EffectTypes.SequenceEqual(new[] { RelicModel.ModifyCardHealingPower }) &&
                relic.MagicPowerModifiers?.Count == 1 && relic.MagicPowerModifiers[0].EffectIndex == 0 &&
                relic.MagicPowerModifiers[0].PowerModifier == 10 && relic.MagicPowerModifiers[0].ConditionCount == 0 ||
            CompatiblePhaseHealRelic(relic) || CompatiblePhaseEnergyRelic(relic) ||
            RelicMoonPhaseHandUpgradeModel.CompatibleMoonSpells(relic) ||
            relic.DataId == RelicMoonPhaseStatusModel.DataId && relic.EffectTypes.SequenceEqual(new[] { RelicMoonPhaseStatusModel.Effect }) &&
                RelicMoonPhaseStatusModel.Validate(relic) == null;

        private static bool CompatiblePhaseEnergyRelic(CombatRelicState relic)
        {
            if (relic.DataId != "70096a91-220a-4ab3-b8d0-b8fe8f3a819c" || relic.AssetKey != "MoonPhaseModifyEmber" ||
                relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false ||
                !relic.EffectTypes.SequenceEqual(new[] { RelicResourceModel.MoonPhaseEnergy }) || relic.ResourceEffects?.Count != 1)
                return false;
            RelicResourceEffect effect = relic.ResourceEffects[0];
            if (effect.EffectIndex != 0 || effect.Type != RelicResourceModel.MoonPhaseEnergy || effect.ParamInt != 1 ||
                effect.ParamInt2 != 2 || effect.Amount != 1 || effect.CanApplyInPreviewMode ||
                effect.RequiredMutatorId != null || effect.Conditions.Count != 1) return false;
            RelicConditionState condition = effect.Conditions[0];
            return condition.Query.Type == "MoonPhase" && condition.Query.Duration == "ThisTurn" &&
                condition.Query.TypeMatchDataIds == null && condition.Query.SubtypeMatchDataIds == null &&
                condition.Query.SubtypeIsNone && condition.Query.Subtype == null && condition.Query.SpecificCardDataId == null &&
                condition.Query.SourceRawCost == null && !condition.Query.VariableCost && condition.Query.TargetTeam == "None" &&
                !condition.Query.CurrentCostCardModifiersOnly && condition.TrackTriggerCount && condition.Comparator == 6 &&
                condition.Value == 1 && !condition.AllowMultiple;
        }

        private static bool CompatiblePhaseHealRelic(CombatRelicState relic) =>
            relic.DataId == "6d977136-5aac-44fd-9843-54cf2a0bd01d" && relic.AssetKey == "MooncycleRestoreFriendlyHealth" &&
            relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
            relic.EffectTypes.SequenceEqual(new[] { RelicMoonPhaseHealModel.Effect }) &&
            relic.MoonPhaseHealRules?.Count == 1 && relic.MoonPhaseHealRules[0].EffectIndex == 0 &&
            relic.MoonPhaseHealRules[0].TargetPlayer && !relic.MoonPhaseHealRules[0].TargetEnemy &&
            relic.MoonPhaseHealRules[0].TargetMode == "WeakestAllRooms" && relic.MoonPhaseHealRules[0].RequiredSubtype == null &&
            relic.MoonPhaseHealRules[0].HealthAmount == 10 && relic.MoonPhaseHealRules[0].MoonPhaseMask == -1 &&
            relic.MoonPhaseHealRules[0].ConditionCount == 0;
        // This boundary covers spell-owned upgrades; living unit callbacks are separate.
        public static CardMoonUpgradeResult Update(CardInstanceState source, int phase, bool reset = false, bool preview = false, int roomMask = 0)
        {
            CardMaskDescriptor? descriptor = source.MaskDescriptor;
            if (!Present(source))
                return new CardMoonUpgradeResult(source);
            CardMoonUpgradeResult Reject(string reason) => new CardMoonUpgradeResult(null, reason);
            if (descriptor == null || descriptor.Definition.CardType != "Spell" || descriptor.SourceTraits == null || phase is not (1 or 2) || roomMask is not (0 or 1) ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) || descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                source.Permanent.Upgrades.Concat(source.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Moon card upgrades require unchanged spell base traits and captured source parameters.");
            CardSourceTrait[] definitions = descriptor.SourceTraits.Where(trait => trait.Type == Trait).ToArray();
            int[] indices = descriptor.BaseTraits.Select((trait, index) => (trait, index)).Where(item => item.trait.RuntimeType == Trait).Select(item => item.index).ToArray();
            string[] expected = source.DataId switch
            {
                "108b9153-0236-4ba2-ad20-d03a991bbd57" => new[] { "328c86e6-19c4-4bf8-a717-4bae655edf2b" },
                "753f20a2-c625-47b3-be4b-8600b673841e" => new[] { "12793a9c-fae4-42ab-80bb-c3274e681c19" },
                "8757357b-c36f-476d-88de-b9a481d89fcb" => new[] { "9492665d-c631-41a8-9b87-5acd0b80bd91", "de575a7e-a3cf-45de-a67b-949ac05fcee6" },
                "87e45427-921e-45d3-b4c4-9c156a83cedf" => new[] { "6b7b5abc-4397-4e6e-9c25-3db5a7de0dd2", "3bbf752b-475b-4975-b3c7-b970f44a20b6" },
                "cbf4d65d-afdb-4fd8-a7ce-216d58a6b486" => new[] { "0c025567-26a8-4dae-8dca-5d49cef0e291" },
                _ => Array.Empty<string>()
            };
            if (indices.Length != definitions.Length || definitions.Length != expected.Length)
                return Reject("Original moon upgrade source/runtime trait count or identity differs.");
            CardInstanceState card = source;
            for (int position = 0; position < indices.Length; position++)
            {
                int index = indices[position]; descriptor = card.MaskDescriptor!;
                CardTraitValue trait = descriptor.BaseTraits[index]; CardSourceTrait definition = definitions[position];
                CardMoonUpgradeState? state = trait.MoonPhaseUpgradeState;
                if (state == null || definition.MoonPhaseUpgrade == null || trait.ParamInt != definition.Int || trait.DefinitionParamInt != definition.Int ||
                    trait.DeclaredName != Trait || trait.StackMode != 0 || definition.Int is not (1 or 2) || definition.Int2 is not (0 or 1) ||
                    definition.Int3 != 0 || definition.Str.Length != 0 || definition.Float != 0 || definition.Scaling || definition.TrackedValue != "MoonPhase" || definition.EntryDuration != "ThisTurn" ||
                    definition.MoonPhaseUpgrade.DataId != expected[position] || definition.Int != (expected.Length == 2 ? position + 1 : 2) ||
                    definition.Int2 != (source.DataId == "87e45427-921e-45d3-b4c4-9c156a83cedf" ? 1 : 0) ||
                    definition.Bool != (definition.MoonPhaseUpgrade.DataId != "9492665d-c631-41a8-9b87-5acd0b80bd91") ||
                    !KnownPayload(definition.MoonPhaseUpgrade) || !KnownPayload(state.Upgrade) || state.Upgrade.DataId != definition.MoonPhaseUpgrade.DataId)
                    return Reject("Missing or uncalibrated original moon upgrade payload/private state.");
                if (preview && !reset) continue;
                // Native Exclusive uses a nonzero room override in place of
                // global phase; Additive accepts either. The original Gravity
                // traits are Exclusive, even when Eclipse exposes both phases
                // to ordinary character callbacks on the same floor.
                int effective = definition.Int2 == 1 && roomMask != 0 ? roomMask : phase | roomMask;
                bool active = !reset && (definition.Int & effective) != 0;
                if (!reset && state.Active == active) continue;
                CardUpgradeModifier upgrade = state.Upgrade;
                var bases = descriptor.BaseTraits.ToList(); var permanent = card.Permanent.Upgrades.ToList();
                var castEffects = descriptor.AuthoredCastEffects.ToList();
                var modified = new List<string>();
                if (active)
                {
                    if (permanent.Any(item => item.DataId == upgrade.DataId)) return Reject("Duplicate moon upgrades require reference-identity calibration.");
                    foreach (CardTraitValue added in upgrade.Lifecycle!.AddedTraits)
                        if (!CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(item => item.RuntimeType == added.RuntimeType))
                        { bases.Add(added); modified.Add(added.RuntimeType); }
                    CardLifecycleUpgrade lifecycle = WithModified(upgrade.Lifecycle, modified);
                    upgrade = upgrade.WithLifecycle(lifecycle); permanent.Add(upgrade);
                    castEffects.AddRange(lifecycle.Triggers.Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects));
                }
                else
                {
                    foreach (string removed in upgrade.Lifecycle!.TraitsModified)
                        bases.RemoveAll(item => item.RuntimeType == removed && item.Removable);
                    int removedIndex = permanent.FindIndex(item => item.DataId == upgrade.DataId);
                    if (removedIndex >= 0)
                    {
                        permanent.RemoveAt(removedIndex);
                        foreach (string effect in upgrade.Lifecycle.Triggers.Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects))
                            castEffects.Remove(effect);
                    }
                    upgrade = upgrade.WithLifecycle(upgrade.Lifecycle.ClearModified());
                }
                bases[index] = trait.WithMoonUpgrade(new CardMoonUpgradeState(active, true, upgrade));
                card = card.WithMaskDescriptor(descriptor.WithBaseTraits(bases).WithAuthoredCastEffects(castEffects));
                card = card.WithModifiers(new CardModifiers(card.Permanent.Offsets, permanent, card.Permanent.PersistentHealth,
                    card.Permanent.ExternalInteractions, card.Permanent.BoxedCards, card.Permanent.MergedEquipment), card.Temporary, refreshTraits: false);
                card = CardMagicPowerModel.Refresh(card, CardMagicPowerModel.Multiplier(CardExhaustModel.RefreshTraits(card).CombinedTraits!));
            }
            return new CardMoonUpgradeResult(card);
        }
        private static CardLifecycleUpgrade WithModified(CardLifecycleUpgrade source, IReadOnlyList<string> modified) => new CardLifecycleUpgrade(
            source.Values, source.AssetKey, source.Unique, source.RemoveOnDiscard, source.AddedTraits, source.RemovedRuntimeTypes,
            source.AvoidClobbering, modified, source.ReplacedAssets, source.Triggers, source.OriginalDamage, source.OriginalHeal, source.InstanceId);
        private static bool KnownPayload(CardUpgradeModifier upgrade)
        {
            CardLifecycleUpgrade? life = upgrade.Lifecycle;
            if (life == null || !life.AvoidClobbering || life.RemovedRuntimeTypes.Count != 0 || life.ReplacedAssets.Count != 0 ||
                upgrade.ExternalInteractions.Count != 0 || upgrade.RemoveOnDiscard || upgrade.Unique || upgrade.ExcludeFromClones || upgrade.Statuses.Count != 0 ||
                upgrade.Stats.Damage != 0 || upgrade.Stats.Heal != 0 || upgrade.Stats.Cost != 0 || upgrade.Stats.Health != 0 || upgrade.Stats.Size != 0 ||
                upgrade.Stats.XCost != 0 || upgrade.Stats.EquipmentLimit != 0 || upgrade.Stats.UpgradeSlotCount != 0) return false;
            if (upgrade.DataId == "328c86e6-19c4-4bf8-a717-4bae655edf2b")
                return life.Triggers.Count == 0 && life.AddedTraits.Count == 1 && life.AddedTraits[0].RuntimeType == "CardTraitStrongerMagicPower" && life.AddedTraits[0].StackMode == 0;
            if (life.AddedTraits.Count != 0 || life.Triggers.Count != 1 || life.Triggers[0].Type != "OnCast" || life.Triggers[0].ExternalInteractions.Count != 0 || life.Triggers[0].Actions.Count != 1) return false;
            CardActionEffect effect = life.Triggers[0].Actions[0];
            return upgrade.DataId switch
            {
                "12793a9c-fae4-42ab-80bb-c3274e681c19" => effect.Type == "AddStatus" && effect.Target == "DropTargetCharacter" && effect.Statuses.Count == 1 && effect.Statuses[0].Id == "dazed" && effect.Statuses[0].Stacks == 1,
                "9492665d-c631-41a8-9b87-5acd0b80bd91" => effect.Type == "Damage" && effect.Target == "Room" && effect.Value == 80,
                "de575a7e-a3cf-45de-a67b-949ac05fcee6" => effect.Type == "Damage" && effect.Target == "Tower" && effect.Value == 80,
                "6b7b5abc-4397-4e6e-9c25-3db5a7de0dd2" => effect.Type == "Bump" && effect.Target == "LastTargetedCharacters" && effect.Value == -1,
                "3bbf752b-475b-4975-b3c7-b970f44a20b6" => effect.Type == "Bump" && effect.Target == "LastTargetedCharacters" && effect.Value == 1,
                "0c025567-26a8-4dae-8dca-5d49cef0e291" => effect.Type == "Damage" && effect.Target == "FrontInRoom" && effect.Value == 25,
                _ => false
            };
        }
    }
}
