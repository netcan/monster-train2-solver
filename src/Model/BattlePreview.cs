using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class BattlePreviewModel
    {
        // The installed game's preview does not guard SetAttackDamageDealt with PreviewMode.
        // Room units are copied, while source card caches also retain native
        // preview writes. Their weak references resolve to the restored live units.
        internal static TrainCombatResult Refresh(TrainCombatState source)
        {
            CombatContext? context = source.Context;
            if (context?.Statistics == null) return new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            var previewTrain = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, room.Units, room.ExternalInteractions, context, preview: true)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);
            EnchantmentWorld? originalWorld = context.Enchantments;
            if (originalWorld?.AutomaticLifecycle == true)
            {
                EnchantmentCombatState prepared = originalWorld.Frame(context);
                foreach (int id in originalWorld.EnchanterIds) prepared = EnchantmentCombatModel.PrepareForPreview(prepared, id);
                context = context.WithEnchantments(EnchantmentWorld.From(prepared, true));
                previewTrain = EnchantmentWorldModel.Rebase(new TrainCombatState(prepared.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    room.Deployment, room.Units, room.ExternalInteractions, context, true)).ToArray(), source.Movement,
                    source.EnemySlotsPerRoom, context), preview: true, testRng: context.IsolatedBattlePreview == true ? context.BattleRng : originalWorld.TestRng);
            }
            var overwrittenSummons = new HashSet<(int UnitId, int TriggerId, int EffectIndex)>();
            int firstPreviewId = context.NextUnitId ?? int.MaxValue;
            bool previewBirth = false;
            var originalUnits = source.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0)
                .Select(unit => unit.Id).ToHashSet();
            void CarryRoomCaches(CombatContext from)
            {
                foreach (CardInstanceState original in context.CardRegistry ?? context.CardInstances ?? Array.Empty<CardInstanceState>())
                {
                    CardInstanceState? captured = from.FindCard(original.InstanceId);
                    IReadOnlyList<int>? cache = captured?.RawPlayedRoomUnitIds ?? captured?.PlayedRoomUnitIds;
                    if (original.PlayedRoomUnitIds != null && cache != null)
                    {
                        int[] restored = cache.Where(originalUnits.Contains).ToArray();
                        if (!restored.SequenceEqual(original.PlayedRoomUnitIds) || original.RawPlayedRoomUnitIds != null &&
                            !restored.SequenceEqual(original.RawPlayedRoomUnitIds))
                            context = context.WithCard(original.WithRoomCacheState(restored,
                                original.RawPlayedRoomUnitIds == null ? null : restored));
                    }
                }
            }
            void ObserveSummons(IEnumerable<CombatUnit> units, CombatContext? after)
            {
                previewBirth |= after?.NextUnitId > firstPreviewId;
                foreach (CombatUnit unit in units)
                for (int triggerIndex = 0; triggerIndex < unit.Triggers.Count; triggerIndex++)
                for (int effectIndex = 0; effectIndex < unit.Triggers[triggerIndex].Effects.Count; effectIndex++)
                    if (unit.Triggers[triggerIndex].Effects[effectIndex].Summon?.FirstSpawnedUnitId >= firstPreviewId)
                        overwrittenSummons.Add((unit.Id, unit.Triggers[triggerIndex].StateId ?? triggerIndex, effectIndex));
            }
            CombatUnit RestorePrimaryEffects(CombatUnit unit) => overwrittenSummons.Count == 0 ? unit :
                unit.WithTriggers(unit.Triggers.Select((trigger, index) => trigger.WithEffects(trigger.Effects.Select((effect, effectIndex) =>
                    effect.Summon != null && overwrittenSummons.Contains((unit.Id, trigger.StateId ?? index, effectIndex))
                        ? effect.WithSummon(effect.Summon.WithFirstSpawned(0)) : effect).ToArray())).ToArray());
            // Native UI preview runs only the player pre-discard phase, once before all rooms.
            TrainCombatResult preDiscard = TrainCombatModel.EndTurnPreHandDiscard(previewTrain, CombatTeam.Player);
            if (!preDiscard.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                Array.Empty<RoomCombatResult>(), "Battle preview: " + preDiscard.UnsupportedReason);
            CombatContext previewContext = preDiscard.State!.Context!;
            CarryRoomCaches(previewContext);
            ObserveSummons(preDiscard.State.Rooms.SelectMany(room => room.Units), previewContext);
            foreach (RoomCombatState room in preDiscard.State.Rooms.Reverse())
            {
                RoomCombatResult preview = RoomCombatModel.Resolve(EnchantmentWorldModel.Refresh(new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.Units, room.ExternalInteractions, previewContext, preview: true)));
                if (!preview.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                    Array.Empty<RoomCombatResult>(), "Battle preview: " + preview.UnsupportedReason);
                ObserveSummons(preview.State!.Units.Concat(preview.RetainedUnits), preview.State.Context);
                CarryRoomCaches(preview.State.Context!);
                foreach (CardInstanceState card in previewContext.CardRegistry ?? previewContext.CardInstances ?? Array.Empty<CardInstanceState>())
                {
                    CardInstanceState? captured = preview.State.Context!.FindCard(card.InstanceId);
                    if (card.PlayedRoomUnitIds != null && captured?.PlayedRoomUnitIds != null &&
                        (!card.PlayedRoomUnitIds.SequenceEqual(captured.PlayedRoomUnitIds) ||
                            card.RawPlayedRoomUnitIds != null && !card.RawPlayedRoomUnitIds.SequenceEqual(captured.RawPlayedRoomUnitIds!)))
                        previewContext = previewContext.WithCard(card.WithRoomCacheState(captured.PlayedRoomUnitIds, captured.RawPlayedRoomUnitIds));
                }
                previewContext = previewContext.WithStatistics(previewContext.Statistics!.WithLastAttackDamage(preview.State!.Context!.Statistics!.LastAttackDamageDealt));
                if (context.IsolatedBattlePreview == true) previewContext = previewContext.WithBattleRng(preview.State!.Context!.BattleRng);
                if (originalWorld?.AutomaticLifecycle == true) previewContext = previewContext.WithEnchantments(preview.State!.Context!.Enchantments);
                if (preview.RetainedUnits.Any(unit => unit.Health <= 0 && (unit.EndsBattleOnDeath || unit.IsPyre))) break;
            }
            context = context.WithStatistics(context.Statistics!.WithLastAttackDamage(previewContext.Statistics!.LastAttackDamageDealt));
            // Native preview births overwrite shared weak references. Their temporary
            // Unity objects disappear before the next stable decision capture.
            if (previewBirth && context.LastSpawnedUnitId.HasValue) context = context.WithLastSpawned(0);
            if (originalWorld?.AutomaticLifecycle == true)
            {
                EnchantmentWorld observed = previewContext.Enchantments!;
                var actors = observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit)).ToDictionary(unit => unit.Id);
                CombatUnit RestoreEnchantments(CombatUnit original)
                {
                    if (!actors.TryGetValue(original.Id, out CombatUnit? tested)) return original;
                    return original.WithTriggers(original.Triggers.Select((trigger, index) => trigger.WithEffects(trigger.Effects.Select((effect, effectIndex) =>
                    {
                        EnchantmentRule? after = index < tested.Triggers.Count && effectIndex < tested.Triggers[index].Effects.Count
                            ? tested.Triggers[index].Effects[effectIndex].Enchantment : null;
                        return effect.Enchantment == null || after == null ? effect : effect.WithEnchantment(effect.Enchantment.WithState(
                            after.State));
                    }).ToArray())).ToArray());
                }
                context = context.WithEnchantments(new EnchantmentWorld(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    room.Deployment, room.Units.Select(RestoreEnchantments).ToArray(), room.ExternalInteractions, null, room.Preview)).ToArray(),
                    source.Movement, source.EnemySlotsPerRoom, originalWorld.RetainedUnits.Select(actor => new EnchantmentRetainedUnit(
                        RestoreEnchantments(actor.Unit), actor.RoomIndex, actor.Preview)).ToArray(), originalWorld.EnchanterIds,
                    originalWorld.AllowUpdates, originalWorld.Updating, originalWorld.Preview, originalWorld.TestRng, true));
            }
            var train = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(RestorePrimaryEffects).ToArray(), room.ExternalInteractions, context)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context);
            if (originalWorld?.AutomaticLifecycle == true)
            {
                train = new TrainCombatState(train.Rooms.Select(EnchantmentWorldModel.Refresh).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
                TrainCombatResult settled = EnchantmentWorldModel.UpdateAll(train);
                if (!settled.Supported) return settled;
                bool statusesChanged = !train.Rooms.SelectMany(room => room.Units).Select(unit => string.Join(";", unit.Statuses.Select(status => status.Id + ":" + status.Stacks)))
                    .SequenceEqual(settled.State!.Rooms.SelectMany(room => room.Units).Select(unit => string.Join(";", unit.Statuses.Select(status => status.Id + ":" + status.Stacks))));
                train = settled.State!;
                if (statusesChanged) return Refresh(train);
            }
            return new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        }
    }
}
