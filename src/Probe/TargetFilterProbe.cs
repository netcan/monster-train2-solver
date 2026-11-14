using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TargetFilterProbe
    {
        internal static readonly bool HasSelfHealthFilter = AccessTools.Method(typeof(TargetHelper), "CheckSelfTargetFiltered",
            new[] { typeof(CharacterState), typeof(CardEffectData.HealthFilter), typeof(bool) }) != null;
    }
}
