using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicTurnPhaseStep
    {
        public int OwnerIndex { get; }
        public int EffectIndex { get; }
        public string Type { get; }
        public TrainCombatState Before { get; }
        public TrainCombatState After { get; }
        internal RelicTurnPhaseStep(int owner, int effect, string type, TrainCombatState before, TrainCombatState after)
        { OwnerIndex = owner; EffectIndex = effect; Type = type; Before = before; After = after; }
    }

    internal static class RelicTurnPhaseModel
    {
        internal static bool IsBeforeDraw(string type) => type == RelicResourceModel.Energy ||
            type == RelicDamagedFloorEnergyModel.Effect || RelicTurnEnergyModel.IsEffect(type) ||
            type == RelicSoloUnitUpgradeModel.Effect || type == RelicSingleFloorCapacityModel.Effect ||
            type == RelicStartTurnHealModel.Effect || type == RelicStartTurnStatusModel.Effect ||
            type == RelicPileStartTurnModel.Effect || type == RelicTurnDrawModel.FullHealth ||
            type == RelicRelentlessDrawModel.Effect || type == RelicFirstCardCostModel.Effect;

        internal static TrainCombatResult GeneralStart(TrainCombatState source)
        {
            var state = source;
            bool placement = source.Rooms.Any(room => room.Deployment);
            bool preview = source.Rooms.Any(room => room.Preview);
            foreach (var key in Keys(source, type => type == RelicSoloUnitUpgradeModel.FrontRoomEffect || type == RelicCardTypeSequenceModel.Effect))
            {
                if (placement && state.Context!.Relics![key.Owner].DisallowedInPlacementPhase == true) continue;
                if (preview && key.Type == RelicCardTypeSequenceModel.Effect) continue;
                if (key.Type == RelicSoloUnitUpgradeModel.FrontRoomEffect)
                {
                    var result = RelicSoloUnitUpgradeModel.Apply(state, key.Owner, key.Effect);
                    if (!result.Supported) return Fail(result.UnsupportedReason!);
                    state = result.State!;
                }
                else
                {
                    var result = RelicCardTypeSequenceModel.StartTurn(state.Context!, key.Owner);
                    if (!result.Supported) return Fail(result.UnsupportedReason!);
                    state = CardSpellModel.WithContext(state, result.Context);
                }
            }
            return Pass(state);
        }

        internal static TrainCombatResult BeforeDraw(TrainCombatState source, BattlePlayRules? definitions,
            ICollection<RelicTurnPhaseStep>? steps = null)
        {
            var state = source;
            foreach (var key in Keys(source, IsBeforeDraw))
            {
                var before = state; var context = state.Context!;
                int turn = context.QueryFrame?.Turn ?? -1;
                if (turn < 0) return Fail("Before-draw dispatch requires the current native turn.");
                bool placement = state.Rooms.Any(room => room.Deployment);
                bool preview = state.Rooms.Any(room => room.Preview);
                var owner = context.Relics![key.Owner];
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                if (preview && (key.Type == RelicSoloUnitUpgradeModel.Effect ||
                    key.Type == RelicStartTurnHealModel.Effect || key.Type == RelicFirstCardCostModel.Effect)) continue;
                int selected = context.QueryFrame?.SelectedRoomIndex ?? -1;
                string? error = null; RoomOutcome outcome = RoomOutcome.Exchanged;
                switch (key.Type)
                {
                    case RelicResourceModel.Energy:
                    case RelicDamagedFloorEnergyModel.Effect:
                    case RelicTurnEnergyModel.Reducing:
                    case RelicTurnEnergyModel.Occupied:
                    {
                        var result = RelicResourceModel.ApplyEnergyTiming(context, preview: preview, placement: placement,
                            train: state, onlyOwner: key.Owner, onlyEffect: key.Effect);
                        error = result.UnsupportedReason;
                        if (result.Supported) state = CardSpellModel.WithContext(state, result.Context!);
                        break;
                    }
                    case RelicSoloUnitUpgradeModel.Effect:
                    {
                        var result = RelicSoloUnitUpgradeModel.Apply(state, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) state = result.State!;
                        break;
                    }
                    case RelicSingleFloorCapacityModel.Effect:
                    {
                        var result = RelicSingleFloorCapacityModel.Apply(context, turn, preview, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) state = CardSpellModel.WithContext(state, result.Context!);
                        break;
                    }
                    case RelicStartTurnHealModel.Effect:
                    {
                        var result = RelicStartTurnHealModel.Apply(state, turn, selected, placement, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) state = result.State!;
                        break;
                    }
                    case RelicStartTurnStatusModel.Effect:
                    {
                        var result = RelicStartTurnStatusModel.Apply(state, definitions?.Rooms, turn, placement, preview, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) state = result.State!;
                        break;
                    }
                    case RelicPileStartTurnModel.Effect:
                    {
                        var result = RelicPileStartTurnModel.Apply(state, definitions, selected, placement, preview, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) { state = result.State!; outcome = result.Outcome; }
                        break;
                    }
                    case RelicTurnDrawModel.FullHealth:
                    case RelicRelentlessDrawModel.Effect:
                    {
                        int health = state.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
                        var result = RelicTurnDrawModel.BeforeDraw(context, health, placement, preview, state, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) state = CardSpellModel.WithContext(state, result.Context!);
                        break;
                    }
                    case RelicFirstCardCostModel.Effect:
                    {
                        var result = RelicFirstCardCostModel.StartTurnBeforeDraw(context, definitions, key.Owner, key.Effect);
                        error = result.UnsupportedReason; if (result.Supported) state = CardSpellModel.WithContext(state, result.Context);
                        break;
                    }
                }
                if (error != null) return Fail("Before-draw " + key.Type + ": " + error);
                steps?.Add(new RelicTurnPhaseStep(key.Owner, key.Effect, key.Type, before, state));
                if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated) return Pass(state, outcome);
            }
            return Pass(state);
        }

        private static IEnumerable<(int Owner, int Effect, string Type)> Keys(TrainCombatState state, Func<string, bool> included)
        {
            var owners = state.Context?.Relics ?? Array.Empty<CombatRelicState>();
            for (int owner = 0; owner < owners.Count; owner++)
            for (int effect = 0; effect < owners[owner].EffectTypes.Count; effect++)
                if (included(owners[owner].EffectTypes[effect])) yield return (owner, effect, owners[owner].EffectTypes[effect]);
        }
        private static TrainCombatResult Pass(TrainCombatState state, RoomOutcome outcome = RoomOutcome.Exchanged) => new(state, outcome, Array.Empty<RoomCombatResult>());
        private static TrainCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
