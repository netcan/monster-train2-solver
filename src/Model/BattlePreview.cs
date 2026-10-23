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
        internal static TrainCombatResult Refresh(TrainCombatState source, BattlePlayRules? playRules = null)
        {
            CombatContext? context = source.Context;
            if (context?.Statistics == null) return new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            if (context.LifeLinks is LifeLinkContext links)
            {
                if (playRules == null || !source.Rooms.All(TrueFinalBossCombatModel.Supports))
                    return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), "Uncalibrated true-final-boss preview scope.");
                context = context.WithLifeLinks(new LifeLinkContext(links.Actors.Select(actor => new LifeLinkActor(actor.Id,
                    actor.LinkedId, actor.Health, actor.Health, actor.Initialized, actor.HasLifeLink, actor.TrueFinalBoss,
                    actor.OuterBoss)).ToArray(), true, 0));
            }
            BattleSpawnPoints? originalPoints = context.SpawnPoints;
            if (originalPoints != null) context = context.WithSpawnPoints(originalPoints.BeginPreview());
            var previewTrain = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, room.Units, room.ExternalInteractions, context, preview: true,
                activeCompanionBossIds: room.ActiveCompanionBossIds)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);
            EnchantmentWorld? originalWorld = context.Enchantments;
            if (originalWorld?.AutomaticLifecycle == true)
            {
                EnchantmentCombatState prepared = originalWorld.Frame(context);
                var live = originalWorld.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
                foreach (int id in originalWorld.EnchanterIds.Where(live.Contains))
                    prepared = EnchantmentCombatModel.PrepareForPreview(prepared, id);
                context = context.WithEnchantments(EnchantmentWorld.From(prepared, true));
                previewTrain = EnchantmentWorldModel.Rebase(new TrainCombatState(prepared.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    room.Deployment, room.Units, room.ExternalInteractions, context, true, room.ActiveCompanionBossIds)).ToArray(), source.Movement,
                    source.EnemySlotsPerRoom, context), preview: true, testRng: context.IsolatedBattlePreview == true ? context.BattleRng : originalWorld.TestRng);
            }
            var overwrittenSummons = new HashSet<(int UnitId, int TriggerId, int EffectIndex)>();
            var sacrificeCards = new Dictionary<int, int>();
            int firstPreviewId = context.NextUnitId ?? int.MaxValue;
            int firstPreviewCardId = context.NextCardId;
            bool previewBirth = false;
            var originalUnits = source.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0)
                .Select(unit => unit.Id).ToHashSet();
            void CarryRoomCaches(CombatContext from)
            {
                // HeroManager keeps preview-born enemies alive after restoring
                // their primary state. Card cache weak references still resolve
                // to these actors, including births from earlier previews.
                if (from.Enchantments is EnchantmentWorld observed)
                    originalUnits.UnionWith(observed.Rooms.SelectMany(room => room.Units)
                        .Concat(observed.RetainedUnits.Select(actor => actor.Unit))
                        .Where(unit => unit.Team == CombatTeam.Enemy &&
                            (unit.DeathState?.PreviewPrimary ?? unit) is CombatUnit primary &&
                            primary.Health > 0 && primary.DeathState?.IsDestroyed != true).Select(unit => unit.Id));
                foreach (RoomEchoState echo in (context.RoomCapacities ?? Array.Empty<RoomCapacityState>())
                    .SelectMany(room => room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                    .Where(attachment => attachment.Echo?.CopyCardId > 0).Select(attachment => attachment.Echo!))
                {
                    CardPlayedCost? cost = from.Statistics?.PlayedCosts.FirstOrDefault(value => value.CardId == echo.CopyCardId);
                    if (cost != null) context = context.WithStatistics(context.Statistics?.WithPlayedCost(cost.CardId, cost.Cost));
                }
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
                // HeroManager births do not overwrite MonsterManager's last
                // spawned player reference; only temporary player births do.
                previewBirth |= after?.LastSpawnedUnitId >= firstPreviewId ||
                    units.Concat(after?.Enchantments?.RetainedUnits.Select(actor => actor.Unit) ?? Array.Empty<CombatUnit>())
                        .Any(unit => unit.Id >= firstPreviewId && unit.Team == CombatTeam.Player);
                foreach (CombatUnit unit in units)
                {
                    // SacrificeCard lives outside CharacterState.PrimaryStateInformation.
                    // Preview cancellation restores HP and IsSacrifice, but keeps this reference.
                    if (unit.SacrificeCardId is int cardId) sacrificeCards[unit.Id] = cardId;
                    for (int triggerIndex = 0; triggerIndex < unit.Triggers.Count; triggerIndex++)
                    for (int effectIndex = 0; effectIndex < unit.Triggers[triggerIndex].Effects.Count; effectIndex++)
                        if (unit.Triggers[triggerIndex].Effects[effectIndex].Summon?.FirstSpawnedUnitId >= firstPreviewId)
                            overwrittenSummons.Add((unit.Id, unit.Triggers[triggerIndex].StateId ?? triggerIndex, effectIndex));
                }
            }
            CombatUnit RestorePrimaryEffects(CombatUnit unit)
            {
                if (sacrificeCards.TryGetValue(unit.Id, out int cardId)) unit = unit.WithSacrifice(cardId, markSacrifice: false);
                return overwrittenSummons.Count == 0 ? unit : unit.WithTriggers(unit.Triggers.Select((trigger, index) => trigger.WithEffects(trigger.Effects.Select((effect, effectIndex) =>
                    effect.Summon != null && overwrittenSummons.Contains((unit.Id, trigger.StateId ?? index, effectIndex))
                        ? effect.WithSummon(effect.Summon.WithFirstSpawned(0)) : effect).ToArray())).ToArray());
            }
            // Native UI preview runs only the player pre-discard phase, once before all rooms.
            TrainCombatResult preDiscard = TrainCombatModel.EndTurnPreHandDiscard(previewTrain, CombatTeam.Player);
            if (!preDiscard.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                Array.Empty<RoomCombatResult>(), "Battle preview: " + preDiscard.UnsupportedReason);
            TrainCombatState preparedTrain = preDiscard.State!;
            if (playRules != null && preparedTrain.Context!.Cards.Hand.Any(card =>
                playRules.Cards.Any(rule => rule.DataId == card.DataId && rule.OnUnplayedNegativeEffects.Count > 0)))
            {
                // CardManager previews unplayed negative triggers before the first
                // room, including ordinary battles. Original scourges can already
                // kill the preview Pyre and stop every subsequent room here.
                var unplayed = preparedTrain.Context!.Cards.Hand.Select((card, index) => new
                { Card = card, Index = index, Rule = playRules!.Cards.FirstOrDefault(rule => rule.DataId == card.DataId) })
                    .Where(item => item.Rule?.OnUnplayedNegativeEffects.Count > 0)
                    .OrderBy(item => item.Rule!.UnplayedNegativeOrder.HasValue ? 0 : 1)
                    .ThenBy(item => item.Rule!.UnplayedNegativeOrder ?? item.Index).ToArray();
                foreach (var item in unplayed)
                {
                    CardInstanceState? instance = preparedTrain.Context!.FindCard(item.Card.InstanceId);
                    if (instance == null) return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), "Missing preview unplayed card instance.");
                    CardPlayRule rule = CardModifierModel.Resolve(item.Rule!, instance);
                    if (rule.HasUnsupportedUnplayedTriggers || rule.OnUnplayedNegativeEffects.Any(effect => effect.Type != "Damage" || effect.Target != "Pyre" || effect.Range != null))
                        return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), "Uncalibrated preview unplayed effect.");
                    TrainSpellResult applied = CardSpellModel.ApplyForCardPlay(preparedTrain, preparedTrain.Context.QueryFrame?.SelectedRoomIndex ?? 0,
                        rule.OnUnplayedNegativeEffects, 0, item.Card.InstanceId, playRules!, preparedTrain.Context.OtherPiles,
                        onUnplayedNegativeEffects: true);
                    if (!applied.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), applied.UnsupportedReason);
                    preparedTrain = applied.State!;
                    if (applied.PendingCallbacks.Count > 0)
                    {
                        TrainCombatResult completed = TrainCombatModel.ApplyCharacterQueue(preparedTrain, applied.PendingCallbacks.ToList());
                        if (!completed.Supported) return completed;
                        preparedTrain = completed.State!;
                    }
                }
            }
            CombatContext previewContext = preparedTrain.Context!;
            CarryRoomCaches(previewContext);
            ObserveSummons(preparedTrain.Rooms.SelectMany(room => room.Units), previewContext);
            int pyreRoomIndex = preparedTrain.Rooms.Count - 1;
            CombatUnit? previewPyre = preparedTrain.Rooms[pyreRoomIndex].Units.FirstOrDefault(unit => unit.IsPyre);
            bool previewStopped = source.Rooms.SelectMany(room => room.Units).Any(unit => unit.IsPyre) &&
                (previewPyre == null || previewPyre.Health <= 0);
            foreach (RoomCombatState room in preparedTrain.Rooms.Reverse().Where(_ => context.LifeLinks == null || previewPyre != null))
            {
                if (previewStopped) break;
                bool targetsPyre = room.RoomIndex != pyreRoomIndex && room.Units.Any(unit =>
                    unit.Triggers.Any(trigger => trigger.Effects.Any(effect =>
                        effect.Type is "CardEffectPyreDamageIfNoPlayerUnits" or "CardEffectHealTrain")));
                bool targetsTower = room.Units.SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects)
                    .Any(effect => (effect.Action ?? effect.UnitUpgrade)?.Target is "Tower" or "FrontInAllRooms");
                if ((targetsTower || previewContext.LifeLinks != null) && previewContext.Enchantments == null)
                    return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(),
                        "Tower preview targeting requires its captured complete actor world.");
                RoomCombatResult preview = RoomCombatModel.Resolve(EnchantmentWorldModel.Refresh(new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.Units, room.ExternalInteractions, previewContext, preview: true,
                    activeCompanionBossIds: room.ActiveCompanionBossIds)),
                    externalPyre: targetsPyre ? previewPyre : null,
                    externalPyreRoomIndex: targetsPyre && previewPyre != null ? pyreRoomIndex : -1,
                    externalTargets: !targetsTower && previewContext.LifeLinks == null ? null : previewContext.Enchantments!.Rooms
                        .Where(other => other.RoomIndex != room.RoomIndex).SelectMany(other => other.Units
                            .Where(unit => !targetsPyre || unit.Id != previewPyre?.Id).Select(unit => (other.RoomIndex, unit))).ToArray());
                if (!preview.Supported)
                    return new TrainCombatResult(null, RoomOutcome.Unsupported,
                        Array.Empty<RoomCombatResult>(), "Battle preview floor " + room.RoomIndex + ": " + preview.UnsupportedReason);
                if (targetsPyre && previewPyre != null)
                    previewPyre = preview.RetainedUnits.Single(unit => unit.Id == previewPyre.Id);
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
                if (previewContext.LifeLinks != null) previewContext = previewContext.WithLifeLinks(preview.State.Context.LifeLinks);
                if (originalPoints?.NextPreviewCopyId.HasValue == true && preview.State.Context.SpawnPoints != null)
                    previewContext = previewContext.WithSpawnPoints(preview.State.Context.SpawnPoints);
                if (context.IsolatedBattlePreview == true) previewContext = previewContext.WithBattleRng(preview.State!.Context!.BattleRng);
                if (originalWorld?.AutomaticLifecycle == true)
                {
                    previewContext = previewContext.WithEnchantments(preview.State!.Context!.Enchantments);
                    if (preview.State.Context.NextUnitId is int nextId) previewContext = previewContext.WithNextUnitId(nextId);
                    if (previewContext.CardRegistry != null && preview.State.Context.CardRegistry != null)
                        previewContext = previewContext.WithCardRegistry(previewContext.CardRegistry.Concat(
                            preview.State.Context.CardRegistry.Where(card => card.InstanceId >= firstPreviewCardId)).ToArray())
                            .WithNextCardId(preview.State.Context.NextCardId);
                }
                if (context.LifeLinks != null && previewPyre == null || preview.State.Units.Concat(preview.RetainedUnits)
                    .Any(unit => unit.Health <= 0 && (unit.EndsBattleOnDeath || unit.IsPyre))) break;
            }
            context = context.WithStatistics(context.Statistics!.WithLastAttackDamage(previewContext.Statistics!.LastAttackDamageDealt));
            if (source.Context!.LifeLinks != null) context = context.WithLifeLinks(source.Context.LifeLinks);
            if (originalPoints?.NextPreviewCopyId.HasValue == true)
                context = context.WithSpawnPoints(BattleSpawnPoints.EndPreview(originalPoints, previewContext.SpawnPoints!, clearBirths: true));
            // Native preview births overwrite shared weak references. Their temporary
            // Unity objects disappear before the next stable decision capture.
            if (previewBirth && context.LastSpawnedUnitId.HasValue) context = context.WithLastSpawned(0);
            if (originalWorld?.AutomaticLifecycle == true)
            {
                EnchantmentWorld observed = previewContext.Enchantments!;
                var actors = observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit)).ToDictionary(unit => unit.Id);
                int[] removed = actors.Keys.Where(id => id >= firstPreviewId).ToArray();
                if (previewContext.NextUnitId is int nextId) context = context.WithNextUnitId(nextId);
                if (context.CardRegistry != null && previewContext.CardRegistry != null)
                    context = context.WithCardRegistry(context.CardRegistry.Concat(
                        PreviewBirthModel.RestoreSourceCards(previewContext, firstPreviewCardId, originalUnits)).ToArray())
                        .WithNextCardId(previewContext.NextCardId);
                if (context.SpawnPoints != null && originalPoints?.NextPreviewCopyId.HasValue != true)
                    context = context.WithSpawnPoints(context.SpawnPoints.WithState(context.SpawnPoints.Groups, context.SpawnPoints.Units.Concat(
                        removed.Select(id => new UnitSpawnPointState(id, null, null, spawnedInPreview: true))).ToArray()));
                // Ordinary solitary bosses restore their preview actions. The
                // separately managed outer boss and its companion retain them.
                int[] previewBossIds = actors.Values.Where(unit => unit.IsBoss == true && unit.BumpRules?.OuterBoss != true &&
                    unit.BumpRules?.CompanionBoss != true)
                    .Select(unit => unit.Id).ToArray();
                CombatUnit RestoreEnchantments(CombatUnit original)
                {
                    if (!actors.TryGetValue(original.Id, out CombatUnit? tested)) return original;
                    return PreviewEffectsModel.Restore(RestorePrimaryEffects(original), tested, removed,
                        resetPreviewActionsForUnitIds: previewBossIds);
                }
                // HeroManager's global preview reset also reaches enemy births
                // retained by earlier temporary boss-kill previews this turn.
                context = context.WithEnchantments(new EnchantmentWorld(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    room.Deployment, room.Units.Select(RestoreEnchantments).ToArray(), room.ExternalInteractions, null, room.Preview,
                    room.ActiveCompanionBossIds)).ToArray(),
                    source.Movement, source.EnemySlotsPerRoom, originalWorld.RetainedUnits.Select(actor =>
                        actor.Preview && actor.Unit.Team == CombatTeam.Enemy ? new EnchantmentRetainedUnit(
                            PreviewBirthModel.Restore(actors.TryGetValue(actor.Unit.Id, out CombatUnit? tested) ? tested : actor.Unit,
                                removed, resetPreviewActionsForUnitIds: previewBossIds), -1, false) : new EnchantmentRetainedUnit(
                            RestoreEnchantments(actor.Unit), actor.RoomIndex, actor.Preview))
                        .Concat(PreviewBirthModel.Retain(observed, firstPreviewId, removed, previewBossIds)).ToArray(),
                    originalWorld.EnchanterIds.Concat(observed.EnchanterIds).Distinct().ToArray(),
                    originalWorld.AllowUpdates, originalWorld.Updating, originalWorld.Preview, originalWorld.TestRng, true));
            }
            // Ordinary preview births have temporary simulation IDs: native identity
            // capture skips them. Only the observed enchantment lifecycle above keeps
            // those IDs and advances the persistent counter. Preserve older retained
            // actors, including deaths and already observed preview births.
            if (context.SpawnPoints is BattleSpawnPoints restoredPoints && context.NextUnitId is int nextUnitId &&
                restoredPoints.Units.Any(unit => unit.SpawnedInPreview && unit.UnitId >= nextUnitId))
                context = context.WithSpawnPoints(restoredPoints.WithState(restoredPoints.Groups,
                    restoredPoints.Units.Where(unit => !unit.SpawnedInPreview || unit.UnitId < nextUnitId).ToArray()));
            var train = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(RestorePrimaryEffects).ToArray(), room.ExternalInteractions, context, room.Preview,
                room.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context);
            if (originalWorld?.AutomaticLifecycle == true)
            {
                train = new TrainCombatState(train.Rooms.Select(EnchantmentWorldModel.Refresh).ToArray(), train.Movement, train.EnemySlotsPerRoom, context);
                TrainCombatResult settled = EnchantmentWorldModel.UpdateAll(train);
                if (!settled.Supported) return settled;
                bool statusesChanged = !train.Rooms.SelectMany(room => room.Units).Select(unit => string.Join(";", unit.Statuses.Select(status => status.Id + ":" + status.Stacks)))
                    .SequenceEqual(settled.State!.Rooms.SelectMany(room => room.Units).Select(unit => string.Join(";", unit.Statuses.Select(status => status.Id + ":" + status.Stacks))));
                train = settled.State!;
                // The isolated preview protocol includes the primary order update and
                // its drained callbacks, so extra UI refreshes cannot advance combat RNG.
                if (context.IsolatedBattlePreview == true)
                    train = EnchantmentWorldModel.Rebase(new TrainCombatState(train.Rooms, train.Movement,
                        train.EnemySlotsPerRoom, train.Context!.WithBattleRng(source.Context!.BattleRng)), testRng: originalWorld.TestRng);
                if (statusesChanged) return Refresh(train, playRules);
            }
            return new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        }
    }
}
