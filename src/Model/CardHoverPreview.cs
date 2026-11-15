using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardHoverPreviewResult
    {
        public bool Supported => Restored != null;
        public string? UnsupportedReason { get; }
        public TrainCombatState? CopiedBefore { get; }
        public TrainCombatState? AfterEffects { get; }
        public TrainCombatState? Restored { get; }
        public IReadOnlyList<int> CopiedIds { get; }
        public IReadOnlyList<CombatUnit> TemporaryRemovedUnits { get; }
        internal CardHoverPreviewResult(string error)
        { UnsupportedReason = error; CopiedIds = Array.Empty<int>(); TemporaryRemovedUnits = Array.Empty<CombatUnit>(); }
        internal CardHoverPreviewResult(TrainCombatState copiedBefore, TrainCombatState afterEffects,
            TrainCombatState restored, IReadOnlyList<int> copiedIds, IReadOnlyList<CombatUnit> removedUnits)
        {
            CopiedBefore = copiedBefore; AfterEffects = afterEffects; Restored = restored;
            CopiedIds = Array.AsReadOnly(copiedIds.ToArray());
            TemporaryRemovedUnits = Array.AsReadOnly(removedUnits.ToArray());
        }
    }

    // CombatManager.PreviewCardPlay has its own protocol. It does not pay for a
    // card, move piles, increment played-card statistics, or reset the TFB counter.
    // Calibrated maximum-health, damage and healing spells use temporary room copies,
    // including lethal ordinary units and nonlethal linked maximum-health changes. Battle previews keep
    // their separate protocol and validation.
    public static class CardHoverPreviewModel
    {
        public static CardHoverPreviewResult Apply(TrainCombatState source, CardPlayRule rule, PlayCardAction action)
        {
            CombatContext? primary = source.Context;
            if (primary?.QueryFrame?.SelectedRoomIndex != action.RoomIndex)
                return new CardHoverPreviewResult("Card hover requires matching selected-room focus; native OnCast can modify uncopied primary actors.");
            LifeLinkContext? links = primary.LifeLinks;
            if (links != null && (links.TemporaryPreviewIds != null || links.CalculatingPreview ||
                    links.Actors.Any(actor => !actor.HasPreviewHealthCopy.HasValue)) ||
                source.Rooms.Any(room => room.Preview) || primary.Enchantments is not EnchantmentWorld world ||
                world.Preview || world.TemporaryPreviewUnitIds != null || primary.SpawnPoints?.NextPreviewCopyId == null)
                return new CardHoverPreviewResult("Card hover requires captured primary life-link, health-cache, world and physical-copy state.");
            if (source.Rooms.SelectMany(room => room.Units).Any(RoomHealthAttackModel.HasSource))
                return new CardHoverPreviewResult("Friendly missing-health room modifiers require native card-hover lifecycle calibration.");
            if (rule.Effect != "Spell" || rule.CardType != "Spell" || rule.IsAnyAbility != false ||
                rule.ExternalInteractions.Count != 0 || rule.Equipment != null || rule.Ability != null ||
                rule.SpawnUnit != null || rule.Summon != null || rule.HasKillTriggers || rule.HasSpareTriggers ||
                rule.Effects.Count == 0 || rule.Effects.Concat(rule.OnCastEffects).Any(effect =>
                    effect.Type is not ("BuffHealth" or "DebuffHealth" or "Damage" or "Heal") ||
                    effect.Target is not ("DropTargetCharacter" or "Room") &&
                        !(effect.Type is "Damage" or "Heal" && effect.Target == "LastTargetedCharacters")))
                return new CardHoverPreviewResult("Card hover currently requires calibrated maximum-health, damage or healing spell effects.");
            if (rule.Effects.Concat(rule.OnCastEffects).Any(effect => effect.Type is "Damage" or "Heal") &&
                (links != null || rule.OnCastEffects.Count > 1 && rule.OnCastEffectGroupSizes == null ||
                    rule.Effects.Concat(rule.OnCastEffects).Any(effect => effect.Type is not ("Damage" or "Heal"))))
                return new CardHoverPreviewResult("Damage/healing hover requires damage/healing main/OnCast effects and no linked bodies.");
            if (action.Operation != null || action.ActivatorUnitId != 0 || action.MixSelections.Count != 0 || action.SummonSelections.Count != 0 ||
                action.RecursionSelections.Count != 0 || !primary.Cards.Hand.Any(card =>
                    card.InstanceId == action.CardInstanceId && card.DataId == rule.DataId))
                return new CardHoverPreviewResult("Card hover requires the captured hand card and an ordinary spell input.");
            CardInstanceState? hovered = primary.FindCard(action.CardInstanceId);
            if (hovered?.MaskDescriptor == null || hovered.MaskDescriptor.BaseTraits.Count != 0 ||
                hovered.MaskDescriptor.TemporaryTraits.Count != 0 || hovered.MaskDescriptor.PermanentReplacements.Count != 0 ||
                hovered.Permanent.Upgrades.Count != 0 || hovered.Temporary.Upgrades.Count != 0)
                return new CardHoverPreviewResult("Card hover with card traits or upgrades requires native calibration.");
            // Even empty native damage modifier planes clamp negative endpoints.
            rule = CardModifierModel.Resolve(rule, hovered);
            string? groupError = CardTriggerGroupModel.Validate(rule.OnCastEffectGroupSizes, rule.OnCastEffects.Count);
            if (groupError != null) return new CardHoverPreviewResult(groupError);
            if (rule.ExternalInteractions.Count != 0)
                return new CardHoverPreviewResult("Card hover modifiers: " + string.Join("; ", rule.ExternalInteractions));
            string? rangeError = rule.Effects.Concat(rule.OnCastEffects).Select(effect => effect.Range?.Validate()).FirstOrDefault(error => error != null);
            if (rangeError != null) return new CardHoverPreviewResult("Card hover range: " + rangeError);
            if (world.EnchanterIds.Count != 0 || world.RetainedUnits.Count != 0 || primary.Relics == null ||
                primary.Relics.Any(relic => relic.EffectTypes.Count != 0) || source.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Triggers.Any(trigger => trigger.Kind is "CardSpellPlayed" or "OnPreOwnAbilityActivated" or "OnOwnAbilityActivated")))
                return new CardHoverPreviewResult("Card hover with enchanters, retained actors, active relic effects or played-card callbacks requires native calibration.");
            if (source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger =>
                trigger.Kind is not ("PostCombat" or "OnSpawn" or "OnTrainRoomLoop") &&
                trigger.Effects.Any(effect => effect.Type != "CardEffectRewardGold"))))
                return new CardHoverPreviewResult("Card hover lifecycle callbacks beyond the calibrated gold rewards require native calibration.");
            // Raw primary UI caches and the retained accumulator need not match
            // the canonical normal-combat plane. Validate linkage here; the spell
            // resolver validates the complete temporary train after copying.
            string? error = links == null ? null : LifeLinkModel.Validate(links);
            if (error != null) return new CardHoverPreviewResult("Card hover primary validation: " + error);
            RoomCombatState? targetRoom = source.Rooms.SingleOrDefault(room => room.RoomIndex == action.RoomIndex);
            if (targetRoom == null || (action.TargetUnitId == 0
                    ? rule.Effects.Concat(rule.OnCastEffects).Any(effect => effect.Target == "DropTargetCharacter")
                    : !targetRoom.Units.Any(unit => unit.Id == action.TargetUnitId)))
                return new CardHoverPreviewResult("Card hover requires its captured room and any required drop target.");

            // Native collects enemies then players in the room, plus companion
            // bosses elsewhere. The original five linked bodies are not companions.
            int[] copiedIds = targetRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy)
                .Concat(targetRoom.Units.Where(unit => unit.Team == CombatTeam.Player)).Select(unit => unit.Id)
                .Concat(source.Rooms.Where(room => room.RoomIndex != action.RoomIndex).SelectMany(room => room.Units)
                    .Where(unit => unit.BumpRules?.CompanionBoss == true).Select(unit => unit.Id)).Distinct().ToArray();
            var temporaryLinks = links == null ? null : new LifeLinkContext(links.Actors, links.CalculatingPreview, links.SharedPreviewHpChange,
                copiedIds.Where(id => links.Actors.Any(actor => actor.Id == id)).ToArray());
            BattleSpawnPoints points = primary.SpawnPoints.BeginPreview();
            CombatContext context = primary.WithLifeLinks(temporaryLinks).WithSpawnPoints(points)
                .WithEnchantments(world.WithTemporaryPreviewUnits(copiedIds));
            TrainCombatState copied = WithContext(source, context, preview: true, copiedIds);
            bool dropOverride = rule.Effects.Any(effect => effect.Target == "DropTargetCharacter");
            bool continuousEffects = rule.Effects.All(effect => effect.Type is "Damage" or "Heal");
            // OnCast runs before the final main-effect death queue drains. Keep
            // damage/healing effects in one queue and derive drop overrides from main only.
            TrainSpellResult effects = continuousEffects
                ? CardSpellModel.ApplyCardHoverEffects(copied, action.RoomIndex, rule.Effects.Concat(rule.OnCastEffects).ToArray(),
                    action.TargetUnitId, action.CardInstanceId, dropOverride, rule.Effects.Count, rule.OnCastEffectGroupSizes)
                : CardSpellModel.Apply(copied, action.RoomIndex, rule.Effects, action.TargetUnitId, action.CardInstanceId);
            if (!effects.Supported) return new CardHoverPreviewResult("Card hover main effects: " + effects.UnsupportedReason);
            TrainCombatState after = effects.State!;
            var transientActors = effects.RetainedUnits.ToDictionary(unit => unit.Id);
            if (effects.PendingCallbacks.Count != 0)
                return new CardHoverPreviewResult("Card hover main effects left uncalibrated pending callbacks.");
            if (!continuousEffects && rule.OnCastEffects.Count > 0)
            {
                effects = CardSpellModel.Apply(after, action.RoomIndex, rule.OnCastEffects,
                    action.TargetUnitId, action.CardInstanceId);
                if (!effects.Supported) return new CardHoverPreviewResult("Card hover OnCast effects: " + effects.UnsupportedReason);
                if (effects.PendingCallbacks.Count != 0)
                    return new CardHoverPreviewResult("Card hover OnCast effects left uncalibrated pending callbacks.");
                after = effects.State!;
                foreach (var unit in effects.RetainedUnits) transientActors[unit.Id] = unit;
            }
            // Cancellation restores the original actor planes and cached preview
            // HP. Native keeps the allocator, shared maximum-health accumulator,
            // and any BattleTest draws; the real Battle stream stays unchanged.
            var restoredLinks = links == null ? null : new LifeLinkContext(links.Actors, links.CalculatingPreview,
                after.Context!.LifeLinks!.SharedPreviewHpChange);
            foreach (var actor in after.Context!.Enchantments!.RetainedUnits) transientActors.TryAdd(actor.Unit.Id, actor.Unit);
            var presentIds = after.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var removed = transientActors.Values.Where(unit => copiedIds.Contains(unit.Id) && !presentIds.Contains(unit.Id))
                .OrderBy(unit => unit.Id).ToArray();
            copied = ProjectPoints(primary.SpawnPoints, copied, copiedIds);
            after = ProjectPoints(primary.SpawnPoints, after, copiedIds);
            points = after.Context!.SpawnPoints!;
            context = primary.WithSpawnPoints(points).WithLifeLinks(restoredLinks)
                .WithEnchantments(world.WithTestRng(after.Context!.Enchantments!.TestRng));
            TrainCombatState restored = WithContext(source, context, preview: false);
            return new CardHoverPreviewResult(copied, after, restored, copiedIds, removed);
        }

        // Native captures expose primary actor/room point references even while
        // the operation uses temporary groups. Keep those groups during effect
        // resolution and project the observed pointers only at capture boundaries.
        private static TrainCombatState ProjectPoints(BattleSpawnPoints primary, TrainCombatState selected, IReadOnlyList<int> copiedIds)
        {
            var world = selected.Context!.Enchantments!;
            var context = selected.Context!.WithSpawnPoints(BattleSpawnPoints.EndPreview(primary,
                selected.Context.SpawnPoints!, clearBirths: false)).WithEnchantments(new EnchantmentWorld(
                    world.Rooms, world.Movement, world.EnemySlotsPerRoom,
                    world.RetainedUnits.Where(actor => !copiedIds.Contains(actor.Unit.Id)).ToArray(), world.EnchanterIds,
                    world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
            return WithContext(selected, context, preview: true);
        }

        private static TrainCombatState WithContext(TrainCombatState source, CombatContext context, bool preview,
            IReadOnlyCollection<int>? copiedIds = null) =>
            EnchantmentWorldModel.Rebase(new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(
                room.RoomIndex, room.Deployment, copiedIds == null ? room.Units : room.Units.Select(unit => copiedIds.Contains(unit.Id)
                    ? unit.WithTriggers(unit.Triggers.Select(trigger => trigger.ForPreview()).ToArray()) : unit).ToArray(),
                room.ExternalInteractions, context, preview,
                room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context), preview: preview);
    }
}
