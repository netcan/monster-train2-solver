using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCheaperSpellCostScenario
    {
        internal const string Name = "original-reduce-spell-cost-cheaper";
        internal const string RelicId = "9cc85dbb-d0ae-409b-8f26-41d2202eb1e0";
        private const string RelicKey = "ReduceSpellCostOfCheaper";
        private const string UpgradeKey = "FreeSpellBlessing";
        private const string SyntheticPrefix = "59f23b76-0a79-4e0b-8f34-5100000000";
        private static string triggerCardId = "";
        private static string cheapCardId = "";
        private static bool playedTrigger;
        private static bool playedCheapCard;
        private static readonly HashSet<int> scenarioCardInstanceIds = new HashSet<int>();

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Cheaper-spell relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("Cheaper-spell relic is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            CardUpgradeData upgrade = relic.GetEffects().Single().GetParamCardUpgradeData()!;
            log.LogInfo("ORIGINAL-CHEAPER-SPELL-RELIC-ACQUIRED unchanged=" + relic.name + " upgrade=" + upgrade.name +
                " costReduction=" + upgrade.GetCostReduction() + " filters=" + upgrade.GetFilters().Count + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Cheaper-spell fixture must be prepared on deployment turn.");
            SaveManager save = managers.GetSaveManager();
            AllGameData gameData = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData template = save.GetDeckState().Select(card => gameData.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetCardType() == CardType.Spell)
                ?? gameData.GetAllCardData().FirstOrDefault(card => card != null && card.GetCardType() == CardType.Spell)
                ?? throw new InvalidOperationException("Cheaper-spell fixture needs a native Spell card definition.");

            triggerCardId = AddSpell(template, gameData, cards, 1, 3, CardData.CostType.Default);
            cheapCardId = AddSpell(template, gameData, cards, 2, 1, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 6, 2, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 3, 3, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 4, 0, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 5, 0, CardData.CostType.ConsumeRemainingEnergy);
            FullBattleTrace.Active!.InvalidateCardRules();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-CHEAPER-SPELL-RELIC-PREPARED cost3 trigger, cost1 target, equal-cost, zero-cost and X-cost controls are in hand.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared) return null;
            CombatContext? context = source.Spawn.Train.Context;
            if (context == null) return null;
            if (playedCheapCard) return null;
            string selected = playedTrigger ? cheapCardId : triggerCardId;
            CardToken? token = context.Cards.Hand.FirstOrDefault(card => card.DataId == selected);
            if (token == null) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(action => action.CardInstanceId == token.InstanceId);
            if (action != null)
            {
                if (playedTrigger) playedCheapCard = true;
                else playedTrigger = true;
            }
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => scenarioCardInstanceIds.Contains(action.CardInstanceId);

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData()
                ?? throw new InvalidOperationException("Original cheaper-spell relic upgrade data is missing.");
            if (relic.GetID() != RelicId || relic.name != RelicKey ||
                effect.GetEffectClassName() != "RelicEffectAddTempUpgradeToCardsInHandIfCheaper" ||
                effect.GetParamCardType() != CardType.Spell || upgrade.name != UpgradeKey || upgrade.GetCostReduction() != 99 ||
                upgrade.GetFilters().Count != 2)
                throw new InvalidOperationException("Original ReduceSpellCostOfCheaper definition changed.");
        }

        private static string AddSpell(CardData template, AllGameData gameData, CardManager cards, int ordinal,
            int cost, CardData.CostType costType)
        {
            string id = SyntheticPrefix + ordinal.ToString("D2");
            if (gameData.FindCardData(id) != null) throw new InvalidOperationException("Synthetic cheaper-spell card id is already registered.");
            CardData spell = UnityEngine.Object.Instantiate(template);
            spell.name = "PojuCheaperSpell" + ordinal;
            Set(spell, "id", id);
            Set(spell, "cost", cost);
            Set(spell, "costType", costType);
            Set(spell, "targetless", true);
            Set(spell, "targetsRoom", false);
            Set(spell, "isUnitAbility", false);
            Set(spell, "isRoomAbility", false);
            Set(spell, "traits", new List<CardTraitData>());
            Set(spell, "effects", new List<CardEffectData>());
            Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gameData)).Add(spell);
            CardState? added = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Synthetic cheaper-spell card did not enter the native hand.");
            scenarioCardInstanceIds.Add(FullBattleTrace.Active!.CardId(added));
            return id;
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
