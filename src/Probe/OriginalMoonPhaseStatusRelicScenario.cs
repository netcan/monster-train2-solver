using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoonPhaseStatusRelicScenario
    {
        internal const string Name = "original-moon-phase-status-relic";
        internal const string DataId = "e013b17d-753b-47b8-a837-02af85addb4b";
        internal const string AssetKey = "MoonPhaseAddArmorToSilent";
        internal const string SilentId = "4f0f249f-2a0e-4704-b8e2-8395505e6bdb";
        private const string FaeId = "d10e2dac-fa58-4509-b0ee-112b3bf78800";
        private static int step;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] AcolyteCardIds { get; private set; } = Array.Empty<int>();
        internal static int[] FaeCardIds { get; private set; } = Array.Empty<int>();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Moon-phase status relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Moon-phase status relic is already present.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-STATUS-RELIC-ACQUIRED unchanged MoonPhaseAddArmorToSilent; " +
                "10 Armor to all friendly Silent units on New Moon.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                managers.GetRoomManager()!.GetNumRooms() < 2)
                throw new InvalidOperationException("Moon-phase status relic missed deployment setup or lacks multiple rooms.");
            if (managers.GetSaveManager().GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("Moon-phase status relic is missing or duplicated at battle setup.");
            if (managers.GetSaveManager().GetDeckHasMoonPhaseCardEffect())
                throw new InvalidOperationException("Moon-phase status isolation requires a deck without other moon-phase card effects.");
            CheckOriginal(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == DataId));

            AllGameData data = managers.GetSaveManager().GetAllGameData();
            var silentCandidate = data.GetAllCardData()
                .Where(card => card.GetCardType() == CardType.Monster)
                .Select(card => new { Card = card, Character = card.GetSpawnCharacterData() })
                .Where(item => item.Character != null && !item.Character.IsChampion() &&
                    item.Character.GetSubtypes().Any(subtype => subtype.Key == "SubtypesData_Silent") &&
                    !item.Character.GetTriggers().Any(trigger => trigger.GetTrigger().ToString() is
                        "OnStatusEffectChanged" or "OnArmorAdded" or "OnNewStatusEffectAdded"))
                .OrderBy(item => item.Character!.GetSize())
                .ThenBy(item => item.Character!.GetTriggers().Count)
                .ThenBy(item => item.Card.GetID(), StringComparer.Ordinal)
                .FirstOrDefault() ?? throw new InvalidOperationException("No original Silent unit is available without unsupported armor callbacks.");
            CardData silentCard = silentCandidate.Card;
            CardData fae = data.FindCardData(FaeId) ??
                throw new InvalidOperationException("Original Fae Meddler is missing.");
            CharacterData character = silentCandidate.Character!;
            if (character.GetTriggers().Any(trigger => trigger.GetTrigger().ToString() is
                "OnStatusEffectChanged" or "OnArmorAdded" or "OnNewStatusEffectAdded"))
                throw new InvalidOperationException("Selected Silent unit has a moon-phase status callback outside the modeled boundary.");
            if (!fae.GetSpawnCharacterData()!.GetTriggers().Any(trigger =>
                trigger.GetTrigger() == CharacterTriggerData.Trigger.OnSpawn &&
                trigger.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase")))
                throw new InvalidOperationException("Original Fae Meddler no longer advances the moon on spawn.");

            Set(character, "health", 40);
            Set(silentCard, "cost", 0);
            Set(fae, "cost", 0);
            Set(managers.GetPlayerManager(), "isMoonPhaseTrackingActive", true);
            CardManager cards = managers.GetCardManager()!;
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState[] acolytes = Enumerable.Range(0, 2)
                .Select(_ => cards.AddNewCard(silentCard, CardPile.HandPile, false, false, animate: false)).ToArray();
            CardState[] faes = Enumerable.Range(0, 2)
                .Select(_ => cards.AddNewCard(fae, CardPile.HandPile, false, false, animate: false)).ToArray();
            if (acolytes.Any(card => card == null) || faes.Any(card => card == null) ||
                acolytes.Any(card => !cards.GetHand().Contains(card)) || faes.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Moon-phase status probe could not create all Silent and phase-control cards.");
            AcolyteCardIds = acolytes.Select(FullBattleTrace.Active!.CardId).ToArray();
            FaeCardIds = faes.Select(FullBattleTrace.Active!.CardId).ToArray();
            step = 0;
            Prepared = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-STATUS-RELIC-PREPARED unchanged owner; two original Silent witnesses " +
                silentCard.GetID() + " on separate floors and two original Fae phase advances; phase=" +
                managers.GetPlayerManager().CurrentMoonPhase + "; isolated native moon tracking enabled.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 4) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            int cardId = step < 2 ? AcolyteCardIds[step] : FaeCardIds[step - 2];
            int roomIndex = step == 0 ? 0 : step == 1 ? 1 : 0;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                play.CardInstanceId == cardId && play.RoomIndex == roomIndex);
            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            step++;
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled &&
            (AcolyteCardIds.Contains(action.CardInstanceId) || FaeCardIds.Contains(action.CardInstanceId));

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            SubtypeData? subtype = effect.GetParamCharacterSubtype();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (relic.name != AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnMoonPhase" ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetTargetMode() != TargetMode.Tower ||
                effect.GetParamBool() != true || effect.GetParamInt2() != 1 || subtype?.Key != "SubtypesData_Silent" ||
                statuses.Length != 1 || statuses[0].statusId != "armor" || statuses[0].count != 10 ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original MoonPhaseAddArmorToSilent payload changed.");
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
