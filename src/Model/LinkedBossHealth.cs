using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class LinkedBossHealthResult
    {
        public TrainCombatState? State { get; }
        public IReadOnlyList<HealthChangedEvent> Events { get; }
        public LifeLinkDamageResult? DamageStage { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal LinkedBossHealthResult(TrainCombatState? state, IReadOnlyList<HealthChangedEvent> events,
            LifeLinkDamageResult? damageStage, string? unsupportedReason = null)
        { State = state; Events = Array.AsReadOnly(events.ToArray()); DamageStage = damageStage; UnsupportedReason = unsupportedReason; }
    }
    // Bridge calibrated live health updates to all physical train rooms. This
    // interface admits calibrated nonterminal damage/healing, while the TFB
    // controller, attacks and terminal cleanup remain separate boundaries.
    public static class LinkedBossHealthModel
    {
        internal static CombatContext? WithHealth(CombatContext source, int targetId, int health, out string? error)
        {
            LifeLinkContext? world = source.LifeLinks;
            error = world == null ? "Missing global linked-boss health." : LifeLinkModel.Validate(world);
            if (error != null) return null;
            LifeLinkActor? target = world!.Actors.SingleOrDefault(actor => actor.Id == targetId);
            if (target == null || health <= 0 || world.SharedPreviewHpChange != 0)
            { error = "Linked boss health requires a captured live target; preview and terminal updates remain guarded."; return null; }
            if (world.CalculatingPreview)
            {
                // Preview copies have independent HP. This bounded no-allied-unit
                // route damages Dominion only; outer-body preview damage needs
                // its shared damage accumulator and separate native evidence.
                if (target.OuterBoss || source.Enchantments?.Preview != true)
                { error = "Outer linked-body preview damage remains uncalibrated."; return null; }
                return RebaseContext(source, new LifeLinkContext(world.Actors.Select(actor => actor.Id == targetId
                    ? new LifeLinkActor(actor.Id, actor.LinkedId, Math.Min(health, 99999), Math.Min(health, 99999),
                        actor.Initialized, actor.HasLifeLink, actor.TrueFinalBoss, actor.OuterBoss) : actor).ToArray(), true, 0));
            }
            int owner = target.LinkedId == 0 ? target.Id : target.LinkedId;
            return RebaseContext(source, new LifeLinkContext(world.Actors.Select(actor =>
            {
                bool shared = (actor.LinkedId == 0 ? actor.Id : actor.LinkedId) == owner;
                int hp = shared ? Math.Min(health, 99999) : actor.Health;
                return new LifeLinkActor(actor.Id, actor.LinkedId, hp, hp, actor.Initialized, actor.HasLifeLink, actor.TrueFinalBoss, actor.OuterBoss);
            }).ToArray(), false, 0));
        }

        private static CombatContext RebaseContext(CombatContext source, LifeLinkContext world)
        {
            CombatContext context = source.WithLifeLinks(world);
            EnchantmentWorld? enchantments = context.Enchantments;
            if (enchantments == null) return context;
            var health = world.Actors.ToDictionary(actor => actor.Id, actor => actor.Health);
            CombatUnit Sync(CombatUnit unit) => health.TryGetValue(unit.Id, out int hp) ? unit.WithHealth(hp) : unit;
            return context.WithEnchantments(new EnchantmentWorld(enchantments.Rooms.Select(room =>
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Preview ? room.Units : room.Units.Select(Sync).ToArray(),
                    room.ExternalInteractions, null, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                enchantments.Movement, enchantments.EnemySlotsPerRoom, enchantments.RetainedUnits.Select(actor =>
                    new EnchantmentRetainedUnit(actor.Preview ? actor.Unit : Sync(actor.Unit), actor.RoomIndex, actor.Preview)).ToArray(),
                enchantments.EnchanterIds, enchantments.AllowUpdates, enchantments.Updating, enchantments.Preview,
                enchantments.TestRng, enchantments.AutomaticLifecycle));
        }

        public static LinkedBossHealthResult ApplyDamage(TrainCombatState source, int targetId, int damage, int sourceCardId = 0) => Combat(source, targetId, damage, false, sourceCardId);
        public static LinkedBossHealthResult ApplyHeal(TrainCombatState source, int targetId, int amount) => Combat(source, targetId, amount, true);
        private static LinkedBossHealthResult Combat(TrainCombatState source, int targetId, int amount, bool healing, int sourceCardId = 0)
        {
            RoomCombatState? room = source.Rooms.SingleOrDefault(item => item.Units.Any(unit => unit.Id == targetId));
            if (room == null || source.Context?.LifeLinks?.Actors.Any(actor => actor.Id == targetId) != true)
                return new LinkedBossHealthResult(null, Array.Empty<HealthChangedEvent>(), null, "Missing physical linked-boss target.");
            RoomCombatResult applied = healing ? RoomCombatModel.ApplyCardHeal(room, targetId, amount) : RoomCombatModel.ApplyCardDamage(room, targetId, amount, sourceCardId);
            if (!applied.Supported) return new LinkedBossHealthResult(null, Array.Empty<HealthChangedEvent>(), null, applied.UnsupportedReason);
            CombatContext context = applied.State!.Context!;
            var byId = context.LifeLinks!.Actors.ToDictionary(actor => actor.Id);
            var rooms = source.Rooms.Select(original =>
            {
                RoomCombatState changed = original.RoomIndex == room.RoomIndex ? applied.State : original;
                return new RoomCombatState(changed.RoomIndex, changed.Deployment, changed.Units.Select(unit =>
                    byId.TryGetValue(unit.Id, out LifeLinkActor? linked) ? unit.WithHealth(linked.Health) : unit).ToArray(),
                    changed.ExternalInteractions, context, changed.Preview, changed.ActiveCompanionBossIds);
            }).ToArray();
            int beforeHealth = room.Units.Single(unit => unit.Id == targetId).Health;
            int afterHealth = byId[targetId].Health;
            return new LinkedBossHealthResult(new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, context),
                beforeHealth == afterHealth ? Array.Empty<HealthChangedEvent>() : new[] { new HealthChangedEvent(targetId, afterHealth) }, null);
        }

        public static LinkedBossHealthResult Apply(TrainCombatState source, int targetId, string operation, int value)
        {
            LinkedBossHealthResult Reject(string reason) => new LinkedBossHealthResult(null, Array.Empty<HealthChangedEvent>(), null, reason);
            LifeLinkContext? world = source.Context?.LifeLinks;
            if (world == null) return Reject("Linked boss updates require the captured live global roster.");
            string? invalid = LifeLinkModel.Validate(world);
            if (invalid != null) return Reject(invalid);
            if (world.CalculatingPreview || world.SharedPreviewHpChange != 0 || world.Actors.Any(actor => actor.Health != actor.PreviewHealth))
                return Reject("Live train health updates cannot use UI preview health or its damage accumulator.");
            CombatUnit[] units = source.Rooms.SelectMany(room => room.Units).ToArray();
            if (source.Rooms.Any(room => room.Preview) || units.Select(unit => unit.Id).Distinct().Count() != units.Length ||
                world.Actors.Any(actor => units.Count(unit => unit.Id == actor.Id && unit.Health == actor.Health) != 1))
                return Reject("Linked boss updates require unique physical live units with matching captured health.");
            var health = new LinkedHealthState(world.Actors.Select(actor => new LinkedHealthActor(actor.Id, actor.LinkedId,
                actor.Health, units.Single(unit => unit.Id == actor.Id).MaxHealth, false)).ToArray());
            TrueFinalBossHealthResult mutation = TrueFinalBossHealthModel.Mutate(new TrueFinalBossHealthState(health, world), targetId, operation, value);
            if (!mutation.Supported) return Reject(mutation.UnsupportedReason!);
            var nextWorld = new LifeLinkContext(mutation.State!.LifeLink.Actors.Select(actor => new LifeLinkActor(actor.Id,
                actor.LinkedId, actor.Health, actor.Health, actor.Initialized, actor.HasLifeLink, actor.TrueFinalBoss, actor.OuterBoss)).ToArray(), false, 0);
            CombatContext context = RebaseContext(source.Context!, nextWorld);
            var byId = nextWorld.Actors.ToDictionary(actor => actor.Id);
            var rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => byId.TryGetValue(unit.Id, out LifeLinkActor? linked) ? unit.WithHealth(linked.Health) : unit).ToArray(),
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            return new LinkedBossHealthResult(new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, context),
                mutation.Events, mutation.DamageStage);
        }
    }
}
