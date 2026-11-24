#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBattleSetupWitness
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string? Mode = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_BATTLE_SETUP");
        private static readonly string[] OriginalIds = { RelicModel.ChampionReplacementId, RelicModel.ForceTrialId };
        internal static bool Enabled => Mode is "Stock" or "Modified";
        internal static bool Modified => Mode == "Modified";
        private static string[] SelectedIds => Modified ? OriginalIds : Array.Empty<string>();
        private static CardData[] championPool = Array.Empty<CardData>();
        private static TrialSample? trialBefore, trialIntro;

        internal static void Select(RunSetupScreen screen, SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(SaveManager).Module.ModuleVersionId != Module || save.PreviewMode)
                throw new InvalidOperationException("Expected pinned original 2.3.1 Run Setup.");
            RelicEffectData replacement = Effect(RelicModel.ChampionReplacementId, "ChampionReplacement", nameof(RelicEffectReplaceChampion));
            RelicEffectData trial = Effect(RelicModel.ForceTrialId, "TimeForTrials", nameof(RelicEffectModifyTrial));
            if (!trial.GetParamBool() || replacement.GetParamCardPool() is not { } pool)
                throw new InvalidOperationException("Original trial lock or champion pool changed.");
            championPool = pool.GetAllChoices(new List<CardData>()).ToArray();
            if (championPool.Length == 0)
                throw new InvalidOperationException("Original replacement champion pool is empty.");

            AccessTools.DeclaredMethod(typeof(RunSetupScreen), "HandleMutatorDialogClosed")
                .Invoke(screen, new object[] { SelectedIds });

            RelicEffectData Effect(string id, string key, string type)
            {
                MutatorData owner = save.GetAllGameData().FindMutatorData(id)
                    ?? throw new InvalidOperationException("Missing original Run Setup owner " + key);
                if (owner.name != key || owner.DisallowInDeploymentPhase || !save.IsDlcAvailableWhenStartingRun(owner.GetRequiredDLC()) ||
                    owner.GetEffects() is not [var effect] || effect.GetEffectClassName() != type || effect.GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Changed original Run Setup owner " + key);
                return effect;
            }
        }

        internal static void ConfirmMutatorShowcase(RewardScreen screen) =>
            OriginalHandAndEmberWitness.ConfirmMutatorShowcase(screen, OriginalIds);

        internal static void Capture(AllGameManagers managers, FullBattleTrace trace)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()
                ?? throw new InvalidOperationException("Missing original deployment combat manager.");
            var starting = save.GetStartingConditions();
            ScenarioData scenario = save.GetCurrentScenarioData()
                ?? throw new InvalidOperationException("Missing original opening scenario.");
            string[] mutators = save.GetMutators().Select(owner => owner.GetRelicDataID()).ToArray();
            if (save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 0 || combat.GetTurnCount() != 0 ||
                !combat.IsPlacementPhase || scenario.GetEnemyRelicData().Length != 0 ||
                save.GetRunType() != (Modified ? RunType.Custom : RunType.Class) || !mutators.SequenceEqual(SelectedIds) ||
                trialBefore == null || trialIntro == null || trialBefore.Id != trialIntro.Id || trialBefore.SinId != trialIntro.SinId ||
                trialBefore.Enabled || trialIntro.Enabled != Modified || trialIntro.Locked != Modified)
                throw new InvalidOperationException("Native opening did not settle the expected ordinary trial and Run Setup owners.");

            CardState champion = save.GetDeckState().Single(card => card.IsChampionCard());
            var definition = save.GetAllGameData().FindCardData(champion.GetCardDataID())
                ?? throw new InvalidOperationException("Missing original deck champion definition.");
            string championClass = champion.GetLinkedClassID();
            string? replacementClass = save.GetRelicPersistentState().GetReplacementMainClassId();
            var heroes = managers.GetHeroManager() ?? throw new InvalidOperationException("Missing native enemy roster.");
            var cards = managers.GetCardManager() ?? throw new InvalidOperationException("Missing native deployment hand.");
            string? activeSin = heroes.GetHeroBlessings()
                .SingleOrDefault(sin => sin.GetRelicDataID() == trialIntro.SinId)?.GetRelicDataID();
            if ((Modified ? championClass == starting.ClassId || championClass == starting.SubclassId ||
                    championClass != replacementClass || !championPool.Contains(definition) || activeSin != trialIntro.SinId
                    : championClass != starting.ClassId || !string.IsNullOrEmpty(replacementClass) || activeSin != null) ||
                !cards.GetHand().Contains(champion))
                throw new InvalidOperationException("The original deck champion or active trial Sin did not appear in deployment.");

            BattleTurnState deployment = trace.CaptureDecision();
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalBattleSetup", GameVersion = Application.version, GameModuleMvid = Module,
                Mode, starting.Seed, Scenario = scenario.name, ScenarioId = scenario.GetID(), starting.ClassId, starting.SubclassId,
                ReplacementClassId = replacementClass, Mutators = mutators,
                DeckChampion = new { Id = champion.GetCardDataID(), Key = definition.name, ClassId = championClass,
                    InstanceId = trace.CardId(champion), Upgrades = champion.GetCardStateModifiers().GetCardUpgrades()
                        .Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray() },
                ChampionPool = championPool.Select(card => new { Id = card.GetID(), ClassId = card.GetLinkedClass()?.GetID() }).ToArray(),
                TrialBefore = trialBefore, TrialIntro = trialIntro, ActiveTrialSinId = activeSin, Deployment = deployment
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "original-battle-setup.mt2f"));
            archive.Write(stream);
        }

        private sealed class TrialSample(TrialData trial, bool enabled, bool locked)
        {
            public string Id { get; } = trial.GetID();
            public string SinId { get; } = trial.Sin.GetID();
            public bool Enabled { get; } = enabled;
            public bool Locked { get; } = locked;
        }

        [HarmonyPatch(typeof(BattleIntroScreen), "SetupScenarioTrial")]
        private static class ObserveTrialSetup
        {
            private static bool Prepare() => Enabled;

            private static void Prefix(ScenarioData currentScenario, out bool __state)
            {
                SaveManager? save = AllGameManagers.Instance?.GetSaveManager();
                __state = championPool.Length > 0 && save?.GetCurrentDistance() == 0 && currentScenario.GetEnemyRelicData().Length == 0;
                if (__state && save != null && trialBefore == null)
                    trialBefore = new TrialSample(Trial(save), save.GetTrialEnabled(), false);
            }

            private static void Postfix(BattleIntroScreen __instance, bool __state)
            {
                if (!__state) return;
                SaveManager save = AllGameManagers.Instance!.GetSaveManager();
                var lockRoot = (GameObject)AccessTools.Field(typeof(BattleIntroScreen), "trialLockedRoot").GetValue(__instance);
                trialIntro = new TrialSample(Trial(save), save.GetTrialEnabled(),
                    lockRoot.activeSelf && __instance.GetTrialToggleButton().IsSelectionStateDisabled());
            }

            private static TrialData Trial(SaveManager save) => save.GetTrialData(onlyIfEnabled: false)
                ?? throw new InvalidOperationException("The original Ring 1 scenario has no ordinary trial.");
        }
    }
}
#endif
