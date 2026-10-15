using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardTypeCostScenario
    {
        internal const string Name = "original-card-type-cost-relic";
        internal const string AssetKey = "CheaperBlightScourge";
        internal const string DataId = "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15";
        internal const string BlightId = "21038774-c0a4-413c-ab7e-27e458228e5f";
        internal const string JunkId = "62cc5532-5fa0-4e66-9fec-85be573af359";
        internal static string Scenario => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
        internal static bool Ordering => Scenario == "card-type-cost-discount-first" || Scenario == "card-type-cost-upgrade-first";
        internal static bool UpgradeFirst => Scenario == "card-type-cost-upgrade-first";
        internal static bool Enabled => Scenario == Name || Ordering;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        private static int blightInstanceId;
        private static int junkInstanceId;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Original card-type cost relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Card-type cost relic is already present.");
            CollectableRelicData? marker = Ordering ? CreateMarker(save.GetAllGameData()) : null;
            if (UpgradeFirst) save.AddRelic(marker!);
            save.AddRelic(relic);
            if (Ordering && !UpgradeFirst) save.AddRelic(marker!);
            Acquired = true;
            log.LogInfo("ORIGINAL-CARD-TYPE-COST-ACQUIRED unchanged CheaperBlightScourge Blight=-99 Junk=-99.");
            if (Ordering) log.LogInfo("CARD-TYPE-COST-ORDER-ACQUIRED " + Scenario + "; authored zero-cost Blight damage marker=7.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("Card-type cost relic missed the native deployment boundary.");
            CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));
            CardManager cards = managers.GetCardManager()!;
            CardState Add(string id, string key, CardType type, int cost)
            {
                CardData definition = save.GetAllGameData().FindCardData(id) ??
                    throw new InvalidOperationException("Missing original card " + key + ".");
                if (definition.name != key || definition.GetCardType() != type || definition.GetCost() != cost ||
                    definition.GetEffects().Count != 1 || definition.GetEffects()[0].GetEffectStateName() != "CardEffectNULL" ||
                    !cards.GetBelowHandSize())
                    throw new InvalidOperationException("Original cost witness definition or hand space changed: " + key);
                CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Original cost witness did not enter hand: " + key);
                return added;
            }
            CardState blight = Add(BlightId, "SelfPurgeBlight", CardType.Blight, 3);
            CardState junk = Add(JunkId, "ScourgeT1", CardType.Junk, 1);
            blightInstanceId = FullBattleTrace.Active!.CardId(blight);
            junkInstanceId = FullBattleTrace.Active.CardId(junk);
            CaptureReady = true;
            foreach (CardState card in new[] { blight, junk, cards.GetHand().First(item =>
                item.GetCardType() != CardType.Blight && item.GetCardType() != CardType.Junk) })
            {
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            int Cost(CardState card) => card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(),
                managers.GetRelicManager(), null);
            log.LogInfo("ORIGINAL-CARD-TYPE-COST-PREPARED blight=" + blightInstanceId +
                " cost=" + Cost(blight) + " junk=" + junkInstanceId + " cost=" + Cost(junk));
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (state.Spawn.Train.Context == null) return null;
            foreach (int id in new[] { blightInstanceId, junkInstanceId })
                if (state.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == id))
                    return BattleActionModel.EnumerateSupportedPlays(state)
                        .FirstOrDefault(action => action.CardInstanceId == id);
            return null;
        }

        internal static bool IsScenarioPrelude(PlayCardAction action) =>
            action.CardInstanceId == blightInstanceId || action.CardInstanceId == junkInstanceId;

        private static CollectableRelicData CreateMarker(AllGameData data)
        {
            // Keep the original discount untouched. The authored second relic isolates
            // whether a later native filter observes offsets from earlier dispatches.
            CollectableRelicData source = data.GetAllCollectableRelicData().Single(item => item.name == "ReduceStarterCost");
            RelicEffectData original = source.GetEffects().Single(item => item.GetEffectClassName() == "RelicEffectAddTempUpgrade");
            var marker = UnityEngine.Object.Instantiate(source);
            marker.name = "PojuZeroCostBlightUpgrade";
            Set(marker, "id", "fc119000-0000-4000-8000-000000000190");
            Set(marker, "disallowInDeploymentPhase", false);
            var upgrade = UnityEngine.Object.Instantiate(original.GetParamCardUpgradeData());
            upgrade.name = "PojuZeroCostBlightDamage";
            Set(upgrade, "id", "fc119000-0000-4000-8000-000000000191");
            Set(upgrade, "costReduction", 0);
            Set(upgrade, "xCostReduction", 0);
            Set(upgrade, "bonusDamage", 7);
            Set(upgrade, "isUnique", false);
            var mask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
            mask.name = "PojuZeroCostBlightMask";
            Set(mask, "cardType", CardType.Blight);
            Set(mask, "costRange", new UnityEngine.Vector2(0, 0));
            Set(upgrade, "filters", new List<CardUpgradeMaskData> { mask });
            var effect = (RelicEffectData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null)!;
            Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "paramBool", false);
            Set(marker, "effects", new List<RelicEffectData> { effect });
            ((List<CardUpgradeData>)AccessTools.Field(typeof(AllGameData), "cardUpgradeDatas").GetValue(data)).Add(upgrade);
            ((List<CollectableRelicData>)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(data)).Add(marker);
            return marker;
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.name != AssetKey || relic.DisallowInDeploymentPhase || effects.Length != 2)
                throw new InvalidOperationException("Original card-type cost relic identity changed.");
            for (int index = 0; index < effects.Length; index++)
                if (effects[index].GetEffectClassName() != "RelicEffectModifyCardTypeCost" ||
                    effects[index].GetParamCardType() != (index == 0 ? CardType.Blight : CardType.Junk) ||
                    effects[index].GetParamInt() != -99 || effects[index].GetParamBool() ||
                    effects[index].GetParamCharacterSubtype()?.IsNone != true ||
                    effects[index].GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original card-type cost payload changed at effect " + index + ".");
        }
    }
}
