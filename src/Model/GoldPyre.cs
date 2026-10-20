using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicPyreAttackRefreshResult
    {
        public CombatContext? Context { get; }
        public CombatUnit? Pyre { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal RelicPyreAttackRefreshResult(CombatContext? context, CombatUnit? pyre, string? reason = null)
        { Context = context; Pyre = pyre; UnsupportedReason = reason; }
    }

    public static partial class RelicResourceModel
    {
        public static RelicResourceResult AdjustPyreAttack(CombatContext source, int delta)
        {
            string? error = Ready(source);
            if (error != null) return new RelicResourceResult(null, 0, error);
            BattleResourceState resources = source.Resources!;
            int value = Math.Max(0, unchecked(resources.PyreModifiedBaseAttack + delta));
            return new RelicResourceResult(source.WithResources(resources.WithPyreModifiedBaseAttack(value)), value);
        }

        // The gold signal is synchronous. Recompute only the native raw attack
        // field; applied-upgrade and status accumulators remain independent.
        public static RelicPyreAttackRefreshResult RefreshPyreAttack(CombatContext source, CombatUnit? livePyre,
            bool placement = false)
        {
            string? error = RelicModel.Validate(source);
            if (error != null) return new RelicPyreAttackRefreshResult(null, null, error);
            if (source.Resources == null || livePyre == null)
                return new RelicPyreAttackRefreshResult(source, livePyre);
            error = Ready(source);
            if (error != null) return new RelicPyreAttackRefreshResult(null, null, error);
            if (!livePyre.IsPyre || livePyre.Modifiers == null)
                return new RelicPyreAttackRefreshResult(null, null, "Pyre refresh requires its live native raw attack and upgrade accumulator.");
            RelicResourceResult attack = GetPyreAttack(source, placement, notify: true);
            if (!attack.Supported) return new RelicPyreAttackRefreshResult(null, null, attack.UnsupportedReason);
            UnitModifiers old = livePyre.Modifiers;
            int raw = unchecked(attack.Value + old.AttackDamageAdded);
            var modifiers = new UnitModifiers(raw, old.AttackDamageAdded, old.DamageBuff, old.RawSize, old.EquipmentLimit,
                old.CanBeHealed, old.IsClone, old.Upgrades, old.HealthFromUpgrades, old.SpawnerMatchesDefinition,
                old.DamageBuffFromStatusEffects, old.MagebladeDamagePerMagicPower, old.StatusAttackModifiers, old.IsChampion, old.SelfAttackRules, old.RoomResources, old.RoomStatuses, old.RoomTriggerCounts, old.SpawnCopySubtype);
            var pyre = new CombatUnit(livePyre.Id, livePyre.AssetKey, livePyre.Team,
                Math.Max(0, unchecked(raw + old.DamageBuff)), livePyre.Health, livePyre.MaxHealth,
                livePyre.CanAttack, livePyre.IsPyre, livePyre.EndsBattleOnDeath, livePyre.Statuses, livePyre.Triggers,
                livePyre.SpawnerCardId, livePyre.Size, livePyre.StatusImmunities, livePyre.Subtypes, modifiers,
                livePyre.IsBoss, livePyre.LastAttackerId, livePyre.StatusRegistry, livePyre.EquipmentCards,
                livePyre.NextTriggerId, livePyre.Ability, livePyre.StatusDictionary, livePyre.AbilityRules,
                livePyre.HordeDefinition, livePyre.IsSpawning, livePyre.SacrificeCardId, livePyre.DeathState,
                livePyre.BumpRules, livePyre.LastFeederId, livePyre.MoonPhaseMask);
            CombatContext context = attack.Context!;
            EnchantmentWorld? world = context.Enchantments;
            if (world != null)
                context = context.WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room => new RoomCombatState(
                    room.RoomIndex, room.Deployment, room.Units.Select(unit => unit.Id == pyre.Id ? pyre : unit).ToArray(),
                    room.ExternalInteractions, null, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                    world.Movement, world.EnemySlotsPerRoom,
                    world.RetainedUnits, world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview,
                    world.TestRng, world.AutomaticLifecycle));
            return new RelicPyreAttackRefreshResult(context, pyre);
        }

        // Callers own a fresh local array for the active immutable operation.
        // Synchronize its other room immediately, before the next queued effect.
        internal static RelicPyreAttackRefreshResult RefreshPyreAttack(RoomCombatState[] rooms, CombatContext source,
            bool placement)
        {
            CombatUnit[] pyres = rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre && unit.DeathState?.IsDestroyed != true).ToArray();
            if (pyres.Length > 1) return new RelicPyreAttackRefreshResult(null, null, "A native train has only one live Pyre heart.");
            RelicPyreAttackRefreshResult result = RefreshPyreAttack(source, pyres.SingleOrDefault(), placement);
            if (!result.Supported || result.Pyre == null) return result;
            for (int index = 0; index < rooms.Length; index++)
                if (rooms[index].Units.Any(unit => unit.Id == result.Pyre.Id))
                {
                    RoomCombatState room = rooms[index];
                    rooms[index] = new RoomCombatState(room.RoomIndex, room.Deployment,
                        room.Units.Select(unit => unit.Id == result.Pyre.Id ? result.Pyre : unit).ToArray(),
                        room.ExternalInteractions, result.Context, room.Preview, room.ActiveCompanionBossIds);
                }
            return result;
        }

        public static TrainCombatResult RefreshPyreAttack(TrainCombatState source, bool placement = false)
        {
            if (source.Context == null) return GoldModel.Fail("Pyre refresh requires shared battle context.");
            if (source.Rooms.Any(room => room.ExternalInteractions.Count > 0))
                return GoldModel.Fail(string.Join("; ", source.Rooms.SelectMany(room => room.ExternalInteractions)));
            CombatUnit[] pyres = source.Rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre && unit.DeathState?.IsDestroyed != true).ToArray();
            if (pyres.Length > 1) return GoldModel.Fail("A native train has only one live Pyre heart.");
            RelicPyreAttackRefreshResult result = RefreshPyreAttack(source.Context, pyres.SingleOrDefault(), placement);
            if (!result.Supported) return GoldModel.Fail(result.UnsupportedReason!);
            RoomCombatState[] rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => result.Pyre?.Id == unit.Id ? result.Pyre! : unit).ToArray(),
                room.ExternalInteractions, result.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            return GoldModel.Success(EnchantmentWorldModel.Rebase(new TrainCombatState(rooms, source.Movement,
                source.EnemySlotsPerRoom, result.Context)));
        }

        // Save's permanent adjustment participates in attack queries, not the
        // cached raw field written by PyreRoomState.HandleGoldChanged.
        public static int AttackDamage(CombatUnit unit, CombatContext? context) => UnitAttackOverrideModel.Resolve(unit.Statuses, unit.MaxHealth) ?? Math.Max(0,
            unchecked(unit.AttackBeforeStatuses + unit.AttackFromStatuses +
                RelicStatusAttackModel.AdditionalAttack(unit.Modifiers?.StatusAttackModifiers, unit.Team, unit.Statuses) +
                UnitSelfAttackModel.Additional(unit, context) +
                (unit.IsPyre ? context?.Resources?.PyreModifiedBaseAttack ?? 0 : 0)));
    }

    public static class GoldModel
    {
        public static TrainCombatResult Adjust(TrainCombatState source, int amount, bool isReward = false, bool preview = false,
            int roundIncrement = 5)
        {
            if (source.Context == null) return Fail("Gold adjustment requires shared battle context.");
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return Fail(error);
            int adjusted;
            try { adjusted = GoldRewardModel.Adjust(amount, isReward, roundIncrement,
                source.Context.Resources?.GoldRewardMultiplier ?? 1f); }
            catch (OverflowException) { return Fail("Gold reward float conversion exceeds the modeled native integer range."); }
            // AdjustGold(0) never calls SetGold. A clipped, unchanged result from
            // a nonzero amount still dispatches the native gold signal.
            return adjusted == 0 ? Success(source) : Set(source, unchecked(source.Context.Gold + adjusted), preview);
        }

        public static TrainCombatResult Set(TrainCombatState source, int gold, bool preview = false)
        {
            if (source.Context == null) return Fail("Setting gold requires shared battle context.");
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return Fail(error);
            CombatContext context = source.Context.WithGold(Math.Max(0, gold));
            TrainCombatState changed = CardSpellModel.WithContext(source, context);
            if (preview) return Success(changed);
            bool placement = source.Rooms.Any(room => room.Deployment);
            return RelicResourceModel.RefreshPyreAttack(changed, placement);
        }

        internal static TrainCombatResult Success(TrainCombatState state) => new TrainCombatResult(state,
            RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        internal static TrainCombatResult Fail(string error) => new TrainCombatResult(null,
            RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
