using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBrokenRailScenario
    {
        internal const string Name = "original-broken-rail";
        internal const string OwnerId = "1699b5c7-8c34-471c-8e5a-c9830e2bc482";
        internal const string UpgradeId = "5941c92e-cdb4-4732-841c-3ab1ddbdb688";
        internal const int ForgePoints = 3;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static int scenarioCardInstanceId;
        private static readonly HashSet<int> ScenarioActionIds = new HashSet<int>();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("BrokenRail must be acquired before battle in an isolated save.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("BrokenRail is already acquired.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-BROKEN-RAIL-ACQUIRED unchanged +2 HP forge upgrade.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("BrokenRail requires its isolated deployment state.");

            AllGameData data = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => data.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetSpawnCharacterData() != null)
                ?? throw new InvalidOperationException("A native monster carrier is required for BrokenRail.");
            CharacterData character = carrier.GetSpawnCharacterData()!;
            Set(character, "health", 80);
            Set(character, "attackDamage", 20);
            Set(character, "size", 1);
            Set(character, "equipmentLimit", 3);
            Set(character, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(character, "triggers", new List<CharacterTriggerData>());
            Set(character, "unitAbility", null);
            Set(carrier, "cost", 0);
            Set(carrier, "traits", new List<CardTraitData>());
            Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());

            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState testCard = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("BrokenRail test carrier creation failed.");
            scenarioCardInstanceId = FullBattleTrace.Active!.CardId(testCard);
            ScenarioActionIds.Clear();

            save.SetForgePoints(ForgePoints);
            save.SetForgeToggleActive(true);
            AccessTools.Field(typeof(PlayerManager), "isForgeTrackingActive")
                .SetValue(managers.GetPlayerManager(), true);
            if (!CardForgeHelper.IsCardOfInterest(testCard, managers.GetCoreManagers()))
                throw new InvalidOperationException("The BrokenRail native carrier is not forge eligible.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("ORIGINAL-BROKEN-RAIL-PREPARED points=3 native ordinary monster, 80 HP, 20 attack.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || ScenarioActionIds.Count > 0) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state)
                .FirstOrDefault(play => play.CardInstanceId == scenarioCardInstanceId && play.RoomIndex == 0);
            if (action == null) throw new InvalidOperationException("BrokenRail test monster has no supported room-zero play.");
            ScenarioActionIds.Add(action.CardInstanceId);
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && ScenarioActionIds.Contains(action.CardInstanceId);

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            var state = new CardUpgradeState();
            state.Setup(upgrade);
            if (relic.name != "BrokenRail" || effect.GetEffectClassName() != RelicModel.ModifyForgeUpgrade ||
                effect.GetEffectConditions().Count != 0 || upgrade == null || upgrade.GetID() != UpgradeId ||
                state.GetAdditionalHP() != 2 || state.GetAdditionalUnhealedHP() != 0 ||
                state.GetAttackDamage() != 0 || state.GetAttackDamageBuff() != 0 || state.GetCostReduction() != 0 ||
                state.GetAdditionalHeal() != 0 || state.GetAdditionalSize() != 0 || state.GetAdditionalEquipmentLimit() != 0 ||
                state.GetAdditionalUpgradeSlotCount() != 0 || state.GetStatusEffectUpgrades().Count != 0 ||
                state.GetTraitDataUpgrades().Count != 0 || state.GetRemoveTraitUpgrades().Count != 0 ||
                state.GetTriggerUpgrades().Count != 0 || state.GetCardTriggerUpgrades().Count != 0 ||
                state.GetUnitAbilityUpgrade() != null || state.GetRoomAbilityUpgrade() != null || state.GetFilters().Count != 0)
                throw new InvalidOperationException("The unchanged BrokenRail owner or +2 HP upgrade payload changed.");
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
