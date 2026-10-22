using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // The calibrated five-body control sacrifices Savagery during destruction.
    // Its group disappears before the outer bodies can enter Pyre combat.
    internal static class TrueFinalBossRelentlessPhaseModel
    {
        internal static OuterBossPhaseResult Apply(OuterBossPhaseState source, bool preCombat)
        {
            OuterBossPhaseResult Fail(string reason) => new OuterBossPhaseResult(null, reason);
            BattleTurnState battle = source.Battle; OuterBossProgress boss = source.Boss;
            TrainCombatState train = battle.Spawn.Train;
            if (battle.PlayRules == null || train.Context?.SpawnPoints == null ||
                !train.Rooms.All(TrueFinalBossCombatModel.Supports) ||
                train.Context.Relics?.Any(relic => relic.EffectTypes.Count > 0 || relic.IsCovenant == true) == true ||
                train.Rooms.SelectMany(room => room.Units).Any(unit => !unit.IsPyre &&
                    (!train.Context.LifeLinks!.Actors.Any(actor => actor.Id == unit.Id) || unit.EquipmentCards.Count > 0 ||
                        unit.Modifiers?.Upgrades.Count > 0 || unit.Triggers.Any(trigger => trigger.Kind is not ("OnSpawn" or "PostCombat" or "OnTrainRoomLoop")) ||
                        unit.Statuses.Any(status => status.Id is not ("buff" or "immobile" or "lifelink" or "pyrebound" or "spell shield" or "pyrebane" or "trample" or "relentless")))) ||
                battle.PlayRules.Rooms.Any(room => !room.Enabled) ||
                train.Rooms[2].Units.Single(unit => unit.Id == boss.UnitId).Status("relentless")?.Stacks != 1)
                return Fail("Hellforged relentless phases require the calibrated five bodies and Pyre without other units or disabled floors.");
            BossActionControllerProgress controller = boss.Controller;
            if (preCombat)
            {
                BossActionControllerResult next = BossActionControllerModel.Apply(controller, "DecideNext", true);
                if (!next.Supported || next.State!.NextAction != null) return Fail(next.UnsupportedReason ?? "Unexpected Hellforged action.");
                controller = next.State;
            }
            var progress = new OuterBossProgress(boss.UnitId, boss.AssetKey, boss.Phase, boss.ValidAttackPhases,
                boss.BaseCanAttack, boss.Room, boss.MovementSpeed, boss.VisitedRooms, controller, boss.Actions, boss.DestroyActions);
            if (preCombat || !source.TrueFinalBoss!.WaitedOnRelentless)
                return new OuterBossPhaseResult(Rebuild(source, train, progress,
                    new TrueFinalBossPhaseFlags(true, true, 0), battle.PlayRules, battle.SelectedRoom));

            int savageryRoom = Array.FindIndex(train.Rooms.ToArray(), room => room.Units.Any(unit => unit.AssetKey == "Boss_Level9_Savagery_V2"));
            if (savageryRoom != 2 || battle.Spawn.Looping || battle.Spawn.Phase < battle.Spawn.Waves.Count)
                return Fail("Hellforged destruction requires the calibrated exhausted queue and top-floor Savagery sacrifice.");
            CombatContext context = train.Context;
            foreach (CombatUnit unit in train.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre))
            {
                RoomCombatState floor = train.Rooms.Single(room => room.Units.Any(actor => actor.Id == unit.Id));
                BattleSpawnPointResult removed = BattleSpawnPointModel.Apply(context.SpawnPoints!, floor, "Set", CombatTeam.Enemy, unit.Id);
                if (!removed.Supported) return Fail(removed.Error!);
                context = context.WithSpawnPoints(removed.State!);
            }
            RoomCombatState[] rooms = train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => unit.IsPyre).ToArray(), room.ExternalInteractions, null, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            EnchantmentWorld oldWorld = context.Enchantments!;
            var world = new EnchantmentWorld(rooms, Array.Empty<EnemyMovement>(), train.EnemySlotsPerRoom,
                Array.Empty<EnchantmentRetainedUnit>(), Array.Empty<int>(), oldWorld.AllowUpdates, oldWorld.Updating,
                oldWorld.Preview, oldWorld.TestRng, oldWorld.AutomaticLifecycle);
            context = new CombatContext(new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(),
                Array.Empty<CardToken>(), context.Cards.Rng, context.Cards.DrawModifier, context.Cards.ExternalInteractions,
                context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn),
                context.BattleRng, context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules,
                context.Statistics?.ClearHandPlayHistory(), context.CardInstances == null ? null : Array.Empty<CardInstanceState>(),
                context.CardRegistry, true, context.NextAddedTemporaryUpgrades, context.OtherPiles?.Select(CardPileModel.Clear).ToArray(),
                context.QueryFrame?.With(runningCombat: false, selectedRoomIndex: savageryRoom), true, context.MagicPower,
                context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache,
                context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId,
                context.NextUnitId, context.SpawnPoints, context.SummonCatalog, world, context.PurifyBlockedTriggers,
                context.Relics, context.TriggerCounts, context.Resources, context.MixState, context.ConduitRetained);
            train = CardSpellModel.WithContext(new TrainCombatState(rooms, Array.Empty<EnemyMovement>(), train.EnemySlotsPerRoom, context), context);
            train = TrainCombatModel.CompleteFrameRemovals(train);
            BossActionGroupProgress destroy = controller.DestroyGroup;
            controller = new BossActionControllerProgress(controller.Groups, new BossActionGroupProgress(destroy.Repeats,
                destroy.ActionIndex, destroy.CurrentAction, new[] { false }, destroy.AlwaysValid), controller.GroupIndex,
                controller.CurrentGroup, controller.InitialAction);
            var deadBoss = new OuterBossProgress(boss.UnitId, boss.AssetKey, boss.Phase, boss.ValidAttackPhases,
                boss.BaseCanAttack, -1, boss.MovementSpeed, boss.VisitedRooms, controller, boss.Actions, boss.DestroyActions);
            return new OuterBossPhaseResult(Rebuild(source, train, deadBoss, source.TrueFinalBoss,
                battle.PlayRules.WithRooms(battle.PlayRules.Rooms.Select(room => room.IsPyre ? room : room.WithEnabled(false)).ToArray()),
                savageryRoom, terminal: true), outcome: RoomOutcome.BattleWon);
        }

        internal static OuterBossPhaseState Rebuild(OuterBossPhaseState source, TrainCombatState train, OuterBossProgress boss,
            TrueFinalBossPhaseFlags flags, BattlePlayRules rules, int selectedRoom, bool terminal = false, int? spawnPhase = null, bool? looping = null)
        {
            BattleTurnState battle = source.Battle; EnemySpawnState old = battle.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, spawnPhase ?? old.Phase, looping ?? old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds,
                terminal ? null : boss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, flags);
            return new OuterBossPhaseState(new BattleTurnState(spawn, battle.Energy, battle.EnergyPerTurn, battle.DrawPerTurn,
                battle.ForgePoints, battle.DragonsHoard, battle.MoonPhase, battle.RngStreams,
                train.Context?.OtherPiles ?? battle.OtherPiles, battle.ExternalInteractions, rules, battle.BattlePreviewEnabled,
                battle.UiRngIsolated, battle.CanonicalDecisionReferences, battle.CanonicalPhysicalReferences, selectedRoom,
                battle.MoonPhaseTrackingActive), boss, flags);
        }
    }
}
