using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicFloorTransitionDamageRule
    {
        public int EffectIndex { get; }
        public string Trigger { get; }
        public int DamageAmount { get; }

        public RelicFloorTransitionDamageRule(int effectIndex, string trigger, int damageAmount)
        { EffectIndex = effectIndex; Trigger = trigger; DamageAmount = damageAmount; }
    }

    internal static class RelicFloorTransitionDamageModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.DamageEnemyOnAscend || item.type == RelicModel.DamageEnemyOnDescend)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.FloorTransitionDamageRules?.Count > 0 ? "Unexpected relic floor-transition damage rules." : null;
            if (relic.FloorTransitionDamageRules == null ||
                !indices.SequenceEqual(relic.FloorTransitionDamageRules.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic floor-transition damage definitions.";
            foreach (RelicFloorTransitionDamageRule rule in relic.FloorTransitionDamageRules)
            {
                string expected = relic.EffectTypes[rule.EffectIndex] == RelicModel.DamageEnemyOnAscend
                    ? "PostAscension" : "PostDescension";
                if (rule.Trigger != expected) return "Relic floor-transition damage trigger differs from its native effect.";
            }
            return null;
        }

        internal static bool RequiresDamage(RoomCombatState state, RoomCombatModel.QueuedCharacterTrigger queued) =>
            !queued.RelicDamageApplied && !state.Preview && state.RoomIndex == queued.RoomIndex &&
            (queued.Kind == "PostAscension" || queued.Kind == "PostDescension") &&
            state.Context?.Relics?.Any(relic => relic.FloorTransitionDamageRules?.Any(rule => rule.Trigger == queued.Kind) == true) == true;

        internal static bool TryDefer(RoomCombatState state, RoomCombatModel.QueuedCharacterTrigger queued,
            Action<RoomCombatModel.QueuedCharacterTrigger> enqueue, out RoomCombatResult result)
        {
            result = null!;
            if (!RequiresDamage(state, queued)) return false;
            string? error = RelicModel.Validate(state.Context?.Relics);
            if (error != null)
            {
                result = Fail(error);
                return true;
            }
            CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == queued.Unit.Id);
            if (actor == null || actor.DeathState?.IsDestroyed == true) return false;
            if (actor.Health <= 0)
            {
                result = Fail("Floor-transition relic damage requires a living captured character.");
                return true;
            }

            var matching = state.Context!.Relics!.SelectMany(relic =>
                relic.DisallowedInPlacementPhase == true && state.Deployment ? Array.Empty<RelicFloorTransitionDamageRule>() :
                relic.FloorTransitionDamageRules?.Where(rule => rule.Trigger == queued.Kind) ?? Array.Empty<RelicFloorTransitionDamageRule>()).ToArray();
            if (matching.Length == 0) return false;
            if (matching.Length > 1)
            {
                result = Fail("Multiple relic floor-transition damage effects require native per-effect queue boundaries.");
                return true;
            }

            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            // Native BalanceData lists both transition effect classes as piercing.
            RoomCombatResult damaged = RoomCombatModel.ApplyRelicDamage(state, actor.Id,
                Math.Abs(matching[0].DamageAmount), callbacks.Add, piercing: true);
            if (!damaged.Supported)
            {
                result = damaged;
                return true;
            }
            for (int index = 0; index < callbacks.Count; index++)
                callbacks[index] = callbacks[index].WithRelicDamageRemoval();
            callbacks.AddRange(damaged.PendingCallbacks.Select(callback => callback.WithRelicDamageRemoval()));
            foreach (RoomCombatModel.QueuedCharacterTrigger callback in callbacks) enqueue(callback);
            enqueue(queued.WithRelicDamageApplied());
            result = new RoomCombatResult(damaged.State, damaged.Outcome, damaged.Rounds, damaged.Events.ToList(),
                retainedUnits: damaged.RetainedUnits, dispatches: damaged.Dispatches);
            return true;
        }

        private static RoomCombatResult Fail(string error) =>
            new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
