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
    internal static class CardOwnedMaskCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_OWNED_CARD_MASKS") != "1") return;
            var managers = AllGameManagers.Instance!; var data = managers.GetSaveManager().GetAllGameData(); var relics = managers.GetRelicManager();
            var cards = data.GetAllCardData().ToArray(); var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            var originalRelics = ((IEnumerable)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(data)).Cast<RelicData>();
            var masks = data.GetAllCardUpgradeData().SelectMany(upgrade => upgrade.GetFilters())
                .Concat(originalRelics.SelectMany(relic => relic.GetEffects()).Select(effect => effect.GetParamCardUpgradeData()).Where(upgrade => upgrade != null)
                    .SelectMany(upgrade => upgrade.GetFilters()))
                .Concat(cards.SelectMany(card => card.GetEffects()).Select(effect => effect.GetParamCardFilter()).Where(mask => mask != null))
                .Distinct().OrderBy(mask => mask.name, StringComparer.Ordinal).ToArray();
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return result;
            }
            var before = JToken.FromObject(trace.CaptureContext()); var rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            object Clone(object value) => AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null);
            CardState Copy(CardState original)
            {
                var card = (CardState)Clone(original);
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, CardTraitCompositionProbe.Bases(original).Select(trait => (CardTraitState)Clone(trait)).ToList());
                AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(card, CardTraitCompositionProbe.Combined(original).Select(trait => (CardTraitState)Clone(trait)).ToList());
                return card;
            }
            var rows = new List<object>();
            void Observe(string name, string kind, CardState card)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    var input = CardOwnedMaskProbe.Capture(card);
                    var view = CardUpgradeMaskCalibration.Card(card, relics);
                    int permanentCost = card.GetCostWithoutTraits(true);
                    var results = masks.Select(mask => mask.FilterCard(card, relics)).ToArray();
                    rows.Add(new { Name = name, Kind = kind, Pass = pass, Before = input, Actual = CardOwnedMaskProbe.Capture(card),
                        Card = view, CostWithoutTemporary = permanentCost, Results = results });
                }
            }
            foreach (var original in owned) Observe(original.GetCardDataID(), "Owned", Copy(original));
            var visibleData = data.GetAllCardUpgradeData().First(upgrade => upgrade.GetUpgradeIcon() != null && upgrade.GetBonusDamage() > 0 &&
                upgrade.GetTraitDataUpgrades().Count == 0 && upgrade.GetRemoveTraitUpgrades().Count == 0);
            var abilityData = data.GetAllCardUpgradeData().First(upgrade => upgrade.GetUnitAbilityUpgrade() != null);
            var parameterTrait = cards.SelectMany(card => card.GetTraits()).FirstOrDefault(trait => trait.GetCardUpgradeDataParam()?.GetCardTriggerUpgrades()
                .Any(trigger => trigger.GetTrigger() == CardTriggerType.OnCast) == true);
            CardTraitState Trait(CardTraitData definition, CardState card)
            {
                var trait = (CardTraitState)Activator.CreateInstance(TypeNameCache.GetType(definition.GetTraitStateName())); trait.Setup(definition, card); return trait;
            }
            var exhaust = new CardTraitData(); exhaust.Setup("CardTraitExhaustState");
            int index = 0;
            foreach (var original in new[] { owned.First(card => card.IsSpawnerCard()), owned.First(card => card.GetCardType() == CardType.Spell) })
            foreach (int cacheMode in new[] { 0, 1, 2, 3 })
            foreach (int cost in new[] { -3, 0, 3, 99 })
            foreach (int iconMode in new[] { 0, 1, 2 })
            foreach (bool purified in new[] { false, true })
            {
                var card = Copy(original); var permanent = new CardStateModifiers(); var temporary = new CardStateModifiers();
                AccessTools.Field(typeof(CardState), "cardModifiers").SetValue(card, permanent);
                AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(card, temporary);
                AccessTools.Field(typeof(CardState), "cost").SetValue(card, cost);
                AccessTools.Field(typeof(CardState), "costType").SetValue(card, cost == 99 ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
                AccessTools.Field(typeof(CardState), "purified").SetValue(card, purified);
                AccessTools.Field(typeof(CardStateModifiers), "additionalCost").SetValue(permanent, 1);
                AccessTools.Field(typeof(CardStateModifiers), "additionalCost").SetValue(temporary, -2);
                AccessTools.Field(typeof(CardStateModifiers), "additionalSize").SetValue(permanent, 5);
                AccessTools.Field(typeof(CardStateModifiers), "additionalSize").SetValue(temporary, -7);
                AccessTools.Field(typeof(CardState), "traits").SetValue(card, new List<CardTraitState> { Trait(exhaust, card), Trait(exhaust, card) });
                AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(card, new List<CardTraitState>());
                AccessTools.Method(typeof(CardState), "RefreshCombinedCardTraits").Invoke(card, new object[] { true });
                permanent.AddTraitReplacement(typeof(CardTraitExhaustState), typeof(CardTraitIgnoreArmor));
                var visible = new CardUpgradeState(); visible.Setup(visibleData);
                AccessTools.Field(typeof(CardUpgradeState), "hideUpgradeIconOnCard").SetValue(visible, iconMode == 1);
                AccessTools.Field(typeof(CardUpgradeState), "isRegionRunUpgrade").SetValue(visible, iconMode == 2);
                permanent.GetCardUpgrades().Add(visible);
                var ability = new CardUpgradeState(); ability.Setup(abilityData); temporary.GetCardUpgrades().Add(ability);
                var status = new CardUpgradeState(); status.Setup();
                status.GetStatusEffectUpgrades().Add(new StatusEffectStackData { statusId = "armor", count = 2 });
                status.GetStatusEffectUpgrades().Add(new StatusEffectStackData { statusId = "armor", count = 3, fromPermanentUpgrade = true });
                status.GetTraitDataUpgrades().Add(exhaust);
                if (parameterTrait != null) status.GetTraitDataUpgrades().Add(parameterTrait);
                temporary.GetCardUpgrades().Add(status);
                AccessTools.Field(typeof(CardState), "refreshCombinedTraitsDirty").SetValue(card, cacheMode == 1);
                AccessTools.Field(typeof(CardState), "refreshCombinedTraitsCardModifiersDirtyCount").SetValue(card, permanent.CombinedTraitsDirtyCount - (cacheMode == 2 ? 1 : 0));
                AccessTools.Field(typeof(CardState), "refreshCombinedTraitsTempCardModifiersDirtyCount").SetValue(card, temporary.CombinedTraitsDirtyCount - (cacheMode == 3 ? 1 : 0));
                Observe("controlled-" + index++, "Controlled", card);
            }
            var dummyCard = Copy(owned.First(card => card.IsSpawnerCard()));
            var dummyPermanent = new CardStateModifiers();
            AccessTools.Field(typeof(CardState), "cardModifiers").SetValue(dummyCard, dummyPermanent);
            AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(dummyCard, new CardStateModifiers());
            AccessTools.Field(typeof(CardState), "traits").SetValue(dummyCard, new List<CardTraitState> { new CardTraitDummy() });
            AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(dummyCard, new List<CardTraitState> { new CardTraitDummy() });
            dummyPermanent.AddTraitReplacement(typeof(CardTraitDummy), typeof(CardTraitDrawInDeploymentPhase));
            AccessTools.Field(typeof(CardState), "refreshCombinedTraitsDirty").SetValue(dummyCard, true);
            Observe("dummy-definition-replacement", "Edge", dummyCard);
            var rngAfter = Rng(); bool unchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureContext()));
            if (!unchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Owned card mask calibration changed native context/RNG/frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, ContextUnchanged = unchanged,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter,
                ParameterTraitPresent = parameterTrait != null, Masks = masks.Select(CardUpgradeMaskProbe.Rule).ToArray(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-owned-mask-calibration.mt2f")))
                document.Write(stream);
        }
    }
}
