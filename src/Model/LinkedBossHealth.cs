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
    // interface does not admit the TFB controller or full damage/heal callbacks.
    public static class LinkedBossHealthModel
    {
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
            CombatContext context = source.Context!.WithLifeLinks(nextWorld);
            var byId = nextWorld.Actors.ToDictionary(actor => actor.Id);
            var rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => byId.TryGetValue(unit.Id, out LifeLinkActor? linked) ? unit.WithHealth(linked.Health) : unit).ToArray(),
                room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            return new LinkedBossHealthResult(new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, context),
                mutation.Events, mutation.DamageStage);
        }
    }
}
