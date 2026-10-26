using System;

namespace MonsterTrain2Poju.Probe
{
    // Allocation-free change detection between native input/settlement boundaries.
    // This only schedules captures; publications still use the full immutable key.
    internal readonly struct AdvisorObservation : IEquatable<AdvisorObservation>
    {
        private readonly int room;
        private readonly int abilityCards;
        private readonly (int Turn, int Phase, int Energy, int Moon, int Pyre,
            int Gold, int Forge, int Hoard, int DrawModifier, int Hand, int Draw,
            int Discard, int DiscardBuffer, int Exhausted, int Purged, int Eaten) values;

        internal AdvisorObservation(AllGameManagers managers)
        {
            CombatManager combat = managers.GetCombatManager();
            PlayerManager player = managers.GetPlayerManager();
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager();
            room = managers.GetRoomManager().GetSelectedRoom();
            abilityCards = AbilityCardProbe.Count;
            values = (combat.GetTurnCount(), (int)combat.GetCombatPhase(),
                player.GetEnergy(), (int)player.CurrentMoonPhase, save.GetTowerHP(), save.GetGold(),
                save.GetForgePoints(), save.GetDragonsHoardAmount(), cards.GetDrawCountModifier(),
                cards.GetHand().Count, cards.GetDrawPile().Count, cards.GetDiscardPile().Count,
                cards.GetDiscardBufferPile().Count, cards.GetExhaustedPile().Count,
                cards.GetPurgedPile().Count, cards.GetEatenPile().Count);
        }

        internal bool SameGameplay(AdvisorObservation other) => abilityCards == other.abilityCards && values.Equals(other.values);
        public bool Equals(AdvisorObservation other) => room == other.room && SameGameplay(other);
        public override bool Equals(object? other) => other is AdvisorObservation stamp && Equals(stamp);
        public override int GetHashCode() => HashCode.Combine(room, abilityCards, values.GetHashCode());
    }
}
