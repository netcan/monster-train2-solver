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
        public SummonChoiceCandidate(string cardDataId, bool hasSpawnCharacter, int size)
        { CardDataId = cardDataId; HasSpawnCharacter = hasSpawnCharacter; Size = size; }
    }

    // The pool has already passed card-pool/DLC/relic filtering. These filters
    // are a separate input boundary, not inferred from a card being playable.
    public sealed class SummonChoiceRule
    {
        public IReadOnlyList<SummonChoiceCandidate> Pool { get; }
        public int Count { get; }
        public string OnePerFloorSubtype { get; }
        public SummonChoiceRule(IReadOnlyList<SummonChoiceCandidate> pool, int count, string onePerFloorSubtype)
        { Pool = Array.AsReadOnly(pool.ToArray()); Count = count; OnePerFloorSubtype = onePerFloorSubtype; }
        public SummonChoiceRule WithCount(int count) => new SummonChoiceRule(Pool, count, OnePerFloorSubtype);
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
