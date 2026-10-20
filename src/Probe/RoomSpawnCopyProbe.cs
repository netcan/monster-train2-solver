using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomSpawnCopyProbe
    {
        internal static bool Known(RoomModifierData modifier) => modifier.GetRoomStateModifierClassName() == "RoomStateSpawnUnitOnUnitSpawnModifier";
        internal static string? Rule(IEnumerable<RoomModifierData> modifiers, List<string> interactions)
        {
            RoomModifierData[] matches = modifiers.Where(Known).ToArray();
            if (matches.Length == 0) return null;
            if (matches.Length != 1 || !matches[0].GetParamSubtype().IsNone || matches[0].DisallowedInDeploymentPhase)
                interactions.Add("Uncalibrated room birth copy subtype or deployment policy");
            return matches[0].GetParamSubtype().IsNone ? "" : matches[0].GetParamSubtype().Key;
        }
    }
}
