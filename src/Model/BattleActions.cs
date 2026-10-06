using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
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
        public CardActionEffect(string type, string target, int value, bool allowEnemy, bool allowPlayer, IReadOnlyList<CombatStatus> statuses,
            CardUpgradeModifier? upgrade = null, string lifetime = "")
        { Type = type; Target = target; Value = value; AllowEnemy = allowEnemy; AllowPlayer = allowPlayer; Statuses = Array.AsReadOnly(statuses.ToArray());
            Upgrade = upgrade; Lifetime = lifetime; }
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
        public CardPlayRule(string dataId, string assetKey, int cost, string effect, string destination,
            CombatUnit? spawnUnit, IReadOnlyList<string> externalInteractions, IReadOnlyList<CardActionEffect>? effects = null)
        {
            DataId = dataId; AssetKey = assetKey; Cost = cost; Effect = effect; Destination = destination;
            SpawnUnit = spawnUnit; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            Effects = Array.AsReadOnly((effects ?? Array.Empty<CardActionEffect>()).ToArray());
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
            CardToken? card = context.Cards.Hand.FirstOrDefault(item => item.InstanceId == action.CardInstanceId);
            if (card == null) return Illegal("The selected card instance is not in hand.");
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            if (rule == null) return Unsupported("Missing play definition for " + card.DataId);
            if (context.CardInstances != null)
            {
                CardInstanceState? instance = context.CardInstances.FirstOrDefault(item => item.InstanceId == card.InstanceId);
                if (instance == null) return Unsupported("Missing card instance modifiers.");
                rule = CardModifierModel.Resolve(rule, instance);
            }
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (rule.Cost < 0) return Unsupported("Variable or negative card costs are not implemented.");
            if (source.Energy < rule.Cost) return Illegal("Insufficient energy.");
            RoomPlayRule? targetRule = source.PlayRules.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            RoomCombatState? target = train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            if (targetRule == null || target == null) return Illegal("The target room does not exist.");
            if (!targetRule.Enabled || targetRule.IsPyre) return Illegal("This card cannot be played in the target room.");
            CardToken[] allCards = context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard)
                .Concat(source.OtherPiles.SelectMany(pile => pile.Cards)).ToArray();
            if (allCards.Select(item => item.InstanceId).Distinct().Count() != allCards.Length ||
                allCards.Any(item => item.InstanceId <= 0 || item.InstanceId >= context.NextCardId))
                return Unsupported("Invalid card identity allocation or duplicate pile membership.");
            if (source.OtherPiles.Select(pile => pile.Name).Distinct().Count() != source.OtherPiles.Count)
                return Unsupported("Duplicate card piles.");
            target = new RoomCombatState(target.RoomIndex, target.Deployment, target.Units, target.ExternalInteractions, context, target.Preview);
            CombatUnit[] players = target.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            int position = action.PlayerPosition == -1 ? players.Length : action.PlayerPosition;
            CombatUnit? spawned = null;
            RoomCombatState? spellRoom = null;
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
                spawned = new CombatUnit(nextUnitId++, template.AssetKey, CombatTeam.Player, template.BaseAttack,
                    template.Health, template.MaxHealth, template.CanAttack, false, false, template.Statuses,
                    template.Triggers, card.InstanceId, template.Size, template.StatusImmunities, template.Subtypes, template.Modifiers);
                context = context.WithStatistics(context.Statistics?.Spawn(action.RoomIndex, template.Subtypes));
            }
            else if (rule.Effect == "Spell")
            {
                if (action.PlayerPosition != -1) return Illegal("A spell does not take a summon position.");
                CombatUnit? victim = target.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId);
                if (victim == null || victim.IsPyre || victim.Statuses.Any(status => status.Id == "untouchable"))
                    return Illegal("The spell has no legal unit target in the selected room.");
                if (rule.Effects.Count > 0 && (victim.Team == CombatTeam.Enemy && !rule.Effects[0].AllowEnemy ||
                    victim.Team == CombatTeam.Player && !rule.Effects[0].AllowPlayer)) return Illegal("The spell excludes the target team.");
                RoomCombatResult result = CardSpellModel.Apply(target, rule.Effects, victim.Id, card.InstanceId, targetRule.PlayerCapacity, targetRule.EnemyCapacity);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                spellRoom = result.State!; context = spellRoom.Context!; outcome = result.Outcome;
            }
            else if (rule.Effect != "Null") return Unsupported("Unimplemented card effect " + rule.Effect);
            else if (action.PlayerPosition != -1 || action.TargetUnitId != 0) return Illegal("A no-target card does not take a target or position.");

            List<CardToken> hand = context.Cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToList();
            List<CardToken> discard = context.Cards.Discard.ToList();
            CardPileState[] piles = source.OtherPiles.ToArray();
            bool terminal = outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated;
            if (spellRoom != null)
            {
                var alive = new HashSet<int>(spellRoom.Units.Select(unit => unit.Id));
                foreach (CombatUnit dead in target.Units.Where(unit => unit.SpawnerCardId > 0 && !alive.Contains(unit.Id)))
                {
                    CardPileState? standby = piles.FirstOrDefault(pile => pile.Name == "Standby");
                    CardPileState? exhausted = piles.FirstOrDefault(pile => pile.Name == "Exhausted");
                    CardToken? deadCard = standby?.Cards.FirstOrDefault(item => item.InstanceId == dead.SpawnerCardId);
                    if (standby == null || exhausted == null || deadCard == null) return Unsupported("Missing dead unit spawner card routing.");
                    piles = piles.Select(pile => pile == standby ? new CardPileState(pile.Name,
                        pile.Cards.Where(item => item.InstanceId != dead.SpawnerCardId).ToArray()) : pile == exhausted
                            ? new CardPileState(pile.Name, pile.Cards.Concat(new[] { deadCard }).ToArray()) : pile).ToArray();
                }
            }
            if (terminal) piles = piles.Select(pile => new CardPileState(pile.Name, Array.Empty<CardToken>())).ToArray();
            else if (rule.Destination == "Discard") discard.Add(card);
            else
            {
                CardPileState? destination = piles.FirstOrDefault(pile => pile.Name == rule.Destination);
                if (destination == null || (rule.Destination != "Standby" && rule.Destination != "Purged" && rule.Destination != "Exhausted"))
                    return Unsupported("Unimplemented card destination " + rule.Destination);
                piles = piles.Select(pile => pile == destination ? new CardPileState(pile.Name,
                    pile.Cards.Concat(new[] { card }).ToArray()) : pile).ToArray();
            }
            BattleStatistics? statistics = context.Statistics?.Increment(card.InstanceId, "TimesPlayed").Increment(card.InstanceId, "TimesDiscarded");
            if (rule.Destination == "Exhausted") statistics = statistics?.Increment(card.InstanceId, "TimesExhausted");
            context = new CombatContext(new CardCycleState(hand, context.Cards.Draw, discard, context.Cards.Rng,
                context.Cards.DrawModifier, context.Cards.ExternalInteractions), context.BattleRng,
                context.Gold, context.NextCardId, context.MaxHandSize, context.StatusRules, statistics,
                terminal ? context.CardInstances : context.CardInstances?.Select(instance => instance.InstanceId == card.InstanceId
                    ? instance.OnDiscard(true, rule.Cost) : instance).ToArray());
            RoomCombatState[] rooms = train.Rooms.Select(room =>
            {
                CombatUnit[] units = room.Units.ToArray();
                if (room.RoomIndex == target.RoomIndex && spellRoom != null) units = spellRoom.Units.ToArray();
                if (room.RoomIndex == target.RoomIndex && spawned != null)
                {
                    var nextPlayers = players.ToList(); nextPlayers.Insert(position, spawned);
                    units = room.Units.Where(unit => unit.Team == CombatTeam.Enemy).Concat(nextPlayers).ToArray();
                }
                return new RoomCombatState(room.RoomIndex, room.Deployment, units, room.ExternalInteractions, context);
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
                piles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled), outcome: outcome);
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
                if (rule?.Effect == "Spell")
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
                for (int offset = 0; offset < roomCount; offset++)
                {
                    int roomIndex = (source.Spawn.Turn + offset) % roomCount;
                    int count = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex)
                        .Units.Count(unit => unit.Team == CombatTeam.Player);
                    if (effect == "Spell")
                    {
                        bool damage = rule.Effects.Any(item => item.Type == "Damage");
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
