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
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<string?> CallbackScenarioIds = new List<string?>();
        private static RelicEffectBuffPyreForBossKill? listener;
        private static readonly int Tier = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_SILVER_POSTBATTLE") switch
        {
            "1" => 1,
            "2" => 2,
            "3" => 3,
            _ => 0
        };
        internal static bool Enabled => Tier != 0;

        internal static void Capture(AllGameManagers managers)
        {
            (string ownerId, int amount) = Tier switch
            {
                1 => ("50198df9-788c-4618-942a-57361c585a57", 5),
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

            SoulData soul = save.GetAllGameData().FindSoulData(ownerId)
                ?? throw new InvalidOperationException("Original " + ownerKey + " soul is missing.");
            if (soul.name != ownerKey || soul.GetTierLevel() != Tier || soul.GetSoulType() != SoulData.SoulType.Artifact ||
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

            uint[] battle = before.Rng["Battle"]!.ToObject<uint[]>()!;
            RngDraw expected = new UnityRng(battle[0], battle[1], battle[2], battle[3]).Range(0, 2);
            var successor = JToken.FromObject(new[] { expected.State.S0, expected.State.S1, expected.State.S2, expected.State.S3 });
            int attackDelta = expected.Value == 0 ? amount : 0;
            int healthDelta = expected.Value == 0 ? 0 : amount;
            if (!JToken.DeepEquals(successor, afterVictory.Rng["Battle"]) ||
                !before.Rng.Properties().Where(property => property.Name != "Battle")
                    .All(property => JToken.DeepEquals(property.Value, afterVictory.Rng[property.Name])) ||
                afterVictory.PersistentAttack != before.PersistentAttack + attackDelta ||
                afterVictory.Health != before.Health + healthDelta || afterVictory.Maximum != before.Maximum + healthDelta ||
                afterVictory.ModifiedBaseAttack != before.ModifiedBaseAttack)
                throw new InvalidOperationException("Native " + ownerKey + " stat or Battle RNG delta differed from its callback.");

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSilverPostBattleControlledDispatch", GameVersion = Application.version, GameModuleMvid = Module,
                Owner = new { Id = owner.GetRelicDataID(), Key = owner.GetAssetName(), Tier = soul.GetTierLevel(),
                    Effects = owner.GetEffects().Select(effect => effect.GetType().Name).ToArray(),
                    Attack = buff.GetParamInt(), Health = buff.GetParamInt2(), OnlyMiniboss = buff.GetParamBool() },
                Seed = save.GetStartingConditions().Seed, CheckpointDistance = save.GetCurrentDistance(),
                CompletedVictory = new { Id = scenarioId, Key = completed.Scenario.name, Index = completed.Record.battleIndex,
                    Won = completed.Record.WonBattle(), OuterBoss = completed.Scenario.HasOuterTrainBoss() },
                CallbackScenarioIds, Outcome = expected.Value == 0 ? "PersistentAttack" : "CurrentAndMaxHealth",
                Before = before, AfterNull = afterNull, AfterVictory = afterVictory
            });
            using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "original-silver-postbattle.mt2f"));
            archive.Write(file);
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
            private static bool Prepare() => Enabled;
            private static void Prefix(RelicEffectBuffPyreForBossKill __instance, ScenarioData? completedScenario)
            {
                if (ReferenceEquals(__instance, listener)) CallbackScenarioIds.Add(completedScenario?.GetID());
            }
        }
    }
}
#endif
