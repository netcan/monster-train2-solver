using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignRelicCalibration
    {
        private static readonly string[] Ids = {
            "60a7f580-0788-42f2-8896-cb8d22e849a5", "3203c2d3-58f3-4f38-888e-886d8faae758",
            "ffcb6931-e45e-4e27-bacf-4c649779c2be", "07bdefdb-03b8-4159-b74b-be85f68c4c56",
            "00a8f443-cdea-437a-98bf-0ebe0ead00b0", "c3f91e0e-629f-4d3f-b422-ce1653147293" };
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_RELICS") != "1") return;
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            var owners = Ids.Select(id => data.GetAllCollectableRelicData().Single(item => item.GetID() == id)).ToArray();
            if (save.PreviewMode || save.GetCollectedRelics().Any(item => Ids.Contains(item.GetRelicDataID())))
                throw new InvalidOperationException("Campaign calibration needs isolated original owners.");
            JObject Snapshot() => JObject.FromObject(new {
                RelicIds = save.GetCollectedRelics().Select(item => item.GetRelicDataID()).ToArray(),
                Gold = save.GetGold(), Cap = save.GetDragonsHoardCap(), Hoard = save.GetDragonsHoardAmount(),
                Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(),
                Frame = UnityEngine.Time.frameCount, Rng = RngStates() });
            JObject before = Snapshot();
            var slots = new List<object>(); var merchant = new List<object>();
            var trial = new List<object>(); var hoard = new List<object>();
            void Remove(CollectableRelicData relic) => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == relic.GetID()));
            int Raw(string field) => (int)AccessTools.Field(typeof(BalanceData), field).GetValue(save.GetBalanceData());
            var deck = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!).ToArray();
            CardData[] witnesses = {
                deck.First(card => card.GetCardType() == CardType.Monster),
                deck.First(card => card.GetCardType() == CardType.Spell),
                data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment),
                data.FindCardData("21038774-c0a4-413c-ab7e-27e458228e5f")! };
            void Slots(string phase)
            {
                var effects = managers.GetRelicManager().GetRelicEffects(new List<IModifyCardUpgradeSlotCountRelicEffect>());
                foreach (CardData definition in witnesses)
                foreach (int modifier in new[] { -5, 0, 1, 20 })
                {
                    var card = new CardState(definition, save, setupStartingUpgrades: false);
                    var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalUpgradeSlotCount(modifier);
                    card.GetCardStateModifiers().AddUpgrade(upgrade);
                    string type = card.GetCardType().ToString();
                    string prefix = type == "Monster" ? "unit" : type == "Spell" ? "spell" : "equipment";
                    slots.Add(new { Phase = phase, CardDataId = definition.GetID(), CardType = type,
                        Base = Raw(prefix + "UpgradeSlots"), Maximum = Raw(prefix + "MaxUpgradeSlots"),
                        Modifier = card.GetModifiedUpgradeSlotCount(),
                        Effects = effects.Select(effect => new { CardType = effect.CardType.ToString(), effect.Value, Subtype = effect.Subtype.Key }).ToArray(),
                        Actual = save.GetBalanceData().GetUpgradeSlots(card, effects, managers.GetRelicManager()) });
                }
            }
            Slots("baseline"); save.AddRelic(owners[0]); Slots("spell");
            save.AddRelic(owners[1]); Slots("both"); Remove(owners[0]); Slots("unit");
            Remove(owners[1]); Slots("removed");

            var originalRewards = (List<GrantableRewardData>)AccessTools.Field(typeof(AllGameData), "rewardDatas").GetValue(data);
            void Merchant(string phase)
            {
                foreach (bool purge in new[] { false, true })
                {
                    GrantableRewardData reward = UnityEngine.Object.Instantiate(originalRewards.First(item => purge ? item is PurgeRewardData : item is GoldRewardData));
                    int[] costs = { 1, 3, 7, 101, 106 }; Set(reward, "costs", costs);
                    var good = new MerchantGoodState(reward, save, false, null!);
                    var shop = UnityEngine.ScriptableObject.CreateInstance<MerchantData>();
                    foreach (int purchases in new[] { 0, 1, 2, 3, 4, 8 })
                    foreach (float discount in new[] { 0f, .1f, .5f })
                    {
                        Set(good, "numPurchases", purchases); Set(shop, "discountPercent", discount);
                        merchant.Add(new { Phase = phase, Costs = costs, Purchases = purchases, Discount = discount,
                            DiscountAllowed = !purge, RewardType = reward.GetType().Name, Actual = good.GetCost(shop) });
                    }
                }
            }
            Merchant("baseline"); save.AddRelic(owners[2]); Merchant("owned"); Remove(owners[2]); Merchant("removed");

            int originalGold = save.GetGold();
            void Trial(string phase)
            {
                foreach (int amount in new[] { -5, 0, 1, 3, 5, 7, 10, 11, 101, 105, 110 })
                foreach (bool apply in new[] { false, true })
                {
                    var reward = (GoldRewardData)UnityEngine.Object.Instantiate(originalRewards.First(item => item is GoldRewardData));
                    Set(reward, "_amount", amount); Set(reward, "_applyTrialDataModifiers", apply); reward.SetSaveManager(save);
                    int shown = reward.RewardValue; int gold = save.GetGold();
                    reward.GrantReward(new GrantableRewardData.GrantParams { coreGameManagers = managers.GetGameRunManagers(), source = GrantableRewardData.Source.Battle });
                    trial.Add(new { Phase = phase, Amount = amount, ApplyTrial = apply,
                        Multiplier = ((MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save)).GetRule(MutableRules.Condition.GoldRewardMultiplier, 1f),
                        Shown = shown, Granted = save.GetGold() - gold });
                    save.SetGold(originalGold, isTotalReset: true);
                }
            }
            Trial("baseline"); save.AddRelic(owners[3]); Trial("owned"); Remove(owners[3]); Trial("removed");

            int originalCap = save.GetDragonsHoardCap(), originalHoard = save.GetDragonsHoardAmount();
            int maximum = save.GetBalanceData().GetMaxDragonsHoard();
            foreach (var owner in owners.Skip(4))
            foreach (int cap in new[] { 0, maximum - 1, maximum })
            {
                save.SetDragonsHoardCap(cap); save.SetDragonsHoardAmount(0);
                save.AddRelic(owner); int acquired = save.GetDragonsHoardCap();
                save.SetDragonsHoardAmount(acquired); Remove(owner);
                hoard.Add(new { OwnerId = owner.GetID(), Change = owner.GetEffects().Single().GetParamInt(),
                    BeforeCap = cap, Maximum = maximum, AcquiredCap = acquired, BeforeRemovalAmount = acquired,
                    RemovedCap = save.GetDragonsHoardCap(), RemovedAmount = save.GetDragonsHoardAmount() });
            }
            save.SetDragonsHoardCap(originalCap); save.SetDragonsHoardAmount(originalHoard);
            JObject after = Snapshot();
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Campaign calibration changed owned relics, balance, cap/amount, deck, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new {
                Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after,
                Owners = owners.Select(owner => new { DataId = owner.GetID(), AssetKey = owner.name, DisallowedInPlacementPhase = owner.DisallowInDeploymentPhase,
                    Effects = owner.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(),
                        Int2 = effect.GetParamInt2(), Float = effect.GetParamFloat(), Subtype = effect.GetParamCharacterSubtype()?.Key,
                        Conditions = effect.GetEffectConditions().Count }).ToArray() }).ToArray(), Slots = slots, Merchant = merchant, Trial = trial, Hoard = hoard }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-relic-calibration.mt2f")))
                document.Write(stream);
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static JObject RngStates()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
    }
}
