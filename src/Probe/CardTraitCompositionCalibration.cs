using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardTraitCompositionCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_TRAIT_COMPOSITION") != "1") return;
            var managers = AllGameManagers.Instance!;
            var definitions = managers.GetSaveManager().GetAllGameData().GetAllCardData();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>());
            JObject Rng()
            {
                var value = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) value[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return value;
            }
            var before = JToken.FromObject(trace.CaptureContext()); var rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            var rows = new List<object>();
            void Observe(string name, string kind, CardState card)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    var input = CardTraitCompositionProbe.Capture(card, true);
                    AccessTools.Method(typeof(CardState), "RefreshCombinedCardTraits").Invoke(card, new object[] { true });
                    rows.Add(new { Name = name, Kind = kind, Pass = pass, Before = input,
                        Actual = CardTraitCompositionProbe.Capture(card, true), Origins = CardTraitCompositionProbe.Origins(card) });
                }
            }
            CardTraitState Trait(CardTraitData data, CardState card)
            {
                var state = (CardTraitState)Activator.CreateInstance(TypeNameCache.GetType(data.GetTraitStateName()));
                state.Setup(data, card); return state;
            }
            foreach (var data in definitions)
            {
                var card = new CardState();
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, data.GetTraits().Select(trait => Trait(trait, card)).ToList());
                Observe(data.GetID(), "DefinitionTraits", card);
            }
            object Clone(object source) => AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(source, null);
            foreach (var original in owned)
            {
                var card = (CardState)Clone(original);
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, CardTraitCompositionProbe.Bases(original).Select(trait => (CardTraitState)Clone(trait)).ToList());
                AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(card, new List<CardTraitState>());
                // Refresh reads these modifier objects; it never modifies their lists.
                Observe(original.GetCardDataID(), "OwnedTraits", card);
            }
            CardTraitData Data(string name, int value)
            {
                var data = new CardTraitData(); data.Setup(name);
                AccessTools.Field(typeof(CardTraitData), "paramInt").SetValue(data, value);
                AccessTools.Field(typeof(CardTraitData), "traitIsRemovable").SetValue(data, true);
                return data;
            }
            var exhaust = Data("CardTraitExhaustState", 2); var purge = Data("CardTraitSelfPurge", 3);
            var piercing = Data("CardTraitIgnoreArmor", 4); var deployment = Data("CardTraitDrawInDeploymentPhase", 5);
            CardTraitData[][] seeds = { Array.Empty<CardTraitData>(), new[] { exhaust }, new[] { exhaust, exhaust }, new[] { exhaust, purge },
                new[] { piercing }, new[] { deployment }, new[] { piercing, exhaust }, new[] { exhaust, piercing, exhaust } };
            CardTraitData[][] additions = { Array.Empty<CardTraitData>(), new[] { exhaust }, new[] { piercing }, new[] { purge, exhaust } };
            string[][] removed = { Array.Empty<string>(), new[] { "CardTraitExhaustState" }, new[] { "CardTraitIgnoreArmor" } };
            (string Old, string New)[][] replacements = { Array.Empty<(string Old, string New)>(), new[] { ("CardTraitExhaustState", "CardTraitIgnoreArmor") },
                new[] { ("CardTraitIgnoreArmor", "CardTraitDrawInDeploymentPhase") },
                new[] { ("CardTraitExhaustState", "CardTraitIgnoreArmor"), ("CardTraitIgnoreArmor", "CardTraitDrawInDeploymentPhase") } };
            int index = 0;
            foreach (var seed in seeds)
            foreach (var upgradeTraits in additions)
            foreach (var exclusions in removed)
            foreach (var changes in replacements)
            {
                var card = new CardState(); var bases = seed.Select(data => Trait(data, card)).ToList();
                foreach (var trait in bases) trait.SetParamInt(trait.GetParamInt() + 7);
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, bases);
                if (index % 2 == 0) card.GetTemporaryCardStateModifiers().AddTemporaryTrait(Trait(exhaust, card));
                var upgrade = new CardUpgradeState(); upgrade.Setup();
                upgrade.GetTraitDataUpgrades().AddRange(upgradeTraits); upgrade.GetRemoveTraitUpgrades().AddRange(exclusions);
                card.GetTemporaryCardStateModifiers().GetCardUpgrades().Add(upgrade);
                foreach (var change in changes)
                    card.GetCardStateModifiers().AddTraitReplacement(TypeNameCache.GetType(change.Old), TypeNameCache.GetType(change.New));
                Observe("controlled-" + index++, "Controlled", card);
            }
            var rngAfter = Rng(); bool unchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureContext()));
            if (!unchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Trait composition calibration changed native context/RNG/frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, ContextUnchanged = unchanged,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-trait-composition-calibration.mt2f")))
                document.Write(stream);
        }
    }
}
