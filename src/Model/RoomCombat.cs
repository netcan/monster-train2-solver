using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonsterTrain2Poju.Model
{
    public enum CombatTeam { Enemy, Player }
    public enum RoomOutcome { Exchanged, Cleared, PlayerDefeated, BattleWon, Stalemate, Unsupported }

    public sealed class CombatStatus
    {
        public string Id { get; }
        public int Stacks { get; }
        public int ParamInt { get; }
        public bool RemoveWhenTriggered { get; }
        public bool RemoveStackAtEnd { get; }
        public bool RemoveAllAtEnd { get; }
        public bool RemoveAfterPostCombat { get; }
        public bool PreventRemovalDuringRelentless { get; }
        public bool SkipDuringDeployment { get; }
        public bool RemoveDuringDeployment { get; }
        public bool? TriggerVfxEnemy { get; }
        public bool? TriggerVfxPlayer { get; }
        public bool? Stackable { get; }

        public CombatStatus(string id, int stacks, int paramInt = 0,
            bool removeWhenTriggered = false, bool removeStackAtEnd = false,
            bool removeAllAtEnd = false, bool removeAfterPostCombat = false,
            bool preventRemovalDuringRelentless = false, bool skipDuringDeployment = false,
            bool removeDuringDeployment = false, bool? triggerVfxEnemy = null, bool? triggerVfxPlayer = null, bool? stackable = null)
        {
            Id = id;
            Stacks = stacks;
            ParamInt = paramInt;
            RemoveWhenTriggered = removeWhenTriggered;
            RemoveStackAtEnd = removeStackAtEnd;
            RemoveAllAtEnd = removeAllAtEnd;
            RemoveAfterPostCombat = removeAfterPostCombat;
            PreventRemovalDuringRelentless = preventRemovalDuringRelentless;
            SkipDuringDeployment = skipDuringDeployment;
            RemoveDuringDeployment = removeDuringDeployment;
            TriggerVfxEnemy = triggerVfxEnemy;
            TriggerVfxPlayer = triggerVfxPlayer;
            Stackable = stackable;
        }

        internal CombatStatus WithStacks(int stacks) => new CombatStatus(Id, stacks, ParamInt,
            RemoveWhenTriggered, RemoveStackAtEnd, RemoveAllAtEnd, RemoveAfterPostCombat,
            PreventRemovalDuringRelentless, SkipDuringDeployment, RemoveDuringDeployment, TriggerVfxEnemy, TriggerVfxPlayer, Stackable);
    }

    public sealed class CombatUnit
    {
        public int Id { get; }
        public string AssetKey { get; }
        public CombatTeam Team { get; }
        public int BaseAttack { get; }
        public int Attack => Math.Max(0, BaseAttack + StatusAmount("buff") + StatusAmount("valor") - StatusAmount("debuff"));
        public int Health { get; }
        public int MaxHealth { get; }
        public bool CanAttack { get; }
        public bool IsPyre { get; }
        public bool EndsBattleOnDeath { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public IReadOnlyList<CombatTrigger> Triggers { get; }
        public int SpawnerCardId { get; }
        public int Size { get; }
        public IReadOnlyList<string> StatusImmunities { get; }
        public IReadOnlyList<string> Subtypes { get; }
        public UnitModifiers? Modifiers { get; }
        public bool? IsBoss { get; }
        // Null denotes a legacy capture without relationship state; zero is known-none.
        public int? LastAttackerId { get; }
        public int Attacks => Math.Max(1, Status("multistrike") is CombatStatus multi
            ? multi.ParamInt + multi.Stacks - 1 : 1);

        public CombatUnit(int id, string assetKey, CombatTeam team, int baseAttack,
            int health, int maxHealth, bool canAttack, bool isPyre, bool endsBattleOnDeath,
            IReadOnlyList<CombatStatus> statuses, IReadOnlyList<CombatTrigger>? triggers = null, int spawnerCardId = 0, int size = 0,
            IReadOnlyList<string>? statusImmunities = null, IReadOnlyList<string>? subtypes = null, UnitModifiers? modifiers = null, bool? isBoss = null,
            int? lastAttackerId = null)
        {
            Id = id;
            LastAttackerId = lastAttackerId;
            AssetKey = assetKey;
            Team = team;
            BaseAttack = baseAttack;
            Health = health;
            MaxHealth = maxHealth;
            CanAttack = canAttack;
            IsPyre = isPyre;
            EndsBattleOnDeath = endsBattleOnDeath;
            Statuses = Array.AsReadOnly(statuses.Where(item => item.Stacks > 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).ToArray());
            Triggers = Array.AsReadOnly((triggers ?? Array.Empty<CombatTrigger>()).ToArray());
            SpawnerCardId = spawnerCardId;
            Size = size;
            StatusImmunities = Array.AsReadOnly((statusImmunities ?? Array.Empty<string>())
                .Concat(Statuses.Any(status => status.Id == "relentless") ? new[] { "rooted" } : Array.Empty<string>())
                .Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray());
            Subtypes = Array.AsReadOnly((subtypes ?? Array.Empty<string>()).ToArray());
            Modifiers = modifiers;
            IsBoss = isBoss;
        }

        internal CombatStatus? Status(string id) => Statuses.FirstOrDefault(item => item.Id == id);
        internal int StatusAmount(string id) => Status(id) is CombatStatus status
            ? status.Stacks * status.ParamInt : 0;

        internal CombatUnit WithoutRemovedAttacker(ISet<int> activeIds) => !LastAttackerId.HasValue || LastAttackerId == 0 || activeIds.Contains(LastAttackerId.Value)
            ? this : new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth, CanAttack, IsPyre, EndsBattleOnDeath, Statuses,
                Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes, Modifiers, IsBoss, 0);
    }

    public sealed class RoomCombatState
    {
        public int RoomIndex { get; }
        public bool Deployment { get; }
        public IReadOnlyList<CombatUnit> Units { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public CombatContext? Context { get; }
        public bool Preview { get; }

        // Units are ordered front to back within each team. Identity is separate from asset name.
        public RoomCombatState(int roomIndex, bool deployment, IReadOnlyList<CombatUnit> units,
            IReadOnlyList<string> externalInteractions, CombatContext? context = null, bool preview = false)
        {
            RoomIndex = roomIndex;
            Deployment = deployment;
            Units = Array.AsReadOnly(units.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            Context = context;
            Preview = preview;
        }
    }

    public sealed class CombatEvent
    {
        public int Round { get; }
        public string Kind { get; }
        public int Actor { get; }
        public int Target { get; }
        public int Amount { get; }

        internal CombatEvent(int round, string kind, int actor, int target, int amount)
        { Round = round; Kind = kind; Actor = actor; Target = target; Amount = amount; }
    }

    public sealed class RoomCombatResult
    {
        public RoomCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public int Rounds { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;

        internal RoomCombatResult(RoomCombatState? state, RoomOutcome outcome, int rounds,
            List<CombatEvent> events, string? reason = null)
        {
            State = state; Outcome = outcome; Rounds = rounds;
            Events = Array.AsReadOnly(events.ToArray()); UnsupportedReason = reason;
        }
    }

    public static class RoomCombatModel
    {
        internal sealed class QueuedCharacterTrigger
        {
            internal int RoomIndex { get; }
            internal CombatUnit Unit { get; set; }
            internal string Kind { get; }
            internal int ParamInt { get; }
            internal CombatUnit? OverrideTarget { get; }
            internal bool ReturnSpawnerAfterQueue { get; }
            internal bool DeferUntilRemoval { get; }
            internal QueuedCharacterTrigger(int roomIndex, CombatUnit unit, string kind = "OnDeath", bool returnSpawnerAfterQueue = false, bool deferUntilRemoval = false, int paramInt = 0,
                CombatUnit? overrideTarget = null)
            { RoomIndex = roomIndex; Unit = unit; Kind = kind; ReturnSpawnerAfterQueue = returnSpawnerAfterQueue; DeferUntilRemoval = deferUntilRemoval; ParamInt = paramInt; OverrideTarget = overrideTarget; }
        }
        private static readonly HashSet<string> KnownStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "armor", "damage shield", "dazed", "stealth", "ambush", "multistrike",
            "spikes", "lifesteal", "fragile", "piercing", "immune", "immobile",
            "relentless", "sweep", "sniper", "rooted", "haste", "untouchable",
            "buff", "debuff", "regen", "poison", "melee weakness", "silenced", "valor", "pyregel",
            "heal multiplier", "heal immunity"
        };
        internal static bool KnowsStatus(string id) => KnownStatuses.Contains(id);

        public static RoomCombatResult Exchange(RoomCombatState state) => Run(state, false);
        public static RoomCombatResult Resolve(RoomCombatState state) => Run(state, true);

        public static RoomCombatResult ApplyUnitPostCombat(RoomCombatState state, IReadOnlyList<int> cannotAttackOrHeal,
            IReadOnlyList<int> cannotFireTriggers)
        {
            string? error = Validate(state);
            if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            return new Engine(state, new List<CombatEvent>()).UnitPostCombat(cannotAttackOrHeal, cannotFireTriggers);
        }

        public static RoomCombatResult ApplyUnitTurn(RoomCombatState state, int unitId)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == unitId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Unit turn requires a living actor in the room.");
            return new Engine(state, new List<CombatEvent>()).UnitTurn(unitId);
        }

        public static RoomCombatResult ApplyTeamTurnBegin(RoomCombatState state, CombatTeam team)
        {
            string? error = Validate(state);
            if (error != null || team != CombatTeam.Enemy && team != CombatTeam.Player)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid combat team.");
            return new Engine(state, new List<CombatEvent>()).TeamTurnBegin(team);
        }

        public static RoomCombatResult ApplyEndTurnPreHandDiscard(RoomCombatState state, int unitId)
            => ApplyCharacterPhase(state, unitId, "EndTurnPreHandDiscard");

        public static RoomCombatResult ApplyPreCombat(RoomCombatState state, int unitId)
            => ApplyCharacterPhase(state, unitId, "PreCombat");

        private static RoomCombatResult ApplyCharacterPhase(RoomCombatState state, int unitId, string kind)
        {
            var queue = new List<QueuedCharacterTrigger>();
            RoomCombatResult result = ApplyCharacterPhase(state, unitId, kind, queue.Add);
            if (!result.Supported) return result;
            var events = result.Events.ToList();
            RoomOutcome outcome = result.Outcome;
            bool drained = DrainCharacterQueue(queue, queued =>
            {
                result = ApplyQueuedCharacterTrigger(result.State!, queued, queue.Add);
                if (!result.Supported) return false;
                events.AddRange(result.Events);
                if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                return true;
            }, queued =>
            {
                result = SettleQueuedSpawner(result.State!, queued.Unit);
                return result.Supported;
            });
            if (!drained) return result;
            return new RoomCombatResult(result.State, outcome, 0, events);
        }

        internal static bool DrainCharacterQueue(List<QueuedCharacterTrigger> queue, Func<QueuedCharacterTrigger, bool> fire,
            Func<QueuedCharacterTrigger, bool> returnSpawner)
        {
            int next = 0;
            var pending = new List<QueuedCharacterTrigger>();
            bool Fire(QueuedCharacterTrigger queued)
            {
                // Callbacks retain the same native character object after lethal damage. Its
                // latest death snapshot carries health and once flags across phase engines.
                QueuedCharacterTrigger? dead = queue.LastOrDefault(item => item.Kind == "OnDeath" &&
                    item.RoomIndex == queued.RoomIndex && item.Unit.Id == queued.Unit.Id && item.Unit.Health <= 0);
                if (dead != null) queued.Unit = dead.Unit;
                if (!fire(queued)) return false;
                if (queued.Unit.Health <= 0)
                    foreach (QueuedCharacterTrigger item in queue.Where(item => item.RoomIndex == queued.RoomIndex && item.Unit.Id == queued.Unit.Id))
                        item.Unit = queued.Unit;
                return true;
            }
            bool Drain()
            {
                while (next < queue.Count)
                {
                    QueuedCharacterTrigger queued = queue[next++];
                    if (queued.DeferUntilRemoval) pending.Add(queued);
                    else if (!Fire(queued)) return false;
                }
                // Native snapshots all eligible deaths and marks the complete batch as being
                // removed. New deaths during a removal are handled before the current spawner returns.
                QueuedCharacterTrigger[] removing = pending.OrderBy(item => item.Unit.Team).ThenBy(item => item.Unit.Id).ToArray();
                pending.Clear();
                foreach (QueuedCharacterTrigger dead in removing)
                {
                    if (!Fire(dead) || !Drain()) return false;
                    if (dead.ReturnSpawnerAfterQueue && !returnSpawner(dead)) return false;
                }
                return true;
            }
            return Drain();
        }

        internal static RoomCombatResult ApplyCharacterPhase(RoomCombatState state, int unitId, string kind, Action<QueuedCharacterTrigger> enqueue)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == unitId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Character phases require a living character actor.");
            return new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: enqueue).CharacterPhase(unitId, kind);
        }

        internal static RoomCombatResult ApplyQueuedCharacterTrigger(RoomCombatState state, QueuedCharacterTrigger queued, Action<QueuedCharacterTrigger> enqueue)
            => new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: enqueue, resetPreviewTriggers: false).QueuedTrigger(queued);

        internal static RoomCombatResult SettleQueuedSpawner(RoomCombatState state, CombatUnit unit)
            => new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false).ReturnQueuedSpawner(unit);

        public static RoomCombatResult ApplySpawnTriggers(RoomCombatState state, int unitId, bool fromCard)
        {
            string? error = Validate(state);
            CombatUnit? unit = state.Units.FirstOrDefault(item => item.Id == unitId);
            if (error != null || unit == null || fromCard && unit.SpawnerCardId <= 0)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Spawn triggers require a living new unit and its source card identity.");
            return new Engine(state, new List<CombatEvent>(), pendingSummonCardId: fromCard ? unit.SpawnerCardId : 0).Spawn(unitId, fromCard);
        }

        public static RoomCombatResult ApplyCardDamage(RoomCombatState state, int targetId, int damage, int sourceCardId = 0,
            bool deferSpawnerExhaustion = false) => ApplyCardDamage(state, targetId, damage, sourceCardId, deferSpawnerExhaustion, true);

        internal static RoomCombatResult ApplyCardDamageAfterTraits(RoomCombatState state, int targetId, int damage, int sourceCardId,
            bool deferSpawnerExhaustion) => ApplyCardDamage(state, targetId, damage, sourceCardId, deferSpawnerExhaustion, false);

        private static RoomCombatResult ApplyCardDamage(RoomCombatState state, int targetId, int damage, int sourceCardId,
            bool deferSpawnerExhaustion, bool applyTraits)
        {
            string? error = Validate(state);
            if (error != null || damage < 0 || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid card damage target or amount.");
            return new Engine(state, new List<CombatEvent>(), deferSpawnerExhaustion).CardDamage(targetId, damage, sourceCardId, applyTraits);
        }

        internal static RoomCombatResult ApplyUnitModification(RoomCombatState state, CombatUnit changed) =>
            new Engine(state, new List<CombatEvent>()).ModifyUnit(changed);

        internal static RoomCombatResult ApplyUnitUpgrade(RoomCombatState state, int targetId, CardUpgradeModifier upgrade,
            string lifetime, bool remove, int? roomCapacity, int sourceCardId, string? triggerKind)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing unit upgrade target.");
            return new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false)
                .UnitUpgrade(targetId, upgrade, lifetime, remove, roomCapacity, sourceCardId, triggerKind);
        }

        public static RoomCombatResult ApplyCardHeal(RoomCombatState state, int targetId, int amount)
        {
            string? error = Validate(state);
            CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || amount < 0 || target?.Modifiers == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Card healing requires a valid amount, target and healability state.");
            return new Engine(state, new List<CombatEvent>()).CardHeal(targetId, amount);
        }

        private static RoomCombatResult Run(RoomCombatState state, bool entireRoom)
        {
            var events = new List<CombatEvent>();
            string? error = Validate(state);
            if (error != null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, error);
            var engine = new Engine(state, events);
            return engine.Run(entireRoom);
        }

        internal static string? Validate(RoomCombatState state, int? dyingTargetId = null)
        {
            if (state.ExternalInteractions.Count > 0)
                return string.Join("; ", state.ExternalInteractions);
            foreach (CardInstanceState instance in state.Context?.CardInstances ?? Array.Empty<CardInstanceState>())
            {
                string? unsupported = CardModifierModel.UnsupportedReason(instance);
                if (unsupported != null) return unsupported;
            }
            if (state.Units.Select(unit => unit.Id).Distinct().Count() != state.Units.Count)
                return "Unit instance IDs must be unique.";
            foreach (CombatUnit unit in state.Units)
            {
                if (unit.Modifiers != null && (unit.Modifiers.HealthFromUpgrades.Count > 0 ||
                    unit.Modifiers.Upgrades.Any(upgrade => upgrade.ExternalInteractions.Count > 0)))
                    return "Unmodeled applied unit upgrade interactions.";
                if (unit.Health < 0 || unit.Health == 0 && unit.Id != dyingTargetId || unit.Health > unit.MaxHealth)
                    return "Only living units with valid health can enter room combat.";
                if (unit.LastAttackerId < 0) return "Invalid last-attacker identity.";
                if (unit.Statuses.Select(status => status.Id).Distinct().Count() != unit.Statuses.Count)
                    return "Duplicate status IDs on unit " + unit.Id;
                foreach (CombatStatus status in unit.Statuses)
                    if (!KnownStatuses.Contains(status.Id))
                        return "Unmodeled status " + status.Id + " on " + unit.AssetKey;
                if (!state.Deployment && unit.Status("valor") != null &&
                    state.Context?.StatusRules.FirstOrDefault(rule => rule.Id == "armor") == null)
                    return "Valor requires the armor status definition.";
                foreach (CombatTrigger trigger in unit.Triggers)
                {
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "PostCombat" && trigger.Kind != "PostCombatHealing" && trigger.Kind != "OnHeal" &&
                        trigger.Kind != "OnSpawn" && trigger.Kind != "OnUnscaledSpawn" && trigger.Kind != "OnSpawnNotFromCard" &&
                        trigger.Kind != "OnTurnBegin" && trigger.Kind != "OnTeamTurnBegin" && trigger.Kind != "EndTurnPreHandDiscard" && trigger.Kind != "PreCombat" &&
                        trigger.Kind != "OnHit" && trigger.Kind != "OnKill" && trigger.Kind != "OnAttackingBeforeDamage" && trigger.Kind != "OnAttacking")
                        return "Unmodeled trigger " + trigger.Kind;
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "PostCombat" && trigger.SkipDuringDeployment == null)
                        return trigger.Kind + " requires deployment timing state.";
                    if (trigger.TriggerAtThreshold > 0 && trigger.Kind != "OnHit" && trigger.Kind != "OnKill" &&
                        trigger.Kind != "OnAttackingBeforeDamage" && trigger.Kind != "OnAttacking")
                        return "Threshold arguments are not modeled for " + trigger.Kind;
                    if (trigger.FireCount < 0) return "Invalid trigger fire count.";
                    foreach (CombatEffect effect in trigger.Effects)
                    {
                        if (effect.Action != null && effect.Type != "CardEffectHeal" && effect.Type != "CardEffectDamage" && effect.Type != "CardEffectAddStatusEffect")
                            return "Mismatched triggered action definition.";
                        if (effect.DamageStatusMultiplier != null && effect.Type != "CardEffectDamage")
                            return "Mismatched triggered damage multiplier.";
                        if (effect.StatusScaling != null && effect.Type != "CardEffectAddStatusEffect") return "Mismatched triggered status scaling.";
                        if (effect.Type == "CardEffectDespawnCharacter") continue;
                        if (effect.Type == "CardEffectHeal" || effect.Type == "CardEffectDamage" || effect.Type == "CardEffectAddStatusEffect")
                        {
                            CardActionEffect? action = effect.Action;
                            if (action?.Type != (effect.Type == "CardEffectHeal" ? "Heal" : effect.Type == "CardEffectDamage" ? "Damage" : "AddStatus")) return "Missing triggered action definition.";
                            if (action.Type == "AddStatus")
                            {
                                string? statusError = TriggeredStatusModel.Validate(effect);
                                if (statusError != null) return statusError;
                                if (state.Context == null) return "Triggered status requires shared battle context.";
                            }
                            if (action.Type == "Heal" && state.Units.Any(target => target.Modifiers == null))
                                return "Triggered healing requires unit healability state.";
                            if (!new[] { "Self", "Room", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets", "RandomInRoom", "LastAttackedCharacter" }.Contains(action.Target))
                                return "Unmodeled triggered action target " + action.Target;
                            if ((action.Range != null || action.Target == "RandomInRoom") && state.Context == null)
                                return "Triggered action randomness requires shared battle context.";
                            string? healError = (action.Target == "LastAttackedCharacter" ? null : action.Filters?.Validate()) ?? action.Range?.Validate();
                            if (healError != null) return healError;
                            continue;
                        }
                        if (effect.Type == "CardEffectAddCardUpgradeToUnits" || effect.Type == "CardEffectAddTempCardUpgradeToUnits" ||
                            effect.Type == "CardEffectRemoveTempUpgradeFromUnit")
                        {
                            CardActionEffect? action = effect.UnitUpgrade;
                            if (action?.Upgrade == null || action.Type != (effect.Type == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" : "UnitUpgrade"))
                                return "Missing triggered unit upgrade definition.";
                            if (state.Context?.CardInstances == null || unit.Modifiers == null)
                                return "Triggered unit upgrades require unit and card modifier state.";
                            if (action.Upgrade.ExternalInteractions.Count > 0)
                                return string.Join("; ", action.Upgrade.ExternalInteractions);
                            if (action.Upgrade.RestrictSizeToRoomCapacity)
                                return "Triggered size restrictions require room capacity state.";
                            if (action.Target != "Self" && !new[] { "Room", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets", "RandomInRoom", "LastAttackedCharacter" }.Contains(action.Target))
                                return "Unmodeled triggered upgrade target " + action.Target;
                            if (action.Range != null) return "Triggered upgrade range initialization is not modeled.";
                            string? filterError = action.Target == "LastAttackedCharacter" ? null : action.Filters?.Validate();
                            if (filterError != null) return filterError;
                            continue;
                        }
                        if (effect.Type != "CardEffectRewardGold" && effect.Type != "CardEffectAddBattleCard")
                            return "Unmodeled effect " + effect.Type;
                        if (state.Context == null) return "Unit effects require shared battle context.";
                        if (effect.Type == "CardEffectRewardGold" && effect.Value < 0)
                            return "Conditional gold costs are not modeled.";
                        if (effect.Type == "CardEffectAddBattleCard" &&
                            (effect.Generation == null && effect.CardPool.Count == 0 || !new[] { "DeckPile", "DeckPileTop", "DeckPileRandom",
                                "HandPile", "DiscardPile" }.Contains(effect.Destination)))
                            return "Unmodeled generated card destination or empty pool.";
                    }
                }
            }
            return null;
        }

        private sealed class WorkingUnit
        {
            internal CombatUnit Source;
            internal int Health;
            internal readonly Dictionary<string, CombatStatus> Statuses;
            internal readonly List<CombatTrigger> Triggers;
            internal bool Despawned;
            internal bool DeathFinished;
            internal bool Removed;
            internal int? LastAttackerId;
            internal bool Alive => Health > 0 && !Removed;
            internal int Attack => Math.Max(0, Source.BaseAttack + Amount("buff") + Amount("valor") - Amount("debuff"));

            internal WorkingUnit(CombatUnit source)
            {
                Source = source; Health = source.Health; LastAttackerId = source.LastAttackerId;
                Statuses = source.Statuses.ToDictionary(status => status.Id, StringComparer.Ordinal);
                Triggers = source.Triggers.ToList();
            }

            internal bool Has(string id) => Statuses.ContainsKey(id);
            internal void Apply(CombatUnit changed)
            {
                Source = changed; Health = changed.Health; LastAttackerId = changed.LastAttackerId;
                Statuses.Clear();
                foreach (CombatStatus status in changed.Statuses) Statuses.Add(status.Id, status);
                Triggers.Clear(); Triggers.AddRange(changed.Triggers);
            }
            internal int Amount(string id) => Statuses.TryGetValue(id, out CombatStatus? status)
                ? status.ParamInt * status.Stacks : 0;
            internal int Count(string id) => Statuses.TryGetValue(id, out CombatStatus? status) ? status.Stacks : 0;
            internal void Remove(string id, int count)
            {
                if (!Statuses.TryGetValue(id, out CombatStatus? status)) return;
                if (status.Stacks <= count) Statuses.Remove(id);
                else Statuses[id] = status.WithStacks(status.Stacks - count);
            }
            internal CombatUnit Freeze() => new CombatUnit(Source.Id, Source.AssetKey, Source.Team,
                Source.BaseAttack, Health, Source.MaxHealth, Source.CanAttack, Source.IsPyre,
                Source.EndsBattleOnDeath, Statuses.Values.ToArray(), Triggers, Source.SpawnerCardId, Source.Size, Source.StatusImmunities, Source.Subtypes, Source.Modifiers, Source.IsBoss, LastAttackerId);
        }

        private sealed class Engine
        {
            private readonly RoomCombatState source;
            private readonly List<WorkingUnit> units;
            private readonly List<CombatEvent> events;
            private bool battleWon;
            private int round;
            private CombatContext? context;
            private readonly bool deferSpawnerExhaustion;
            private readonly int pendingSummonCardId;
            private readonly Action<QueuedCharacterTrigger>? enqueueCharacterTrigger;
            private readonly Queue<(WorkingUnit Unit, string Kind, bool CanFire, int ParamInt, WorkingUnit? OverrideTarget)> triggerQueue = new Queue<(WorkingUnit, string, bool, int, WorkingUnit?)>();
            private bool runningTriggerQueue;
            private bool killCamActivated;
            // Older captures omitted the identity store. Retain observed source cards while
            // this operation finishes, without inventing unobserved references in its output.
            private IReadOnlyList<CardInstanceState>? legacyDetachedCards;
            private readonly HashSet<int> cannotAttackOrHeal = new HashSet<int>();
            private readonly HashSet<int> cannotFireTriggers = new HashSet<int>();
            private string? unsupportedReason;

            internal Engine(RoomCombatState source, List<CombatEvent> events, bool deferSpawnerExhaustion = false, int pendingSummonCardId = 0,
                Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null, bool resetPreviewTriggers = true)
            {
                this.source = source; this.events = events;
                this.deferSpawnerExhaustion = deferSpawnerExhaustion;
                this.pendingSummonCardId = pendingSummonCardId;
                this.enqueueCharacterTrigger = enqueueCharacterTrigger;
                units = source.Units.Select(unit => new WorkingUnit(unit)).ToList();
                if (source.Preview && resetPreviewTriggers)
                    foreach (WorkingUnit unit in units)
                        for (int index = 0; index < unit.Triggers.Count; index++) unit.Triggers[index] = unit.Triggers[index].ForPreview();
                context = source.Context;
                killCamActivated = context?.KillCamActivated == true;
            }

            internal RoomCombatResult Run(bool entireRoom)
            {
                bool relentless = units.Any(unit => unit.Has("relentless")) && BothTeamsPresent();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                do
                {
                    if (round > 0 && entireRoom) Clear(true, relentless);
                    // Compare the same phase: decay before the next exchange can break a apparent cycle.
                    if (entireRoom && !seen.Add(Signature()))
                        return Finish(RoomOutcome.Stalemate);
                    round++;
                    Exchange();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    if (!entireRoom) return Finish(RoomOutcome.Exchanged);
                    if (battleWon) return Finish(RoomOutcome.BattleWon);
                    if (!source.Preview && units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    PostCombat();
                    if (battleWon) return Finish(RoomOutcome.BattleWon);
                    if (!source.Preview && units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    Clear(false, relentless);
                } while (relentless && BothTeamsPresent());
                RunUnitPostCombat();
                if (!source.Deployment)
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                    {
                        WorkingUnit? front = units.FirstOrDefault(unit => unit.Alive && unit.Source.Team == team && !unit.Has("untouchable"));
                        if (front == null || !front.Has("valor") || front.Has("immune") || front.Source.StatusImmunities.Contains("armor")) continue;
                        int goal = Math.Min(9999, front.Amount("valor"));
                        if (goal > front.Count("armor")) front.Statuses["armor"] = context!.StatusRules.First(rule => rule.Id == "armor").WithStacks(goal);
                    }
                Clear(true, relentless);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Cleared);
            }

            internal RoomCombatResult UnitPostCombat(IReadOnlyList<int> cannotHeal, IReadOnlyList<int> cannotTrigger)
            {
                cannotAttackOrHeal.UnionWith(cannotHeal); cannotFireTriggers.UnionWith(cannotTrigger);
                RunUnitPostCombat();
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            private void RunUnitPostCombat()
            {
                // Native snapshots enemies then players, preserving front-to-back order.
                // Each actor finishes its healing queue before its ordinary post-combat queue.
                foreach (WorkingUnit unit in units.OrderBy(unit => unit.Source.Team).ToArray())
                {
                    if (!unit.Alive) continue;
                    bool canFire = !cannotFireTriggers.Contains(unit.Source.Id);
                    if (!cannotAttackOrHeal.Contains(unit.Source.Id)) FireTriggers(unit, "PostCombatHealing", canFire);
                    FireTriggers(unit, "PostCombat", canFire);
                }
            }

            internal RoomCombatResult CardDamage(int targetId, int damage, int sourceCardId, bool applyTraits = true)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Damage(null, target, damage, "Spell", sourceCardId, applyTraits);
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult ModifyUnit(CombatUnit changed)
            {
                int index = units.FindIndex(unit => unit.Source.Id == changed.Id);
                WorkingUnit target = new WorkingUnit(changed);
                units[index] = target;
                if (!target.Alive) Death(null, target, 0);
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult UnitUpgrade(int targetId, CardUpgradeModifier upgrade, string lifetime,
                bool remove, int? roomCapacity, int sourceCardId, string? triggerKind)
            {
                ApplyUpgrade(units.Single(unit => unit.Source.Id == targetId), upgrade, lifetime, remove,
                    roomCapacity, sourceCardId, triggerKind);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult CardHeal(int targetId, int amount)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Heal(target, amount, "SpellHeal");
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult Spawn(int unitId, bool fromCard)
            {
                WorkingUnit spawned = units.Single(unit => unit.Source.Id == unitId);
                FireTriggers(spawned, "OnSpawn");
                FireTriggers(spawned, "OnUnscaledSpawn");
                if (!fromCard) FireTriggers(spawned, "OnSpawnNotFromCard");
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult UnitTurn(int unitId)
            {
                round = 1;
                Turn(units.Single(unit => unit.Source.Id == unitId));
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult TeamTurnBegin(CombatTeam team)
            {
                BeginTeam(team);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult CharacterPhase(int unitId, string kind)
            {
                FireTriggers(units.Single(unit => unit.Source.Id == unitId), kind, fromQueue: true);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult QueuedTrigger(QueuedCharacterTrigger queued)
            {
                WorkingUnit? actor = units.FirstOrDefault(unit => unit.Source.Id == queued.Unit.Id);
                if (actor == null && (queued.Kind == "OnDeath" || queued.Kind == "OnHit" || queued.Kind == "OnKill" ||
                    queued.Kind == "OnAttackingBeforeDamage" || queued.Kind == "OnAttacking"))
                { actor = new WorkingUnit(queued.Unit); units.Add(actor); }
                // A queued OnHeal on an actor killed by a later phase effect has no live effects.
                if (actor != null)
                {
                    WorkingUnit? overridden = queued.OverrideTarget == null ? null : units.FirstOrDefault(unit => unit.Source.Id == queued.OverrideTarget.Id);
                    if (overridden == null && queued.OverrideTarget != null)
                    { overridden = new WorkingUnit(queued.OverrideTarget); units.Add(overridden); }
                    FireTriggers(actor, queued.Kind, fromQueue: true, paramInt: queued.ParamInt, overrideTarget: overridden);
                    queued.Unit = actor.Freeze();
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult ReturnQueuedSpawner(CombatUnit unit)
            {
                SettleDeadSpawner(new WorkingUnit(unit));
                return Finish(RoomOutcome.Exchanged);
            }

            private void BeginTeam(CombatTeam team)
            {
                foreach (WorkingUnit actor in units.Where(unit => unit.Alive && unit.Source.Team == team).ToArray())
                    FireTriggers(actor, "OnTeamTurnBegin");
            }

            private void Exchange()
            {
                // Native clears both prevention sets for each exchange, including relentless.
                cannotAttackOrHeal.Clear(); cannotFireTriggers.Clear();
                WorkingUnit[] quick = units.Where(unit => unit.Alive &&
                    unit.Source.Team == CombatTeam.Player && unit.Has("ambush")).ToArray();
                foreach (WorkingUnit unit in quick)
                {
                    Trigger(unit, "ambush", 1);
                    Turn(unit);
                }
                BeginTeam(CombatTeam.Enemy);
                foreach (WorkingUnit unit in units.Where(unit => unit.Source.Team == CombatTeam.Enemy).ToArray())
                    Turn(unit);
                BeginTeam(CombatTeam.Player);
                foreach (WorkingUnit unit in units.Where(unit => unit.Source.Team == CombatTeam.Player).ToArray())
                    if (!quick.Contains(unit)) Turn(unit);
            }

            private void Turn(WorkingUnit actor)
            {
                if (!actor.Alive) return;
                bool dazed = actor.Has("dazed") && Active(actor.Statuses["dazed"]);
                if (dazed)
                {
                    cannotAttackOrHeal.Add(actor.Source.Id); cannotFireTriggers.Add(actor.Source.Id);
                    Trigger(actor, "dazed", 1);
                    Emit("Dazed", actor, actor, 0);
                }
                // Native queues this after attack/trigger prevention, before attack conditions.
                // Ignored-silence triggers can run while dazed, without restoring this turn's attack.
                FireTriggers(actor, "OnTurnBegin", canFireTriggers: !dazed);
                if (!actor.Alive || dazed || !actor.Source.CanAttack || actor.Attack <= 0) return;
                int strikes = actor.Statuses.TryGetValue("multistrike", out CombatStatus? multi)
                    ? Math.Max(1, multi.ParamInt + multi.Stacks - 1) : 1;
                for (int strike = 0; strike < strikes && actor.Alive; strike++)
                {
                    WorkingUnit[] targets = units.Where(unit => unit.Alive && unit.Source.Team != actor.Source.Team &&
                        !unit.Has("stealth") && !unit.Has("untouchable")).ToArray();
                    if (targets.Length == 0) continue;
                    bool sweep = actor.Has("sweep");
                    if (!sweep) targets = new[] { actor.Has("sniper") ? targets.Last() : targets.First() };
                    if (strike > 0) Trigger(actor, "multistrike", 1);
                    // Sweep fixes its target list before damage. Each hit has its own native boss-kill preview.
                    if (sweep) runningTriggerQueue = true;
                    foreach (WorkingUnit target in targets)
                        if (target.Alive)
                        {
                            PreviewBossAttack(actor, target);
                            if (unsupportedReason != null) return;
                            // Native uses IsDestroyed here. Lethal retaliation during sweep
                            // leaves the attacker object available until the group is removed.
                            Damage(actor, target, actor.Despawned ? 0 : actor.Attack, "Attack");
                        }
                    if (sweep) DrainLocalTriggerQueue();
                }
            }

            private void PreviewBossAttack(WorkingUnit actor, WorkingUnit target)
            {
                if (source.Preview || context?.Statistics == null || !units.Any(unit => unit.Alive &&
                    (unit.Source.IsPyre || unit.Source.IsBoss == true || unit.Source.IsBoss == null && unit.Source.EndsBattleOnDeath))) return;
                var copied = new RoomCombatState(source.RoomIndex, source.Deployment, units.Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context, preview: true);
                var preview = new Engine(copied, new List<CombatEvent>());
                // A sweep preview shares the running-queue gate: its temporary callbacks
                // stay queued rather than healing or damaging between individual targets.
                preview.runningTriggerQueue = runningTriggerQueue;
                preview.Damage(preview.units.Single(unit => unit.Source.Id == actor.Source.Id),
                    preview.units.Single(unit => unit.Source.Id == target.Source.Id), actor.Attack, "Attack");
                if (preview.unsupportedReason != null) { unsupportedReason = "Boss kill preview: " + preview.unsupportedReason; return; }
                // Native restores characters, but CardStatistics is shared: queries can refresh its
                // membership and SetAttackDamageDealt survives, including nested retaliation.
                context = context.WithStatistics(preview.context!.Statistics);
                if (preview.units.Any(unit => !unit.Alive && (unit.Source.EndsBattleOnDeath || unit.Source.IsPyre) &&
                    units.Any(live => live.Alive && live.Source.Id == unit.Source.Id))) ClearTerminalCards();
            }

            private void Damage(WorkingUnit? actor, WorkingUnit target, int damage, string kind, int sourceCardId = 0, bool applyTraits = true)
            {
                if (target.LastAttackerId.HasValue) target.LastAttackerId = actor != null && !actor.Source.IsPyre ? actor.Source.Id : 0;
                int raw = damage;
                int blocked = 0;
                bool direct = kind == "Attack";
                if (direct && target.Has("melee weakness") && !target.Has("damage shield"))
                {
                    int stacks = target.Count("melee weakness");
                    damage = checked(damage * (stacks + 1));
                    if (damage != raw) Trigger(target, "melee weakness", stacks);
                }
                if (applyTraits)
                {
                    bool retainedLegacy = context?.CardRegistry == null && legacyDetachedCards != null;
                    CombatContext? scalingContext = retainedLegacy ? context!.WithCardRegistry(legacyDetachedCards) : context;
                    DamageScalingResult scaled = DamageScalingModel.Apply(scalingContext, actor?.Source.SpawnerCardId ?? sourceCardId, sourceCardId, damage);
                    if (!scaled.Supported) { unsupportedReason = scaled.UnsupportedReason; return; }
                    context = retainedLegacy ? scaled.Context!.WithCardRegistry(null) : scaled.Context; damage = scaled.Damage;
                }
                damage = Math.Max(0, damage);
                if (target.Has("pyregel"))
                {
                    damage = checked(damage + target.Amount("pyregel"));
                    Trigger(target, "pyregel", 1);
                }
                // The native shield stage checks card/relic piercing without an attacker argument.
                // Unit piercing bypasses armor, but does not bypass this shield stage.
                if (damage > 0 && target.Has("damage shield"))
                {
                    blocked += damage;
                    Trigger(target, "damage shield", 1);
                    damage = 0;
                }
                if (damage > 0 && target.Has("armor") && !(sourceCardId == 0 && (actor?.Has("piercing") ?? false)))
                {
                    int armor = target.Amount("armor");
                    int spent = Math.Min(target.Count("armor"), damage);
                    blocked += Math.Min(damage, armor);
                    damage = Math.Max(0, damage - armor);
                    Trigger(target, "armor", spent);
                }
                if (damage > 0 && target.Has("fragile")) damage = target.Health + damage - 1;
                if (target.Has("untouchable")) damage = 0;
                if (actor != null && context?.Statistics != null)
                    context = context.WithStatistics(context.Statistics.WithLastAttackDamage(checked(damage + blocked)));
                // Native fixes damage and resulting HP before firing the pre-damage
                // callback. Healing, armor or attack changes there do not recalculate it.
                int resultingHealth = Math.Max(0, target.Health - damage);
                if (direct && actor != null && actor != target)
                    FireTriggers(actor, "OnAttackingBeforeDamage", overrideTarget: target);
                if (unsupportedReason != null) return;
                target.Health = resultingHealth;
                if (!source.Preview && !target.Alive && target.Source.EndsBattleOnDeath)
                { battleWon = true; context = context?.WithBossesDead(); }
                Emit(kind, actor, target, damage);
                if (direct && actor != null && actor != target)
                    FireTriggers(actor, "OnAttacking", overrideTarget: target);
                // Native queues Slay before lifesteal and retaliation, for every damage type
                // with a character attacker, including damage from a character effect.
                if (actor != null && !actor.Despawned && !actor.Removed && !target.Alive) FireTriggers(actor, "OnKill");
                // Native lifesteal heals by unmodified attack, even against armor; it happens before spikes.
                // A dying sweep attacker has not been destroyed yet: its remaining
                // attacks still consume lifesteal, while Heal itself refuses revival.
                if (direct && actor != null && !actor.Despawned && !actor.Removed && actor.Has("lifesteal") && raw > 0)
                {
                    Trigger(actor, "lifesteal", 1);
                    Heal(actor, raw, "Lifesteal");
                }
                if (direct && actor != null && actor.Alive && target.Has("spikes"))
                {
                    int retaliation = target.Amount("spikes");
                    Trigger(target, "spikes", 1);
                    Damage(target, actor, retaliation, "Spikes");
                }
                if (target.DeathFinished) return;
                if (damage > 0 || blocked > 0) FireTriggers(target, "OnHit", paramInt: damage);
                if (!target.Alive && !target.DeathFinished)
                {
                    Death(actor, target, sourceCardId, deferRemoval: runningTriggerQueue);
                }
            }

            private readonly List<(WorkingUnit Unit, bool Return)> deferredDamageDeaths = new List<(WorkingUnit, bool)>();

            private void Death(WorkingUnit? actor, WorkingUnit target, int sourceCardId, bool deferRemoval = false)
            {
                if (target.DeathFinished) return;
                target.DeathFinished = true;
                bool deferReturn = deferRemoval && target.Source.Team == CombatTeam.Player && target.Source.SpawnerCardId > 0 && !DeferSpawner(target);
                Emit("Death", actor, target, 0);
                // Native UpdateHp sets this gate before death triggers are fired.
                if (!source.Preview && target.Source.EndsBattleOnDeath)
                { battleWon = true; context = context?.WithBossesDead(); }
                // CheckForDeath runs the terminal kill camera before dispatching death signals.
                if (!source.Preview && (target.Source.EndsBattleOnDeath || target.Source.IsPyre)) ClearTerminalCards();
                if (!source.Preview && context?.Statistics != null)
                {
                    int responsible = sourceCardId > 0 ? sourceCardId : target.Source.SpawnerCardId;
                    // Native has no IncrementStat call (and no cache refresh) for a null responsible card.
                    BattleStatistics statistics = responsible > 0 ? context.LiveStatistics! : context.Statistics;
                    context = context.WithStatistics(statistics.Death(target.Source.Team == CombatTeam.Player,
                        responsible, requireTrackedCard: context.CardInstances?.Count == 0));
                }
                if (enqueueCharacterTrigger != null) enqueueCharacterTrigger(new QueuedCharacterTrigger(source.RoomIndex, target.Freeze(),
                    returnSpawnerAfterQueue: deferReturn, deferUntilRemoval: deferRemoval));
                else
                {
                    if (deferRemoval) deferredDamageDeaths.Add((target, deferReturn));
                    else FireTriggers(target, "OnDeath");
                }
                if (!source.Preview && !deferReturn && !DeferSpawner(target)) SettleDeadSpawner(target);
                if (!deferRemoval) target.Removed = true;
            }

            private void SettleDeadSpawner(WorkingUnit unit)
            {
                if (source.Preview || unit.Source.Team != CombatTeam.Player || unit.Source.SpawnerCardId <= 0 || DetachedSpawner(unit)) return;
                if (context?.Statistics != null)
                    context = context.WithStatistics(context.LiveStatistics!.Increment(unit.Source.SpawnerCardId, "TimesExhausted",
                        requireTrackedCard: context.CardInstances?.Count == 0));
                RouteSpawner(unit);
            }

            private bool DeferSpawner(WorkingUnit unit) => deferSpawnerExhaustion ||
                pendingSummonCardId > 0 && unit.Source.SpawnerCardId == pendingSummonCardId;

            private bool DetachedSpawner(WorkingUnit unit) => context?.CardInstances != null &&
                !context.CardInstances.Any(card => card.InstanceId == unit.Source.SpawnerCardId) &&
                (context.KillCamActivated == true || context.CardRegistry?.Any(card => card.InstanceId == unit.Source.SpawnerCardId) == true);

            private void RouteSpawner(WorkingUnit unit)
            {
                if (unit.Source.Team != CombatTeam.Player || unit.Source.SpawnerCardId <= 0 || context?.OtherPiles == null) return;
                if (DetachedSpawner(unit)) return;
                CardPileState? standby = context.OtherPiles.FirstOrDefault(pile => pile.Name == "Standby");
                CardPileState? exhausted = context.OtherPiles.FirstOrDefault(pile => pile.Name == "Exhausted");
                int id = unit.Source.SpawnerCardId;
                CardToken? card = standby?.Cards.FirstOrDefault(item => item.InstanceId == id);
                if (card == null && exhausted?.Cards.Any(item => item.InstanceId == id) == true) return;
                // OnSpawn runs before its played card enters standby. Its discard callback later
                // allocates/removes that slot and applies the exhausted counter exactly once.
                if (card == null && id == pendingSummonCardId &&
                    context.OtherPiles.Any(pile => pile.Name == "DiscardBuffer" && pile.Cards.Any(item => item.InstanceId == id))) return;
                if (standby == null || exhausted == null || card == null)
                { unsupportedReason = "Missing dead unit spawner card routing."; return; }
                context = context.WithOtherPiles(context.OtherPiles.Select(pile => pile == standby ? CardPileModel.Remove(pile, id) :
                    pile == exhausted ? CardPileModel.Add(CardPileModel.Remove(pile, id), card) : pile).ToArray());
            }

            private void ClearTerminalCards()
            {
                // Native ShowKillCam clears the active card piles before StopCombat is entered.
                if (context == null || killCamActivated) return;
                killCamActivated = true;
                if (context.CardRegistry == null) legacyDetachedCards = context.CardInstances;
                context = new CombatContext(new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(),
                    Array.Empty<CardToken>(), context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions), context.BattleRng, context.Gold,
                    context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics,
                    context.CardInstances == null ? null : Array.Empty<CardInstanceState>(), context.CardRegistry, context.AllScenarioBossesDead,
                    context.NextAddedTemporaryUpgrades, context.OtherPiles?.Select(CardPileModel.Clear).ToArray(), context.QueryFrame,
                    context.KillCamActivated.HasValue ? true : (bool?)null, context.MagicPower, context.IsolatedBattlePreview);
            }

            private void PostCombat()
            {
                // Enemy post-combat runs before player post-combat, each front to back.
                foreach (WorkingUnit unit in units.OrderBy(unit => unit.Source.Team))
                {
                    if (!unit.Alive) continue;
                    if (!source.Deployment && unit.Has("regen"))
                    {
                        int amount = unit.Amount("regen");
                        Trigger(unit, "regen", 1);
                        Heal(unit, amount, "Regen");
                    }
                    if (unit.Alive && unit.Has("poison") && Active(unit.Statuses["poison"]))
                    {
                        int amount = unit.Amount("poison");
                        Trigger(unit, "poison", 1);
                        Damage(null, unit, amount, "Poison");
                    }
                }
            }

            private void Heal(WorkingUnit unit, int amount, string kind)
            {
                if (!unit.Alive || unit.Source.Modifiers?.CanBeHealed == false) return;
                int modified = HealingModel.ModifiedAmount(amount, unit.Statuses.Values.ToArray());
                if (modified < 0) return;
                int old = unit.Health;
                unit.Health += Math.Min(modified, unit.Source.MaxHealth - unit.Health);
                Emit(kind, unit, unit, unit.Health - old);
                // Native ApplyHeal runs these even when clipping or immunity produces zero restoration.
                FireTriggers(unit, "OnHeal", paramInt: unit.Health - old);
            }

            private void FireTriggers(WorkingUnit unit, string kind, bool canFireTriggers = true, bool fromQueue = false, int paramInt = 0,
                WorkingUnit? overrideTarget = null)
            {
                if (!fromQueue && enqueueCharacterTrigger != null)
                { enqueueCharacterTrigger(new QueuedCharacterTrigger(source.RoomIndex, unit.Freeze(), kind, paramInt: paramInt, overrideTarget: overrideTarget?.Freeze())); return; }
                if (!fromQueue && runningTriggerQueue)
                { triggerQueue.Enqueue((unit, kind, canFireTriggers, paramInt, overrideTarget)); return; }
                bool startedQueue = !runningTriggerQueue;
                runningTriggerQueue = true;
                ExecuteTriggers(unit, kind, canFireTriggers, paramInt, overrideTarget);
                if (!startedQueue) return;
                DrainLocalTriggerQueue();
            }

            private void DrainLocalTriggerQueue()
            {
                runningTriggerQueue = true;
                while (triggerQueue.Count > 0 && unsupportedReason == null)
                {
                    var queued = triggerQueue.Dequeue();
                    ExecuteTriggers(queued.Unit, queued.Kind, queued.CanFire, queued.ParamInt, queued.OverrideTarget);
                }
                var removing = deferredDamageDeaths.OrderBy(dead => dead.Unit.Source.Team).ThenBy(dead => dead.Unit.Source.Id).ToArray();
                deferredDamageDeaths.Clear();
                runningTriggerQueue = false;
                foreach (var dead in removing)
                {
                    FireTriggers(dead.Unit, "OnDeath");
                    if (dead.Return) SettleDeadSpawner(dead.Unit);
                    dead.Unit.Removed = true;
                }
            }

            private void ExecuteTriggers(WorkingUnit unit, string kind, bool canFireTriggers, int paramInt, WorkingUnit? overrideTarget)
            {
                if (kind == "OnDeath" && unit.Despawned) return;
                if (!unit.Alive && kind != "OnDeath" && (unit.Source.IsBoss == true || unit.Source.EndsBattleOnDeath)) return;
                for (int index = 0; index < unit.Triggers.Count; index++)
                {
                    CombatTrigger trigger = unit.Triggers[index];
                    if (trigger.Kind != kind || trigger.Once && trigger.HasTriggered ||
                        trigger.TriggerAtThreshold > 0 && paramInt < trigger.TriggerAtThreshold ||
                        source.Deployment && trigger.SkipDuringDeployment == true ||
                        !canFireTriggers && !trigger.IgnoreSilence ||
                        unit.Has("silenced") && !trigger.IgnoreSilence) continue;
                    // Removed native characters have no spawn point and skip trigger
                    // preflight, though their once flag can still be marked before abort.
                    if (!unit.Removed && !ActionTriggerPassesTest(unit, trigger)) continue;
                    var effects = trigger.Effects.ToArray();
                    // Native marks the trigger before its effects; nested death effects observe it.
                    unit.Triggers[index] = trigger.Fired(effects);
                    for (int fire = 0; fire < trigger.FireCount; fire++)
                    {
                        if (!unit.Alive && kind != "OnDeath" && kind != "OnHit" && kind != "OnKill") return;
                        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                        {
                            effects = unit.Triggers[index].Effects.ToArray();
                            CombatEffect effect = effects[effectIndex];
                            if (effect.Action != null)
                            {
                                if (!ApplyTriggeredAction(unit, effect, overrideTarget)) break;
                            }
                            else if (effect.UnitUpgrade != null)
                            {
                                if (!ApplyTriggeredUpgrade(unit, effect.UnitUpgrade, kind, overrideTarget)) break;
                            }
                            else if (effect.Type == "CardEffectDespawnCharacter")
                            {
                                int remaining = effect.Counter - 1;
                                effects[effectIndex] = effect.WithCounter(remaining);
                                unit.Triggers[index] = trigger.Fired(effects);
                                if (remaining <= 0 && unit.Alive)
                                {
                                    unit.Despawned = true; unit.Health = 0; Emit("Despawn", unit, unit, 0);
                                    if (!source.Preview && context?.Statistics != null && unit.Source.Team == CombatTeam.Player && !DeferSpawner(unit))
                                        context = context.WithStatistics(context.LiveStatistics!.Increment(unit.Source.SpawnerCardId, "TimesExhausted",
                                            requireTrackedCard: context.CardInstances?.Count == 0));
                                    if (!source.Preview) RouteSpawner(unit);
                                }
                            }
                            else if (effect.Type == "CardEffectRewardGold")
                            {
                                if (source.Preview) continue;
                                int reward = GoldRewardModel.Adjust(effect.Value);
                                context = new CombatContext(context!.Cards, context.BattleRng,
                                    Math.Max(0, checked(context.Gold + reward)), context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics,
                                    context.CardInstances, context.CardRegistry, context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades, context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview);
                                Emit("Gold", unit, unit, reward);
                            }
                            else if (effect.Type == "CardEffectAddBattleCard" && !source.Preview && !battleWon && context != null && context.AllScenarioBossesDead != true &&
                                (effect.Generation?.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize)) AddCards(unit, effect);
                        }
                        context = context?.AfterCardEffects();
                    }
                    unit.Triggers[index] = trigger.Fired(unit.Triggers[index].Effects);
                }
            }

            private RoomCombatState CurrentRoom() => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions, context, source.Preview);

            private RoomCombatState UpgradeRoom(WorkingUnit target) => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive || unit == target).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions, context, source.Preview);

            private bool ApplyUpgrade(WorkingUnit target, CardUpgradeModifier upgrade, string lifetime, bool remove,
                int? roomCapacity, int sourceCardId, string? kind)
            {
                RoomCombatResult result = UnitModifierModel.ApplyWithSettlement(UpgradeRoom(target), target.Source.Id, upgrade, lifetime,
                    remove, roomCapacity, sourceCardId, kind, (state, changed) =>
                    {
                        context = state.Context;
                        bool wasAlive = target.Alive;
                        target.Apply(changed);
                        if (wasAlive && !target.Alive) Death(null, target, 0);
                        // The native effect retains its target object through removal and
                        // terminal clearing. Its remaining source-card work must still run.
                        return new RoomCombatResult(UpgradeRoom(target), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
                    }, allowDyingTarget: true);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.State!.Context;
                return true;
            }

            private CardTargets TriggerTargets(WorkingUnit actor, CardActionEffect action, bool testing, WorkingUnit? overrideTarget = null)
            {
                if (action.Target == "LastAttackedCharacter")
                {
                    // The override bypasses every team/status/health/subtype/boss filter,
                    // including for a retained dying victim. Trigger preflight has no override.
                    if (overrideTarget != null) return new CardTargets(new[] { overrideTarget.Source.Id });
                    WorkingUnit[] candidates = units.Where(unit => !unit.Removed &&
                        (unit.Source.Team == CombatTeam.Enemy ? action.AllowEnemy : action.AllowPlayer)).OrderBy(unit => unit.Source.Team).ToArray();
                    if (candidates.Any(unit => !unit.LastAttackerId.HasValue))
                        return new CardTargets(Array.Empty<int>(), "Last-attacked targeting requires captured attacker relationships.");
                    return new CardTargets(candidates.Where(unit => unit.LastAttackerId == actor.Source.Id).Select(unit => unit.Source.Id).ToArray());
                }
                if (action.Target != "Self") return CardTargetModel.Collect(CurrentRoom(), action, Array.Empty<int>(), isTesting: testing || source.Preview);
                // Native Self bypasses team, health, status, subtype and untouchable filters.
                if (action.Filters?.IgnoreBosses == true)
                {
                    if (!actor.Source.IsBoss.HasValue) return new CardTargets(Array.Empty<int>(), "Self boss filtering requires boss state.");
                    if (actor.Source.IsBoss.Value) return new CardTargets(Array.Empty<int>());
                }
                return new CardTargets(new[] { actor.Source.Id });
            }

            private bool ActionTriggerPassesTest(WorkingUnit actor, CombatTrigger trigger)
            {
                if (!trigger.Effects.Any(effect => effect.UnitUpgrade != null || effect.Action != null)) return true;
                bool passed = trigger.Effects.Count == 0;
                foreach (CombatEffect effect in trigger.Effects)
                {
                    CardActionEffect? action = effect.UnitUpgrade ?? effect.Action;
                    if (action == null)
                    {
                        passed |= effect.Type != "CardEffectAddBattleCard" || !source.Preview &&
                            context!.AllScenarioBossesDead != true &&
                            (effect.Generation?.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize);
                        continue;
                    }
                    if (action.Tests?.ShouldTest == false) continue;
                    CardTargets targets = TriggerTargets(actor, action, testing: true);
                    if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                    if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                    bool valid = action.Type == "AddStatus" ? StatusTest(actor, effect, targets) : ActionTestValid(action, targets, TestActionAmount(action));
                    if (!valid && action.Tests?.FailToCast == true) return false;
                    passed |= valid;
                }
                return passed;
            }

            private bool ApplyTriggeredUpgrade(WorkingUnit actor, CardActionEffect action, string kind, WorkingUnit? overrideTarget)
            {
                CardTargets tested = TriggerTargets(actor, action, testing: true, overrideTarget);
                if (!tested.Supported) { unsupportedReason = tested.UnsupportedReason; return false; }
                if (tested.BattleRng.HasValue) context = context!.WithBattleRng(tested.BattleRng.Value);
                if (action.Type != "RemoveUnitUpgrade" && tested.UnitIds.Count == 0)
                    return action.Tests?.CancelSubsequent != true;
                CardTargets targets = TriggerTargets(actor, action, testing: false, overrideTarget);
                if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                foreach (int targetId in targets.UnitIds)
                {
                    WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == targetId && (unit.Alive || action.Target == "Self" && unit == actor || action.Target == "LastAttackedCharacter"));
                    if (target == null) continue;
                    if (!ApplyUpgrade(target, action.Upgrade!, action.Lifetime, action.Type == "RemoveUnitUpgrade",
                        null, actor.Source.SpawnerCardId, kind)) return false;
                    Emit(action.Type, actor, target, 0);
                }
                return true;
            }

            private static bool ActionTestValid(CardActionEffect action, CardTargets targets, int amount = 0) =>
                action.Type == "Damage" ? amount >= 0 && (action.Range == null || action.Range.Max > 0) &&
                    (action.Target != "DropTargetCharacter" || targets.UnitIds.Count > 0) :
                action.Type == "RemoveUnitUpgrade" || action.Type == "Heal" && action.Target == "Room" || targets.UnitIds.Count > 0;

            private int SampleActionAmount(CardActionEffect action)
            {
                if (action.Range == null) return action.Value;
                RngDraw draw = action.Range.Sample(context!.BattleRng);
                context = context.WithBattleRng(draw.State); return draw.Value;
            }

            private int TestActionAmount(CardActionEffect action) => action.Type == "Damage" ? SampleActionAmount(action) : action.Value;

            private bool ApplyTriggeredAction(WorkingUnit actor, CombatEffect effect, WorkingUnit? overrideTarget)
            {
                CardActionEffect action = effect.Action!;
                CardTargets tested = TriggerTargets(actor, action, testing: true, overrideTarget);
                if (!tested.Supported) { unsupportedReason = tested.UnsupportedReason; return false; }
                if (tested.BattleRng.HasValue) context = context!.WithBattleRng(tested.BattleRng.Value);
                bool valid = action.Type == "AddStatus" ? StatusTest(actor, effect, tested) : ActionTestValid(action, tested, TestActionAmount(action));
                if (unsupportedReason != null) return false;
                if (!valid) return action.Tests?.CancelSubsequent != true;
                CardTargets targets = TriggerTargets(actor, action, testing: false, overrideTarget);
                if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                if (action.Type == "AddStatus")
                {
                    RoomCombatResult applied = TriggeredStatusModel.Apply(RetainedStatusRoom(actor, targets.UnitIds), actor.Source.Id, effect, targets.UnitIds);
                    if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                    context = applied.State!.Context;
                    foreach (CombatUnit changed in applied.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                    foreach (CombatEvent item in applied.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                    return true;
                }
                int amount = SampleActionAmount(action);
                // Native samples once after collection, including an empty Room target set.
                if (action.Type == "Damage" && effect.DamageStatusMultiplier != null)
                    amount = unchecked(amount * actor.Count(effect.DamageStatusMultiplier));
                foreach (int targetId in targets.UnitIds)
                {
                    WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == targetId);
                    if (target == null) continue;
                    if (action.Type == "Heal") Heal(target, amount, "TriggeredHeal");
                    // Effect-state parameters have no parent card, but CharacterState.ApplyEffects
                    // passes the spawner as playedCard for damage traits and death attribution.
                    else if (target.Alive && !target.Removed) Damage(actor, target, amount, "TriggeredDamage", actor.Source.SpawnerCardId);
                }
                return true;
            }

            private RoomCombatState RetainedStatusRoom(WorkingUnit actor, IReadOnlyList<int> targets) =>
                new RoomCombatState(source.RoomIndex, source.Deployment,
                    units.Where(unit => unit.Alive || unit == actor || targets.Contains(unit.Source.Id)).Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context, source.Preview);

            private bool StatusTest(WorkingUnit actor, CombatEffect effect, CardTargets targets)
            {
                CombatStatus? selected = null;
                if (source.Preview && context?.IsolatedBattlePreview == true && effect.Action!.Statuses.Count > 1)
                {
                    RngDraw choice = context.BattleRng.Range(0, effect.Action.Statuses.Count);
                    context = context.WithBattleRng(choice.State); selected = effect.Action.Statuses[choice.Value];
                }
                bool result = TriggeredStatusModel.Test(RetainedStatusRoom(actor, targets.UnitIds), effect, targets.UnitIds, out string? error, selected);
                if (error != null) unsupportedReason = error;
                return result;
            }

            private void AddCards(WorkingUnit actor, CombatEffect effect)
            {
                CardGenerationRule rule = effect.Generation ?? new CardGenerationRule(effect.Destination, effect.Count,
                    effect.CardPool.Select(id => new CardCreationRule(id, CardModifiers.Empty(), null, Array.Empty<string>())).ToArray(), effect.SkipDuplicateInHand);
                CardGenerationResult result = CardGenerationModel.Apply(context!, rule, actor.Source.SpawnerCardId);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return; }
                context = result.Context;
                foreach (CardToken card in result.AddedCards) Emit("AddCard", actor, actor, card.InstanceId);
            }

            private void Trigger(WorkingUnit unit, string id, int count)
            {
                if (unit.Statuses.TryGetValue(id, out CombatStatus? status) && status.RemoveWhenTriggered &&
                    (!source.Deployment || status.RemoveDuringDeployment)) unit.Remove(id, count);
            }

            private bool Active(CombatStatus status) => !source.Deployment || !status.SkipDuringDeployment;

            private void Clear(bool after, bool relentless)
            {
                if (source.Deployment) return;
                foreach (WorkingUnit unit in units.Where(unit => unit.Alive))
                    foreach (CombatStatus status in unit.Statuses.Values.ToArray())
                        if (status.RemoveAfterPostCombat == after)
                        {
                            if (status.RemoveStackAtEnd && !(relentless && status.PreventRemovalDuringRelentless))
                                unit.Remove(status.Id, 1);
                            else if (!status.RemoveStackAtEnd && status.RemoveAllAtEnd)
                                unit.Remove(status.Id, status.Stacks);
                        }
            }

            private bool BothTeamsPresent() => units.Any(unit => unit.Alive && unit.Source.Team == CombatTeam.Player &&
                !unit.Has("untouchable")) && units.Any(unit => unit.Alive && unit.Source.Team == CombatTeam.Enemy &&
                !unit.Has("untouchable"));

            private string Signature()
            {
                var text = new StringBuilder();
                foreach (WorkingUnit unit in units)
                {
                    text.Append(unit.Source.Id).Append(':').Append(unit.Health).Append(':').Append(unit.Source.BaseAttack)
                        .Append(':').Append(unit.Source.MaxHealth).Append(':').Append(unit.Source.Size).Append(':');
                    text.Append(unit.LastAttackerId).Append(':').Append(unit.Removed).Append(':');
                    if (unit.Source.Modifiers != null)
                    {
                        UnitModifiers modifiers = unit.Source.Modifiers;
                        text.Append(modifiers.AttackDamage).Append(',').Append(modifiers.AttackDamageAdded).Append(',')
                            .Append(modifiers.DamageBuff).Append(',').Append(modifiers.RawSize).Append(',').Append(modifiers.EquipmentLimit).Append(';');
                        foreach (CardUpgradeModifier upgrade in modifiers.Upgrades)
                            text.Append(upgrade.DataId.Length).Append(':').Append(upgrade.DataId).Append(':')
                                .Append(upgrade.Stats.Damage).Append(',').Append(upgrade.Stats.Health).Append(',')
                                .Append(upgrade.Stats.Size).Append(',').Append(upgrade.UnhealedHealth).Append(',').Append(upgrade.DamageBuff).Append(';');
                    }
                    foreach (CombatStatus status in unit.Statuses.Values.OrderBy(status => status.Id, StringComparer.Ordinal))
                        text.Append(status.Id).Append('=').Append(status.Stacks).Append(',');
                    text.Append(';');
                    foreach (CombatTrigger trigger in unit.Triggers)
                    {
                        text.Append(trigger.Kind).Append(':').Append(trigger.HasTriggered).Append(':');
                        foreach (CombatEffect effect in trigger.Effects) text.Append(effect.Value).Append(':').Append(effect.Counter).Append(',');
                    }
                }
                if (context != null)
                {
                    text.Append(context.Gold).Append(':').Append(context.NextCardId).Append(':')
                        .Append(context.BattleRng.S0).Append(',').Append(context.BattleRng.S1).Append(',')
                        .Append(context.BattleRng.S2).Append(',').Append(context.BattleRng.S3);
                    foreach (CardToken card in context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard))
                        text.Append('|').Append(card.InstanceId).Append(':').Append(card.DataId);
                    foreach (CardInstanceState card in context.CardInstances ?? Array.Empty<CardInstanceState>())
                    {
                        text.Append("|card:").Append(card.InstanceId);
                        AppendModifiers(card.Permanent); AppendModifiers(card.Temporary);
                    }
                    foreach (CardInstanceState card in context.CardRegistry ?? Array.Empty<CardInstanceState>())
                    {
                        text.Append("|reference:").Append(card.InstanceId);
                        AppendModifiers(card.Permanent); AppendModifiers(card.Temporary);
                    }
                    if (context.Statistics != null) text.Append("|stats:").Append(context.Statistics.Signature());
                }
                return text.ToString();

                void AppendModifiers(CardModifiers modifiers)
                {
                    text.Append('[').Append(modifiers.PersistentHealth);
                    foreach (CardUpgradeModifier upgrade in modifiers.Upgrades)
                        text.Append('|').Append(upgrade.DataId.Length).Append(':').Append(upgrade.DataId).Append(':')
                            .Append(upgrade.Stats.Damage).Append(',').Append(upgrade.Stats.Health).Append(',')
                            .Append(upgrade.Stats.Size).Append(',').Append(upgrade.UnhealedHealth).Append(',').Append(upgrade.DamageBuff);
                    text.Append(']');
                }
            }

            private void Emit(string kind, WorkingUnit? actor, WorkingUnit target, int amount) =>
                events.Add(new CombatEvent(round, kind, actor?.Source.Id ?? 0, target.Source.Id, amount));

            private RoomCombatResult Finish(RoomOutcome outcome) => unsupportedReason != null
                ? new RoomCombatResult(null, RoomOutcome.Unsupported, round, events, unsupportedReason) : new RoomCombatResult(
                new RoomCombatState(source.RoomIndex, source.Deployment,
                    units.Where(unit => unit.Alive).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions, context, source.Preview),
                outcome, round, events);
        }
    }
}
