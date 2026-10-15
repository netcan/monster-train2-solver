using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignPostBattleCalibration
    {
        private static readonly string[] Ids = { "5cad6ad0-769b-46d3-9464-514b8e74190a", "202a7153-bfe9-4da9-be3e-ddcf8c195354" };
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_POST_BATTLE") != "1") return;
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var relics = managers.GetRelicManager();
            var owners = Ids.Select(id => data.GetAllCollectableRelicData().Single(owner => owner.GetID() == id)).ToArray();
            if (save.PreviewMode || save.GetCollectedRelics().Any(owner => Ids.Contains(owner.GetRelicDataID())) ||
                save.GetAllRelics().Any(owner => owner.GetEffects().Any(effect => effect is RelicEffectAddPostBattleReward || effect is RelicEffectAddPostBattleRewardOnBossDeath)))
                throw new InvalidOperationException("Post-battle calibration needs isolated original owners.");
            var rewards = owners.Select(owner => owner.GetEffects().Single().GetParamReward()).ToArray();
            foreach (var reward in rewards)
                if (!(reward is GrantableRewardData)) throw new InvalidOperationException("Post-battle reward requires expanded consumer model: " + reward.GetType().Name + "/" + reward.name);
            object Reward(RewardData reward) => new { Id = reward.GetID(), Key = reward.name, Type = reward.GetType().Name };
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            JObject Snapshot() => JObject.FromObject(new { Relics = save.GetCollectedRelics().Select(owner => owner.GetRelicDataID()).ToArray(),
                Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(), Gold = save.GetGold(), Health = save.GetTowerHP(), Maximum = save.GetMaxTowerHP(),
                Frame = UnityEngine.Time.frameCount, Rng = Rng() });
            JObject before = Snapshot(); var rows = new List<object>();
            var scenarios = data.GetAllScenarioDatas().Where(scenario => !scenario.IsArchived).Take(3).ToArray();
            if (scenarios.Length != 3) throw new InvalidOperationException("Post-battle scenario controls missing.");
            void Remove(CollectableRelicData owner) => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == owner.GetID()));
            void Samples(string phase)
            {
                foreach (var scenario in scenarios)
                foreach (int prefixCount in new[] { 0, 1, 2 })
                foreach (int repetition in new[] { 0, 1 })
                {
                    var list = Enumerable.Repeat((GrantableRewardData)rewards[0], prefixCount).ToList();
                    object rngBefore = Rng(); relics.ApplyPostBattleRelicRewards(scenario, list);
                    rows.Add(new { Phase = phase, Scenario = scenario.GetID(), PrefixCount = prefixCount, Repetition = repetition,
                        Owned = save.GetCollectedRelics().Select(owner => owner.GetRelicDataID()).Where(id => Ids.Contains(id)).ToArray(),
                        RngBefore = rngBefore, RngAfter = Rng(), Actual = list.Select(Reward).ToArray(),
                        OriginalReferences = list.All(reward => rewards.Any(original => ReferenceEquals(original, reward))) });
                }
            }
            try
            {
                Samples("baseline"); save.AddRelic(owners[0]); Samples("void"); save.AddRelic(owners[1]); Samples("both");
                Remove(owners[0]); Samples("mother"); save.AddRelic(owners[0]); Samples("reversed"); Remove(owners[1]); Remove(owners[0]); Samples("removed");
            }
            finally { foreach (var owner in owners) if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == owner.GetID())) Remove(owner); }
            JObject after = Snapshot();
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Post-battle recorded state/frame/RNG changed.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after,
                Owners = owners.Select(owner => new { Id = owner.GetID(), Key = owner.name, DisallowedInPlacementPhase = owner.DisallowInDeploymentPhase,
                    Effects = owner.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                        Conditions = effect.GetEffectConditions().Count, Reward = Reward(effect.GetParamReward()) }).ToArray() }).ToArray(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-post-battle-calibration.mt2f"))) document.Write(stream);
        }
    }
}
