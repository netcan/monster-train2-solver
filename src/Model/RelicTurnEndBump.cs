using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicTurnEndBumpModel
    {
        internal const string Effect = "RelicEffectBumpOnTurnEnd";
        internal const string GravityId = "adb3bfb7-2c54-4742-b55e-053fe3217737";
        internal const string RaceId = "f843a4a2-1e29-4710-a88a-94b30a651d8e";

        internal static bool IsOwner(string id) => id is GravityId or RaceId;
        internal static bool Present(CombatContext? context) => context?.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) == true;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            IsOwner(owner.DataId) && owner.DisallowedInPlacementPhase == true && owner.EffectTypes.Count == 1 &&
            rule.EffectIndex == 0 && rule.ParamInt == (owner.DataId == GravityId ? -1 : 1) && rule.Amount == rule.ParamInt &&
            rule.CanApplyInPreviewMode == false && rule.RequiredMutatorId == null && rule.Conditions.Count == 0
                ? null : "Missing unchanged native combat-end movement definition.";

        internal static TrainCombatResult Apply(TrainCombatState source, IReadOnlyList<RoomPlayRule>? roomRules,
            IReadOnlyCollection<int> removedFromDeck, List<RoomCombatResult> roomResults)
        {
            if (roomRules == null) return Fail("Combat-end movement requires captured room play rules.");
            var state = source;
            var rules = roomRules;
            foreach (CombatRelicState owner in source.Context!.Relics!)
            {
                CombatContext context = state.Context!;
                if (owner.DataId == RelicSpawnRetreatModel.DataId)
                {
                    state = CardSpellModel.WithContext(state, RelicSpawnRetreatModel.Reset(context));
                    continue;
                }
                if (owner.DataId is RelicEndCombatModel.CavendishId or RelicEndCombatModel.GrosMichelId)
                {
                    context = RelicEndCombatModel.Apply(context, out string? error, removedFromDeck, resetRetreat: false);
                    if (error != null) return Fail(error);
                    state = CardSpellModel.WithContext(state, context);
                    continue;
                }
                if (!owner.EffectTypes.Contains(Effect)) continue;

                var rule = owner.ResourceEffects!.Single();
                var outermost = state.Rooms.Count - 1;
                IEnumerable<int> floors = rule.Amount < 0 ? Enumerable.Range(1, outermost) : Enumerable.Range(0, outermost).Reverse();
                foreach (int floor in floors)
                {
                    var room = state.Rooms[floor];
                    var targets = room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 &&
                        !unit.IsPyre && unit.IsBoss != true && unit.BumpRules?.OuterBoss != true).Select(unit => unit.Id).ToArray();
                    if (targets.Length == 0) continue;
                    context = state.Context!;
                    if (context.QueryFrame?.SelectedRoomIndex is int selected)
                    {
                        context = ConduitModel.Select(context, selected, floor);
                        state = CardSpellModel.WithContext(state, context.WithQueryFrame(context.QueryFrame!.With(selectedRoomIndex: floor)));
                    }
                    BumpResult moved = BumpModel.Apply(state, targets, rule.Amount, rules);
                    if (!moved.Supported) return Fail(moved.UnsupportedReason!);
                    state = moved.State!;
                    rules = moved.RoomRules;
                    if (moved.Events.Count != 0)
                        roomResults.Add(new RoomCombatResult(state.Rooms[floor], moved.Outcome, 0, moved.Events.ToList(), retainedUnits: moved.RetainedUnits));
                    if (moved.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
                        return new(state, moved.Outcome, roomResults, roomRules: rules);
                }
            }
            return new(state, RoomOutcome.Cleared, roomResults, roomRules: rules);

            TrainCombatResult Fail(string reason) => new(null, RoomOutcome.Unsupported, roomResults, reason);
        }
    }
}
