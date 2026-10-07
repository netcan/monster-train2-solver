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
        public CardEffectTests(bool shouldTest, bool failToCast, bool cancelSubsequent, bool strictTargets, bool? canPlayAfterBossDead = null)
        { ShouldTest = shouldTest; FailToCast = failToCast; CancelSubsequent = cancelSubsequent; StrictTargets = strictTargets;
            CanPlayAfterBossDead = canPlayAfterBossDead; }
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
        public CardActionEffect(string type, string target, int value, bool allowEnemy, bool allowPlayer, IReadOnlyList<CombatStatus> statuses,
            CardUpgradeModifier? upgrade = null, string lifetime = "", CardEffectTests? tests = null, CardEffectRange? range = null, CardTargetFilters? filters = null,
            CardGenerationRule? generation = null)
        { Type = type; Target = target; Value = value; AllowEnemy = allowEnemy; AllowPlayer = allowPlayer; Statuses = Array.AsReadOnly(statuses.ToArray());
            Upgrade = upgrade; Lifetime = lifetime; Tests = tests; Range = range; Filters = filters; Generation = generation; }
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
    }

    // Rules copied from definitions at the root, including reachable generated card definitions.
    // Unimplemented traits/effects remain attached to the card; they never silently become no-ops.
    public sealed class CardPlayRule
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public int Cost { get; }
        public string Effect { get; }
        public string Destination { get; }
        public CombatUnit? SpawnUnit { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public IReadOnlyList<CardActionEffect> Effects { get; }
        public IReadOnlyList<string>? UpgradeInteractions { get; }
        public IReadOnlyList<string>? HandDiscardInteractions { get; }
        public IReadOnlyList<string>? HandConsumeInteractions { get; }
        public CardPlayRule(string dataId, string assetKey, int cost, string effect, string destination,
            CombatUnit? spawnUnit, IReadOnlyList<string> externalInteractions, IReadOnlyList<CardActionEffect>? effects = null,
            IReadOnlyList<string>? upgradeInteractions = null, IReadOnlyList<string>? handDiscardInteractions = null,
            IReadOnlyList<string>? handConsumeInteractions = null)
        {
            DataId = dataId; AssetKey = assetKey; Cost = cost; Effect = effect; Destination = destination;
            SpawnUnit = spawnUnit; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            Effects = Array.AsReadOnly((effects ?? Array.Empty<CardActionEffect>()).ToArray());
            UpgradeInteractions = upgradeInteractions == null ? null : Array.AsReadOnly(upgradeInteractions.ToArray());
            HandDiscardInteractions = handDiscardInteractions == null ? null : Array.AsReadOnly(handDiscardInteractions.ToArray());
            HandConsumeInteractions = handConsumeInteractions == null ? null : Array.AsReadOnly(handConsumeInteractions.ToArray());
        }
    }

    public sealed class BattlePlayRules
    {
        public IReadOnlyList<RoomPlayRule> Rooms { get; }
        public IReadOnlyList<CardPlayRule> Cards { get; }
        public IReadOnlyList<CombatStatus> StatusRules { get; }
        public BattlePlayRules(IReadOnlyList<RoomPlayRule> rooms, IReadOnlyList<CardPlayRule> cards, IReadOnlyList<CombatStatus>? statusRules = null)
        { Rooms = Array.AsReadOnly(rooms.ToArray()); Cards = Array.AsReadOnly(cards.ToArray()); StatusRules = Array.AsReadOnly((statusRules ?? Array.Empty<CombatStatus>()).ToArray()); }
    }

    public sealed class PlayCardAction
    {
        public int CardInstanceId { get; }
        public int RoomIndex { get; }
        // -1 chooses the native first empty slot; 0..count inserts at the selected player position.
        public int PlayerPosition { get; }
        public int TargetUnitId { get; }
        public PlayCardAction(int cardInstanceId, int roomIndex, int playerPosition = -1, int targetUnitId = 0)
        { CardInstanceId = cardInstanceId; RoomIndex = roomIndex; PlayerPosition = playerPosition; TargetUnitId = targetUnitId; }
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

    public static class BattleActionModel
    {
        public static BattleActionResult PlayCard(BattleTurnState source, PlayCardAction action)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null)
                return Unsupported("Missing card/room play definitions or battle context.");
            if (source.ExternalInteractions.Count > 0 || source.Spawn.ExternalInteractions.Count > 0)
                return Unsupported(string.Join("; ", source.ExternalInteractions.Concat(source.Spawn.ExternalInteractions)));
            if (source.PlayRules.Cards.Select(card => card.DataId).Distinct().Count() != source.PlayRules.Cards.Count ||
                source.PlayRules.Rooms.Select(room => room.RoomIndex).Distinct().Count() != source.PlayRules.Rooms.Count)
                return Unsupported("Duplicate play definitions.");
            TrainCombatState train = source.Spawn.Train;
            string? trainValidation = TrainCombatModel.Validate(train);
            if (trainValidation != null) return Unsupported(trainValidation);
            if (source.Spawn.NextUnitId <= 0 || train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id <= 0 || unit.Id >= source.Spawn.NextUnitId || unit.Size < 0))
                return Unsupported("Invalid unit identity allocation or size.");
            CombatContext context = train.Context;
            string? frameError = StatisticQueryFrame.ValidateDecision(source);
            if (frameError != null) return Unsupported(frameError);
            CardToken? card = context.Cards.Hand.FirstOrDefault(item => item.InstanceId == action.CardInstanceId);
            if (card == null) return Illegal("The selected card instance is not in hand.");
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            string? uiRangeError = CardSpellModel.UnisolatedUiRangeReason(source);
            if (uiRangeError != null) return Unsupported(uiRangeError);
            if (rule == null) return Unsupported("Missing play definition for " + card.DataId);
            CardInstanceState? playingInstance = context.CardInstances?.FirstOrDefault(item => item.InstanceId == card.InstanceId);
            if (context.CardInstances != null)
            {
                if (playingInstance == null) return Unsupported("Missing card instance modifiers.");
                rule = CardModifierModel.Resolve(rule, playingInstance);
            }
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (rule.Cost < 0) return Unsupported("Variable or negative card costs are not implemented.");
            if (source.Energy < rule.Cost) return Illegal("Insufficient energy.");
            RoomPlayRule? targetRule = source.PlayRules.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            RoomCombatState? target = train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            if (targetRule == null || target == null) return Illegal("The target room does not exist.");
            if (!targetRule.Enabled || targetRule.IsPyre) return Illegal("This card cannot be played in the target room.");
            string? membershipError = CardPileModel.ValidateMembership(context.Cards, source.OtherPiles, context.NextCardId);
            if (membershipError != null) return Unsupported(membershipError);
            if (source.OtherPiles.Select(pile => pile.Name).Distinct().Count() != source.OtherPiles.Count)
                return Unsupported("Duplicate card piles.");
            foreach (CardPileState pile in source.OtherPiles)
            {
                string? pileError = CardPileModel.Validate(pile);
                if (pileError != null) return Unsupported(pileError);
            }
            context = context.WithOtherPiles(source.OtherPiles).WithStatistics(context.Statistics?.WithPlayedCost(card.InstanceId, rule.Cost));
            CombatContext castingContext = context;
            context = context.WithQueryFrame(context.QueryFrame?.With(energy: source.Energy - rule.Cost));
            CardPileState[] piles = source.OtherPiles.ToArray();
            // A naturally played card remains owned in the discard buffer during queued effects.
            // Scaling queries refresh membership here, before the final destination is assigned.
            piles = piles.Select(pile => pile.Name == "DiscardBuffer" && !pile.Cards.Any(item => item.InstanceId == card.InstanceId)
                ? CardPileModel.Add(pile, card) : pile).ToArray();
            context = context.WithOtherPiles(piles);
            // Native direct play removes the card from hand before queued effects execute.
            context = new CombatContext(new CardCycleState(context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToArray(),
                context.Cards.Draw, context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier, context.Cards.ExternalInteractions),
                context.BattleRng, context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics, context.CardInstances,
                context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated);
            target = new RoomCombatState(target.RoomIndex, target.Deployment, target.Units, target.ExternalInteractions, context, target.Preview);
            CombatUnit[] players = target.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            int position = action.PlayerPosition == -1 ? players.Length : action.PlayerPosition;
            int? spawnedId = null;
            bool effectsApplied = false;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            int nextUnitId = source.Spawn.NextUnitId;
            if (rule.Effect == "SpawnMonster")
            {
                if (action.TargetUnitId != 0) return Illegal("A summon takes a spawn position, not a unit target.");
                CombatUnit? template = rule.SpawnUnit;
                if (template == null || template.IsPyre || template.Size < 0 || rule.Destination != "Standby")
                    return Unsupported("Invalid unit spawn definition.");
                string? validation = RoomCombatModel.Validate(new RoomCombatState(target.RoomIndex, target.Deployment,
                    new[] { template }, Array.Empty<string>(), context));
                if (validation != null) return Unsupported(validation);
                if (targetRule.SummonBlocked || players.Length >= targetRule.PlayerSlots ||
                    players.Sum(unit => (long)unit.Size) + template.Size > targetRule.PlayerCapacity)
                    return Illegal("The room cannot accept this unit's size or another spawn slot.");
                if (position < 0 || position > players.Length) return Illegal("Invalid summon position.");
                if (nextUnitId <= 0 || train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id >= nextUnitId))
                    return Unsupported("Invalid unit identity allocation.");
                var spawned = new CombatUnit(nextUnitId++, template.AssetKey, CombatTeam.Player, template.BaseAttack,
                    template.Health, template.MaxHealth, template.CanAttack, false, false, template.Statuses,
                    template.Triggers, card.InstanceId, template.Size, template.StatusImmunities, template.Subtypes, template.Modifiers, template.IsBoss, template.LastAttackerId);
                context = context.WithStatistics(context.Statistics?.Spawn(action.RoomIndex, template.Subtypes));
                spawnedId = spawned.Id;
                var nextPlayers = players.ToList(); nextPlayers.Insert(position, spawned);
                var entered = new RoomCombatState(target.RoomIndex, target.Deployment,
                    target.Units.Where(unit => unit.Team == CombatTeam.Enemy).Concat(nextPlayers).ToArray(), target.ExternalInteractions, context, target.Preview);
                RoomCombatResult spawnTriggers = RoomCombatModel.ApplySpawnTriggers(entered, spawned.Id, fromCard: true);
                if (!spawnTriggers.Supported) return Unsupported(spawnTriggers.UnsupportedReason!);
                context = spawnTriggers.State!.Context!; outcome = spawnTriggers.Outcome;
                piles = context.OtherPiles?.ToArray() ?? piles;
                RoomCombatState[] enteredRooms = train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.RoomIndex == target.RoomIndex ? spawnTriggers.State.Units : room.Units, room.ExternalInteractions, context, room.Preview)).ToArray();
                var enteredIds = new HashSet<int>(enteredRooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                train = new TrainCombatState(enteredRooms, train.Movement.Where(item => enteredIds.Contains(item.UnitId)).ToArray(), train.EnemySlotsPerRoom, context);
                effectsApplied = true;
            }
            else if (rule.Effect == "Spell")
            {
                if (action.PlayerPosition != -1) return Illegal("A spell does not take a summon position.");
                if (CardSpellModel.RequiresUnitTarget(rule.Effects))
                {
                    CombatUnit? victim = target.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
                    if (victim == null || victim.IsPyre || victim.Statuses.Any(status => status.Id == "untouchable"))
                        return Illegal("The spell has no legal unit target in the selected room.");
                    CardActionEffect? targeted = rule.Effects.FirstOrDefault(effect => effect.Target == "DropTargetCharacter" && effect.Type != "HandUpgrade");
                    if (targeted != null && (victim.Team == CombatTeam.Enemy && !targeted.AllowEnemy ||
                        victim.Team == CombatTeam.Player && !targeted.AllowPlayer)) return Illegal("The spell excludes the target team.");
                }
                else if (action.TargetUnitId != 0) return Illegal("A room or hand spell does not take a unit target.");
                TrainCombatState spellInput = CardSpellModel.WithContext(train, context);
                SpellCastCheck cast = CardSpellModel.TestPlay(CardSpellModel.WithContext(train, castingContext), action.RoomIndex,
                    rule.Effects, action.TargetUnitId, source.PlayRules);
                if (!cast.Supported) return Unsupported(cast.UnsupportedReason!);
                if (!cast.CanPlay) return Illegal("Every effect failed its cast test or a required effect failed.");
                spellInput = CardSpellModel.WithContext(spellInput, context.WithBattleRng(cast.BattleRngAfterTests!.Value));
                TrainSpellResult result = CardSpellModel.Apply(spellInput, action.RoomIndex, rule.Effects, action.TargetUnitId,
                    card.InstanceId, source.PlayRules, piles);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                piles = result.OtherPiles?.ToArray() ?? piles;
                train = result.State!; context = train.Context!; outcome = result.Outcome; effectsApplied = true;
            }
            else if (rule.Effect != "Null") return Unsupported("Unimplemented card effect " + rule.Effect);
            else if (action.PlayerPosition != -1 || action.TargetUnitId != 0) return Illegal("A no-target card does not take a target or position.");

            List<CardToken> hand = context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToList();
            List<CardToken> discard = context.Cards.Discard.ToList();
            bool terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            if (effectsApplied && !(terminal && context.OtherPiles != null))
            {
                var alive = new HashSet<int>(train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
                foreach (CombatUnit dead in source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.SpawnerCardId > 0 && !alive.Contains(unit.Id)))
                {
                    CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                    CardPileState? exhausted = piles.FirstOrDefault(pile => pile.Name == "Exhausted");
                    CardToken? deadCard = standby?.Cards.FirstOrDefault(item => item.InstanceId == dead.SpawnerCardId);
                    if (deadCard == null && exhausted?.Cards.Any(item => item.InstanceId == dead.SpawnerCardId) == true) continue;
                    if (standby == null || exhausted == null || deadCard == null) return Unsupported("Missing dead unit spawner card routing.");
                    piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(pile, dead.SpawnerCardId) : pile == exhausted
                            ? new CardPileState(pile.Name, pile.Cards.Concat(new[] { deadCard }).ToArray()) : pile).ToArray();
                }
            }
            if (terminal) piles = piles.Select(CardPileModel.Clear).ToArray();
            // Native DiscardCard/PurgeCard remove a naturally played card's retained buffer reference.
            piles = piles.Select(pile => pile.Name == "DiscardBuffer" ? CardPileModel.Remove(pile, card.InstanceId) : pile).ToArray();
            if (terminal && rule.Destination != "Discard") return Unsupported("Terminal played-card routing outside discard is not validated.");
            if (rule.Destination == "Discard") discard.Add(card);
            else
            {
                CardPileState? destination = piles.FirstOrDefault(pile => pile.Name == rule.Destination);
                if (destination == null || (rule.Destination != "Standby" && rule.Destination != "Purged" && rule.Destination != "Exhausted"))
                    return Unsupported("Unimplemented card destination " + rule.Destination);
                piles = piles.Select(pile => pile == destination ? CardPileModel.Add(pile, card) : pile).ToArray();
            }
            bool summonRemoved = spawnedId.HasValue && !train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == spawnedId.Value);
            if (summonRemoved && !terminal)
            {
                CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                CardPileState? exhausted = piles.FirstOrDefault(pile => pile.Name == "Exhausted");
                if (standby == null || exhausted == null) return Unsupported("Missing removed summon card routing.");
                piles = piles.Select(pile => pile == standby ? CardPileModel.Remove(pile, card.InstanceId) :
                    pile == exhausted ? CardPileModel.Add(pile, card) : pile).ToArray();
            }
            if (terminal && context.Statistics != null && context.Statistics.DeckCards == null)
                return Unsupported("Terminal card resolution requires permanent deck membership.");
            // After ClearCards the played callback refreshes statistics from the permanent deck.
            // Discard then restores the resolving card, and its statistic refresh keeps only that card.
            BattleStatistics? statistics = terminal ? context.Statistics?.RefreshDeckAfterCardTerminal() : context.Statistics;
            // A generated spell absent from the fallback deck still enters played history;
            // native IncrementStat cannot increment its missing dictionary entry.
            statistics = terminal && statistics != null && !statistics.TrackedCards.Contains(card.InstanceId)
                ? statistics.RecordPlayedCard(card.InstanceId) : statistics?.Increment(card.InstanceId, "TimesPlayed");
            if (terminal) statistics = statistics?.RefreshOwnedCards(new[] { card.InstanceId });
            statistics = statistics?.Increment(card.InstanceId, "TimesDiscarded").WithPlayedCost(card.InstanceId, null);
            if (!terminal && rule.Destination == "Exhausted") statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            if (summonRemoved && !terminal) statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            context = context.AfterCardEffects();
            context = new CombatContext(new CardCycleState(hand, context.Cards.Draw, discard, context.Cards.Rng,
                context.Cards.DrawModifier, context.Cards.ExternalInteractions), context.BattleRng,
                context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, statistics,
                terminal ? playingInstance == null ? context.CardInstances : new[] { (context.FindCard(card.InstanceId) ?? playingInstance).OnDiscard(true, rule.Cost) } :
                context.CardInstances?.Select(instance => instance.InstanceId == card.InstanceId
                    ? instance.OnDiscard(true, rule.Cost) : instance).ToArray(), context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades,
                context.OtherPiles == null ? null : piles, context.QueryFrame?.With(runningCombat: !terminal), context.KillCamActivated);
            RoomCombatState[] rooms = train.Rooms.Select(room =>
            {
                return new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions, context, room.Preview);
            }).ToArray();
            var living = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            train = new TrainCombatState(rooms, train.Movement.Where(rule => living.Contains(rule.UnitId)).ToArray(), train.EnemySlotsPerRoom, context);
            if (source.BattlePreviewEnabled && !terminal)
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!; context = train.Context!;
            }
            EnemySpawnState spawn = source.Spawn;
            spawn = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
                nextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions);
            return new BattleActionResult(new BattleTurnState(spawn, source.Energy - rule.Cost, source.EnergyPerTurn,
                source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase,
                source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? context.BattleRng : stream.Name == "CardDraw" ? context.Cards.Rng : stream.State)).ToArray(),
                piles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated), outcome: outcome);
        }

        // Enumerates the implemented legal actions. Unsupported hand cards remain visible to the caller.
        // This is not a completeness certificate for the full game's action space.
        public static IReadOnlyList<PlayCardAction> EnumerateSupportedPlays(BattleTurnState source)
        {
            var actions = new List<PlayCardAction>();
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return actions.AsReadOnly();
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
            foreach (RoomPlayRule room in source.PlayRules.Rooms)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule?.Effect == "Spell" && CardSpellModel.RequiresUnitTarget(rule.Effects))
                {
                    foreach (CombatUnit target in source.Spawn.Train.Rooms.First(item => item.RoomIndex == room.RoomIndex).Units)
                    {
                        var action = new PlayCardAction(card.InstanceId, room.RoomIndex, targetUnitId: target.Id);
                        if (PlayCard(source, action).Supported) actions.Add(action);
                    }
                    continue;
                }
                int players = source.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == room.RoomIndex)?
                    .Units.Count(unit => unit.Team == CombatTeam.Player) ?? 0;
                int first = rule?.Effect == "SpawnMonster" ? 0 : -1;
                int last = rule?.Effect == "SpawnMonster" ? players : -1;
                for (int position = first; position <= last; position++)
                {
                    var action = new PlayCardAction(card.InstanceId, room.RoomIndex, position);
                    if (PlayCard(source, action).Supported) actions.Add(action);
                }
            }
            return actions.AsReadOnly();
        }

        public static PlayCardAction? ChooseUnitAndJunkPlay(BattleTurnState source)
            => ChoosePlay(source, false);
        public static PlayCardAction? ChooseUnitSpellAndJunkPlay(BattleTurnState source)
            => ChoosePlay(source, true);
        private static PlayCardAction? ChoosePlay(BattleTurnState source, bool spells)
        {
            if (source.PlayRules == null) return null;
            IReadOnlyList<PlayCardAction> plays = EnumerateSupportedPlays(source);
            int roomCount = source.PlayRules.Rooms.Count(room => !room.IsPyre);
            if (roomCount == 0) return null;
            // A deterministic verification policy: rotate floors each turn, summon before clearing junk.
            // It is intentionally simple; a search may choose any enumerated action instead.
            foreach (string effect in spells ? new[] { "SpawnMonster", "Spell", "Null" } : new[] { "SpawnMonster", "Null" })
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
                    if (effect == "Spell")
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
