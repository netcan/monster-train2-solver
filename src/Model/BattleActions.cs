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
        public string SacrificeSubtype { get; }
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
        public int? StatusAdjustmentFilter { get; }
        public bool DamagePerHpLastAttacker { get; }
        public bool DamagePerHpIncludeArmor { get; }
        public RoomCapacityAttachmentState? RoomAttachment { get; }
        public CardRecursionRule? Recursion { get; }
        public CardActionEffect(string type, string target, int value, bool allowEnemy, bool allowPlayer, IReadOnlyList<CombatStatus> statuses,
            CardUpgradeModifier? upgrade = null, string lifetime = "", CardEffectTests? tests = null, CardEffectRange? range = null, CardTargetFilters? filters = null,
            CardGenerationRule? generation = null, bool onlyIfNoEnemies = false, bool? cooldownParameter = null,
            AbilityChangeRule? abilityChange = null, bool? copyHeroStats = null, bool scaleUpgrade = false,
            bool? doNotFullyRemove = null, string sacrificeSubtype = "", TypedDrawRule? typedDraw = null,
            NextDrawUpgradeRule? nextDrawUpgrade = null, int requiredGold = 0, bool? goldCostRequired = null,
            bool useCardTraits = false, bool requireEquipment = false, bool useSelectedRoom = false,
            bool countStatusOnlyOnTargets = false, CombatUnit? spawnFallbackUnit = null,
            bool disallowStatusStackModifiers = false, int? statusAdjustmentFilter = null,
            bool damagePerHpLastAttacker = false, bool damagePerHpIncludeArmor = false,
            RoomCapacityAttachmentState? roomAttachment = null, CardRecursionRule? recursion = null)
        { Type = type; Target = target; Value = value; AllowEnemy = allowEnemy; AllowPlayer = allowPlayer; Statuses = Array.AsReadOnly(statuses.ToArray());
            Upgrade = upgrade; Lifetime = lifetime; Tests = tests; Range = range; Filters = filters; Generation = generation; OnlyIfNoEnemies = onlyIfNoEnemies;
            CooldownParameter = cooldownParameter; AbilityChange = abilityChange; CopyHeroStats = copyHeroStats; ScaleUpgrade = scaleUpgrade;
            DoNotFullyRemove = doNotFullyRemove; SacrificeSubtype = sacrificeSubtype;
            TypedDraw = typedDraw; NextDrawUpgrade = nextDrawUpgrade; RequiredGold = requiredGold; GoldCostRequired = goldCostRequired;
            UseCardTraits = useCardTraits; RequireEquipment = requireEquipment; UseSelectedRoom = useSelectedRoom;
            CountStatusOnlyOnTargets = countStatusOnlyOnTargets; SpawnFallbackUnit = spawnFallbackUnit;
            DisallowStatusStackModifiers = disallowStatusStackModifiers; StatusAdjustmentFilter = statusAdjustmentFilter;
            DamagePerHpLastAttacker = damagePerHpLastAttacker; DamagePerHpIncludeArmor = damagePerHpIncludeArmor;
            RoomAttachment = roomAttachment; Recursion = recursion; }
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
        public CardPlayRule(string dataId, string assetKey, int cost, string effect, string destination,
            CombatUnit? spawnUnit, IReadOnlyList<string> externalInteractions, IReadOnlyList<CardActionEffect>? effects = null,
            IReadOnlyList<string>? upgradeInteractions = null, IReadOnlyList<string>? handDiscardInteractions = null,
            IReadOnlyList<string>? handConsumeInteractions = null, string costType = "Default", EquipmentDefinition? equipment = null,
            CardAbilityRule? ability = null, UnitSummonRule? summon = null, string? cardType = null, bool? isAnyAbility = null,
            bool? canPlayInEngineRoom = null, IReadOnlyList<CardActionEffect>? onCastEffects = null)
        {
            DataId = dataId; AssetKey = assetKey; Cost = cost; CostType = costType; Effect = effect; Destination = destination;
            Equipment = equipment; Ability = ability; CardType = cardType; IsAnyAbility = isAnyAbility; CanPlayInEngineRoom = canPlayInEngineRoom;
            SpawnUnit = spawnUnit; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            Summon = summon;
            Effects = Array.AsReadOnly((effects ?? Array.Empty<CardActionEffect>()).ToArray());
            OnCastEffects = Array.AsReadOnly((onCastEffects ?? Array.Empty<CardActionEffect>()).ToArray());
            UpgradeInteractions = upgradeInteractions == null ? null : Array.AsReadOnly(upgradeInteractions.ToArray());
            HandDiscardInteractions = handDiscardInteractions == null ? null : Array.AsReadOnly(handDiscardInteractions.ToArray());
            HandConsumeInteractions = handConsumeInteractions == null ? null : Array.AsReadOnly(handConsumeInteractions.ToArray());
        }
        internal CardPlayRule WithSpawn(CombatUnit unit) => new CardPlayRule(DataId, AssetKey, Cost, Effect, Destination,
            unit, ExternalInteractions, Effects, UpgradeInteractions, HandDiscardInteractions, HandConsumeInteractions, CostType, Equipment, Ability, Summon,
            CardType, IsAnyAbility, CanPlayInEngineRoom, OnCastEffects);
    }

    public sealed class BattlePlayRules
    {
        public IReadOnlyList<RoomPlayRule> Rooms { get; }
        public IReadOnlyList<CardPlayRule> Cards { get; }
        public IReadOnlyList<CombatStatus> StatusRules { get; }
        public UnitCopyCatalog? UnitCopyCatalog { get; }
        public BattlePlayRules(IReadOnlyList<RoomPlayRule> rooms, IReadOnlyList<CardPlayRule> cards, IReadOnlyList<CombatStatus>? statusRules = null,
            UnitCopyCatalog? unitCopyCatalog = null)
        { Rooms = Array.AsReadOnly(rooms.ToArray()); Cards = Array.AsReadOnly(cards.ToArray()); StatusRules = Array.AsReadOnly((statusRules ?? Array.Empty<CombatStatus>()).ToArray()); UnitCopyCatalog = unitCopyCatalog; }
        internal BattlePlayRules WithRooms(IReadOnlyList<RoomPlayRule> rooms) => new(rooms, Cards, StatusRules, UnitCopyCatalog);
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
        // Spawn-point index selected by effects that move an existing unit to a room location.
        public int TargetSpawnPointIndex { get; }
        public IReadOnlyList<CardRecursionSelection> RecursionSelections { get; }
        public PlayCardAction(int cardInstanceId, int roomIndex, int playerPosition = -1, int targetUnitId = 0,
            int activatorUnitId = 0, int targetSpawnPointIndex = -1,
            IReadOnlyList<CardRecursionSelection>? recursionSelections = null)
        { CardInstanceId = cardInstanceId; RoomIndex = roomIndex; PlayerPosition = playerPosition; TargetUnitId = targetUnitId;
            ActivatorUnitId = activatorUnitId; TargetSpawnPointIndex = targetSpawnPointIndex;
            RecursionSelections = Array.AsReadOnly((recursionSelections ?? Array.Empty<CardRecursionSelection>()).ToArray()); }
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
            if (action.ActivatorUnitId != 0)
            {
                if (source.SelectedRoom >= 0 && source.SelectedRoom != action.RoomIndex &&
                    source.PlayRules?.Rooms.Any(room => room.RoomIndex == action.RoomIndex) == true)
                {
                    BattleTurnResult selection = BattleTurnModel.SelectRoom(source, action.RoomIndex);
                    if (!selection.Supported) return Unsupported(selection.UnsupportedReason!);
                    source = selection.State!;
                }
                return UnitAbilityModel.Activate(source, action);
            }
            if (action.TargetSpawnPointIndex != -1)
                return Illegal("This card play does not take a destination spawn point.");
            if (source.PlayRules == null || source.Spawn.Train.Context == null)
                return Unsupported("Missing card/room play definitions or battle context.");
            if (source.ExternalInteractions.Count > 0 || source.Spawn.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", source.ExternalInteractions.Concat(source.Spawn.ExternalInteractions)));
            if (source.PlayRules.Cards.Select(card => card.DataId).Distinct().Count() != source.PlayRules.Cards.Count ||
                source.PlayRules.Rooms.Select(room => room.RoomIndex).Distinct().Count() != source.PlayRules.Rooms.Count)
                return Unsupported("Duplicate play definitions.");
            TrainCombatState train = source.Spawn.Train;
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
            CardToken? card = context.Cards.Hand.FirstOrDefault(item => item.InstanceId == action.CardInstanceId);
            if (card == null) return Illegal("The selected card instance is not in hand.");
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            CardPlayRule? originalRule = rule;
            string? uiRangeError = CardSpellModel.UnisolatedUiRangeReason(source);
            if (uiRangeError != null) return Unsupported(uiRangeError);
            if (rule == null) return Unsupported("Missing play definition for " + card.DataId);
            CardInstanceState? playingInstance = context.CardInstances?.FirstOrDefault(item => item.InstanceId == card.InstanceId);
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
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (rule.CostType != "Default" && rule.CostType != "ConsumeRemainingEnergy")
                return Unsupported("Unmodeled card cost type " + rule.CostType);
            if (rule.Cost < 0) return Unsupported("Negative card costs are not implemented.");
            int paidCost = rule.CostType == "ConsumeRemainingEnergy" ? source.Energy : rule.Cost;
            if (source.Energy < 0) return Unsupported("Negative decision energy.");
            if (source.Energy < paidCost) return Illegal("Insufficient energy.");
            RoomPlayRule? targetRule = source.PlayRules.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            RoomCombatState? target = train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            if (targetRule == null || target == null) return Illegal("The target room does not exist.");
            if (!targetRule.Enabled || targetRule.IsPyre && (rule.CanPlayInEngineRoom != true || rule.Effect == "SpawnMonster"))
                return Illegal("This card cannot be played in the target room.");
            CardActionEffect[] actionEffects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            if (CardSpellModel.RequiresUnitTarget(actionEffects))
            {
                CombatUnit? victim = target.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
                if (victim == null || victim.IsPyre || victim.Statuses.Any(status => status.Id == "untouchable"))
                    return Illegal("The card has no legal unit target in the selected room.");
                CardActionEffect? targeted = actionEffects.FirstOrDefault(effect => effect.Target == "DropTargetCharacter" && effect.Type != "HandUpgrade");
                if (targeted != null && (victim.Team == CombatTeam.Enemy && !targeted.AllowEnemy ||
                    victim.Team == CombatTeam.Player && !targeted.AllowPlayer)) return Illegal("The card excludes the target team.");
            }
            else if (action.TargetUnitId != 0) return Illegal("This card does not take a unit target.");
            string? membershipError = CardPileModel.ValidateMembership(context.Cards, source.OtherPiles, context.NextCardId);
            if (membershipError != null) return Unsupported(membershipError);
            if (source.OtherPiles.Select(pile => pile.Name).Distinct().Count() != source.OtherPiles.Count)
                return Unsupported("Duplicate card piles.");
            foreach (CardPileState pile in source.OtherPiles)
            {
                string? pileError = CardPileModel.Validate(pile);
                if (pileError != null) return Unsupported(pileError);
            }
            context = context.WithOtherPiles(source.OtherPiles).WithStatistics(context.Statistics?.WithPlayedCost(card.InstanceId, paidCost));
            CombatContext castingContext = context;
            context = context.WithAbilityActivator(context.LastAbilityActivatorUnitId.HasValue ? 0 : (int?)null);
            context = context.WithQueryFrame(context.QueryFrame?.With(energy: source.Energy - paidCost));
            // Native records the payment on the card before any queued effect runs.
            if (playingInstance != null) context = context.WithCard(playingInstance.WithPlayedCost(paidCost));
            CardPileState[] piles = source.OtherPiles.ToArray();
            // A naturally played card remains owned in the discard buffer during queued effects.
            // Scaling queries refresh membership here, before the final destination is assigned.
            piles = piles.Select(pile => pile.Name == "DiscardBuffer" && !pile.Cards.Any(item => item.InstanceId == card.InstanceId)
                ? CardPileModel.Add(pile, card) : pile).ToArray();
            context = context.WithOtherPiles(piles);
            // Native direct play removes the card from hand before queued effects execute.
            context = new CombatContext(new CardCycleState(context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToArray(),
                context.Cards.Draw, context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId),
                context.BattleRng, context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics, context.CardInstances,
                context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources);
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
            bool effectsApplied = false;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pendingSpellCallbacks = Array.Empty<RoomCombatModel.QueuedCharacterTrigger>();
            IReadOnlyList<CombatUnit> retainedSpellUnits = Array.Empty<CombatUnit>();
            int nextUnitId = source.Spawn.NextUnitId;
            string? identityError = UnitIdentityModel.Validate(context, train.Rooms.SelectMany(room => room.Units), nextUnitId);
            if (identityError != null) return Unsupported(identityError);
            if (rule.Effect == "SpawnMonster")
            {
                if (action.TargetUnitId != 0) return Illegal("A summon takes a spawn position, not a unit target.");
                CombatUnit? template = rule.SpawnUnit;
                if (template == null && !(rule.Summon?.Pool.Count > 0 && rule.Summon.NativeBaseSize.HasValue) ||
                    template?.IsPyre == true || template?.Size < 0 || rule.Destination != "Standby")
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
                        template.IsSpawning.HasValue ? true : (bool?)null, template.SacrificeCardId, template.DeathState, bumpRules: template.BumpRules);
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
                    spawnTriggers = RoomCombatModel.ApplySpawnTriggers(entered, spawned.Id, fromCard: true, startingApplications: initialStatuses);
                    if (!spawnTriggers.Supported) return Unsupported(spawnTriggers.UnsupportedReason!);
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
                effectsApplied = true;
            }
            else if (rule.Effect == "Spell" || rule.Effect == "Equipment")
            {
                if (action.PlayerPosition != -1) return Illegal("A spell does not take a summon position.");
                TrainCombatState spellInput = CardSpellModel.WithContext(train, context);
                SpellCastCheck cast = CardSpellModel.TestPlay(CardSpellModel.WithContext(train, castingContext), action.RoomIndex,
                    rule.Effects, action.TargetUnitId, source.PlayRules, sourceCardId: card.InstanceId);
                if (!cast.Supported) return Unsupported(cast.UnsupportedReason!);
                if (!cast.CanPlay) return Illegal("Every effect failed its cast test or a required effect failed.");
                spellInput = CardSpellModel.WithContext(spellInput, context.WithBattleRng(cast.BattleRngAfterTests!.Value));
                TrainSpellResult result = CardSpellModel.ApplyForCardPlay(spellInput, action.RoomIndex, rule.Effects, action.TargetUnitId,
                    card.InstanceId, source.PlayRules, piles, recursionSelections: action.RecursionSelections
                        .Where(selection => selection.EffectIndex < rule.Effects.Count).ToArray());
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                piles = result.OtherPiles?.ToArray() ?? piles;
                train = result.State!; context = train.Context!; outcome = result.Outcome; effectsApplied = true;
                outputPlayRules = result.PlayRules ?? outputPlayRules;
                pendingSpellCallbacks = result.PendingCallbacks;
                retainedSpellUnits = result.RetainedUnits;
            }
            else if (rule.Effect != "Null") return Unsupported("Unimplemented card effect " + rule.Effect);
            else if (action.PlayerPosition != -1) return Illegal("A no-target card does not take a summon position.");

            bool terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            if (terminal && context.Statistics != null && context.Statistics.DeckCards == null)
                return Unsupported("Terminal card resolution requires permanent deck membership.");
            // Native marks the card played before Rally, while it still belongs to
            // the resolving buffer and retains its paid cost.
            BattleStatistics? playedStatistics = terminal ? context.Statistics?.RefreshDeckAfterCardTerminal() : context.Statistics;
            playedStatistics = terminal && playedStatistics != null && !playedStatistics.TrackedCards.Contains(card.InstanceId)
                ? playedStatistics.RecordPlayedCard(card.InstanceId) : playedStatistics?.Increment(card.InstanceId, "TimesPlayed");
            context = context.WithStatistics(playedStatistics);
            train = CardSpellModel.WithContext(train, context);
            if (rule.OnCastEffects.Count > 0)
            {
                CardRecursionSelection[] onCastSelections = action.RecursionSelections
                    .Where(selection => selection.EffectIndex >= rule.Effects.Count)
                    .Select(selection => new CardRecursionSelection(selection.EffectIndex - rule.Effects.Count, selection.CardInstanceIds)).ToArray();
                TrainSpellResult result = CardSpellModel.ApplyForCardPlay(train, action.RoomIndex, rule.OnCastEffects,
                    action.TargetUnitId, card.InstanceId, source.PlayRules, piles, recursionSelections: onCastSelections,
                    onCastEffects: true);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
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
            RelicCardPlayMagicPowerResult cardPlayMagicPower = RelicCardPlayMagicPowerModel.Apply(
                context, card.InstanceId, source.PlayRules, source.Spawn.Turn);
            if (!cardPlayMagicPower.Supported) return Unsupported(cardPlayMagicPower.UnsupportedReason!);
            context = cardPlayMagicPower.Context;
            train = CardSpellModel.WithContext(train, context);
            TrainCombatResult delayedStatusRemovals = TrainCombatModel.ProcessDelayedStatusRemovalsAfterCardPlayed(train);
            if (!delayedStatusRemovals.Supported) return Unsupported(delayedStatusRemovals.UnsupportedReason!);
            train = delayedStatusRemovals.State!; context = train.Context!;
            if (delayedStatusRemovals.Outcome != RoomOutcome.Exchanged) outcome = delayedStatusRemovals.Outcome;
            List<CardToken> hand = context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToList();
            List<CardToken> discard = context.Cards.Discard.ToList();
            if (effectsApplied && !(terminal && context.OtherPiles != null))
            {
                var alive = new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                foreach (CombatUnit dead in source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.SpawnerCardId > 0 && !alive.Contains(unit.Id)))
                {
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
                    if (standby == null || exhausted == null || deadCard == null) return Unsupported("Missing dead unit spawner card routing.");
                    piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(pile, dead.SpawnerCardId) : pile == exhausted
                            ? new CardPileState(pile.Name, pile.Cards.Concat(new[] { deadCard }).ToArray()) : pile).ToArray();
                }
            }
            if (terminal) piles = piles.Select(CardPileModel.Clear).ToArray();
            CardInstanceState? discardInstance = context.FindCard(card.InstanceId) ?? playingInstance;
            bool consumedOnPlay = discardInstance != null && CardExhaustModel.ShouldExhaustOnPlayedDiscard(discardInstance);
            bool ephemeralPurgeCallbacks = rule.Destination == "Purged" && discardInstance?.MaskDescriptor != null &&
                CardBranchMaskModel.Resolve(discardInstance).Traits.Contains("CardTraitEphemeral", StringComparer.Ordinal);
            if (consumedOnPlay && rule.HandConsumeInteractions?.Count > 0)
                return Unsupported(string.Join("; ", rule.HandConsumeInteractions));
            if (consumedOnPlay && rule.Effect == "SpawnMonster" &&
                !piles.Any(pile => pile.Name == "Standby" && pile.UnitConditions != null))
                return Unsupported("Consume summons require captured native unit standby conditions.");
            // Native DiscardCard/PurgeCard remove a naturally played card's retained buffer reference.
            piles = piles.Select(pile => pile.Name == "DiscardBuffer" ? CardPileModel.Remove(pile, card.InstanceId) : pile).ToArray();
            if (terminal && rule.Destination != "Discard" && !(rule.Destination == "Standby" && rule.Effect == "SpawnMonster"))
                return Unsupported("Terminal routing for this card effect/destination is not validated.");
            if (rule.Destination == "Discard") discard.Add(card);
            else
            {
                CardPileState? destination = piles.FirstOrDefault(pile => pile.Name == rule.Destination);
                if (destination == null || (rule.Destination != "Standby" && rule.Destination != "Purged" && rule.Destination != "Exhausted"))
                    return Unsupported("Unimplemented card destination " + rule.Destination);
                piles = piles.Select(pile =>
                {
                    if (pile != destination) return pile;
                    CardPileState added = CardPileModel.Add(pile, card);
                    // Native Ephemeral calls PurgeCard from both OnCardPlayed and OnCardDiscarded.
                    return ephemeralPurgeCallbacks ? CardPileModel.Add(added, card) : added;
                }).ToArray();
            }
            if (rule.Effect == "Equipment")
            {
                if (rule.Equipment == null || action.TargetUnitId <= 0) return Unsupported("Missing equipment standby host.");
                piles = piles.Select(pile => pile.Name == "Standby" ? CardPileModel.BindEquipment(pile,
                    new EquipmentStandbyCondition(card.InstanceId, action.TargetUnitId, rule.Equipment.ReturnToHand)) : pile).ToArray();
            }
            if (rule.Effect == "SpawnMonster" && spawnedId.HasValue && piles.Any(pile => pile.Name == "Standby" && pile.UnitConditions != null))
                piles = piles.Select(pile => pile.Name == "Standby" ? CardPileModel.BindUnit(pile,
                    new UnitStandbyCondition(card.InstanceId, spawnedId.Value,
                        !train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == spawnedId.Value))) : pile).ToArray();
            bool summonRemoved = spawnedId.HasValue && !train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == spawnedId.Value);
            bool summonedEndless = summonRemoved && rule.SpawnUnit?.Statuses.Any(status => status.Id == "endless" && status.Stacks > 0) == true;
            if (summonRemoved && !terminal && !consumedOnPlay)
            {
                if (!summonedEndless)
                {
                    CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                    CardPileState? exhausted = piles.FirstOrDefault(pile => pile.Name == "Exhausted");
                    if (standby == null || exhausted == null) return Unsupported("Missing removed summon card routing.");
                    piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(pile, card.InstanceId) :
                        pile == exhausted ? CardPileModel.Add(pile, card) : pile).ToArray();
                }
            }
            if (terminal && context.Statistics != null && context.Statistics.DeckCards == null)
                return Unsupported("Terminal card resolution requires permanent deck membership.");
            // Discard restores the resolving card after terminal clearing.
            BattleStatistics? statistics = context.Statistics;
            if (terminal) statistics = statistics?.RefreshOwnedCards(new[] { card.InstanceId });
            statistics = statistics?.Increment(card.InstanceId, "TimesDiscarded").WithPlayedCost(card.InstanceId, null);
            if (!terminal && rule.Destination == "Exhausted") statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            if (summonRemoved && !summonedEndless && !terminal && !consumedOnPlay) statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            if (rule.Destination == "Purged") statistics = statistics?.RemoveDeckCard(card.InstanceId);
            context = context.AfterCardEffects();
            context = new CombatContext(new CardCycleState(hand, context.Cards.Draw, discard, context.Cards.Rng,
                context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId), context.BattleRng,
                context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, statistics,
                terminal ? playingInstance == null ? context.CardInstances : new[] { (context.FindCard(card.InstanceId) ?? playingInstance).OnDiscard(true, paidCost) } :
                context.CardInstances?.Select(instance => instance.InstanceId == card.InstanceId
                    ? instance.OnDiscard(true, paidCost) : instance).ToArray(), context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades,
                context.OtherPiles == null ? null : piles, context.QueryFrame?.With(runningCombat: !terminal), context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources);
            if (summonedEndless && !terminal && !consumedOnPlay)
                context = UnitStandbyModel.ReturnEndless(context, card.InstanceId);
            if (consumedOnPlay)
            {
                if (context.OtherPiles == null || !context.OtherPiles.Any(pile => pile.Name == "Standby") ||
                    !context.OtherPiles.Any(pile => pile.Name == "Exhausted")) return Unsupported("Consume requires captured standby and exhausted piles.");
                context = CardExhaustModel.ReturnReady(CardExhaustModel.MoveConsumedToStandby(context, card));
            }
            RoomCombatState[] rooms = train.Rooms.Select(room =>
            {
                return new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                    context, room.Preview, room.ActiveCompanionBossIds);
            }).ToArray();
            context = UnitStandbyModel.ReturnReady(context);
            context = EquipmentModel.ReturnUnattached(context, new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id)));
            piles = context.OtherPiles?.ToArray() ?? piles;
            rooms = rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            var living = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            train = new TrainCombatState(rooms, train.Movement.Where(rule => living.Contains(rule.UnitId)).ToArray(), train.EnemySlotsPerRoom, context);
            // CardManager.OnCardPlayed refreshes auras after discard/cost clearing,
            // then drains their callbacks before the UI combat preview can start.
            TrainCombatResult cardCompleted = EnchantmentWorldModel.UpdateAll(train);
            if (!cardCompleted.Supported) return Unsupported(cardCompleted.UnsupportedReason!);
            train = cardCompleted.State!; context = train.Context!;
            if (source.BattlePreviewEnabled && !terminal)
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!; context = train.Context!;
            }
            EnemySpawnState spawn = source.Spawn;
            if (source.CanonicalPhysicalReferences) train = TrainCombatModel.CompleteFrameRemovals(train);
            else if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
            spawn = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
                train.Context?.NextUnitId ?? nextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences);
            if (context.RoomCapacities != null) outputPlayRules = outputPlayRules.WithCapacities(context.RoomCapacities);
            return new BattleActionResult(new BattleTurnState(spawn, context.EnergyState == null ? source.Energy - paidCost : context.QueryFrame!.Energy!.Value, source.EnergyPerTurn,
                source.DrawPerTurn, context.QueryFrame?.ForgePoints ?? source.ForgePoints, source.DragonsHoard, source.MoonPhase,
                source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? context.BattleRng : stream.Name == "CardDraw" ? context.Cards.Rng : stream.State)).ToArray(),
                piles, source.ExternalInteractions, outputPlayRules, source.BattlePreviewEnabled, source.UiRngIsolated,
                source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences,
                source.SelectedRoom < 0 ? source.SelectedRoom : action.RoomIndex, source.MoonPhaseTrackingActive), outcome: outcome);
        }

        // Unsupported attempts are kept separate from legal actions so callers can detect an incomplete search frontier.
        public static BattleActionEnumeration EnumerateActions(BattleTurnState source)
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
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
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
            BattleActionEnumeration abilities = UnitAbilityModel.EnumerateActivations(source);
            actions.AddRange(abilities.SupportedCandidates);
            unsupported.AddRange(abilities.UnsupportedAttempts);
            return new BattleActionEnumeration(actions, unsupported);

            void Attempt(PlayCardAction action)
            {
                BattleActionResult result = PlayCard(source, action);
                if (result.Supported) actions.Add(new SupportedActionCandidate(action, result));
                else if (result.Rejection == ActionRejection.Unsupported)
                    unsupported.Add(new UnsupportedActionAttempt(action, result.Reason ?? "Unsupported card action."));
            }
        }

        private static IEnumerable<PlayCardAction> EnumerateRecursionChoices(BattleTurnState source,
            PlayCardAction action, CardPlayRule rule)
        {
            var choiceEffects = rule.Effects.Concat(rule.OnCastEffects).Select((effect, index) => (effect, index))
                .Where(item => item.effect.Type == "Recursion" &&
                    item.effect.Recursion?.SelectionMode is "ChooseToHand" or "ChooseToDeck").ToArray();
            if (choiceEffects.Length == 0)
            {
                yield return action;
                yield break;
            }

            var selections = new List<CardRecursionSelection>();
            var usedCards = new HashSet<int>();
            foreach (PlayCardAction branch in Expand(0)) yield return branch;

            IEnumerable<PlayCardAction> Expand(int choiceIndex)
            {
                if (choiceIndex == choiceEffects.Length)
                {
                    yield return new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                        action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex, selections);
                    yield break;
                }

                (CardActionEffect effect, int effectIndex) = choiceEffects[choiceIndex];
                IReadOnlyList<int> eligible = CardRecursionModel.EligibleCards(source.Spawn.Train.Context!.Cards,
                    source.Spawn.Train.Context.OtherPiles, effect, source.PlayRules, action.CardInstanceId,
                    out string? error, source.Spawn.Train.Context.CardRegistry);
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

        public static IReadOnlyList<PlayCardAction> EnumerateSupportedPlays(BattleTurnState source) => EnumerateActions(source).SupportedActions;

        public static PlayCardAction? ChooseUnitAndJunkPlay(BattleTurnState source)
            => ChoosePlay(source, false);
        public static PlayCardAction? ChooseUnitSpellAndJunkPlay(BattleTurnState source)
            => ChoosePlay(source, true);
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
        private static PlayCardAction? ChoosePlay(BattleTurnState source, bool spells)
        {
            if (source.PlayRules == null) return null;
            IReadOnlyList<PlayCardAction> plays = EnumerateSupportedPlays(source);
            int roomCount = source.PlayRules.Rooms.Count(room => !room.IsPyre);
            if (roomCount == 0) return null;
            // A deterministic verification policy: rotate floors each turn, summon before clearing junk.
            // It is intentionally simple; a search may choose any enumerated action instead.
            foreach (string effect in spells ? new[] { "SpawnMonster", "Equipment", "Spell", "Null" } : new[] { "SpawnMonster", "Null" })
            foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule?.Effect != effect) continue;
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
                            PlayCardAction? roomSpell = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
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
                            PlayCardAction? cast = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                                action.RoomIndex == roomIndex && action.TargetUnitId == target.Id);
                            if (cast != null) return cast;
                        }
                        continue;
                    }
                    int position = effect == "SpawnMonster" ? (source.Spawn.Turn + card.InstanceId) % (count + 1) : -1;
                    PlayCardAction? choice = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
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
