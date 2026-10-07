using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class GenerationScenario
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData rally = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            CardData steward = save.GetAllGameData().FindCardData(owned.First(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").GetCardDataID())!;
            CardUpgradeData starting = Upgrade("PojuGenerationStarting", "bc8496d3-638a-46c2-9df3-240700000001", 1, 2);
            Set(rally, "startingUpgrades", rally.GetUpgradeData().Concat(new[] { starting }).ToList());
            Set(steward, "startingUpgrades", steward.GetUpgradeData().Concat(new[] { starting }).ToList());
            CardUpgradeData optional = Upgrade("PojuGenerationOptional", "bc8496d3-638a-46c2-9df3-240700000002", 1, 1);
            if (lethal)
            {
                var discardUpgrades = save.GetBalanceData().GetCardUpgradesOnAddCardToDiscardPile();
                discardUpgrades.Add(DiscardUpgrade(starting, Upgrade("PojuGenerationMatchingDiscard", "bc8496d3-638a-46c2-9df3-240700000005", 2, 0)));
                discardUpgrades.Add(DiscardUpgrade(null, Upgrade("PojuGenerationUnconditionalDiscard", "bc8496d3-638a-46c2-9df3-240700000006", 1, 0)));
                discardUpgrades.Add(DiscardUpgrade(optional, Upgrade("PojuGenerationOptionalDiscard", "bc8496d3-638a-46c2-9df3-240700000007", 1, 0)));
            }
            CardPool rallyPool = Pool("PojuGenerationRallyPool", rally);
            CardPool unitPool = Pool("PojuGenerationUnitPool", steward);
            CardPool mixedPool = Pool("PojuGenerationMixedPool", steward, rally);
            CardPool emptyPool = Pool("PojuGenerationEmptyPool");
            if (!lethal)
            {
                CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
                var trigger = new CharacterTriggerData(CharacterTriggerData.Trigger.PostCombat,
                    Generate(unitPool, CardPile.HandPile, 1, optional: optional, copy: true));
                Set(trigger, "triggerOnce", true); Set(trigger, "suppressTriggerNotification", true);
                Set(trigger, "requiredStatusEffects", new List<StatusEffectStackData>());
                Set(trigger, "requiredStatusEffectsForDyingCharacter", new List<StatusEffectStackData>());
                Set(unit, "triggers", unit.GetTriggers().Concat(new[] { trigger }).ToList());
            }
            var effects = rally.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectDamage", 0, TargetMode.Tower, Team.Type.Heroes));
            effects.Add(Generate(rallyPool, CardPile.HandPile, 3, duplicate: true));
            effects.Add(Generate(unitPool, CardPile.HandPile, 4, optional: optional));
            effects.Add(Generate(mixedPool, CardPile.DeckPileRandom, 2, optional: optional, copy: true));
            effects.Add(Generate(rallyPool, CardPile.DeckPileTop, 0, copy: true, ignoreTemp: true));
            effects.Add(Generate(unitPool, CardPile.DeckPile, -2));
            effects.Add(Generate(mixedPool, CardPile.DiscardPile, 2, optional: optional, copy: true));
            effects.Add(Generate(emptyPool, CardPile.DeckPileRandom, 2));
            // Range fields do not represent either the pile enum or generation count.
            var ignoredRange = Generate(unitPool, CardPile.HandPile, 1, requireSpace: true);
            Set(ignoredRange, "useIntRange", true); Set(ignoredRange, "paramMinInt", -100); Set(ignoredRange, "paramMaxInt", 100);
            effects.Add(ignoredRange);
            effects.Add(Effect("CardEffectDiscardHand", 2)); // Every new generated spell must initialize its own counter.
            effects.Add(Effect("CardEffectBuffDamage", 4, TargetMode.Tower, Team.Type.Monsters));
            if (lethal)
            {
                effects.Add(Effect("CardEffectDamage", 1000, TargetMode.Tower, Team.Type.Heroes));
                effects.Add(Generate(mixedPool, CardPile.DeckPileRandom, 2, copy: true));
            }
            Set(rally, "targetless", false); Set(rally, "targetsRoom", true);
            foreach (CardState card in spells)
            {
                card.Setup(rally, save);
                Set(card.GetCardStateModifiers(), "additionalDamage", 3);
                var excluded = new CardUpgradeState(); excluded.Setup(Upgrade("PojuGenerationExcluded", "bc8496d3-638a-46c2-9df3-240700000003", 2, 0));
                Set(excluded, "excludeFromClones", true); card.ApplyPermanentUpgrade(excluded, save);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(1); temporary.SetRemoveOnDiscard(true);
                card.ApplyTemporaryUpgrade(temporary, save);
            }
            foreach (CardState card in spells.Take(2)) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            foreach (CardState card in cards.GetHand().Where(card => card.GetCardType() == CardType.Monster).ToArray())
                cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardState card in owned.Where(card => card.GetCardType() != CardType.Monster))
                if (cards.GetHand().Count < cards.GetMaxHandSize() && !cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            while (cards.GetHand().Count < cards.GetMaxHandSize()) cards.AddNewCard(rally, CardPile.HandPile, false, false, animate: false);
            var pending = new CardUpgradeState(); pending.Setup(Upgrade("PojuGenerationNext", "bc8496d3-638a-46c2-9df3-240700000004", 3, 2));
            ((IList)AccessTools.Field(typeof(CardManager), "nextAddedTempCardUpgrades").GetValue(cards)).Add(pending);
            managers.GetPlayerManager().AddEnergy(1); Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("GENERATION-PREPARED all five piles, full-hand/duplicate refusals, signed counts, empty pools, starting/optional/copied/pending upgrades and fresh effect counters; lethal=" + lethal);
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int hp)
        {
            CardUpgradeData data = DynamicUpgradeScenario.Upgrade(name, id, damage, hp, 0, 0, "armor", 0);
            Set(data, "statusEffectUpgrades", new List<StatusEffectStackData>()); return data;
        }
        private static BalanceData.CardUpgradeOnAddCardToDiscardPile DiscardUpgrade(CardUpgradeData? check, CardUpgradeData upgrade)
        {
            var rule = new BalanceData.CardUpgradeOnAddCardToDiscardPile();
            Set(rule, "upgradeToCheck", check!); Set(rule, "upgradeToAdd", upgrade); return rule;
        }
        private static CardPool Pool(string name, params CardData[] cards)
        {
            CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = name;
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            foreach (CardData card in cards) AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { card });
            return pool;
        }
        private static CardEffectData Generate(CardPool pool, CardPile pile, int count, bool duplicate = false, bool requireSpace = false,
            CardUpgradeData? optional = null, bool copy = false, bool ignoreTemp = false)
        {
            CardEffectData data = Effect("CardEffectAddBattleCard", (int)pile, TargetMode.Room, Team.Type.None);
            Set(data, "paramCardPool", pool); Set(data, "additionalParamInt", count); Set(data, "paramBool2", duplicate);
            Set(data, "paramBool", requireSpace); Set(data, "paramCardUpgradeData", optional!);
            Set(data, "copyModifiersFromSource", copy); Set(data, "ignoreTemporaryModifiersFromSource", ignoreTemp); return data;
        }
        private static CardEffectData Effect(string type, int amount, TargetMode mode = TargetMode.Room, Team.Type team = Team.Type.None)
        { var data = new CardEffectData(type, null!, team); data.Cheat_SetTargetMode(mode); Set(data, "paramInt", amount); return data; }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);

        internal sealed class Record
        {
            public string Origin { get; set; } = "";
            public int SourceCardId { get; set; }
            public CardGenerationRule Rule { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CardGenerationResult Predicted { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public string? Difference { get; set; }
        }
        private static IEnumerator Wrap(IEnumerator native, CardEffectState state, CardEffectParams parameters)
        {
            Record? record = null;
            try
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (trace != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                {
                    CardState? source = parameters.selfTarget?.GetSpawnerCard() ?? parameters.playedCard;
                    record = new Record { Origin = parameters.selfTarget == null ? "Spell" : "Unit", SourceCardId = source == null ? 0 : trace.CardId(source),
                        Rule = CardGenerationProbe.Definition(state), Before = trace.CaptureContext() };
                    record.Predicted = CardGenerationModel.Apply(record.Before, record.Rule, record.SourceCardId); Records.Add(record);
                }
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            while (native.MoveNext()) yield return native.Current;
            if (record == null) yield break;
            try
            {
                record.Actual = FullBattleTrace.Active!.CaptureContext();
                record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                    JToken.DeepEquals(JToken.FromObject(record.Predicted.Context!), JToken.FromObject(record.Actual)) ? null : "Generated card state, upgrades, allocation or RNG differs";
                Debug.Log("GENERATION-" + (record.Difference == null ? "MATCH" : "MISMATCH") + " pile=" + record.Rule.Destination + " count=" + record.Rule.Count);
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
        }
        [HarmonyPatch(typeof(CardEffectAddBattleCard), nameof(CardEffectAddBattleCard.ApplyEffect))]
        private static class GenerationPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is "generation" or "generation-lethal" or "spawn-triggers" or "spawn-triggers-lethal" or "pre-hand-discard" or "pre-hand-discard-lethal" or "clone-upgrade-refresh" or "pre-combat" or "pre-combat-lethal" or "triggered-healing" or "post-combat-healing" or "triggered-damage" or "damage-death-queue" or "terminal-death-damage") __result = Wrap(__result, cardEffectState, cardEffectParams); }
        }
    }
}
