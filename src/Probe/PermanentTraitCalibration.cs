using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class PermanentTraitCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_PERMANENT_TRAITS") != "1") return;
            var managers = AllGameManagers.Instance!; var save = managers.GetSaveManager();
            var definition = save.GetAllGameData().FindCardData("fb0683b1-20ac-4dd2-b580-14b502e182f4")!;
            var initial = JToken.FromObject(trace.CaptureDecision()); int frame = UnityEngine.Time.frameCount;
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                result["Unity"] = JToken.FromObject(UnityEngine.Random.state); return result;
            }
            var rng = Rng(); var rows = new List<object>();
            CardTraitData Data(string name, int mode, int parameter, bool removable = true)
            {
                var data = new CardTraitData(); data.Setup(name);
                AccessTools.Field(typeof(CardTraitData), "stackMode").SetValue(data, (CardTraitData.StackMode)mode);
                AccessTools.Field(typeof(CardTraitData), "paramInt").SetValue(data, parameter);
                AccessTools.Field(typeof(CardTraitData), "traitIsRemovable").SetValue(data, removable);
                return data;
            }
            CardInstanceState State(CardState card) => CardModifierProbe.Capture(new[] { card }, _ => 77).Single();
            void Apply(string group, CardState card, CardUpgradeState upgrade)
            {
                var before = State(card); var payload = CardModifierProbe.Upgrade(upgrade, false, true);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                rows.Add(new { Group = group, Before = before, Upgrade = payload, Actual = State(card) });
            }
            foreach (string trait in new[] { CardRetainModel.Trait, CardCorruptStateModel.Trait,
                "CardTraitExhaustState", "CardTraitDistinct", "CardTraitEphemeral" })
            foreach (int mode in new[] { 0, 1, 2, 3 })
            foreach (bool present in new[] { false, true })
            foreach (bool avoid in new[] { false, true })
            {
                var card = new CardState(); card.Setup(definition, save);
                if (present) card.AddTrait(Data(trait, 0, 1));
                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (var retained in card.GetTraitStates().OfType<CardTraitRetain>())
                        AccessTools.Field(typeof(CardTraitRetain), "retained").SetValue(retained, true);
                    foreach (var consumed in card.GetTraitStates().OfType<CardTraitExhaustState>())
                    {
                        AccessTools.Field(typeof(CardTraitExhaustState), "isExhausted").SetValue(consumed, true);
                        AccessTools.Field(typeof(CardTraitExhaustState), "exhaustedCard").SetValue(consumed, card);
                    }
                    var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAvoidClobberingExistingTraits(avoid);
                    upgrade.GetTraitDataUpgrades().Add(Data(trait, mode, 3));
                    Apply(trait + "/" + mode + "/" + present + "/" + avoid, card, upgrade);
                }
            }
            foreach (string trait in new[] { "CardTraitEphemeral", "CardTraitCopyOnPlay" })
            foreach (bool temporary in new[] { false, true })
            foreach (bool removable in new[] { false, true })
            foreach (bool avoid in new[] { false, true })
            {
                var card = new CardState(); card.Setup(definition, save);
                var data = Data(trait, 0, 1, removable);
                if (temporary) card.AddTemporaryTrait(data, managers.GetCoreManagers()); else card.AddTrait(data);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAvoidClobberingExistingTraits(avoid);
                upgrade.GetRemoveTraitUpgrades().Add(trait);
                Apply("Remove/" + trait + "/" + temporary + "/" + removable + "/" + avoid, card, upgrade);
            }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(initial, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged) throw new InvalidOperationException("Permanent trait calibration changed live decision/RNG/frame.");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rng, RngAfter = Rng(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "permanent-trait-calibration.mt2f"))) fixture.Write(stream);
        }
    }
}
