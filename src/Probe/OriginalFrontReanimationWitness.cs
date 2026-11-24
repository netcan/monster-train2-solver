#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFrontReanimationWitness
    {
        private const string Status = "undying";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string Mode = Environment.GetEnvironmentVariable("MT2_PROBE_FRONT_REANIMATION") ?? "";
        private static readonly (string Key, string Id, int Stacks) Definition = Mode switch
        {
            "Tier2" => ("Enemy_FrontReanimationT2", "d5bc1611-0bd2-47c5-9d78-3280efbc8fbe", 1),
            "Tier3" => ("Enemy_FrontReanimationT3", "a8202bfb-55f0-4630-a592-d60177796bf0", 2),
            _ => default
        };
        internal static bool Enabled => Definition.Id != null;
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_FRONT_REANIMATION_FULL_BATTLE") == "1";
        internal static bool Captured { get; private set; }
        private static RelicEffectAddStatusEffectOnFirstSpawn? active;
        private static object? owner;
        private static readonly List<Observation> Rows = new();
        private static readonly List<BattleTurnState> Decisions = new();

        private static void Acquire(AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var heroes = managers.GetHeroManager();
            if (Application.version != "2.3.1" || typeof(HeroManager).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetRunType() != RunType.Class || save.GetCurrentDistance() != 1 ||
                save.GetCurrentScenarioData()?.name != "Level2BattleJunker" || save.GetCovenants().Count != 0 ||
                save.GetMutators().Count != 0 || save.GetTrialEnabled() || managers.GetCombatManager().GetTurnCount() != 0 ||
                heroes.GetHeroBlessings().Any(blessing => blessing.GetEffects().Any(effect => effect is RelicEffectAddStatusEffectOnFirstSpawn)))
                throw new InvalidOperationException("Expected the pinned original Ring 2 deployment without another first-spawn owner.");
            var definition = save.GetAllGameData().FindEnemySinsData(Definition.Id)
                ?? throw new InvalidOperationException("Missing original first-spawn reanimation Sin.");
            var effect = definition.GetEffects().Single();
            var status = effect.GetParamStatusEffects().Single();
            if (definition.name != Definition.Key || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectAddStatusEffectOnFirstSpawn) ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetTargetMode() != TargetMode.FrontInRoom ||
                effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnSpawn || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamCharacterSubtype()?.IsNone != true || status.statusId != Status || status.count != Definition.Stacks)
                throw new InvalidOperationException("Original first-spawn reanimation payload changed.");
            heroes.AddHeroBlessing(definition);
            var installed = heroes.GetHeroBlessings().Single(blessing => blessing.GetRelicDataID() == Definition.Id);
            if (installed.GetEffects() is not [RelicEffectAddStatusEffectOnFirstSpawn native])
                throw new InvalidOperationException("The original hero blessing did not initialize its first-spawn callback.");
            active = native;
            owner = new
            {
                Definition.Id, AssetKey = Definition.Key, Type = native.GetType().Name, Definition.Stacks,
                Team = effect.GetParamSourceTeam().ToString(), Target = effect.GetTargetMode().ToString(),
                Trigger = effect.GetParamTrigger().ToString(), native.CanApplyInPreviewMode, definition.DisallowInDeploymentPhase
            };
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            if (active == null) Acquire(managers);
            var decision = trace.CaptureDecision();
            Decisions.Add(decision);
            var committed = Rows.Where(row => !row.Preview && row.AfterLatch.HasValue).ToArray();
            int first = Array.FindIndex(committed, IsGrant);
            int later = -1;
            int reset = -1;
            int next = -1;
            if (first >= 0)
            {
                int afterDraw = Array.FindIndex(committed, first + 1, row => row.Kind == "AfterDraw");
                int sameWaveEnd = afterDraw < 0 ? committed.Length : afterDraw;
                later = Array.FindIndex(committed, first + 1, sameWaveEnd - first - 1,
                    row => row.Kind == "Birth" && row.Team == "Heroes" && row.Actor != committed[first].Actor && row.BeforeLatch &&
                    row.AfterLatch == true && row.BeforeStacks == row.AfterStacks);
                if (afterDraw >= 0 && committed[afterDraw].BeforeLatch && committed[afterDraw].AfterLatch == false)
                {
                    reset = afterDraw;
                    int nextAfterDraw = Array.FindIndex(committed, reset + 1, row => row.Kind == "AfterDraw");
                    int nextWaveEnd = nextAfterDraw < 0 ? committed.Length : nextAfterDraw;
                    next = Array.FindIndex(committed, reset + 1, nextWaveEnd - reset - 1, IsGrant);
                }
            }
            bool complete = later >= 0 && next >= 0;
            if (!complete && decision.Spawn.Turn < 3)
            {
                advanceTurn();
                return false;
            }
            string fileName = complete ? "front-reanimation.mt2f" : "front-reanimation-diagnostic.mt2f";
            using (var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalFirstSpawnReanimation", GameVersion = Application.version,
                GameModuleMvid = Module, Activation = "HeroManager.AddHeroBlessing", Mode, Owner = owner,
                NativeRunInput = trace.ObservedRunInput, Decisions, Rows,
                CommittedRowIndices = new { FirstGrant = first, LaterCandidate = later, AfterDrawReset = reset, NextGrant = next },
                Phases = trace.CaptureObservedPhases(default, trace.ObservedPhaseCounts)
            }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, fileName)))
                archive.Write(stream);
            if (!complete)
                throw new InvalidOperationException($"Native first-spawn, later candidate, AfterDraw reset and next-turn grant were not all observed ({committed.Length} committed callbacks, indices {first}/{later}/{reset}/{next}). Saved {fileName}.");
            Captured = true;
            return true;
        }

        private static bool IsGrant(Observation row) => row.Kind == "Birth" && row.Team == "Heroes" && !row.BeforeLatch &&
            row.AfterLatch == true && row.AfterStacks - row.BeforeStacks == Definition.Stacks;

        private static bool Latch() => (bool)AccessTools.Field(typeof(RelicEffectAddStatusEffectOnFirstSpawn),
            "firstWasSummonedThisTurn").GetValue(active);

        private static uint[] BattleRng() => RngCalibration.Words(RandomManager.GetState(RngId.Battle));

        private sealed class Observation
        {
            public string Kind { get; set; } = "";
            public int Frame { get; set; }
            public int Turn { get; set; }
            public bool Preview { get; set; }
            public int? Actor { get; set; }
            public string? AssetKey { get; set; }
            public string? Team { get; set; }
            public int? Room { get; set; }
            public bool? FromCard { get; set; }
            public bool BeforeLatch { get; set; }
            public bool? AfterLatch { get; set; }
            public int? BeforeStacks { get; set; }
            public int? AfterStacks { get; set; }
            public uint[] RngBefore { get; set; } = null!;
            public uint[]? RngAfter { get; set; }
        }

        private static IEnumerator Observe(IEnumerator native, string kind, CharacterState? actor = null, CardState? fromCard = null)
        {
            var managers = AllGameManagers.Instance!;
            var row = new Observation
            {
                Kind = kind, Frame = Time.frameCount, Turn = managers.GetCombatManager().GetTurnCount(),
                Preview = managers.GetSaveManager().PreviewMode,
                Actor = actor == null ? null : FullBattleTrace.Active!.UnitId(actor),
                AssetKey = actor?.GetSourceCharacterData().GetAssetKey(),
                Team = actor?.GetTeamType().ToString(), Room = actor?.GetCurrentRoomIndex(),
                FromCard = actor == null ? null : fromCard != null, BeforeLatch = Latch(),
                BeforeStacks = actor?.GetStatusEffectStacks(Status), RngBefore = BattleRng()
            };
            Rows.Add(row);
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); }
            row.AfterLatch = Latch();
            row.AfterStacks = actor?.GetStatusEffectStacks(Status);
            row.RngAfter = BattleRng();
        }

        [HarmonyPatch(typeof(RelicEffectAddStatusEffectOnFirstSpawn), nameof(RelicEffectAddStatusEffectOnFirstSpawn.OnCharacterAdded))]
        private static class BirthPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(RelicEffectAddStatusEffectOnFirstSpawn __instance, CharacterState character, CardState? fromCard,
                ref IEnumerator __result)
            {
                if (ReferenceEquals(__instance, active)) __result = Observe(__result, "Birth", character, fromCard);
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddStatusEffectOnFirstSpawn), nameof(RelicEffectAddStatusEffectOnFirstSpawn.ApplyEffectTurnPhaseTiming))]
        private static class AfterDrawPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(RelicEffectAddStatusEffectOnFirstSpawn __instance, ref IEnumerator __result)
            {
                if (ReferenceEquals(__instance, active)) __result = Observe(__result, "AfterDraw");
            }
        }
    }
}
#endif
