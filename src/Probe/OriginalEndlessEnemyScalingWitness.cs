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
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEndlessEnemyScalingWitness
    {
        private const string OwnerId = "b342ef1d-306b-4d40-aa9d-782bbae58af3";
        private const string OwnerKey = "Enemy_EndlessScaling";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ENDLESS_ENEMY_SCALING_WITNESS") == "1";
        internal static bool ControlledFirstLoop { get; private set; }
        private static object? scenario;
        private static BattleTurnState? deployment;
        private static readonly List<Birth> Births = new List<Birth>();

        internal static void BeginFirstLoop(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.IsEndlessRun || save.GetRunType() != RunType.Class ||
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
            var balance = save.GetBalanceData().GetEndlessScalingData();
            scenario = new
            {
                OwnerId, OwnerKey, Loop = loop, Distance = distance, RunLength = length,
                Scenario = save.GetCurrentScenarioData()?.name, ControlledFirstLoop,
                Attack = balance.GetAttackScaling(loop, distance, length),
                Health = balance.GetHealthScaling(loop, distance, length),
                Status = balance.GetStatusScaling(loop, distance, length),
                BuffDamage = balance.GetCardEffectBuffDamageScaling(loop, distance, length),
                Heal = balance.GetCardEffectHealScaling(loop, distance, length),
                Damage = balance.GetCardEffectDamageScaling(loop, distance, length),
                ExcludedSubtypes = effect.GetParamExcludeCharacterSubtypes().Select(subtype => subtype.Key).ToArray()
            };
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
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalEndlessEnemyScaling", GameVersion = Application.version, GameModuleMvid = Module,
                Scenario = scenario, Deployment = deployment, FirstDecision = firstDecision, Births
            });
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "endless-enemy-scaling.mt2f")))
                document.Write(output);
            if (Births.Count == 0 || Births.Any(birth => birth.After == null))
                throw new InvalidOperationException("The original first enemy wave did not complete an Endless birth callback.");
            log.LogInfo($"ENDLESS-ENEMY-SCALING OBSERVED original callbacks={Births.Count} first={Births[0].AssetKey}");
            return true;
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
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    birth.After = ReadState();
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
