using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPurgeToExhaustScenario
    {
        internal const string Name = "original-purge-to-exhaust-relic";
        internal const string AssetKey = "PurgeToExhaust";
        internal const string DataId = "10648aee-8577-49b6-8659-318a7f922d5a";
        internal const string SelfPurgeBlightId = "21038774-c0a4-413c-ab7e-27e458228e5f";
        internal const string PositiveSpellId = "fc119000-0000-4000-8000-0000000002a1";
        internal const string SpellControlId = "fc119000-0000-4000-8000-0000000002a2";
        internal const string PositiveEquipmentId = "fc119000-0000-4000-8000-0000000002a3";
        internal const string EquipmentControlId = "fc119000-0000-4000-8000-0000000002a4";
        internal const string BlightControlId = "fc119000-0000-4000-8000-0000000002a5";

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        private static int battleSpellInstanceId;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("PurgeToExhaust must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("PurgeToExhaust is already present in the isolated save.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-PURGE-TO-EXHAUST-ACQUIRED unchanged PurgeToExhaust SelfPurge=>Exhaust for spells/equipment.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("PurgeToExhaust missed the native deployment boundary.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);

            AllGameData data = save.GetAllGameData();
            CardData template = data.FindCardData(SelfPurgeBlightId) ??
                throw new InvalidOperationException("SelfPurgeBlight control definition is missing.");
            if (template.name != "SelfPurgeBlight" || template.GetCardType() != CardType.Blight ||
                !template.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitSelfPurge") ||
                template.GetEffects().Count != 1 || template.GetEffects()[0].GetEffectStateName() != "CardEffectNULL")
                throw new InvalidOperationException("Native SelfPurgeBlight control definition changed.");

            CardData Create(string assetKey, string id, CardType type, bool selfPurge)
            {
                CardData card = UnityEngine.Object.Instantiate(template);
                card.name = assetKey;
                Set(card, "id", id);
                Set(card, "cardType", type);
                Set(card, "cost", 0);
                Set(card, "targetless", true);
                Set(card, "targetsRoom", false);
                Set(card, "isUnitAbility", false);
                Set(card, "isRoomAbility", false);
                Set(card, "traits", selfPurge
                    ? new List<CardTraitData> { template.GetTraits().Single(trait => trait.GetTraitStateName() == "CardTraitSelfPurge") }
                    : new List<CardTraitData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                return card;
            }

            CardData positiveSpell = Create("PojuPurgeToExhaustSpell", PositiveSpellId, CardType.Spell, selfPurge: true);
            CardData spellControl = Create("PojuPurgeToExhaustSpellControl", SpellControlId, CardType.Spell, selfPurge: false);
            CardData positiveEquipment = Create("PojuPurgeToExhaustEquipment", PositiveEquipmentId, CardType.Equipment, selfPurge: true);
            CardData equipmentControl = Create("PojuPurgeToExhaustEquipmentControl", EquipmentControlId, CardType.Equipment, selfPurge: false);

            CaptureReady = true;
            foreach (CardData definition in new[] { positiveSpell, spellControl, positiveEquipment, equipmentControl })
            {
                var card = new CardState(definition, save, setupStartingUpgrades: false);
                ApplyRepeated(managers.GetRelicManager(), card);
            }
            ApplyRepeated(managers.GetRelicManager(), new CardState(template, save, setupStartingUpgrades: false));

            CardManager cards = managers.GetCardManager()!;
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("No room in hand for the battle exhaust witness.");
            CardState battleSpell = cards.AddNewCard(positiveSpell, CardPile.HandPile, false, false, animate: false);
            if (battleSpell == null || !cards.GetHand().Contains(battleSpell))
                throw new InvalidOperationException("PurgeToExhaust battle spell did not enter hand.");
            battleSpellInstanceId = FullBattleTrace.Active!.CardId(battleSpell);
            ApplyRepeated(managers.GetRelicManager(), battleSpell);
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-PURGE-TO-EXHAUST-PREPARED manager controls=spell/equipment positive and no-trait plus SelfPurgeBlight filter control; battleSpell=" + battleSpellInstanceId + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (battleSpellInstanceId == 0 || state.Spawn.Train.Context == null ||
                !state.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == battleSpellInstanceId)) return null;
            return BattleActionModel.EnumerateSupportedPlays(state)
                .FirstOrDefault(action => action.CardInstanceId == battleSpellInstanceId);
        }

        internal static bool IsScenarioPrelude(PlayCardAction action) => action.CardInstanceId == battleSpellInstanceId;

        private static void ApplyRepeated(RelicManager relics, CardState card)
        {
            relics.ApplyCardStateModifiers(card, resetTempCardModifiers: false);
            relics.ApplyCardStateModifiers(card);
            relics.ApplyCardStateModifiers(card);
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            object Field(object target, string name) => AccessTools.Field(target.GetType(), name).GetValue(target);
            bool Empty(CardUpgradeMaskData? mask, string field) => mask != null && ((ICollection)Field(mask, field)).Count == 0;
            bool Filter(CardUpgradeMaskData? mask, string assetKey, CardType type) => mask != null && mask.name == assetKey &&
                (CardType)Field(mask, "cardType") == type && Empty(mask, "additionalCardTypes") &&
                Empty(mask, "requiredRarities") && Empty(mask, "excludedRarities") &&
                Empty(mask, "requiredSubtypes") && Empty(mask, "excludedSubtypes") &&
                Empty(mask, "requiredStatusEffects") && Empty(mask, "excludedStatusEffects") &&
                Empty(mask, "requiredCardTraits") && Empty(mask, "excludedCardTraits") &&
                Empty(mask, "requiredCardEffects") && Empty(mask, "excludedCardEffects");
            if (relic.name != AssetKey || relic.DisallowInDeploymentPhase || effects.Length != 1 ||
                effects[0].GetEffectClassName() != "RelicEffectModifyTrait" || effects[0].GetEffectConditions().Count != 0 ||
                effects[0].GetSourceTrait() != "CardTraitSelfPurge" || effects[0].GetTargetTrait() != "CardTraitExhaustState" ||
                !Filter(effects[0].GetParamCardFilter(), "OnlySpells", CardType.Spell) ||
                !Filter(effects[0].GetParamCardFilterSecondary(), "OnlyEquipment", CardType.Equipment))
                throw new InvalidOperationException("Original PurgeToExhaust owner, trait conversion, ordered filters or conditions changed.");
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
