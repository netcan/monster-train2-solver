using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityCardProbe
    {
        private static IDictionary Cache => (IDictionary)AccessTools.Field(typeof(UnitOrRoomAbilityCardStateCache), "cardStates")
            .GetValue(UnitOrRoomAbilityCardStateCache.Instance);
        internal static AbilityCardCacheEntry[] Capture(FullBattleTrace trace)
        {
            var entries = new List<AbilityCardCacheEntry>();
            foreach (DictionaryEntry pair in Cache)
                entries.Add(new AbilityCardCacheEntry((string)pair.Key, trace.CardId((CardState)pair.Value)));
            return entries.ToArray();
        }
        internal sealed class Record
        {
            public CardCreationRule Creation { get; set; } = null!;
            public bool Created { get; set; }
            public int CardId { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal sealed class Access
        {
            public int UnitId { get; set; }
            public string DataId { get; set; } = "";
            public int CardId { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<Access> Accesses = new List<Access>();
        private static bool Observing => FullBattleTrace.Active != null && AbilityCooldownScenario.CacheScenario &&
            AbilityCooldownScenario.Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(UnitOrRoomAbilityCardStateCache), nameof(UnitOrRoomAbilityCardStateCache.Get))]
        private static class GetPatch
        {
            private static void Prefix(CardData cardData, out Record? __state)
            {
                __state = null;
                if (!Observing) return;
                bool created = !Cache.Contains(cardData.GetID());
                if (!created && Records.Any(record => !record.Created && record.Creation.DataId == cardData.GetID())) return;
                try
                {
                    __state = new Record { Creation = CardGenerationProbe.Creation(cardData), Created = created,
                        Before = FullBattleTrace.Active!.CaptureContext() };
                    Records.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(CardState __result, Record? __state)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (trace == null) return;
                try
                {
                    trace.CardId(__result);
                    if (__state == null) return;
                    __state.CardId = trace.CardId(__result); __state.After = trace.CaptureContext();
                    AbilityCardResult predicted = AbilityCardModel.Get(__state.Before, __state.Creation);
                    __state.UnsupportedReason = predicted.UnsupportedReason;
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                        predicted.Created == __state.Created && predicted.Card!.InstanceId == __state.CardId &&
                        JToken.DeepEquals(JToken.FromObject(predicted.Context!), JToken.FromObject(__state.After))
                            ? null : "Ability cache context or returned identity differs";
                }
                catch (Exception error) { trace.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.GetUnitAbilityCardState))]
        private static class UnitAccessPatch
        {
            private static void Postfix(CharacterState __instance, CardState? __result)
            {
                if (!Observing || __result == null) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                int unitId = trace.UnitId(__instance);
                if (!Accesses.Any(access => access.UnitId == unitId && access.DataId == __result.GetCardDataID()))
                    Accesses.Add(new Access { UnitId = unitId, DataId = __result.GetCardDataID(), CardId = trace.CardId(__result) });
            }
        }
    }
}
