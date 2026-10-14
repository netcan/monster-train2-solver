using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class BattleActionProbe
    {
        private static readonly HashSet<string> SpellEffectTypes = new HashSet<string>(new[]
        {
            "CardEffectNULL", "CardEffectCopyUnits", "CardEffectBump", "CardEffectUnitAbilityTeleport", "CardEffectAttackWithUnit", "CardEffectRemoveStatusEffect",
            "CardEffectSetUnitAbility", "CardEffectRemoveAbility", "CardEffectResetCooldown", "CardEffectAdjustAbilityCooldown",
            "CardEffectAttachEquipment", "CardEffectAttachTrainRoomAttachment", "CardEffectBuffEquipment", "CardEffectRemoveEquipment",
            "CardEffectRewardGold", "CardEffectGainEnergy", "CardEffectAdjustEnergy", "CardEffectGainEnergyNextTurn", "CardEffectGainEnergyEveryTurn",
            "CardEffectAdjustDragonsHoard", "CardEffectRewardGoldPerDragonHoard",
            "CardEffectDamage", "CardEffectDamagePerSourceAttack", "CardEffectDamagePerHp", "CardEffectDamageByUnitsKilled", "CardEffectTransferAttackFromUnit", "CardEffectDraw", "CardEffectDrawAdditionalNextTurn",
            "CardEffectRemoveAllStatusEffects", "CardEffectTransferAllStatusEffectsFromUnit", "CardEffectTransferAllStatusEffects",
            "CardEffectAdjustRoomCapacity", "CardEffectAdjustRoomMaxCorruption", "CardEffectAdjustCorruption",
            "CardEffectAddPermanentCorruption",
            "CardEffectSetMoonPhase", "CardEffectAdvanceMoonPhase",
            "CardEffectAdjustForgePoints", "CardEffectDiscardHand", "CardEffectRandomDiscard", "CardEffectChooseDiscard", "CardEffectAddBattleCard",
            "CardEffectHeal", "CardEffectHealAndOverheal", "CardEffectHealTrain", "CardEffectSetHealth", "CardEffectSacrifice", "CardEffectKill", "CardEffectRecursion", "CardEffectBuffDamage", "CardEffectDebuffDamage",
            "CardEffectBuffMaxHealth", "CardEffectDebuffMaxHealth", "CardEffectAddStatusEffect", "CardEffectMultiplyStatusEffect",
            "CardEffectMultiplyAllPositiveStatusEffects", "CardEffectAdjustAllStatusEffects", "CardEffectAddStatusEffectPerOtherEffect",
            "CardEffectSpawnMonsterOrAddStatusEffect", "CardEffectFloorRearrange", "CardEffectAddCardUpgradeToUnits", "CardEffectDrawType",
            "CardEffectAddTempCardUpgradeToNextDrawnCard", "CardEffectAddTempCardUpgradeToUnits", "CardEffectRemoveTempUpgradeFromUnit",
            "CardEffectAddCardUpgradeToUnitsPerStatusEffect",
            "CardEffectAddTempCardUpgradeToCardsInHand", "CardEffectAddPermanentCardUpgradeToCardsInHand",
            "CardEffectAddTempCardUpgradeToSpellsInDeck", "CardEffectAddUpgradedCopy"
        }, StringComparer.Ordinal);
        private readonly FullBattleTrace trace;
        private readonly UnitPlayModelProbe projection;
        private readonly ManualLogSource log;
        private readonly List<Record> records = new List<Record>();
        private BattlePlayRules? rules;
        internal IReadOnlyList<Record> Records => records;
        internal void InvalidateRules() => rules = null;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);
        internal BattleActionProbe(FullBattleTrace trace, UnitPlayModelProbe projection, ManualLogSource log)
        { this.trace = trace; this.projection = projection; this.log = log; }

        internal BattlePlayRules CaptureRules(EnemySpawnState spawn)
        {
            if (rules != null) return rules;
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            var nativeCards = cards.GetAllCards(new List<CardState>()).ToArray();
            var definitions = nativeCards.Select(card => card.GetCardDataID()).Concat(spawn.Waves.SelectMany(wave => wave.Candidates)
                .SelectMany(group => group.Units).Concat(spawn.Treasures).SelectMany(unit => unit.Unit.Triggers)
                .SelectMany(trigger => trigger.Effects).SelectMany(effect => effect.CardPool)).Distinct().ToArray();
            definitions = definitions.Concat(spawn.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units)
                .Concat(spawn.Treasures).Select(unit => unit.Unit.Ability?.DataId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>()).Distinct().ToArray();
            definitions = definitions.Concat(spawn.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units)
                .Concat(spawn.Treasures).SelectMany(unit => unit.Unit.Triggers).SelectMany(trigger => trigger.Effects)
                .Select(effect => effect.Action?.AbilityChange?.Definition?.DataId).Where(id => !string.IsNullOrEmpty(id)).Cast<string>()).Distinct().ToArray();
            definitions = definitions.Concat(nativeCards.SelectMany(card => card.GetCardStateModifiers().GetCardUpgrades()
                .Concat(card.GetTemporaryCardStateModifiers().GetCardUpgrades())).Select(upgrade => upgrade.GetUnitAbilityUpgrade())
                .Where(ability => ability != null).Select(ability => ability!.GetID())).Distinct().ToArray();
            var unplayedOrder = new Dictionary<string, int>(StringComparer.Ordinal);
            IReadOnlyList<CardData> simultaneousUnplayed = save.GetAllGameData().GetBalanceData()
                .GetCardsThatResolveSimultaneouslyOnUnplayed();
            for (int index = 0; index < simultaneousUnplayed.Count; index++)
            {
                CardData? card = simultaneousUnplayed[index];
                if (card != null && !unplayedOrder.ContainsKey(card.GetID())) unplayedOrder.Add(card.GetID(), index);
            }
            RoomManager rooms = managers.GetRoomManager()!;
            var roomRules = new List<RoomPlayRule>();
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                RoomState room = rooms.GetRoom(index);
                CapacityInfo capacity = room.GetCapacityInfo(Team.Type.Monsters);
                roomRules.Add(new RoomPlayRule(index, capacity.max, capacity.numSpawnPoints,
                    room.IsRoomEnabled(), room.IsRoomSummonBlocked(managers.GetRelicManager()), room.GetIsPyreRoom(),
                    room.GetCapacityInfo(Team.Type.Heroes).max));
            }
            bool hasMix = definitions.Any(id => save.GetAllGameData().FindCardData(id)?.GetEffects().Any(effect =>
                effect.GetEffectStateName() is "CardEffectMix" or "CardEffectMixRandom") == true);
            ElixirCatalog? elixirs = hasMix ? ElixirCatalogProbe.Capture(AllGameManagers.Instance!) : null;
            var reachable = new Dictionary<string, CardPlayRule>(StringComparer.Ordinal);
            var pending = new Queue<string>(definitions);
            if (elixirs != null)
                foreach (ElixirRecipe recipe in elixirs.Recipes) pending.Enqueue(recipe.Creation.DataId);
            while (pending.Count > 0)
            {
                string id = pending.Dequeue(); if (reachable.ContainsKey(id)) continue;
                CardPlayRule rule = Definition(save.GetAllGameData().FindCardData(id)!, unplayedOrder); reachable.Add(id, rule);
                CombatUnit[] spawned = new[] { rule.SpawnUnit, rule.Summon?.Additional?.Unit }
                    .Where(unit => unit != null).Cast<CombatUnit>()
                    .Concat(rule.Summon?.Pool.Select(choice => choice.Unit) ?? Array.Empty<CombatUnit>()).ToArray();
                foreach (string child in rule.Effects.Where(effect => effect.Generation != null).SelectMany(effect => effect.Generation!.Pool).Select(card => card.DataId)
                    .Concat(spawned.SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects).SelectMany(effect => effect.CardPool)))
                    pending.Enqueue(child);
                foreach (string child in rule.Effects.Select(effect => effect.AbilityChange?.Definition?.DataId)
                    .Concat(spawned.SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects)
                        .Select(effect => effect.Action?.AbilityChange?.Definition?.DataId)).Where(child => !string.IsNullOrEmpty(child)).Cast<string>()) pending.Enqueue(child);
                foreach (string child in rule.Effects.Select(effect => effect.Upgrade?.AbilityUpgrade?.Definition?.DataId)
                    .Concat(rule.Equipment?.Upgrades.Select(upgrade => upgrade.AbilityUpgrade?.Definition?.DataId) ?? Array.Empty<string?>())
                    .Where(child => !string.IsNullOrEmpty(child)).Cast<string>()) pending.Enqueue(child);
                foreach (CombatUnit unit in spawned.Where(unit => unit.Ability?.HasAbility == true)) pending.Enqueue(unit.Ability!.DataId);
            }
            rules = new BattlePlayRules(roomRules, reachable.Values.OrderBy(rule => rule.DataId, StringComparer.Ordinal).ToArray(),
                new[] { "armor", "valor", "pyregel", "duality" }.Select(id => Status(id, 1)).ToArray(),
                reachable.Values.Any(rule => rule.Effects.Any(effect => effect.Type == "CopyUnits")) ? UnitCopyCatalogProbe.Capture(reachable.Values.ToArray(), spawn) : null,
                elixirs);
            return rules;
        }

        internal static CardPlayRule Definition(CardData data, IReadOnlyDictionary<string, int>? unplayedOrder = null)
        {
            var interactions = new List<string>();
            var onCastEffects = new List<CardActionEffect>();
            var onUnplayedNegativeEffects = new List<CardActionEffect>();
            bool hasUnsupportedUnplayedTriggers = false;
            int? unplayedNegativeOrder = unplayedOrder != null && unplayedOrder.TryGetValue(data.GetID(), out int triggerOrder)
                ? triggerOrder : (int?)null;
            var upgradeInteractions = data.GetTraits().Where(trait => !DamageScalingProbe.Known(trait.GetTraitStateName()))
                .Select(trait => "Upgrade trait " + trait.GetTraitStateName()).ToArray();
            foreach (CardTriggerEffectData trigger in data.GetCardTriggers())
            {
                string triggerType = trigger.GetTrigger().ToString();
                if (trigger.GetTriggerEffects().Count > 0)
                {
                    interactions.Add("Card triggers");
                    if (triggerType.StartsWith("OnUnplayed", StringComparison.Ordinal)) hasUnsupportedUnplayedTriggers = true;
                    continue;
                }
                if (triggerType == "OnCast")
                {
                    foreach (CardEffectData effect in trigger.GetCardEffects())
                        onCastEffects.Add(DescribeTriggerEffect(effect, interactions, data));
                }
                else if (triggerType == "OnUnplayedNegative")
                {
                    int priorInteractions = interactions.Count;
                    CardActionEffect[] actions = trigger.GetCardEffects()
                        .Select(effect => DescribeTriggerEffect(effect, interactions, data)).ToArray();
                    if (actions.Length > 0 && interactions.Count == priorInteractions && actions.All(effect => effect.Type != "Unsupported"))
                        onUnplayedNegativeEffects.AddRange(actions);
                    else
                    {
                        hasUnsupportedUnplayedTriggers = true;
                        interactions.Add("Card triggers");
                    }
                }
                else
                {
                    interactions.Add("Card triggers");
                    if (triggerType.StartsWith("OnUnplayed", StringComparison.Ordinal)) hasUnsupportedUnplayedTriggers = true;
                }
            }
            if (data.GetTraits().Any(trait => !DamageScalingProbe.Known(trait.GetTraitStateName()))) interactions.Add("Card traits");
            bool selfPurge = data.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitSelfPurge");
            CardEffectData[] effects = data.GetEffects().ToArray();
            string kind = effects.Length == 0 ? "CardEffectNULL" : effects.Length == 1 ? effects[0].GetEffectStateName() : "MultipleEffects";
            CombatUnit? template = null;
            UnitSummonRule? summon = null;
            var spellEffects = new List<CardActionEffect>();
            string destination = selfPurge ? "Purged" : "Discard";
            if (kind == "CardEffectSpawnMonster")
            {
                CardEffectData effect = effects[0];
                CharacterData? unit = effect.GetParamCharacterData();
                CharacterData[] pool = effect.GetParamCharacterDataPool()?.ToArray() ?? Array.Empty<CharacterData>();
                if (unit == null && pool.Length == 0 ||
                    effect.GetTargetMode() != TargetMode.Room || selfPurge)
                    interactions.Add("Additional/modified unit spawn");
                CardData? fallback = !effect.GetParamBool() || unit == null ? null : Fallback(unit);
                CardData? primarySource = effect.GetParamBool() ? fallback : data;
                if (unit != null)
                    template = SpawnTemplate(unit, primarySource?.GetSpawnCharacterData() == null || primarySource.GetSpawnCharacterData() == unit, interactions);
                kind = "SpawnMonster"; destination = "Standby";
                if (effect.GetParamInt() > 1 || effect.GetParamCardUpgradeData() != null || effect.GetParamBool() || effect.GetParamAdditionalCharacterData() != null || pool.Length > 0)
                {
                    CardUpgradeModifier? upgrade = null;
                    if (effect.GetParamCardUpgradeData() != null)
                    { var state = new CardUpgradeState(); state.Setup(effect.GetParamCardUpgradeData()); upgrade = CardModifierProbe.Upgrade(state); }
                    CharacterData? additional = effect.GetParamAdditionalCharacterData();
                    CardData? additionalFallback = !effect.GetParamBool() || additional == null ? null : Fallback(additional);
                    CardData? additionalSource = effect.GetParamBool() ? additionalFallback : data;
                    UnitSummonChoice? choice = additional == null ? null : new UnitSummonChoice(
                        SpawnTemplate(additional, additionalSource?.GetSpawnCharacterData() == null || additionalSource.GetSpawnCharacterData() == additional, interactions),
                        additionalFallback == null ? null : CardGenerationProbe.Creation(additionalFallback), effect.GetParamBool());
                    var choices = new List<UnitSummonChoice>();
                    foreach (CharacterData candidate in pool)
                    {
                        if (candidate == null) { interactions.Add("Null character in unit pool"); continue; }
                        CardData? candidateFallback = !effect.GetParamBool() ? null : Fallback(candidate);
                        CardData? candidateSource = effect.GetParamBool() ? candidateFallback : data;
                        choices.Add(new UnitSummonChoice(SpawnTemplate(candidate,
                            candidateSource?.GetSpawnCharacterData() == null || candidateSource.GetSpawnCharacterData() == candidate, interactions),
                            candidateFallback == null ? null : CardGenerationProbe.Creation(candidateFallback), effect.GetParamBool()));
                    }
                    summon = new UnitSummonRule(effect.GetParamInt(), CardGenerationProbe.Creation(data), Status("cardless", 1), upgrade,
                        effect.GetParamBool(), fallback == null ? null : CardGenerationProbe.Creation(fallback), choice,
                        choices, pool.Length == 0 ? null : (int?)(data.GetSpawnCharacterData()?.GetSize() ?? 0),
                        data.GetSpawnCharacterData() != null, effect.GetParamBool());
                }
            }
            else if (kind == "CardEffectNULL") kind = "Null";
            else if ((data.GetCardType() == CardType.Spell || data.GetCardType() == CardType.Equipment || data.GetCardType() == CardType.TrainRoomAttachment) && effects.Length > 0 && effects.All(SupportedSpellEffect))
            {
                kind = "Spell";
                foreach (CardEffectData effect in effects)
                    spellEffects.Add(DescribeSpellEffect(data, effect, interactions));
            }
            else interactions.Add("Unimplemented play effect " + kind);
            EquipmentDefinition? equipment = null;
            if (effects.Any(effect => effect.GetEffectStateName() == "CardEffectAttachEquipment"))
            {
                var upgrades = effects.Where(effect => effect.GetParamCardUpgradeData() != null).Select(effect =>
                { var state = new CardUpgradeState(); state.Setup(effect.GetParamCardUpgradeData()); return CardModifierProbe.Upgrade(state); }).ToArray();
                equipment = new EquipmentDefinition(upgrades, data.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitReturnToHandEquipment"), data.GetID());
                interactions.AddRange(upgrades.SelectMany(upgrade => upgrade.ExternalInteractions));
                if (data.GetCardType() == CardType.Equipment) { kind = "Equipment"; destination = "Standby"; }
            }
            if (data.GetCardType() == CardType.TrainRoomAttachment) destination = "Standby";
            return new CardPlayRule(data.GetID(), data.name, data.GetCost(), kind, destination, template,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), spellEffects, upgradeInteractions,
                HandInteractions(data, false), HandInteractions(data, true), data.GetCostType().ToString(), equipment,
                data.IsUnitAbility() ? new CardAbilityRule("Unit", data.CanAbilityTargetOtherFloors(),
                    data.GetEffects().All(effect => ((ICardEffect)Activator.CreateInstance(typeof(CardState).Assembly
                        .GetType(effect.GetEffectStateName())!)!).CanPlayWhenHandFull)) : null, summon,
                data.GetCardType().ToString(), data.IsUnitAbility() || data.IsRoomAbility(),
                effects.All(effect => ((ICardEffect)Activator.CreateInstance(typeof(CardEffectBase).Assembly
                    .GetType(effect.GetEffectStateName())!)!).CanPlayInEngineRoom), onCastEffects: onCastEffects,
                onUnplayedNegativeEffects: onUnplayedNegativeEffects, unplayedNegativeOrder: unplayedNegativeOrder,
                hasUnsupportedUnplayedTriggers: hasUnsupportedUnplayedTriggers);
        }

        private static bool SupportedSpellEffect(CardEffectData effect) =>
            AbilityLifecycleProbe.Known(effect.GetEffectStateName()) || AbilityCooldownProbe.Known(effect.GetEffectStateName()) ||
            SpellEffectTypes.Contains(effect.GetEffectStateName());
        internal static bool SupportsCardActionEffect(CardEffectData effect) => SupportedSpellEffect(effect);

        internal static CardActionEffect DescribeTriggerEffect(CardEffectData effect, List<string> interactions, CardData? source = null)
        {
            if (!SupportedSpellEffect(effect))
            {
                interactions.Add("Unimplemented card-trigger effect " + effect.GetEffectStateName());
                return new CardActionEffect("Unsupported", "None", 0, false, false, Array.Empty<CombatStatus>());
            }
            return DescribeSpellEffect(source, effect, interactions);
        }

        private static CardActionEffect DescribeSpellEffect(CardData? data, CardEffectData effect, List<string> interactions)
        {
            if (AbilityLifecycleProbe.Known(effect.GetEffectStateName())) return AbilityLifecycleProbe.Describe(effect);
            if (AbilityCooldownProbe.Known(effect.GetEffectStateName())) return AbilityCooldownProbe.Describe(effect, effect.GetParamInt(), effect.GetParamBool());
            bool handUpgrade = effect.GetEffectStateName() == "CardEffectAddTempCardUpgradeToCardsInHand" ||
                effect.GetEffectStateName() == "CardEffectAddPermanentCardUpgradeToCardsInHand";
            bool deckSpellUpgrade = effect.GetEffectStateName() == "CardEffectAddTempCardUpgradeToSpellsInDeck";
            bool upgradedCopy = effect.GetEffectStateName() == "CardEffectAddUpgradedCopy";
            bool cardOnlyDrawTarget = (effect.GetEffectStateName() == "CardEffectDrawType" ||
                effect.GetEffectStateName() == "CardEffectAddTempCardUpgradeToNextDrawnCard") &&
                (effect.GetTargetMode() == TargetMode.DrawPile || effect.GetTargetMode() == TargetMode.Hand ||
                 effect.GetTargetMode() == TargetMode.LastDrawnCard);
            bool randomDiscardHandTarget = (effect.GetEffectStateName() == "CardEffectRandomDiscard" ||
                effect.GetEffectStateName() == "CardEffectChooseDiscard") && effect.GetTargetMode() == TargetMode.Hand;
            bool recursionCardPileTarget = effect.GetEffectStateName() == "CardEffectRecursion" &&
                (effect.GetTargetMode() == TargetMode.Exhaust || effect.GetTargetMode() == TargetMode.Eaten || effect.GetTargetMode() == TargetMode.Discard);
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            if (effect.GetEffectStateName() == "CardEffectNULL" &&
                (effect.GetParamBool() || effect.GetAdditionalParamInt() is < 0 or > 1))
                interactions.Add("Unmodeled null-effect targeting or forwarding");
            if (effect.GetEffectStateName() == "CardEffectTransferAllStatusEffectsFromUnit" &&
                (effect.GetTargetMode() != TargetMode.LastSacrificedCharacter || effect.GetParamInt() != 2 ||
                 effect.GetAdditionalParamInt() != 0 || effect.GetAdditionalParamInt1() != 0 ||
                 effect.GetParamBool() || effect.GetParamBool2()))
                interactions.Add("Unmodeled status-transfer mode or parameters");
            if (effect.GetEffectStateName() == "CardEffectTransferAllStatusEffects" &&
                (effect.GetTargetMode() != TargetMode.DropTargetCharacter || effect.GetParamStatusEffects().Length != 1))
                interactions.Add("Unmodeled room-status transfer targets or status pool");
            if (effect.GetUseStatusEffectStackMultiplier() || effect.GetUseHealthMissingStackMultiplier() || effect.GetUseMagicPowerMultiplier() ||
                effect.GetEffectStateName() != "CardEffectSacrifice" && effect.GetEffectStateName() != "CardEffectKill" &&
                effect.GetEffectStateName() != "CardEffectRecursion" && effect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" &&
                !effect.GetParamSubtype().IsNone ||
                (handUpgrade ? effect.GetTargetMode() != TargetMode.Hand && effect.GetTargetMode() != TargetMode.Room :
                    !cardOnlyDrawTarget && !randomDiscardHandTarget && !recursionCardPileTarget && !CardTargetModel.Supports(effect.GetTargetMode().ToString())))
                interactions.Add("Spell scaling or target filters");
            string type = EnergyModel.IsNativeEffect(effect.GetEffectStateName()) ? EnergyEffectProbe.Type(effect) :
                effect.GetEffectStateName() == "CardEffectNULL" ? "Null" :
                effect.GetEffectStateName() == "CardEffectAttachTrainRoomAttachment" ? "AttachRoomCapacity" :
                effect.GetEffectStateName() == "CardEffectAdjustForgePoints" ? ForgePointsModel.Effect :
                effect.GetEffectStateName() == "CardEffectCopyUnits" ? "CopyUnits" :
                effect.GetEffectStateName() == "CardEffectBump" ? "Bump" :
                effect.GetEffectStateName() == "CardEffectUnitAbilityTeleport" ? "AbilityTeleport" :
                effect.GetEffectStateName() == "CardEffectAttackWithUnit" ? "AttackWithUnit" :
                effect.GetEffectStateName() == "CardEffectAttachEquipment" ? "AttachEquipment" :
                effect.GetEffectStateName() == "CardEffectBuffEquipment" ? "BuffEquipment" :
                effect.GetEffectStateName() == "CardEffectRemoveEquipment" ? "RemoveEquipment" :
                effect.GetEffectStateName() == "CardEffectAdjustDragonsHoard" ? "AdjustDragonsHoard" :
                effect.GetEffectStateName() == "CardEffectRewardGoldPerDragonHoard" ? "RewardGoldPerDragonHoard" :
                effect.GetEffectStateName() == "CardEffectRewardGold" ? "RewardGold" :
                effect.GetEffectStateName() == "CardEffectDamage" ? "Damage" :
                effect.GetEffectStateName() == "CardEffectDamagePerSourceAttack" ? "DamagePerSourceAttack" :
                effect.GetEffectStateName() == "CardEffectDamagePerHp" ? "DamagePerHp" :
                effect.GetEffectStateName() == "CardEffectDamageByUnitsKilled" ? "DamageByUnitsKilled" :
                effect.GetEffectStateName() == "CardEffectTransferAttackFromUnit" ? "TransferAttackFromUnit" :
                effect.GetEffectStateName() == "CardEffectTransferAllStatusEffectsFromUnit" ? "TransferAllStatusesFromUnit" :
                effect.GetEffectStateName() == "CardEffectTransferAllStatusEffects" ? "TransferAllStatuses" :
                effect.GetEffectStateName() == "CardEffectAdjustRoomCapacity" ? "AdjustCapacity" :
                effect.GetEffectStateName() == "CardEffectAdjustRoomMaxCorruption" ? "AdjustMaxCorruption" :
                effect.GetEffectStateName() == "CardEffectAdjustCorruption" ? "AdjustCorruption" :
                effect.GetEffectStateName() == "CardEffectAddPermanentCorruption" ? "AddPermanentCorruption" :
                effect.GetEffectStateName() == "CardEffectSetMoonPhase" ? "SetMoonPhase" :
                effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase" ? "AdvanceMoonPhase" :
                effect.GetEffectStateName() == "CardEffectDraw" ? "Draw" :
                effect.GetEffectStateName() == "CardEffectDrawType" ? "DrawType" :
                effect.GetEffectStateName() == "CardEffectAddTempCardUpgradeToNextDrawnCard" ? "ScheduleNextDrawUpgrade" :
                effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn" ? "DrawNextTurn" :
                effect.GetEffectStateName() == "CardEffectDiscardHand" ? "DiscardHand" :
                effect.GetEffectStateName() == "CardEffectRandomDiscard" ? "RandomDiscard" :
                effect.GetEffectStateName() == "CardEffectChooseDiscard" ? "ChooseDiscard" :
                effect.GetEffectStateName() == "CardEffectAddBattleCard" ? "Generate" :
                deckSpellUpgrade ? "UpgradeDeckSpells" : upgradedCopy ? "GenerateUpgradedCopy" :
                effect.GetEffectStateName() == "CardEffectBuffDamage" ? "BuffAttack" :
                effect.GetEffectStateName() == "CardEffectDebuffDamage" ? "DebuffAttack" :
                effect.GetEffectStateName() == "CardEffectBuffMaxHealth" ? "BuffHealth" :
                effect.GetEffectStateName() == "CardEffectDebuffMaxHealth" ? "DebuffHealth" :
                effect.GetEffectStateName() == "CardEffectHeal" ? "Heal" :
                effect.GetEffectStateName() == "CardEffectHealAndOverheal" ? "HealOverheal" :
                effect.GetEffectStateName() == "CardEffectHealTrain" ? "HealTrain" :
                effect.GetEffectStateName() == "CardEffectSetHealth" ? "SetHealth" :
                effect.GetEffectStateName() == "CardEffectSacrifice" ? "Sacrifice" :
                effect.GetEffectStateName() == "CardEffectKill" ? "Kill" :
                effect.GetEffectStateName() == "CardEffectRecursion" ? "Recursion" :
                effect.GetEffectStateName() == "CardEffectFloorRearrange" ? "FloorRearrange" :
                effect.GetEffectStateName() == "CardEffectAddStatusEffectPerOtherEffect" ? "AddStatusPerOtherEffect" :
                effect.GetEffectStateName() == "CardEffectMultiplyStatusEffect" ? "MultiplyStatus" :
                effect.GetEffectStateName() == "CardEffectMultiplyAllPositiveStatusEffects" ? "MultiplyAllPositiveStatusEffects" :
                effect.GetEffectStateName() == "CardEffectAdjustAllStatusEffects" ? "AdjustAllStatusEffects" :
                effect.GetEffectStateName() == "CardEffectSpawnMonsterOrAddStatusEffect" ? "SpawnOrStatus" :
                effect.GetEffectStateName() == "CardEffectAddStatusEffect" ? "AddStatus" :
                effect.GetEffectStateName() == "CardEffectRemoveAllStatusEffects" ? "RemoveAllStatusEffects" :
                handUpgrade ? "HandUpgrade" : effect.GetEffectStateName() == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" :
                effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnitsPerStatusEffect" ? "UnitUpgradePerStatus" : "UnitUpgrade";
            CardUpgradeModifier? upgrade = type == "DrawNextTurn" ? BonusDrawProbe.Upgrade(effect) : null;
            IReadOnlyList<CardUpgradeMaskRule> upgradeFilters = Array.Empty<CardUpgradeMaskRule>();
            CardGenerationRule? generation = null;
            if (upgradedCopy)
            {
                CardUpgradeData? copyUpgradeData = effect.GetParamCardUpgradeData();
                if (copyUpgradeData != null)
                {
                    var upgradeState = new CardUpgradeState(); upgradeState.Setup(copyUpgradeData);
                    upgrade = CardModifierProbe.Upgrade(upgradeState);
                    interactions.AddRange(CardModifierModel.UpgradeInteractions(upgrade));
                }
                if (data == null) interactions.Add("Upgraded-copy trigger requires its source card definition");
                else generation = new CardGenerationRule(((CardPile)effect.GetParamInt()).ToString(), 1,
                    new[] { CardGenerationProbe.Creation(data) }, copyModifiers: effect.GetCopyModifiersFromSource(),
                    ignoreTemporaryModifiers: effect.GetIgnoreTempModifiersFromSource(), upgrade: upgrade,
                    deterministicPoolSelection: true, requireCopyableSource: true,
                    upgradeIsPermanent: effect.GetParamBool());
            }
            if (type == "Recursion" && effect.GetParamCardUpgradeData() != null)
            {
                var upgradeState = new CardUpgradeState(); upgradeState.Setup(effect.GetParamCardUpgradeData());
                upgrade = CardModifierProbe.Upgrade(upgradeState);
                interactions.AddRange(CardModifierModel.UpgradeInteractions(upgrade));
            }
            if (effect.GetUseIntRange() && !EnergyModel.IsEffect(type) && type != ForgePointsModel.Effect &&
                !new[] { "CopyUnits", "Bump", "Damage", "DamagePerSourceAttack", "DamagePerHp", "Heal", "AddStatus", "MultiplyStatus",
                    "MultiplyAllPositiveStatusEffects", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth", "Draw", "DrawType",
                    "ScheduleNextDrawUpgrade", "DrawNextTurn", "AdjustCapacity", "DiscardHand", "RandomDiscard", "Generate" }.Contains(type))
                interactions.Add("Unimplemented integer range consumer " + type);
            string lifetime = "";
            if (type == "BuffHealth") lifetime = ((UnitUpgradeLifetimeTempOnly)effect.GetAdditionalParamInt1()).ToString();
            bool scaleUpgrade = (effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnits" ||
                effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnitsPerStatusEffect" ||
                effect.GetEffectStateName() == "CardEffectBuffEquipment") && effect.GetParamBool();
            if (type == "UnitUpgrade" || type == "UnitUpgradePerStatus" || type == "RemoveUnitUpgrade" ||
                type == "AttachEquipment" || type == "BuffEquipment" || handUpgrade || deckSpellUpgrade)
            {
                if (effect.GetParamCardUpgradeData() == null) interactions.Add("Missing unit upgrade data");
                else
                {
                    CardUpgradeData upgradeData = effect.GetParamCardUpgradeData();
                    var upgradeState = new CardUpgradeState(); upgradeState.Setup(upgradeData);
                    bool filtersIgnoredByEffect = effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnits" ||
                        effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnitsPerStatusEffect" || deckSpellUpgrade;
                    upgrade = CardModifierProbe.Upgrade(upgradeState, false, filtersAlreadyApplied: filtersIgnoredByEffect);
                    interactions.AddRange(upgrade.ExternalInteractions);
                    if (deckSpellUpgrade) upgradeFilters = upgradeData.GetFilters().Select(CardUpgradeMaskProbe.Rule).ToArray();
                    if (scaleUpgrade && !upgradeData.CanScaleUpgradeToSingleInstance()) interactions.Add("Invalid scaled unit upgrade definition");
                }
                lifetime = type == "BuffEquipment" ? "TemporaryUntilUnitDeath" : deckSpellUpgrade ? "TemporaryUntilEndOfBattle" : handUpgrade ?
                    effect.GetEffectStateName() == "CardEffectAddPermanentCardUpgradeToCardsInHand" ? "Permanent" : "TemporaryUntilEndOfBattle" :
                    type == "AttachEquipment" ? "" : ((UnitUpgradeLifetime)effect.GetAdditionalParamInt1()).ToString();
            }
            var statuses = new List<CombatStatus>();
            if (type == "AddStatus" || type == "HealOverheal" || type == "MultiplyStatus" || type == "AdjustAllStatusEffects" ||
                type == "TransferAllStatuses" ||
                type == "AddStatusPerOtherEffect" || type == "SpawnOrStatus" || type == "UnitUpgradePerStatus")
            {
                if (type != "AdjustAllStatusEffects" && type != "HealOverheal" &&
                    effect.GetParamStatusEffects().Length == 0) interactions.Add("Empty status effect pool");
                foreach (StatusEffectStackData status in effect.GetParamStatusEffects())
                {
                    if (type == "AddStatus" && !StatusEffectManager.Instance.GetStatusEffectDataById(status.statusId)!.IsStackable() &&
                        (effect.GetParamBool() || effect.GetTargetMode() == TargetMode.DropTargetCharacter))
                        interactions.Add("Nonstackable status legality");
                    statuses.Add(Status(status.statusId, status.count));
                }
            }
            if (type == "AdjustAllStatusEffects" && effect.GetAdditionalParamInt() != 0 && effect.GetAdditionalParamInt() != 1)
                interactions.Add("Unknown all-status adjustment filter");
            CombatUnit? spawnFallbackUnit = null;
            if (type == "SpawnOrStatus")
            {
                CharacterData? fallback = effect.GetParamCharacterData();
                if (fallback == null) interactions.Add("Missing spawn-or-status fallback character.");
                else spawnFallbackUnit = SpawnTemplate(fallback, true, interactions);
            }
            RoomCapacityAttachmentState? roomAttachment = null;
            if (type == "AttachRoomCapacity")
            {
                if (data == null) interactions.Add("Room attachment trigger requires card ownership.");
                else roomAttachment = CapacityAttachment(data, effect, interactions);
            }
            return new CardActionEffect(type, effect.GetTargetMode().ToString(), effect.GetParamInt(),
                type == "SpawnOrStatus" || effect.GetTargetTeamType().HasFlag(Team.Type.Heroes),
                type != "SpawnOrStatus" && effect.GetTargetTeamType().HasFlag(Team.Type.Monsters), statuses, upgrade, lifetime,
                new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(), effect.GetShouldCancelSubsequentEffectsIfTestFails(),
                    (type == "AddStatus" || type == "AdjustAllStatusEffects") && effect.GetParamBool(),
                    ((ICardEffect)Activator.CreateInstance(typeof(CardEffectBase).Assembly.GetType(effect.GetEffectStateName())!)!).CanPlayAfterBossDead,
                    mustHaveValidTargets: type == "Kill" && effect.GetParamBool3()),
                effect.GetUseIntRange() ? new CardEffectRange(effect.GetParamMinInt(), effect.GetParamMaxInt(), effect.GetParamMultiplier()) : null,
                new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()),
                type == "Generate" ? CardGenerationProbe.Definition(effect) : generation, type == "AdjustCapacity" && effect.GetParamBool(),
                copyHeroStats: type == "CopyUnits" ? effect.GetParamBool() : (bool?)null,
                scaleUpgrade: scaleUpgrade, doNotFullyRemove: type == "Sacrifice" ? effect.GetParamBool() : (bool?)null,
                targetSubtype: type == "DamageByUnitsKilled" ? effect.GetParamStr() :
                    (type == "Sacrifice" || type == "Kill") && !effect.GetParamSubtype().IsNone ? effect.GetParamSubtype().Key : "",
                typedDraw: type == "DrawType" ? TypedDrawProbe.DrawRule(effect) : null,
                nextDrawUpgrade: type == "ScheduleNextDrawUpgrade" ? TypedDrawProbe.UpgradeRule(effect) : null,
                requiredGold: type == "ScheduleNextDrawUpgrade" ? effect.GetAdditionalParamInt() : 0,
                goldCostRequired: type == "RewardGold" ? effect.GetParamBool() : (bool?)null,
                useCardTraits: (type == "DamagePerSourceAttack" || type == "HealTrain") && effect.GetParamBool(),
                requireEquipment: type == "AttackWithUnit" && effect.GetParamBool(), useSelectedRoom: type == "AttackWithUnit" && effect.GetParamBool2(),
                countStatusOnlyOnTargets: type == "AddStatusPerOtherEffect" && effect.GetParamBool(), spawnFallbackUnit: spawnFallbackUnit,
                disallowStatusStackModifiers: type == "SpawnOrStatus" && effect.GetDisallowStatusEffectStackModifiers(),
                statusAdjustmentFilter: type == "AdjustAllStatusEffects" ? (int?)effect.GetAdditionalParamInt() : null,
                damagePerHpLastAttacker: type == "DamagePerHp" && effect.GetParamBool(),
                damagePerHpIncludeArmor: type == "DamagePerHp" && effect.GetParamBool2(), roomAttachment: roomAttachment,
                recursion: type == "Recursion" ? new CardRecursionRule(effect.GetTargetCardSelectionMode().ToString(), effect.GetTargetCardType().ToString(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key, effect.GetParamBool(), effect.GetParamBool2(),
                    effect.GetParamCardFilter() != null, effect.GetParamCardUpgradeData() != null,
                    effect.GetParamCardFilter() == null ? null : CardUpgradeMaskProbe.Rule(effect.GetParamCardFilter())) : null,
                upgradeFilters: upgradeFilters,
                removeHalfStatusEffects: type == "RemoveAllStatusEffects" ? (bool?)effect.GetParamBool() : null,
                overheal: type == "HealOverheal" ? new OverhealRule(effect.GetAdditionalParamInt(), effect.GetAdditionalParamInt1()) : null,
                corruptionAdd: type == "AdjustCorruption" ? (bool?)effect.GetParamBool() : null,
                nullTargetless: type == "Null" && effect.GetParamBool2(),
                nullForwardSelf: type == "Null" && effect.GetAdditionalParamInt() == 1,
                chooseDiscard: type == "ChooseDiscard" ? new CardChooseDiscardRule(effect.GetAdditionalParamInt(),
                    effect.GetParamBool(), effect.GetParamBool2(), effect.GetParamCardPool() != null) : null,
                transferAttackMultiplier: type == "TransferAttackFromUnit" ? effect.GetAdditionalParamInt() : 0);
        }

        private static RoomCapacityAttachmentState? CapacityAttachment(CardData card, CardEffectData effect,
            List<string> interactions)
        {
            CardUpgradeData? definition = effect.GetParamCardUpgradeData();
            if (definition == null || effect.GetAdditionalParamInt() != 0)
            {
                interactions.Add("Unmodeled room attachment definition or type");
                return null;
            }
            var upgrade = new CardUpgradeState();
            upgrade.Setup(definition);
            RoomModifierData[] modifiers = definition.GetRoomModifierUpgrades().ToArray();
            bool supported = modifiers.Length == 1 && modifiers[0].GetRoomStateModifierClassName() == "RoomStateCapacityModifier" &&
                upgrade.GetAttackDamage() == 0 && upgrade.GetAttackDamageBuff() == 0 && upgrade.GetAdditionalHP() == 0 &&
                upgrade.GetAdditionalUnhealedHP() == 0 && upgrade.GetCostReduction() == 0 && upgrade.GetAdditionalSize() == 0 &&
                upgrade.GetAdditionalEquipmentLimit() == 0 && upgrade.GetStatusEffectUpgrades().Count == 0 &&
                upgrade.GetTraitDataUpgrades().Count == 0 && upgrade.GetRemoveTraitUpgrades().Count == 0 &&
                upgrade.GetTriggerUpgrades().Count == 0 && upgrade.GetCardTriggerUpgrades().Count == 0 &&
                upgrade.GetUpgradesToRemove().Count == 0 && upgrade.GetFilters().Count == 0 &&
                upgrade.GetUnitAbilityUpgrade() == null && upgrade.GetRoomAbilityUpgrade() == null && !upgrade.IsUnique();
            if (!supported)
            {
                interactions.Add("Unmodeled mixed room attachment payload");
                return null;
            }
            return new RoomCapacityAttachmentState(card.GetID(), card.name, effect.GetAdditionalParamInt(),
                modifiers[0].GetParamInt());
        }

        private static CardData? Fallback(CharacterData unit) => AllGameManagers.Instance!.GetSaveManager().GetAllGameData()
            .GetAllCardData().FirstOrDefault(candidate => candidate != null && candidate.IsSpawnerCard() &&
                candidate.GetSpawnCharacterData() != null && candidate.GetSpawnCharacterData()!.GetID() == unit.GetID());

        internal static CombatUnit SpawnTemplate(CharacterData unit, bool matchesSource, List<string> interactions)
        {
            EnemyDefinition definition = EnemySpawningProbe.Definition(unit);
            interactions.AddRange(definition.ExternalInteractions);
            if (unit.GetGraftedEquipment() != null) interactions.Add("Grafted equipment");
            CombatUnit source = definition.Unit;
            UnitModifiers? m = source.Modifiers;
            var modifiers = m == null ? null : new UnitModifiers(m.AttackDamage, m.AttackDamageAdded, m.DamageBuff,
                m.RawSize, m.EquipmentLimit, m.CanBeHealed, m.IsClone, m.Upgrades, m.HealthFromUpgrades, matchesSource,
                m.DamageBuffFromStatusEffects, m.MagebladeDamagePerMagicPower);
            return new CombatUnit(0, source.AssetKey, CombatTeam.Player, source.BaseAttack, source.Health,
                source.MaxHealth, source.CanAttack, false, false, source.Statuses, source.Triggers, size: source.Size,
                statusImmunities: source.StatusImmunities, subtypes: source.Subtypes, modifiers: modifiers, isBoss: source.IsBoss,
                lastAttackerId: 0, statusRegistry: source.StatusRegistry, equipmentCards: source.EquipmentCards,
                nextTriggerId: source.NextTriggerId, ability: source.Ability, statusDictionary: source.StatusDictionary,
                abilityRules: source.AbilityRules, hordeDefinition: source.HordeDefinition, isSpawning: source.IsSpawning,
                sacrificeCardId: source.SacrificeCardId, deathState: source.DeathState, bumpRules: source.BumpRules);
        }

        internal static CardPlayRule BirthDefinition(CardData data, CharacterData monster, bool sourceAbsent = false)
        {
            CardPlayRule rule = Definition(data);
            var interactions = rule.ExternalInteractions.ToList();
            CombatUnit unit = SpawnTemplate(monster, sourceAbsent || data.GetSpawnCharacterData() == null || data.GetSpawnCharacterData() == monster, interactions);
            return new CardPlayRule(rule.DataId, rule.AssetKey, rule.Cost, rule.Effect, rule.Destination, unit,
                interactions.Distinct().OrderBy(item => item, StringComparer.Ordinal).ToArray(), rule.Effects,
                rule.UpgradeInteractions, rule.HandDiscardInteractions, rule.HandConsumeInteractions, rule.CostType,
                rule.Equipment, rule.Ability, rule.Summon, rule.CardType, rule.IsAnyAbility, rule.CanPlayInEngineRoom, rule.OnCastEffects,
                rule.OnUnplayedNegativeEffects, rule.UnplayedNegativeOrder, rule.HasUnsupportedUnplayedTriggers);
        }

        private static string[] HandInteractions(CardData data, bool consume)
        {
            var interactions = new List<string>();
            if (data.GetCardTriggers().Any(trigger => trigger.GetTrigger().ToString().IndexOf("Discard", StringComparison.OrdinalIgnoreCase) >= 0))
                interactions.Add("Hand removal card triggers");
            foreach (var trait in data.GetTraits())
            {
                Type? type = typeof(CardTraitState).Assembly.GetType(trait.GetTraitStateName());
                if (type == null) { interactions.Add("Unknown hand removal trait"); continue; }
                if (consume)
                {
                    if (typeof(CardTraitSalvage).IsAssignableFrom(type)) interactions.Add("Salvage consumption callback");
                    if (type == typeof(CardTraitGraftedEquipment)) interactions.Add("Grafted equipment standby trait state");
                }
                else if (type == typeof(CardTraitEphemeral) || type == typeof(CardTraitInfusion) || type == typeof(CardTraitPersistent) || type == typeof(CardTraitTreasure) ||
                    type != typeof(CardTraitSelfPurge) && type != typeof(CardTraitExhaustState) && type.GetMethod("OnCardDiscarded")?.DeclaringType != typeof(CardTraitState))
                    interactions.Add(type.Name + " hand discard callback/routing");
            }
            return interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        internal static CombatStatus Status(string id, int count)
        {
            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager == null) throw new InvalidOperationException("Status data is unavailable while capturing '" + id + "'.");
            StatusEffectData? rule = manager.GetStatusEffectDataById(id);
            if (rule == null) throw new InvalidOperationException("Unknown status while capturing card rules: '" + id + "'.");
            return new CombatStatus(id, count, rule.GetParamInt(), rule.GetRemoveWhenTriggered(), rule.GetRemoveStackAtEndOfTurn(),
                rule.GetRemoveAtEndOfTurn(), rule.GetRemoveAtEndOfTurnAfterPostCombat(), id == "cooldown", rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                TriggeredVfx(rule, -1f), TriggeredVfx(rule, 1f), rule.IsStackable(), rule.IsHidden(), rule.GetDisplayCategory().ToString(),
                rule.IsPropagatable(), rule.GetExcludeHeroPropagation(), rule.GetExcludeMonsterPropagation(),
                rule.GetRemoveWhenTriggeredAfterCardPlayed(), rule.GetRemoveAtEndOfTurnIfTriggered());
        }

        internal static bool TriggeredVfx(StatusEffectData rule, float facing) => rule.GetOnTriggeredVFX()?.GetVfxPrefab(facing) != null;

        internal void Begin(PlayCardAction action, bool scenarioAction = false,
            bool includeOuterBossForBump = false, bool ignoreOuterBossSpawnInteraction = false)
        {
            try
            {
                if (records.Any(record => record.Actual == null)) throw new InvalidOperationException("Previous card action is pending.");
                trace.InvalidateCardRules();
                BattleTurnState before = trace.CaptureDecision(includeOuterBossForBump, ignoreOuterBossSpawnInteraction);
                records.Add(new Record { Index = records.Count, ScenarioAction = scenarioAction,
                    IncludeOuterBossForBump = includeOuterBossForBump,
                    IgnoreOuterBossSpawnInteraction = ignoreOuterBossSpawnInteraction, Before = before, Action = action,
                    Predicted = BattleActionModel.PlayCard(before, action) });
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal void Complete()
        {
            Record? record = records.LastOrDefault(item => item.Actual == null);
            if (record == null) return;
            try
            {
                trace.InvalidateCardRules();
                record.Actual = trace.CaptureDecision(record.IncludeOuterBossForBump,
                    record.IgnoreOuterBossSpawnInteraction);
                record.ActualOutcome = trace.NativeWon == null ? RoomOutcome.Exchanged :
                    trace.NativeWon.Value ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
                if (record.Predicted.Supported)
                {
                    bool terminal = record.Predicted.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                    JToken predicted = BattleTurnProbe.Comparable(record.Predicted.State!, terminal);
                    JToken actual = BattleTurnProbe.Comparable(record.Actual, terminal);
                    record.Difference = record.Predicted.Outcome != record.ActualOutcome ? "Card play terminal outcome differs" :
                        JToken.DeepEquals(predicted, actual) ? null : "Card play decision state differs";
                    if (record.Difference != null && record.Predicted.Outcome == record.ActualOutcome &&
                        predicted is JObject predictedObject && actual is JObject actualObject)
                    {
                        string fields = string.Join(",", predictedObject.Properties()
                            .Where(property => !JToken.DeepEquals(property.Value, actualObject[property.Name]))
                            .Select(property => property.Name));
                        log.LogInfo("ACTION-MODEL-DIFF index=" + record.Index + " fields=" + fields);
                    }
                    log.LogInfo("ACTION-MODEL-" + (record.Difference == null ? "MATCH" : "MISMATCH") + " index=" + record.Index);
                }
                else log.LogInfo("ACTION-MODEL-UNSUPPORTED " + record.Predicted.Reason);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal sealed class Record
        {
            public int Index { get; set; }
            public bool ScenarioAction { get; set; }
            public bool IncludeOuterBossForBump { get; set; }
            public bool IgnoreOuterBossSpawnInteraction { get; set; }
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction Action { get; set; } = null!;
            public BattleActionResult Predicted { get; set; } = null!;
            public BattleTurnState? Actual { get; set; }
            public RoomOutcome ActualOutcome { get; set; }
            public string? Difference { get; set; }
        }
    }
}
