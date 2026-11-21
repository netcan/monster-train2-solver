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
        internal int TrainValidations { get; }
        internal BattleSearchMetrics(int requests = 0, int simulated = 0, int cached = 0, int filtered = 0,
            int turns = 0, int simulatedTurns = 0, int cachedTurns = 0, int policies = 0, int trainValidations = 0)
        { CandidateRequests = requests; SimulatedCandidates = simulated; CachedCandidates = cached;
            PrefilteredCandidates = filtered; EndTurnRequests = turns; SimulatedEndTurns = simulatedTurns;
            CachedEndTurns = cachedTurns; CachedPolicies = policies; TrainValidations = trainValidations; }
        internal static BattleSearchMetrics Sum(IEnumerable<BattleSearchMetrics> values)
        {
            var all = values.ToArray();
            return new BattleSearchMetrics(all.Sum(x => x.CandidateRequests), all.Sum(x => x.SimulatedCandidates),
                all.Sum(x => x.CachedCandidates), all.Sum(x => x.PrefilteredCandidates), all.Sum(x => x.EndTurnRequests),
                all.Sum(x => x.SimulatedEndTurns), all.Sum(x => x.CachedEndTurns), all.Sum(x => x.CachedPolicies),
                all.Sum(x => x.TrainValidations));
        }
    }

    // One bounded search owns one workspace. Reference identity is deliberately
    // conservative: never merge distinct histories, allocators, RNG or selections.
    // Cached results are immutable, including failed/unsupported transitions.
    internal sealed class BattleSearchWorkspace
    {
        private readonly bool enabled;
        private readonly bool cacheTrainValidation;
        internal bool OptimizeSelections => enabled;
        private readonly Dictionary<BattleTurnState, Entry> entries = new Dictionary<BattleTurnState, Entry>();
        private int requests, simulated, cached, filtered, turns, simulatedTurns, cachedTurns, policies, trainValidations;
        internal BattleSearchWorkspace(bool enabled = true, bool cacheTrainValidation = true)
        { this.enabled = enabled; this.cacheTrainValidation = enabled && cacheTrainValidation; }
        internal BattleSearchMetrics Metrics => new BattleSearchMetrics(requests, simulated, cached, filtered,
            turns, simulatedTurns, cachedTurns, policies, trainValidations);
        private Entry For(BattleTurnState state)
        {
            if (!entries.TryGetValue(state, out var entry)) entries.Add(state, entry = new Entry());
            return entry;
        }
        private sealed class Entry
        {
            internal readonly Dictionary<(int, int, int, int, int, int, int?), BattleActionResult> Actions = new Dictionary<(int, int, int, int, int, int, int?), BattleActionResult>();
            internal readonly Dictionary<(int, int, CombatTeam?, string), BattleActionResult> IllegalInputs = new Dictionary<(int, int, CombatTeam?, string), BattleActionResult>();
            internal readonly Dictionary<(int Card, int Room), BattleActionResult> UnaffordableCards = new Dictionary<(int, int), BattleActionResult>();
            internal readonly Dictionary<(int?, bool), BattleActionEnumeration> Enumerations = new Dictionary<(int?, bool), BattleActionEnumeration>();
            internal readonly Dictionary<int, (SupportedActionCandidate? Candidate, IReadOnlyList<UnsupportedActionAttempt> Omitted)> Tactical = new Dictionary<int, (SupportedActionCandidate?, IReadOnlyList<UnsupportedActionAttempt>)>();
            internal BattleActionEnumeration? Abilities;
            internal BattleTurnResult? End;
            internal readonly Dictionary<string, BattleTurnResult> DialogEnds = new Dictionary<string, BattleTurnResult>(StringComparer.Ordinal);
            internal (SupportedActionCandidate? Candidate, IReadOnlyList<UnsupportedActionAttempt> Omitted)? Policy;
            internal bool TrainValidated;
            internal string? TrainValidationError;
        }

        internal string? ValidateTrain(BattleTurnState source)
        {
            // Direct card candidates check the same immutable train before any
            // per-action work. Cache both success and failure at that boundary.
            Entry? entry = cacheTrainValidation ? For(source) : null;
            if (entry?.TrainValidated == true) return entry.TrainValidationError;
            trainValidations++;
            string? error = TrainCombatModel.Validate(source.Spawn.Train);
            if (entry != null)
            {
                entry.TrainValidationError = error;
                entry.TrainValidated = true;
            }
            return error;
        }

        internal BattleActionResult Play(BattleTurnState source, PlayCardAction action)
        {
            requests++;
            if (!enabled || action.Operation != null)
            { simulated++; return BattleActionModel.PlayCard(source, action); }
            var entry = For(source);
            // Direct card cost is checked before target/position/selection
            // effects. Reuse only an observed exact cost rejection, scoped to
            // this immutable decision and room; abilities and spawn-point
            // inputs have different earlier checks and must bypass this cache.
            bool direct = action.ActivatorUnitId == 0 && !action.ActivatorRoomIndex.HasValue && action.TargetSpawnPointIndex == -1;
            var costKey = (action.CardInstanceId, action.RoomIndex);
            if (direct && entry.UnaffordableCards.TryGetValue(costKey, out var unaffordable))
            { filtered++; return unaffordable; }
            void RememberCost(BattleActionResult rejected)
            {
                if (direct && rejected.Rejection == ActionRejection.Illegal && rejected.Reason == "Insufficient energy.")
                    entry.UnaffordableCards[costKey] = rejected;
            }
            // Successful complex choices still require their complete payload.
            if (action.RecursionSelections.Count != 0 || action.MixSelections.Count != 0 || action.SummonSelections.Count != 0)
            {
                simulated++;
                var selected = BattleActionModel.PlayCard(source, action, this);
                RememberCost(selected); return selected;
            }
            var key = (action.CardInstanceId, action.RoomIndex, action.PlayerPosition, action.TargetUnitId,
                action.ActivatorUnitId, action.TargetSpawnPointIndex, action.ActivatorRoomIndex);
            if (entry.Actions.TryGetValue(key, out var result)) { cached++; return result; }
            string? illegal = IllegalInput(source, action);
            CombatTeam? team = illegal == null ? null : source.Spawn.Train.Rooms.FirstOrDefault(x => x.RoomIndex == action.RoomIndex)?.Units
                .FirstOrDefault(x => x.Id == action.TargetUnitId)?.Team;
            var family = (action.CardInstanceId, action.RoomIndex, team, illegal ?? "");
            if (illegal != null && entry.IllegalInputs.TryGetValue(family, out result))
            { filtered++; return result; }
            simulated++; result = BattleActionModel.PlayCard(source, action, this);
            RememberCost(result);
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
        internal BattleTurnResult EndTurn(BattleTurnState source, IReadOnlyList<RelentlessDecision>? decisions = null)
        {
            turns++;
            string? key = decisions?.Count > 0 ? string.Join(";", decisions.Select(choice =>
                choice.RoomIndex + ":" + choice.CompletedRounds + ":" + (choice.Continue ? "1" : "0"))) : null;
            if (enabled && key == null && For(source).End is BattleTurnResult existing) { cachedTurns++; return existing; }
            if (enabled && key != null && For(source).DialogEnds.TryGetValue(key, out BattleTurnResult cachedResult))
            { cachedTurns++; return cachedResult; }
            simulatedTurns++;
            var result = BattleTurnModel.EndTurn(source, decisions);
            if (enabled) { if (key == null) For(source).End = result; else For(source).DialogEnds[key] = result; }
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
