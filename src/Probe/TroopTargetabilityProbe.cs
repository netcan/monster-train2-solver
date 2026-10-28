#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TroopTargetabilityProbe
    {
        internal sealed class Record
        {
            public object? Before { get; set; }
            public object? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();

        [HarmonyPatch(typeof(RoomStateTroopUnitUpgradeModifier), nameof(RoomStateTroopUnitUpgradeModifier.OnUnitOrderPossiblyChanged))]
        private static class OrderPatch
        {
            private static void Postfix(RoomStateTroopUnitUpgradeModifier __instance, RoomState room, ref IEnumerator __result)
            {
                if (UnsupportedBranchObservation.ObserveTroopTargetability && FullBattleTrace.Active != null)
                    __result = Observe(__result, __instance, room);
            }
        }

        private static IEnumerator Observe(IEnumerator native, RoomStateTroopUnitUpgradeModifier modifier, RoomState room)
        {
            var record = new Record { Before = Snapshot(modifier, room) };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); record.After = Snapshot(modifier, room); }
        }

        private static object Snapshot(RoomStateTroopUnitUpgradeModifier modifier, RoomState room)
        {
            try
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                var cached = (List<CharacterState>)AccessTools.Field(typeof(RoomStateTroopUnitUpgradeModifier), "cachedAffectedTargets").GetValue(modifier);
                int Id(CharacterState actor) => trace.TryGetUnitId(actor, out int id) ? id : 0;
                return new { Frame = UnityEngine.Time.frameCount, Room = room.GetRoomIndex(),
                    Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                    Cached = cached.Select(Id).ToArray(),
                    Units = trace.KnownUnits.Where(actor => !actor.IsDestroyed && actor.GetSourceCharacterData()?.GetAssetKey() == "MonsterFunguy")
                        .Select(actor => new { Id = Id(actor), actor.PreviewMode, actor.IsDead,
                            Health = actor.GetHP(), MaxHealth = actor.GetMaxHP(), Attack = actor.GetAttackDamage(),
                            Horde = actor.GetStatusEffectStacks("horde"), Untouchable = actor.GetStatusEffectStacks("untouchable") }).ToArray() };
            }
            catch (Exception error) { return new { Error = error.GetType().Name + ": " + error.Message }; }
        }
    }
}
#endif
