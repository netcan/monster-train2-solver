using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleSearchMetrics
    {
        public int CandidateRequests { get; }
        public int SimulatedCandidates { get; }
        public int CachedCandidates { get; }
        public int PrefilteredCandidates { get; }
        public int EndTurnRequests { get; }
        public int SimulatedEndTurns { get; }
        public int CachedEndTurns { get; }
        public int CachedPolicies { get; }
        internal BattleSearchMetrics(int requests = 0, int simulated = 0, int cached = 0, int filtered = 0,
            int turns = 0, int simulatedTurns = 0, int cachedTurns = 0, int policies = 0)
        { CandidateRequests = requests; SimulatedCandidates = simulated; CachedCandidates = cached;
            PrefilteredCandidates = filtered; EndTurnRequests = turns; SimulatedEndTurns = simulatedTurns;
            CachedEndTurns = cachedTurns; CachedPolicies = policies; }
        internal static BattleSearchMetrics Sum(IEnumerable<BattleSearchMetrics> values)
        {
            var all = values.ToArray();
            return new BattleSearchMetrics(all.Sum(x => x.CandidateRequests), all.Sum(x => x.SimulatedCandidates),
                all.Sum(x => x.CachedCandidates), all.Sum(x => x.PrefilteredCandidates), all.Sum(x => x.EndTurnRequests),
                all.Sum(x => x.SimulatedEndTurns), all.Sum(x => x.CachedEndTurns), all.Sum(x => x.CachedPolicies));
        }
    }

    // One bounded search owns one workspace. Reference identity is deliberately
    // conservative: never merge distinct histories, allocators, RNG or selections.
    // Cached results are immutable, including failed/unsupported transitions.
    internal sealed class BattleSearchWorkspace
    {
        private readonly bool enabled;
        internal bool OptimizeSelections => enabled;
        private readonly Dictionary<BattleTurnState, Entry> entries = new Dictionary<BattleTurnState, Entry>();
        private int requests, simulated, cached, filtered, turns, simulatedTurns, cachedTurns, policies;
        internal BattleSearchWorkspace(bool enabled = true) { this.enabled = enabled; }
        internal BattleSearchMetrics Metrics => new BattleSearchMetrics(requests, simulated, cached, filtered,
            turns, simulatedTurns, cachedTurns, policies);
        private Entry For(BattleTurnState state)
        {
            if (!entries.TryGetValue(state, out var entry)) entries.Add(state, entry = new Entry());
            return entry;
        }
        private sealed class Entry
        {
            internal readonly Dictionary<(int, int, int, int, int, int, int?), BattleActionResult> Actions = new Dictionary<(int, int, int, int, int, int, int?), BattleActionResult>();
            internal readonly Dictionary<(int, int, CombatTeam?, string), BattleActionResult> IllegalInputs = new Dictionary<(int, int, CombatTeam?, string), BattleActionResult>();
            internal readonly Dictionary<(int?, bool), BattleActionEnumeration> Enumerations = new Dictionary<(int?, bool), BattleActionEnumeration>();
            internal readonly Dictionary<int, (SupportedActionCandidate? Candidate, IReadOnlyList<UnsupportedActionAttempt> Omitted)> Tactical = new Dictionary<int, (SupportedActionCandidate?, IReadOnlyList<UnsupportedActionAttempt>)>();
            internal BattleActionEnumeration? Abilities;
            internal BattleTurnResult? End;
            internal (SupportedActionCandidate? Candidate, IReadOnlyList<UnsupportedActionAttempt> Omitted)? Policy;
        }
        internal BattleActionResult Play(BattleTurnState source, PlayCardAction action)
        {
            requests++;
            // Selection payload order is significant. Leave complex selections
            // with the full model rather than keying them incompletely.
            if (!enabled || action.RecursionSelections.Count != 0 || action.MixSelections.Count != 0)
            { simulated++; return BattleActionModel.PlayCard(source, action); }
            var entry = For(source);
            var key = (action.CardInstanceId, action.RoomIndex, action.PlayerPosition, action.TargetUnitId,
                action.ActivatorUnitId, action.TargetSpawnPointIndex, action.ActivatorRoomIndex);
            if (entry.Actions.TryGetValue(key, out var result)) { cached++; return result; }
            string? illegal = IllegalInput(source, action);
            CombatTeam? team = illegal == null ? null : source.Spawn.Train.Rooms.FirstOrDefault(x => x.RoomIndex == action.RoomIndex)?.Units
                .FirstOrDefault(x => x.Id == action.TargetUnitId)?.Team;
            var family = (action.CardInstanceId, action.RoomIndex, team, illegal ?? "");
            if (illegal != null && entry.IllegalInputs.TryGetValue(family, out result))
            { filtered++; return result; }
            simulated++; result = BattleActionModel.PlayCard(source, action);
            if (entry.Actions.Count < 256) entry.Actions.Add(key, result);
            // The first representative must actually reach this exact legality
            // rejection. Preserve earlier unsupported diagnostics and modifiers.
            if (illegal != null && result.Rejection == ActionRejection.Illegal && result.Reason == illegal)
                entry.IllegalInputs[family] = result;
            return result;
        }
        private static string? IllegalInput(BattleTurnState state, PlayCardAction action)
        {
            if (action.ActivatorUnitId != 0 || action.ActivatorRoomIndex.HasValue) return null;
            RoomPlayRule? room = state.PlayRules?.Rooms.FirstOrDefault(x => x.RoomIndex == action.RoomIndex);
            if (room?.Enabled == false) return "This card cannot be played in the target room.";
            CardToken? card = state.Spawn.Train.Context?.Cards.Hand.FirstOrDefault(x => x.InstanceId == action.CardInstanceId);
            CardPlayRule? rule = state.PlayRules?.Cards.FirstOrDefault(x => x.DataId == card?.DataId);
            if (room?.IsPyre == true && rule?.Effect == "SpawnMonster") return "This card cannot be played in the target room.";
            CombatUnit? target = state.Spawn.Train.Rooms.FirstOrDefault(x => x.RoomIndex == action.RoomIndex)?.Units
                .FirstOrDefault(x => x.Id == action.TargetUnitId);
            if (target == null || rule == null) return null;
            if (target.IsPyre || target.Statuses.Any(x => x.Id == "untouchable"))
                return "The card has no legal unit target in the selected room.";
            CardActionEffect[] targeted = rule.Effects.Concat(rule.OnCastEffects)
                .Where(x => x.Target == "DropTargetCharacter" && x.Type != "HandUpgrade").ToArray();
            if (targeted.Length > 0 && !targeted.Any(x => target.Team == CombatTeam.Enemy ? x.AllowEnemy : x.AllowPlayer))
                return "The card excludes the target team.";
            return null;
        }
        internal BattleTurnResult EndTurn(BattleTurnState source)
        {
            turns++;
            if (enabled && For(source).End is BattleTurnResult existing) { cachedTurns++; return existing; }
            simulatedTurns++;
            var result = BattleTurnModel.EndTurn(source);
            if (enabled) For(source).End = result;
            return result;
        }
        internal BattleActionEnumeration Enumerate(BattleTurnState source, int? onlyCardId = null, bool includeAbilities = true)
        {
            var key = (onlyCardId, includeAbilities);
            if (enabled && For(source).Enumerations.TryGetValue(key, out var existing)) return existing;
            var result = BattleActionModel.EnumerateActions(source, onlyCardId, includeAbilities, this);
            if (enabled) For(source).Enumerations[key] = result;
            return result;
        }
        internal BattleActionEnumeration Abilities(BattleTurnState source)
        {
            if (enabled && For(source).Abilities is BattleActionEnumeration existing) return existing;
            var result = UnitAbilityModel.EnumerateActivations(source, Play);
            if (enabled) For(source).Abilities = result;
            return result;
        }
        internal SupportedActionCandidate? Choose(BattleTurnState source, out IReadOnlyList<UnsupportedActionAttempt> omitted, int room)
        {
            if (enabled && For(source).Tactical.TryGetValue(room, out var existing))
            { policies++; omitted = existing.Omitted; return existing.Candidate; }
            var result = BattleTacticalPolicy.Choose(source, out omitted, room, this);
            if (enabled) For(source).Tactical[room] = (result, omitted);
            return result;
        }
        internal SupportedActionCandidate? ChoosePolicy(BattleTurnState source, out IReadOnlyList<UnsupportedActionAttempt> omitted)
        {
            if (enabled && For(source).Policy is var existing && existing.HasValue)
            { policies++; omitted = existing.Value.Omitted; return existing.Value.Candidate; }
            var result = BattleActionModel.ChooseUnitSpellAndJunkCandidate(source, out omitted, this);
            if (enabled) For(source).Policy = (result, omitted);
            return result;
        }
    }
}
