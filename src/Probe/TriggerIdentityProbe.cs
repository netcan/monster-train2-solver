using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Observe allocation, rather than the first subsequent snapshot: a trigger may
    // be added and removed between snapshots while its callbacks retain effects.
    internal static class TriggerIdentityProbe
    {
        private static void Register(CharacterState unit)
        {
            if (FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                FullBattleTrace.Active.TriggerStates(unit);
        }
        [HarmonyPatch(typeof(CharacterState), "AddNewCharacterTriggerState")]
        private static class UpgradePatch
        {
            private static void Postfix(CharacterState __instance) => Register(__instance);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddTrigger))]
        private static class AddPatch
        {
            private static void Postfix(CharacterState __instance) => Register(__instance);
        }
    }
}
