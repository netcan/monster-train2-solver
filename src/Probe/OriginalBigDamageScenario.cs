using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBigDamageScenario
    {
        internal const string Name = "original-big-damage-many-morsels";
        internal const string DataId = "15f3c641-14ba-4305-8c9f-cdb2ebb4e572";
        private const string SnackSubtype = "SubtypesData_Snack";
        private const string SnackCardId = "fc119000-0000-4000-8000-000000000751";
        private const string SnackCharacterId = "fc119000-0000-4000-8000-000000000752";
        private const string ControlCardId = "fc119000-0000-4000-8000-000000000753";
        private const string ControlCharacterId = "fc119000-0000-4000-8000-000000000754";
        private static string snackCardId = "";
        private static string controlCardId = "";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("BigDamageFromManyMorsels must be acquired before battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            RelicEffectData effect = relic.GetEffects().Single();
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            if (relic.name != "BigDamageFromManyMorsels" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectDamageOnMonsterSummoned" || effect.GetParamInt() != 200 ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetTargetMode() != TargetMode.Tower ||
                Field("paramTrackedValue").ToString() != "MonsterSubtypePlayed" ||
                Field("paramCardType").ToString() != "Monster" || (string)Field("paramSubtype") != SnackSubtype ||
                (bool)Field("paramTrackTriggerCount") || Field("paramEntryDuration").ToString() != "ThisBattle" ||
                Convert.ToInt32(Field("paramComparator")) != 2 || (int)Field("paramInt") != 20 ||
                (bool)Field("allowMultipleTriggersPerDuration") ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Original BigDamageFromManyMorsels definition changed.");

            CardData[] carriers = AddCarrierDefinitions(save);
            controlCardId = carriers[0].GetID(); snackCardId = carriers[1].GetID();
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-BIG-DAMAGE-ACQUIRED unchanged BigDamageFromManyMorsels damage=200 team=Heroes target=Tower condition=SnackThisBattleEqual20.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared || !Acquired || managers.GetSaveManager().PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("BigDamageFromManyMorsels controls require deployment.");
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardState control = cards.AddNewCard(data.FindCardData(controlCardId)!, CardPile.HandPile, false, false, animate: false);
            CardState snack = cards.AddNewCard(data.FindCardData(snackCardId)!, CardPile.HandPile, false, false, animate: false);
            if (control == null || snack == null || !cards.GetHand().Contains(control) || !cards.GetHand().Contains(snack))
                throw new InvalidOperationException("BigDamageFromManyMorsels control carriers did not enter the native hand.");

            CardStatistics statistics = managers.GetCardStatistics();
            SubtypeData subtype = SubtypeManager.GetSubtypeData(SnackSubtype);
            Seed("numMonsterSubtypesSpawnedThisTurn"); Seed("numMonsterSubtypesSpawnedThisBattle");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-BIG-DAMAGE-PREPARED seeded Snack count=19; non-Snack control=" + FullBattleTrace.Active!.CardId(control) +
                " and Snack threshold witness=" + FullBattleTrace.Active.CardId(snack) + " are playable in the native hand.");

            void Seed(string fieldName)
            {
                var counts = (System.Collections.IDictionary)AccessTools.Field(typeof(CardStatistics), fieldName).GetValue(statistics);
                counts[subtype] = 19;
            }
        }

        internal static PlayCardAction? Choose(BattleTurnState state, PlayCardAction? fallback, ManualLogSource log)
        {
            if (!Prepared) return null;
            CombatContext? context = state.Spawn.Train.Context;
            if (context == null) return null;
            bool Played(string dataId) => context.Statistics?.Values.Any(value => value.Type == "TimesPlayed" &&
                value.Duration == "ThisBattle" && value.Value > 0 && context.FindCard(value.CardId)?.DataId == dataId) == true;
            bool controlPlayed = Played(controlCardId), snackPlayed = Played(snackCardId);
            bool targetReady = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
            if (state.Spawn.Turn <= 2)
                log.LogInfo("ORIGINAL-BIG-DAMAGE-DECISION turn=" + state.Spawn.Turn + " phase=" + state.Spawn.Phase +
                    " targetReady=" + targetReady + " controlPlayed=" + controlPlayed + " snackPlayed=" + snackPlayed +
                    " snackInHand=" + context.Cards.Hand.Any(card => context.FindCard(card.InstanceId)?.DataId == snackCardId) +
                    " fallback=" + (fallback == null ? "none" : context.FindCard(fallback.CardInstanceId)?.DataId ?? "unknown"));
            BattleActionEnumeration actions = BattleActionModel.EnumerateActions(state);
            PlayCardAction? snackPlay = actions.SupportedActions.FirstOrDefault(play => play.RoomIndex == 0 &&
                context.FindCard(play.CardInstanceId)?.DataId == snackCardId) ?? actions.SupportedActions.FirstOrDefault(play =>
                context.FindCard(play.CardInstanceId)?.DataId == snackCardId);
            if (state.Spawn.Turn <= 2)
                log.LogInfo("ORIGINAL-BIG-DAMAGE-ACTIONS turn=" + state.Spawn.Turn + " hand=" + context.Cards.Hand.Count +
                    " supported=" + actions.SupportedActions.Count + " snackPlay=" + (snackPlay == null ? "none" : snackPlay.CardInstanceId + "/" + snackPlay.RoomIndex) +
                    " firstUnsupported=" + (actions.UnsupportedAttempts.FirstOrDefault()?.Reason ?? "none"));
            string? wanted = !controlPlayed ? controlCardId : !snackPlayed && targetReady ? snackCardId : null;
            if (wanted != null)
                return wanted == snackCardId ? snackPlay : actions.SupportedActions.FirstOrDefault(play => play.RoomIndex == 0 &&
                    context.FindCard(play.CardInstanceId)?.DataId == wanted) ?? actions.SupportedActions.FirstOrDefault(play =>
                    context.FindCard(play.CardInstanceId)?.DataId == wanted);
            if (!snackPlayed && !targetReady && fallback != null &&
                context.FindCard(fallback.CardInstanceId)?.DataId == snackCardId)
                return null;
            return BattleActionModel.ChooseUnitAndJunkPlay(state);
        }

        private static CardData[] AddCarrierDefinitions(SaveManager save)
        {
            AllGameData data = save.GetAllGameData();
            CardData template = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!)
                .First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CharacterData baseUnit = template.GetSpawnCharacterData()!;
            CharacterData CloneUnit(string key, string id, bool snack)
            {
                CharacterData unit = UnityEngine.Object.Instantiate(baseUnit);
                unit.name = key; Set(unit, "id", id); Set(unit, "health", 200); Set(unit, "attackDamage", 0); Set(unit, "size", 1);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null);
                List<string> subtypes = unit.GetSubtypes().Select(item => item.Key).Where(item => item != SnackSubtype).ToList();
                if (snack) subtypes.Add(SnackSubtype);
                Set(unit, "subtypeKeys", subtypes);
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
                return unit;
            }
            CardData Carrier(string key, string id, CharacterData unit)
            {
                CardData card = UnityEngine.Object.Instantiate(template);
                card.name = key; Set(card, "id", id); Set(card, "cardType", CardType.Monster); Set(card, "cost", 0);
                Set(card, "costType", CardData.CostType.Default); Set(card, "targetless", false); Set(card, "targetsRoom", true);
                Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(TargetMode.Room); Set(spawn, "paramInt", 1); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                return card;
            }

            CharacterData controlUnit = CloneUnit("PojuBigDamageControl", ControlCharacterId, false);
            CharacterData snackUnit = CloneUnit("PojuBigDamageSnack", SnackCharacterId, true);
            return new[] { Carrier("PojuBigDamageControl", ControlCardId, controlUnit),
                Carrier("PojuBigDamageSnack", SnackCardId, snackUnit) };
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
