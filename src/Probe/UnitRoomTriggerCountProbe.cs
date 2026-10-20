using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitRoomTriggerCountProbe
    {
        internal sealed class Query
        {
            public int UnitId { get; set; }
            public int Count { get; set; }
        }
        internal sealed class Record
        {
            public RoomCombatState Room { get; set; } = null!;
            public Query[] Queries { get; set; } = Array.Empty<Query>();
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static bool observed;
        internal static bool Captured(AllGameManagers managers) => observed |= managers.GetCardManager()?.GetAllCards(new List<CardState>())
            .Any(card => card.GetSpawnCharacterData()?.GetRoomModifiersData().Any(data =>
                data.GetRoomStateModifierClassName() == UnitRoomTriggerCountModel.Type) == true) == true;
        internal static TriggerCountModifier[]? Rules(IEnumerable<RoomModifierData> definitions)
        {
            TriggerCountModifier[] rules = definitions.Where(data => data.GetRoomStateModifierClassName() == UnitRoomTriggerCountModel.Type)
                .Select(data => new TriggerCountModifier(data.GetParamTrigger().ToString(), data.GetParamInt())).ToArray();
            return rules.Length == 0 ? null : rules;
        }
        internal static int Amount(RoomState room, CharacterState target, CharacterTriggerData.Trigger trigger)
        {
            var units = new List<CharacterState>(); room.AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
            return units.Where(unit => unit.IsAlive && unit.GetTeamType() == target.GetTeamType())
                .SelectMany(unit => unit.GetRoomStateModifiers()).Where(modifier => modifier.GetType().Name == UnitRoomTriggerCountModel.Type)
                .Cast<RoomStateModifierBase>().Select(modifier => modifier.GetRoomModifierData())
                .Where(data => data.GetParamTrigger() == trigger).Sum(data => data.GetParamInt());
        }
        internal static void Observe(RoomCombatState room, IReadOnlyList<CharacterState> characters, FullBattleTrace trace)
        {
            if (room.Preview || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS")?.Contains("bc89dc02-3fe0-42c3-bcb0-62a0e3ae0c84") != true) return;
            Records.Add(new Record { Room = room, Queries = characters.Where(unit => unit.IsAlive).Select(unit => new Query {
                UnitId = trace.UnitId(unit), Count = unit.GetTriggerFireCount(CharacterTriggerData.Trigger.OnMoonPhaseShift) }).ToArray() });
        }
    }
}
