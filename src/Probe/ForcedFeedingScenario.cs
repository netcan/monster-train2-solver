using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class ForcedFeedingScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_FORCED_FEEDING") == "1";
        internal static bool ScalingMode => Environment.GetEnvironmentVariable("MT2_PROBE_SHROUD_SPIKE") == "1";
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool DeploymentRejected { get; private set; }
        private static bool observing, controlPlayed;
        internal sealed class Record
        {
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public TrainCombatState? Predicted { get; set; }
            public int FoodStatBefore { get; set; }
            public int FoodStatAfter { get; set; }
            public int Meals { get; set; }
            public int CardId { get; set; }
            public int TriggerCount { get; set; } = 1;
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static void CheckDeployment(AllGameManagers managers)
        {
            CardEffectState effect = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .First(card => card.GetCardDataID() == "236215da-c1dd-40b7-8793-27ce327a7e78").GetEffectStates().Single();
            TrainCombatState before = FullBattleTrace.Active!.CaptureTrain();
            var parameters = new CardEffectParams();
            DeploymentRejected = !new CardEffectFeederRules().TestEffect(effect, parameters, managers.GetCoreManagers());
            if (!DeploymentRejected || !JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(FullBattleTrace.Active.CaptureTrain())))
                throw new InvalidOperationException("Forced feeding deployment test changed state or accepted the cast.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            bool Feast(PlayCardAction play) => source.PlayRules!.Cards.Single(rule =>
                rule.DataId == source.Spawn.Train.Context!.FindCard(play.CardInstanceId)!.DataId).AssetKey == "Feast";
            if (!source.Spawn.Train.Rooms[0].Deployment)
            {
                if (!controlPlayed)
                {
                    PlayCardAction? control = plays.FirstOrDefault(play => Feast(play) && play.RoomIndex == 1);
                    if (control != null) { controlPlayed = true; return control; }
                }
                if (ScalingMode)
                {
                    PlayCardAction? spike = plays.FirstOrDefault(play => play.RoomIndex == 0 && play.TargetUnitId > 0 &&
                        source.Spawn.Train.Context!.FindCard(play.CardInstanceId)!.DataId == "fecc00fd-70ef-473e-bfa1-35c221540d43" &&
                        source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Id == play.TargetUnitId && RoomCombatModel.IsFood(unit)));
                    if (spike != null) return spike;
                }
                PlayCardAction? positive = plays.FirstOrDefault(play => Feast(play) && play.RoomIndex == 0 &&
                    source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit =>
                        unit.Team == CombatTeam.Player && RoomCombatModel.IsFood(unit)));
                if (positive != null) return positive;
            }
            return plays.FirstOrDefault(play => play.RoomIndex == 0 &&
                source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(play.CardInstanceId)!.DataId).SpawnUnit is CombatUnit unit &&
                (!RoomCombatModel.IsFood(unit) || play.PlayerPosition > 0));
        }
        [HarmonyPatch(typeof(CardEffectFeederRules), nameof(CardEffectFeederRules.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && !observing && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            var row = new Record(); Records.Add(row); observing = true;
            try
            {
                row.Before = FullBattleTrace.Active!.CaptureTrain();
                CardState parent = effect.GetParentCardState();
                row.CardId = FullBattleTrace.Active.CardId(parent);
                CardTraitScalingFeederRules? scaling = parent.GetTraitStates().OfType<CardTraitScalingFeederRules>().FirstOrDefault();
                row.TriggerCount = scaling?.GetAdditionalTriggers(AllGameManagers.Instance!.GetCardStatistics()) ?? 1;
                row.Targets = parameters.targets.Select(FullBattleTrace.Active.UnitId).ToArray();
                row.FoodStatBefore = AllGameManagers.Instance!.GetSaveManager().GetRunStat(RunStat.StatType.FoodUnitsEaten);
                if (row.Targets.Length == 0) row.Predicted = row.Before;
                else
                {
                    int floor = parameters.targets[0].GetCurrentRoomIndex();
                    RoomCombatResult result = RoomCombatModel.ApplyFeeding(row.Before.Rooms.Single(room => room.RoomIndex == floor), CombatTeam.Player, row.Targets, row.TriggerCount);
                    row.UnsupportedReason = result.UnsupportedReason;
                    row.Meals = result.Events.Count(item => item.Kind == "Eaten");
                    if (result.Supported) row.Predicted = CardSpellModel.WithContext(new TrainCombatState(row.Before.Rooms.Select(room =>
                        room.RoomIndex == floor ? result.State! : room).ToArray(), row.Before.Movement, row.Before.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                }
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); observing = true;
                try
                {
                    row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    row.FoodStatAfter = AllGameManagers.Instance!.GetSaveManager().GetRunStat(RunStat.StatType.FoodUnitsEaten);
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Forced feeding complete train state differs.";
                    if (row.FoodStatAfter - row.FoodStatBefore != unchecked(row.Meals * row.TriggerCount)) row.Difference = "Forced feeding run statistic differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; }
            }
        }
    }
}
