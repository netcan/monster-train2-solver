using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RedCrownEquipmentObservation
    {
        private const string PoolName = "RedCrownEquipmentPool";
        private static readonly List<Selection> selections = new List<Selection>();
        [ThreadStatic] private static Selection? pending;

        private static bool Enabled => UnsupportedBranchObservation.Enabled &&
            Environment.GetEnvironmentVariable("MT2_PROBE_OBSERVE_RED_CROWN_EQUIPMENT") == "1";

        private sealed class Selection
        {
            internal Selection(CardEffectState effect) { Effect = effect; }
            internal CardEffectState Effect { get; }
            internal string[]? FilteredPool { get; set; }
            internal CardState? Card { get; set; }
            internal uint[] RngBefore { get; } = BattleRng();
            internal uint[]? RngAfter { get; set; }
        }

        internal static object? Capture(PlayCardAction? action, BattleTurnState actual)
        {
            if (!Enabled) return null;

            Selection selection = selections.Single();
            FullBattleTrace trace = FullBattleTrace.Active!;
            if (selection.Card == null || selection.FilteredPool == null || selection.RngAfter == null || action == null)
                throw new InvalidOperationException("Native red crown equipment selection was not observed.");
            string dataId = selection.Card.GetCardDataID();
            CombatUnit host = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit =>
                unit.SpawnerCardId == action.CardInstanceId && unit.Status("redcrown")?.Stacks == 1);
            CardPileState standby = actual.OtherPiles.Single(pile => pile.Name == "Standby");
            int cardId = standby.Cards.Single(card => card.DataId == dataId && host.EquipmentCards?.Contains(card.InstanceId) == true).InstanceId;
            if (!trace.KnownUnits.Any(unit => unit.GetNonPreviewEquipment().Contains(selection.Card)) ||
                standby.EquipmentConditions?.Any(condition => condition.CardId == cardId &&
                    condition.HostUnitId == host.Id && !condition.ReturnToHand) != true || !selection.FilteredPool.Contains(dataId))
                throw new InvalidOperationException("Native red crown equipment attachment did not settle.");

            return new { Pool = PoolName, OrderedCardDataIds = selection.FilteredPool,
                SelectedCardDataId = dataId, CardId = cardId, HostUnitId = host.Id,
                selection.RngBefore, selection.RngAfter };
        }

        private static uint[] BattleRng() => RngCalibration.Words(RandomManager.GetState(RngId.Battle));

        [HarmonyPatch(typeof(CardEffectGrantEquipmentFromPool), "GetRandomCard")]
        private static class SelectEquipmentPatch
        {
            private static bool Prepare() => Enabled;

            private static void Prefix(CardEffectState cardEffectState, out Selection? __state)
            {
                __state = AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == false &&
                    cardEffectState.GetParamCardPool()?.name == PoolName ? new Selection(cardEffectState) : null;
                pending = __state;
            }

            private static Exception? Finalizer(CardState? __result, Selection? __state, Exception? __exception)
            {
                pending = null;
                if (__exception == null && __state != null)
                {
                    __state.Card = __result;
                    __state.RngAfter = BattleRng();
                    selections.Add(__state);
                }
                return __exception;
            }
        }

        [HarmonyPatch]
        private static class FilterEquipmentPoolPatch
        {
            private static bool Prepare() => Enabled;
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(CardEffectState),
                nameof(CardEffectState.GetFilteredCardListFromPool), new[] { typeof(RelicManager), typeof(List<CardData>).MakeByRefType() });

            private static void Postfix(CardEffectState __instance, ref List<CardData> toProcessCards, bool __result)
            {
                if (__result && pending is Selection selection && ReferenceEquals(selection.Effect, __instance))
                    selection.FilteredPool = toProcessCards.Select(card => card.GetID()).ToArray();
            }
        }
    }
}
