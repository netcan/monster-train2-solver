#if DEVELOPMENT_PROBE
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Read-only native witnesses for the explicitly requested component.
    internal static class TriggeredCorruptionProbe
    {
        internal sealed class Record
        {
            public int UnitId, Turn, Room, AttackBefore, AttackAfter, ArmorBefore, ArmorAfter, CorruptionBefore, CorruptionAfter;
            public bool Preview, Completed;
            public string AssetKey = "";
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static IEnumerator Observe(IEnumerator native, CharacterState actor)
        {
            var managers = AllGameManagers.Instance;
            var room = actor.GetCurrentRoom();
            var record = new Record { UnitId = FullBattleTrace.Active!.UnitId(actor),
                Turn = managers.GetCombatManager().GetTurnCount(), Room = room.GetRoomIndex(),
                Preview = managers.GetSaveManager().PreviewMode, AssetKey = actor.GetSourceCharacterData().GetAssetKey(),
                AttackBefore = actor.GetAttackDamage(), ArmorBefore = actor.GetStatusEffectStacks("armor"),
                CorruptionBefore = room.GetCurrentCorruption() };
            Records.Add(record);
            while (native.MoveNext()) yield return native.Current;
            record.AttackAfter = actor.GetAttackDamage(); record.ArmorAfter = actor.GetStatusEffectStacks("armor");
            record.CorruptionAfter = room.GetCurrentCorruption(); record.Completed = true;
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                bool fromRunningTriggerQueue, ref IEnumerator __result)
            {
                if (UnsupportedBranchObservation.ObserveTriggeredCorruption && FullBattleTrace.Active != null &&
                    Records.Count < 2048 && fromRunningTriggerQueue && trigger == CharacterTriggerData.Trigger.CorruptionAdded &&
                    __instance.GetSourceCharacterData().GetAssetKey() == "MonsterShardsoulCarver")
                    __result = Observe(__result, __instance);
            }
        }
    }
}
#endif
