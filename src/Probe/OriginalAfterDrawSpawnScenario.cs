#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAfterDrawSpawnScenario
    {
        internal const string Name = "original-after-draw-spawn";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired => original != null;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static readonly List<Callback> Callbacks = new();
        private static MutatorData? original;
        private static int[] defenders = Array.Empty<int>();

        internal sealed class Callback
        {
            public int Turn { get; set; }
            public bool Deployment { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
        }

        private static object Describe(MutatorData owner)
        {
            var effect = owner.GetEffects().Single();
            return new { Id = owner.GetID(), Key = owner.name, PlacementExcluded = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Conditions = effect.GetEffectConditions().Count,
                Roster = effect.GetParamCharacters().Select(unit => unit.GetID()).ToArray() };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original after-draw summons require an isolated first battle.");
            string key = Environment.GetEnvironmentVariable("MT2_PROBE_AFTER_DRAW_SPAWN")!;
            string id = key switch
            {
                "Morsels" => RelicResourceProbe.AfterDrawMorselsId,
                "CultLeader" => RelicResourceProbe.AfterDrawCultLeaderId,
                _ => throw new InvalidOperationException("Missing original after-draw summon variant.")
            };
            var owner = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData()).OfType<MutatorData>()
                .Single(data => data.GetID() == id);
            var effect = owner.GetEffects().Single();
            if (owner.DisallowInDeploymentPhase || effect.GetEffectClassName() != nameof(RelicEffectSpawnMonsterOnTurnStart) ||
                effect.GetEffectConditions().Count != 0 || effect.GetParamCharacters().Count != (key == "Morsels" ? 3 : 1))
                throw new InvalidOperationException("Original after-draw summon definition changed.");
            original = owner;
            Definition = Describe(owner);
            save.AddMutator(owner);
            log.LogInfo("ORIGINAL-AFTER-DRAW-SPAWN-ACQUIRED " + owner.name);
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var combat = managers.GetCombatManager();
            if (original == null || !combat.IsPlacementPhase || Callbacks.Count != 1 || Callbacks[0].After == null)
                throw new InvalidOperationException("Original after-draw summons must settle before deployment input.");
            var trace = FullBattleTrace.Active!;
            defenders = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1)
                .Select(trace.CardId).ToArray();
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            Prepared = true;
            log.LogInfo("ORIGINAL-AFTER-DRAW-SPAWN-PREPARED deployment callback settled.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int floor = Array.FindIndex(defenders,
                id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (floor < 0) return null;
            var frontier = BattleActionModel.EnumerateActions(state, defenders[floor]);
            var action = frontier.SupportedActions.FirstOrDefault(candidate => candidate.RoomIndex == floor);
            if (action != null) return action;

            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "after-draw-spawn-decision.mt2f");
            using var document = NativeFixtureCapture.Capture(new { State = state, CardId = defenders[floor], Floor = floor });
            using var stream = File.Create(path);
            document.Write(stream);
            string reasons = string.Join(", ", frontier.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct());
            throw new InvalidOperationException($"Missing after-draw spawn control on floor {floor}. {reasons}");
        }

        internal static bool IsScenarioAction(PlayCardAction action) => defenders.Contains(action.CardInstanceId);

        [HarmonyPatch(typeof(RelicEffectSpawnMonsterOnTurnStart), nameof(RelicEffectSpawnMonsterOnTurnStart.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectSpawnMonsterOnTurnStart __instance, ref IEnumerator __result)
            {
                var trace = FullBattleTrace.Active;
                if (trace != null && original != null && __instance.GetRelicDataId() == original.GetID() &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, trace, AllGameManagers.Instance.GetCombatManager());
            }
        }

        private static IEnumerator Observe(IEnumerator native, FullBattleTrace trace, CombatManager combat)
        {
            var record = new Callback { Turn = combat.GetTurnCount(), Deployment = combat.IsPlacementPhase, Before = trace.CaptureTrain() };
            Callbacks.Add(record);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.After = trace.CaptureTrain();
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
