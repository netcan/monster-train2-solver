using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFirstCardCostScenario
    {
        internal const string Name = "original-reduce-first-spell-cost-per-deck-size";
        internal const string RelicId = "64d9943e-02e7-469d-b516-c483a3ad8ccf";
        internal const string RelicKey = "ReduceFirstSpellCostPerDeckSize";
        private const string Effect = "RelicEffectModifyFirstCardCostPerPile";
        private const string SyntheticPrefix = "64d9943e-02e7-469d-b516-c483a3ad8ccf-";
        private static string firstCardId = "";
        private static string secondCardId = "";
        private static string retainedCardId = "";
        private static bool playedFirstCard;
        private static bool playedSecondCard;
        private static bool playedRetainedCard;
        private static readonly HashSet<int> scenarioCardInstanceIds = new HashSet<int>();

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("First-card cost relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("First-card cost relic is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-FIRST-CARD-COST-RELIC-ACQUIRED unchanged=" + relic.name + " pile=DeckPile threshold=15.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("First-card cost fixture must be prepared on deployment turn.");
            SaveManager save = managers.GetSaveManager();
            AllGameData gameData = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            int visibleDeckCount = CardPileHelper.GetVisibleCardCount(CardPile.DeckPile, cards, save);
            if (visibleDeckCount < 15)
                throw new InvalidOperationException("First-card cost fixture requires at least 15 visible DeckPile cards; found " + visibleDeckCount + ".");
            CardData template = save.GetDeckState().Select(card => gameData.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetCardType() == CardType.Spell)
                ?? gameData.GetAllCardData().FirstOrDefault(card => card != null && card.GetCardType() == CardType.Spell)
                ?? throw new InvalidOperationException("First-card cost fixture needs a native Spell card definition.");

            firstCardId = AddSpell(template, gameData, cards, 1, 3, CardData.CostType.Default);
            secondCardId = AddSpell(template, gameData, cards, 2, 1, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 3, 2, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 4, 0, CardData.CostType.Default);
            AddSpell(template, gameData, cards, 5, 0, CardData.CostType.ConsumeRemainingEnergy);
            retainedCardId = AddSpell(template, gameData, cards, 8, 4, CardData.CostType.Default, persistent: true);
            FullBattleTrace.Active!.InvalidateCardRules();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-FIRST-CARD-COST-PREPARED visibleDeck=" + visibleDeckCount + " threshold=15 " +
                "normal/zero/X-cost controls and a retained Spell for next-turn reset.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared) return null;
            CombatContext? context = source.Spawn.Train.Context;
            if (context == null) return null;
            string? selected = !playedFirstCard ? firstCardId : !playedSecondCard ? secondCardId :
                source.Spawn.Turn > 0 && !playedRetainedCard ? retainedCardId : null;
            if (selected == null) return null;
            CardToken? token = context.Cards.Hand.FirstOrDefault(card => card.DataId == selected);
            if (token == null)
                throw new InvalidOperationException("Required first-card cost scenario Spell is absent from hand: " + selected);
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(item => item.CardInstanceId == token.InstanceId);
            if (action == null)
                throw new InvalidOperationException("Required first-card cost scenario Spell has no supported play: " + selected +
                    " on turn " + source.Spawn.Turn + ".");
            if (!playedFirstCard) playedFirstCard = true;
            else if (!playedSecondCard) playedSecondCard = true;
            else playedRetainedCard = true;
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => scenarioCardInstanceIds.Contains(action.CardInstanceId);

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.GetID() != RelicId || relic.name != RelicKey ||
                effect.GetEffectClassName() != Effect || effect.GetParamCardType() != CardType.Spell ||
                effect.GetParamMinInt() != (int)CardPile.DeckPile || effect.GetParamMaxInt() != 15 ||
                effect.GetEffectConditions().Count != 0 || relic.DisallowInDeploymentPhase)
                throw new InvalidOperationException("Original ReduceFirstSpellCostPerDeckSize definition changed.");
        }

        private static string AddSpell(CardData template, AllGameData gameData, CardManager cards, int ordinal,
            int cost, CardData.CostType costType, bool persistent = false)
        {
            string id = SyntheticPrefix + ordinal.ToString("D2");
            if (gameData.FindCardData(id) != null) throw new InvalidOperationException("Synthetic first-card cost id is already registered.");
            CardData spell = UnityEngine.Object.Instantiate(template);
            spell.name = "PojuFirstCardCostSpell" + ordinal;
            Set(spell, "id", id);
            Set(spell, "cost", cost);
            Set(spell, "costType", costType);
            Set(spell, "targetless", true);
            Set(spell, "targetsRoom", false);
            Set(spell, "isUnitAbility", false);
            Set(spell, "isRoomAbility", false);
            var traits = new List<CardTraitData>();
            if (persistent)
            {
                var retained = new CardTraitData();
                retained.Setup("CardTraitPersistent");
                traits.Add(retained);
            }
            Set(spell, "traits", traits);
            Set(spell, "effects", new List<CardEffectData>());
            Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gameData)).Add(spell);
            CardState? added = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Synthetic first-card cost Spell did not enter the native hand.");
            scenarioCardInstanceIds.Add(FullBattleTrace.Active!.CardId(added));
            return id;
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
