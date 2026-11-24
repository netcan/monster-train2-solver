#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEndlessEnemyScalingWitness
    {
        private const string OwnerId = "b342ef1d-306b-4d40-aa9d-782bbae58af3";
        private const string OwnerKey = "Enemy_EndlessScaling";
        private const string OpeningScenario = "TestEndless_Level1Battle2";
        private const int ScheduledWaveCount = 6;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_WITNESS") == "1";
        internal static bool FactorBatch => Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_FACTOR_BATCH") == "1";
        internal static bool AllWaves => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_ALL_WAVES") == "1";
        private static bool ControlledNextRing => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_CONTROLLED_NEXT_RING") == "1";
        private static bool ControlledLoop2 => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_CONTROLLED_LOOP2") == "1";
        private static bool ControlledRoute => ControlledNextRing || ControlledLoop2;
        private static int ControlledDistance => int.Parse(
            Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_CONTROLLED_DISTANCE") ?? "1", CultureInfo.InvariantCulture);
        private static bool ObserveOuterBoss => ControlledNextRing && ControlledDistance == 2 && OuterBossBirthProbe.Enabled;
        internal static bool ControlledFirstLoop { get; private set; }
        private static bool controlledAdvanceApplied;
        private static object? scenario;
        private static BattleTurnState? deployment;
        private static readonly List<Birth> Births = new List<Birth>();
        private static readonly List<Wave> Waves = new List<Wave>();
        private static Wave? activeWave;

        internal static void CaptureFactorBatch(AllGameManagers managers)
        {
            var gameData = managers.GetAllGameData();
            var balance = gameData.GetBalanceData();
            var owner = balance.GetEndlessScalingSins();
            int runLength = balance.GetRunData(forFtue: false, forEndless: true, forRegionRun: false).GetNumDistances();
            if (Application.version != "2.3.1" || typeof(CharacterState).Module.ModuleVersionId != Module || runLength != 8 ||
                owner?.GetID() != OwnerId || owner.name != OwnerKey || owner.GetEffects() is not [var effect] ||
                effect.GetEffectClassName() != nameof(RelicEffectEndlessScalingOnSpawn))
                throw new InvalidOperationException("The Endless factor batch requires the pinned original balance data and eight-battle route.");

            Dictionary<string, uint[]> ReadRng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id)));
            var rngBefore = ReadRng();
            int frame = Time.frameCount;
            var scaling = balance.GetEndlessScalingData();
            var factors = (from loop in Enumerable.Range(1, 3)
                           from distance in Enumerable.Range(0, runLength)
                           select new
                           {
                               Loop = loop, Distance = distance,
                               Attack = scaling.GetAttackScaling(loop, distance, runLength),
                               Health = scaling.GetHealthScaling(loop, distance, runLength),
                               Status = scaling.GetStatusScaling(loop, distance, runLength),
                               BuffDamage = scaling.GetCardEffectBuffDamageScaling(loop, distance, runLength),
                               Heal = scaling.GetCardEffectHealScaling(loop, distance, runLength),
                               Damage = scaling.GetCardEffectDamageScaling(loop, distance, runLength)
                           }).ToArray();
            var statusData = managers.GetStatusEffectManager().GetAllStatusEffectsData();
            var statuses = statusData.GetStatusEffectData()
                .Select(data => new { Id = data.GetStatusId(), ScalesInEndless = data.ScalesInEndless() })
                .OrderBy(data => data.Id, StringComparer.Ordinal).ToArray();
            var scalable = statuses.Where(data => data.ScalesInEndless).Select(data => data.Id).ToHashSet();
            var characters = gameData.GetAllCharacterData();
            var startingStatuses = characters.SelectMany(character => character.GetStartingStatusEffects())
                .Where(status => scalable.Contains(status.statusId)).ToArray();
            var types = new[] { nameof(CardEffectDamage), nameof(CardEffectHeal), nameof(CardEffectBuffDamage),
                nameof(CardEffectAddStatusEffect), nameof(CardEffectEnchant) };
            var payloads = characters.SelectMany(character => character.GetTriggers()).SelectMany(trigger => trigger.GetEffects())
                .Where(data => types.Contains(data.GetEffectStateName())).GroupBy(data => data.GetEffectStateName())
                .OrderBy(group => group.Key, StringComparer.Ordinal).Select(group =>
                {
                    var eligibleStatuses = group.SelectMany(data => data.GetParamStatusEffectStackData() ?? Array.Empty<StatusEffectStackData>())
                        .Where(status => scalable.Contains(status.statusId)).ToArray();
                    return new
                    {
                        Type = group.Key, Definitions = group.Count(), PositiveInts = group.Count(data => data.GetParamInt() > 0),
                        ZeroInts = group.Count(data => data.GetParamInt() == 0),
                        PositiveEligibleStatuses = eligibleStatuses.Count(status => status.count > 0),
                        ZeroEligibleStatuses = eligibleStatuses.Count(status => status.count == 0)
                    };
                }).ToArray();
            var rngAfter = ReadRng();
            if (frame != Time.frameCount || rngBefore.Any(pair => !pair.Value.SequenceEqual(rngAfter[pair.Key])))
                throw new InvalidOperationException("The Endless factor queries changed frame or deterministic RNG state.");

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalEndlessEnemyScalingFactors", GameVersion = Application.version, GameModuleMvid = Module,
                OwnerId, OwnerKey, RunLength = runLength, ExcludedSubtypes = effect.GetParamExcludeCharacterSubtypes().Select(subtype => subtype.Key).ToArray(),
                Factors = factors, StatusEligibility = statuses, AuthoredTriggerPayloads = payloads,
                AuthoredStartingStacks = new { Positive = startingStatuses.Count(status => status.count > 0), Zero = startingStatuses.Count(status => status.count == 0) },
                RngBefore = rngBefore, RngAfter = rngAfter
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "endless-enemy-scaling-factors.mt2f"));
            document.Write(output);
        }

        internal static void BeginFirstLoop(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (AllWaves || ControlledRoute || save.IsEndlessRun || save.GetRunType() != RunType.Class ||
                save.GetGameSequence() != SaveData.GameSequence.DestinationReached)
                throw new InvalidOperationException("Expected a fresh isolated Class run before opening the original Endless loop.");
            ControlledFirstLoop = true;
            save.SetEndlessRun();
            managers.GetGameStateManager().LoopEndlessRun();
            log.LogInfo("ENDLESS-ENEMY-SCALING-ROUTE native first loop configured on the isolated Class run.");
        }

        internal static void Arm(SaveManager save)
        {
            var native = save.GetEndlessScalingSins();
            var run = save.GetRunState();
            if (Application.version != "2.3.1" || typeof(CharacterState).Module.ModuleVersionId != Module ||
                !save.IsEndlessRun || save.PreviewMode || run.GetLoopIndex() < 1 || native?.GetID() != OwnerId ||
                native.name != OwnerKey || native.GetEffects() is not [var effect] ||
                effect.GetEffectClassName() != nameof(RelicEffectEndlessScalingOnSpawn))
                throw new InvalidOperationException("Expected the pinned original positive-loop Endless enemy sin.");
            int loop = run.GetLoopIndex();
            int distance = run.GetCurrentDistance();
            int length = save.GetRunLength();
            string? encounter = save.GetCurrentScenarioData()?.name;
            if (AllWaves && (ControlledFirstLoop || save.InTestScenario || SaveManager.WasDevCheatUsed ||
                save.GetRunType() != RunType.Class || loop != 1 || distance != 0 || encounter != OpeningScenario))
                throw new InvalidOperationException("Expected the original post-victory Endless opening for the six-wave witness.");
            if (ControlledNextRing && (!controlledAdvanceApplied || loop != 1 || distance != ControlledDistance || string.IsNullOrEmpty(encounter)))
                throw new InvalidOperationException("The controlled Endless advance did not reach the requested original battle.");
            if (ControlledLoop2 && (!controlledAdvanceApplied || loop != 2 || distance != 0 || string.IsNullOrEmpty(encounter)))
                throw new InvalidOperationException("The controlled Endless loop did not reach its second-loop opening.");
            var balance = save.GetBalanceData().GetEndlessScalingData();
            scenario = new
            {
                OwnerId, OwnerKey, Loop = loop, Distance = distance, RunLength = length,
                Scenario = encounter, ControlledFirstLoop,
                Attack = balance.GetAttackScaling(loop, distance, length),
                Health = balance.GetHealthScaling(loop, distance, length),
                Status = balance.GetStatusScaling(loop, distance, length),
                BuffDamage = balance.GetCardEffectBuffDamageScaling(loop, distance, length),
                Heal = balance.GetCardEffectHealScaling(loop, distance, length),
                Damage = balance.GetCardEffectDamageScaling(loop, distance, length),
                ExcludedSubtypes = effect.GetParamExcludeCharacterSubtypes().Select(subtype => subtype.Key).ToArray()
            };
        }

        [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.GoToNextScenario))]
        private static class ControlledAdvancePatch
        {
            private static void Prefix()
            {
                if (!ControlledRoute || controlledAdvanceApplied) return;
                SaveManager save = AllGameManagers.Instance!.GetSaveManager();
                var run = save.GetRunState();
                if (Application.version != "2.3.1" || typeof(CharacterState).Module.ModuleVersionId != Module || AllWaves ||
                    ControlledNextRing && ControlledLoop2 ||
                    save.PreviewMode || save.InTestScenario || SaveManager.WasDevCheatUsed || !save.IsEndlessRun ||
                    save.GetRunType() != RunType.Class || save.GetGameSequence() != SaveData.GameSequence.DestinationReached ||
                    run.GetLoopIndex() != 1 || run.GetCurrentDistance() != 0 || run.GetNumBattlesCompleted() != save.GetRunLength() ||
                    save.GetCurrentScenarioData()?.name != OpeningScenario)
                    throw new InvalidOperationException("Controlled Endless advance requires the original post-victory first-loop checkpoint.");
                if (ControlledLoop2)
                {
                    save.LoopEndlessRun();
                    var loop = save.GetRunState();
                    if (loop.GetLoopIndex() != 2 || loop.GetCurrentDistance() != 0 || save.GetCurrentScenarioData() == null)
                        throw new InvalidOperationException("The native Endless loop did not produce a second-loop opening.");
                    controlledAdvanceApplied = true;
                    return;
                }
                int distance = ControlledDistance;
                if (distance is not (1 or 2))
                    throw new InvalidOperationException("The controlled Endless witness accepts Ring 2 or Ring 3.");
                var next = save.GetScenarioData(distance);
                if (next == null) throw new InvalidOperationException("The original Endless route has no battle at the requested distance.");

                // The successful native ContinueRun load reaches this callback before resolving the battle intro.
                for (int step = 0; step < distance; step++) save.AdvanceToNextNode();
                if (save.GetCurrentDistance() != distance || save.GetCurrentScenarioData() != next)
                    throw new InvalidOperationException("The native node advance selected an unexpected Endless scenario.");
                controlledAdvanceApplied = true;
            }
        }

        internal static bool Tick(FullBattleTrace trace, CombatManager combat, ManualLogSource log, Action advanceTurn)
        {
            if (scenario == null) throw new InvalidOperationException("The original Endless battle was not armed.");
            if (deployment == null && combat.GetTurnCount() == 0)
            {
                deployment = trace.CaptureDecision();
                AllGameManagers.Instance!.GetSaveManager().MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 1) return false;
            var firstDecision = trace.CaptureDecision();
            if (ControlledRoute && (Waves.Count != 1 || !Waves[0].PhaseAdvanced || firstDecision.Spawn.Phase != 1))
                throw new InvalidOperationException("Expected the first scheduled birth wave in the controlled Endless battle.");
            var waveBirths = ControlledRoute ? Waves[0].Births : Births;
            string captureName = ControlledLoop2 ? "endless-enemy-scaling-controlled-loop2.mt2f" :
                ControlledNextRing ? "endless-enemy-scaling-controlled-next-ring.mt2f" : "endless-enemy-scaling.mt2f";
            string boundary = ControlledLoop2 ? "OriginalEndlessEnemyScalingControlledLoop2" :
                ControlledNextRing ? "OriginalEndlessEnemyScalingControlledNextRing" : "OriginalEndlessEnemyScaling";
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = boundary,
                GameVersion = Application.version, GameModuleMvid = Module,
                Scenario = scenario, Deployment = deployment, FirstDecision = firstDecision, Births = waveBirths
            });
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, captureName)))
                document.Write(output);
            if (waveBirths.Count == 0 || waveBirths.Any(birth => birth.After == null))
                throw new InvalidOperationException("The original first enemy wave did not complete an Endless birth callback.");
            if (ObserveOuterBoss)
            {
                OuterBossBirthProbe.Write(log);
                if (!OuterBossBirthProbe.HasEndlessScaling)
                    throw new InvalidOperationException("The original outer-boss birth did not record its Endless scaling callback.");
            }
            if (ControlledLoop2)
                log.LogInfo($"ENDLESS-ENEMY-SCALING-CONTROLLED-LOOP2 OBSERVED loop=2 distance=0 callbacks={waveBirths.Count} first={waveBirths[0].AssetKey}");
            else if (ControlledNextRing)
                log.LogInfo($"ENDLESS-ENEMY-SCALING-CONTROLLED-NEXT-RING OBSERVED loop=1 distance={ControlledDistance} callbacks={waveBirths.Count} first={waveBirths[0].AssetKey}");
            else log.LogInfo($"ENDLESS-ENEMY-SCALING OBSERVED original callbacks={Births.Count} first={Births[0].AssetKey}");
            return true;
        }

        internal static bool ObserveWaveDecision(FullBattleTrace trace, ManualLogSource log)
        {
            if (deployment == null)
            {
                deployment = trace.CaptureDecision();
                if (scenario == null || deployment.Spawn.Turn != 0 || deployment.Spawn.Phase != 0 ||
                    deployment.Spawn.Looping || deployment.Spawn.Waves.Count != ScheduledWaveCount)
                    throw new InvalidOperationException("Expected a deployment with six native scheduled waves.");
            }
            else if (Waves.LastOrDefault() is Wave wave && wave.SettledDecision == null)
            {
                var decision = trace.CaptureDecision();
                if (!wave.PhaseAdvanced || decision.Spawn.Phase != wave.Index + 1 || wave.Births.Any(birth => birth.After == null))
                    throw new InvalidOperationException($"The original enemy wave {wave.Index} did not settle as scheduled.");
                wave.SettledDecision = decision;
            }

            if (Waves.Count != ScheduledWaveCount || Waves.Any(wave => wave.SettledDecision == null)) return false;
            trace.CompleteEndTurn(RoomOutcome.Exchanged);
            WriteWaves(log, null);
            return true;
        }

        internal static void WriteWaves(ManualLogSource log, bool? nativeWon)
        {
            int completed = Waves.Count(wave => wave.PhaseAdvanced);
            if (deployment == null || Births.Count == 0)
                throw new InvalidOperationException("The original Endless battle ended before any captured wave births.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalEndlessEnemyScalingWaves", GameVersion = Application.version, GameModuleMvid = Module,
                Scenario = scenario, Deployment = deployment, ScheduledWaves = ScheduledWaveCount, CompletedWaves = completed,
                NativeWon = nativeWon, Waves, UnscheduledBirths = Births.Except(Waves.SelectMany(wave => wave.Births)).ToArray(),
                Turns = FullBattleTrace.Active!.ObservedTurns.Select(turn =>
                    new { turn.Index, turn.Before, turn.Actual, turn.ActualOutcome, turn.RelentlessDecisions }).ToArray()
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "endless-enemy-scaling-waves.mt2f"));
            document.Write(output);
            log.LogInfo($"ENDLESS-ENEMY-SCALING-WAVES OBSERVED waves={completed}/{ScheduledWaveCount} callbacks={Births.Count} nativeWon={nativeWon}");
        }

        private sealed class Wave
        {
            public int Index { get; set; }
            public int NativeTurn { get; set; }
            public bool PhaseAdvanced { get; set; }
            public BattleTurnState? SettledDecision { get; set; }
            public List<Birth> Births { get; } = new List<Birth>();
        }

        [HarmonyPatch(typeof(HeroManager), "DoSpawning")]
        private static class ScheduledWavePatch
        {
            private static readonly FieldInfo Phase = AccessTools.Field(typeof(HeroManager), "spawnPatternPhase");

            private static void Postfix(HeroManager __instance, ref IEnumerator __result)
            {
                if ((AllWaves || ControlledRoute) && deployment != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, __instance);
            }

            private static IEnumerator Observe(IEnumerator native, HeroManager heroes)
            {
                // The native scheduler advances this zero-based phase after its roster has entered.
                int index = (int)Phase.GetValue(heroes);
                Wave? wave = index < ScheduledWaveCount ? new Wave
                {
                    Index = index, NativeTurn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount()
                } : null;
                if (wave != null)
                {
                    if (index != Waves.Count || activeWave != null)
                        throw new InvalidOperationException($"Unexpected native Endless wave phase {index}.");
                    Waves.Add(wave);
                    activeWave = wave;
                }
                try { while (native.MoveNext()) yield return native.Current; }
                finally
                {
                    (native as IDisposable)?.Dispose();
                    if (wave != null)
                    {
                        wave.PhaseAdvanced = (int)Phase.GetValue(heroes) == index + 1;
                        activeWave = null;
                    }
                }
            }
        }

        private sealed class Birth
        {
            public string AssetKey { get; set; } = "";
            public int? UnitId { get; set; }
            public int Room { get; set; }
            public bool FromCard { get; set; }
            public int BaselineAttack { get; set; }
            public int BaselineHealth { get; set; }
            public int RelicAttackBonus { get; set; }
            public float LateAttackMultiplier { get; set; }
            public object? Before { get; set; }
            public object? After { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectEndlessScalingOnSpawn), nameof(RelicEffectEndlessScalingOnSpawn.OnCharacterAdded))]
        private static class ScalingPatch
        {
            private static readonly FieldInfo ScheduledHeroes = AccessTools.Field(typeof(HeroManager), "toProcessHeroes");

            private static void Postfix(CharacterState character, CardState fromCard, ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                if (Enabled && scenario != null && !coreGameManagers.GetSaveManager().PreviewMode &&
                    !character.PreviewMode && character.GetTeamType() == Team.Type.Heroes)
                    __result = Observe(__result, character, fromCard, coreGameManagers);
            }

            private static IEnumerator Observe(IEnumerator native, CharacterState actor, CardState? fromCard, ICoreGameManagers managers)
            {
                CharacterData data = actor.GetSourceCharacterData();
                var relics = managers.GetRelicManager();
                var statusManager = managers.GetStatusEffectManager();
                bool Eligible(string id) => statusManager.GetStatusEffectDataById(id)?.ScalesInEndless() == true;
                object ReadState() => new
                {
                    Attack = actor.GetUnbuffedAttackDamage(), Health = actor.GetHP(), MaximumHealth = actor.GetMaxHP(),
                    Statuses = data.GetStartingStatusEffects().Select(status => new
                    {
                        Id = status.statusId, Authored = status.count, ScalesInEndless = Eligible(status.statusId),
                        Live = actor.GetStatusEffectStacks(status.statusId)
                    }).ToArray(),
                    Triggers = actor.GetTriggers().Select(trigger => new
                    {
                        Kind = trigger.GetTrigger().ToString(),
                        Effects = trigger.GetEffectStates().Select(effect => new
                        {
                            Type = effect.GetCardEffect()?.GetType().Name, RuntimeInt = effect.GetParamInt(),
                            AuthoredInt = effect.GetSourceCardEffectData().GetParamInt(),
                            Statuses = (effect.GetParamStatusEffectStackData() ?? Array.Empty<StatusEffectStackData>())
                                .Select(status => new { Id = status.statusId, Stacks = status.count, ScalesInEndless = Eligible(status.statusId) }).ToArray()
                        }).ToArray()
                    }).ToArray(),
                    Rng = new[] { RngId.Battle, RngId.BattleTest }.Select(id => RngCalibration.Words(RandomManager.GetState(id))).ToArray()
                };
                var birth = new Birth
                {
                    AssetKey = data.GetAssetKey(), UnitId = FullBattleTrace.Active?.UnitId(actor), Room = actor.GetCurrentRoomIndex(),
                    FromCard = fromCard != null, BaselineAttack = actor.GetEndlessBaselineAttack(), BaselineHealth = actor.GetEndlessBaselineHealth(),
                    RelicAttackBonus = relics.GetAdditionalAttackFromRelics(actor),
                    LateAttackMultiplier = relics.GetLateAttackMultiplierFromRelics(actor.GetTeamType(), actor.IsChampion()), Before = ReadState()
                };
                Births.Add(birth);
                if (activeWave is Wave wave &&
                    ScheduledHeroes.GetValue(AllGameManagers.Instance!.GetHeroManager()) is List<CharacterState> roster && roster.Contains(actor))
                    wave.Births.Add(birth);
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    birth.After = ReadState();
                    if (ObserveOuterBoss && actor.IsOuterTrainBoss()) OuterBossBirthProbe.ObserveEndlessScaling(actor, birth);
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
