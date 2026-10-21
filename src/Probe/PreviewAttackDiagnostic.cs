using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PreviewAttackDiagnostic
    {
        internal static readonly List<object> Records = new List<object>();
        private static object Units(int roomIndex)
        {
            var units = new List<CharacterState>();
            AllGameManagers.Instance!.GetRoomManager().GetRoom(roomIndex)
                .AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
            return units.Select(unit => new { unit = unit.GetSourceCharacterData().name, hp = unit.GetHP(),
                attack = unit.GetAttackDamage(), undying = unit.GetStatusEffectStacks("undying") }).ToArray();
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, int roomIndex)
        {
            object before = Units(roomIndex);
            string name = actor.GetSourceCharacterData().name;
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                Records.Add(new { turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(), roomIndex,
                    actor = name, before, after = Units(roomIndex),
                    lastDamage = (int)AccessTools.Field(typeof(CardStatistics), "lastAttackDamageDealt")
                        .GetValue(AllGameManagers.Instance.GetCardStatistics()) });
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RunUnitTurn))]
        private static class Patch
        {
            private static void Postfix(CharacterState attackerState, int roomIndex, ref IEnumerator __result)
            {
                var managers = AllGameManagers.Instance;
                if (Environment.GetEnvironmentVariable("MT2_PROBE_PREVIEW_ATTACK_TRACE") == "1" &&
                    FullBattleTrace.Active != null && managers!.GetSaveManager().PreviewMode &&
                    managers.GetCombatManager().GetTurnCount() >= 7)
                    __result = Observe(__result, attackerState, roomIndex);
            }
        }
    }
}
