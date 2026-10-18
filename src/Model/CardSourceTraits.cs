using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Original CardData parameters, independent of mutable runtime trait state.
    // Null provenance on older descriptors is distinct from an empty trait list.
    public sealed class CardSourceTrait
    {
        public string Type { get; }
        public int Int { get; }
        public int Int2 { get; }
        public int Int3 { get; }
        public bool Bool { get; }
        public string Str { get; }
        public float Float { get; }
        public bool Scaling { get; }
        public string TrackedValue { get; }
        public string EntryDuration { get; }
        // Captured only for room-status queries; null means unknown/not applicable.
        public string? StatusQueryTeam { get; }
        public IReadOnlyList<string>? StatusQueryStatuses { get; }
        public string? HandCostTargetType { get; }
        public CardSourceTrait(string type, int @int, int int2, int int3, bool @bool, string str, float @float,
            bool scaling, string trackedValue, string entryDuration, string? statusQueryTeam = null,
            IReadOnlyList<string>? statusQueryStatuses = null, string? handCostTargetType = null)
        { Type = type; Int = @int; Int2 = int2; Int3 = int3; Bool = @bool; Str = str; Float = @float;
            Scaling = scaling; TrackedValue = trackedValue; EntryDuration = entryDuration; StatusQueryTeam = statusQueryTeam;
            StatusQueryStatuses = statusQueryStatuses == null ? null : Array.AsReadOnly(statusQueryStatuses.ToArray());
            HandCostTargetType = handCostTargetType; }
    }
}
