using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitSelfAttackProbe
    {
        internal sealed class AttackQuery
        {
            public int UnitId { get; set; }
            public int Attack { get; set; }
        }
        internal sealed class Record
        {
            public RoomCombatState Room { get; set; } = null!;
            public AttackQuery[] Attacks { get; set; } = Array.Empty<AttackQuery>();
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool KnownUpgrade(IEnumerable<RoomModifierData> definitions)
        {
            var values = definitions.ToArray();
            return values.Length == 1 && values[0].GetRoomStateModifierClassName() == UnitSelfAttackModel.MissingHealth &&
                values[0].GetParamInt() == 1 && !values[0].DisallowedInDeploymentPhase;
        }
        internal static void Observe(RoomCombatState room, IReadOnlyList<CharacterState> characters, FullBattleTrace trace)
        {
            if (room.Context == null || room.Preview) return;
            int[] ids = room.Units.Where(unit => unit.Modifiers?.SelfAttackRules?.Count > 0).Select(unit => unit.Id).ToArray();
            if (ids.Length == 0) return;
            Records.Add(new Record { Room = room, Attacks = characters.Where(character => ids.Contains(trace.UnitId(character)))
                .Select(character => new AttackQuery { UnitId = trace.UnitId(character), Attack = character.GetAttackDamage() }).ToArray() });
        }
        internal static UnitSelfAttackRule[]? Rules(IEnumerable<RoomModifierData> definitions)
        {
            UnitSelfAttackRule[] rules = definitions.Where(definition => UnitSelfAttackModel.Known(definition.GetRoomStateModifierClassName()))
                .Select(definition => new UnitSelfAttackRule(definition.GetRoomStateModifierClassName(), definition.GetParamInt(),
                    definition.GetRoomStateModifierClassName() == UnitSelfAttackModel.MissingHealth ? 1 : Math.Max(1, definition.GetParamInt2()),
                    definition.GetRoomStateModifierClassName() == UnitSelfAttackModel.MissingHealth ? "" :
                        (definition.GetParamStatusEffects() ?? Array.Empty<StatusEffectStackData>()).FirstOrDefault()?.statusId ?? ""))
                .ToArray();
            return rules.Length == 0 ? null : rules;
        }
    }
}
