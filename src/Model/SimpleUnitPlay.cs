using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class SimpleUnitPlay
    {
        public int HandIndex { get; }
        public int CardInstanceId { get; }
        public int RoomIndex { get; }
        public int Cost { get; }
        public int Size { get; }
        public string UnitAssetKey { get; }
        public int Attack { get; }
        public int Health { get; }

        public SimpleUnitPlay(int handIndex, int cardInstanceId, int roomIndex,
            int cost, int size, string unitAssetKey, int attack, int health)
        {
            HandIndex = handIndex;
            CardInstanceId = cardInstanceId;
            RoomIndex = roomIndex;
            Cost = cost;
            Size = size;
            UnitAssetKey = unitAssetKey;
            Attack = attack;
            Health = health;
        }
    }

    public sealed class ModelStep
    {
        public CombatProjection? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;

        private ModelStep(CombatProjection? state, string? unsupportedReason)
        {
            State = state;
            UnsupportedReason = unsupportedReason;
        }

        internal static ModelStep Accepted(CombatProjection state) => new ModelStep(state, null);
        internal static ModelStep Unsupported(string reason) => new ModelStep(null, reason);
    }

    public static class SimpleUnitPlayModel
    {
        private const string StewardCardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

        public static ModelStep Apply(CombatProjection state, SimpleUnitPlay action)
        {
            if (state.Scenario != "Level1BattleJunker" || state.Turn != 0)
            {
                return ModelStep.Unsupported("Only turn zero of Level1BattleJunker is modeled.");
            }
            if (action.HandIndex < 0 || action.HandIndex >= state.Hand.Count ||
                state.Hand[action.HandIndex].InstanceId != action.CardInstanceId)
            {
                return ModelStep.Unsupported("The selected card instance is not at that hand index.");
            }
            if (state.Hand[action.HandIndex].DataId != StewardCardId)
            {
                return ModelStep.Unsupported("This card's play effects are not modeled.");
            }
            if (action.RoomIndex != 0 || action.Cost != 1 || action.Size != 3 ||
                action.UnitAssetKey != "TrainStewardBig" || action.Attack != 8 || action.Health != 25)
            {
                return ModelStep.Unsupported("The Steward definition or target room differs from the verified fixture.");
            }
            RoomToken? room = state.Rooms.FirstOrDefault(item => item.Index == action.RoomIndex);
            if (room == null || action.RoomIndex < 0 || action.RoomIndex == state.Rooms.Count - 1)
            {
                return ModelStep.Unsupported("The selected room is not a normal train room.");
            }
            if (state.Energy < action.Cost || room.MonsterCapacity + action.Size > room.MonsterCapacityMax ||
                state.Monsters.Any(unit => unit.Room == action.RoomIndex))
            {
                return ModelStep.Unsupported("The room or energy cannot accept this simple summon.");
            }

            CombatProjectionData next = state.CopyData();
            next.Hand.RemoveAt(action.HandIndex);
            next.Energy -= action.Cost;
            int nextUnitId = state.Heroes.Concat(state.Monsters).Select(unit => unit.InstanceId)
                .DefaultIfEmpty(0).Max() + 1;
            next.Monsters.Add(new UnitToken(nextUnitId, action.UnitAssetKey,
                action.RoomIndex, 0, action.Attack, action.Health, action.Health, action.Size, string.Empty));
            int roomPosition = next.Rooms.FindIndex(item => item.Index == action.RoomIndex);
            next.Rooms[roomPosition] = room.WithMonster(action.Size);
            return ModelStep.Accepted(new CombatProjection(next));
        }
    }
}
