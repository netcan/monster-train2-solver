using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamagedFloorEnergyProbe
    {
        internal sealed class Operation
        {
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public int Notifications { get; set; }
            public string? Difference { get; set; }
        }
        internal static readonly List<object> Queries = new List<object>();
        internal static readonly List<Operation> Operations = new List<Operation>();
        private static bool calibrating;
        private static Operation? active;
        internal static void Query(AllGameManagers managers)
        {
            TrainCombatState before = FullBattleTrace.Active!.CaptureTrain();
            int native = managers.GetRelicManager().GetEnergyPerTurnModification();
            RelicResourceResult model = RelicResourceModel.GetEnergyModification(before.Context!, train: before);
            string? difference = !model.Supported ? model.UnsupportedReason : model.Value != native ? "Energy query differs." :
                HalfHealthLifecycleCalibration.Difference(before, FullBattleTrace.Active.CaptureTrain());
            Queries.Add(new { Before = before, Native = native, Difference = difference, Completed = true });
            if (difference != null) throw new InvalidOperationException(difference);
        }
        internal static object Calibrate(AllGameManagers managers)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            CharacterState[] actors = trace.KnownUnits.Where(unit => !unit.IsDestroyed).ToArray();
            CharacterState[] players = Enumerable.Range(0, 3).Select(room => actors.Single(unit => unit.GetTeamType() == Team.Type.Monsters &&
                !unit.IsPyreHeart() && unit.GetCurrentRoomIndex() == room)).ToArray();
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object[] originals = actors.Select(unit => primary.GetValue(unit)).ToArray();
            var energyField = AccessTools.Field(typeof(PlayerManager), "energy"); var playerManager = managers.GetPlayerManager();
            int energy = playerManager.GetEnergy(); TrainCombatState original = trace.CaptureTrain(); object rng = HalfHealthLifecycleCalibration.Rng();
            var streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id, id =>
                new HadesRNG((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id })));
            UnityEngine.Random.State unity = UnityEngine.Random.state;
            IRelicEffect native = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() ==
                DamagedFloorEnergyBattleScenario.OwnerId).GetEffects().Single();
            var timing = (ITurnPhaseTimingRelicEffect)native;
            var parameters = new RelicEffectParams { relicManager = managers.GetRelicManager() };
            var samples = new List<object>(); calibrating = true;
            try
            {
                for (int mask = 0; mask < 8; mask++) foreach (int start in new[] { 0, 99 }) Record(mask, start, "mask");
                Record(7, 0, "zero-hp"); Record(7, 0, "negative-hp"); Record(7, 0, "over-max"); Record(0, 0, "pyre-only");
            }
            finally
            {
                Restore(); energyField.SetValue(playerManager, energy); calibrating = false;
                foreach (var pair in streams) ((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { pair.Key })).Init(pair.Value);
                UnityEngine.Random.state = unity;
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(rng, HalfHealthLifecycleCalibration.Rng());
            if (restoration != null) throw new InvalidOperationException("Damaged-floor restoration: " + restoration);
            return new { Completed = true, LiveContextUnchanged = true, RestorationDifference = restoration,
                RngBefore = rng, RngAfter = HalfHealthLifecycleCalibration.Rng(), Samples = samples };
            void Restore() { for (int index = 0; index < actors.Length; index++) primary.SetValue(actors[index], originals[index]); }
            void Record(int mask, int start, string kind)
            {
                Restore(); energyField.SetValue(playerManager, start);
                foreach (CharacterState unit in actors)
                {
                    object live = primary.GetValue(unit);
                    object shadow = AccessTools.Method(live.GetType(), "DeepCopyForPreview").Invoke(live, null); primary.SetValue(unit, shadow);
                    int floor = Array.IndexOf(players, unit);
                    int hp = floor >= 0 ? ((mask & (1 << floor)) != 0 ? unit.GetMaxHP() - 1 : unit.GetMaxHP()) : unit.GetHP();
                    if (floor == 1 && kind != "mask") hp = kind == "zero-hp" ? 0 : kind == "negative-hp" ? -1 : kind == "over-max" ? unit.GetMaxHP() + 1 : hp;
                    if (unit.IsPyreHeart() && kind == "pyre-only") hp = unit.GetMaxHP() - 1;
                    AccessTools.Property(shadow.GetType(), "Hp").SetValue(shadow, hp);
                }
                TrainCombatState before = trace.CaptureTrain();
                bool condition = native.AreConditionsTrue(managers.GetCardStatistics());
                bool test = timing.TestEffectTurnPhaseTiming(parameters, managers.GetCoreManagers());
                int query = managers.GetRelicManager().GetEnergyPerTurnModification();
                var modelQuery = RelicResourceModel.GetEnergyModification(before.Context!, train: before);
                string? difference = !modelQuery.Supported ? modelQuery.UnsupportedReason : modelQuery.Value != query ? "Native energy query differs." : null;
                IEnumerator routine = timing.ApplyEffectTurnPhaseTiming(parameters, managers.GetCoreManagers());
                while (routine.MoveNext()) throw new InvalidOperationException("Unexpected asynchronous floor-energy body.");
                TrainCombatState actual = trace.CaptureTrain();
                var result = RelicResourceModel.ApplyEnergyTiming(before.Context!, train: before);
                difference ??= !result.Supported ? result.UnsupportedReason : HalfHealthLifecycleCalibration.Difference(
                    CardSpellModel.WithContext(before, result.Context!), actual);
                samples.Add(new { Mask = mask, Kind = kind, Before = before, NativeCondition = condition, NativeTest = test,
                    NativeQuery = query, Actual = actual, Completed = true, Difference = difference });
                if (difference != null) throw new InvalidOperationException(kind + ": " + difference);
            }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            Operation row = new Operation { Before = FullBattleTrace.Active!.CaptureTrain() }; Operations.Add(row); active = row;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { active = null; }
            row.Actual = FullBattleTrace.Active.CaptureTrain();
            var result = RelicResourceModel.ApplyEnergyTiming(row.Before.Context!, train: row.Before);
            row.Difference = !result.Supported ? result.UnsupportedReason : HalfHealthLifecycleCalibration.Difference(
                CardSpellModel.WithContext(row.Before, result.Context!), row.Actual);
            row.Completed = true;
            if (row.Difference != null) throw new InvalidOperationException("Native floor-energy callback: " + row.Difference);
        }
        [HarmonyPatch]
        private static class TimingPatch
        {
            private static bool Prepare() => typeof(RelicEffectBase).Assembly.GetType(RelicDamagedFloorEnergyModel.Effect) != null;
            private static IEnumerable<MethodBase> TargetMethods()
            {
                Type? type = typeof(RelicEffectBase).Assembly.GetType(RelicDamagedFloorEnergyModel.Effect);
                if (type != null) yield return AccessTools.Method(type, "ApplyEffectTurnPhaseTiming");
            }
            private static void Postfix(ref IEnumerator __result)
            { if (DamagedFloorEnergyBattleScenario.Prepared && !calibrating) __result = Observe(__result); }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            { if (active != null && triggeredEffect.GetType().Name == RelicDamagedFloorEnergyModel.Effect) active.Notifications++; }
        }
    }
}
