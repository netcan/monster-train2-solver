using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicOpeningSpawnRule
    {
        public string Team { get; }
        public IReadOnlyList<SummonUnitDefinition> Units { get; }
        public RelicOpeningSpawnRule(string team, IReadOnlyList<SummonUnitDefinition> units)
        { Team = team; Units = Array.AsReadOnly(units.ToArray()); }
    }
    internal static class RelicOpeningSpawnModel
    {
        internal const string Effect = "RelicEffectSpawnUnitStartOfCombat";
        internal const string DataId = "80a1b361-ee3a-43ae-86bd-da1ddac33a0a";
        internal const string AssetKey = "SpawnRandomMorsels";
        internal const string MinerId = "854f3636-2b50-45eb-80af-cef27a99d8d6";
        internal static bool Test(int roomIndex, int roomCount) => roomIndex >= 0 && roomIndex < roomCount;
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == true && owner.EffectTypes.SequenceEqual(Enumerable.Repeat(Effect, 3)) &&
                effect.EffectIndex >= 0 && effect.EffectIndex < 3 && effect.ParamInt == 2 - effect.EffectIndex &&
                effect.ParamInt2 == 0 && effect.Amount == 0 && !effect.CanApplyInPreviewMode && effect.Conditions.Count == 0 &&
                effect.RequiredMutatorId == null && effect.OpeningSpawn?.Team == "Monsters" && effect.OpeningSpawn.Units.Count == 1 &&
                effect.OpeningSpawn.Units[0].CharacterId == MinerId && effect.OpeningSpawn.Units[0].Unit.AssetKey == "MorselMorselMiner" &&
                effect.OpeningSpawn.Units[0].Fallback == null && effect.OpeningSpawn.Units[0].ExternalInteractions.Count == 0
                ? null : "Missing or changed original start-of-combat Morsel spawn definition.";
        }
        internal static TrainCombatResult Apply(TrainCombatState source, string ownerId, int effectIndex)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == ownerId);
            RelicResourceEffect? effect = owner?.ResourceEffects?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            string? error = effect == null ? "Missing opening-spawn owner/effect." : Validate(owner!, effect);
            if (error != null) return Fail(error);
            if (!Test(effect!.ParamInt, source.Rooms.Count)) return Match(source);
            TrainCombatState state = source;
            if (state.Context!.QueryFrame?.SelectedRoomIndex != null)
                state = CardSpellModel.WithContext(state, state.Context.WithQueryFrame(state.Context.QueryFrame.With(selectedRoomIndex: effect.ParamInt)));
            var results = new List<RoomCombatResult>();
            foreach (SummonUnitDefinition definition in effect.OpeningSpawn!.Units)
            {
                RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == effect.ParamInt);
                CombatStatus? cardless = state.Context!.StatusRules.SingleOrDefault(item => item.Id == "cardless");
                int position = state.Context.SpawnPoints?.FirstEmpty(room.RoomIndex, CombatTeam.Player) ?? -1;
                if (cardless == null || position < 0) return Fail("Opening spawn requires native cardless metadata and an available physical slot.");
                var rule = new CardPlayRule("", definition.Unit.AssetKey, 0, "SpawnMonster", "Room", definition.Unit,
                    definition.ExternalInteractions, cardType: "Monster");
                UnitBirthResult born = UnitBirthModel.SpawnFront(room, rule, 0, position, true, cardless.WithStacks(1));
                if (!born.Supported) return Fail(born.Result.UnsupportedReason!);
                state = CardSpellModel.WithContext(new TrainCombatState(state.Rooms.Select(item => item.RoomIndex == room.RoomIndex ? born.Result.State! : item).ToArray(),
                    state.Movement, state.EnemySlotsPerRoom, born.Result.State!.Context), born.Result.State.Context!);
                results.Add(born.Result);
            }
            return new(state, RoomOutcome.Exchanged, results);
        }
        private static TrainCombatResult Match(TrainCombatState state) => new(state, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        private static TrainCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
