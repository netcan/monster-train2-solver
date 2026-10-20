using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomStatusStackProbe
    {
        internal static bool Known(RoomModifierData modifier) => modifier.GetRoomStateModifierClassName() == RoomStatusStackModel.Type;
        internal static RoomStatusStackRule[]? Rules(IEnumerable<RoomModifierData> modifiers, List<string> interactions)
        {
            RoomModifierData[] matches = modifiers.Where(Known).ToArray();
            if (matches.Length == 0) return null;
            if (matches.Length != 1 || matches[0].DisallowedInDeploymentPhase || matches[0].GetParamInt() != 3 ||
                matches[0].GetParamStatusEffects().Length != 1 || matches[0].GetParamStatusEffects()[0].statusId != "pyregel")
                interactions.Add("Uncalibrated room status stack modifier");
            return matches.SelectMany(modifier => modifier.GetParamStatusEffects().Select(status =>
                new RoomStatusStackRule(status.statusId, modifier.GetParamInt()))).ToArray();
        }
    }
}
