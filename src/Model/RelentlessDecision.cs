namespace MonsterTrain2Poju.Model
{
    // Inputs to a room-combat coroutine, at the native notification before
    // the next exchange. Decisions are immutable and may span several floors.
    public sealed class RelentlessDecision
    {
        public int RoomIndex { get; }
        public int CompletedRounds { get; }
        public bool Continue { get; }
        public RelentlessDecision(int roomIndex, int completedRounds, bool @continue)
        { RoomIndex = roomIndex; CompletedRounds = completedRounds; Continue = @continue; }
    }

    public sealed class RelentlessChoiceRequest
    {
        public int RoomIndex { get; }
        public int CompletedRounds { get; }
        public RelentlessChoiceRequest(int roomIndex, int completedRounds)
        { RoomIndex = roomIndex; CompletedRounds = completedRounds; }
        public RelentlessDecision Choose(bool @continue) => new RelentlessDecision(RoomIndex, CompletedRounds, @continue);
    }
}
