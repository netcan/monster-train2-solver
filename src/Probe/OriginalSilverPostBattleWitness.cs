#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSilverPostBattleWitness
    {
        private const string SilverT1 = "50198df9-788c-4618-942a-57361c585a57";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<string?> CallbackScenarioIds = new List<string?>();
        private static RelicEffectBuffPyreForBossKill? listener;
        private static SoulState? naturalOwner;
        private static ScenarioData? naturalScenario;
        private static Snapshot? beforeCallback, afterCallback;
        private static bool inBattleComplete;
        private static int battleCompletions;
        private static readonly int Tier = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_SILVER_POSTBATTLE") switch
        {
            "1" => 1,
            "2" => 2,
            "3" => 3,
            _ => 0
        };
        internal static bool Enabled => Tier != 0;
        internal static bool NaturalEnabled => Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_SILVER_T1") == "1";

        internal static void AttachBeforePrologue(SaveManager save, RelicManager relics)
        {
            if (naturalOwner != null) return;
            ScenarioData? scenario = save.GetCurrentScenarioData();
            if (Application.version != "2.3.1" || typeof(SaveManager).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetRunType() != RunType.RegionRun ||
                !save.IsDlcInstalled(ShinyShoe.DLC.Railforged) || save.GetGameSequence() != SaveData.GameSequence.BattleIntro ||
                save.GetCurrentDistance() != 0 || save.GetRunState().GetNumBattlesCompleted() != 0 ||
                scenario?.name != "SoulSavior_R0_Battle_Morsels" || scenario.HasOuterTrainBoss() ||
                save.GetAllRelics().Any(owner => owner.GetRelicDataID() == SilverT1))
                throw new InvalidOperationException("Expected the original unopened RegionRun prologue without SilverT1.");
            naturalScenario = scenario;
            (naturalOwner, _) = Attach(save, relics, SilverT1, tier: 1, amount: 5);
        }

        internal static void WriteNextIntro(SaveManager save, string checkpoint, string checkpointSha)
        {
            if (naturalOwner == null || naturalScenario == null || beforeCallback == null || afterCallback == null ||
                battleCompletions != 1 || !CallbackScenarioIds.SequenceEqual(new[] { naturalScenario.GetID() }) ||
                save.GetGameSequence() != SaveData.GameSequence.BattleIntro || save.GetCurrentDistance() != 1 ||
                save.GetCurrentScenarioData()?.name != "SoulSavior_R1_Battle_HealOnShiftHeavy" ||
                !save.GetBattleRecords().Any(record => record.loopIndex == 0 && record.battleIndex == 0 && record.WonBattle()) ||
                !save.GetSelectedSouls().Any(owner => owner.GetRelicDataID() == SilverT1))
                throw new InvalidOperationException("Original SilverT1 victory did not settle into the next BattleIntro.");

            string outcome = ValidateEffect(beforeCallback, afterCallback, "SilverT1", 5);
            var intro = new Snapshot(save);
            if (intro.PersistentAttack != afterCallback.PersistentAttack ||
                intro.Maximum != afterCallback.Maximum || intro.Health != afterCallback.Health ||
                intro.ModifiedBaseAttack != afterCallback.ModifiedBaseAttack)
                throw new InvalidOperationException("The next original BattleIntro did not retain the SilverT1 Pyre state.");

            JObject saved = JObject.Parse(File.ReadAllText(Path.Combine(checkpoint, "saves", "save-regionRun.json")));
            int? savedAttack = saved["relicPersistentState"]?.Value<int?>("pyreDamageIncrease");
            if (savedAttack != intro.PersistentAttack || saved["souls"]?.Count(soul =>
                soul.Value<string>("relicDataID") == SilverT1) != 1)
                throw new InvalidOperationException("The copied native checkpoint is missing the settled Pyre attack or SilverT1 owner.");

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSilverNaturalPostBattle", GameVersion = Application.version, GameModuleMvid = Module,
                Seed = save.GetStartingConditions().Seed,
                Owner = new { Id = naturalOwner.GetRelicDataID(), Key = naturalOwner.GetAssetName(),
                    Effects = naturalOwner.GetEffects().Select(effect => effect.GetType().Name).ToArray() },
                Won = true, CompletedScenarioId = naturalScenario.GetID(), CompletedScenario = naturalScenario.name,
                BattleCompletions = battleCompletions, CallbackScenarioIds, Outcome = outcome,
                BeforeCallback = beforeCallback, AfterCallback = afterCallback, NextIntro = intro,
                NextScenario = save.GetCurrentScenarioData()!.name, NextDistance = save.GetCurrentDistance(),
                Checkpoint = checkpoint, CheckpointSha = checkpointSha, SavedPersistentAttack = savedAttack
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "original-silver-natural-postbattle.mt2f"));
            archive.Write(stream);
        }

        internal static void Capture(AllGameManagers managers)
        {
            (string ownerId, int amount) = Tier switch
            {
                1 => (SilverT1, 5),
                2 => ("354e6614-8b05-4933-bd24-97a41fc63061", 7),
                3 => ("44e74028-4548-4f36-911d-cdf4dd821511", 10),
                _ => throw new InvalidOperationException("Expected a stock Silver tier.")
            };
            string ownerKey = "SilverT" + Tier;
            SaveManager save = managers.GetSaveManager();
            RelicManager relics = managers.GetRelicManager();
            if (Application.version != "2.3.1" || typeof(SaveManager).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetRunType() != RunType.RegionRun || !save.IsDlcInstalled(ShinyShoe.DLC.Railforged) ||
                save.GetGameSequence() != SaveData.GameSequence.BattleIntro || save.GetCurrentDistance() < 1 ||
                save.GetAllRelics().Any(owner => owner.GetRelicDataID() == ownerId ||
                    owner.GetEffects().Any(effect => effect is ITurnPhaseEndOfBattleRelicEffect)))
                throw new InvalidOperationException("Expected an isolated original RegionRun with an earlier victory and no other end-of-battle relic.");

            var completed = save.GetBattleRecords()
                .Where(record => record.loopIndex == 0 && record.battleIndex < save.GetCurrentDistance() && record.WonBattle() &&
                    save.GetRunState().GetScenarioIndex(record.battleIndex) == record.scenarioIndex)
                .Select(record => new { Record = record, Scenario = save.GetScenarioData(record.battleIndex) })
                .LastOrDefault(row => !row.Scenario.HasOuterTrainBoss())
                ?? throw new InvalidOperationException("The original RegionRun checkpoint has no completed ordinary scenario.");

            var (owner, buff) = Attach(save, relics, ownerId, Tier, amount);

            var before = new Snapshot(save);
            relics.ApplyEndOfBattleRelicEffects(null);
            var afterNull = new Snapshot(save);
            relics.ApplyEndOfBattleRelicEffects(completed.Scenario);
            var afterVictory = new Snapshot(save);
            listener = null;
            string scenarioId = completed.Scenario.GetID();
            if (!CallbackScenarioIds.SequenceEqual(new[] { null, scenarioId }) ||
                !JToken.DeepEquals(JObject.FromObject(before), JObject.FromObject(afterNull)))
                throw new InvalidOperationException("Original end-of-battle dispatcher or null control diverged.");

            string outcome = ValidateEffect(before, afterVictory, ownerKey, amount);
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSilverPostBattleControlledDispatch", GameVersion = Application.version, GameModuleMvid = Module,
                Owner = new { Id = owner.GetRelicDataID(), Key = owner.GetAssetName(), Tier,
                    Effects = owner.GetEffects().Select(effect => effect.GetType().Name).ToArray(),
                    Attack = buff.GetParamInt(), Health = buff.GetParamInt2(), OnlyMiniboss = buff.GetParamBool() },
                Seed = save.GetStartingConditions().Seed, CheckpointDistance = save.GetCurrentDistance(),
                CompletedVictory = new { Id = scenarioId, Key = completed.Scenario.name, Index = completed.Record.battleIndex,
                    Won = completed.Record.WonBattle(), OuterBoss = completed.Scenario.HasOuterTrainBoss() },
                CallbackScenarioIds, Outcome = outcome,
                Before = before, AfterNull = afterNull, AfterVictory = afterVictory
            });
            using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "original-silver-postbattle.mt2f"));
            archive.Write(file);
        }

        private static (SoulState Owner, RelicEffectData Buff) Attach(SaveManager save, RelicManager relics, string ownerId, int tier, int amount)
        {
            string ownerKey = "SilverT" + tier;
            SoulData soul = save.GetAllGameData().FindSoulData(ownerId)
                ?? throw new InvalidOperationException("Original " + ownerKey + " soul is missing.");
            if (soul.name != ownerKey || soul.GetTierLevel() != tier || soul.GetSoulType() != SoulData.SoulType.Artifact ||
                soul.DisallowInDeploymentPhase || soul.GetEffects() is not [var buff, var reward] ||
                buff.GetEffectClassName() != nameof(RelicEffectBuffPyreForBossKill) ||
                buff.GetParamInt() != amount || buff.GetParamInt2() != amount || buff.GetParamBool() || buff.GetEffectConditions().Count != 0 ||
                reward.GetEffectClassName() != nameof(RelicEffectAddPostBattleRewardOnBossDeath))
                throw new InvalidOperationException("The installed original " + ownerKey + " callback payload changed.");
            save.AddSelectedSoul(soul);
            SoulState owner = save.GetSelectedSouls().Single(item => item.GetRelicDataID() == ownerId);
            SoulHelper.AttachSoulEffect(owner, playerInitiated: false, relics, save);
            if (!SoulHelper.GetIsSoulEffectAttached(owner, save, out _) || owner.GetEffects() is not
                [RelicEffectBuffPyreForBossKill active, RelicEffectAddPostBattleRewardOnBossDeath] || active.CanApplyInPreviewMode)
                throw new InvalidOperationException("Native " + ownerKey + " soul did not settle its two ordered effects.");
            listener = active;
            return (owner, buff);
        }

        private static string ValidateEffect(Snapshot before, Snapshot after, string ownerKey, int amount)
        {
            uint[] battle = before.Rng["Battle"]!.ToObject<uint[]>()!;
            RngDraw expected = new UnityRng(battle[0], battle[1], battle[2], battle[3]).Range(0, 2);
            var successor = JToken.FromObject(new[] { expected.State.S0, expected.State.S1, expected.State.S2, expected.State.S3 });
            int attackDelta = expected.Value == 0 ? amount : 0;
            int healthDelta = expected.Value == 0 ? 0 : amount;
            if (!JToken.DeepEquals(successor, after.Rng["Battle"]) ||
                !before.Rng.Properties().Where(property => property.Name != "Battle")
                    .All(property => JToken.DeepEquals(property.Value, after.Rng[property.Name])) ||
                after.PersistentAttack != before.PersistentAttack + attackDelta ||
                after.Health != before.Health + healthDelta || after.Maximum != before.Maximum + healthDelta ||
                after.ModifiedBaseAttack != before.ModifiedBaseAttack)
                throw new InvalidOperationException("Native " + ownerKey + " stat or Battle RNG delta differed from its callback.");
            return expected.Value == 0 ? "PersistentAttack" : "CurrentAndMaxHealth";
        }

        private sealed class Snapshot(SaveManager save)
        {
            public int Health { get; } = save.GetTowerHP();
            public int Maximum { get; } = save.GetMaxTowerHP();
            public int PersistentAttack { get; } = save.GetRelicPersistentState().GetPyreDamageIncrease();
            public int BaseAttackQuery { get; } = save.GetBasePyreAttack();
            public int ModifiedBaseAttack { get; } = save.GetModifiedPyreBaseAttack();
            public JObject Rng { get; } = new JObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .Where(id => id is not RngId.NonDeterministic and not RngId.Chatter)
                .Select(id => new JProperty(id.ToString(), JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id))))));
        }

        [HarmonyPatch(typeof(RelicEffectBuffPyreForBossKill), nameof(RelicEffectBuffPyreForBossKill.ApplyEffectEndOfBattle))]
        private static class ObserveCallback
        {
            private static bool Prepare() => Enabled || NaturalEnabled;
            private static void Prefix(RelicEffectBuffPyreForBossKill __instance, ScenarioData? completedScenario, ICoreGameManagers coreGameManagers, out bool __state)
            {
                __state = false;
                if (!ReferenceEquals(__instance, listener)) return;
                CallbackScenarioIds.Add(completedScenario?.GetID());
                __state = inBattleComplete && completedScenario?.GetID() == naturalScenario?.GetID();
                if (__state) beforeCallback = new Snapshot(coreGameManagers.GetSaveManager());
            }

            private static void Postfix(ICoreGameManagers coreGameManagers, bool __state)
            {
                if (__state) afterCallback = new Snapshot(coreGameManagers.GetSaveManager());
            }
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.BattleComplete))]
        private static class ObserveNaturalCompletion
        {
            private static bool Prepare() => NaturalEnabled;
            private static void Prefix(SaveManager __instance, out bool __state)
            {
                __state = naturalOwner != null && !__instance.PreviewMode && __instance.GetCurrentDistance() == 0 &&
                    __instance.GetCurrentScenarioData()?.GetID() == naturalScenario?.GetID();
                if (__state) inBattleComplete = true;
            }

            private static void Postfix(bool __state)
            {
                if (!__state) return;
                inBattleComplete = false;
                battleCompletions++;
            }
        }
    }
}
#endif
