using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicProbe
    {
        internal static CombatRelicState[] Capture(AllGameManagers managers)
        {
            // Includes hero blessings, covenants, mutators, Pyre artifacts and souls,
            // in the exact order searched by GetRelicEffect<T>.
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics.Select(relic => new CombatRelicState(relic.GetRelicDataID(), relic.GetAssetName(),
                relic.GetEffects().Select(effect => effect.GetType().Name).ToArray())).ToArray();
        }
    }
}
