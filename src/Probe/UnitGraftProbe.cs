using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitGraftProbe
    {
        internal static bool Known(string id) => id is "31faf1a8-4838-4c5f-9c4b-605baa37b5ef" or
            "3a8240bd-8b96-4d6e-b2d1-49ff8b5d8022" or "7227d3ea-343a-45b2-ae2c-6f05383fcc7c" or
            "d105aac6-843e-43eb-b1b0-098302e7a09a" or "eee797b2-ff2a-4d2d-9b23-d62a114388a5";
        internal static bool Known(CardState card) => Known(card.GetCardDataID()) || EventEquipment(card) != null;
        private static CardData? EventEquipment(CardState? card)
        {
            if (card?.GetCardDataID() != CardGraftedEquipmentModel.EventHostId ||
                card.GetCardStateModifiers().GraftedEquipmentCardState?.GetCardDataID() != CardGraftedEquipmentModel.EventEquipmentId)
                return null;
            return AllGameManagers.Instance!.GetAllGameData().FindCardData(CardGraftedEquipmentModel.EventEquipmentId);
        }
        internal static UnitGraftRule? EventRule(CardState? card)
        {
            CardData? equipment = EventEquipment(card);
            return equipment == null ? null : Create(equipment);
        }
        internal static UnitGraftRule? Rule(CharacterData data)
        {
            CardData? graft = data.GetGraftedEquipment();
            if (graft == null) return null;
            return Create(graft);
        }
        private static UnitGraftRule Create(CardData graft) => new UnitGraftRule(CardGenerationProbe.Creation(graft), BattleActionProbe.Definition(graft).Equipment
                ?? throw new InvalidOperationException("Missing graft equipment definition."));
    }
}
