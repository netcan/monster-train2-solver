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

        public CombatStatus(string id, int stacks, int paramInt = 0,
            bool removeWhenTriggered = false, bool removeStackAtEnd = false,
            bool removeAllAtEnd = false, bool removeAfterPostCombat = false,
            bool preventRemovalDuringRelentless = false, bool skipDuringDeployment = false,
            bool removeDuringDeployment = false)
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
        }

        internal CombatStatus WithStacks(int stacks) => new CombatStatus(Id, stacks, ParamInt,
            RemoveWhenTriggered, RemoveStackAtEnd, RemoveAllAtEnd, RemoveAfterPostCombat,
            PreventRemovalDuringRelentless, SkipDuringDeployment, RemoveDuringDeployment);
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
        public int Attacks => Math.Max(1, Status("multistrike") is CombatStatus multi
            ? multi.ParamInt + multi.Stacks - 1 : 1);

        public CombatUnit(int id, string assetKey, CombatTeam team, int baseAttack,
            int health, int maxHealth, bool canAttack, bool isPyre, bool endsBattleOnDeath,
            IReadOnlyList<CombatStatus> statuses, IReadOnlyList<CombatTrigger>? triggers = null, int spawnerCardId = 0, int size = 0,
            IReadOnlyList<string>? statusImmunities = null, IReadOnlyList<string>? subtypes = null, UnitModifiers? modifiers = null)
        {
            Id = id;
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
        }

        internal CombatStatus? Status(string id) => Statuses.FirstOrDefault(item => item.Id == id);
        internal int StatusAmount(string id) => Status(id) is CombatStatus status
            ? status.Stacks * status.ParamInt : 0;
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
        private static readonly HashSet<string> KnownStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "armor", "damage shield", "dazed", "stealth", "ambush", "multistrike",
            "spikes", "lifesteal", "fragile", "piercing", "immune", "immobile",
            "relentless", "sweep", "sniper", "rooted", "haste", "untouchable",
            "buff", "debuff", "regen", "poison", "melee weakness", "silenced", "valor", "pyregel",
            "heal multiplier", "heal immunity"
        };

        public static RoomCombatResult Exchange(RoomCombatState state) => Run(state, false);
        public static RoomCombatResult Resolve(RoomCombatState state) => Run(state, true);

        public static RoomCombatResult ApplyCardDamage(RoomCombatState state, int targetId, int damage, int sourceCardId = 0)
        {
            string? error = Validate(state);
            if (error != null || damage < 0 || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid card damage target or amount.");
            return new Engine(state, new List<CombatEvent>()).CardDamage(targetId, damage, sourceCardId);
        }

        internal static RoomCombatResult ApplyUnitModification(RoomCombatState state, CombatUnit changed) =>
            new Engine(state, new List<CombatEvent>()).ModifyUnit(changed);

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

        internal static string? Validate(RoomCombatState state)
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
                if (unit.Health <= 0 || unit.Health > unit.MaxHealth)
                    return "Only living units with valid health can enter room combat.";
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
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "PostCombat" && trigger.Kind != "OnHeal")
                        return "Unmodeled trigger " + trigger.Kind;
                    if (trigger.Kind == "OnHeal" && trigger.SkipDuringDeployment == null)
                        return "OnHeal requires deployment timing state.";
                    if (trigger.FireCount < 0) return "Invalid trigger fire count.";
                    foreach (CombatEffect effect in trigger.Effects)
                    {
                        if (effect.Type == "CardEffectDespawnCharacter") continue;
                        if (effect.Type != "CardEffectRewardGold" && effect.Type != "CardEffectAddBattleCard")
                            return "Unmodeled effect " + effect.Type;
                        if (state.Context == null) return "Unit effects require shared battle context.";
                        if (effect.Type == "CardEffectRewardGold" && effect.Value < 0)
                            return "Conditional gold costs are not modeled.";
                        if (effect.Type == "CardEffectAddBattleCard" &&
                            (effect.CardPool.Count == 0 || !new[] { "DeckPile", "DeckPileTop", "DeckPileRandom",
                                "HandPile", "DiscardPile" }.Contains(effect.Destination)))
                            return "Unmodeled generated card destination or empty pool.";
                    }
                }
            }
            return null;
        }

        private sealed class WorkingUnit
        {
            internal readonly CombatUnit Source;
            internal int Health;
            internal readonly Dictionary<string, CombatStatus> Statuses;
            internal readonly List<CombatTrigger> Triggers;
            internal bool Despawned;
            internal bool Alive => Health > 0;
            internal int Attack => Math.Max(0, Source.BaseAttack + Amount("buff") + Amount("valor") - Amount("debuff"));

            internal WorkingUnit(CombatUnit source)
            {
                Source = source; Health = source.Health;
                Statuses = source.Statuses.ToDictionary(status => status.Id, StringComparer.Ordinal);
                Triggers = source.Triggers.ToList();
            }

            internal bool Has(string id) => Statuses.ContainsKey(id);
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
                Source.EndsBattleOnDeath, Statuses.Values.ToArray(), Triggers, Source.SpawnerCardId, Source.Size, Source.StatusImmunities, Source.Subtypes, Source.Modifiers);
        }

        private sealed class Engine
        {
            private readonly RoomCombatState source;
            private readonly List<WorkingUnit> units;
            private readonly List<CombatEvent> events;
            private bool battleWon;
            private int round;
            private CombatContext? context;

            internal Engine(RoomCombatState source, List<CombatEvent> events)
            {
                this.source = source; this.events = events;
                units = source.Units.Select(unit => new WorkingUnit(unit)).ToList();
                if (source.Preview)
                    foreach (WorkingUnit unit in units)
                        for (int index = 0; index < unit.Triggers.Count; index++) unit.Triggers[index] = unit.Triggers[index].ForPreview();
                context = source.Context;
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
                foreach (WorkingUnit unit in units.OrderBy(unit => unit.Source.Team).ToArray())
                    if (unit.Alive) FireTriggers(unit, "PostCombat");
                if (!source.Deployment)
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                    {
                        WorkingUnit? front = units.FirstOrDefault(unit => unit.Alive && unit.Source.Team == team && !unit.Has("untouchable"));
                        if (front == null || !front.Has("valor") || front.Has("immune") || front.Source.StatusImmunities.Contains("armor")) continue;
                        int goal = Math.Min(9999, front.Amount("valor"));
                        if (goal > front.Count("armor")) front.Statuses["armor"] = context!.StatusRules.First(rule => rule.Id == "armor").WithStacks(goal);
                    }
                Clear(true, relentless);
                return Finish(battleWon ? RoomOutcome.BattleWon : RoomOutcome.Cleared);
            }

            internal RoomCombatResult CardDamage(int targetId, int damage, int sourceCardId)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Damage(null, target, damage, "Spell", sourceCardId);
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

            internal RoomCombatResult CardHeal(int targetId, int amount)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Heal(target, amount, "SpellHeal");
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            private void Exchange()
            {
                WorkingUnit[] quick = units.Where(unit => unit.Alive &&
                    unit.Source.Team == CombatTeam.Player && unit.Has("ambush")).ToArray();
                foreach (WorkingUnit unit in quick)
                {
                    Trigger(unit, "ambush", 1);
                    Turn(unit);
                }
                foreach (WorkingUnit unit in units.Where(unit => unit.Source.Team == CombatTeam.Enemy).ToArray())
                    Turn(unit);
                foreach (WorkingUnit unit in units.Where(unit => unit.Source.Team == CombatTeam.Player).ToArray())
                    if (!quick.Contains(unit)) Turn(unit);
            }

            private void Turn(WorkingUnit actor)
            {
                if (!actor.Alive) return;
                if (actor.Has("dazed") && Active(actor.Statuses["dazed"]))
                {
                    Trigger(actor, "dazed", 1);
                    Emit("Dazed", actor, actor, 0);
                    return;
                }
                if (!actor.Source.CanAttack || actor.Attack <= 0) return;
                int strikes = actor.Statuses.TryGetValue("multistrike", out CombatStatus? multi)
                    ? Math.Max(1, multi.ParamInt + multi.Stacks - 1) : 1;
                for (int strike = 0; strike < strikes && actor.Alive; strike++)
                {
                    WorkingUnit[] targets = units.Where(unit => unit.Alive && unit.Source.Team != actor.Source.Team &&
                        !unit.Has("stealth") && !unit.Has("untouchable")).ToArray();
                    if (targets.Length == 0) continue;
                    if (!actor.Has("sweep")) targets = new[] { actor.Has("sniper") ? targets.Last() : targets.First() };
                    if (strike > 0) Trigger(actor, "multistrike", 1);
                    // Sweep fixes its target list before damage; a retaliatory death must not hit later targets.
                    foreach (WorkingUnit target in targets)
                        if (target.Alive) Damage(actor, target, actor.Alive ? actor.Attack : 0, "Attack");
                }
            }

            private void Damage(WorkingUnit? actor, WorkingUnit target, int damage, string kind, int sourceCardId = 0)
            {
                int raw = damage;
                int blocked = 0;
                bool direct = kind == "Attack";
                if (direct && target.Has("melee weakness") && !target.Has("damage shield"))
                {
                    int stacks = target.Count("melee weakness");
                    damage = checked(damage * (stacks + 1));
                    if (damage != raw) Trigger(target, "melee weakness", stacks);
                }
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
                if (damage > 0 && target.Has("armor") && !(actor?.Has("piercing") ?? false))
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
                target.Health = Math.Max(0, target.Health - damage);
                Emit(kind, actor, target, damage);
                // Native lifesteal heals by unmodified attack, even against armor; it happens before spikes.
                if (direct && actor != null && actor.Alive && actor.Has("lifesteal") && raw > 0)
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
                if (!target.Alive)
                {
                    Death(actor, target, sourceCardId);
                }
            }

            private void Death(WorkingUnit? actor, WorkingUnit target, int sourceCardId)
            {
                Emit("Death", actor, target, 0);
                // Native UpdateHp sets this gate before death triggers are fired.
                if (!source.Preview && target.Source.EndsBattleOnDeath)
                { battleWon = true; context = context?.WithBossesDead(); }
                FireTriggers(target, "OnDeath");
                if (!source.Preview && context?.Statistics != null)
                    context = context.WithStatistics(context.LiveStatistics!.Death(target.Source.Team == CombatTeam.Player,
                        sourceCardId > 0 ? sourceCardId : target.Source.SpawnerCardId, requireTrackedCard: context.CardInstances?.Count == 0)
                        .Increment(target.Source.Team == CombatTeam.Player ? target.Source.SpawnerCardId : 0, "TimesExhausted",
                            requireTrackedCard: context.CardInstances?.Count == 0));
                if (!source.Preview && (target.Source.EndsBattleOnDeath || target.Source.IsPyre)) ClearTerminalCards();
            }

            private void ClearTerminalCards()
            {
                // Native ShowKillCam clears the active card piles before StopCombat is entered.
                if (context == null) return;
                context = new CombatContext(new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(),
                    Array.Empty<CardToken>(), context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions), context.BattleRng, context.Gold,
                    context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics,
                    context.CardInstances == null ? null : Array.Empty<CardInstanceState>(), context.CardRegistry, context.AllScenarioBossesDead);
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
                FireTriggers(unit, "OnHeal");
            }

            private void FireTriggers(WorkingUnit unit, string kind)
            {
                if (kind == "OnDeath" && unit.Despawned) return;
                for (int index = 0; index < unit.Triggers.Count; index++)
                {
                    CombatTrigger trigger = unit.Triggers[index];
                    if (trigger.Kind != kind || trigger.Once && trigger.HasTriggered ||
                        source.Deployment && trigger.SkipDuringDeployment == true ||
                        unit.Has("silenced") && !trigger.IgnoreSilence) continue;
                    var effects = trigger.Effects.ToArray();
                    for (int fire = 0; fire < trigger.FireCount; fire++)
                    {
                        if (!unit.Alive && kind != "OnDeath") break;
                        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                        {
                            CombatEffect effect = effects[effectIndex];
                            if (effect.Type == "CardEffectDespawnCharacter")
                            {
                                int remaining = effect.Counter - 1;
                                effects[effectIndex] = effect.WithCounter(remaining);
                                if (remaining <= 0 && unit.Alive)
                                {
                                    unit.Despawned = true; unit.Health = 0; Emit("Despawn", unit, unit, 0);
                                    if (!source.Preview && context?.Statistics != null && unit.Source.Team == CombatTeam.Player)
                                        context = context.WithStatistics(context.LiveStatistics!.Increment(unit.Source.SpawnerCardId, "TimesExhausted",
                                            requireTrackedCard: context.CardInstances?.Count == 0));
                                }
                            }
                            else if (effect.Type == "CardEffectRewardGold")
                            {
                                if (source.Preview) continue;
                                int reward = GoldRewardModel.Adjust(effect.Value);
                                context = new CombatContext(context!.Cards, context.BattleRng,
                                    Math.Max(0, checked(context.Gold + reward)), context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics,
                                    context.CardInstances, context.CardRegistry, context.AllScenarioBossesDead);
                                Emit("Gold", unit, unit, reward);
                            }
                            else if (effect.Type == "CardEffectAddBattleCard" && !battleWon && context?.AllScenarioBossesDead != true) AddCards(unit, effect);
                        }
                    }
                    unit.Triggers[index] = trigger.Fired(effects);
                }
            }

            private void AddCards(WorkingUnit actor, CombatEffect effect)
            {
                CombatContext current = context!;
                var hand = current.Cards.Hand.ToList(); var draw = current.Cards.Draw.ToList();
                var discard = current.Cards.Discard.ToList();
                UnityRng rng = current.BattleRng;
                int nextId = current.NextCardId;
                for (int index = 0; index < Math.Max(1, effect.Count); index++)
                {
                    RngDraw selected = rng.Range(0, effect.CardPool.Count);
                    rng = selected.State;
                    string dataId = effect.CardPool[selected.Value];
                    if (effect.Destination == "HandPile" && (hand.Count >= current.MaxHandSize ||
                        effect.SkipDuplicateInHand && hand.Any(card => card.DataId == dataId))) continue;
                    var card = new CardToken(nextId++, dataId);
                    switch (effect.Destination)
                    {
                        case "HandPile": hand.Insert(0, card); break;
                        case "DiscardPile": discard.Insert(0, card); break;
                        case "DeckPileTop": draw.Add(card); break;
                        case "DeckPileRandom":
                            RngDraw placement = rng.Range(0, draw.Count); rng = placement.State;
                            draw.Insert(placement.Value, card); break;
                        default: draw.Insert(0, card); break;
                    }
                    Emit("AddCard", actor, actor, card.InstanceId);
                }
                context = new CombatContext(new CardCycleState(hand, draw, discard, current.Cards.Rng,
                    current.Cards.DrawModifier, current.Cards.ExternalInteractions), rng, current.Gold,
                    nextId, current.MaxHandSize, current.StatusRules,
                    current.Statistics?.TrackCards(hand.Concat(draw).Concat(discard).Select(card => card.InstanceId)),
                    current.CardInstances?.Concat(hand.Concat(draw).Concat(discard).Where(card => card.InstanceId >= current.NextCardId)
                        .Select(card => CardInstanceState.Empty(card.InstanceId, card.DataId))).ToArray(), current.CardRegistry, current.AllScenarioBossesDead);
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
                    text.Append(unit.Source.Id).Append(':').Append(unit.Health).Append(':');
                    foreach (CombatStatus status in unit.Statuses.Values.OrderBy(status => status.Id, StringComparer.Ordinal))
                        text.Append(status.Id).Append('=').Append(status.Stacks).Append(',');
                    text.Append(';');
                    foreach (CombatTrigger trigger in unit.Triggers)
                    {
                        text.Append(trigger.Kind).Append(':').Append(trigger.HasTriggered).Append(':');
                        foreach (CombatEffect effect in trigger.Effects) text.Append(effect.Counter).Append(',');
                    }
                }
                if (context != null)
                {
                    text.Append(context.Gold).Append(':').Append(context.NextCardId).Append(':')
                        .Append(context.BattleRng.S0).Append(',').Append(context.BattleRng.S1).Append(',')
                        .Append(context.BattleRng.S2).Append(',').Append(context.BattleRng.S3);
                    foreach (CardToken card in context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard))
                        text.Append('|').Append(card.InstanceId).Append(':').Append(card.DataId);
                    if (context.Statistics != null) text.Append("|stats:").Append(context.Statistics.Signature());
                }
                return text.ToString();
            }

            private void Emit(string kind, WorkingUnit? actor, WorkingUnit target, int amount) =>
                events.Add(new CombatEvent(round, kind, actor?.Source.Id ?? 0, target.Source.Id, amount));

            private RoomCombatResult Finish(RoomOutcome outcome) => new RoomCombatResult(
                new RoomCombatState(source.RoomIndex, source.Deployment,
                    units.Where(unit => unit.Alive).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions, context, source.Preview),
                outcome, round, events);
        }
    }
}
