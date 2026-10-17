using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicStartTurnHealResult
    {
        internal TrainCombatState? State { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => State != null;
        internal RelicStartTurnHealResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    internal static class RelicStartTurnHealModel
    {
        internal const string Effect = "RelicEffectHealStartOfTurn";
        internal const string ChampionSubtype = "SubtypesData_Champion_83f21cbe-9d9b-4566-a2c3-ca559ab8ff34";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            RelicStartTurnHealRule[] rules = (relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).Select(rule => rule.StartTurnHeal!).ToArray();
            if (indices.Length == 0)
                return (relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>()).Any(rule => rule.StartTurnHeal != null)
                    ? "Unexpected relic start-turn healing definition." : null;
            RelicStartTurnHealRule?[] indexed = (relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).Select(rule => rule.StartTurnHeal).ToArray();
            if (!indices.SequenceEqual((relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).Select(rule => rule.EffectIndex)) ||
                rules.Length != indices.Length || indexed.Any(rule => rule == null))
                return "Missing ordered native relic start-turn healing definitions.";
            foreach (RelicStartTurnHealRule rule in rules)
            {
                if (rule.MaxTurns < 0 || rule.Behavior != 0 || float.IsNaN(rule.HealingAmount) ||
                    float.IsInfinity(rule.HealingAmount) || rule.SourceTeam != "Monsters" ||
                    rule.TargetMode != "Tower" || rule.RequiredSubtype != ChampionSubtype ||
                    rule.ConditionCount != 0 || relic.DisallowedInPlacementPhase != true)
                    return "Unmodeled native champion start-turn healing rule.";
            }
            return null;
        }

        internal static RelicStartTurnHealResult Apply(TrainCombatState source, int turn, int selectedRoom, bool placement = false)
        {
            if (source.Context?.Relics?.Any(relic => relic.EffectTypes.Contains(Effect)) != true)
                return new RelicStartTurnHealResult(source);
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return Fail(error);
            if (placement) return new RelicStartTurnHealResult(source);

            TrainCombatState state = source;
            foreach (CombatRelicState relic in state.Context!.Relics!)
            {
                if (!relic.EffectTypes.Contains(Effect)) continue;
                error = Validate(relic);
                if (error != null) return Fail(error);
                foreach (RelicStartTurnHealRule rule in relic.ResourceEffects!
                    .Where(item => item.Type == Effect).Select(item => item.StartTurnHeal!))
                {
                    int callbackNumber = turn;
                    if (rule.MaxTurns == 0 || callbackNumber < 1 || callbackNumber > rule.MaxTurns) continue;
                    // Native TargetMode.Tower spans all player floors despite roomIndex being populated.
                    var targets = state.Rooms.SelectMany(room => room.Units
                        .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 &&
                            unit.Subtypes.Contains(rule.RequiredSubtype!))
                        .Select(unit => (room.RoomIndex, unit.Id))).ToArray();
                    foreach ((int roomIndex, int targetId) in targets)
                    {
                        RoomCombatState room = state.Rooms.First(item => item.RoomIndex == roomIndex);
                        CombatUnit target = room.Units.Single(unit => unit.Id == targetId);
                        int amount = target.MaxHealth - target.Health;
                        if (amount <= 0) continue;
                        RoomCombatResult healed = RoomCombatModel.ApplyCardHeal(room.WithContext(state.Context), targetId, amount);
                        if (!healed.Supported) return Fail(healed.UnsupportedReason!);
                        if (healed.Outcome != RoomOutcome.Exchanged)
                            return Fail("Champion start-turn healing triggered an unmodeled terminal room outcome.");
                        CombatContext context = healed.State!.Context ?? state.Context!;
                        RoomCombatState[] rooms = state.Rooms.Select(item => item.RoomIndex == roomIndex
                            ? healed.State!.WithContext(context) : item.WithContext(context)).ToArray();
                        state = new TrainCombatState(rooms, state.Movement, state.EnemySlotsPerRoom, context);
                    }
                }
            }
            return new RelicStartTurnHealResult(state);
        }

        private static RelicStartTurnHealResult Fail(string error) => new(null, error);
    }
}
