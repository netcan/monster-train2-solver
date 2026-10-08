using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class HeroUnitBirthModel
    {
        internal static UnitCloneResult Apply(TrainCombatState source, int sourceId, int roomIndex, UnitCopyCatalog? catalog,
            bool copyStats, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> prior)
        {
            TrainCombatState state = source; int unitId = 0; RoomOutcome outcome = RoomOutcome.Exchanged;
            var pending = prior.ToList(); var events = new List<CombatEvent>(); var dispatched = new List<UnitCloneCallback>();
            RoomCombatState? destination = state.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (destination == null || state.Context?.SpawnPoints == null) return Fail("Hero birth requires its selected room and physical points.");
            BattleSpawnPointResult centered = BattleSpawnPointModel.Apply(state.Context.SpawnPoints, destination, "Compact", CombatTeam.Enemy);
            if (!centered.Supported) return Fail(centered.Error!);
            CombatContext centeredContext = state.Context.WithSpawnPoints(centered.State!);
            destination = new RoomCombatState(roomIndex, destination.Deployment,
                BattleSpawnPointModel.Order(centered.State!, roomIndex, destination.Units), destination.ExternalInteractions, centeredContext);
            state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(room => room.RoomIndex == roomIndex ? destination : room).ToArray(),
                state.Movement, state.EnemySlotsPerRoom, centeredContext), centeredContext);
            int point = state.Context.SpawnPoints.FirstEmpty(roomIndex, CombatTeam.Enemy);
            if (point < 0) return Result();
            CombatUnit? original = Get(sourceId);
            if (original == null) return Fail("Missing retained hero-copy source.");
            UnitCopyBirthDefinition? birth = catalog?.Births.SingleOrDefault(item => item.Unit.AssetKey == original.AssetKey);
            EnemyDefinition? definition = birth?.HeroDefinition;
            if (definition == null) return Fail("Missing raw hero birth definition.");
            if (birth!.Grafted || definition.ExternalInteractions.Count > 0)
                return Fail("Unmodeled hero birth interactions: " + string.Join(", ", definition.ExternalInteractions));
            if (definition.Unit.IsBoss == true || definition.Unit.EndsBattleOnDeath)
                return Fail("Hero-copy Boss registration and death ownership require their native lifecycle inputs.");
            string? error = AbilityLifecycleModel.SpawnError(definition.Unit, state.Context);
            if (error != null) return Fail(error);
            UnitIdentityAllocation allocated = UnitIdentityModel.Allocate(state.Context);
            if (!allocated.Supported) return Fail(allocated.UnsupportedReason!);
            unitId = allocated.UnitId;
            CombatUnit unit = AbilityLifecycleModel.SuppressAtSpawn(definition.Create(unitId), allocated.Context);
            BattleSpawnPointResult physical = BattleSpawnPointModel.Birth(allocated.Context!.SpawnPoints!, destination, unit, point, shift: false);
            if (!physical.Supported) return Fail(physical.Error!);
            CombatContext context = allocated.Context.WithSpawnPoints(physical.State!);
            var entered = new RoomCombatState(roomIndex, destination.Deployment,
                BattleSpawnPointModel.Order(physical.State, roomIndex, destination.Units.Append(unit).ToArray()), destination.ExternalInteractions, context);
            state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(room => room.RoomIndex == roomIndex ? entered : room).ToArray(),
                state.Movement.Append(new EnemyMovement(unitId, 1, definition.Ascends, definition.Loops, definition.CompanionBoss)).ToArray(), state.EnemySlotsPerRoom, context), context);
            RoomCombatResult prepared = RoomCombatModel.PrepareSpawnTriggers(FindRoom()!, unitId, false, unit.StatusRegistry ?? unit.Statuses);
            if (!Accept(prepared)) return Fail(prepared.UnsupportedReason!);
            // CreateHeroState clears IsSpawning before the separate OnSpawn call.
            state = UnitCloneModel.ReplaceUnit(state, HordeStatusModel.WithSpawning(Get(unitId)!, false));
            // Hero covenant/added notifications drain setup and incoming callbacks
            // before the separate OnSpawn queue, including their child callbacks.
            if (!Drain()) return Fail(error!);
            foreach (string phase in new[] { "OnSpawn", "OnUnscaledSpawn", "OnSpawnNotFromCard", "AfterSpawnEnchant" })
            {
                CombatUnit? live = Get(unitId);
                if (live == null) return Fail("A hero removed during birth requires retained birth-object transitions.");
                pending.Add(new RoomCombatModel.QueuedCharacterTrigger(roomIndex, live, phase));
                if (!Drain()) return Fail(error!);
            }
            if (!copyStats) return Result();
            original = Get(sourceId);
            if (original == null) return Fail("Hero birth removed its retained copy source.");
            UnitCloneRule? recipe = catalog!.Resolve(state.Context!, original);
            if (recipe == null) return Fail("Missing source equipment copy definitions.");
            var targetEquipmentStatuses = new List<string>();
            foreach (int equipmentId in original.EquipmentCards ?? Array.Empty<int>())
            {
                UnitCloneGearRule? equipment = recipe.Gear.SingleOrDefault(item => item.CardId == equipmentId);
                if (equipment == null) return Fail("Missing copied hero equipment definition.");
                if (equipment.Grafted) continue;
                CardGenerationResult copied = CardGenerationModel.CloneDetached(state.Context!, equipment.Creation, equipmentId);
                if (!copied.Supported) return Fail(copied.UnsupportedReason!);
                state = CardSpellModel.WithContext(state, copied.Context!); int newCardId = copied.AddedCards.Single().InstanceId;
                CombatUnit actor = Get(unitId)!;
                state = UnitCloneModel.ReplaceUnit(state, UnitCloneModel.Copy(actor, actor.Modifiers!, actor.BaseAttack, actor.Health, actor.MaxHealth,
                    (actor.EquipmentCards ?? Array.Empty<int>()).Append(newCardId).ToArray()));
                state = CardSpellModel.WithContext(state, state.Context!.WithCard(state.Context.FindCard(newCardId)!.WithEquippedUnit(unitId)));
                CardInstanceState card = state.Context!.FindCard(newCardId)!;
                foreach (CardUpgradeModifier upgrade in equipment.EffectUpgrades.Concat(card.Permanent.Upgrades).Concat(card.Temporary.Upgrades))
                {
                    RoomCombatResult applied = RoomCombatModel.ApplyDirectUnitUpgrade(FindRoom()!, unitId, upgrade, false, "", null,
                        equipmentSourceCardId: newCardId, captureAllDispatches: true);
                    if (!Accept(applied)) return Fail(applied.UnsupportedReason!);
                }
                targetEquipmentStatuses.AddRange(equipment.EffectUpgrades.SelectMany(upgrade => upgrade.Statuses).Select(status => status.Id));
            }
            original = Get(sourceId);
            if (original == null || Get(unitId) == null) return Fail("Hero equipment copying removed a retained actor.");
            RoomCombatResult stats = CharacterCopyModel.CopyStats(FindRoom()!, unitId, original, Array.Empty<StatisticCount>(),
                recipe.Gear.SelectMany(gear => gear.EffectUpgrades).SelectMany(upgrade => upgrade.Statuses).Select(status => status.Id).Distinct().ToArray(), targetEquipmentStatuses);
            return Accept(stats) ? Result() : Fail(stats.UnsupportedReason!);

            CombatUnit? Get(int id) => state.Rooms.SelectMany(room => room.Units).FirstOrDefault(actor => actor.Id == id);
            RoomCombatState? FindRoom() => state.Rooms.FirstOrDefault(room => room.Units.Any(actor => actor.Id == unitId));
            bool Drain()
            {
                UnitCloneResult drained = UnitCloneModel.Drain(new UnitCloneResult(state, unitId, outcome, null,
                    Array.Empty<CombatEvent>(), Array.Empty<UnitCloneBoundary>(), pending));
                if (!drained.Supported) { error = drained.UnsupportedReason; return false; }
                state = drained.State!; pending.Clear(); events.AddRange(drained.Events); dispatched.AddRange(drained.Dispatched);
                if (drained.Outcome != RoomOutcome.Exchanged) outcome = drained.Outcome;
                return true;
            }
            bool Accept(RoomCombatResult result)
            {
                if (!result.Supported) return false;
                state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(room => room.RoomIndex == result.State!.RoomIndex ? result.State : room).ToArray(),
                    state.Movement, state.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                pending.AddRange(result.PendingCallbacks); events.AddRange(result.Events);
                dispatched.AddRange(result.Dispatches.Select(item => new UnitCloneCallback(item.ActorId, item.Kind, item.ParamInt, item.ParamInt2,
                    item.ParamString, item.TriggerCount, item.DyingId, item.OverrideTargetId, item.LastSpawnedOverrideUnitId)));
                if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                return true;
            }
            UnitCloneResult Result() => new UnitCloneResult(state, unitId, outcome, null, events, Array.Empty<UnitCloneBoundary>(), pending, dispatched);
            UnitCloneResult Fail(string reason) => new UnitCloneResult(null, unitId, RoomOutcome.Unsupported, reason, events, Array.Empty<UnitCloneBoundary>(), pending, dispatched);
        }
    }
}
