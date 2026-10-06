using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardToken
    {
        public int InstanceId { get; }
        public string DataId { get; }

        public CardToken(int instanceId, string dataId)
        {
            InstanceId = instanceId;
            DataId = dataId;
        }
    }

    public sealed class UnitToken
    {
        public int InstanceId { get; }
        public string AssetKey { get; }
        public int Room { get; }
        public int Position { get; }
        public int Attack { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public int Size { get; }
        public string Statuses { get; }

        public UnitToken(int instanceId, string assetKey, int room, int position,
            int attack, int health, int maxHealth, int size, string statuses)
        {
            InstanceId = instanceId;
            AssetKey = assetKey;
            Room = room;
            Position = position;
            Attack = attack;
            Health = health;
            MaxHealth = maxHealth;
            Size = size;
            Statuses = statuses;
        }
    }

    public sealed class RoomToken
    {
        public int Index { get; }
        public int HeroCapacity { get; }
        public int HeroCapacityMax { get; }
        public int HeroNextSpawn { get; }
        public int MonsterCapacity { get; }
        public int MonsterCapacityMax { get; }
        public int MonsterNextSpawn { get; }
        public int Corruption { get; }

        public RoomToken(int index, int heroCapacity, int heroCapacityMax, int heroNextSpawn,
            int monsterCapacity, int monsterCapacityMax, int monsterNextSpawn, int corruption)
        {
            Index = index;
            HeroCapacity = heroCapacity;
            HeroCapacityMax = heroCapacityMax;
            HeroNextSpawn = heroNextSpawn;
            MonsterCapacity = monsterCapacity;
            MonsterCapacityMax = monsterCapacityMax;
            MonsterNextSpawn = monsterNextSpawn;
            Corruption = corruption;
        }

        internal RoomToken WithMonster(int size) => new RoomToken(Index, HeroCapacity,
            HeroCapacityMax, HeroNextSpawn, MonsterCapacity + size, MonsterCapacityMax,
            MonsterNextSpawn, Corruption);
    }

    // Mutable capture DTO; CombatProjection copies it before a worker can use it.
    public sealed class CombatProjectionData
    {
        public string Scenario { get; set; } = string.Empty;
        public int Turn { get; set; }
        public int Energy { get; set; }
        public int PyreHealth { get; set; }
        public int PyreMaxHealth { get; set; }
        public int Gold { get; set; }
        public int ForgePoints { get; set; }
        public int DragonsHoard { get; set; }
        public int DrawModifier { get; set; }
        public string GameplayRng { get; set; } = string.Empty;
        public List<CardToken> Hand { get; set; } = new List<CardToken>();
        public List<CardToken> Draw { get; set; } = new List<CardToken>();
        public List<CardToken> Discard { get; set; } = new List<CardToken>();
        public List<CardToken> DiscardBuffer { get; set; } = new List<CardToken>();
        public List<CardToken> Exhausted { get; set; } = new List<CardToken>();
        public List<CardToken> Eaten { get; set; } = new List<CardToken>();
        public List<CardToken> Purged { get; set; } = new List<CardToken>();
        public List<UnitToken> Heroes { get; set; } = new List<UnitToken>();
        public List<UnitToken> Monsters { get; set; } = new List<UnitToken>();
        public List<RoomToken> Rooms { get; set; } = new List<RoomToken>();
    }

    public sealed class CombatProjection
    {
        public string Scenario { get; }
        public int Turn { get; }
        public int Energy { get; }
        public int PyreHealth { get; }
        public int PyreMaxHealth { get; }
        public int Gold { get; }
        public int ForgePoints { get; }
        public int DragonsHoard { get; }
        public int DrawModifier { get; }
        public string GameplayRng { get; }
        public IReadOnlyList<CardToken> Hand { get; }
        public IReadOnlyList<CardToken> Draw { get; }
        public IReadOnlyList<CardToken> Discard { get; }
        public IReadOnlyList<CardToken> DiscardBuffer { get; }
        public IReadOnlyList<CardToken> Exhausted { get; }
        public IReadOnlyList<CardToken> Eaten { get; }
        public IReadOnlyList<CardToken> Purged { get; }
        public IReadOnlyList<UnitToken> Heroes { get; }
        public IReadOnlyList<UnitToken> Monsters { get; }
        public IReadOnlyList<RoomToken> Rooms { get; }

        public CombatProjection(CombatProjectionData data)
        {
            Scenario = data.Scenario;
            Turn = data.Turn;
            Energy = data.Energy;
            PyreHealth = data.PyreHealth;
            PyreMaxHealth = data.PyreMaxHealth;
            Gold = data.Gold;
            ForgePoints = data.ForgePoints;
            DragonsHoard = data.DragonsHoard;
            DrawModifier = data.DrawModifier;
            GameplayRng = data.GameplayRng;
            Hand = Array.AsReadOnly(data.Hand.ToArray());
            Draw = Array.AsReadOnly(data.Draw.ToArray());
            Discard = Array.AsReadOnly(data.Discard.ToArray());
            DiscardBuffer = Array.AsReadOnly(data.DiscardBuffer.ToArray());
            Exhausted = Array.AsReadOnly(data.Exhausted.ToArray());
            Eaten = Array.AsReadOnly(data.Eaten.ToArray());
            Purged = Array.AsReadOnly(data.Purged.ToArray());
            Heroes = Array.AsReadOnly(data.Heroes.ToArray());
            Monsters = Array.AsReadOnly(data.Monsters.ToArray());
            Rooms = Array.AsReadOnly(data.Rooms.ToArray());
        }

        internal CombatProjectionData CopyData() => new CombatProjectionData
        {
            Scenario = Scenario,
            Turn = Turn,
            Energy = Energy,
            PyreHealth = PyreHealth,
            PyreMaxHealth = PyreMaxHealth,
            Gold = Gold,
            ForgePoints = ForgePoints,
            DragonsHoard = DragonsHoard,
            DrawModifier = DrawModifier,
            GameplayRng = GameplayRng,
            Hand = Hand.ToList(),
            Draw = Draw.ToList(),
            Discard = Discard.ToList(),
            DiscardBuffer = DiscardBuffer.ToList(),
            Exhausted = Exhausted.ToList(),
            Eaten = Eaten.ToList(),
            Purged = Purged.ToList(),
            Heroes = Heroes.ToList(),
            Monsters = Monsters.ToList(),
            Rooms = Rooms.ToList()
        };
    }
}
