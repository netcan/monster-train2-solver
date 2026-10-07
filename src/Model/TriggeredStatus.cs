using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TriggeredStatusScaling
    {
        public string? ActorStatus { get; }
        public bool MissingHealth { get; }
        public bool MagicPower { get; }
        public bool IgnoreDualism { get; }
        public string Subtype { get; }
        public TriggeredStatusScaling(string? actorStatus = null, bool missingHealth = false, bool magicPower = false,
            bool ignoreDualism = false, string subtype = "")
        { ActorStatus = actorStatus; MissingHealth = missingHealth; MagicPower = magicPower; IgnoreDualism = ignoreDualism; Subtype = subtype; }
    }

    internal static class TriggeredStatusModel
    {
        internal static string? Validate(CombatEffect effect)
        {
            CardActionEffect action = effect.Action!;
            if (action.Statuses.Count == 0) return "Triggered status requires a nonempty status pool.";
            if (action.Statuses.Any(status => !RoomCombatModel.KnowsStatus(status.Id))) return "Unmodeled triggered status definition.";
            if (action.Tests?.StrictTargets == true || action.Target == "DropTargetCharacter")
            {
                if (action.Statuses.Any(status => status.Stackable == null)) return "Strict status legality requires stackability definitions.";
                if (action.Statuses.Count > 1 && action.Statuses.Any(status => status.Stackable == false))
                    return "Random nonstackable legality requires the native BattleTest stream.";
                if (CardTargetModel.IsRandom(action.Target) && action.Statuses.Any(status => status.Stackable == false))
                    return "Random nonstackable targets require the native BattleTest stream.";
            }
            return null;
        }

        internal static bool Test(RoomCombatState room, CombatEffect effect, IReadOnlyList<int> targets, out string? error, CombatStatus? testedStatus = null)
        {
            error = null; CardActionEffect action = effect.Action!;
            // Stackable pools share legality, so their test-stream selection cannot change gameplay.
            if (action.Tests?.StrictTargets != true && action.Target != "DropTargetCharacter") return true;
            if (targets.Count == 0) return false;
            CombatStatus status = testedStatus ?? action.Statuses[0];
            if (status.Stackable == true) return true;
            foreach (int id in targets)
            {
                CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == id);
                if (target == null || target.Status(status.Id)?.Stacks > 0) continue;
                string subtype = effect.StatusScaling?.Subtype ?? "";
                if (subtype.Length > 0 && !target.Subtypes.Contains(subtype)) continue;
                if (action.Filters?.IgnoreBosses == true)
                {
                    if (!target.IsBoss.HasValue) { error = "Status legality requires captured boss state."; return false; }
                    if (target.IsBoss.Value) continue;
                }
                return true;
            }
            return false;
        }

        // The engine supplies a fixed collection and retains dying actor/target snapshots.
        // Native TestEffect selects on BattleTest; only actual application advances Battle here.
        internal static RoomCombatResult Apply(RoomCombatState source, int actorId, CombatEffect effect, IReadOnlyList<int> targetIds,
            int? sourceCardId = null)
        {
            string? error = Validate(effect);
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == actorId);
            if (error != null || actor == null || source.Context == null || targetIds.Any(id => source.Units.All(unit => unit.Id != id)))
                return Unsupported(error ?? "Triggered status requires captured actor, targets and battle context.");
            CardActionEffect action = effect.Action!;
            if (source.Preview && source.Context.IsolatedBattlePreview != true &&
                (action.Range != null || action.Value != 0 || action.Statuses.Count > 1 || CardTargetModel.IsRandom(action.Target)))
                return Unsupported("Randomized status previews require the explicit isolated preview RNG protocol.");
            CombatContext context = source.Context;
            CombatStatus status = action.Statuses[0];
            if (action.Statuses.Count > 1)
            {
                RngDraw choice = context.BattleRng.Range(0, action.Statuses.Count);
                context = context.WithBattleRng(choice.State); status = action.Statuses[choice.Value];
            }
            TriggeredStatusScaling? scaling = effect.StatusScaling;
            CombatUnit? first = targetIds.Count == 0 ? null : source.Units.First(unit => unit.Id == targetIds[0]);
            bool multiplied = scaling?.ActorStatus != null || first != null && (scaling?.MissingHealth == true || scaling?.MagicPower == true);
            if (multiplied)
            {
                int multiplier = scaling?.ActorStatus == null ? 0 : actor.Status(scaling.ActorStatus)?.Stacks ?? 0;
                if (first != null && scaling?.MissingHealth == true) multiplier = unchecked(multiplier + Math.Max(0, first.MaxHealth - first.Health));
                if (first != null && scaling?.MagicPower == true)
                {
                    RoomMagicPower[]? powers = context.MagicPower?.Where(room => room.RoomIndex == source.RoomIndex && room.Team == first.Team).ToArray();
                    if (powers?.Length != 1) return Unsupported("Status scaling requires one captured room/team magic power value.");
                    multiplier = unchecked(multiplier + powers[0].Value);
                }
                status = status.WithStacks(unchecked(status.Stacks * multiplier));
            }
            int chance = action.Value;
            if (action.Range != null)
            {
                RngDraw draw = action.Range.Sample(context.BattleRng); context = context.WithBattleRng(draw.State); chance = draw.Value;
            }
            RoomCombatState state = WithContext(source, context); var events = new List<CombatEvent>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (int id in targetIds.Reverse())
            {
                if (chance != 0)
                {
                    RngDraw draw = state.Context!.BattleRng.Range(0, 100);
                    state = WithContext(state, state.Context.WithBattleRng(draw.State));
                    if (draw.Value >= chance) continue;
                }
                CombatUnit before = state.Units.First(unit => unit.Id == id);
                RoomCombatResult added = StatusApplicationModel.ApplyRetained(state, id, status, sourceCardId ?? actor.SpawnerCardId,
                    overrideImmunity: action.Target == "Pyre");
                if (!added.Supported) return added;
                callbacks.AddRange(added.PendingCallbacks);
                state = added.State!;
                CombatUnit changed = state.Units.First(unit => unit.Id == id);
                // Captures and target filters expose only positive native status counts.
                changed = CardSpellModel.Copy(changed, changed.Health, changed.Statuses.Where(item => item.Stacks > 0).ToArray());
                state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units.Select(unit => unit.Id == id ? changed : unit).ToArray(),
                    state.ExternalInteractions, state.Context, state.Preview);
                events.Add(new CombatEvent(0, "TriggeredStatus:" + status.Id, actorId, id,
                    (changed.Status(status.Id)?.Stacks ?? 0) - (before.Status(status.Id)?.Stacks ?? 0)));
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events, pendingCallbacks: callbacks);
        }
        private static RoomCombatState WithContext(RoomCombatState source, CombatContext context) =>
            new RoomCombatState(source.RoomIndex, source.Deployment, source.Units, source.ExternalInteractions, context, source.Preview);
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
