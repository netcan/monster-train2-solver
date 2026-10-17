using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRebalanceValorScenario
    {
        internal const string Name = "original-rebalance-valor";
        internal const string RelicId = "76511782-d2c1-43d1-9878-e465758017d8";
        internal const string RelicKey = "RebalanceValor";
        private static readonly HashSet<int> scenarioCardIds = new HashSet<int>();
        private static bool scenarioCardPlayed;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("RebalanceValor must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != RelicKey || effect.GetEffectClassName() != RelicModel.RebalanceValor ||
                effect.GetParamMinInt() != 0 || effect.GetParamMaxInt() != 1 ||
                effect.GetEffectConditions().Count != 0 || relic.DisallowInDeploymentPhase)
                throw new InvalidOperationException("Original RebalanceValor definition changed.");
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("RebalanceValor is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-REBALANCE-VALOR-ACQUIRED unchanged attackPerStack=0 armorPerStack=1.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("RebalanceValor fixture must be prepared on deployment turn.");
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData steward = owned.Select(card => save.GetAllGameData().FindCardData(card.GetCardDataID())!)
                .FirstOrDefault(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("RebalanceValor fixture needs an owned Train Steward card.");
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80);
            Set(unit, "attackDamage", 2);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "valor", count = 2 } });
            Set(steward, "cost", 0);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save, setupStartingUpgrades: false);
            CardState added = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("RebalanceValor Train Steward did not enter the native hand.");
            scenarioCardIds.Add(FullBattleTrace.Active!.CardId(added));
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-REBALANCE-VALOR-PREPARED carrier=TrainSteward valor=2 attackBonus=0 armorBonus=1.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared || scenarioCardPlayed || source.Spawn.Train.Context == null || source.PlayRules == null) return null;
            int cardId = scenarioCardIds.Single();
            PlayCardAction action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(action => action.CardInstanceId == cardId && action.RoomIndex == 0)
                ?? throw new InvalidOperationException("RebalanceValor carrier has no supported room-zero play.");
            scenarioCardPlayed = true;
            return action;
        }

        internal static bool IsScenarioCard(CardState? card) => card != null && FullBattleTrace.Active != null &&
            scenarioCardIds.Contains(FullBattleTrace.Active.CardId(card));

        internal static bool IsScenarioAction(PlayCardAction action) => scenarioCardIds.Contains(action.CardInstanceId);

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
