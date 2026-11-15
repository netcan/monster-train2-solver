using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomEnemyEntryProbe
    {
        internal sealed class Record
        {
            public string Trigger { get; set; } = "";
            public int ActorId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch(typeof(RoomStateAddStatusEffectOnEnemyEnter), nameof(RoomStateAddStatusEffectOnEnemyEnter.ApplyCharacterTriggerModification))]
        private static class EntryPatch
        {
            [HarmonyPostfix]
            private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState affectedCharacter, ref IEnumerator __result)
            {
                if (OriginalCloisterScenario.Enabled && FullBattleTrace.Active != null && !affectedCharacter.PreviewMode &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode && trigger is
                        CharacterTriggerData.Trigger.AfterSpawnEnchant or CharacterTriggerData.Trigger.PostAscension or
                        CharacterTriggerData.Trigger.PostDescension or CharacterTriggerData.Trigger.OnTurnBegin)
                    __result = Observe(__result, affectedCharacter, trigger);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger trigger)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState? room = actor.GetCurrentRoom(allowLastKnownRoom: true);
            Record? record = null;
            try
            {
                if (room != null)
                {
                    record = new Record { Trigger = trigger.ToString(), ActorId = trace.UnitId(actor), Before = trace.Capture(room) };
                    Records.Add(record);
                }
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            while (native.MoveNext()) yield return native.Current;
            try { if (record != null) { record.After = trace.Capture(room!); record.Completed = true; } }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal static bool Known(RoomModifierData modifier) => modifier.GetRoomStateModifierClassName() == RoomEnemyEntryModel.Type;
        internal static RoomEnemyEntryRule[]? Rules(IEnumerable<RoomModifierData> modifiers, List<string> interactions)
        {
            RoomEnemyEntryRule[] rules = modifiers.Where(Known).Select(modifier => new RoomEnemyEntryRule(
                modifier.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                modifier.DisallowedInDeploymentPhase)).ToArray();
            foreach (RoomEnemyEntryRule rule in rules)
            {
                string? error = RoomEnemyEntryModel.Validate(rule);
                if (error != null) interactions.Add(error);
            }
            return rules.Length == 0 ? null : rules;
        }
    }
}
