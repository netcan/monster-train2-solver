using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitRoomStatusProbe
    {
        internal sealed class Query
        {
            public int UnitId { get; set; }
            public int NativeAttack { get; set; }
            public int BuffBonus { get; set; }
            public int ValorBonus { get; set; }
            public int SpikesBonus { get; set; }
        }
        internal sealed class Record
        {
            public RoomCombatState Room { get; set; } = null!;
            public Query[] Queries { get; set; } = Array.Empty<Query>();
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static UnitRoomStatusRule[]? Rules(IEnumerable<RoomModifierData> definitions)
        {
            UnitRoomStatusRule[] rules = definitions.Where(data => UnitRoomStatusModel.Known(data.GetRoomStateModifierClassName()))
                .Select(data => new UnitRoomStatusRule(data.GetRoomStateModifierClassName(), UnitRoomStatusModel.Status(data.GetRoomStateModifierClassName()), data.GetParamInt())).ToArray();
            return rules.Length == 0 ? null : rules;
        }
        internal static void Observe(RoomCombatState room, IReadOnlyList<CharacterState> characters, FullBattleTrace trace)
        {
            string mode = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
            if (room.Preview || room.Context == null || !mode.StartsWith("original-isolated-spells:", StringComparison.Ordinal) ||
                !new[] { "3cb0976c-7cf5-4470-bb5a-7eacfb404e9a", "1362aa8c-d7b4-49b9-a4f7-268f47de4aa6", "a5b05b40-45be-41ea-8c56-dd18f3feb1b4" }.Any(mode.Contains)) return;
            RoomState nativeRoom = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(room.RoomIndex);
            Records.Add(new Record { Room = room, Queries = characters.Where(unit => unit.IsAlive).Select(unit => new Query {
                UnitId = trace.UnitId(unit), NativeAttack = unit.GetAttackDamage(), BuffBonus = nativeRoom.GetRoomStateModifiedBuff(unit),
                ValorBonus = nativeRoom.GetRoomStateModifiedValorBonusPerStack(unit),
                SpikesBonus = nativeRoom.GetRoomStateModifiedDamage(unit, Damage.Type.Spikes, false, AllGameManagers.Instance!.GetCoreManagers()) }).ToArray() });
        }
    }
}
