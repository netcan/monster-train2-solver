using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitRoomResourceProbe
    {
        internal static int HealingCostDelta(RoomState room, CardPlayRule rule, CardInstanceState card)
        {
            if (AllGameManagers.Instance!.GetSaveManager().PreviewMode || !UnitRoomResourceModel.EligibleForHealingCost(rule, card)) return 0;
            var units = new List<CharacterState>();
            room.AddCharactersToList(units, Team.Type.Monsters);
            return units.Where(unit => unit.IsAlive).SelectMany(unit => unit.GetRoomStateModifiers<RoomStateHealCostModifier>())
                .Sum(modifier => modifier.GetRoomModifierData().GetParamInt());
        }
        internal static UnitRoomResourceRule[]? Rules(IEnumerable<RoomModifierData> definitions)
        {
            UnitRoomResourceRule[] rules = definitions.Where(data => UnitRoomResourceModel.Known(data.GetRoomStateModifierClassName()))
                .Select(data => new UnitRoomResourceRule(data.GetRoomStateModifierClassName(), data.GetParamInt(), data.DisallowedInDeploymentPhase)).ToArray();
            return rules.Length == 0 ? null : rules;
        }
    }
}
