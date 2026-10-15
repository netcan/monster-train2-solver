using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningCorruptionScenario
    {
        internal const string Name = "original-opening-corruption-relics";
        internal static readonly string[] Ids = { "3a28026e-e44b-4456-b535-7134e7525240", "a66188b9-845d-475e-b6e6-7ea5335ec00b", "a528a1b2-0d14-4fc5-9b8f-4b1f23722422" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? active;
        internal sealed class Snapshot
        {
            public RoomCapacityState[] Rooms { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public BattleResourceState Resources { get; set; } = null!;
            public int Energy { get; set; }
            public int Turn { get; set; }
            public bool Placement { get; set; }
            public bool Preview { get; set; }
            public int UnitCorruptionTriggers { get; set; }
        }
        internal sealed class Record
        {
            public string OwnerId { get; set; } = "";
            public string Type { get; set; } = "";
            public Snapshot Before { get; set; } = null!;
            public Snapshot? After { get; set; }
            public int Notifications { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Opening corruption relics must precede battle.");
            foreach (string id in Ids)
            {
                var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                int index = Array.IndexOf(Ids, id); Check(relic, index);
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id)) throw new InvalidOperationException("Opening corruption owner already acquired.");
                save.AddRelic(relic);
            }
            Acquired = true;
            log.LogInfo("ORIGINAL-OPENING-CORRUPTION-ACQUIRED unchanged max +1, top-floor permanent +1, post-opening-draw +2.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 || Records.Count != 3 || Records.Any(row => row.After == null))
                throw new InvalidOperationException("Opening corruption natural callbacks are incomplete.");
            for (int index = 0; index < Ids.Length; index++)
                Check(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == Ids[index]), index);
            var rooms = RoomCapacityProbe.Capture(managers.GetRoomManager()!);
            if (rooms.Any(room => room.MaxCorruption != 5 || room.PermanentCorruption != (room.RoomIndex == 2 ? 1 : 0) || room.Corruption != (room.RoomIndex == 2 ? 3 : 2)))
                throw new InvalidOperationException("Original opening corruption room values differ.");
            Prepared = true;
            log.LogInfo("ORIGINAL-OPENING-CORRUPTION-PREPARED three natural deployment callbacks; max=5 all rooms; corruption=2 except top=3/permanent=1; original deck and waves.");
        }
        private static Snapshot Capture()
        {
            var managers = AllGameManagers.Instance!; var rooms = managers.GetRoomManager()!;
            int triggers = 0;
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                var units = new List<CharacterState>(); rooms.GetRoom(index).AddCharactersToList(units, Team.Type.Monsters | Team.Type.Heroes);
                triggers += units.Sum(unit => unit.GetTriggers().Count(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.CorruptionAdded));
            }
            return new Snapshot { Rooms = RoomCapacityProbe.Capture(rooms), Relics = RelicProbe.Capture(managers),
                Resources = RelicResourceProbe.CaptureResources(managers)!, Energy = managers.GetPlayerManager().GetEnergy(),
                Turn = managers.GetCombatManager()!.GetTurnCount(), Placement = managers.GetCombatManager()!.IsPlacementPhase,
                Preview = managers.GetSaveManager().PreviewMode, UnitCorruptionTriggers = triggers };
        }
        private static IEnumerator Wrap(IEnumerator native, IRelicEffect effect)
        {
            var owner = AllGameManagers.Instance!.GetSaveManager().GetCollectedRelics().Single(relic => relic.GetEffects().Any(item => ReferenceEquals(item, effect)));
            var row = new Record { OwnerId = owner.GetRelicDataID(), Type = effect.GetType().Name, Before = Capture() };
            Records.Add(row); active = row;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { active = null; }
            row.After = Capture();
        }
        [HarmonyPatch(typeof(RelicEffectModifyMaxCorruption), nameof(RelicEffectModifyMaxCorruption.ApplyEffectStartOfBattle))]
        private static class MaxPatch
        {
            private static void Postfix(RelicEffectModifyMaxCorruption __instance, ref IEnumerator __result)
            { if (Enabled && Acquired) __result = Wrap(__result, __instance); }
        }
        [HarmonyPatch(typeof(RelicEffectAddPermanentCorruptionStartOfCombat), nameof(RelicEffectAddPermanentCorruptionStartOfCombat.ApplyEffectStartOfBattle))]
        private static class PermanentPatch
        {
            private static void Postfix(RelicEffectAddPermanentCorruptionStartOfCombat __instance, ref IEnumerator __result)
            { if (Enabled && Acquired) __result = Wrap(__result, __instance); }
        }
        [HarmonyPatch(typeof(RelicEffectModifyCorruptionStartOfCombat), nameof(RelicEffectModifyCorruptionStartOfCombat.ApplyEffectOnPostOpeningDraw))]
        private static class DrawPatch
        {
            private static void Postfix(RelicEffectModifyCorruptionStartOfCombat __instance, ref IEnumerator __result)
            { if (Enabled && Acquired) __result = Wrap(__result, __instance); }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            { if (active != null && RelicResourceModel.IsOpeningCorruption(triggeredEffect.GetType().Name)) active.Notifications++; }
        }
        private static void Check(CollectableRelicData relic, int index)
        {
            var effect = relic.GetEffects().Single();
            string[] keys = { "IncreaseMaxCorruption", "UnremovableCorruptionTop", "CorruptionBattleStart" };
            string[] types = { RelicResourceModel.MaxCorruptionStart, RelicResourceModel.PermanentCorruptionStart, RelicResourceModel.CorruptionPostOpeningDraw };
            if (relic.name != keys[index] || relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != types[index] ||
                effect.GetParamInt() != (index == 2 ? 2 : 1) || effect.GetParamFloat() != (index == 1 ? 2f : 0f) ||
                effect.GetParamInt2() != 0 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original opening corruption payload changed.");
        }
    }
}
