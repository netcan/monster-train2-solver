using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // These observations contain raw balance/save/definition inputs as well as
    // independent native results. Acquisitions precede battle/card projection.
    internal static class RelicResourceProbe
    {
        internal sealed class ResourceSnapshot
        {
            public BattleResourceState Resources { get; set; } = null!;
            public CombatRelicState[] Relics { get; set; } = null!;
            public int Energy { get; set; }
            public int Draw { get; set; }
            public int PyreHP { get; set; }
            public int MaxHP { get; set; }
            public int PyreAttack { get; set; }
            public int EnergyModification { get; set; }
            public int DeploymentModification { get; set; }
            public string Phase { get; set; } = "";
            public string MoonPhase { get; set; } = "";
            public int Turn { get; set; }
            public bool Preview { get; set; }
            public int Gold { get; set; }
            public int Hoard { get; set; }
            public int Capacity { get; set; }
        }

        internal sealed class Operation
        {
            public string Kind { get; set; } = "";
            public string OwnerDataId { get; set; } = "";
            public int EffectIndex { get; set; } = -1;
            public bool Deployment { get; set; }
            public int Notifications { get; set; }
            public int CardRefreshes { get; set; }
            public int MonsterRefreshes { get; set; }
            public ResourceSnapshot Before { get; set; } = null!;
            public ResourceSnapshot? After { get; set; }
        }

        internal static readonly List<Operation> Operations = new List<Operation>();
        private static Operation? energyOperation;
        private static Operation? hoardOperation;
        private static bool dragonHoardResourceObserved;
        private static bool runCardResourceObserved;
        private static bool goldRewardResourceObserved;
        private static bool trainHealResourceObserved;
        private static bool grosMichelResourceObserved;
        private static bool pileDrawResourceObserved;

        private static RelicState[] Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics.ToArray();
        }

        internal static BattleResourceState? CaptureResources(AllGameManagers managers)
        {
            // Relics can be acquired before battle creates the card manager.
            // Resource balance/save inputs already exist; there are no live cards
            // to inspect for Hoard or train-healing effects at that boundary.
            CardManager? cards = managers.GetCardManager();
            CardEffectState[] cardEffects = cards == null ? Array.Empty<CardEffectState>() :
                cards.GetAllCards(new List<CardState>()).SelectMany(card => card.GetEffectStates()).ToArray();
            bool dragonHoardCards = cardEffects
                .Any(effect => effect.GetCardEffect() is IDragonsHoardCardEffect ||
                    effect.GetCardEffect() is CardEffectRewardGoldPerDragonHoard);
            bool trainHealCards = cardEffects.Any(effect => effect.GetCardEffect() is CardEffectHealTrain);
            runCardResourceObserved |= cardEffects.Any(effect => effect.GetCardEffect() is CardEffectAddRunCard);
            goldRewardResourceObserved |= cardEffects.Any(effect => effect.GetCardEffect() is CardEffectRewardGold);
            dragonHoardResourceObserved |= dragonHoardCards;
            trainHealResourceObserved |= trainHealCards;
            grosMichelResourceObserved |= Current(managers).Any(owner => owner.GetRelicDataID() == RelicEndCombatModel.GrosMichelId);
            pileDrawResourceObserved |= cards?.GetAllCards(new List<CardState>()).Any(card => card.GetTraitStates().Any(trait => trait is CardTraitScalingCardDrawCountPerPile)) == true;
            if (!RelicProbe.HasReplayHistory(managers) && !OriginalResourceRelicBatchScenario.Enabled && !PyreResourceRefreshScenario.Enabled && !OriginalFloorCapacityRelicScenario.Enabled &&
                !OriginalMoonPhaseEnergyRelicScenario.Enabled && !OriginalTriggerRelicBatchScenario.FusedPyreshardOnly &&
                !OriginalStartCombatHoardScenario.Enabled && !OriginalStartCombatHoardScenario.Acquired &&
                !OriginalFirstCardCostScenario.Enabled &&
                !grosMichelResourceObserved && !pileDrawResourceObserved &&
                !Current(managers).Any(relic => relic.GetEffects().Any(effect => RelicResourceModel.IsEffect(effect.GetType().Name))) &&
                !dragonHoardResourceObserved && !trainHealResourceObserved && !runCardResourceObserved && !goldRewardResourceObserved)
                return null;
            SaveManager save = managers.GetSaveManager();
            BalanceData balance = save.GetBalanceData();
            CharacterData character = save.GetActiveSavePyreCharacterData();
            if (character == null) throw new InvalidOperationException("Missing native resource Pyre definition.");
            PyreHeartData pyre = character.GetPyreHeartData();
            var rawSave = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            if (rawSave == null) throw new InvalidOperationException("Missing native raw resource save.");
            return new BattleResourceState(balance.GetStartOfTurnEnergy(), balance.GetStartOfDeploymentPhaseEnergy(),
                balance.GetStartOfTurnCards(), balance.GetMaxEnergy(), balance.GetMaxHandSize(), character.GetID(),
                pyre.GetAttack(), rawSave.GetMaxTowerHP(), save.GetRelicPersistentState()?.GetPyreDamageIncrease() ?? 0,
                rawSave.GetTowerModifiedBaseAttack(),
                ((int[])AccessTools.Field(typeof(PyreHeartData), "upgradeAttack").GetValue(pyre)).ToArray(),
                ((int[])AccessTools.Field(typeof(PyreHeartData), "upgradeHP").GetValue(pyre)).ToArray(),
                rawSave.GetMutatorIDs().ToArray(),
                ((MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save))
                    .GetRule(MutableRules.Condition.GoldRewardMultiplier, 1f),
                baseFloorCapacity: balance.GetCharacterCapacityPerFloor(), dragonsHoardCap: save.GetDragonsHoardCap(),
                towerHealingDisabled: save.GetRelicModifiedRules().GetTowerHealingDisabled(),
                visibleDeckCount: save.GetVisibleDeckCount());
        }

        internal static RelicResourceEffect Definition(IRelicEffect effect, int index, AllGameManagers managers)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            string type = effect.GetType().Name;
            int amount;
            float? value = null;
            int? registration = null;
            float? capacityValue = null;
            int? capacityRegistration = null;
            MutatorData? required = null;
            CardGenerationRule? generation = null;
            RelicUnitResourceRule? unitTrigger = null;
            RelicOpeningDrawRule? openingDraw = null;
            RelicPileStartTurnRule? pileStartTurn = null;
            RelicMoonPhaseStatusRule? moonPhaseStatus = null;
            RelicStartTurnHealRule? startTurnHeal = null;
            RelicStartTurnRoomEffectRule? startTurnRoomEffect = null;
            RelicCardPlayDamageRule? cardPlayDamage = null;
            RelicFriendlyTriggerDamageRule? unitDamage = null;
            RelicMooncycleRoomEffectRule? mooncycleRoomEffect = null;
            RelicRandomDrawCostRule? randomDrawCost = null;
            RelicSuppressExhaustRule? suppressExhaust = null;
            RelicEndTurnHealthRule? endTurnHealth = null;
            RelicCardTypeSequenceRule? cardTypeSequence = null;
            RelicUnitSpawnEffectRule? unitSpawnEffect = null;
            RelicDeathEffectRule? deathEffect = null;
            RelicOpeningSpawnRule? openingSpawn = null;
            RelicOpeningDeckSpawnRule? deckSpawn = null;
            RelicDrawAndEnergyRule? drawAndEnergy = null;
            if (effect is RelicEffectSpawnUnitFromDeck)
            {
                var cached = (List<CardState>)AccessTools.Field(effect.GetType(), "toProcessCards").GetValue(effect);
                deckSpawn = new((int)AccessTools.Field(effect.GetType(), "roomIndex").GetValue(effect),
                    cached.Select(card => FullBattleTrace.Active!.CardId(card)).ToArray());
                amount = 0;
            }
            else if (effect is RelicEffectSpawnUnitStartOfCombat)
            {
                openingSpawn = new RelicOpeningSpawnRule(data.GetParamSourceTeam().ToString(), data.GetParamCharacters().Select(character =>
                {
                    var interactions = new List<string>();
                    CombatUnit unit = BattleActionProbe.SpawnTemplate(character, true, interactions);
                    return new SummonUnitDefinition(character.GetID(), unit, null, interactions);
                }).ToArray());
                amount = 0;
            }
            else if (effect is RelicEffectAddEffectOnUnitDeath)
            {
                if (data.GetParamCardEffectData().Count != 1) throw new InvalidOperationException("Unmodeled death effect list.");
                deathEffect = new RelicDeathEffectRule(AccessTools.Field(effect.GetType(), "sourceTeam").GetValue(effect).ToString()!,
                    ((SubtypeData)AccessTools.Field(effect.GetType(), "subtypeData").GetValue(effect)).Key,
                    UnitTriggerActionProbe.Definition(data.GetParamCardEffectData().Single())!);
                amount = 0;
            }
            else if (effect is RelicEffectAddEffectOnUnitSpawn)
            {
                var interactions = new List<string>();
                CardEffectData nested = data.GetParamCardEffectData().Single();
                CardActionEffect action = BattleActionProbe.DescribeTriggerEffect(nested, interactions);
                var metadata = data.GetTriggers().Select(trigger => new RelicStartTurnEffectTriggerMetadata(
                    trigger.GetTrigger().ToString(), trigger.GetTriggerOnce(), trigger.GetHideVisualAndIgnoreSilence(),
                    trigger.GetOnlyTriggerIfEquipped(), trigger.GetEffects()
                        .Select(value => BattleActionProbe.DescribeTriggerEffect(value, interactions)).ToArray())).ToArray();
                if (nested.GetUseStatusEffectStackMultiplier() || nested.GetUseHealthMissingStackMultiplier() ||
                    nested.GetUseMagicPowerMultiplier() || nested.GetParamBool2()) interactions.Add("Nested spawn multipliers");
                unitSpawnEffect = new RelicUnitSpawnEffectRule(
                    AccessTools.Field(effect.GetType(), "sourceTeam").GetValue(effect).ToString()!,
                    ((SubtypeData)AccessTools.Field(effect.GetType(), "subtypeData").GetValue(effect)).Key,
                    action, interactions, metadata);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectEnergyAndCardDrawOnCardPlaySequence)
            {
                var types = (List<CardType>)AccessTools.Field(effect.GetType(), "typePlayedList").GetValue(effect);
                cardTypeSequence = new RelicCardTypeSequenceRule(types.Select(type => type.ToString()).ToArray());
                amount = (int)AccessTools.Field(effect.GetType(), "energy").GetValue(effect);
            }
            else if (effect is RelicEffectDamageOrHealUnitsEndOfTurn)
            {
                object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
                endTurnHealth = new RelicEndTurnHealthRule(Field("teamTarget").ToString()!, (bool)Field("isHeal"));
                amount = (int)Field("healthDelta");
            }
            else if (effect is RelicEffectSuppressExhaust)
            {
                var interactions = new List<string>();
                if (data.GetParamCardEffectData().Count != 0 || data.GetTriggers().Count != 0 || data.GetParamCardUpgradeData() != null ||
                    data.GetParamCardFilter() != null || data.GetParamCardFilterSecondary() != null) interactions.Add("Additional suppression payload");
                object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
                suppressExhaust = new RelicSuppressExhaustRule((int)Field("percentageChance"), Field("cardType").ToString()!, (bool)Field("testPassed"), interactions);
                amount = (int)Field("percentageChance");
            }
            else if (effect is RelicEffectDrawCardAndGainEmberOnDrawType)
            {
                var interactions = new List<string>();
                if (data.GetParamInt3() != 0 || data.GetParamMaxInt() != 1 || data.GetParamFloat() != 0 ||
                    data.GetParamBool() || data.GetParamBool2() || data.GetParamBool3() || data.GetParamBool4() ||
                    data.GetParamCardEffectData().Count != 0 || data.GetTriggers().Count != 0 || data.GetParamCardUpgradeData() != null)
                    interactions.Add("Additional draw/energy payload");
                drawAndEnergy = new(data.GetParamCardFilter() == null ? null : CardUpgradeMaskProbe.Rule(data.GetParamCardFilter()),
                    data.GetParamCardFilterSecondary() == null ? null : CardUpgradeMaskProbe.Rule(data.GetParamCardFilterSecondary()), interactions);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectRandomCardCostOnDraw)
            {
                var interactions = new List<string>();
                if (data.GetParamCardEffectData().Count != 0 || data.GetTriggers().Count != 0 || data.GetParamCardUpgradeData() != null ||
                    data.GetParamCardFilter() != null || data.GetParamCardFilterSecondary() != null) interactions.Add("Additional random draw-cost payload");
                randomDrawCost = new RelicRandomDrawCostRule(data.GetParamMaxInt(), data.GetParamBool(), data.GetParamInt3(),
                    data.GetParamBool2(), data.GetParamBool3(), data.GetParamBool4(), interactions);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectDrawOnPreOpeningDraw)
            {
                var upgradeData = data.GetParamCardUpgradeData(); CardUpgradeModifier? upgrade = null;
                if (upgradeData != null) { var state = new CardUpgradeState(); state.Setup(upgradeData); upgrade = CardModifierProbe.Upgrade(state); }
                var first = data.GetParamCardFilter(); var second = data.GetParamCardFilterSecondary();
                openingDraw = new RelicOpeningDrawRule(first == null ? null : CardUpgradeMaskProbe.Rule(first),
                    second == null ? null : CardUpgradeMaskProbe.Rule(second), data.GetParamBool(), upgrade);
                amount = data.GetParamInt();
            }
            else if (RelicUnitTriggerCardGenerationModel.IsEffect(type))
            {
                var subtype = data.GetParamCharacterSubtype();
                unitTrigger = new RelicUnitResourceRule(data.GetParamTrigger().ToString(), data.GetParamSourceTeam().ToString(),
                    subtype == null || subtype.IsNone ? null : subtype.Key, Array.Empty<string>());
                var upgradeData = data.GetParamCardUpgradeData();
                CardUpgradeModifier? upgrade = null;
                if (upgradeData != null)
                {
                    var state = new CardUpgradeState(); state.Setup(upgradeData);
                    upgrade = CardModifierProbe.Upgrade(state);
                }
                var pool = data.GetParamCardPool()?.GetFilteredChoices() ?? new List<CardData>();
                generation = new CardGenerationRule("HandPile", Math.Max(data.GetParamInt(), 1),
                    pool.Select(CardGenerationProbe.Creation).ToArray(), upgrade: upgrade, permanent: false);
                amount = Math.Max(data.GetParamInt(), 1);
            }
            else if (RelicUnitResourceModel.IsEffect(type))
            {
                var subtype = data.GetParamCharacterSubtype();
                unitTrigger = new RelicUnitResourceRule(data.GetParamTrigger().ToString(), data.GetParamSourceTeam().ToString(),
                    effect is RelicEffectEmberOnUnitTrigger && subtype != null && !subtype.IsNone ? subtype.Key : null,
                    effect is RelicEffectGoldOnTrigger ? data.GetParamStatusEffects().Select(status => status.statusId).ToArray() : Array.Empty<string>());
                amount = data.GetParamInt();
            }
            else if (RelicCardDrawOnHealModel.IsEffect(type))
            {
                unitTrigger = new RelicUnitResourceRule("OnHeal", data.GetParamSourceTeam().ToString(), null, Array.Empty<string>());
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectAddCardsStartOfBattle)
            {
                var upgradeData = data.GetParamCardUpgradeData();
                CardUpgradeModifier? upgrade = null;
                if (upgradeData != null) { var state = new CardUpgradeState(); state.Setup(upgradeData); upgrade = CardModifierProbe.Upgrade(state); }
                var pool = data.GetParamCardPool()?.GetFilteredChoices() ?? new List<CardData>();
                bool allCards = data.GetParamBool();
                bool partFactory = ((RelicEffectBase)effect).SourceRelicState.GetRelicDataID() == OriginalPartFactoryScenario.DataId;
                generation = new CardGenerationRule(data.GetParamBool3() ? "HandPile" : "DeckPileRandom",
                    allCards ? pool.Count : 1, pool.Select(card => partFactory ? CardGenerationProbe.PartFactoryCreation(card) : CardGenerationProbe.Creation(card)).ToArray(), upgrade: upgrade,
                    deterministicPoolSelection: allCards, permanent: data.GetParamBool2(), upgradeIsPermanent: upgradeData != null);
                amount = allCards ? pool.Count : 1;
            }
            else if (effect is RelicEffectAddBattleCardToHand)
            {
                var upgradeData = data.GetParamCardUpgradeData();
                CardUpgradeModifier? upgrade = null;
                if (upgradeData != null) { var state = new CardUpgradeState(); state.Setup(upgradeData); upgrade = CardModifierProbe.Upgrade(state); }
                generation = new CardGenerationRule("HandPile", 1,
                    (data.GetParamCardPool()?.GetFilteredChoices() ?? new List<CardData>()).Select(CardGenerationProbe.Creation).ToArray(), upgrade: upgrade);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectAddPrecachedBattleCardToHand)
            {
                CardPool? pool = data.GetParamCardPool();
                List<CardData> cards = pool?.GetAllChoices(new List<CardData>()) ?? new List<CardData>();
                SaveManager save = managers.GetSaveManager();
                ClassData mainClass = save.GetMainClass();
                ClassData subClass = save.GetSubClass();
                bool[] eligibility = cards.Select(card => card.GetLinkedClass() == mainClass ||
                    card.GetLinkedClass() == subClass).ToArray();
                CardCreationRule[] choices = cards.Select((card, index) => eligibility[index]
                    ? CardGenerationProbe.Creation(card)
                    : new CardCreationRule(card.GetID(), CardModifiers.Empty(), null, Array.Empty<string>())).ToArray();
                generation = new CardGenerationRule("HandPile", 1, choices, permanent: false,
                    poolEligibility: eligibility);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectAddChampionCardToHand)
            {
                SaveManager save = managers.GetSaveManager();
                RandomChampionPool pool = data.GetParamRandomChampionPool() ?? throw new InvalidOperationException("Champion card pool is missing.");
                List<RandomChampionPool.GrantedChampionInfo> champions = pool.GetAllChampions();
                HashSet<string> deckChampions = save.GetDeckState().Where(card => card.IsChampionCard())
                    .Select(card => card.GetCardDataID()).ToHashSet(StringComparer.Ordinal);
                bool[] eligible = champions.Select(champion => !deckChampions.Contains(champion.championCard.GetID())).ToArray();
                string[] championIds = save.GetAllGameData().GetAllCardData()
                    .Where(card => card.GetSpawnCharacterData()?.IsChampion() == true).Select(card => card.GetID()).ToArray();
                generation = new CardGenerationRule("HandPile", 1,
                    champions.Select(champion => CardGenerationProbe.Creation(champion.championCard, champion.upgrades)).ToArray(),
                    permanent: false, poolEligibility: eligible, championCardDataIds: championIds);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectDrawToMaxStartOfCombat)
                amount = data.GetParamInt();
            else if (effect is RelicEffectCardDrawPerPileStartOfTurn)
                amount = (int)AccessTools.Field(effect.GetType(), "_statCountPerCardEffect").GetValue(effect);
            else if (effect is RelicEffectDamageOnCardPlayed)
            {
                amount = (int)AccessTools.Field(effect.GetType(), "damageAmount").GetValue(effect);
                cardPlayDamage = new RelicCardPlayDamageRule(data.GetParamCardType().ToString(), data.GetTargetMode().ToString(),
                    data.GetParamSourceTeam().ToString(), data.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray());
            }
            else if (effect is RelicEffectDamageEnemyOnHeal)
            {
                amount = (int)AccessTools.Field(effect.GetType(), "_healToDamageMultiplier").GetValue(effect);
                unitTrigger = new RelicUnitResourceRule(data.GetParamTrigger().ToString(), data.GetParamSourceTeam().ToString(),
                    null, Array.Empty<string>());
                unitDamage = new RelicFriendlyTriggerDamageRule("FrontInRoom");
            }
            else if (effect is RelicEffectDamageEnemyOnFriendlyTrigger)
            {
                amount = (int)AccessTools.Field(effect.GetType(), "damageAmount").GetValue(effect);
                unitTrigger = new RelicUnitResourceRule(data.GetParamTrigger().ToString(), data.GetParamSourceTeam().ToString(),
                    data.GetParamCharacterSubtype().IsNone ? null : data.GetParamCharacterSubtype().Key,
                    data.GetParamStatusEffects().Select(status => status.statusId).ToArray());
                unitDamage = new RelicFriendlyTriggerDamageRule(data.GetTargetMode().ToString());
            }
            else if (effect is RelicEffectDamageOnExhaust)
            {
                amount = (int)AccessTools.Field(effect.GetType(), "_damageAmount").GetValue(effect);
                cardPlayDamage = new RelicCardPlayDamageRule(data.GetParamCardType().ToString(), data.GetTargetMode().ToString(),
                    data.GetParamSourceTeam().ToString(), data.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray());
            }
            else if (effect is RelicEffectModifyDragonsHoardStartOfCombat)
                amount = data.GetParamInt();
            else if (effect is RelicEffectAddEffectPerPileStartOfTurn)
            {
                CardEffectData nested = data.GetParamCardEffectData().Single();
                var interactions = new List<string>();
                CardActionEffect action = BattleActionProbe.DescribeTriggerEffect(nested, interactions);
                pileStartTurn = new RelicPileStartTurnRule(((CardPile)data.GetParamMinInt()).ToString(),
                    data.GetParamMaxInt(), data.GetParamBool(), action, interactions);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectAddEffectOnMooncycle)
            {
                var interactions = new List<string>();
                CardEffectData[] nested = data.GetParamCardEffectData().ToArray();
                CardActionEffect[] actions = nested.Select(action => BattleActionProbe.DescribeTriggerEffect(action, interactions)).ToArray();
                mooncycleRoomEffect = new RelicMooncycleRoomEffectRule(data.GetParamInt(), data.GetParamMaxInt(), data.GetParamFloat(),
                    data.GetParamBool(), data.GetParamBool2(), data.GetParamBool3(), data.GetParamBool4(), actions, interactions,
                    nested.Any(action => action.GetUseStatusEffectStackMultiplier() || action.GetUseHealthMissingStackMultiplier() ||
                        action.GetUseMagicPowerMultiplier() || action.GetParamBool2()), data.GetTriggers().Count);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectAddEffectStartOfTurn)
            {
                CardEffectData nested = data.GetParamCardEffectData().Single();
                var interactions = new List<string>();
                CardActionEffect action = BattleActionProbe.DescribeTriggerEffect(nested, interactions);
                RelicStartTurnEffectTriggerMetadata[] triggerMetadata = data.GetTriggers().Select(trigger =>
                {
                    var triggerInteractions = new List<string>();
                    CardActionEffect[] triggerEffects = trigger.GetEffects()
                        .Select(triggerEffect => BattleActionProbe.DescribeTriggerEffect(triggerEffect, triggerInteractions)).ToArray();
                    interactions.AddRange(triggerInteractions);
                    return new RelicStartTurnEffectTriggerMetadata(trigger.GetTrigger().ToString(),
                        trigger.GetTriggerOnce(), trigger.GetHideVisualAndIgnoreSilence(),
                        trigger.GetOnlyTriggerIfEquipped(), triggerEffects);
                }).ToArray();
                startTurnRoomEffect = new RelicStartTurnRoomEffectRule(index, data.GetParamInt(), data.GetParamMaxInt(),
                    data.GetParamFloat(), data.GetParamBool(), data.GetParamBool2(), data.GetParamBool3(), data.GetParamBool4(),
                    action, interactions, nested.GetUseStatusEffectStackMultiplier(),
                    nested.GetUseHealthMissingStackMultiplier(), nested.GetUseMagicPowerMultiplier(), nested.GetParamBool2(),
                    triggerMetadata);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw)
                amount = data.GetParamInt();
            else if (effect is RelicEffectChangeStartingHandSize || effect is RelicEffectAdditionalDrawOnTowerFullHealth)
            {
                var draw = (IBalanceEditingRelicEffect)effect;
                value = draw.Value;
                amount = effect is RelicEffectAdditionalDrawOnTowerFullHealth ? data.GetParamInt() : UnityEngine.Mathf.RoundToInt(draw.Value);
                var rules = (MutableRules?)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(managers.GetSaveManager());
                if (rules != null)
                {
                    var map = (Dictionary<MutableRules.Condition, List<IBalanceEditingRelicEffect>>)
                        AccessTools.Field(typeof(MutableRules), "ruleMap").GetValue(rules);
                    if (map.TryGetValue(MutableRules.Condition.ChangeStartingHandSize, out var list))
                    {
                        int order = list.FindIndex(item => ReferenceEquals(item, effect));
                        if (order >= 0) registration = order;
                    }
                }
            }
            else if (effect is RelicEffectChangeFloorCapacity capacity)
            {
                capacityValue = capacity.Value;
                amount = UnityEngine.Mathf.RoundToInt(capacity.Value);
                var rules = (MutableRules?)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(managers.GetSaveManager());
                if (rules != null)
                {
                    var map = (Dictionary<MutableRules.Condition, List<IBalanceEditingRelicEffect>>)
                        AccessTools.Field(typeof(MutableRules), "ruleMap").GetValue(rules);
                    if (map.TryGetValue(MutableRules.Condition.AdditionalCharactersPerFloor, out var list))
                    {
                        int order = list.FindIndex(item => ReferenceEquals(item, effect));
                        if (order >= 0) capacityRegistration = order;
                    }
                }
            }
            else if (effect is RelicEffectIncreasePyreAttack || effect is RelicEffectIncreasePyreHealth)
            {
                amount = (int)AccessTools.Field(effect.GetType(), effect is RelicEffectIncreasePyreAttack ? "_attackAmount" : "_healthAmount").GetValue(effect);
                required = (MutatorData?)AccessTools.Field(effect.GetType(), "_requiredMutator").GetValue(effect);
            }
            else if (type == RelicPayCardCostWithPyreHealthModel.Effect)
                amount = data.GetParamInt();
            else if (effect is RelicEffectAdditionalSmeltEmber)
                amount = (int)AccessTools.Field(effect.GetType(), "_additionalSmeltEmber").GetValue(effect);
            else if (effect is RelicEffectAdjustMixAmount)
                amount = (int)AccessTools.Field(effect.GetType(), "additionalAmount").GetValue(effect);
            else if (effect is RelicEffectConserveForgePoints)
                amount = (int)AccessTools.Field(effect.GetType(), "_value").GetValue(effect);
            else if (effect is RelicEffectModifyMaxCorruption || effect is RelicEffectModifyCorruptionStartOfCombat)
                amount = (int)AccessTools.Field(effect.GetType(), "corruptionChange").GetValue(effect);
            else if (effect is RelicEffectAddPermanentCorruptionStartOfCombat)
                amount = (int)AccessTools.Field(effect.GetType(), "addPermanentAmount").GetValue(effect);
            else if (effect is RelicEffectChangeSingleFloorCapacity)
                amount = (int)AccessTools.Field(effect.GetType(), "adjustAmount").GetValue(effect);
            else if (effect is RelicEffectConserveEnergyEndOfTurn conserve)
            {
                IConserveEnergyEndOfTurnRelicEffect.ConserveParamaters parameters = conserve.GetConserveParameters();
                if (parameters.Mode != IConserveEnergyEndOfTurnRelicEffect.ConserveMode.All || parameters.Amount.HasValue)
                    throw new InvalidOperationException("Unexpected native end-of-turn conservation mode.");
                amount = 0;
            }
            else if (effect is RelicEffectAddStatusEffectOnMoonPhase)
            {
                SubtypeData? subtype = data.GetParamCharacterSubtype();
                moonPhaseStatus = new RelicMoonPhaseStatusRule(data.GetParamSourceTeam().ToString(),
                    data.GetTargetMode().ToString(), data.GetParamStatusEffects()
                        .Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                    subtype == null || subtype.IsNone ? null : subtype.Key, data.GetParamBool(), data.GetParamInt2());
                amount = data.GetParamInt();
            }
            else if (type == RelicStartTurnHealModel.Effect)
            {
                SubtypeData? subtype = data.GetParamCharacterSubtype();
                object targets = AccessTools.Field(effect.GetType(), "targetsData").GetValue(effect);
                string targetMode = AccessTools.Field(targets.GetType(), "targetMode").GetValue(targets).ToString()!;
                startTurnHeal = new RelicStartTurnHealRule(index, data.GetParamInt(), data.GetParamInt2(),
                    data.GetParamFloat(), data.GetParamSourceTeam().ToString(), targetMode,
                    subtype == null || subtype.IsNone ? null : subtype.Key, data.GetEffectConditions().Count);
                amount = data.GetParamInt();
            }
            else if (effect is RelicEffectPyreHeartResurrection)
                amount = 0; // The native effect initializes only ParamFloat; ParamInt is unused.
            else if (effect is GrosMichelRelicEffectChanceToDestroySelf)
                amount = (int)AccessTools.Field(effect.GetType(), "chanceToDestroySelf").GetValue(effect);
            else if (effect is RelicEffectEnergyOnCardEarlyDiscard)
                amount = data.GetParamInt();
            else if (effect is RelicEffectAddGoldOnExhaust)
                amount = data.GetParamInt();
            else if (effect is RelicEffectEnergyOnCorruptRestrictedPlayed)
                amount = (int)AccessTools.Field(effect.GetType(), "energyChange").GetValue(effect);
            else if (effect is RelicEffectCorruptionOnCorruptRestrictedPlayed)
                amount = (int)AccessTools.Field(effect.GetType(), "corruptionChange").GetValue(effect);
            else
                amount = (int)AccessTools.Field(effect.GetType(), "energyChange").GetValue(effect);
            return new RelicResourceEffect(index, type, data.GetParamInt(), data.GetParamInt2(), amount,
                effect.CanApplyInPreviewMode, required?.GetID(), RelicCardModifierProbe.Conditions(effect), value, registration,
                capacityValue, capacityRegistration, data.GetParamMinInt(),
                effect is RelicEffectAddPermanentCorruptionStartOfCombat || effect is RelicEffectChangeSingleFloorCapacity || effect is RelicEffectRandomCardCostOnDraw || effect is RelicEffectSuppressExhaust || effect is RelicEffectPyreHeartResurrection ? data.GetParamFloat() : 0,
                generation, unitTrigger, openingDraw, pileStartTurn, moonPhaseStatus, startTurnHeal, startTurnRoomEffect, cardPlayDamage, unitDamage, mooncycleRoomEffect, randomDrawCost, suppressExhaust, endTurnHealth, cardTypeSequence, unitSpawnEffect, deathEffect, openingSpawn, deckSpawn, drawAndEnergy);
        }

        internal static ResourceSnapshot Capture()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            CombatManager? combat = managers.GetCombatManager();
            return new ResourceSnapshot
            {
                Resources = CaptureResources(managers)!, Relics = RelicProbe.Capture(managers),
                Energy = managers.GetPlayerManager().GetEnergy(), Draw = save.GetStartOfTurnCards(),
                PyreHP = save.GetTowerHP(), MaxHP = save.GetMaxTowerHP(), PyreAttack = save.GetBasePyreAttack(),
                EnergyModification = managers.GetRelicManager().GetEnergyPerTurnModification(),
                DeploymentModification = managers.GetRelicManager().GetEnergyPerDeploymentPhaseModification(),
                Phase = combat?.GetCombatPhase().ToString() ?? "UNUSED", Turn = combat?.GetTurnCount() ?? 0,
                MoonPhase = managers.GetPlayerManager().CurrentMoonPhase.ToString(),
                Preview = save.PreviewMode, Gold = save.GetGold(), Hoard = save.GetDragonsHoardAmount(),
                Capacity = save.GetCharacterCapacityPerFloor()
            };
        }

        private static IEnumerator WrapHoard(IEnumerator native, RelicEffectModifyDragonsHoardStartOfCombat effect)
        {
            Operation? record = null;
            try
            {
                var managers = AllGameManagers.Instance!;
                RelicState owner = Current(managers).Single(relic => relic.GetEffects().Any(item => ReferenceEquals(item, effect)));
                record = new Operation { Kind = "StartCombatHoard", OwnerDataId = owner.GetRelicDataID(),
                    Deployment = managers.GetCombatManager()!.IsPlacementPhase,
                    EffectIndex = owner.GetEffects().FindIndex(item => ReferenceEquals(item, effect)), Before = Capture() };
                Operations.Add(record);
                hoardOperation = record;
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
            }
            finally
            {
                hoardOperation = null;
                if (record != null)
                {
                    try { record.After = Capture(); }
                    catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
                }
            }
        }

        private static IEnumerator WrapEnergy(IEnumerator native, IRelicEffect effect, bool deployment, string kind = "Energy")
        {
            Operation? record = null;
            try
            {
                var managers = AllGameManagers.Instance!;
                RelicState owner = Current(managers).Single(relic => relic.GetEffects().Any(item => ReferenceEquals(item, effect)));
                record = new Operation { Kind = kind, OwnerDataId = owner.GetRelicDataID(), Deployment = deployment,
                    EffectIndex = owner.GetEffects().FindIndex(item => ReferenceEquals(item, effect)), Before = Capture() };
                Operations.Add(record);
                energyOperation = record;
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            // The callback's body executes in MoveNext, not its iterator factory.
            while (native.MoveNext()) yield return native.Current;
            energyOperation = null;
            if (record != null)
            {
                try { record.After = Capture(); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.AddRelic))]
        private static class AcquirePatch
        {
            private static void Prefix(RelicData relicData, out Operation? __state)
            {
                __state = null;
                if (!(OriginalResourceRelicBatchScenario.Enabled && OriginalResourceRelicBatchScenario.Artifacts.Any(item => item.Id == relicData.GetID())) &&
                    !(PyreResourceRefreshScenario.Enabled && PyreResourceRefreshScenario.Artifacts.Any(item => item.Id == relicData.GetID())) &&
                    !(OriginalFloorCapacityRelicScenario.Enabled && OriginalFloorCapacityRelicScenario.Artifacts.Any(item => item.Id == relicData.GetID())) &&
                    !(OriginalMoonPhaseEnergyRelicScenario.Enabled && relicData.GetID() == OriginalMoonPhaseEnergyRelicScenario.DataId) &&
                    !(OriginalStartCombatEnergyScenario.Enabled && relicData.GetID() == OriginalStartCombatEnergyScenario.DataId) &&
                    !(OriginalStartCombatHoardScenario.Enabled && relicData.GetID() == OriginalStartCombatHoardScenario.DataId)) return;
                try
                {
                    __state = new Operation { Kind = "Acquire", OwnerDataId = relicData.GetID(), Before = Capture() };
                    Operations.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
            private static void Postfix(Operation? __state)
            {
                if (__state == null) return;
                try { __state.After = Capture(); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergy), nameof(RelicEffectModifyEnergy.ApplyEffectTurnPhaseTiming))]
        private static class EnergyPatch
        {
            private static void Postfix(RelicEffectModifyEnergy __instance, ref IEnumerator __result)
            { if (OriginalResourceRelicBatchScenario.Prepared || PyreResourceRefreshScenario.Prepared || OriginalFloorCapacityRelicScenario.Prepared) __result = WrapEnergy(__result, __instance, false); }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergyDuringDeployment), nameof(RelicEffectModifyEnergyDuringDeployment.ApplyEffectTurnPhaseTiming))]
        private static class DeploymentPatch
        {
            private static void Postfix(RelicEffectModifyEnergyDuringDeployment __instance, ref IEnumerator __result)
            { if (OriginalResourceRelicBatchScenario.Prepared || PyreResourceRefreshScenario.Prepared || OriginalFloorCapacityRelicScenario.Prepared) __result = WrapEnergy(__result, __instance, true); }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergyOnMoonPhase), nameof(RelicEffectModifyEnergyOnMoonPhase.ApplyEffectMoonPhase))]
        private static class MoonPhaseEnergyPatch
        {
            private static void Postfix(RelicEffectModifyEnergyOnMoonPhase __instance, ref IEnumerator __result)
            {
                if (OriginalMoonPhaseEnergyRelicScenario.Prepared)
                    __result = WrapEnergy(__result, __instance, false, "MoonPhaseEnergy");
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyEnergyStartOfCombat), nameof(RelicEffectModifyEnergyStartOfCombat.ApplyEffectStartOfBattle))]
        private static class StartCombatEnergyPatch
        {
            private static void Postfix(RelicEffectModifyEnergyStartOfCombat __instance, ref IEnumerator __result)
            {
                if (OriginalStartCombatEnergyScenario.Acquired)
                    __result = WrapEnergy(__result, __instance, AllGameManagers.Instance!.GetCombatManager()!.IsPlacementPhase, "StartCombatEnergy");
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyDragonsHoardStartOfCombat), nameof(RelicEffectModifyDragonsHoardStartOfCombat.ApplyEffectStartOfBattle))]
        private static class StartCombatHoardPatch
        {
            private static void Postfix(RelicEffectModifyDragonsHoardStartOfCombat __instance, ref IEnumerator __result)
            {
                if (OriginalStartCombatHoardScenario.Acquired)
                    __result = WrapHoard(__result, __instance);
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.OnDragonsHoardChanged))]
        private static class CardHoardRefreshPatch
        {
            private static void Postfix()
            {
                if (hoardOperation != null) hoardOperation.CardRefreshes++;
            }
        }

        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.OnDragonsHoardChanged))]
        private static class MonsterHoardRefreshPatch
        {
            private static void Postfix()
            {
                if (hoardOperation != null) hoardOperation.MonsterRefreshes++;
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            {
                if (energyOperation != null && RelicResourceModel.IsEffect(triggeredEffect.GetType().Name))
                    energyOperation.Notifications++;
            }
        }
    }
}
