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
        public CardSourceTrait(string type, int @int, int int2, int int3, bool @bool, string str, float @float,
            bool scaling, string trackedValue, string entryDuration)
        { Type = type; Int = @int; Int2 = int2; Int3 = int3; Bool = @bool; Str = str; Float = @float;
            Scaling = scaling; TrackedValue = trackedValue; EntryDuration = entryDuration; }
    }
}
