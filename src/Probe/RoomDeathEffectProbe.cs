using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomDeathEffectProbe
    {
        internal static bool RequiresBirthWorld(FullBattleTrace trace) => trace.KnownCards.Any(card =>
            card.GetCardDataID() == "e1a39d88-4a77-4399-98b2-0b81f44fe6a8");
        internal sealed class Record
        {
            public CombatUnit Actor { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class BirthIdentityPatch
        {
            private static void Prefix(ref Action<CharacterState> afterCharacterCreated)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (trace == null || !RequiresBirthWorld(trace)) return;
                Action<CharacterState>? callback = afterCharacterCreated;
                afterCharacterCreated = actor => { trace.UnitId(actor); callback?.Invoke(actor); };
            }
        }
        [HarmonyPatch(typeof(RoomStateAddEffectOnUnitDeath), nameof(RoomStateAddEffectOnUnitDeath.ApplyCharacterTriggerModification))]
        private static class Patch
        {
            private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState affectedCharacter, ref IEnumerator __result)
            {
                if (trigger == CharacterTriggerData.Trigger.OnDeath && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, affectedCharacter);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState? room = actor.GetCurrentRoom(allowLastKnownRoom: true);
            Record? record = null;
            try
            {
                if (room != null)
                {
                    record = new Record { Actor = trace.CaptureUnit(actor), Before = trace.Capture(room) };
                    Records.Add(record);
                }
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            while (native.MoveNext()) yield return native.Current;
            try { if (record != null) { record.After = trace.Capture(room!); record.Completed = true; } }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal static bool Known(RoomModifierData value) => value.GetRoomStateModifierClassName() == "RoomStateAddEffectOnUnitDeath";
        internal static RoomDeathEffectRule? Rule(IEnumerable<RoomModifierData> values, List<string> interactions)
        {
            RoomModifierData[] modifiers = values.Where(Known).ToArray();
            if (modifiers.Length == 0) return null;
            if (modifiers.Length != 1) { interactions.Add("Multiple room death modifiers."); return null; }
            RoomModifierData modifier = modifiers[0];
            var result = new RoomDeathEffectRule(modifier.GetParamCardEffectData()
                .Select(effect => BattleActionProbe.DescribeTriggerEffect(effect, interactions, captureBirthLifecycle: true)).ToArray(),
                modifier.GetParamCardUpgradeData()?.GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray() ?? Array.Empty<CardUpgradeMaskRule>(),
                modifier.DisallowedInDeploymentPhase);
            string? error = RoomDeathEffectModel.Validate(result);
            if (error != null) interactions.Add(error);
            return result;
        }
    }
}
