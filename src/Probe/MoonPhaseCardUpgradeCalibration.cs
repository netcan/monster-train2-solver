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
    internal static class MoonPhaseCardUpgradeCalibration
    {
        internal static bool Capturing { get; private set; }
        internal static CardUpgradeModifier Definition(CardTraitData trait)
        {
            var upgrade = new CardUpgradeState(); upgrade.Setup(trait.GetCardUpgradeDataParam());
            upgrade.SetAvoidClobberingExistingTraits(true);
            return CardModifierProbe.Upgrade(upgrade);
        }
        internal static CardMoonUpgradeState State(CardTraitState trait)
        {
            var cached = (CardUpgradeState?)AccessTools.Field(typeof(CardTraitAddCardUpgradeOnMoonPhase), "cachedTraitUpgradeState").GetValue(trait);
            bool active = (bool)AccessTools.Field(typeof(CardTraitAddCardUpgradeOnMoonPhase), "upgradeActive").GetValue(trait);
            return new CardMoonUpgradeState(active, cached != null, cached == null ? Definition(trait.GetCardTraitData()) : CardModifierProbe.Upgrade(cached));
        }
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); PlayerManager player = managers.GetPlayerManager();
            var phaseField = AccessTools.Field(typeof(PlayerManager), "currentMoonPhase"); object savedPhase = phaseField.GetValue(player);
            JObject before = JObject.FromObject(trace.CaptureDecision()); JObject rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            var rows = new List<object>();
            CardData[] originals = all.GetAllCardData().Where(card => card.GetCardType() == CardType.Spell &&
                card.GetTraits().Any(trait => trait.GetTraitStateName() == CardMoonUpgradeModel.Trait)).OrderBy(card => card.GetID(), StringComparer.Ordinal).ToArray();
            if (originals.Length != 5) throw new InvalidOperationException("Original moon-upgrade spell inventory changed.");
            CardState prototype = managers.GetCardManager()!.GetAllCards(new List<CardState>()).First(card => card.GetCardType() == CardType.Spell);
            CardUpgradeData numeric = all.GetAllCardUpgradeData().First(upgrade => upgrade.GetBonusDamage() > 0 &&
                upgrade.GetTraitDataUpgrades().Count == 0 && upgrade.GetCardTriggerUpgrades().Count == 0 && upgrade.GetStatusEffectUpgrades().Count == 0);
            Capturing = true;
            try
            {
                foreach (CardData original in originals)
                foreach (string variant in new[] { "plain", "magic-power", "existing-stronger" })
                {
                    CardState card = CardUpgradeLifecycleProbe.Copy(prototype); card.Setup(original, save);
                    if (variant == "magic-power")
                    { var upgrade = new CardUpgradeState(); upgrade.Setup(numeric); card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true); }
                    if (variant == "existing-stronger")
                    { var stronger = new CardTraitData(); stronger.Setup("CardTraitStrongerMagicPower"); card.AddTemporaryTrait(stronger, managers.GetCoreManagers()); }
                    CardInstanceState Snapshot() => CardModifierProbe.Capture(new[] { card }, _ => 1).Single();
                    void Observe(string operation, int phase, bool preview = false)
                    {
                        phaseField.SetValue(player, (PlayerManager.MoonPhase)phase);
                        CardInstanceState input = Snapshot();
                        foreach (CardTraitAddCardUpgradeOnMoonPhase trait in CardTraitCompositionProbe.Bases(card).OfType<CardTraitAddCardUpgradeOnMoonPhase>().ToArray())
                        {
                            if (operation == "Discard")
                                Drain(trait.OnCardDiscarded(new CardManager.DiscardCardParams { discardCard = card }, managers.GetCoreManagers()));
                            else if (operation == "Draw") trait.OnCardDrawn(card, managers.GetCoreManagers());
                            else
                            {
                                var previewField = AccessTools.Field(typeof(SaveManager), "_previewMode"); object savedPreview = previewField.GetValue(save);
                                try { previewField.SetValue(save, preview); Drain(trait.OnTraitUpdatedForPlayerResource(card, null, managers.GetCoreManagers())); }
                                finally { previewField.SetValue(save, savedPreview); }
                            }
                        }
                        rows.Add(new { CardDataId = original.GetID(), Variant = variant, Operation = operation, Phase = phase, Preview = preview,
                            Before = input, Actual = Snapshot() });
                    }
                    Observe("Draw", 1); Observe("Resource", 2); Observe("Resource", 2); Observe("Resource", 1);
                    Observe("Resource", 2, preview: true); Observe("Resource", 2); Observe("Discard", 2); Observe("Discard", 1); Observe("Draw", 1);
                }
            }
            finally { phaseField.SetValue(player, savedPhase); Capturing = false; }
            JObject after = JObject.FromObject(trace.CaptureDecision()); JObject rngAfter = Rng();
            bool unchanged = JToken.DeepEquals(before, after);
            if (!unchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Moon card-upgrade calibration changed live decision, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                DecisionUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "moon-card-upgrade-calibration.mt2f"))) document.Write(stream);
        }
        private static void Drain(IEnumerator native)
        {
            try { while (native.MoveNext()) { if (native.Current is IEnumerator nested) Drain(nested); else if (native.Current != null) throw new InvalidOperationException("Unexpected asynchronous moon spell upgrade callback."); } }
            finally { (native as IDisposable)?.Dispose(); }
        }
        private static JObject Rng()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
    }
}
