using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardDrawOnCardPlayScenario
    {
        internal const string Name = "original-card-draw-on-card-play";
        internal const string RelicId = "f3f07b9b-1349-41d0-abab-bd5ec04d81e4";
        internal const string RelicKey = "CardDrawOnCardPlay";
        private const string SnackRelicId = "8b00c46d-374e-4afb-aa27-526873e242d1";
        private const string SnackRelicKey = "DrawOnSnackPlayed";
        private const string SnackSubtype = "SubtypesData_Snack";
        private const string SnackCardId = "fc119000-0000-4000-8000-000000000251";
        private const string SnackCharacterId = "fc119000-0000-4000-8000-000000000252";
        private static string snackCardId = "";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Card-draw-on-play relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CollectableRelicData snackRelic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == SnackRelicId);
            CheckOriginal(relic);
            CheckSnackOriginal(snackRelic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId || item.GetRelicDataID() == SnackRelicId))
                throw new InvalidOperationException("A card-draw relic is already present in the test run.");
            CardData snack = AddSnackCarrierDefinitions(save);
            snackCardId = snack.GetID();
            save.AddRelic(relic);
            save.AddRelic(snackRelic);
            Acquired = true;
            RelicEffectData effect = relic.GetEffects().Single();
            log.LogInfo("ORIGINAL-CARD-DRAW-RELICS-ACQUIRED unchanged relics=" + relic.name + "," + snackRelic.name +
                " syntheticSnackCarrier=" + snack.name + " id=" + snackCardId + " cost=" + snack.GetCost() +
                " firstRelicCount=" + effect.GetParamInt() + " firstRelicConditions=" + effect.GetEffectConditions().Count + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("The Snack card must be prepared on the deployment turn after acquiring the original relics.");
            CardData snack = managers.GetSaveManager().GetAllGameData().FindCardData(snackCardId)
                ?? throw new InvalidOperationException("The selected native Snack card disappeared before battle setup.");
            CardManager cards = managers.GetCardManager()!;
            CardState? added = cards.AddNewCard(snack, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("The original Snack monster did not enter the native hand.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-CARD-DRAW-RELICS-PREPARED syntheticSnackCarrier=" + snack.name + " id=" + snackCardId + " inHand=true.");
        }

        internal static PlayCardAction? ChooseSnack(BattleTurnState source)
        {
            if (!Enabled || !Prepared || snackCardId.Length == 0) return null;
            CombatContext? context = source.Spawn.Train.Context;
            return context == null ? null : BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == snackCardId);
        }

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            if (relic.GetID() != RelicId || effect.GetEffectClassName() != "RelicEffectCardDrawOnCardPlay" ||
                effect.GetParamInt() != 1 || effect.GetParamCardType() != CardType.Invalid ||
                Field("paramTrackedValue").ToString() != "AnyCardPlayed" ||
                Field("paramCardType").ToString() != "Any" || (bool)Field("paramTrackTriggerCount") ||
                Field("paramEntryDuration").ToString() != "ThisTurn" || Convert.ToInt32(Field("paramComparator")) != 6 ||
                (int)Field("paramInt") != 3 || (bool)Field("allowMultipleTriggersPerDuration"))
                throw new InvalidOperationException("Original CardDrawOnCardPlay definition changed.");
        }

        private static void CheckSnackOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            if (relic.GetID() != SnackRelicId || relic.name != SnackRelicKey ||
                effect.GetEffectClassName() != "RelicEffectCardDrawOnCardPlay" || effect.GetParamInt() != 1 ||
                effect.GetParamCardType() != CardType.Monster || effect.GetParamCharacterSubtype()?.Key != SnackSubtype ||
                Field("paramTrackedValue").ToString() != "TimesPlayed" || Field("paramCardType").ToString() != "Monster" ||
                !(bool)Field("paramTrackTriggerCount") || Field("paramEntryDuration").ToString() != "ThisTurn" ||
                Convert.ToInt32(Field("paramComparator")) != 6 || (int)Field("paramInt") != 1 ||
                (bool)Field("allowMultipleTriggersPerDuration"))
                throw new InvalidOperationException("Original DrawOnSnackPlayed definition changed.");
        }

        private static CardData AddSnackCarrierDefinitions(SaveManager save)
        {
            AllGameData data = save.GetAllGameData();
            CardData template = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!)
                .First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData()!);
            unit.name = "TrainStewardPojuSnack";
            Set(unit, "id", SnackCharacterId);
            List<string> subtypes = unit.GetSubtypes().Select(item => item.Key).ToList();
            if (!subtypes.Contains(SnackSubtype, StringComparer.Ordinal)) subtypes.Add(SnackSubtype);
            Set(unit, "subtypeKeys", subtypes);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "triggers", new List<CharacterTriggerData>());
            Set(unit, "unitAbility", null);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);

            CardData carrier = UnityEngine.Object.Instantiate(template);
            carrier.name = "PojuDrawOnSnackCarrier";
            Set(carrier, "id", SnackCardId);
            Set(carrier, "cardType", CardType.Monster);
            Set(carrier, "cost", 0);
            Set(carrier, "costType", CardData.CostType.Default);
            Set(carrier, "targetless", false);
            Set(carrier, "targetsRoom", true);
            Set(carrier, "isUnitAbility", false);
            Set(carrier, "isRoomAbility", false);
            Set(carrier, "traits", new List<CardTraitData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
            Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(spawn, "paramInt", 1);
            Set(spawn, "paramCharacterData", unit);
            Set(carrier, "effects", new List<CardEffectData> { spawn });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            return carrier;
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
