using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnUpgradeProbe
    {
        internal const string UpgradeDataId = "c2f6ed7f-18ce-4070-b65f-7dd9f5190027";
        internal sealed class Record
        {
            public int UnitId { get; set; }
            public int SpawnerCardId { get; set; }
            public CardUpgradeModifier Upgrade { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? AfterDirect { get; set; }
            public RoomCombatState? After { get; set; }
            public bool SourceAdded { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            internal CardUpgradeState NativeUpgrade = null!;
            internal RoomState Room = null!;
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static IEnumerator Observe(IEnumerator native, CharacterState unit, CardUpgradeState upgrade)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var room = unit.GetCurrentRoom();
            var before = trace.Capture(room);
            int unitId = trace.UnitId(unit);
            var record = new Record { UnitId = unitId, SpawnerCardId = before.Units.Single(actor => actor.Id == unitId).SpawnerCardId,
                Upgrade = CardModifierProbe.Upgrade(upgrade), Before = before, NativeUpgrade = upgrade, Room = room };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.AfterDirect = trace.Capture(room);
                if (record.SpawnerCardId == 0) { record.After = record.AfterDirect; Complete(record); }
            }
        }
        private static void Complete(Record record)
        {
            record.Completed = true;
            RoomCombatResult predicted = SpawnUpgradeModel.Apply(record.Before, record.UnitId, record.SpawnerCardId, record.Upgrade);
            RoomCombatResult direct = UnitModifierModel.ApplyDirect(record.Before, record.UnitId, record.Upgrade);
            record.Difference = !predicted.Supported ? predicted.UnsupportedReason : !direct.Supported ? direct.UnsupportedReason :
                !JToken.DeepEquals(JToken.FromObject(direct.State!), JToken.FromObject(record.AfterDirect!)) ? "Extra spawn direct upgrade differs" :
                !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After!)) ? "Extra spawn source write differs" : null;
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyCardUpgrade))]
        private static class DirectPatch
        {
            private static void Postfix(CharacterState __instance, CardUpgradeState cardUpgradeState, bool fromSpawn, ref IEnumerator __result)
            {
                if (!MultiSummonScenario.Prepared || FullBattleTrace.Active == null || fromSpawn ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode || cardUpgradeState.GetCardUpgradeDataId() != UpgradeDataId) return;
                __result = Observe(__result, __instance, cardUpgradeState);
            }
        }
        [HarmonyPatch(typeof(CardStateModifiers), nameof(CardStateModifiers.AddUpgrade))]
        private static class SourcePatch
        {
            private static void Postfix(CardStateModifiers __instance, CardUpgradeState upgradeState, bool __result)
            {
                Record? record = Records.LastOrDefault(sample => !sample.Completed && sample.AfterDirect != null &&
                    ReferenceEquals(sample.NativeUpgrade, upgradeState));
                if (record == null) return;
                var birth = UnitBirthProbe.Records.Last(sample => sample.UnitId == record.UnitId);
                if (!ReferenceEquals(birth.Unit!.GetSpawnerCard().GetTemporaryCardStateModifiers(), __instance)) return;
                record.SourceAdded = __result; record.After = FullBattleTrace.Active!.Capture(record.Room); Complete(record);
            }
        }
    }
}
