using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitRoomResourceProbe
    {
        internal static UnitRoomResourceRule[]? Rules(IEnumerable<RoomModifierData> definitions)
        {
            UnitRoomResourceRule[] rules = definitions.Where(data => UnitRoomResourceModel.Known(data.GetRoomStateModifierClassName()))
                .Select(data => new UnitRoomResourceRule(data.GetRoomStateModifierClassName(), data.GetParamInt(), data.DisallowedInDeploymentPhase)).ToArray();
            return rules.Length == 0 ? null : rules;
        }
    }
}
