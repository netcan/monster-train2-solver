using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Card OnKill runs inside CheckDeathCardTriggers, after a possible Undying
    // revival and before final death/removal. It is separate from character Slay.
    public static class CardKillTriggerModel
    {
        public const string SuppressTrait = "CardTraitModifyCardTriggerSlayFireCount";
        public static bool Supported(CardLifecycleTrigger trigger)
        {
            if (trigger.Type != "OnKill" || trigger.ExternalInteractions.Count != 0 || trigger.Actions.Count != 1 ||
                trigger.Actions[0].Range != null) return false;
            CardActionEffect effect = trigger.Actions[0];
            CardGenerationRule? generation = effect.Generation;
            if (trigger.Effects.SequenceEqual(new[] { "CardTriggerEffectBuffSpellDamage" }))
                return effect.Type == "BuffSourceSpellDamage" && effect.Target == "Self" && effect.Value > 0 &&
                    effect.Lifetime == "SingleRun" && !effect.AllowEnemy && !effect.AllowPlayer;
            // These effects ignore the character target list, including its team flags.
            if (effect.Target == "Room" && effect.Value > 0 &&
                (trigger.Effects.SequenceEqual(new[] { "CardEffectRewardGold" }) && effect.Type == "RewardGold" && effect.GoldCostRequired.HasValue ||
                 trigger.Effects.SequenceEqual(new[] { "CardEffectAdjustDragonsHoard" }) && effect.Type == "AdjustDragonsHoard" ||
                 trigger.Effects.SequenceEqual(new[] { "CardEffectAdjustForgePoints" }) && effect.Type == ForgePointsModel.Effect)) return true;
            if (trigger.Effects.SequenceEqual(new[] { "CardEffectAdjustCorruption" }))
                return effect.Type == "AdjustCorruption" && effect.Target == "Room" && effect.Value > 0 && effect.CorruptionAdd == true;
            if (trigger.Effects.SequenceEqual(new[] { "CardEffectSpawnMonsterOrAddStatusEffect" }))
                return effect.Type == "SpawnOrStatus" && effect.Target == "Room" && effect.SpawnFallbackUnit != null &&
                    effect.Statuses.Count == 1 && effect.Statuses[0].Id == "horde" && effect.Statuses[0].Stacks > 0 &&
                    effect.Filters?.Subtype == "SubtypesData_Horde";
            if (trigger.Effects.SequenceEqual(new[] { "CardEffectAddUpgradedCopy" }))
                // This native effect ignores the character target list and copies playedCard.
                return effect.Type == "GenerateUpgradedCopy" && effect.Target == "LastTargetedCharacters" && generation != null &&
                    generation.Destination == "HandPile" && generation.Count == 1 && generation.Pool.Count == 0 &&
                    generation.DeterministicPoolSelection && generation.RequireCopyableSource && generation.Permanent != true &&
                    !generation.UpgradeIsPermanent && generation.ExternalInteractions.Count == 0 &&
                    generation.Upgrade != null && generation.Upgrade.ExternalInteractions.Count == 0 && generation.OnlyUnequippedTargets == null;
            if (effect.AllowEnemy || effect.AllowPlayer) return false;
            if (trigger.Effects.SequenceEqual(new[] { "CardEffectGainEnergy" }))
                return effect.Type == "GainEnergy" && effect.Target is "None" or "Room" && effect.Value > 0;
            return trigger.Effects.SequenceEqual(new[] { "CardEffectAddBattleCard" }) && effect.Type == "Generate" && effect.Target is "Hand" or "Room" &&
                generation != null && generation.Destination == "HandPile" && generation.Count > 0 && generation.Pool.Count > 0 &&
                generation.ExternalInteractions.Count == 0 && generation.Permanent != true && generation.Upgrade == null &&
                !generation.CopyModifiers && !generation.RequireCopyableSource && generation.OnlyUnequippedTargets == null;
        }

        internal static string? Validate(CardInstanceState card)
        {
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            var modifiers = descriptor?.SourceTraits?.Where(trait => trait.Type == SuppressTrait).ToArray() ?? Array.Empty<CardSourceTrait>();
            int baseCount = descriptor?.BaseTraits.Count(trait => trait.RuntimeType == SuppressTrait) ?? 0;
            if (baseCount > 0 || modifiers.Length > 0 || descriptor?.TemporaryTraits.Any(trait => trait.RuntimeType == SuppressTrait) == true)
            {
                if (modifiers.Length != 1 || baseCount != 1 || descriptor!.TemporaryTraits.Any(trait => trait.RuntimeType == SuppressTrait) ||
                    descriptor.PermanentReplacements.Count != 0 || !modifiers[0].Bool || modifiers[0].KillTargetSubtype is not ("" or "SubtypesData_FunGuy") ||
                    modifiers[0].KillTargetTeam is not ("Heroes" or "Monsters" or "Heroes, Monsters") ||
                    descriptor.BaseTraits.Any(trait => trait.RuntimeType == SuppressTrait && trait.ParamInt != modifiers[0].Int))
                    return "Card OnKill suppression requires original team/subtype metadata; added traits and bonus fire counts are uncalibrated.";
            }
            var triggers = card.MaskDescriptor?.KillTriggers;
            if (triggers == null || triggers.Count == 0) return null;
            if (card.MaskDescriptor!.Definition.CardType != "Spell" || triggers.Any(trigger => !Supported(trigger)))
                return "Uncalibrated card OnKill payload; only fixed spell resources, source damage growth, room corruption, hand-pool generation and temporary hand self-copies are supported.";
            if (triggers.Any(trigger => trigger.Actions[0].Type == "GenerateUpgradedCopy") &&
                (descriptor!.Definition.HasStartingUpgrades != false || descriptor.Purified || descriptor.PermanentGraft || descriptor.AuthoredCastEffects.Count != 0 ||
                 descriptor.PermanentReplacements.Count != 0 || card.EffectCounters?.Count > 0 ||
                 descriptor.SourceTraits == null || descriptor.BaseTraits.Count != descriptor.SourceTraits.Count ||
                 descriptor.BaseTraits.Where((trait, index) => trait.RuntimeType != descriptor.SourceTraits[index].Type).Any()))
                return "Card OnKill self-copy requires unchanged original base traits, no starting upgrades and a simple spell creation definition.";
            return null;
        }

        internal static CombatContext Apply(CombatContext context, CardInstanceState card, bool preview, bool deployment,
            int attackerUnitId, CombatUnit dying, System.Collections.Generic.IReadOnlyList<CombatUnit> activeUnits,
            Func<CombatContext, RelicPyreAttackRefreshResult> refreshGold,
            Func<CombatContext, CardActionEffect, (CombatContext? Context, string? Error)> applyQueuedEffect, out string? error)
        {
            error = Validate(card);
            if (error != null) return context;
            var triggers = card.MaskDescriptor?.KillTriggers;
            if (triggers == null || triggers.Count == 0) return context;
            if (attackerUnitId > 0)
            { error = "Character-attributed card OnKill requires captured trigger fire-count rules."; return context; }
            if (preview || context.AllScenarioBossesDead == true) return context;
            CardSourceTrait? suppression = card.MaskDescriptor?.SourceTraits?.FirstOrDefault(trait => trait.Type == SuppressTrait);
            if (suppression != null)
            {
                if (context.Relics?.Any(relic => relic.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes")) == true)
                { error = "Card OnKill subtype suppression with all-subtype relic rules requires native calibration."; return context; }
                bool team = dying.Team == CombatTeam.Player ? suppression.KillTargetTeam!.Contains("Monsters") : suppression.KillTargetTeam!.Contains("Heroes");
                bool subtype = suppression.KillTargetSubtype == "" ? dying.Team != CombatTeam.Player || !dying.IsPyre : dying.Subtypes.Contains(suppression.KillTargetSubtype!);
                if (team && subtype) return context;
            }
            foreach (var trigger in triggers)
            {
                if (deployment && !trigger.DeploymentAllowed.HasValue)
                { error = "Deployment card OnKill requires captured disallowed-trigger rules."; return context; }
                if (deployment && trigger.DeploymentAllowed == false) continue;
                CardActionEffect effect = trigger.Actions[0];
                if (effect.Type is "RewardGold" or "AdjustDragonsHoard" or ForgePointsModel.Effect)
                {
                    if (context.EnergyState?.PyreAlive == null) { error = "Card OnKill resources require captured Pyre liveness."; return context; }
                    if (context.EnergyState.PyreAlive == false) continue;
                }
                if (effect.Type is "SpawnOrStatus" or "AdjustCorruption")
                {
                    var applied = applyQueuedEffect(context, effect);
                    if (applied.Context == null) { error = applied.Error ?? "Card OnKill queued effect failed."; return context; }
                    context = applied.Context;
                }
                else if (effect.Type == "BuffSourceSpellDamage")
                {
                    CardInstanceState current = context.FindCard(card.InstanceId) ?? card;
                    context = context.WithCard(current.WithModifiers(current.Permanent.WithAdditionalDamage(effect.Value), current.Temporary));
                }
                else if (effect.Type == "GainEnergy")
                {
                    error = EnergyModel.Validate(context);
                    if (error != null) return context;
                    context = EnergyModel.Apply(context, "GainEnergy", effect.Value);
                }
                else if (effect.Type == "RewardGold")
                {
                    if (context.Resources == null) { error = "Card OnKill gold requires captured gold reward rules."; return context; }
                    int reward = GoldRewardModel.Adjust(effect.Value, multiplier: context.Resources.GoldRewardMultiplier);
                    if (reward != 0)
                    {
                        var refreshed = refreshGold(context.WithGold(Math.Max(0, unchecked(context.Gold + reward))));
                        if (!refreshed.Supported) { error = refreshed.UnsupportedReason; return context; }
                        context = refreshed.Context!;
                    }
                }
                else if (effect.Type == "AdjustDragonsHoard")
                {
                    if (context.QueryFrame?.DragonsHoard is not int current || context.Resources?.DragonsHoardCap is not int cap || cap < 0)
                    { error = "Card OnKill Dragon's Hoard requires captured current amount and cap."; return context; }
                    error = CardSpellModel.ValidateHoardRefresh(activeUnits, context, new[] { effect });
                    if (error != null) return context;
                    context = context.WithQueryFrame(context.QueryFrame.With(dragonsHoard: Math.Clamp(unchecked(current + effect.Value), 0, cap)));
                }
                else if (effect.Type == ForgePointsModel.Effect)
                {
                    ForgePointsResult changed = ForgePointsModel.Apply(context, effect.Value, card.InstanceId, deployment, preview);
                    if (!changed.Supported) { error = changed.UnsupportedReason; return context; }
                    context = changed.Context!;
                }
                else if (effect.Type == "GenerateUpgradedCopy")
                {
                    CardGenerationRule rule = effect.Generation!;
                    CardMaskDescriptor d = card.MaskDescriptor!;
                    var original = new CardMaskDescriptor(d.Definition, d.BaseCost, d.BaseTraits, d.AuthoredCastEffects,
                        sourceTraits: d.SourceTraits, killTriggers: d.KillTriggers);
                    var creation = new CardCreationRule(card.DataId, CardModifiers.Empty(), null, Array.Empty<string>(),
                        card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits, card.CapacityScalingTraits,
                        card.EquippedUnitId, original, card.ForgeCountScalingTraits, card.CanBeCopied, card.TrainHealScalingTraits);
                    var copy = new CardGenerationRule(rule.Destination, 1, new[] { creation }, copyModifiers: rule.CopyModifiers,
                        ignoreTemporaryModifiers: rule.IgnoreTemporaryModifiers, upgrade: rule.Upgrade,
                        deterministicPoolSelection: true, requireCopyableSource: true);
                    CardGenerationResult generated = CardGenerationModel.Apply(context, copy, card.InstanceId);
                    if (!generated.Supported) { error = generated.UnsupportedReason; return context; }
                    context = generated.Context!;
                }
                else if (effect.Generation!.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize)
                {
                    CardGenerationResult generated = CardGenerationModel.Apply(context, effect.Generation, card.InstanceId);
                    if (!generated.Supported) { error = generated.UnsupportedReason; return context; }
                    context = generated.Context!;
                }
                context = context.AfterCardEffects();
            }
            return context;
        }
    }
}
