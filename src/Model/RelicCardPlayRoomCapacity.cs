using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardPlayRoomCapacityModifier
    {
        public int EffectIndex { get; }
        public int Amount { get; }
        public string CardType { get; }

        public RelicCardPlayRoomCapacityModifier(int effectIndex, int amount, string cardType)
        { EffectIndex = effectIndex; Amount = amount; CardType = cardType; }
    }

    internal static class RelicCardPlayRoomCapacityModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyRoomCapacityOnCardPlay)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CardPlayRoomCapacityModifiers?.Count > 0 ? "Unexpected card-play room-capacity definitions." : null;
            if (relic.CardPlayRoomCapacityModifiers == null ||
                !indices.SequenceEqual(relic.CardPlayRoomCapacityModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native card-play room-capacity definitions.";
            if (relic.CardPlayRoomCapacityModifiers.Any(effect => string.IsNullOrEmpty(effect.CardType)))
                return "Incomplete native card-play room-capacity definition.";
            return null;
        }

        internal static RoomCapacityResult ApplyOne(CombatContext source, int playedCardId,
            BattlePlayRules? definitions, int relicIndex, int effectIndex, int selectedRoomIndex)
        {
            if (source.Relics == null || relicIndex < 0 || relicIndex >= source.Relics.Count)
                return Fail("Missing card-play room-capacity relic state.");
            CombatRelicState relic = source.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= relic.EffectTypes.Count ||
                relic.EffectTypes[effectIndex] != RelicModel.ModifyRoomCapacityOnCardPlay)
                return Fail("Card-play room-capacity effect order changed.");
            RelicCardPlayRoomCapacityModifier? effect = relic.CardPlayRoomCapacityModifiers?
                .SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (effect == null) return Fail("Missing ordered native card-play room-capacity definition.");

            CardInstanceState? played = source.FindCard(playedCardId);
            CardPlayRule? definition = played == null ? null : definitions?.Cards.FirstOrDefault(item => item.DataId == played.DataId);
            string? cardType = definition?.CardType ?? played?.MaskDescriptor?.Definition.CardType;
            if (string.IsNullOrEmpty(cardType))
                return Fail("Card-play room-capacity filtering requires matching played-card type metadata.");
            if (cardType != effect.CardType) return new RoomCapacityResult(source, 0);
            if (selectedRoomIndex < 0)
                return Fail("Card-play room-capacity effect requires the native selected room.");

            return RoomCapacityModel.Apply(source, selectedRoomIndex,
                new CardActionEffect("AdjustCapacity", "Room", effect.Amount, false, true, Array.Empty<CombatStatus>()));
        }

        private static RoomCapacityResult Fail(string error) => new RoomCapacityResult(null, 0, error);
    }
}
