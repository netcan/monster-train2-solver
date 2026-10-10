using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardUpgradeLifecycleCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CARD_UPGRADE_LIFECYCLE") != "1") return;
            var managers = AllGameManagers.Instance!; var save = managers.GetSaveManager(); var data = save.GetAllGameData();
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return result;
            }
            var before = JToken.FromObject(trace.CaptureContext()); var rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            var probe = new CardUpgradeLifecycleProbe(); var rows = new List<object>();
            void Observe(string group, string operation, CardState card, CardUpgradeState? upgrade = null, string? target = null, int index = -1, string? triggerId = null)
            {
                var input = probe.Capture(card); var payload = upgrade == null ? null : probe.Upgrade(upgrade); bool? returned = null;
                switch (operation)
                {
                    case "Apply": returned = card.ApplyTemporaryUpgrade(upgrade!, save, triggerId); break;
                    case "RemoveId": returned = card.RemoveUpgrade(target!, card.GetTemporaryCardStateModifiers(), triggerId); break;
                    case "RemoveIndex": returned = card.RemoveUpgrade(index, card.GetTemporaryCardStateModifiers()); break;
                    case "Discard": card.OnCardDiscarded(); break;
                    case "Reset": card.ResetTemporaryCardModifiers(save); break;
                    case "ResetTraits": card.ResetTemporaryCardModifierTraits(save); break;
                    case "Query": break;
                    default: throw new InvalidOperationException(operation);
                }
                var actual = probe.Capture(card);
                var view = CardUpgradeMaskCalibration.Card(card, managers.GetRelicManager());
                rows.Add(new { Group = group, Operation = operation, Payload = payload, Target = target, Index = index, TriggerId = triggerId,
                    Before = input, Returned = returned, Actual = actual, Card = view, AfterQuery = probe.Capture(card) });
            }
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>());
            var originalRelics = ((IEnumerable)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(data)).Cast<RelicData>().ToArray();
            var relicUpgrades = originalRelics.SelectMany(relic => relic.GetEffects().Where(effect => effect.GetEffectClassName() == "RelicEffectAddTempUpgrade")
                .Select(effect => new { Relic = relic.name, Upgrade = effect.GetParamCardUpgradeData()! })).ToArray();
            foreach (var relic in relicUpgrades)
            foreach (var original in new[] { owned.First(card => card.IsSpawnerCard()), owned.First(card => card.GetCardType() == CardType.Spell) })
            {
                var card = CardUpgradeLifecycleProbe.Copy(original); string group = "original/" + relic.Relic + "/" + original.GetCardType();
                var upgrade = new CardUpgradeState(); upgrade.Setup(relic.Upgrade);
                Observe(group, "Apply", card, upgrade, triggerId: "original");
                Observe(group, "Apply", card, upgrade, triggerId: "second");
                Observe(group, "RemoveId", card, target: upgrade.GetCardUpgradeDataId(), triggerId: "original");
                Observe(group, "Reset", card);
            }
            var numericData = data.GetAllCardUpgradeData().First(upgrade => upgrade.GetBonusDamage() > 0 && !upgrade.GetUpgradeWillBeScaledByNonMagicPowerTrait() &&
                upgrade.GetTraitDataUpgrades().Count == 0 && upgrade.GetRemoveTraitUpgrades().Count == 0);
            var triggerData = data.GetAllCardUpgradeData().SelectMany(upgrade => upgrade.GetCardTriggerUpgrades()).First(trigger => trigger.GetTrigger() == CardTriggerType.OnCast);
            var exhaust = new CardTraitData(); exhaust.Setup("CardTraitExhaustState");
            var stronger = new CardTraitData(); stronger.Setup("CardTraitStrongerMagicPower");
            CardTraitState Trait(CardTraitData definition, CardState card)
            { var value = (CardTraitState)Activator.CreateInstance(TypeNameCache.GetType(definition.GetTraitStateName())); value.Setup(definition, card); return value; }
            foreach (bool avoid in new[] { false, true })
            foreach (bool modified in new[] { false, true })
            foreach (bool hasPermanent in new[] { false, true })
            {
                var card = CardUpgradeLifecycleProbe.Copy(owned.First(item => item.IsSpawnerCard()));
                var permanent = new CardStateModifiers(); var temporary = new CardStateModifiers();
                AccessTools.Field(typeof(CardState), "cardModifiers").SetValue(card, permanent);
                AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(card, temporary);
                AccessTools.Field(typeof(CardState), "upgradeTriggers").SetValue(card, new List<CardTriggerEffectState>());
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, new List<CardTraitState> { Trait(exhaust, card) });
                AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(card, new List<CardTraitState>());
                AccessTools.Field(typeof(CardState), "refreshCombinedTraitsDirty").SetValue(card, true);
                if (hasPermanent) { var numeric = new CardUpgradeState(); numeric.Setup(numericData); permanent.GetCardUpgrades().Add(numeric); }
                temporary.AddTemporaryTrait(Trait(stronger, card));
                card.SetRemoveFromStandByPileOverride(CardPile.DiscardPile);
                string group = "controlled/" + avoid + "/" + modified + "/" + hasPermanent;
                var payload = new CardUpgradeState(); payload.Setup(numericData); payload.SetAvoidClobberingExistingTraits(avoid);
                payload.GetTraitDataUpgrades().Add(exhaust); payload.GetCardTriggerUpgrades().Add(triggerData); payload.SetRemoveOnDiscard(true);
                if (modified) payload.SetTraitModified("CardTraitExhaustState");
                Observe(group, "Query", card);
                Observe(group, "Apply", card, payload, triggerId: "a");
                Observe(group, "Apply", card, payload, triggerId: "b");
                var unique = new CardUpgradeState(); unique.Setup(payload); AccessTools.Field(typeof(CardUpgradeState), "isUnique").SetValue(unique, true);
                Observe(group, "Apply", card, unique, triggerId: "unique");
                Observe(group, "RemoveId", card, target: payload.GetCardUpgradeDataId(), triggerId: "a");
                var empty = new CardUpgradeState(); empty.Setup(); AccessTools.Field(typeof(CardUpgradeState), "isUnique").SetValue(empty, true);
                empty.GetTraitDataUpgrades().Add(stronger); empty.GetCardTriggerUpgrades().Add(triggerData);
                Observe(group, "Apply", card, empty, triggerId: "empty1");
                Observe(group, "Apply", card, empty, triggerId: "empty2");
                Observe(group, "RemoveIndex", card, index: temporary.GetCardUpgrades().Count - 1);
                Observe(group, "RemoveIndex", card, index: -1);
                Observe(group, "RemoveId", card, target: "missing");
                var replacement = new CardUpgradeState(); replacement.Setup(); replacement.GetUpgradesToRemove().Add(numericData);
                Observe(group, "Apply", card, replacement);
                Observe(group, "Discard", card);
                Observe(group, "ResetTraits", card);
                Observe(group, "Reset", card);
            }
            foreach (float scale in new[] { 0.5f, 1.5f, 2.5f, -0.5f, -2.5f, 10.25f })
            {
                var card = CardUpgradeLifecycleProbe.Copy(owned.First(item => item.GetCardType() == CardType.Spell));
                var permanent = new CardStateModifiers(); var numeric = new CardUpgradeState(); numeric.Setup(numericData); permanent.GetCardUpgrades().Add(numeric);
                AccessTools.Field(typeof(CardState), "cardModifiers").SetValue(card, permanent);
                AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(card, new CardStateModifiers());
                var moonData = new CardTraitData(); moonData.Setup("CardTraitScalingMagicPowerOnMoonPhase");
                var moon = Trait(moonData, card);
                AccessTools.Field(typeof(CardTraitScalingMagicPowerOnMoonPhase), "MagicPowerMultiplier").SetValue(moon, scale);
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, new List<CardTraitState> { moon });
                AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(card, new List<CardTraitState>());
                AccessTools.Field(typeof(CardState), "refreshCombinedTraitsDirty").SetValue(card, true);
                string group = "moon/" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Observe(group, "Query", card); Observe(group, "Reset", card);
            }
            var multiplierOverrides = typeof(CardTraitState).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(CardTraitState)) &&
                type.GetMethod("GetMagicPowerMultiplier")!.DeclaringType != typeof(CardTraitState)).Select(type => type.FullName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var rngAfter = Rng(); bool unchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureContext()));
            if (!unchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Card upgrade lifecycle calibration changed native context/RNG/frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, ContextUnchanged = unchanged,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter,
                OriginalRelicAssets = relicUpgrades.Select(item => item.Relic).Distinct().ToArray(), MultiplierOverrides = multiplierOverrides, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-upgrade-lifecycle-calibration.mt2f"))) document.Write(stream);
        }
    }
}
