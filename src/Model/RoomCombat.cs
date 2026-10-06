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
        public int Attack => Math.Max(0, BaseAttack + StatusAmount("buff") - StatusAmount("debuff"));
        public int Health { get; }
        public int MaxHealth { get; }
        public bool CanAttack { get; }
        public bool IsPyre { get; }
        public bool EndsBattleOnDeath { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public int Attacks => Math.Max(1, Status("multistrike") is CombatStatus multi
            ? multi.ParamInt + multi.Stacks - 1 : 1);

        public CombatUnit(int id, string assetKey, CombatTeam team, int baseAttack,
            int health, int maxHealth, bool canAttack, bool isPyre, bool endsBattleOnDeath,
            IReadOnlyList<CombatStatus> statuses)
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

        // Units are ordered front to back within each team. Identity is separate from asset name.
        public RoomCombatState(int roomIndex, bool deployment, IReadOnlyList<CombatUnit> units,
            IReadOnlyList<string> externalInteractions)
        {
            RoomIndex = roomIndex;
            Deployment = deployment;
            Units = Array.AsReadOnly(units.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
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
            "buff", "debuff", "regen", "poison", "melee weakness"
        };

        public static RoomCombatResult Exchange(RoomCombatState state) => Run(state, false);
        public static RoomCombatResult Resolve(RoomCombatState state) => Run(state, true);

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
            if (state.Units.Select(unit => unit.Id).Distinct().Count() != state.Units.Count)
                return "Unit instance IDs must be unique.";
            foreach (CombatUnit unit in state.Units)
            {
                if (unit.Health <= 0 || unit.Health > unit.MaxHealth)
                    return "Only living units with valid health can enter room combat.";
                if (unit.Statuses.Select(status => status.Id).Distinct().Count() != unit.Statuses.Count)
                    return "Duplicate status IDs on unit " + unit.Id;
                foreach (CombatStatus status in unit.Statuses)
                    if (!KnownStatuses.Contains(status.Id))
                        return "Unmodeled status " + status.Id + " on " + unit.AssetKey;
            }
            return null;
        }

        private sealed class WorkingUnit
        {
            internal readonly CombatUnit Source;
            internal int Health;
            internal readonly Dictionary<string, CombatStatus> Statuses;
            internal bool Alive => Health > 0;
            internal int Attack => Math.Max(0, Source.BaseAttack + Amount("buff") - Amount("debuff"));

            internal WorkingUnit(CombatUnit source)
            {
                Source = source; Health = source.Health;
                Statuses = source.Statuses.ToDictionary(status => status.Id, StringComparer.Ordinal);
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
                Source.EndsBattleOnDeath, Statuses.Values.ToArray());
        }

        private sealed class Engine
        {
            private readonly RoomCombatState source;
            private readonly List<WorkingUnit> units;
            private readonly List<CombatEvent> events;
            private bool battleWon;
            private int round;

            internal Engine(RoomCombatState source, List<CombatEvent> events)
            {
                this.source = source; this.events = events;
                units = source.Units.Select(unit => new WorkingUnit(unit)).ToList();
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
                    if (units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    PostCombat();
                    if (battleWon) return Finish(RoomOutcome.BattleWon);
                    if (units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    Clear(false, relentless);
                } while (relentless && BothTeamsPresent());
                Clear(true, relentless);
                return Finish(battleWon ? RoomOutcome.BattleWon : RoomOutcome.Cleared);
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

            private void Damage(WorkingUnit? actor, WorkingUnit target, int damage, string kind)
            {
                int raw = damage;
                bool direct = kind == "Attack";
                if (direct && target.Has("melee weakness") && !target.Has("damage shield"))
                {
                    int stacks = target.Count("melee weakness");
                    damage = checked(damage * (stacks + 1));
                    if (damage != raw) Trigger(target, "melee weakness", stacks);
                }
                // The native shield stage checks card/relic piercing without an attacker argument.
                // Unit piercing bypasses armor, but does not bypass this shield stage.
                if (damage > 0 && target.Has("damage shield"))
                {
                    Trigger(target, "damage shield", 1);
                    damage = 0;
                }
                if (damage > 0 && target.Has("armor") && !(actor?.Has("piercing") ?? false))
                {
                    int armor = target.Amount("armor");
                    int spent = Math.Min(target.Count("armor"), damage);
                    damage = Math.Max(0, damage - armor);
                    Trigger(target, "armor", spent);
                }
                if (damage > 0 && target.Has("fragile")) damage = target.Health + damage - 1;
                if (target.Has("untouchable")) damage = 0;
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
                    Emit("Death", actor, target, 0);
                    if (target.Source.EndsBattleOnDeath) battleWon = true;
                }
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
                int old = unit.Health;
                unit.Health = Math.Min(unit.Source.MaxHealth, unit.Health + amount);
                Emit(kind, unit, unit, unit.Health - old);
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
                }
                return text.ToString();
            }

            private void Emit(string kind, WorkingUnit? actor, WorkingUnit target, int amount) =>
                events.Add(new CombatEvent(round, kind, actor?.Source.Id ?? 0, target.Source.Id, amount));

            private RoomCombatResult Finish(RoomOutcome outcome) => new RoomCombatResult(
                new RoomCombatState(source.RoomIndex, source.Deployment,
                    units.Where(unit => unit.Alive).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions),
                outcome, round, events);
        }
    }
}
