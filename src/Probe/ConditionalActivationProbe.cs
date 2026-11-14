using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // In 2.3.1 the old front-position Deathwish became Vanguard. The new
    // Deathwish has a separate health predicate and a separate source set.
    internal static class ConditionalActivationProbe
    {
        internal static readonly bool HasVanguard = typeof(CharacterState).GetMethod("TryUpdateVanguard") != null;
        internal static string FrontKind => HasVanguard ? "OnVanguard" : "OnDeathwish";
        internal static string? Activation(string kind) => HasVanguard &&
            (kind == "OnDeathwish" || kind == "OnDeathwishLost") ? "HalfHealth" : null;

        internal static bool? Active(CharacterState unit, CharacterTriggerState trigger)
        {
            string kind = trigger.GetTrigger().ToString();
            string? field = kind == "OnVanguard" || kind == "OnVanguardLost" ? "vanguardSources" :
                kind == "OnDeathwish" || kind == "OnDeathwishLost" ? HasVanguard ? "deathwishSources" : "deathWishSources" : null;
            if (field == null) return null;
            object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            var members = primary.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(primary) as HashSet<CharacterTriggerState>;
            if (members == null) throw new NotSupportedException("Unknown native conditional trigger source set " + field + ": " +
                typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId);
            return members.Contains(trigger);
        }
    }
}
