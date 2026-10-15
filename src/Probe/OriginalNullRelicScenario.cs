using System;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalNullRelicScenario
    {
        internal const string Id = "815a25e7-2fed-4cb2-a568-79af12cd7116";
        internal static bool Enabled => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").StartsWith("original-null-relic", StringComparison.Ordinal);
        internal static bool Baseline => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "original-null-relic-baseline";
        internal static bool Acquired { get; private set; }
        internal static object? Evidence { get; private set; }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var definition = save.GetAllGameData().GetAllCollectableRelicData().Single(owner => owner.GetID() == Id);
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == Id) ||
                definition.name != "StarterWurmRelic" || definition.GetEffects().Count != 1 || definition.GetEffects()[0].GetEffectClassName() != "RelicEffectNull")
                throw new InvalidOperationException("Original null owner or pre-battle acquisition changed.");
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            JObject Snapshot() => JObject.FromObject(new { Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(),
                Gold = save.GetGold(), Health = save.GetTowerHP(), Maximum = save.GetMaxTowerHP(), Frame = UnityEngine.Time.frameCount, Rng = Rng() });
            var before = Snapshot(); JObject? owned = null, removed = null; string[] interfaces = typeof(RelicEffectNull).GetInterfaces().Select(type => type.Name).OrderBy(name => name).ToArray();
            if (!Baseline)
            {
                save.AddRelic(definition); var runtime = save.GetCollectedRelics().Single(owner => owner.GetRelicDataID() == Id);
                if (runtime.GetEffects().Single().GetType() != typeof(RelicEffectNull)) throw new InvalidOperationException("Native no-op runtime changed.");
                owned = Snapshot(); save.RemoveRelic(runtime); removed = Snapshot(); save.AddRelic(definition);
                if (!JToken.DeepEquals(before, owned) || !JToken.DeepEquals(before, removed) || !JToken.DeepEquals(before, Snapshot()))
                    throw new InvalidOperationException("Null owner acquisition/removal changed recorded deck/resources/frame/RNG.");
            }
            Evidence = new { Id, Key = definition.name, Type = "RelicEffectNull", Interfaces = interfaces,
                Baseline, Before = before, Owned = owned, Removed = removed, Reacquired = Snapshot(),
                Present = save.GetCollectedRelics().Count(owner => owner.GetRelicDataID() == Id) };
            Acquired = true; log.LogInfo("ORIGINAL-NULL-RELIC-ACQUIRED baseline=" + Baseline + "; unchanged original owner, no invented activation.");
        }
    }
}
