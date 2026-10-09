using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicSpawnStatusProbe
    {
        internal sealed class Record
        {
            public int UnitId { get; set; }
            public int FromCardId { get; set; }
            public bool OnlyCovenants { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.CharacterAdded), new[] { typeof(CharacterState), typeof(CardState), typeof(bool) })]
        private static class AddedPatch
        {
            private static void Postfix(CharacterState character, CardState fromCard, bool onlyCovenants, ref IEnumerator __result)
            {
                if (!SpawnStatusRelicScenario.Prepared || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode || onlyCovenants) return;
                __result = Observe(__result, character, fromCard, onlyCovenants);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState character, CardState fromCard, bool covenants)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = character.GetCurrentRoom();
            var record = new Record { UnitId = trace.UnitId(character), FromCardId = fromCard == null ? 0 : trace.CardId(fromCard),
                OnlyCovenants = covenants, Before = trace.Capture(room) };
            Records.Add(record);
            RoomCombatResult predicted = RelicSpawnStatusModel.CharacterAdded(record.Before, record.UnitId, record.FromCardId, covenants);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.After = trace.Capture(room);
                record.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                    JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)) ? null :
                    "Relic CharacterAdded room/context differs.";
            }
        }
    }
}
