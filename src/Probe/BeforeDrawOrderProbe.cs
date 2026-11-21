#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class BeforeDrawOrderProbe
    {
        internal sealed class Record
        {
            public string Type { get; set; } = "";
            public int Turn { get; set; }
            public bool Passed { get; set; }
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static bool Active(RelicEffectBase effect) => BeforeDrawOrderScenario.Enabled && BeforeDrawOrderScenario.Prepared &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
            (effect.GetRelicDataId() == DamagedFloorEnergyBattleScenario.OwnerId || effect.GetRelicDataId() == OriginalChampionStartTurnHealScenario.OwnerId);
        private static IEnumerable<Type> Types => new[] { typeof(RelicEffectModifyEnergyWhenDamagedUnitOnEachFloor), typeof(RelicEffectHealStartOfTurn) };
        private static object Snapshot()
        {
            var managers = AllGameManagers.Instance!; var actors = new List<object>();
            for (int room = 0; room < managers.GetRoomManager().GetNumRooms(); room++)
            {
                var units = new List<CharacterState>(); managers.GetRoomManager().GetRoom(room).AddCharactersToList(units, Team.Type.Monsters);
                actors.AddRange(units.Where(unit => !unit.IsPyreHeart()).Select(unit => new { Id = FullBattleTrace.Active!.UnitId(unit), Room = room,
                    Health = unit.GetHP(), Maximum = unit.GetMaxHP() }));
            }
            return new { Energy = managers.GetPlayerManager().GetEnergy(), Actors = actors.ToArray() };
        }
        [HarmonyPatch]
        private static class TestPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => Types.Select(type => AccessTools.Method(type, "TestEffectTurnPhaseTiming")).Distinct();
            private static void Prefix(RelicEffectBase __instance, out Record? __state)
            {
                __state = null; if (!Active(__instance)) return;
                __state = new Record { Type = __instance.GetType().Name, Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(), Before = Snapshot() };
                Records.Add(__state);
            }
            private static void Postfix(bool __result, Record? __state)
            {
                if (__state == null) return; __state.Passed = __result;
                if (!__result) { __state.After = Snapshot(); __state.Completed = true; }
            }
        }
        [HarmonyPatch]
        private static class ApplyPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => Types.Select(type => AccessTools.Method(type, "ApplyEffectTurnPhaseTiming")).Distinct();
            private static void Postfix(RelicEffectBase __instance, ref IEnumerator __result)
            {
                if (!Active(__instance)) return;
                Record record = Records.Last(row => !row.Completed && row.Type == __instance.GetType().Name);
                __result = Wrap(__result, record);
            }
        }
        private static IEnumerator Wrap(IEnumerator source, Record record)
        {
            try { while (source.MoveNext()) yield return source.Current; record.After = Snapshot(); record.Completed = true; }
            finally { (source as IDisposable)?.Dispose(); }
        }
    }
}
#endif
