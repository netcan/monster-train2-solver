using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossTerminalProbe
    {
        internal sealed class Record
        {
            public int Turn { get; set; }
            public int SourceCardId { get; set; }
            public int Damage { get; set; }
            public int HealthBefore { get; set; }
            public bool NativeReturned { get; set; }
            public CombatContext Before { get; set; } = null!;
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static IEnumerator Wrap(IEnumerator native, CharacterState target, int damage, CardState source)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var record = new Record { Turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount(),
                SourceCardId = trace.CardId(source), Damage = damage, HealthBefore = target.GetHP(), Before = trace.CaptureContext() };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.NativeReturned = true; }
            finally { (native as IDisposable)?.Dispose(); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyDamage))]
        private static class DamagePatch
        {
            private static void Postfix(CharacterState __instance, int damage, CharacterState.ApplyDamageParams damageParams, ref IEnumerator __result)
            {
                if (TrueFinalBossTurnProbe.Enabled && FullBattleTrace.Active != null && __instance.IsPyreHeart() &&
                    damageParams.damageSourceCard != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode)
                    __result = Wrap(__result, __instance, damage, damageParams.damageSourceCard);
            }
        }
    }
}
