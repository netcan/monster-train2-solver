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
        private static string? Variant => Environment.GetEnvironmentVariable("MT2_PROBE_AFTER_DRAW_SPAWN");
        private static bool LonelyRooms => Variant == "CultLeaderLonelyRooms";
        internal static bool Acquired => originals.Length != 0;
        internal static bool Prepared { get; private set; }
        internal static object? Definition => Definitions?.FirstOrDefault();
        internal static object? CurrentDefinition => Acquired ? Describe(originals[0]) : null;
        internal static object[]? Definitions { get; private set; }
        internal static object[]? CurrentDefinitions => Acquired ? originals.Select(Describe).ToArray() : null;
        internal static readonly List<Callback> Callbacks = new();
        private static MutatorData[] originals = Array.Empty<MutatorData>();
        private static int[] defenders = Array.Empty<int>();

        internal sealed class Callback
        {
            public string OwnerId { get; set; } = null!;
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
            if (Acquired) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original after-draw summons require an isolated first battle.");
            string[] ids = Variant switch
            {
                "Morsels" => [RelicResourceProbe.AfterDrawMorselsId],
                "CultLeader" or "CultLeaderLonelyRooms" => [RelicResourceProbe.AfterDrawCultLeaderId],
                "CultLeaderMorsels" => [RelicResourceProbe.AfterDrawCultLeaderId, RelicResourceProbe.AfterDrawMorselsId],
                _ => throw new InvalidOperationException("Missing original after-draw summon variant.")
            };
            var candidates = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData()).OfType<MutatorData>().ToArray();
            var owners = ids.Select(id => candidates.Single(data => data.GetID() == id)).ToArray();
            foreach (MutatorData owner in owners)
            {
                var effect = owner.GetEffects().Single();
                int rosterSize = owner.GetID() == RelicResourceProbe.AfterDrawMorselsId ? 3 : 1;
                if (owner.DisallowInDeploymentPhase || effect.GetEffectClassName() != nameof(RelicEffectSpawnMonsterOnTurnStart) ||
                    effect.GetEffectConditions().Count != 0 || effect.GetParamCharacters().Count != rosterSize)
                    throw new InvalidOperationException("Original after-draw summon definition changed.");
            }
            if (LonelyRooms) OriginalLonelyRoomsBattle.Acquire(save);
            originals = owners;
            Definitions = owners.Select(Describe).ToArray();
            foreach (MutatorData owner in owners)
            {
                save.AddMutator(owner);
                log.LogInfo("ORIGINAL-AFTER-DRAW-SPAWN-ACQUIRED " + owner.name);
            }
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var combat = managers.GetCombatManager();
            if (!Acquired || !combat.IsPlacementPhase ||
                !Callbacks.Select(callback => callback.OwnerId).SequenceEqual(originals.Select(owner => owner.GetID())) ||
                Callbacks.Any(callback => !callback.Deployment || callback.Turn != 0 || callback.After == null))
                throw new InvalidOperationException("Original after-draw summons must settle before deployment input.");
            var trace = FullBattleTrace.Active!;
            defenders = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, LonelyRooms ? 1 : 3, 1)
                .Select(trace.CardId).ToArray();
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            Prepared = true;
            log.LogInfo("ORIGINAL-AFTER-DRAW-SPAWN-PREPARED deployment callback settled.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int defender = Array.FindIndex(defenders,
                id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (defender < 0) return null;
            int floor = LonelyRooms ? 2 : defender;
            var frontier = BattleActionModel.EnumerateActions(state, defenders[defender]);
            var action = frontier.SupportedActions.FirstOrDefault(candidate => candidate.RoomIndex == floor);
            if (action != null) return action;

            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "after-draw-spawn-decision.mt2f");
            using var document = NativeFixtureCapture.Capture(new { State = state, CardId = defenders[defender], Floor = floor });
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
                string ownerId = __instance.GetRelicDataId();
                if (trace != null && originals.Any(owner => owner.GetID() == ownerId) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, trace, AllGameManagers.Instance.GetCombatManager(), ownerId);
            }
        }

        private static IEnumerator Observe(IEnumerator native, FullBattleTrace trace, CombatManager combat, string ownerId)
        {
            var record = new Callback
            {
                OwnerId = ownerId, Turn = combat.GetTurnCount(), Deployment = combat.IsPlacementPhase, Before = trace.CaptureTrain()
            };
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
