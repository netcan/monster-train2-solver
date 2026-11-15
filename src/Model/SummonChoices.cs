using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class SummonChoiceCandidate
    {
        public string CardDataId { get; }
        public bool HasSpawnCharacter { get; }
        public int Size { get; }
        public CombatUnit? Unit { get; }
        public bool ShouldSerializeUnit() => Unit != null;
        public SummonChoiceCandidate(string cardDataId, bool hasSpawnCharacter, int size, CombatUnit? unit = null)
        { CardDataId = cardDataId; HasSpawnCharacter = hasSpawnCharacter; Size = size; Unit = unit; }
    }

    // The pool has already passed card-pool/DLC/relic filtering. These filters
    // are a separate input boundary, not inferred from a card being playable.
    public sealed class SummonChoiceRule
    {
        public IReadOnlyList<SummonChoiceCandidate> Pool { get; }
        public int Count { get; }
        public string OnePerFloorSubtype { get; }
        public CombatStatus? CardlessStatus { get; }
        public bool ShouldSerializeCardlessStatus() => CardlessStatus != null;
        public SummonChoiceRule(IReadOnlyList<SummonChoiceCandidate> pool, int count, string onePerFloorSubtype,
            CombatStatus? cardlessStatus = null)
        { Pool = Array.AsReadOnly(pool.ToArray()); Count = count; OnePerFloorSubtype = onePerFloorSubtype; CardlessStatus = cardlessStatus; }
        public SummonChoiceRule WithCount(int count) => new SummonChoiceRule(Pool, count, OnePerFloorSubtype, CardlessStatus);
    }

    public sealed class SummonChoiceSelection
    {
        public int EffectIndex { get; }
        public string CardDataId { get; }
        public SummonChoiceSelection(int effectIndex, string cardDataId)
        { EffectIndex = effectIndex; CardDataId = cardDataId; }
    }

    public sealed class SummonChoiceDraft
    {
        public IReadOnlyList<string> Offers { get; }
        public UnityRng Rng { get; }
        public bool BlockedBySubtype { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal SummonChoiceDraft(IReadOnlyList<string> offers, UnityRng rng, bool blockedBySubtype = false, string? error = null)
        { Offers = Array.AsReadOnly(offers.ToArray()); Rng = rng; BlockedBySubtype = blockedBySubtype; UnsupportedReason = error; }
    }

    public static class SummonChoiceModel
    {
        // Enumerate possible inputs, then let the full ordered cast validate its
        // actual post-teleport offers. The no-selection branch covers skipped UI.
        internal static IEnumerable<PlayCardAction> Actions(CardPlayRule rule, PlayCardAction action)
        {
            var effects = rule.Effects.Concat(rule.OnCastEffects).Select((effect, index) => (effect, index))
                .Where(item => item.effect.Type == "SummonChoice").ToArray();
            if (effects.Length == 0) { yield return action; yield break; }
            var selections = new List<SummonChoiceSelection>();
            foreach (var branch in Expand(0)) yield return branch;
            IEnumerable<PlayCardAction> Expand(int index)
            {
                if (index == effects.Length)
                {
                    yield return new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                        action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex, action.RecursionSelections,
                        action.MixSelections, action.ActivatorRoomIndex, action.Operation, selections);
                    yield break;
                }
                foreach (var branch in Expand(index + 1)) yield return branch;
                foreach (string id in effects[index].effect.SummonChoice?.Pool.Where(card => card.HasSpawnCharacter)
                    .Select(card => card.CardDataId).Distinct() ?? Enumerable.Empty<string>())
                {
                    selections.Add(new SummonChoiceSelection(effects[index].index, id));
                    foreach (var branch in Expand(index + 1)) yield return branch;
                    selections.RemoveAt(selections.Count - 1);
                }
            }
        }

        // Offer generation only. Applying the player's selection, spawning and
        // subsequent callbacks belong to the caller's ordered effect pipeline.
        public static SummonChoiceDraft Plan(RoomCombatState room, SummonChoiceRule rule, UnityRng rng)
        {
            bool blocked = rule.OnePerFloorSubtype.Length > 0 && room.Units.Any(unit =>
                unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true &&
                unit.Subtypes.Contains(rule.OnePerFloorSubtype));
            if (blocked) return new SummonChoiceDraft(Array.Empty<string>(), rng, true);
            BattleSpawnPoints? points = room.Context?.SpawnPoints;
            if (points?.SelectedGroup(room.RoomIndex, CombatTeam.Player) == null)
                return new SummonChoiceDraft(Array.Empty<string>(), rng, error: "Summon choice requires captured physical player spawn points.");
            // Native GetCanSpawnMonster(room, null, size) takes the null-point
            // branch: an empty physical slot is sufficient, even over pip capacity.
            var available = points.FirstEmpty(room.RoomIndex, CombatTeam.Player) < 0
                ? new List<SummonChoiceCandidate>() : rule.Pool.Where(card => card.HasSpawnCharacter).ToList();
            int count = Math.Max(1, rule.Count);
            if (available.Count <= count)
                return new SummonChoiceDraft(available.Select(card => card.CardDataId).ToArray(), rng);
            var offers = new List<string>();
            while (available.Count > 0 && offers.Count < count)
            {
                RngDraw draw = rng.Range(0, available.Count); rng = draw.State;
                string selected = available[draw.Value].CardDataId;
                offers.Add(selected);
                // Native List.Remove(card) removes the first matching reference,
                // even if a later occurrence of the same definition was drawn.
                available.RemoveAt(available.FindIndex(card => card.CardDataId == selected));
            }
            return new SummonChoiceDraft(offers, rng);
        }
    }
}
