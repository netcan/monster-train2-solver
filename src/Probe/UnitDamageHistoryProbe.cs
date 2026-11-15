using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitDamageHistoryProbe
    {
        private static bool Required(IEnumerable<CombatTrigger> triggers) => triggers.Any(trigger =>
            trigger.Effects.Any(effect => effect.Type == FriendlyDamageStatusModel.NativeEffect));
        internal static UnitDamageHistory? Initial(IEnumerable<CombatTrigger> triggers) =>
            Required(triggers) ? new UnitDamageHistory() : null;
        internal static UnitDamageHistory? Capture(CharacterState unit, IEnumerable<CombatTrigger> triggers) =>
            Required(triggers) ? new UnitDamageHistory(unit.GetLastDamageTaken(), unit.GetLastDamageTakenBeforeArmor(),
                (int)AccessTools.Field(typeof(CharacterState), "lastArmorBlockedOnHit").GetValue(unit),
                (int)AccessTools.Field(typeof(CharacterState), "lastLethalDamagePreventedOnHit").GetValue(unit)) : null;
    }
}
