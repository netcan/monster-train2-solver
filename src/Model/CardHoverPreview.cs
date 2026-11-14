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
    // Maximum-health spells use temporary room copies, including lethal ordinary
    // units and the calibrated nonlethal linked-boss roster. Battle previews keep
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
            if (rule.Effect != "Spell" || rule.CardType != "Spell" || rule.IsAnyAbility != false ||
                rule.ExternalInteractions.Count != 0 || rule.Equipment != null || rule.Ability != null ||
                rule.SpawnUnit != null || rule.Summon != null || rule.HasKillTriggers ||
                rule.Effects.Count == 0 || rule.Effects.Concat(rule.OnCastEffects).Any(effect =>
                    effect.Type is not ("BuffHealth" or "DebuffHealth") || effect.Target is not ("DropTargetCharacter" or "Room")))
                return new CardHoverPreviewResult("Card hover currently requires calibrated maximum-health spell effects and OnCast effects.");
            if (action.Operation != null || action.ActivatorUnitId != 0 || action.MixSelections.Count != 0 ||
                action.RecursionSelections.Count != 0 || !primary.Cards.Hand.Any(card =>
                    card.InstanceId == action.CardInstanceId && card.DataId == rule.DataId))
                return new CardHoverPreviewResult("Card hover requires the captured hand card and an ordinary spell input.");
            CardInstanceState? hovered = primary.FindCard(action.CardInstanceId);
            if (hovered?.MaskDescriptor == null || hovered.MaskDescriptor.BaseTraits.Count != 0 ||
                hovered.MaskDescriptor.TemporaryTraits.Count != 0 || hovered.MaskDescriptor.PermanentReplacements.Count != 0 ||
                hovered.Permanent.Upgrades.Count != 0 || hovered.Temporary.Upgrades.Count != 0)
                return new CardHoverPreviewResult("Card hover with card traits or upgrades requires native calibration.");
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
            if (targetRoom == null || !targetRoom.Units.Any(unit => unit.Id == action.TargetUnitId))
                return new CardHoverPreviewResult("Card hover requires a captured target in its selected room.");

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
            TrainSpellResult effects = CardSpellModel.Apply(copied, action.RoomIndex, rule.Effects,
                action.TargetUnitId, action.CardInstanceId);
            if (!effects.Supported) return new CardHoverPreviewResult("Card hover main effects: " + effects.UnsupportedReason);
            TrainCombatState after = effects.State!;
            if (effects.PendingCallbacks.Count != 0)
                return new CardHoverPreviewResult("Card hover main effects left uncalibrated pending callbacks.");
            if (rule.OnCastEffects.Count > 0)
            {
                effects = CardSpellModel.Apply(after, action.RoomIndex, rule.OnCastEffects,
                    action.TargetUnitId, action.CardInstanceId);
                if (!effects.Supported) return new CardHoverPreviewResult("Card hover OnCast effects: " + effects.UnsupportedReason);
                if (effects.PendingCallbacks.Count != 0)
                    return new CardHoverPreviewResult("Card hover OnCast effects left uncalibrated pending callbacks.");
                after = effects.State!;
            }
            // Cancellation restores the original actor planes and cached preview
            // HP. Native keeps the allocator and shared maximum-health accumulator.
            var restoredLinks = links == null ? null : new LifeLinkContext(links.Actors, links.CalculatingPreview,
                after.Context!.LifeLinks!.SharedPreviewHpChange);
            var removed = after.Context!.Enchantments!.RetainedUnits.Where(actor => copiedIds.Contains(actor.Unit.Id))
                .Select(actor => actor.Unit).ToArray();
            copied = ProjectPoints(primary.SpawnPoints, copied, copiedIds);
            after = ProjectPoints(primary.SpawnPoints, after, copiedIds);
            points = after.Context!.SpawnPoints!;
            context = primary.WithSpawnPoints(points).WithLifeLinks(restoredLinks);
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
