using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicAfterDrawSpawnModel
    {
        internal const string Effect = "RelicEffectSpawnMonsterOnTurnStart";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            if (owner.IsCovenant != false || !owner.DisallowedInPlacementPhase.HasValue ||
                effect.Amount != 0 || effect.CanApplyInPreviewMode || effect.RequiredMutatorId != null || effect.Conditions.Count != 0 ||
                effect.OpeningSpawn is not { Team: "Monsters" } birth || birth.Units.Count > 3)
                return "Unsupported captured after-draw unit birth rule.";
            if (birth.Units.Any(definition => string.IsNullOrEmpty(definition.CharacterId) ||
                definition.Unit.Team != CombatTeam.Player || definition.Unit.IsPyre || definition.Unit.IsBoss == true ||
                definition.Fallback != null || definition.HeroDefinition != null || definition.ExternalInteractions.Count != 0))
                return "Uncalibrated unit definition in after-draw birth rule.";
            return null;
        }

        internal static TrainCombatResult Apply(TrainCombatState source, CombatRelicState owner, RelicResourceEffect effect,
            BattlePlayRules? definitions)
        {
            string? error = Validate(owner, effect);
            if (error != null) return Unsupported(error);
            if (source.Rooms.Any(room => room.Preview) ||
                owner.DisallowedInPlacementPhase == true && source.Rooms.Any(room => room.Deployment) ||
                effect.OpeningSpawn!.Units.Count == 0)
                return new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());

            CombatContext? context = source.Context;
            CombatStatus? cardless = context?.StatusRules.SingleOrDefault(status => status.Id == "cardless");
            var floors = definitions?.Rooms.Where(room => room.RoomIndex is >= 0 and < 3 && !room.IsPyre)
                .OrderBy(room => room.RoomIndex).ToArray();
            if (context?.SpawnPoints == null || cardless == null || floors == null ||
                !floors.Select(room => room.RoomIndex).SequenceEqual(new[] { 0, 1, 2 }) ||
                floors.Any(rule => !source.Rooms.Any(room => room.RoomIndex == rule.RoomIndex) ||
                    rule.Enabled && context.SpawnPoints.SelectedGroup(rule.RoomIndex, CombatTeam.Player) == null))
                return Unsupported("After-draw birth requires the enabled floors, their physical groups and cardless metadata.");

            TrainCombatState state = source;
            var roomResults = new List<RoomCombatResult>();
            foreach (SummonUnitDefinition definition in effect.OpeningSpawn.Units)
            {
                var remaining = new List<RoomPlayRule>(floors);
                UnityRng rng = state.Context!.BattleRng;
                while (remaining.Count > 0)
                {
                    RngDraw floorDraw = rng.Range(0, remaining.Count);
                    rng = floorDraw.State;
                    RoomPlayRule floor = remaining[floorDraw.Value];
                    remaining.RemoveAt(floorDraw.Value);
                    if (!floor.Enabled) continue;

                    // The slot roll occurs before the native birth discovers a full floor.
                    RngDraw slotDraw = rng.Range(0, 100);
                    rng = slotDraw.State;
                    int position = state.Context!.SpawnPoints!.FirstEmpty(floor.RoomIndex, CombatTeam.Player, fromBack: slotDraw.Value < 50);
                    if (position < 0) continue;

                    RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == floor.RoomIndex)
                        .WithContext(state.Context.WithBattleRng(rng));
                    var unitRule = new CardPlayRule("", definition.Unit.AssetKey, 0, "SpawnMonster", "Room", definition.Unit,
                        definition.ExternalInteractions, cardType: "Monster");
                    UnitBirthResult born = UnitBirthModel.SpawnUnselected(room, unitRule, 0, position, true, cardless.WithStacks(1));
                    if (!born.Supported) return Unsupported(born.Result.UnsupportedReason!);
                    state = CardSpellModel.ReplaceRoom(state, born.Result.State!);
                    roomResults.Add(born.Result);
                    if (born.Result.Outcome != RoomOutcome.Exchanged)
                        return new TrainCombatResult(state, born.Result.Outcome, roomResults);
                    // Any further random calls made by the birth belong to the next unit's input.
                    rng = state.Context!.BattleRng;
                    break;
                }
                if (!state.Context!.BattleRng.Equals(rng))
                    state = CardSpellModel.WithContext(state, state.Context.WithBattleRng(rng));
            }
            return new TrainCombatResult(state, RoomOutcome.Exchanged, roomResults);
        }

        private static TrainCombatResult Unsupported(string reason) =>
            new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), reason);
    }
}
