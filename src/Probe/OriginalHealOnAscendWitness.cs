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
    internal static class OriginalHealOnAscendWitness
    {
        internal const string FileName = "heal-on-ascend.mt2f";
        private const string OwnerId = "804934f0-cfa6-4c17-badf-8c0f918c9953";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<Observation> observations = new List<Observation>();
        private static string trialId = "";
        private static SubtypeData[] excludedSubtypes = Array.Empty<SubtypeData>();
        private static int healAmount;
        private static int woundedActor;
        private static bool startingDamage;
        private static BattleTurnState? beforeTurn;
        private static string? failure;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_HEAL_ON_ASCEND") == "1";
        internal static bool Acquired { get; private set; }

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(RelicEffectHealEnemyOnAscend).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 1 || save.GetCovenants().Count != 0 ||
                save.GetMutators().Count != 0 || save.GetTrialEnabled())
                throw new InvalidOperationException("Expected an original Ring 2 checkpoint before the Ascend healing trial.");
            TrialData? current = save.GetTrialData(onlyIfEnabled: false);
            TrialData? trial = current?.Sin?.GetID() == OwnerId ? current : save.GetScenarioData(save.GetCurrentDistance())
                .GetTrialDataList()?.TrialsData.FirstOrDefault(item => item.Sin?.GetID() == OwnerId && item.RewardList.Count > 0);
            if (trial == null)
                throw new InvalidOperationException("The original scenario does not offer the Ascend healing trial.");
            if (trial.Sin.DisallowInDeploymentPhase || trial.Sin.GetEffects() is not [var effect] ||
                effect.GetEffectClassName() != nameof(RelicEffectHealEnemyOnAscend) ||
                effect.GetParamInt() != 150 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("The original Ascend healing trial payload changed.");
            excludedSubtypes = effect.GetParamExcludeCharacterSubtypes();
            healAmount = effect.GetParamInt();
            trialId = trial.GetID();
            if (trial != current) save.SetupTrial(trialId, save.GetCurrentDistance(), 0);
            save.SetTrialEnabled(true);
            if (save.GetTrialSin()?.GetID() != OwnerId)
                throw new InvalidOperationException("Native active trial did not resolve to the Ascend healing owner.");
            Acquired = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            CombatManager combat = managers.GetCombatManager();
            SaveManager save = managers.GetSaveManager();
            int turn = combat.GetTurnCount();
            if (!Acquired || save.PreviewMode) return false;
            if (failure != null) throw new InvalidOperationException(failure);
            if (turn == 0)
            {
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }
            if (turn == 1 && !startingDamage)
            {
                var actors = new List<CharacterState>();
                managers.GetRoomManager().GetRoom(0).AddCharactersToList(actors, Team.Type.Heroes);
                CharacterState target = actors.First(actor => actor.IsAlive && !actor.IsDestroyed && actor.GetHP() > 1 &&
                    actor.GetHP() == actor.GetMaxHP() && actor.GetDamageToTarget(1, null, null, out _) == 1 &&
                    !excludedSubtypes.Any(actor.GetHasSubtype));
                woundedActor = trace.UnitId(target);
                startingDamage = true;
                save.StartCoroutine(Wound());
                return false;

                IEnumerator Wound()
                {
                    int before = target.GetHP();
                    yield return combat.ApplyDamageToTarget(1, target, new CombatManager.ApplyDamageToTargetParameters());
                    yield return combat.RunTriggerQueue();
                    if (target.GetHP() == before - 1) beforeTurn = trace.CaptureDecision();
                    else failure = "The original Ascend trial control did not lose one health.";
                }
            }
            if (beforeTurn == null) return false;
            if (turn == 1)
            {
                advanceTurn();
                return false;
            }
            if (turn != 2 || !observations.Any(row => row.ActorId == woundedActor && row.Before == row.Maximum - 1 &&
                row.After == row.Maximum && row.Team == nameof(Team.Type.Heroes) && row.Room == 1))
                throw new InvalidOperationException("Original Ascend trial did not heal the wounded actor immediately after movement.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalHealOnAscend", GameVersion = Application.version, GameModuleMvid = Module,
                OwnerId, TrialId = trialId, HealAmount = healAmount, ExcludedSubtypes = excludedSubtypes.Select(subtype => subtype.Key).ToArray(),
                WoundedActor = woundedActor, Before = beforeTurn, Observations = observations, Actual = trace.CaptureDecision()
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            archive.Write(stream);
            return true;
        }

        private sealed class Observation(int actorId, string team, int room, int maximum, int before, int after)
        {
            public int ActorId { get; } = actorId;
            public string Team { get; } = team;
            public int Room { get; } = room;
            public int Maximum { get; } = maximum;
            public int Before { get; } = before;
            public int After { get; } = after;
        }

        [HarmonyPatch(typeof(RelicEffectHealEnemyOnAscend), nameof(RelicEffectHealEnemyOnAscend.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectHealEnemyOnAscend __instance, CharacterTriggerRelicEffectParams relicEffectParams,
                ref IEnumerator __result)
            {
                if (!Enabled || beforeTurn == null || __instance.GetRelicDataId() != OwnerId ||
                    FullBattleTrace.Active is not FullBattleTrace trace ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    relicEffectParams.trigger != CharacterTriggerData.Trigger.PostAscension) return;
                __result = Record(__result, trace, relicEffectParams.characterState);
            }

            private static IEnumerator Record(IEnumerator native, FullBattleTrace trace, CharacterState actor)
            {
                int before = actor.GetHP();
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                observations.Add(new Observation(trace.UnitId(actor), actor.GetTeamType().ToString(),
                    actor.GetCurrentRoomIndex(), actor.GetMaxHP(), before, actor.GetHP()));
            }
        }
    }
}
#endif
