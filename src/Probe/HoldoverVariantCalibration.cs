using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class HoldoverVariantCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_HOLDOVER_VARIANTS") != "1") return;
            var managers = AllGameManagers.Instance!;
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            var definition = all.FindCardData("fb0683b1-20ac-4dd2-b580-14b502e182f4")!;
            var merchant = all.GetAllCardUpgradeData().First(upgrade => upgrade.name == "TraitRetain");
            var initial = JToken.FromObject(trace.CaptureDecision()); int frame = UnityEngine.Time.frameCount;
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                result["Unity"] = JToken.FromObject(UnityEngine.Random.state); return result;
            }
            var rng = Rng(); var rows = new List<object>();
            CardInstanceState State(CardState card, int id = 77) => CardModifierProbe.Capture(new[] { card }, _ => id).Single();
            CardTraitData Trait(string name) { var data = new CardTraitData(); data.Setup(name); return data; }
            void Refresh(CardState card) => AccessTools.Method(typeof(CardState), "RefreshCombinedCardTraits").Invoke(card, new object[] { true });
            void Drain(IEnumerator iterator) { while (iterator.MoveNext()) if (iterator.Current is IEnumerator child) Drain(child); }
            foreach (string variant in new[] { "Permanent", "TemporaryUpgrade", "TemporaryTrait", "BaseReplacement", "TemporaryReplacement" })
            foreach (bool removeOnDiscard in new[] { false, true })
            {
                var card = new CardState(); card.Setup(definition, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(merchant); upgrade.SetRemoveOnDiscard(removeOnDiscard);
                if (variant == "BaseReplacement") card.AddTrait(Trait("CardTraitIgnoreArmor"));
                if (variant == "TemporaryReplacement")
                {
                    upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetRemoveOnDiscard(removeOnDiscard);
                    upgrade.GetTraitDataUpgrades().Add(Trait("CardTraitIgnoreArmor"));
                    card.ApplyTemporaryUpgrade(upgrade, save);
                }
                void Observe(string operation, Action action, CardUpgradeModifier? payload = null, bool played = false, bool recast = false)
                {
                    var before = State(card); action();
                    rows.Add(new { Variant = variant, RemoveOnDiscard = removeOnDiscard, Operation = operation,
                        Before = before, Actual = State(card), Upgrade = payload, WasPlayed = played, Recast = recast });
                }
                switch (variant)
                {
                    case "Permanent": Observe("Permanent", () => card.ApplyPermanentUpgrade(upgrade, save), CardModifierProbe.Upgrade(upgrade, false, true)); break;
                    case "TemporaryUpgrade": Observe("TemporaryUpgrade", () => card.ApplyTemporaryUpgrade(upgrade, save), CardModifierProbe.Upgrade(upgrade, false, true)); break;
                    case "TemporaryTrait": Observe("TemporaryTrait", () => card.AddTemporaryTrait(Trait(CardRetainModel.Trait), managers.GetCoreManagers())); break;
                    default:
                        Observe("Replacement", () => { card.GetCardStateModifiers().AddTraitReplacement(typeof(CardTraitIgnoreArmor), typeof(CardTraitRetain)); Refresh(card); }); break;
                }
                foreach (bool played in new[] { false, true })
                foreach (bool recast in new[] { false, true })
                {
                    foreach (var trait in card.GetTraitStates().OfType<CardTraitRetain>())
                        AccessTools.Field(typeof(CardTraitRetain), "retained").SetValue(trait, false);
                    Observe("DiscardCallback", () => {
                        foreach (var trait in card.GetTraitStates().OfType<CardTraitRetain>().ToArray())
                            Drain(trait.OnCardDiscarded(new CardManager.DiscardCardParams { discardCard = card, wasPlayed = played, isRecastCopy = recast }, managers.GetCoreManagers()));
                    }, played: played, recast: recast);
                }
                // Arm every private state before checking query cache, dirty rebuild,
                // reset, cloning and remove-on-discard. No card joins a live pile.
                void Arm() { foreach (var trait in card.GetTraitStates().OfType<CardTraitRetain>()) AccessTools.Field(typeof(CardTraitRetain), "retained").SetValue(trait, true); }
                Arm(); Observe("Query", () => card.GetTraitStates());
                Observe("Refresh", () => Refresh(card));
                Arm(); Observe("Reset", () => card.ResetTraits());
                Arm();
                var source = State(card);
                var copy = CardManager.CopyCardState(card, save, all, managers.GetCardStatistics());
                rows.Add(new { Variant = variant, RemoveOnDiscard = removeOnDiscard, Operation = "Clone", Before = source,
                    Actual = State(copy, 78), SourceAfter = State(card), Creation = CardGenerationProbe.Creation(definition) });
                Observe("DiscardCleanup", () => card.OnCardDiscarded());
            }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, Rng()) &&
                JToken.DeepEquals(initial, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged) throw new InvalidOperationException("Holdover variant calibration changed live decision/RNG/frame.");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                ContextUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rng, RngAfter = Rng(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "holdover-variants-calibration.mt2f"))) fixture.Write(stream);
        }
    }
}
