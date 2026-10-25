using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardEffectTests
    {
        public bool ShouldTest { get; }
        public bool FailToCast { get; }
        public bool CancelSubsequent { get; }
        public bool StrictTargets { get; }
        public bool? CanPlayAfterBossDead { get; }
        public bool MustHaveValidTargets { get; }
        public CardEffectTests(bool shouldTest, bool failToCast, bool cancelSubsequent, bool strictTargets,
            bool? canPlayAfterBossDead = null, bool mustHaveValidTargets = false)
        { ShouldTest = shouldTest; FailToCast = failToCast; CancelSubsequent = cancelSubsequent; StrictTargets = strictTargets;
            CanPlayAfterBossDead = canPlayAfterBossDead; MustHaveValidTargets = mustHaveValidTargets; }
    }
    public sealed class CardActionEffect
    {
        public string Type { get; }
        public string Target { get; }
        public int Value { get; }
        public bool AllowEnemy { get; }
        public bool AllowPlayer { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public string Lifetime { get; }
        public CardEffectTests? Tests { get; }
        public CardEffectRange? Range { get; }
        public CardTargetFilters? Filters { get; }
        public CardGenerationRule? Generation { get; }
        public bool OnlyIfNoEnemies { get; }
        public bool? CooldownParameter { get; }
        public AbilityChangeRule? AbilityChange { get; }
        public bool? CopyHeroStats { get; }
        public bool ScaleUpgrade { get; }
        public bool? DoNotFullyRemove { get; }
        public string TargetSubtype { get; }
        public TypedDrawRule? TypedDraw { get; }
        public NextDrawUpgradeRule? NextDrawUpgrade { get; }
        public int RequiredGold { get; }
        public bool? GoldCostRequired { get; }
        public bool UseCardTraits { get; }
        public bool RequireEquipment { get; }
        public bool UseSelectedRoom { get; }
        public bool CountStatusOnlyOnTargets { get; }
        public CombatUnit? SpawnFallbackUnit { get; }
        public bool DisallowStatusStackModifiers { get; }
        public string? SourceStatusMultiplier { get; }
        public int? StatusAdjustmentFilter { get; }
        public bool? RemoveHalfStatusEffects { get; }
        public OverhealRule? Overheal { get; }
        public bool? CorruptionAdd { get; }
        public bool DamagePerHpLastAttacker { get; }
        public bool DamagePerHpIncludeArmor { get; }
        public RoomCapacityAttachmentState? RoomAttachment { get; }
        public CardRecursionRule? Recursion { get; }
        public CardUpgradeMaskRule? CardFilter { get; }
        public IReadOnlyList<CardUpgradeMaskRule> UpgradeFilters { get; }
        public bool NullTargetless { get; }
        public bool NullForwardSelf { get; }
        public CardChooseDiscardRule? ChooseDiscard { get; }
        public int TransferAttackMultiplier { get; }
        public StatusTransferRule? StatusTransfer { get; }
        public string? UnitTriggerKind { get; }
        public int? HealDamageMultiplier { get; }
        public UnitReturnRule? UnitReturn { get; }
        public EquipmentMergeRule? EquipmentMerge { get; }
        public bool? SuppressPyreRoomFocus { get; }
        public ElixirRecipe? SpikeRecipe { get; }
        public CardActionEffect(string type, string target, int value, bool allowEnemy, bool allowPlayer, IReadOnlyList<CombatStatus> statuses,
            CardUpgradeModifier? upgrade = null, string lifetime = "", CardEffectTests? tests = null, CardEffectRange? range = null, CardTargetFilters? filters = null,
            CardGenerationRule? generation = null, bool onlyIfNoEnemies = false, bool? cooldownParameter = null,
            AbilityChangeRule? abilityChange = null, bool? copyHeroStats = null, bool scaleUpgrade = false,
            bool? doNotFullyRemove = null, string targetSubtype = "", TypedDrawRule? typedDraw = null,
            NextDrawUpgradeRule? nextDrawUpgrade = null, int requiredGold = 0, bool? goldCostRequired = null,
            bool useCardTraits = false, bool requireEquipment = false, bool useSelectedRoom = false,
            bool countStatusOnlyOnTargets = false, CombatUnit? spawnFallbackUnit = null,
            bool disallowStatusStackModifiers = false, int? statusAdjustmentFilter = null,
            bool damagePerHpLastAttacker = false, bool damagePerHpIncludeArmor = false,
            RoomCapacityAttachmentState? roomAttachment = null, CardRecursionRule? recursion = null,
            CardUpgradeMaskRule? cardFilter = null, IReadOnlyList<CardUpgradeMaskRule>? upgradeFilters = null,
            bool? removeHalfStatusEffects = null, OverhealRule? overheal = null, bool? corruptionAdd = null,
            bool nullTargetless = false, bool nullForwardSelf = false, CardChooseDiscardRule? chooseDiscard = null,
            int transferAttackMultiplier = 0, StatusTransferRule? statusTransfer = null, string? unitTriggerKind = null,
            int? healDamageMultiplier = null, UnitReturnRule? unitReturn = null, EquipmentMergeRule? equipmentMerge = null,
            string? sourceStatusMultiplier = null, bool? suppressPyreRoomFocus = null, ElixirRecipe? spikeRecipe = null)
        { Type = type; Target = target; Value = value; AllowEnemy = allowEnemy; AllowPlayer = allowPlayer; Statuses = Array.AsReadOnly(statuses.ToArray());
            Upgrade = upgrade; Lifetime = lifetime; Tests = tests; Range = range; Filters = filters; Generation = generation; OnlyIfNoEnemies = onlyIfNoEnemies;
            CooldownParameter = cooldownParameter; AbilityChange = abilityChange; CopyHeroStats = copyHeroStats; ScaleUpgrade = scaleUpgrade;
            DoNotFullyRemove = doNotFullyRemove; TargetSubtype = targetSubtype;
            TypedDraw = typedDraw; NextDrawUpgrade = nextDrawUpgrade; RequiredGold = requiredGold; GoldCostRequired = goldCostRequired;
            UseCardTraits = useCardTraits; RequireEquipment = requireEquipment; UseSelectedRoom = useSelectedRoom;
            CountStatusOnlyOnTargets = countStatusOnlyOnTargets; SpawnFallbackUnit = spawnFallbackUnit;
            DisallowStatusStackModifiers = disallowStatusStackModifiers; StatusAdjustmentFilter = statusAdjustmentFilter;
            SourceStatusMultiplier = sourceStatusMultiplier;
            RemoveHalfStatusEffects = removeHalfStatusEffects;
            DamagePerHpLastAttacker = damagePerHpLastAttacker; DamagePerHpIncludeArmor = damagePerHpIncludeArmor;
            RoomAttachment = roomAttachment; Recursion = recursion; CardFilter = cardFilter;
            UpgradeFilters = Array.AsReadOnly((upgradeFilters ?? Array.Empty<CardUpgradeMaskRule>()).ToArray());
            Overheal = overheal; CorruptionAdd = corruptionAdd;
            NullTargetless = nullTargetless; NullForwardSelf = nullForwardSelf; ChooseDiscard = chooseDiscard;
            TransferAttackMultiplier = transferAttackMultiplier; StatusTransfer = statusTransfer; UnitTriggerKind = unitTriggerKind;
            HealDamageMultiplier = healDamageMultiplier; UnitReturn = unitReturn; EquipmentMerge = equipmentMerge;
            SuppressPyreRoomFocus = suppressPyreRoomFocus; SpikeRecipe = spikeRecipe; }
    }

    public sealed class StatusTransferRule
    {
        public string TargetMode { get; }
        public string AllowedStatusTypes { get; }
        public string RoomScope { get; }
        public bool AllowStackModification { get; }
        public bool SkipRemove { get; }
        public float StackPercentage { get; }
        public StatusTransferRule(string targetMode, string allowedStatusTypes, string roomScope,
            bool allowStackModification, bool skipRemove, float stackPercentage)
        {
            TargetMode = targetMode; AllowedStatusTypes = allowedStatusTypes; RoomScope = roomScope;
            AllowStackModification = allowStackModification; SkipRemove = skipRemove; StackPercentage = stackPercentage;
        }
    }

    public sealed class OverhealRule
    {
        public int AttackPerPoint { get; }
        public int MaxHealthPerPoint { get; }
        public OverhealRule(int attackPerPoint, int maxHealthPerPoint)
        { AttackPerPoint = attackPerPoint; MaxHealthPerPoint = maxHealthPerPoint; }
    }

    public sealed class CardRecursionRule
    {
        public string SelectionMode { get; }
        public string CardType { get; }
        public string Subtype { get; }
        public bool RequiresRoomInHand { get; }
        public bool DisallowOverflowToDrawPile { get; }
        public bool HasCardFilter { get; }
        public CardUpgradeMaskRule? CardFilter { get; }
        public bool HasUpgrade { get; }
        public CardRecursionRule(string selectionMode, string cardType, string subtype,
            bool requiresRoomInHand, bool disallowOverflowToDrawPile, bool hasCardFilter, bool hasUpgrade,
            CardUpgradeMaskRule? cardFilter = null)
        {
            SelectionMode = selectionMode; CardType = cardType; Subtype = subtype;
            RequiresRoomInHand = requiresRoomInHand; DisallowOverflowToDrawPile = disallowOverflowToDrawPile;
            HasCardFilter = hasCardFilter; CardFilter = cardFilter; HasUpgrade = hasUpgrade;
        }
    }
    public sealed class RoomPlayRule
    {
        public int RoomIndex { get; }
        public int PlayerCapacity { get; }
        public int PlayerSlots { get; }
        public bool Enabled { get; }
        public bool SummonBlocked { get; }
        public bool IsPyre { get; }
        public int EnemyCapacity { get; }
        public RoomPlayRule(int roomIndex, int playerCapacity, int playerSlots, bool enabled, bool summonBlocked, bool isPyre, int enemyCapacity = 0)
        { RoomIndex = roomIndex; PlayerCapacity = playerCapacity; PlayerSlots = playerSlots; Enabled = enabled; SummonBlocked = summonBlocked; IsPyre = isPyre;
            EnemyCapacity = enemyCapacity; }
        internal RoomPlayRule WithEnabled(bool enabled) => new(RoomIndex, PlayerCapacity, PlayerSlots, enabled,
            SummonBlocked, IsPyre, EnemyCapacity);
        internal RoomPlayRule WithCapacities(int playerCapacity, int enemyCapacity) => new(RoomIndex, playerCapacity,
            PlayerSlots, Enabled, SummonBlocked, IsPyre, enemyCapacity);
    }

    // Rules copied from definitions at the root, including reachable generated card definitions.
    // Unimplemented traits/effects remain attached to the card; they never silently become no-ops.
    public sealed class CardAbilityRule
    {
        public string Kind { get; }
        public bool CanTargetOtherFloors { get; }
        public bool CanPlayWhenHandFull { get; }
        public CardAbilityRule(string kind, bool canTargetOtherFloors, bool canPlayWhenHandFull)
        { Kind = kind; CanTargetOtherFloors = canTargetOtherFloors; CanPlayWhenHandFull = canPlayWhenHandFull; }
    }

    public sealed class CardPlayRule
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public int Cost { get; }
        public string CostType { get; }
        public EquipmentDefinition? Equipment { get; }
        public CardAbilityRule? Ability { get; }
        public string? CardType { get; }
        public bool? IsAnyAbility { get; }
        public bool? CanPlayInEngineRoom { get; }
        public string Effect { get; }
        public string Destination { get; }
        public CombatUnit? SpawnUnit { get; }
        public UnitSummonRule? Summon { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public IReadOnlyList<CardActionEffect> Effects { get; }
        public IReadOnlyList<string>? UpgradeInteractions { get; }
        public IReadOnlyList<string>? HandDiscardInteractions { get; }
        public IReadOnlyList<string>? HandConsumeInteractions { get; }
        public IReadOnlyList<CardActionEffect> OnCastEffects { get; }
        public IReadOnlyList<CardActionEffect>? OnForgeEffects { get; }
        public IReadOnlyList<CardActionEffect> OnUnplayedNegativeEffects { get; }
        public int? UnplayedNegativeOrder { get; }
        public bool HasUnsupportedUnplayedTriggers { get; }
        public bool? ForgeEligible { get; }
        public bool HasKillTriggers { get; }
        // False is the legacy schema default; retain exact rule identity for
        // existing captures that predate card-owned kill-trigger metadata.
        public bool ShouldSerializeHasKillTriggers() => HasKillTriggers;
        public CardPlayRule(string dataId, string assetKey, int cost, string effect, string destination,
            CombatUnit? spawnUnit, IReadOnlyList<string> externalInteractions, IReadOnlyList<CardActionEffect>? effects = null,
            IReadOnlyList<string>? upgradeInteractions = null, IReadOnlyList<string>? handDiscardInteractions = null,
            IReadOnlyList<string>? handConsumeInteractions = null, string costType = "Default", EquipmentDefinition? equipment = null,
            CardAbilityRule? ability = null, UnitSummonRule? summon = null, string? cardType = null, bool? isAnyAbility = null,
            bool? canPlayInEngineRoom = null, IReadOnlyList<CardActionEffect>? onCastEffects = null,
            IReadOnlyList<CardActionEffect>? onUnplayedNegativeEffects = null, int? unplayedNegativeOrder = null,
            bool hasUnsupportedUnplayedTriggers = false, bool? forgeEligible = null, bool hasKillTriggers = false,
            IReadOnlyList<CardActionEffect>? onForgeEffects = null)
        {
            DataId = dataId; AssetKey = assetKey; Cost = cost; CostType = costType; Effect = effect; Destination = destination;
            Equipment = equipment; Ability = ability; CardType = cardType; IsAnyAbility = isAnyAbility; CanPlayInEngineRoom = canPlayInEngineRoom;
            SpawnUnit = spawnUnit; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            Summon = summon;
            Effects = Array.AsReadOnly((effects ?? Array.Empty<CardActionEffect>()).ToArray());
            OnCastEffects = Array.AsReadOnly((onCastEffects ?? Array.Empty<CardActionEffect>()).ToArray());
            OnForgeEffects = onForgeEffects == null ? null : Array.AsReadOnly(onForgeEffects.ToArray());
            OnUnplayedNegativeEffects = Array.AsReadOnly((onUnplayedNegativeEffects ?? Array.Empty<CardActionEffect>()).ToArray());
            UnplayedNegativeOrder = unplayedNegativeOrder;
            HasUnsupportedUnplayedTriggers = hasUnsupportedUnplayedTriggers;
            ForgeEligible = forgeEligible;
            HasKillTriggers = hasKillTriggers;
            UpgradeInteractions = upgradeInteractions == null ? null : Array.AsReadOnly(upgradeInteractions.ToArray());
            HandDiscardInteractions = handDiscardInteractions == null ? null : Array.AsReadOnly(handDiscardInteractions.ToArray());
            HandConsumeInteractions = handConsumeInteractions == null ? null : Array.AsReadOnly(handConsumeInteractions.ToArray());
        }
        internal CardPlayRule WithSpawn(CombatUnit unit) => new CardPlayRule(DataId, AssetKey, Cost, Effect, Destination,
            unit, ExternalInteractions, Effects, UpgradeInteractions, HandDiscardInteractions, HandConsumeInteractions, CostType, Equipment, Ability, Summon,
            CardType, IsAnyAbility, CanPlayInEngineRoom, OnCastEffects, OnUnplayedNegativeEffects, UnplayedNegativeOrder,
            HasUnsupportedUnplayedTriggers, ForgeEligible, HasKillTriggers, OnForgeEffects);
        internal CardPlayRule WithForgeEligibility(bool? eligible) => new CardPlayRule(DataId, AssetKey, Cost, Effect, Destination,
            SpawnUnit, ExternalInteractions, Effects, UpgradeInteractions, HandDiscardInteractions, HandConsumeInteractions, CostType, Equipment, Ability, Summon,
            CardType, IsAnyAbility, CanPlayInEngineRoom, OnCastEffects, OnUnplayedNegativeEffects, UnplayedNegativeOrder,
            HasUnsupportedUnplayedTriggers, eligible, HasKillTriggers, OnForgeEffects);
    }

    public sealed class ForgeUpgradeModifier
    {
        public string RelicDataId { get; }
        public string RelicAssetKey { get; }
        public int EffectIndex { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public ForgeUpgradeModifier(string relicDataId, string relicAssetKey, int effectIndex,
            CardUpgradeModifier upgrade, IReadOnlyList<CardUpgradeMaskRule> filters)
        { RelicDataId = relicDataId; RelicAssetKey = relicAssetKey; EffectIndex = effectIndex; Upgrade = upgrade;
            Filters = Array.AsReadOnly(filters.ToArray()); }
    }

    public sealed class ForgeUpgradeDefinition
    {
        public CardUpgradeModifier? DefaultUpgrade { get; }
        public bool HasRoomOverride { get; }
        public IReadOnlyList<ForgeUpgradeModifier> RelicUpgrades { get; }
        public ForgeUpgradeDefinition(CardUpgradeModifier? defaultUpgrade, bool hasRoomOverride,
            IReadOnlyList<ForgeUpgradeModifier> relicUpgrades)
        { DefaultUpgrade = defaultUpgrade; HasRoomOverride = hasRoomOverride;
            RelicUpgrades = Array.AsReadOnly(relicUpgrades.ToArray()); }
    }

    public sealed class BattlePlayRules
    {
        public IReadOnlyList<RoomPlayRule> Rooms { get; }
        public IReadOnlyList<CardPlayRule> Cards { get; }
        public IReadOnlyList<CombatStatus> StatusRules { get; }
        public UnitCopyCatalog? UnitCopyCatalog { get; }
        public ElixirCatalog? Elixirs { get; }
        public ForgeUpgradeDefinition? ForgeUpgrade { get; }
        public BattlePlayRules(IReadOnlyList<RoomPlayRule> rooms, IReadOnlyList<CardPlayRule> cards, IReadOnlyList<CombatStatus>? statusRules = null,
            UnitCopyCatalog? unitCopyCatalog = null, ElixirCatalog? elixirs = null, ForgeUpgradeDefinition? forgeUpgrade = null)
        { Rooms = Array.AsReadOnly(rooms.ToArray()); Cards = Array.AsReadOnly(cards.ToArray()); StatusRules = Array.AsReadOnly((statusRules ?? Array.Empty<CombatStatus>()).ToArray()); UnitCopyCatalog = unitCopyCatalog; Elixirs = elixirs; ForgeUpgrade = forgeUpgrade; }
        internal BattlePlayRules WithRooms(IReadOnlyList<RoomPlayRule> rooms) => new(rooms, Cards, StatusRules, UnitCopyCatalog, Elixirs, ForgeUpgrade);
        internal BattlePlayRules WithForgeEligibility(bool? eligible) => new(Rooms,
            Cards.Select(card => card.WithForgeEligibility(eligible)).ToArray(), StatusRules, UnitCopyCatalog, Elixirs, ForgeUpgrade);
        internal BattlePlayRules WithCapacities(IReadOnlyList<RoomCapacityState> capacities) => WithRooms(Rooms.Select(room =>
        {
            RoomCapacityState? capacity = capacities.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
            return capacity == null ? room : room.WithCapacities(capacity.PlayerMaximum, capacity.EnemyMaximum);
        }).ToArray());
    }

    public sealed class PlayCardAction
    {
        public int CardInstanceId { get; }
        public int RoomIndex { get; }
        // -1 chooses the native first empty slot; 0..count inserts at the selected player position.
        public int PlayerPosition { get; }
        public int TargetUnitId { get; }
        public int ActivatorUnitId { get; }
        public int? ActivatorRoomIndex { get; }
        // Spawn-point index selected by effects that move an existing unit to a room location.
        public int TargetSpawnPointIndex { get; }
        public IReadOnlyList<CardRecursionSelection> RecursionSelections { get; }
        public IReadOnlyList<ElixirMixSelection> MixSelections { get; }
        public PlayCardAction(int cardInstanceId, int roomIndex, int playerPosition = -1, int targetUnitId = 0,
            int activatorUnitId = 0, int targetSpawnPointIndex = -1,
            IReadOnlyList<CardRecursionSelection>? recursionSelections = null,
            IReadOnlyList<ElixirMixSelection>? mixSelections = null, int? activatorRoomIndex = null)
        { CardInstanceId = cardInstanceId; RoomIndex = roomIndex; PlayerPosition = playerPosition; TargetUnitId = targetUnitId;
            ActivatorUnitId = activatorUnitId; TargetSpawnPointIndex = targetSpawnPointIndex;
            ActivatorRoomIndex = activatorRoomIndex;
            RecursionSelections = Array.AsReadOnly((recursionSelections ?? Array.Empty<CardRecursionSelection>()).ToArray());
            MixSelections = Array.AsReadOnly((mixSelections ?? Array.Empty<ElixirMixSelection>()).ToArray()); }
    }

    public enum ActionRejection { None, Illegal, Unsupported }
    public sealed class BattleActionResult
    {
        public BattleTurnState? State { get; }
        public ActionRejection Rejection { get; }
        public string? Reason { get; }
        public RoomOutcome Outcome { get; }
        public bool Supported => State != null;
        internal BattleActionResult(BattleTurnState? state, ActionRejection rejection = ActionRejection.None, string? reason = null, RoomOutcome outcome = RoomOutcome.Exchanged)
        { State = state; Rejection = rejection; Reason = reason; Outcome = state == null ? RoomOutcome.Unsupported : outcome; }
    }
    public sealed class UnsupportedActionAttempt
    {
        public PlayCardAction? Action { get; }
        public string Reason { get; }
        internal UnsupportedActionAttempt(PlayCardAction? action, string reason) { Action = action; Reason = reason; }
    }
    public sealed class SupportedActionCandidate
    {
        public PlayCardAction Action { get; }
        public BattleActionResult Result { get; }
        internal SupportedActionCandidate(PlayCardAction action, BattleActionResult result) { Action = action; Result = result; }
    }
    // This reports model-generated candidates; an empty diagnostic list does not prove full game action-space coverage.
    public sealed class BattleActionEnumeration
    {
        public IReadOnlyList<SupportedActionCandidate> SupportedCandidates { get; }
        public IReadOnlyList<PlayCardAction> SupportedActions { get; }
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }
        public bool HasUnsupportedAttempts => UnsupportedAttempts.Count > 0;
        internal BattleActionEnumeration(IReadOnlyList<SupportedActionCandidate> supportedCandidates,
            IReadOnlyList<UnsupportedActionAttempt> unsupportedAttempts)
        {
            SupportedCandidates = Array.AsReadOnly(supportedCandidates.ToArray());
            SupportedActions = Array.AsReadOnly(supportedCandidates.Select(candidate => candidate.Action).ToArray());
            UnsupportedAttempts = Array.AsReadOnly(unsupportedAttempts.ToArray());
        }
    }

    public static class BattleActionModel
    {
        public static BattleActionResult PlayCard(BattleTurnState source, PlayCardAction action)
        {
            if (action.ActivatorUnitId != 0 || action.ActivatorRoomIndex.HasValue)
            {
                int currentRoom = source.Spawn.Train.Context?.QueryFrame?.SelectedRoomIndex ?? source.SelectedRoom;
                if (currentRoom >= 0 && currentRoom != action.RoomIndex &&
                    source.PlayRules?.Rooms.Any(room => room.RoomIndex == action.RoomIndex) == true)
                {
                    // Native ability activation focuses its room, then refreshes
                    // combat preview after the play. It does not run a second
                    // preview merely for this preceding focus change.
                    BattleTurnResult selection = BattleTurnModel.SelectRoom(source, action.RoomIndex, refreshPreview: false);
                    if (!selection.Supported) return Unsupported(selection.UnsupportedReason!);
                    source = selection.State!;
                }
                return UnitAbilityModel.Activate(source, action);
            }
            if (action.TargetSpawnPointIndex != -1)
                return Illegal("This card play does not take a destination spawn point.");
            if (source.PlayRules == null || source.Spawn.Train.Context == null)
                return Unsupported("Missing card/room play definitions or battle context.");
            IReadOnlyList<string> actionInteractions = BattleTurnModel.ActionInteractions(source);
            if (actionInteractions.Count > 0 || source.Spawn.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", actionInteractions.Concat(source.Spawn.ExternalInteractions)));
            if (source.PlayRules.Cards.Select(card => card.DataId).Distinct().Count() != source.PlayRules.Cards.Count ||
                source.PlayRules.Rooms.Select(room => room.RoomIndex).Distinct().Count() != source.PlayRules.Rooms.Count)
                return Unsupported("Duplicate play definitions.");
            TrainCombatState train = source.Spawn.Train;
            CombatTeam? originalTargetTeam = train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == action.TargetUnitId)?.Team;
            BattlePlayRules outputPlayRules = source.PlayRules;
            string? trainValidation = TrainCombatModel.Validate(train);
            if (trainValidation != null) return Unsupported(trainValidation);
            if (source.Spawn.NextUnitId <= 0 || train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id <= 0 || unit.Id >= source.Spawn.NextUnitId || unit.Size < 0))
                return Unsupported("Invalid unit identity allocation or size.");
            CombatContext context = train.Context;
            string? frameError = StatisticQueryFrame.ValidateDecision(source);
            if (frameError != null) return Unsupported(frameError);
            if (context.QueryFrame?.SelectedRoomIndex != null)
                context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: action.RoomIndex));
            if (source.MoonPhaseTrackingActive.HasValue && context.QueryFrame != null)
                context = context.WithQueryFrame(context.QueryFrame.With(
                    moonPhaseTrackingActive: source.MoonPhaseTrackingActive));
            train = CardSpellModel.WithContext(train, context);
            context = CardMoonUpgradeModel.RefreshHand(context, out string? moonHandError);
            if (moonHandError != null) return Unsupported(moonHandError);
            train = CardSpellModel.WithContext(train, context);
            CardToken? card = context.Cards.Hand.FirstOrDefault(item => item.InstanceId == action.CardInstanceId);
            if (card == null) return Illegal("The selected card instance is not in hand.");
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            CardPlayRule? originalRule = rule;
            string? uiRangeError = CardSpellModel.UnisolatedUiRangeReason(source);
            if (uiRangeError != null) return Unsupported(uiRangeError);
            if (rule == null) return Unsupported("Missing play definition for " + card.DataId);
            CardInstanceState? playingInstance = context.CardInstances?.FirstOrDefault(item => item.InstanceId == card.InstanceId);
            // Native Unplayable rejects the attempt before card effects or Forge upgrades run.
            if (playingInstance?.MaskDescriptor != null && CardExhaustModel.RefreshTraits(playingInstance)
                .CombinedTraits?.Any(trait => trait.RuntimeType == "CardTraitUnplayable") == true)
                return Illegal("The card has an Unplayable trait.");
            if (source.ForgePoints > 0)
            {
                ForgeUpgradeModelResult forged = ForgeUpgradeModel.BeforeDirectCardPlay(context, source.PlayRules,
                    rule, playingInstance, source.ForgePoints);
                if (!forged.Supported) return Unsupported(forged.UnsupportedReason!);
                context = forged.Context!;
                playingInstance = context.FindCard(card.InstanceId);
                train = CardSpellModel.WithContext(train, context);
            }
            if (rule.SpawnUnit != null)
            {
                CombatUnit initialized = AbilityLifecycleModel.InitialAtSpawn(rule.SpawnUnit, playingInstance, context, out string? spawnError);
                if (spawnError != null) return Unsupported(spawnError);
                rule = rule.WithSpawn(initialized);
                originalRule = rule;
            }
            if (context.CardInstances != null)
            {
                if (playingInstance == null) return Unsupported("Missing card instance modifiers.");
                rule = CardModifierModel.Resolve(rule, playingInstance);
            }
            if (playingInstance != null && CardPlayRangeModel.HasLimitedRange(playingInstance))
            {
                RoomPlayRule[] pyreRooms = source.PlayRules.Rooms.Where(room => room.IsPyre).ToArray();
                if (pyreRooms.Length != 1) return Unsupported("Limited Range requires one captured Pyre room.");
                if (!CardPlayRangeModel.Allows(action.RoomIndex, pyreRooms[0].RoomIndex))
                    return Illegal("Limited Range only permits the Pyre room and the room immediately below it.");
            }
            bool supportsCorruptRestrictedTrait = false;
            if (rule.ExternalInteractions.Contains("Card traits") && playingInstance?.MaskDescriptor != null)
            {
                CardUpgradeMaskCard mask;
                try { mask = CardBranchMaskModel.Resolve(playingInstance); }
                catch (InvalidOperationException exception)
                { return Unsupported("Cannot resolve card play traits: " + exception.Message); }
                if (mask.Traits.Contains("CardTraitUnplayable"))
                    return Illegal("The card has an Unplayable trait.");
                string? traitError = RelicCorruptRestrictedCardPlayModel.ValidateCardTraits(playingInstance,
                    out supportsCorruptRestrictedTrait);
                if (traitError != null) return Unsupported(traitError);
            }
            string[] unsupportedInteractions = rule.ExternalInteractions.Where(interaction =>
                interaction != "Card traits" || !supportsCorruptRestrictedTrait).ToArray();
            if (unsupportedInteractions.Length > 0) return Unsupported(string.Join("; ", unsupportedInteractions));
            if (rule.CostType != "Default" && rule.CostType != "ConsumeRemainingEnergy")
                return Unsupported("Unmodeled card cost type " + rule.CostType);
            if (rule.Cost < 0) return Unsupported("Negative card costs are not implemented.");
            int effectiveCost = rule.Cost;
            if (playingInstance != null && CardHandCostModel.HasModifiers(context))
            {
                try { effectiveCost = CardHandCostModel.Resolve(context, playingInstance, originalRule.Cost, rule.CardType); }
                catch (InvalidOperationException error) { return Unsupported(error.Message); }
            }
            int healingCostDelta = UnitRoomResourceModel.HealingCostDelta(train, action.RoomIndex, rule, playingInstance);
            if (rule.CardType == "Spell" || healingCostDelta != 0)
                effectiveCost = (int)Math.Clamp((long)effectiveCost + healingCostDelta +
                    (rule.CardType == "Spell" ? RoomCapacityModel.SpellCostDelta(context, action.RoomIndex) : 0), 0L, 99L);
            int paidCost = rule.CostType == "ConsumeRemainingEnergy" ? source.Energy : effectiveCost;
            if (source.Energy < 0) return Unsupported("Negative decision energy.");
            RoomPlayRule? targetRule = source.PlayRules.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            RoomCombatState? target = train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            if (targetRule == null || target == null) return Illegal("The target room does not exist.");
            if (!targetRule.Enabled || targetRule.IsPyre && (rule.CanPlayInEngineRoom != true || rule.Effect == "SpawnMonster"))
                return Illegal("This card cannot be played in the target room.");
            RelicPyreCostPlan pyreCost = RelicPayCardCostWithPyreHealthModel.Plan(train, effectiveCost,
                source.Energy, rule.CostType, source.Spawn.Turn,
                context.IsolatedBattlePreview == true || target.Preview);
            if (!pyreCost.Supported) return Unsupported(pyreCost.UnsupportedReason!);
            if (!pyreCost.CanPay) return Illegal("Insufficient energy.");
            if (playingInstance != null)
            {
                string? corruptionError = CardCorruptRestrictedModel.Test(context, playingInstance, action.RoomIndex, out bool corruptionAllowed);
                if (corruptionError != null) return Unsupported(corruptionError);
                if (!corruptionAllowed) return Illegal("Insufficient corruption for Corrupt Restricted.");
                CardCorruptRestrictedModel.Requirement(playingInstance, out int requiredCorruption);
                if (requiredCorruption > 0 && (rule.Effect != "Spell" || rule.IsAnyAbility != false))
                    return Unsupported("Positive Corrupt Restricted is calibrated only for direct spell plays.");
            }
            bool paidWithPyre = pyreCost.PaidWithPyre;
            int energyPaidCost = paidWithPyre ? 0 : paidCost;
            int? nativePlayedCost = paidWithPyre ? null : paidCost;
            train = pyreCost.State;
            context = train.Context!;
            target = train.Rooms.First(room => room.RoomIndex == action.RoomIndex);
            CardActionEffect[] actionEffects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            if (CardSpellModel.RequiresUnitTarget(actionEffects))
            {
                CombatUnit? victim = target.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
                if (victim == null || victim.IsPyre || victim.Statuses.Any(status => status.Id == "untouchable"))
                    return Illegal("The card has no legal unit target in the selected room.");
                CardActionEffect[] targeted = actionEffects.Where(effect => effect.Target == "DropTargetCharacter" && effect.Type != "HandUpgrade").ToArray();
                // Duality's damage and healing select opposite teams. Native
                // combines their teams, then tests/applies each effect separately.
                if (targeted.Length > 0 && !targeted.Any(effect => victim.Team == CombatTeam.Enemy ? effect.AllowEnemy : effect.AllowPlayer))
                    return Illegal("The card excludes the target team.");
            }
            else if (action.TargetUnitId != 0) return Illegal("This card does not take a unit target.");
            string? membershipError = CardPileModel.ValidateMembership(context.Cards, source.OtherPiles, context.NextCardId, context);
            if (membershipError != null) return Unsupported(membershipError);
            if (source.OtherPiles.Select(pile => pile.Name).Distinct().Count() != source.OtherPiles.Count)
                return Unsupported("Duplicate card piles.");
            foreach (CardPileState pile in source.OtherPiles)
            {
                string? pileError = CardPileModel.Validate(pile);
                if (pileError != null) return Unsupported(pileError);
            }
            context = context.WithOtherPiles(source.OtherPiles);
            if (!paidWithPyre) context = context.WithStatistics(context.Statistics?.WithPlayedCost(card.InstanceId, paidCost));
            CombatContext castingContext = context;
            context = context.WithAbilityActivator(context.LastAbilityActivatorUnitId.HasValue ? 0 : (int?)null);
            context = context.WithQueryFrame(context.QueryFrame?.With(energy: source.Energy - energyPaidCost));
            // Native records the payment on the card before any queued effect runs.
            if (playingInstance != null && !paidWithPyre) context = context.WithCard(playingInstance.WithPlayedCost(paidCost));
            var suppressionTest = RelicSuppressExhaustModel.PreDiscard(context, card.InstanceId, true, out _, out string? consumeSuppression);
            if (!suppressionTest.Supported) return Unsupported(suppressionTest.UnsupportedReason!);
            context = suppressionTest.Context;
            CardPileState[] piles = source.OtherPiles.ToArray();
            // A naturally played card remains owned in the discard buffer during queued effects.
            // Scaling queries refresh membership here, before the final destination is assigned.
            piles = piles.Select(pile => pile.Name == "DiscardBuffer" && !pile.Cards.Any(item => item.InstanceId == card.InstanceId)
                ? CardPileModel.Add(pile, card) : pile).ToArray();
            context = context.WithOtherPiles(piles);
            // Native direct play removes the card from hand before queued effects execute.
            context = new CombatContext(new CardCycleState(context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToArray(),
                context.Cards.Draw, context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn),
                context.BattleRng, context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics, context.CardInstances,
                context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState, context.ConduitRetained, context.LifeLinks);
            target = new RoomCombatState(target.RoomIndex, target.Deployment, target.Units, target.ExternalInteractions,
                context, target.Preview, target.ActiveCompanionBossIds);
            if (context.FindCard(card.InstanceId)?.PlayedRoomUnitIds != null)
            {
                context = context.WithCard(context.FindCard(card.InstanceId)!.WithPlayedRoomUnits(target.Units
                    .Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).ToArray()));
                target = new RoomCombatState(target.RoomIndex, target.Deployment, target.Units, target.ExternalInteractions,
                    context, target.Preview, target.ActiveCompanionBossIds);
            }
            CombatUnit[] players = target.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            int position = action.PlayerPosition == -1 ? players.Length : action.PlayerPosition;
            int? spawnedId = null;
            bool eatenDuringBirth = false;
            bool effectsApplied = false;
            IReadOnlyList<NextDrawUpgradeRule>? discardDrawUpgrades = null;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pendingSpellCallbacks = Array.Empty<RoomCombatModel.QueuedCharacterTrigger>();
            IReadOnlyList<CombatUnit> retainedSpellUnits = Array.Empty<CombatUnit>();
            IReadOnlyList<int>? lastMainTargets = null;
            var lastDamagedUnitIds = new HashSet<int>();
            int nextUnitId = source.Spawn.NextUnitId;
            string? identityError = UnitIdentityModel.Validate(context, train.Rooms.SelectMany(room => room.Units), nextUnitId);
            if (identityError != null) return Unsupported(identityError);
            string? RunBeforePlayCallbacks()
            {
                if (pyreCost.Callbacks.Count > 0)
                {
                    train = CardSpellModel.WithContext(train, context);
                    TrainCombatResult drained = RelicPayCardCostWithPyreHealthModel.DrainCallbacks(train, pyreCost);
                    if (!drained.Supported) return drained.UnsupportedReason;
                    train = drained.State!; context = train.Context!;
                    if (drained.Outcome != RoomOutcome.Exchanged) outcome = drained.Outcome;
                }
                TrainSpellResult corrupt = CardCorruptStateModel.BeforePlay(CardSpellModel.WithContext(train, context),
                    context.FindCard(card.InstanceId), rule, action.RoomIndex, outputPlayRules);
                if (!corrupt.Supported) return corrupt.UnsupportedReason;
                train = corrupt.State!; context = train.Context!;
                piles = corrupt.OtherPiles?.ToArray() ?? piles;
                outputPlayRules = corrupt.PlayRules ?? outputPlayRules;
                nextUnitId = context.NextUnitId ?? nextUnitId;
                retainedSpellUnits = retainedSpellUnits.Concat(corrupt.RetainedUnits)
                    .GroupBy(unit => unit.Id).Select(group => group.Last()).ToArray();
                if (corrupt.Outcome != RoomOutcome.Exchanged) outcome = corrupt.Outcome;
                target = train.Rooms.First(room => room.RoomIndex == action.RoomIndex);
                players = target.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
                position = action.PlayerPosition == -1 ? players.Length : action.PlayerPosition;
                return null;
            }
            if (rule.Effect == "SpawnMonster")
            {
                string? callbackError = RunBeforePlayCallbacks();
                if (callbackError != null) return Unsupported(callbackError);
                if (action.TargetUnitId != 0) return Illegal("A summon takes a spawn position, not a unit target.");
                CombatUnit? template = rule.SpawnUnit;
                if (template == null && !(rule.Summon?.Pool.Count > 0 && rule.Summon.NativeBaseSize.HasValue) ||
                template?.IsPyre == true || template?.Size < 0 ||
                rule.Destination != "Standby" && !(rule.CardType == "Spell" && rule.Destination == "Discard"))
                    return Unsupported("Invalid unit spawn definition.");
                IReadOnlyList<CombatStatus> initialStatuses = Array.Empty<CombatStatus>();
                if (template != null)
                {
                    CombatUnit startingTemplate = originalRule?.SpawnUnit ?? template;
                    initialStatuses = startingTemplate.StatusRegistry ?? startingTemplate.Statuses;
                    foreach (CardModifiers group in playingInstance == null ? Array.Empty<CardModifiers>() : new[] { playingInstance.Permanent, playingInstance.Temporary })
                        initialStatuses = StatusCallbackModel.MergeStartingStatuses(initialStatuses, group.Upgrades.SelectMany(upgrade => upgrade.Statuses));
                    template = StatusAttackDamageModel.PrepareBirthTemplate(template, initialStatuses);
                }
                string? validation = template == null ? null : RoomCombatModel.Validate(new RoomCombatState(target.RoomIndex, target.Deployment,
                    new[] { template }, Array.Empty<string>(), context, target.Preview, target.ActiveCompanionBossIds));
                if (validation != null) return Unsupported(validation);
                int legalSize = template?.Size ?? 0;
                if (rule.Summon?.NativeBaseSize is int nativeSize)
                {
                    if (playingInstance == null) return Unsupported("Pooled summon legality requires card modifiers.");
                    legalSize = Math.Max(1, Math.Min(6, CardModifierModel.UpgradedStat(nativeSize, "Size", false,
                        playingInstance.Permanent, playingInstance.Temporary)));
                }
                if (targetRule.SummonBlocked || players.Length >= targetRule.PlayerSlots ||
                    players.Sum(unit => (long)unit.Size) + legalSize >
                        (RoomCapacityModel.Maximum(context, action.RoomIndex, CombatTeam.Player) ?? targetRule.PlayerCapacity))
                    return Illegal("The room cannot accept this unit's size or another spawn slot.");
                if (context.SpawnPoints != null && context.SpawnPoints.Group(action.RoomIndex, CombatTeam.Player) == null)
                    return Unsupported("Missing physical summon group.");
                if (position < 0 || (context.SpawnPoints == null ? position > players.Length :
                    position >= context.SpawnPoints.Group(action.RoomIndex, CombatTeam.Player)!.GroupCount))
                    return Illegal("Invalid summon position.");
                if (nextUnitId <= 0 || train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id >= nextUnitId))
                    return Unsupported("Invalid unit identity allocation.");
                RoomCombatResult spawnTriggers;
                if (rule.Summon != null)
                {
                    if (RoomSpawnCopyModel.Active(target.WithContext(context)))
                        return Unsupported("Room spawn copies of repeated/pool summons require their native birth-phase integration.");
                    UnitBirthResult summoned = UnitSummonModel.Apply(target, originalRule!, card.InstanceId, position, targetRule.PlayerSlots);
                    if (!summoned.Supported) return Unsupported(summoned.Result.UnsupportedReason!);
                    spawnedId = summoned.UnitId; spawnTriggers = summoned.Result;
                    nextUnitId = spawnTriggers.State!.Context!.NextUnitId!.Value;
                }
                else
                {
                    if (template == null) return Unsupported("Missing ordinary summon template.");
                    UnitIdentityAllocation allocated = UnitIdentityModel.Allocate(context, nextUnitId);
                    if (!allocated.Supported) return Unsupported(allocated.UnsupportedReason!);
                    nextUnitId = allocated.NextUnitId; context = allocated.Context!;
                    var spawned = new CombatUnit(allocated.UnitId, template.AssetKey, CombatTeam.Player, template.BaseAttack,
                        template.Health, template.MaxHealth, template.CanAttack, false, false, template.Statuses,
                        template.Triggers, card.InstanceId, template.Size, template.StatusImmunities, template.Subtypes, template.Modifiers, template.IsBoss, template.LastAttackerId, template.StatusRegistry, template.EquipmentCards, template.NextTriggerId, template.Ability, template.StatusDictionary, template.AbilityRules, template.HordeDefinition,
                        template.IsSpawning.HasValue ? true : (bool?)null, template.SacrificeCardId, template.DeathState, bumpRules: template.BumpRules, lastFeederId: template.LastFeederId, moonPhaseMask: template.MoonPhaseMask);
                    context = context.WithStatistics(context.Statistics?.Spawn(action.RoomIndex, template.Subtypes));
                    spawnedId = spawned.Id;
                    var nextPlayers = players.ToList();
                    if (context.SpawnPoints != null)
                    {
                        var scope = new RoomCombatState(target.RoomIndex, target.Deployment, target.Units, target.ExternalInteractions,
                            context, target.Preview, target.ActiveCompanionBossIds);
                        var physical = BattleSpawnPointModel.Birth(context.SpawnPoints, scope, spawned, position, shift: true);
                        if (!physical.Supported) return Unsupported(physical.Error!);
                        context = context.WithSpawnPoints(physical.State!);
                        nextPlayers.Add(spawned);
                        nextPlayers = BattleSpawnPointModel.Order(context.SpawnPoints, target.RoomIndex, nextPlayers).ToList();
                    }
                    else nextPlayers.Insert(position, spawned);
                    var entered = new RoomCombatState(target.RoomIndex, target.Deployment,
                        target.Units.Where(unit => unit.Team == CombatTeam.Enemy).Concat(nextPlayers).ToArray(),
                        target.ExternalInteractions, context, target.Preview, target.ActiveCompanionBossIds);
                    MoonPhaseTransitionResult AdvanceFromSpawn(RoomCombatState frame, CardActionEffect effect, int sourceCardId)
                    {
                        RoomCombatState[] phaseRooms = train.Rooms.Select(room => room.RoomIndex == frame.RoomIndex ? frame :
                            new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                                frame.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
                        var phaseTrain = new TrainCombatState(phaseRooms, train.Movement, train.EnemySlotsPerRoom, frame.Context);
                        MoonPhaseTransitionResult shifted = MoonPhaseTransitionModel.Apply(phaseTrain, effect, sourceCardId);
                        if (shifted.Supported) train = shifted.State!;
                        return shifted;
                    }
                    RoomCombatResult ApplySummonDamage(RoomCombatState frame, int addedUnitId,
                        IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> birthCallbacks)
                    {
                        RoomCombatState[] birthRooms = train.Rooms.Select(room => room.RoomIndex == frame.RoomIndex ? frame :
                            new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                                frame.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
                        var birthTrain = new TrainCombatState(birthRooms, train.Movement, train.EnemySlotsPerRoom, frame.Context);
                        RelicSummonDamageResult damaged = RelicSummonDamageModel.CharacterAdded(birthTrain, addedUnitId, birthCallbacks);
                        if (!damaged.Supported)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), damaged.UnsupportedReason);
                        train = damaged.State!; context = train.Context!;
                        return new RoomCombatResult(train.Rooms.Single(room => room.RoomIndex == frame.RoomIndex),
                            damaged.Outcome, 0, damaged.Events.ToList(), pendingCallbacks: damaged.PendingCallbacks,
                            dispatches: damaged.Dispatches);
                    }
                    RoomCombatResult ApplyCharacterRelics(RoomCombatState frame, int actorId, string kind)
                    {
                        RoomCombatState[] effectRooms = train.Rooms.Select(room => room.RoomIndex == frame.RoomIndex ? frame :
                            new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                                frame.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
                        TrainCombatResult applied = RelicUnitSpawnEffectModel.Apply(new TrainCombatState(effectRooms,
                            train.Movement, train.EnemySlotsPerRoom, frame.Context), actorId, kind, managed: true);
                        if (!applied.Supported)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), applied.UnsupportedReason);
                        train = applied.State!; context = train.Context!;
                        return new RoomCombatResult(train.Rooms.Single(room => room.RoomIndex == frame.RoomIndex), applied.Outcome, 0,
                            applied.RoomResults.SelectMany(result => result.Events).ToList());
                    }
                    spawnTriggers = RoomCombatModel.ApplySpawnTriggers(entered, spawned.Id, fromCard: true,
                        startingApplications: initialStatuses, enqueue: null, moonPhase: AdvanceFromSpawn, relicBirthEffect: ApplySummonDamage,
                        relicCharacterEffect: ApplyCharacterRelics);
                    if (!spawnTriggers.Supported) return Unsupported(spawnTriggers.UnsupportedReason!);
                    RoomCombatResult grafted = UnitGraftModel.Apply(spawnTriggers.State!, spawned.Id);
                    if (!grafted.Supported) return Unsupported(grafted.UnsupportedReason!);
                    spawnTriggers = new RoomCombatResult(grafted.State, grafted.Outcome, 0,
                        spawnTriggers.Events.Concat(grafted.Events).ToList());
                    if (RoomSpawnCopyModel.Active(spawnTriggers.State!))
                    {
                        var copyFrame = new TrainCombatState(train.Rooms.Select(room => room.RoomIndex == target.RoomIndex ? spawnTriggers.State! :
                            room.WithContext(spawnTriggers.State!.Context)).ToArray(), train.Movement, train.EnemySlotsPerRoom, spawnTriggers.State!.Context);
                        UnitCloneResult copiedBirth = RoomSpawnCopyModel.PaidBirth(copyFrame, spawned.Id, source.PlayRules.UnitCopyCatalog);
                        if (!copiedBirth.Supported) return Unsupported(copiedBirth.UnsupportedReason!);
                        train = copiedBirth.State!;
                        spawnTriggers = new RoomCombatResult(train.Rooms.Single(room => room.RoomIndex == target.RoomIndex),
                            copiedBirth.Outcome == RoomOutcome.Exchanged ? spawnTriggers.Outcome : copiedBirth.Outcome,
                            0, spawnTriggers.Events.Concat(copiedBirth.Events).ToList());
                    }
                    CombatUnit? foodBirth = spawnTriggers.State!.Units.FirstOrDefault(unit => unit.Id == spawned.Id);
                    if (foodBirth?.Health > 0 && foodBirth.DeathState?.IsDespawned != true && RoomCombatModel.IsFood(foodBirth))
                    {
                        foreach (int ownerId in spawnTriggers.State.Units.Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).ToArray())
                        {
                            CombatUnit? owner = spawnTriggers.State.Units.FirstOrDefault(unit => unit.Id == ownerId);
                            if (owner == null) continue;
                            var foodFrame = new TrainCombatState(train.Rooms.Select(room => room.RoomIndex == target.RoomIndex
                                ? spawnTriggers.State : room.WithContext(spawnTriggers.State.Context)).ToArray(), train.Movement,
                                train.EnemySlotsPerRoom, spawnTriggers.State.Context);
                            TrainCombatResult fed = TrainCombatModel.ApplyCharacterQueue(foodFrame, new List<RoomCombatModel.QueuedCharacterTrigger> {
                                new RoomCombatModel.QueuedCharacterTrigger(target.RoomIndex, owner, "OnFoodSpawn", lastSpawnedOverrideUnitId: spawned.Id) });
                            if (!fed.Supported) return Unsupported(fed.UnsupportedReason!);
                            eatenDuringBirth |= fed.RoomResults.SelectMany(result => result.Events).Any(item => item.Kind == "Eaten" && item.Target == spawned.Id);
                            train = fed.State!;
                            spawnTriggers = new RoomCombatResult(train.Rooms.Single(room => room.RoomIndex == target.RoomIndex), fed.Outcome,
                                0, spawnTriggers.Events.ToList());
                        }
                    }
                    // Ordinary paid births have the same player-wide enchant phase
                    // as repeated and cloned births, before the post-play Rally.
                    var enchantQueue = spawnTriggers.State!.Units.Where(unit => unit.Team == CombatTeam.Player)
                        .Select(unit => new RoomCombatModel.QueuedCharacterTrigger(target.RoomIndex, unit, "AfterSpawnEnchant")).ToList();
                    TrainCombatResult enchanted = TrainCombatModel.ApplyCharacterQueue(new TrainCombatState(train.Rooms.Select(room =>
                        room.RoomIndex == target.RoomIndex ? spawnTriggers.State : new RoomCombatState(room.RoomIndex, room.Deployment,
                            room.Units, room.ExternalInteractions, spawnTriggers.State.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(), train.Movement,
                        train.EnemySlotsPerRoom, spawnTriggers.State.Context), enchantQueue);
                    if (!enchanted.Supported) return Unsupported(enchanted.UnsupportedReason!);
                    train = enchanted.State!;
                    CombatUnit? completedBirth = train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == spawned.Id);
                    if (completedBirth != null) train = UnitCloneModel.ReplaceUnit(train, HordeStatusModel.WithSpawning(completedBirth, false));
                    spawnTriggers = new RoomCombatResult(train.Rooms.Single(room => room.RoomIndex == target.RoomIndex),
                        enchanted.Outcome == RoomOutcome.Exchanged ? spawnTriggers.Outcome : enchanted.Outcome, 0, spawnTriggers.Events.ToList());
                }
                context = spawnTriggers.State!.Context!; outcome = spawnTriggers.Outcome;
                piles = context.OtherPiles?.ToArray() ?? piles;
                RoomCombatState[] enteredRooms = train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.RoomIndex == target.RoomIndex ? spawnTriggers.State.Units : room.Units, room.ExternalInteractions, context,
                    room.Preview, room.ActiveCompanionBossIds)).ToArray();
                var enteredIds = new HashSet<int>(enteredRooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                train = new TrainCombatState(enteredRooms, train.Movement.Where(item => enteredIds.Contains(item.UnitId)).ToArray(), train.EnemySlotsPerRoom, context);
                // CardEffectSpawnMonster ends with HandleRoomUnitOrderPossiblyChanged.
                // This is distinct from each unit's OnSpawn update and the card completion update.
                TrainCombatResult spawnOrder = EnchantmentWorldModel.UpdateAll(train);
                if (!spawnOrder.Supported) return Unsupported(spawnOrder.UnsupportedReason!);
                train = spawnOrder.State!; context = train.Context!;
                if (rule.Effects.Count > 0)
                {
                    if (rule.Effects.Any(effect => effect.Type != "ReplayTrigger") || !spawnedId.HasValue)
                        return Unsupported("Uncalibrated post-spawn effect sequence.");
                    TrainSpellResult replay = CardSpellModel.ApplyAfterSpawn(train, action.RoomIndex, rule.Effects,
                        card.InstanceId, spawnedId.Value, outputPlayRules);
                    if (!replay.Supported) return Unsupported(replay.UnsupportedReason!);
                    train = replay.State!; context = train.Context!; piles = replay.OtherPiles?.ToArray() ?? piles;
                    outcome = replay.Outcome; pendingSpellCallbacks = replay.PendingCallbacks; retainedSpellUnits = replay.RetainedUnits;
                }
                effectsApplied = true;
            }
            else if (rule.Effect == "Spell" || rule.Effect == "Equipment")
            {
                if (action.PlayerPosition != -1) return Illegal("A spell does not take a summon position.");
                // Keep the pre-play ownership snapshot for cast tests, but not
                // its RNG from before pre-discard relics made their random test.
                SpellCastCheck cast = CardSpellModel.TestPlay(CardSpellModel.WithContext(train,
                    castingContext.WithBattleRng(context.BattleRng)), action.RoomIndex,
                    rule.Effects, action.TargetUnitId, source.PlayRules, sourceCardId: card.InstanceId);
                if (!cast.Supported) return Unsupported(cast.UnsupportedReason!);
                if (!cast.CanPlay) return Illegal("Every effect failed its cast test or a required effect failed.");
                context = context.WithBattleRng(cast.BattleRngAfterTests!.Value);
                train = CardSpellModel.WithContext(train, context);
                string? callbackError = RunBeforePlayCallbacks();
                if (callbackError != null) return Unsupported(callbackError);
                TrainCombatState spellInput = CardSpellModel.WithContext(train, context);
                if (playingInstance != null)
                {
                    RoomCorruptionResult consumed = CardCorruptRestrictedModel.BeforeEffects(spellInput, playingInstance, action.RoomIndex);
                    if (!consumed.Supported) return Unsupported(consumed.UnsupportedReason!);
                    spellInput = CardSpellModel.WithContext(spellInput, consumed.Context!);
                }
                TrainSpellResult result = CardSpellModel.ApplyForCardPlay(spellInput, action.RoomIndex, rule.Effects, action.TargetUnitId,
                    card.InstanceId, source.PlayRules, piles, recursionSelections: action.RecursionSelections
                        .Where(selection => selection.EffectIndex < rule.Effects.Count).ToArray(),
                    mixSelections: action.MixSelections.Where(selection => selection.EffectIndex < rule.Effects.Count).ToArray());
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                foreach (CombatEvent damage in result.Events.Where(item => item.Kind == "Spell" && item.Amount > 0))
                    lastDamagedUnitIds.Add(damage.Target);
                piles = result.OtherPiles?.ToArray() ?? piles;
                train = result.State!; context = train.Context!;
                if (result.Outcome != RoomOutcome.Exchanged || outcome == RoomOutcome.Exchanged) outcome = result.Outcome;
                effectsApplied = true;
                outputPlayRules = result.PlayRules ?? outputPlayRules;
                discardDrawUpgrades = result.DiscardDrawUpgrades;
                pendingSpellCallbacks = result.PendingCallbacks;
                retainedSpellUnits = retainedSpellUnits.Concat(result.RetainedUnits)
                    .GroupBy(unit => unit.Id).Select(group => group.Last()).ToArray();
                foreach (SpellTargetCollection collection in result.TargetCollections)
                    if (collection.EffectIndex == 0 || rule.Effects[collection.EffectIndex].Target == "DropTargetCharacter")
                        lastMainTargets = collection.UnitIds;
            }
            else if (rule.Effect != "Null") return Unsupported("Unimplemented card effect " + rule.Effect);
            else if (action.PlayerPosition != -1) return Illegal("A no-target card does not take a summon position.");
            else
            {
                string? callbackError = RunBeforePlayCallbacks();
                if (callbackError != null) return Unsupported(callbackError);
            }

            bool terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            if (terminal && context.Statistics != null && context.Statistics.DeckCards == null)
                return Unsupported("Terminal card resolution requires permanent deck membership.");
            // Native marks the card played before Rally, while it still belongs to
            // the resolving buffer and retains its paid cost.
            BattleStatistics? playedStatistics = terminal ? context.Statistics?.RefreshDeckAfterCardTerminal() : context.Statistics;
            playedStatistics = terminal && playedStatistics != null && !playedStatistics.TrackedCards.Contains(card.InstanceId)
                ? playedStatistics.RecordPlayedCard(card.InstanceId) : playedStatistics?.Increment(card.InstanceId, "TimesPlayed");
            // PlayCard records its hand history before queued effects resolve;
            // native terminal ClearCards has already erased it at this boundary.
            if (!terminal && source.Spawn.Train.Context?.EnergyState?.Phase == "MonsterTurn") playedStatistics = playedStatistics?.RecordHandCardPlayed(card.InstanceId);
            context = context.WithStatistics(playedStatistics);
            train = CardSpellModel.WithContext(train, context);
            if (rule.OnCastEffects.Count > 0)
            {
                CardRecursionSelection[] onCastSelections = action.RecursionSelections
                    .Where(selection => selection.EffectIndex >= rule.Effects.Count)
                    .Select(selection => new CardRecursionSelection(selection.EffectIndex - rule.Effects.Count, selection.CardInstanceIds)).ToArray();
                TrainSpellResult result = CardSpellModel.ApplyForCardPlay(train, action.RoomIndex, rule.OnCastEffects,
                    action.TargetUnitId, card.InstanceId, source.PlayRules, piles, recursionSelections: onCastSelections,
                    onCastEffects: true, priorTargets: lastMainTargets);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                foreach (CombatEvent damage in result.Events.Where(item => item.Kind == "Spell" && item.Amount > 0))
                    lastDamagedUnitIds.Add(damage.Target);
                piles = result.OtherPiles?.ToArray() ?? piles;
                train = result.State!; context = train.Context!;
                outputPlayRules = result.PlayRules ?? outputPlayRules;
                outcome = result.Outcome == RoomOutcome.Exchanged ? outcome : result.Outcome;
                pendingSpellCallbacks = pendingSpellCallbacks.Concat(result.PendingCallbacks).ToArray();
                retainedSpellUnits = retainedSpellUnits.Concat(result.RetainedUnits)
                    .GroupBy(unit => unit.Id).Select(group => group.Last()).ToArray();
                effectsApplied = true;
                terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            }
            bool prefixConsumed = false;
            bool incantObserved = train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed"));
            if (incantObserved && (rule.CardType == null || rule.CardType == "Spell" && !rule.IsAnyAbility.HasValue && rule.Ability == null))
                return Unsupported("Card-play triggers require captured card type and spell ability metadata.");
            if (rule.CardType == "Spell" && (rule.IsAnyAbility ?? rule.Ability != null) == false)
            {
                int[] initialCached = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
                    .Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).ToArray();
                foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                {
                    IReadOnlyList<int> cached = context.FindCard(card.InstanceId)?.PlayedRoomUnitIds ?? initialCached;
                    TrainCombatResult incanted = CardPlayedTriggerModel.Spell(train, team, cached,
                        team == CombatTeam.Player ? pendingSpellCallbacks : null);
                    if (!incanted.Supported) return Unsupported(incanted.UnsupportedReason!);
                    train = incanted.State!; context = train.Context!;
                    if (incanted.Outcome != RoomOutcome.Exchanged) outcome = incanted.Outcome;
                }
                prefixConsumed = true;
                piles = context.OtherPiles?.ToArray() ?? piles;
                terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            }
            if (rule.Effect == "SpawnMonster" && (rule.CardType == null || rule.CardType == "Monster") && rule.Summon?.TriggersPaidRally != false)
            {
                int[] initialCached = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
                    .Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).ToArray();
                foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                {
                    IReadOnlyList<int> cached = context.FindCard(card.InstanceId)?.PlayedRoomUnitIds ?? initialCached;
                    TrainCombatResult rallied = CardPlayedTriggerModel.Rally(train, team, cached,
                        team == CombatTeam.Player ? pendingSpellCallbacks : null);
                    if (!rallied.Supported) return Unsupported(rallied.UnsupportedReason!);
                    train = rallied.State!; context = train.Context!;
                    if (rallied.Outcome != RoomOutcome.Exchanged) outcome = rallied.Outcome;
                }
                prefixConsumed = true;
                piles = context.OtherPiles?.ToArray() ?? piles;
                terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            }
            if (!prefixConsumed && pendingSpellCallbacks.Count > 0)
            {
                TrainCombatResult completed = TrainCombatModel.ApplyCharacterQueue(train, pendingSpellCallbacks.ToList());
                if (!completed.Supported) return Unsupported(completed.UnsupportedReason!);
                train = completed.State!; context = train.Context!;
                if (completed.Outcome != RoomOutcome.Exchanged) outcome = completed.Outcome;
                piles = context.OtherPiles?.ToArray() ?? piles;
                terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            }
            IReadOnlyList<RoomCapacityState>? capacitiesBeforeRelics = context.RoomCapacities;
            RelicCardPlayMagicPowerResult cardPlayMagicPower = RelicCardPlayMagicPowerModel.Apply(
                context, card.InstanceId, source.PlayRules, source.Spawn.Turn,
                context.QueryFrame?.SelectedRoomIndex ?? action.RoomIndex, placement: source.Spawn.Turn == 0,
                preview: context.IsolatedBattlePreview == true || train.Rooms.FirstOrDefault(room =>
                    room.RoomIndex == action.RoomIndex)?.Preview == true, trainState: train, playedCardRule: rule,
                lastTargets: lastMainTargets ?? Array.Empty<int>(), originalTargetTeam: originalTargetTeam,
                lastDamagedUnitIds: lastDamagedUnitIds.ToArray());
            if (!cardPlayMagicPower.Supported) return Unsupported(cardPlayMagicPower.UnsupportedReason!);
            context = cardPlayMagicPower.Context;
            train = cardPlayMagicPower.TrainState ?? CardSpellModel.WithContext(train, context);
            if (cardPlayMagicPower.Outcome != RoomOutcome.Exchanged) outcome = cardPlayMagicPower.Outcome;
            RoomAttachmentUnitSyncResult replicatedCharacters = RoomAttachmentUnitModel.Synchronize(train,
                capacitiesBeforeRelics, context.RoomCapacities);
            if (!replicatedCharacters.Supported) return Unsupported(replicatedCharacters.UnsupportedReason!);
            train = replicatedCharacters.State!;
            TrainSpellResult echoed = RoomEchoModel.Remember(train, source, action, rule);
            if (!echoed.Supported) return Unsupported(echoed.UnsupportedReason!);
            train = echoed.State!; context = train.Context!;
            TrainCombatResult delayedStatusRemovals = TrainCombatModel.ProcessDelayedStatusRemovalsAfterCardPlayed(train);
            if (!delayedStatusRemovals.Supported) return Unsupported(delayedStatusRemovals.UnsupportedReason!);
            train = delayedStatusRemovals.State!; context = train.Context!;
            if (delayedStatusRemovals.Outcome != RoomOutcome.Exchanged) outcome = delayedStatusRemovals.Outcome;
            RelicFirstCardCostResult firstCardCost = RelicFirstCardCostModel.ApplyOnCardPlayed(context, card.InstanceId,
                source.PlayRules);
            if (!firstCardCost.Supported) return Unsupported(firstCardCost.UnsupportedReason!);
            context = firstCardCost.Context;
            train = CardSpellModel.WithContext(train, context);
            List<CardToken> hand = context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToList();
            List<CardToken> discard = context.Cards.Discard.ToList();
            if (effectsApplied && !(terminal && context.OtherPiles != null))
            {
                var alive = new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                foreach (CombatUnit dead in source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.SpawnerCardId > 0 && !alive.Contains(unit.Id)))
                {
                    if (train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == dead.SpawnerCardId && alive.Contains(unit.Id)))
                        continue;
                    CombatUnit? removed = retainedSpellUnits.FirstOrDefault(unit => unit.Id == dead.Id);
                    if (removed?.DeathState?.IsDespawned == true && removed.DeathState.IsDestroyed == true &&
                        !removed.DeathState.HasFinishedDying && removed.Health > 0) continue;
                    CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                    CardPileState? exhausted = piles.FirstOrDefault(pile => pile.Name == "Exhausted");
                    CardToken? deadCard = standby?.Cards.FirstOrDefault(item => item.InstanceId == dead.SpawnerCardId);
                    if (deadCard == null && (exhausted?.Cards.Any(item => item.InstanceId == dead.SpawnerCardId) == true ||
                        context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard).Any(item => item.InstanceId == dead.SpawnerCardId) ||
                        (dead.Status("cardless")?.Stacks > 0 || piles.Any(pile => pile.UnitConditions?.Any(binding => binding.HostUnitId == dead.Id) == true)) && context.CardInstances != null &&
                        !context.CardInstances.Any(item => item.InstanceId == dead.SpawnerCardId) &&
                        context.CardRegistry?.Any(item => item.InstanceId == dead.SpawnerCardId) == true)) continue;
                    // A feeding spell already routes consumed carriers to
                    // Eaten. Its outer card cleanup must not exhaust them again.
                    if (piles.Any(pile => (pile.Name == "Purged" || pile.Name == "Eaten") &&
                        pile.Cards.Any(item => item.InstanceId == dead.SpawnerCardId))) continue;
                    if (standby == null || exhausted == null || deadCard == null) return Unsupported("Missing dead unit spawner card routing.");
                    piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(pile, dead.SpawnerCardId) : pile == exhausted
                            ? new CardPileState(pile.Name, pile.Cards.Concat(new[] { deadCard }).ToArray()) : pile).ToArray();
                }
            }
            if (terminal) piles = piles.Select(CardPileModel.Clear).ToArray();
            if (discardDrawUpgrades != null)
                context = context.WithCards(context.Cards.WithNextDrawUpgrades(discardDrawUpgrades));
            CardInstanceState? discardInstance = context.FindCard(card.InstanceId) ?? playingInstance;
            if (discardInstance != null)
            {
                CardInstanceState unfrozen = RelicFreezeHandUpgradeModel.Played(discardInstance);
                if (!ReferenceEquals(unfrozen, discardInstance))
                {
                    // Terminal clearing removes the resolving card from live
                    // instances; its fallback must retain OnCardPlayed cleanup.
                    playingInstance = unfrozen;
                    context = context.WithCard(unfrozen);
                    discardInstance = unfrozen;
                }
            }
            bool skipNaturalDiscard = discardInstance?.MaskDescriptor != null &&
                CardBranchMaskModel.Resolve(discardInstance).Traits.Any(trait =>
                    trait is "CardTraitInfusion" or "CardTraitPersistent");
            bool consumedOnPlay = !skipNaturalDiscard && consumeSuppression != RelicSuppressExhaustModel.Trait && discardInstance != null && CardExhaustModel.ShouldExhaustOnPlayedDiscard(discardInstance);
            bool ephemeralPurgeCallbacks = !skipNaturalDiscard && rule.Destination == "Purged" && discardInstance?.MaskDescriptor != null &&
                CardBranchMaskModel.Resolve(discardInstance).Traits.Contains("CardTraitEphemeral", StringComparer.Ordinal);
            bool ephemeralBeforeConsume = consumedOnPlay && ephemeralPurgeCallbacks &&
                CardExhaustModel.EphemeralDiscardPrecedesConsume(discardInstance!);
            if (ephemeralBeforeConsume && train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "CardExhausted")))
                return Unsupported("Ephemeral before Consume with reactive exhaustion triggers requires native callback-order calibration.");
            if (consumedOnPlay && rule.HandConsumeInteractions?.Count > 0)
                return Unsupported(string.Join("; ", rule.HandConsumeInteractions));
            if (consumedOnPlay && rule.Effect == "SpawnMonster" &&
                !piles.Any(pile => pile.Name == "Standby" && pile.UnitConditions != null))
                return Unsupported("Consume summons require captured native unit standby conditions.");
            // Native DiscardCard/PurgeCard remove a naturally played card's retained buffer reference.
            if (!skipNaturalDiscard)
                piles = piles.Select(pile => pile.Name == "DiscardBuffer" ? CardPileModel.Remove(pile, card.InstanceId) : pile).ToArray();
            if (terminal && rule.Destination != "Discard" && !(rule.Destination == "Standby" && rule.Effect == "SpawnMonster") &&
                !(ephemeralPurgeCallbacks && discardInstance != null && CardCopyOnPlayModel.SupportsTerminalPurge(discardInstance)))
                return Unsupported("Terminal routing for this card effect/destination is not validated.");
            if (!skipNaturalDiscard && rule.Destination == "Discard") discard.Add(card);
            else if (!skipNaturalDiscard)
            {
                CardPileState? destination = piles.FirstOrDefault(pile => pile.Name == rule.Destination);
                if (destination == null || (rule.Destination != "Standby" && rule.Destination != "Purged" && rule.Destination != "Exhausted"))
                    return Unsupported("Unimplemented card destination " + rule.Destination);
                piles = piles.Select(pile =>
                {
                    if (pile != destination) return pile;
                    CardPileState added = CardPileModel.Add(pile, card);
                    // Ephemeral calls PurgeCard from both OnCardPlayed and
                    // OnCardDiscarded; a separate SelfPurge discard callback adds a third reference.
                    if (ephemeralPurgeCallbacks)
                    {
                        added = CardPileModel.Add(added, card);
                        if (CardBranchMaskModel.Resolve(discardInstance!).Traits.Contains("CardTraitSelfPurge", StringComparer.Ordinal))
                            added = CardPileModel.Add(added, card);
                    }
                    return added;
                }).ToArray();
            }
            if (rule.Effect == "Equipment")
            {
                if (rule.Equipment == null || action.TargetUnitId <= 0) return Unsupported("Missing equipment standby host.");
                piles = piles.Select(pile => pile.Name == "Standby" ? CardPileModel.BindEquipment(pile,
                    new EquipmentStandbyCondition(card.InstanceId, action.TargetUnitId, rule.Equipment.ReturnToHand)) : pile).ToArray();
            }
            if (rule.Effect == "SpawnMonster" && rule.Destination == "Standby" && spawnedId.HasValue &&
                piles.Any(pile => pile.Name == "Standby" && pile.UnitConditions != null))
                piles = piles.Select(pile => pile.Name == "Standby" ? CardPileModel.BindUnit(pile,
                    new UnitStandbyCondition(card.InstanceId, spawnedId.Value,
                        !train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == spawnedId.Value))) : pile).ToArray();
            bool summonRemoved = spawnedId.HasValue && !train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == spawnedId.Value);
            bool summonedEndless = summonRemoved && !eatenDuringBirth && rule.SpawnUnit?.Statuses.Any(status => status.Id == "endless" && status.Stacks > 0) == true;
            if (summonRemoved && !terminal && !consumedOnPlay)
            {
                if (!summonedEndless)
                {
                    CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                    CardPileState? exhausted = piles.FirstOrDefault(pile => pile.Name == (eatenDuringBirth ? "Eaten" : "Exhausted"));
                    if (standby == null || exhausted == null) return Unsupported("Missing removed summon card routing.");
                    piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(pile, card.InstanceId) :
                        pile == exhausted ? CardPileModel.Add(pile, card) : pile).ToArray();
                }
            }
            if (terminal && context.Statistics != null && context.Statistics.DeckCards == null)
                return Unsupported("Terminal card resolution requires permanent deck membership.");
            // Discard restores the resolving card after terminal clearing.
            if (!skipNaturalDiscard)
            {
                RelicCheaperSpellUpgradeResult discarded = RelicCheaperSpellUpgradeModel.CardDiscarded(context, card.InstanceId);
                if (!discarded.Supported) return Unsupported(discarded.UnsupportedReason!);
                context = discarded.Context;
                RelicCheaperSpellUpgradeResult randomDiscarded = RelicRandomHandUpgradeModel.Discarded(context, card.InstanceId);
                if (!randomDiscarded.Supported) return Unsupported(randomDiscarded.UnsupportedReason!);
                context = randomDiscarded.Context;
                var suppressionApplied = RelicSuppressExhaustModel.Discarded(context, out _, consumeSuppression, preview: context.IsolatedBattlePreview == true);
                if (!suppressionApplied.Supported) return Unsupported(suppressionApplied.UnsupportedReason!);
                context = suppressionApplied.Context;
                CardInstanceState? energyCard = context.FindCard(card.InstanceId) ?? discardInstance;
                if (energyCard != null)
                {
                    CardScaledEnergyResult gained = CardScaledEnergyModel.Discarded(context, energyCard, wasPlayed: true);
                    if (!gained.Supported) return Unsupported(gained.UnsupportedReason!);
                    context = gained.Context!;
                    if (CardDiscardDrawModel.Present(energyCard))
                    {
                        // Earlier discard traits have already removed the played card before
                        // its scaling draw runs. Keep the cards that this callback draws.
                        context = context.WithCards(new CardCycleState(hand, context.Cards.Draw, discard, context.Cards.Rng,
                            context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw,
                            context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn)).WithOtherPiles(piles);
                        TypedDrawResult discardDraw = CardDiscardDrawModel.Discarded(context, energyCard, wasPlayed: true, source.PlayRules);
                        if (!discardDraw.Supported) return Unsupported(discardDraw.UnsupportedReason!);
                        context = discardDraw.Context!;
                        hand = context.Cards.Hand.ToList(); discard = context.Cards.Discard.ToList();
                    }
                }
            }
            bool copyOnDiscard = !skipNaturalDiscard && discardInstance != null && CardCopyOnPlayModel.Present(discardInstance);
            BattleStatistics? statistics = context.Statistics;
            if (terminal) statistics = statistics?.RefreshOwnedCards(new[] { card.InstanceId });
            if (!skipNaturalDiscard) statistics = statistics?.Increment(card.InstanceId, "TimesDiscarded");
            statistics = statistics?.WithPlayedCost(card.InstanceId, null);
            if (!skipNaturalDiscard && !terminal && rule.Destination == "Exhausted") statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            if (!skipNaturalDiscard && summonRemoved && !eatenDuringBirth && !summonedEndless && !terminal && !consumedOnPlay)
                statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            if (!skipNaturalDiscard && rule.Destination == "Purged")
            {
                if (statistics?.DeckCards?.Contains(card.InstanceId) == true && context.Resources != null)
                    context = context.WithResources(context.Resources.WithVisibleDeckCount(context.Resources.VisibleDeckCount - 1));
                statistics = statistics?.RemoveDeckCard(card.InstanceId);
            }
            context = context.AfterCardEffects();
            context = new CombatContext(new CardCycleState(hand, context.Cards.Draw, discard, context.Cards.Rng,
                context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn), context.BattleRng,
                context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, statistics,
                terminal ? playingInstance == null ? context.CardInstances : new[] { skipNaturalDiscard
                    ? context.FindCard(card.InstanceId) ?? playingInstance : (context.FindCard(card.InstanceId) ?? playingInstance).OnDiscard(true, nativePlayedCost, consumeSuppression == RelicSuppressExhaustModel.Trait) } :
                context.CardInstances?.Select(instance => instance.InstanceId == card.InstanceId
                    ? skipNaturalDiscard ? instance : instance.OnDiscard(true, nativePlayedCost, consumeSuppression == RelicSuppressExhaustModel.Trait) : instance).ToArray(), context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades,
                context.OtherPiles == null ? null : piles, context.QueryFrame?.With(runningCombat: !terminal), context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState, context.ConduitRetained, context.LifeLinks);
            if (!skipNaturalDiscard) context = ConduitModel.Discarded(context, card.InstanceId);
            if (summonedEndless && !terminal && !consumedOnPlay)
                context = UnitStandbyModel.ReturnEndless(context, card.InstanceId);
            if (consumedOnPlay)
            {
                if (context.OtherPiles == null || !context.OtherPiles.Any(pile => pile.Name == "Standby") ||
                    !context.OtherPiles.Any(pile => pile.Name == "Exhausted")) return Unsupported("Consume requires captured standby and exhausted piles.");
                // Ephemeral purges again from OnCardDiscarded after Consume moves
                // the card to standby. That removes the pending standby return,
                // so only MoveToStandByPile records an exhaustion.
                context = CardExhaustModel.MoveConsumedToStandby(context, card);
            }
            // Native records the discard and earlier Consume callback before creating
            // the copy. The new identity must not inherit their broadcast statistics.
            if (copyOnDiscard)
            {
                CardGenerationResult copied = CardCopyOnPlayModel.Discarded(context, context.FindCard(card.InstanceId)!);
                if (!copied.Supported) return Unsupported(copied.UnsupportedReason!);
                context = copied.Context!;
                // This calibrated box keeps CopyOnPlay before Ephemeral in its
                // discard callbacks. The later purge refreshes deck trait queries
                // after the new copy has entered the native owned-card set.
                if (ephemeralPurgeCallbacks && copied.AddedCards.Count > 0 && context.CardInstances != null)
                    context = context.WithStatistics(context.Statistics?.RefreshOwnedCards(
                        context.CardInstances.Select(instance => instance.InstanceId)));
            }
            if (consumedOnPlay)
            {
                // A later installed Consume trait runs after Ephemeral's
                // discard purge. Its new standby entry then returns normally,
                // recording the second exhaustion instead of being purged.
                context = ephemeralPurgeCallbacks && !ephemeralBeforeConsume
                    ? context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name is "Standby" or "Exhausted"
                        ? CardPileModel.Remove(pile, card.InstanceId) : pile).ToArray())
                    : CardExhaustModel.ReturnReady(context);
            }
            RoomCombatState[] rooms = train.Rooms.Select(room =>
            {
                return new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                    context, room.Preview, room.ActiveCompanionBossIds);
            }).ToArray();
            context = UnitStandbyModel.ReturnReady(context);
            piles = context.OtherPiles?.ToArray() ?? piles;
            rooms = rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            var living = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            train = new TrainCombatState(rooms, train.Movement.Where(rule => living.Contains(rule.UnitId)).ToArray(), train.EnemySlotsPerRoom, context);
            TrainCombatResult returnedEquipment = EquipmentModel.ReturnUnattached(train, action.RoomIndex);
            if (!returnedEquipment.Supported) return Unsupported(returnedEquipment.UnsupportedReason!);
            train = returnedEquipment.State!; context = train.Context!; piles = context.OtherPiles?.ToArray() ?? piles;
            if (returnedEquipment.Outcome != RoomOutcome.Exchanged) outcome = returnedEquipment.Outcome;
            terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            if (consumedOnPlay && !terminal)
            {
                TrainCombatResult exhausted = CardPlayedTriggerModel.Exhausted(train, action.RoomIndex);
                if (!exhausted.Supported) return Unsupported(exhausted.UnsupportedReason!);
                train = exhausted.State!; context = train.Context!; piles = context.OtherPiles?.ToArray() ?? piles;
                if (exhausted.Outcome != RoomOutcome.Exchanged) outcome = exhausted.Outcome;
            }
            // CardManager.OnCardPlayed refreshes auras after discard/cost clearing,
            // then drains their callbacks before the UI combat preview can start.
            TrainCombatResult cardCompleted = EnchantmentWorldModel.UpdateAll(train);
            if (!cardCompleted.Supported) return Unsupported(cardCompleted.UnsupportedReason!);
            train = cardCompleted.State!; context = train.Context!;
            if (source.BattlePreviewEnabled && !terminal)
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train, outputPlayRules);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!; context = train.Context!;
            }
            EnemySpawnState spawn = source.Spawn;
            // Card kills enter the managers' removal lists. Their Unity objects
            // and bound auras survive until the turn-end ProcessRemovals call.
            if (source.CanonicalPhysicalReferences) train = TrainCombatModel.CompleteFrameRemovals(train, Array.Empty<int>());
            else if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
            spawn = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
                train.Context?.NextUnitId ?? nextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, outerBoss: spawn.OuterBoss, queuedOuterBoss: spawn.QueuedOuterBoss,
                queuedTrueFinalBoss: spawn.QueuedTrueFinalBoss, trueFinalBoss: spawn.TrueFinalBoss);
            spawn = ForcedRelentlessModel.Synchronize(spawn);
            if (context.RoomCapacities != null) outputPlayRules = outputPlayRules.WithCapacities(context.RoomCapacities);
            return new BattleActionResult(new BattleTurnState(spawn, context.EnergyState == null ? source.Energy - energyPaidCost : context.QueryFrame!.Energy!.Value, source.EnergyPerTurn,
                Math.Max(0, source.DrawPerTurn - UnitRoomResourceModel.Amount(source.Spawn.Train, UnitRoomResourceModel.Draw) +
                    UnitRoomResourceModel.Amount(train, UnitRoomResourceModel.Draw)), context.QueryFrame?.ForgePoints ?? source.ForgePoints,
                context.QueryFrame?.DragonsHoard ?? source.DragonsHoard,
                context.QueryFrame?.MoonPhase == 1 ? "New" : context.QueryFrame?.MoonPhase == 2 ? "Full" : source.MoonPhase,
                source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? context.BattleRng : stream.Name == "CardDraw" ? context.Cards.Rng : stream.State)).ToArray(),
                piles, BattleTurnModel.AfterActionInteractions(source, context), outputPlayRules, source.BattlePreviewEnabled, source.UiRngIsolated,
                source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                train.Context?.QueryFrame?.SelectedRoomIndex ?? (source.SelectedRoom < 0 ? source.SelectedRoom : action.RoomIndex),
                context.QueryFrame?.MoonPhaseTrackingActive ?? source.MoonPhaseTrackingActive), outcome: outcome);
        }

        // Unsupported attempts are kept separate from legal actions so callers can detect an incomplete search frontier.
        public static BattleActionEnumeration EnumerateActions(BattleTurnState source, int? onlyCardId = null, bool includeAbilities = true)
            => EnumerateActions(source, onlyCardId, includeAbilities, null);

        internal static BattleActionEnumeration EnumerateActions(BattleTurnState source, int? onlyCardId, bool includeAbilities, BattleSearchWorkspace? workspace)
        {
            var actions = new List<SupportedActionCandidate>();
            var unsupported = new List<UnsupportedActionAttempt>();
            if (source.PlayRules == null || source.Spawn.Train.Context == null)
            {
                unsupported.Add(new UnsupportedActionAttempt(null, "Missing card/room play definitions or battle context."));
                return new BattleActionEnumeration(actions, unsupported);
            }
            if (source.PlayRules.Rooms.Count == 0)
                unsupported.Add(new UnsupportedActionAttempt(null, "No captured room play definitions."));
            foreach (RoomCombatState room in source.Spawn.Train.Rooms)
                if (!source.PlayRules.Rooms.Any(rule => rule.RoomIndex == room.RoomIndex))
                    unsupported.Add(new UnsupportedActionAttempt(null, "Missing room play definition for room " + room.RoomIndex + "."));
            foreach (RoomPlayRule room in source.PlayRules.Rooms)
                if (!source.Spawn.Train.Rooms.Any(state => state.RoomIndex == room.RoomIndex))
                    unsupported.Add(new UnsupportedActionAttempt(null, "Missing room state for room " + room.RoomIndex + "."));
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand.Where(card => !onlyCardId.HasValue || card.InstanceId == onlyCardId))
            foreach (RoomPlayRule room in source.PlayRules.Rooms)
            {
                if (!source.Spawn.Train.Rooms.Any(state => state.RoomIndex == room.RoomIndex)) continue;
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule == null)
                {
                    unsupported.Add(new UnsupportedActionAttempt(new PlayCardAction(card.InstanceId, room.RoomIndex),
                        "Missing captured card play definition; the action target space is unknown."));
                    continue;
                }
                CardInstanceState? targetInstance = source.Spawn.Train.Context.FindCard(card.InstanceId);
                if (targetInstance?.Permanent.MergedEquipment != null) rule = targetInstance.Permanent.MergedEquipment.Resolve(rule);
            if (CardSpellModel.RequiresUnitTarget(rule.Effects.Concat(rule.OnCastEffects).ToArray()))
                {
                RoomCombatState targetRoom = source.Spawn.Train.Rooms.First(item => item.RoomIndex == room.RoomIndex);
                int targetedPlayerCount = targetRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
                int firstTargetPosition = rule.Effect == "SpawnMonster" ? 0 : -1;
                int lastTargetPosition = rule.Effect == "SpawnMonster" ? targetedPlayerCount : -1;
                foreach (CombatUnit target in targetRoom.Units)
                for (int position = firstTargetPosition; position <= lastTargetPosition; position++)
                    {
                        foreach (PlayCardAction branch in EnumerateRecursionChoices(source,
                        new PlayCardAction(card.InstanceId, room.RoomIndex, position, target.Id), rule))
                            Attempt(branch);
                    }
                    continue;
                }
                int players = source.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == room.RoomIndex)?
                    .Units.Count(unit => unit.Team == CombatTeam.Player) ?? 0;
                int first = rule?.Effect == "SpawnMonster" ? 0 : -1;
                int last = rule?.Effect == "SpawnMonster" ? players : -1;
                for (int position = first; position <= last; position++)
                    foreach (PlayCardAction branch in EnumerateRecursionChoices(source,
                        new PlayCardAction(card.InstanceId, room.RoomIndex, position), rule!))
                        Attempt(branch);
            }
            if (includeAbilities)
            {
                BattleActionEnumeration abilities = workspace?.Abilities(source) ?? UnitAbilityModel.EnumerateActivations(source);
                actions.AddRange(abilities.SupportedCandidates);
                unsupported.AddRange(abilities.UnsupportedAttempts);
                BattleActionEnumeration roomAbilities = RoomAbilityModel.EnumerateActivations(source, workspace == null ? null : workspace.Play);
                actions.AddRange(roomAbilities.SupportedCandidates);
                unsupported.AddRange(roomAbilities.UnsupportedAttempts);
            }
            return new BattleActionEnumeration(actions, unsupported);

            void Attempt(PlayCardAction action)
            {
                BattleActionResult result = workspace?.Play(source, action) ?? PlayCard(source, action);
                if (result.Supported) actions.Add(new SupportedActionCandidate(action, result));
                else if (result.Rejection == ActionRejection.Unsupported)
                    unsupported.Add(new UnsupportedActionAttempt(action, result.Reason ?? "Unsupported card action."));
            }
        }

        private static IEnumerable<PlayCardAction> EnumerateRecursionChoices(BattleTurnState source,
            PlayCardAction action, CardPlayRule rule)
        {
            var choiceEffects = rule.Effects.Concat(rule.OnCastEffects).Select((effect, index) => (effect, index))
                .Where(item => item.effect.Type is "ChooseDiscard" or "FreezeCard" or "DuplicateCard" or "MergeEquipment" || item.effect.Type == "Recursion" &&
                    item.effect.Recursion?.SelectionMode is "ChooseToHand" or "ChooseToDeck").ToArray();
            if (choiceEffects.Length == 0)
            {
                foreach (PlayCardAction branch in EnumerateMixChoices(source, action, rule)) yield return branch;
                yield break;
            }

            var selections = new List<CardRecursionSelection>();
            var usedCards = new HashSet<int>();
            foreach (PlayCardAction branch in Expand(0)) yield return branch;

            IEnumerable<PlayCardAction> Expand(int choiceIndex)
            {
                if (choiceIndex == choiceEffects.Length)
                {
                    var chosen = new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                        action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex, selections, activatorRoomIndex: action.ActivatorRoomIndex);
                    foreach (PlayCardAction branch in EnumerateMixChoices(source, chosen, rule)) yield return branch;
                    yield break;
                }

                (CardActionEffect effect, int effectIndex) = choiceEffects[choiceIndex];
                CombatContext context = source.Spawn.Train.Context!;
                if (effect.Type == "MergeEquipment")
                {
                    IReadOnlyList<IReadOnlyList<int>> mergeChoices = EquipmentMergeModel.Choices(context, effect.EquipmentMerge!, out string? mergeError);
                    if (mergeError != null) yield break;
                    foreach (IReadOnlyList<int> choice in mergeChoices)
                    {
                        selections.Add(new CardRecursionSelection(effectIndex, choice));
                        foreach (PlayCardAction branch in Expand(choiceIndex + 1)) yield return branch;
                        selections.RemoveAt(selections.Count - 1);
                    }
                    yield break;
                }
                string? error;
                IReadOnlyList<int> eligible = effect.Type == "ChooseDiscard"
                    ? CardChooseDiscardModel.EligibleCards(context, effect, action.CardInstanceId, out error)
                    : effect.Type == "FreezeCard" ? CardFreezeModel.EligibleCards(context, action.CardInstanceId, out error)
                    : effect.Type == "DuplicateCard" ? CardDuplicateModel.EligibleCards(context, effect, action.CardInstanceId, out error)
                    : CardRecursionModel.EligibleCards(context.Cards, context.OtherPiles, effect, source.PlayRules,
                        action.CardInstanceId, out error, context.CardRegistry);
                if (error != null || eligible.Count == 0)
                {
                    foreach (PlayCardAction branch in Expand(choiceIndex + 1)) yield return branch;
                    yield break;
                }

                foreach (IReadOnlyList<int> choice in CardRecursionModel.EnumerateChosenCards(eligible))
                {
                    if (choice.Any(usedCards.Contains)) continue;
                    selections.Add(new CardRecursionSelection(effectIndex, choice));
                    foreach (int id in choice) usedCards.Add(id);
                    foreach (PlayCardAction branch in Expand(choiceIndex + 1)) yield return branch;
                    foreach (int id in choice) usedCards.Remove(id);
                    selections.RemoveAt(selections.Count - 1);
                }
            }
        }

        private static IEnumerable<PlayCardAction> EnumerateMixChoices(BattleTurnState source,
            PlayCardAction action, CardPlayRule rule)
        {
            if (rule.Effects.Any(effect => effect.Type == "CraftSpike"))
            {
                foreach (PlayCardAction choice in SpikeCraftModel.Actions(rule, action)) yield return choice;
                yield break;
            }
            if (rule.Effects.All(effect => effect.Type != "Mix"))
            {
                yield return action;
                yield break;
            }
            // Other effects can change the hand or Battle RNG before the next draft.
            if (rule.Effects.Any(effect => effect.Type != "Mix" || effect.Value < 1) ||
                rule.OnCastEffects.Any(effect => effect.Type == "Mix") ||
                source.PlayRules?.Elixirs == null || source.Spawn.Train.Context?.MixState == null)
            {
                yield return action;
                yield break;
            }
            CombatContext context = source.Spawn.Train.Context;
            context = context.WithCards(new CardCycleState(context.Cards.Hand.Where(card => card.InstanceId != action.CardInstanceId).ToArray(),
                context.Cards.Draw, context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier,
                context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades,
                context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn));
            var choices = new List<ElixirMixSelection>();
            foreach (PlayCardAction branch in Expand(0, context)) yield return branch;

            IEnumerable<PlayCardAction> Expand(int effectIndex, CombatContext currentContext)
            {
                if (effectIndex == rule.Effects.Count)
                {
                    yield return new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                        action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex,
                        action.RecursionSelections, choices, action.ActivatorRoomIndex);
                    yield break;
                }
                CardActionEffect effect = rule.Effects[effectIndex];
                ElixirCatalog catalog = source.PlayRules.Elixirs;
                ElixirMixPlan plan = ElixirMixModel.Plan(currentContext, effect.Value);
                if (!plan.Supported)
                {
                    yield return new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                        action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex,
                        action.RecursionSelections, choices, action.ActivatorRoomIndex);
                    yield break;
                }
                foreach (PlayCardAction branch in ExpandMix(0, plan.Context!)) yield return branch;

                IEnumerable<PlayCardAction> ExpandMix(int mixNumber, CombatContext mixedContext)
                {
                    if (mixNumber == plan.Amounts.Count)
                    {
                        foreach (PlayCardAction branch in Expand(effectIndex + 1, mixedContext)) yield return branch;
                        yield break;
                    }
                    CardToken? current = mixedContext.Cards.Hand.FirstOrDefault(card =>
                        catalog.Recipes.Any(recipe => recipe.Creation.DataId == card.DataId));
                    if (current == null && mixedContext.Cards.Hand.Count >= mixedContext.MaxHandSize)
                    {
                        foreach (PlayCardAction branch in ExpandMix(mixNumber + 1, mixedContext)) yield return branch;
                        yield break;
                    }
                    int amount = plan.Amounts[mixNumber];
                    ElixirDraftOptions draft = current == null
                        ? ElixirMixModel.NewDraft(catalog, mixedContext.BattleRng, amount,
                            mixedContext.MixState!.LastCardDataId)
                        : ElixirMixModel.UpgradeDraft(catalog.Recipes.First(recipe => recipe.Creation.DataId == current.DataId),
                            mixedContext.BattleRng, amount);
                    if (draft.Offers.Count == 0)
                    {
                        foreach (PlayCardAction branch in ExpandMix(mixNumber + 1, mixedContext)) yield return branch;
                        yield break;
                    }
                    for (int choiceIndex = 0; choiceIndex < draft.Offers.Count; choiceIndex++)
                    {
                        CardGenerationResult applied = ElixirMixModel.Apply(mixedContext, catalog, amount, choiceIndex);
                        choices.Add(new ElixirMixSelection(effectIndex, mixNumber, choiceIndex));
                        if (applied.Supported)
                            foreach (PlayCardAction branch in ExpandMix(mixNumber + 1, applied.Context!)) yield return branch;
                        else
                            yield return new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                            action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex,
                            action.RecursionSelections, choices, action.ActivatorRoomIndex);
                        choices.RemoveAt(choices.Count - 1);
                    }
                }
            }
        }

        public static IReadOnlyList<PlayCardAction> EnumerateSupportedPlays(BattleTurnState source, int? onlyCardId = null) =>
            EnumerateActions(source, onlyCardId).SupportedActions;

        public static PlayCardAction? ChooseUnitAndJunkPlay(BattleTurnState source)
            => ChoosePlay(source, false);
        public static PlayCardAction? ChooseUnitAndJunkPlay(BattleTurnState source, Func<CardToken, bool> includeCard)
            => ChoosePlay(source, false, includeCard);
        public static PlayCardAction? ChooseUnitSpellAndJunkPlay(BattleTurnState source)
            => ChoosePlay(source, true);
        public static PlayCardAction? ChooseUnitSpellAndJunkPlay(BattleTurnState source, Func<CardToken, bool> includeCard)
            => ChoosePlay(source, true, includeCard);
        public static PlayCardAction? ChooseEmptySpellThenCards(BattleTurnState source)
        {
            PlayCardAction? ordinary = ChooseUnitSpellAndJunkPlay(source);
            if (ordinary != null)
            {
                CardToken card = source.Spawn.Train.Context!.Cards.Hand.Single(item => item.InstanceId == ordinary.CardInstanceId);
                if (source.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).Effect is "SpawnMonster" or "Equipment")
                    return ordinary;
            }
            // Verification policy: observe Incant before a later spell changes the cached actors' statuses.
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return ordinary;
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule?.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Null") continue;
                foreach (RoomCombatState room in source.Spawn.Train.Rooms.Where(room => !source.PlayRules.Rooms.Single(rule => rule.RoomIndex == room.RoomIndex).IsPyre)
                    .OrderByDescending(room => room.Units.Count(unit => unit.Health > 0 && unit.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed"))))
                {
                    var play = new PlayCardAction(card.InstanceId, room.RoomIndex);
                    if (PlayCard(source, play).Supported) return play;
                }
            }
            return ordinary;
        }
        public static PlayCardAction? ChooseMultiSummonThenCards(BattleTurnState source)
        {
            foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand.Where(card =>
                source.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.Summon != null)))
            foreach (int room in new[] { 0, 1, 2 })
            {
                int position = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == room).Units.Any(unit => unit.Team == CombatTeam.Player) ? 1 : 0;
                var action = new PlayCardAction(card.InstanceId, room, position);
                if (PlayCard(source, action).Supported) return action;
            }
            return ChooseUnitSpellAndJunkPlay(source);
        }
        public static PlayCardAction? ChooseBossRoomSummonThenCards(BattleTurnState source)
        {
            foreach (RoomCombatState room in source.Spawn.Train.Rooms.Where(room =>
                room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.EndsBattleOnDeath && unit.Health > 0)))
                foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand.Where(card =>
                    source.PlayRules?.Cards.Any(rule => rule.DataId == card.DataId && rule.Effect == "SpawnMonster") == true))
                {
                    var action = new PlayCardAction(card.InstanceId, room.RoomIndex, 0);
                    if (PlayCard(source, action).Supported) return action;
                }
            return ChooseUnitSpellAndJunkPlay(source);
        }
        internal static SupportedActionCandidate? ChooseUnitSpellAndJunkCandidate(BattleTurnState source,
            out IReadOnlyList<UnsupportedActionAttempt> unsupported, BattleSearchWorkspace? workspace = null)
        {
            var queried = new Dictionary<int, BattleActionEnumeration>();
            IReadOnlyList<PlayCardAction> Choices(CardToken card)
            {
                if (!queried.TryGetValue(card.InstanceId, out BattleActionEnumeration frontier))
                    queried.Add(card.InstanceId, frontier = workspace?.Enumerate(source, card.InstanceId, false) ?? EnumerateActions(source, card.InstanceId, includeAbilities: false));
                return frontier.SupportedActions;
            }
            PlayCardAction? selected = ChoosePlay(source, true, cardPlays: Choices);
            unsupported = queried.Values.SelectMany(frontier => frontier.UnsupportedAttempts).ToArray();
            return selected == null ? null : queried.Values.SelectMany(frontier => frontier.SupportedCandidates)
                .First(candidate => ReferenceEquals(candidate.Action, selected));
        }
        private static PlayCardAction? ChoosePlay(BattleTurnState source, bool spells, Func<CardToken, bool>? includeCard = null,
            Func<CardToken, IReadOnlyList<PlayCardAction>>? cardPlays = null)
        {
            if (source.PlayRules == null) return null;
            IReadOnlyList<PlayCardAction>? plays = cardPlays == null ? EnumerateSupportedPlays(source) : null;
            int roomCount = source.PlayRules.Rooms.Count(room => !room.IsPyre);
            if (roomCount == 0) return null;
            // A deterministic verification policy: rotate floors each turn, summon before clearing junk.
            // It is intentionally simple; a search may choose any enumerated action instead.
            foreach (string effect in spells ? new[] { "SpawnMonster", "Equipment", "Spell", "Null" } : new[] { "SpawnMonster", "Null" })
            foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand)
            {
                if (includeCard != null && !includeCard(card)) continue;
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule?.Effect != effect) continue;
                IReadOnlyList<PlayCardAction> choices = cardPlays == null ? plays! : cardPlays(card);
                IEnumerable<int> roomOrder = Enumerable.Range(0, roomCount).Select(offset => (source.Spawn.Turn + offset) % roomCount);
                if (effect == "Spell" && !CardSpellModel.RequiresUnitTarget(rule.Effects))
                {
                    CardActionEffect? area = rule.Effects.FirstOrDefault(item => item.Type != "HandUpgrade");
                    if (area != null)
                        roomOrder = roomOrder.OrderByDescending(index => source.Spawn.Train.Rooms.Single(room => room.RoomIndex == index)
                            .Units.Count(unit => !unit.Statuses.Any(status => status.Id == "untouchable") &&
                                (unit.Team == CombatTeam.Enemy ? area.AllowEnemy : area.AllowPlayer)));
                }
                foreach (int roomIndex in roomOrder)
                {
                    int count = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex)
                        .Units.Count(unit => unit.Team == CombatTeam.Player);
                    if (effect == "Spell" || effect == "Equipment")
                    {
                        if (!CardSpellModel.RequiresUnitTarget(rule.Effects))
                        {
                            PlayCardAction? roomSpell = choices.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                                action.RoomIndex == roomIndex && action.TargetUnitId == 0);
                            if (roomSpell != null) return roomSpell;
                            continue;
                        }
                        CardActionEffect firstTarget = rule.Effects.First(item => item.Target == "DropTargetCharacter" && item.Type != "HandUpgrade");
                        bool damage = firstTarget.AllowEnemy && (!firstTarget.AllowPlayer || firstTarget.Type == "Damage");
                        var targets = source.Spawn.Train.Rooms[roomIndex].Units.Where(unit =>
                            !unit.IsPyre && unit.Team == (damage ? CombatTeam.Enemy : CombatTeam.Player)).ToArray();
                        foreach (CombatUnit target in damage ? targets : targets.Reverse())
                        {
                            PlayCardAction? cast = choices.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                                action.RoomIndex == roomIndex && action.TargetUnitId == target.Id);
                            if (cast != null) return cast;
                        }
                        continue;
                    }
                    int position = effect == "SpawnMonster" ? (source.Spawn.Turn + card.InstanceId) % (count + 1) : -1;
                    PlayCardAction? choice = choices.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                        action.RoomIndex == roomIndex && action.PlayerPosition == position);
                    if (choice != null) return choice;
                }
            }
            return null;
        }
        private static BattleActionResult Illegal(string reason) => new BattleActionResult(null, ActionRejection.Illegal, reason);
        private static BattleActionResult Unsupported(string reason) => new BattleActionResult(null, ActionRejection.Unsupported, reason);
    }
}
