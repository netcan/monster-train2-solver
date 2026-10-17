using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalNoUpgradeCardCostScenario
    {
        internal const string Name = "original-reduce-cost-unit-no-upgrade";
        internal const string OwnerId = "f03c7664-0485-4bcf-8bb9-f76321e090c3";
        internal const string AssetKey = "ReduceCostUnitIfNoUpgrade";
        private const string SyntheticPrefix = "fc119000-0000-4000-8000-000000000520";
        private static readonly List<CallbackRecord> callbacks = new List<CallbackRecord>();
        private static object? definitionEvidence;
        private static object? cardEvidence;
        private static CombatRelicState? ownerEvidence;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static IReadOnlyList<CallbackRecord> Callbacks => callbacks;
        internal static object Evidence => new { Definition = definitionEvidence, Owner = ownerEvidence, Cards = cardEvidence, Callbacks = callbacks };

        internal sealed class CallbackRecord
        {
            public string Kind { get; set; } = "";
            public CardInstanceState Before { get; set; } = null!;
            public CardInstanceState? After { get; set; }
            public bool? Returned { get; set; }
            public bool? ModelReturned { get; set; }
            public bool UpgradeAdded { get; set; }
            public bool? ModelUpgradeAdded { get; set; }
            public bool ModelSupported { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
            [Newtonsoft.Json.JsonIgnore] public CombatRelicState[] Owner { get; set; } = Array.Empty<CombatRelicState>();
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Unupgraded-card cost relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != AssetKey || effect.GetEffectClassName() != RelicNoUpgradeCardCostModel.Effect ||
                effect.GetEffectConditions().Count != 0 || save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Original ReduceCostUnitIfNoUpgrade definition or acquisition changed.");

            definitionEvidence = new
            {
                OwnerId,
                AssetKey = relic.name,
                EffectType = effect.GetEffectClassName(),
                DisallowedInPlacementPhase = relic.DisallowInDeploymentPhase,
                ParamInt = effect.GetParamInt(),
                ParamCardType = effect.GetParamCardType().ToString(),
                ConditionCount = effect.GetEffectConditions().Count
            };
            save.AddRelic(relic);
            ownerEvidence = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            Acquired = true;
            log.LogInfo("ORIGINAL-NO-UPGRADE-COST-ACQUIRED unchanged=" + relic.name +
                " amount=" + effect.GetParamInt() + " exclusiveType=" + effect.GetParamCardType() +
                " disallowDeployment=" + relic.DisallowInDeploymentPhase + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Unupgraded-card cost fixture must be prepared on deployment turn.");

            AllGameData data = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardType exclusiveType = ((CollectableRelicData)data.GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId)).GetEffects().Single().GetParamCardType();
            CaptureReady = true;
            CardState monster = AddCard(data, cards, CardType.Monster, 1, 99);
            CardState spell = AddCard(data, cards, CardType.Spell, 2, 99);
            CardState equipment = AddCard(data, cards, CardType.Equipment, 3, 99);

            RelicManager relicManager = managers.GetRelicManager();
            foreach (CardState card in new[] { monster, spell, equipment })
                relicManager.ApplyCardStateModifiers(card, resetTempCardModifiers: false);

            if (!monster.RemoveUpgrade(RelicNoUpgradeCardCostModel.Effect,
                monster.GetTemporaryCardStateModifiers()))
                throw new InvalidOperationException("Monster cost witness did not begin with the native temporary cost modifier.");
            monster.UpdateCardBodyText();
            CardInstanceState beforeHandAdded = Capture(monster);
            relicManager.ApplyCardAddedToHandRelicEffects(monster);
            CardInstanceState afterHandAdded = Capture(monster);
            if (!afterHandAdded.Temporary.Upgrades.Any(item => item.DataId == RelicNoUpgradeCardCostModel.Effect))
                throw new InvalidOperationException("OnCardAdded did not reinstall the missing unupgraded-card cost modifier.");

            CardState upgraded = exclusiveType == CardType.Monster ? monster : spell;
            CardInstanceState beforePermanentCallback = Capture(upgraded);
            CardUpgradeData visibleUpgrade = VisibleUpgrade(data);
            var upgradeState = new CardUpgradeState();
            upgradeState.Setup(visibleUpgrade);
            upgraded.ApplyPermanentUpgrade(upgradeState, save, ignoreUpgradeAnimation: true);
            relicManager.OnCardPermanentUpgradeApplied(upgraded, cards);
            CardInstanceState afterPermanentCallback = Capture(upgraded);
            if (!afterPermanentCallback.Temporary.Upgrades.All(item => item.DataId != RelicNoUpgradeCardCostModel.Effect))
                throw new InvalidOperationException("Visible permanent upgrade did not remove the unupgraded-card cost modifier.");

            cardEvidence = new
            {
                MonsterId = SyntheticPrefix + "01",
                SpellId = SyntheticPrefix + "02",
                EquipmentId = SyntheticPrefix + "03",
                VisibleUpgradeId = visibleUpgrade.GetID(),
                VisibleUpgradeKey = visibleUpgrade.name,
                VisibleUpgradeShowsIcon = upgradeState.ShouldShowIcon(),
                VisibleUpgradeRegionRun = upgradeState.GetIsRegionRunUpgrade(),
                HandAddedBefore = beforeHandAdded,
                HandAddedAfter = afterHandAdded,
                PermanentCallbackBefore = beforePermanentCallback,
                PermanentCallbackAfter = afterPermanentCallback,
                TemporaryCostUpgradeCounts = new[]
                {
                    Count(monster), Count(spell), Count(equipment)
                }
            };
            FullBattleTrace.Active!.InvalidateCardRules();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-NO-UPGRADE-COST-PREPARED monster=" + FullBattleTrace.Active.CardId(monster) +
                " spell=" + FullBattleTrace.Active.CardId(spell) + " equipment=" + FullBattleTrace.Active.CardId(equipment) +
                " permanentUpgrade=" + visibleUpgrade.name + " callbacks=" + callbacks.Count + ".");
        }

        internal static CallbackRecord? Begin(string kind, RelicEffectBase effect, CardState cardState)
        {
            if (!CaptureReady || effect.SourceRelicState?.GetRelicDataID() != OwnerId ||
                cardState == null || !cardState.GetCardDataID().StartsWith(SyntheticPrefix, StringComparison.Ordinal))
                return null;
            try
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                CombatRelicState[] owner = RelicProbe.Capture(AllGameManagers.Instance!)
                    .Where(relic => relic.DataId == OwnerId).ToArray();
                if (owner.Length != 1) throw new InvalidOperationException("Expected one captured unupgraded-card cost owner.");
                var record = new CallbackRecord
                {
                    Kind = kind,
                    Before = CardModifierProbe.Capture(new[] { cardState }, trace.CardId).Single(),
                    Owner = owner
                };
                callbacks.Add(record);
                return record;
            }
            catch (Exception error)
            {
                FullBattleTrace.Active?.CaptureFailure(error);
                return null;
            }
        }

        internal static void Complete(CallbackRecord? record, CardState cardState, bool? returned = null)
        {
            if (record == null) return;
            try
            {
                record.Returned = returned;
                record.After = CardModifierProbe.Capture(new[] { cardState }, FullBattleTrace.Active!.CardId).Single();
                record.UpgradeAdded = record.After.Temporary.Upgrades.Count > record.Before.Temporary.Upgrades.Count;
                RelicCardModifierResult predicted = RelicCardModifierModel.Apply(record.Before, record.Owner,
                    resetTemporary: false);
                record.ModelSupported = predicted.Supported;
                if (!predicted.Supported)
                {
                    record.Error = predicted.UnsupportedReason;
                    return;
                }
                RelicCardModifierDispatch dispatch = predicted.Dispatches.Single();
                record.ModelReturned = dispatch.Returned;
                record.ModelUpgradeAdded = dispatch.UpgradeAdded;
                record.Difference = !JToken.DeepEquals(JToken.FromObject(predicted.Card!), JToken.FromObject(record.After))
                    ? "Native unupgraded-card cost callback differs from the model."
                    : returned.HasValue && returned.Value != dispatch.Returned
                        ? "Native callback result differs from the modeled eligibility result."
                        : record.UpgradeAdded != dispatch.UpgradeAdded
                            ? "Native temporary cost-upgrade installation differs from the model." : null;
            }
            catch (Exception error)
            {
                record.Error = error.ToString();
                FullBattleTrace.Active?.CaptureFailure(error);
            }
        }

        private static CardState AddCard(AllGameData data, CardManager cards, CardType type, int ordinal, int cost)
        {
            CardData template = data.GetAllCardData().FirstOrDefault(card => card != null && card.GetCardType() == type)
                ?? throw new InvalidOperationException("No native " + type + " card is available for the cost witness.");
            string id = SyntheticPrefix + ordinal.ToString("D2");
            if (data.FindCardData(id) != null) throw new InvalidOperationException("Synthetic unupgraded-card cost id already exists.");
            CardData cardData = UnityEngine.Object.Instantiate(template);
            cardData.name = "PojuNoUpgradeCost" + type + ordinal;
            Set(cardData, "id", id);
            Set(cardData, "cost", cost);
            Set(cardData, "startingUpgrades", new List<CardUpgradeData>());
            if (type == CardType.Spell)
            {
                Set(cardData, "targetless", true);
                Set(cardData, "targetsRoom", false);
                Set(cardData, "isUnitAbility", false);
                Set(cardData, "isRoomAbility", false);
                Set(cardData, "traits", new List<CardTraitData>());
                Set(cardData, "effects", new List<CardEffectData>());
                Set(cardData, "effectTriggers", new List<CharacterTriggerData>());
                Set(cardData, "triggers", new List<CardTriggerEffectData>());
            }
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(cardData);
            CardState? added = cards.AddNewCard(cardData, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Synthetic unupgraded-card cost witness did not enter hand: " + type);
            FullBattleTrace.Active!.CardId(added);
            return added;
        }

        private static CardUpgradeData VisibleUpgrade(AllGameData data)
        {
            return data.GetAllCardUpgradeData().FirstOrDefault(item =>
            {
                var state = new CardUpgradeState();
                state.Setup(item);
                return state.ShouldShowIcon() && !state.GetIsRegionRunUpgrade() &&
                    item.GetBonusDamage() == 0 && item.GetCostReduction() == 0 && item.GetXCostReduction() == 0 &&
                    item.GetBonusHeal() == 0 && item.GetBonusSize() == 0 && item.GetBonusEquipment() == 0 &&
                    item.GetBonusUpgradeSlotCount() == 0 && item.GetStatusEffectUpgrades().Count == 0 &&
                    item.GetRoomModifierUpgrades().Count == 0 &&
                    item.GetTraitDataUpgrades().Count == 0 && item.GetRemoveTraitUpgrades().Count == 0 &&
                    item.GetCharacterTriggerUpgrades().Count == 0 && item.GetCardTriggerUpgrades().Count == 0 &&
                    item.GetUnitAbilityUpgrade() == null && item.GetRoomAbilityUpgrade() == null;
            }) ?? throw new InvalidOperationException("No visible, non-region, non-cost card upgrade is available for the callback witness.");
        }

        private static CardInstanceState Capture(CardState card) =>
            CardModifierProbe.Capture(new[] { card }, FullBattleTrace.Active!.CardId).Single();

        private static int Count(CardState card) => card.GetTemporaryCardStateModifiers().GetCardUpgrades()
            .Count(item => item.GetCardUpgradeDataId() == RelicNoUpgradeCardCostModel.Effect);

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
