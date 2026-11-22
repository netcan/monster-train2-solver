using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Capture
{
    internal class CardStatisticRefreshSample
    {
        public int Sequence { get; set; }
        public int Turn { get; set; }
        public int Frame { get; set; }
        public string Phase { get; set; } = "";
        public bool NativePreviewMode { get; set; }
        public int CardId { get; set; }
        public string CardDataId { get; set; } = "";
        public int[] BeforeStoredCards { get; set; } = Array.Empty<int>();
        public int[] AfterStoredCards { get; set; } = Array.Empty<int>();
        public int[] DeckCards { get; set; } = Array.Empty<int>();
        public string[] Caller { get; set; } = Array.Empty<string>();
    }

    internal static class EquipmentStatisticRefreshComparison
    {
        private static readonly string[] Caller =
        {
            "CardStatistics.GetStatValue", "CardTraitScalingAddStatusEffect.GetAdditionalStacks",
            "CardTraitScalingAddStatusEffect.GetCurrentEffectText", "CardState.GetCurrentTraitEffectText",
            "CardState.GetCardText", "CardUI.UpdateTextContent", "CardUI.ApplyStateToUI", "CardManager.RefreshCardInHand",
            "RoomStateConduitModifier.AddMagicPowerModifierToCardsInHand", "RoomStateConduitModifier.RoomSelectionChanged",
            "RoomState.UpdateRoomSelectedRoomStateModifiers"
        };

        internal static bool CanObserve(RoomCombatState before, int hostId, int cardId)
        {
            CombatContext? context = before.Context;
            return before.Deployment && !before.Preview && context?.Statistics?.StoredCards != null &&
                !context.Statistics.StoredCards.Contains(cardId) && context.Statistics.TrackedCards.Contains(cardId) &&
                context.Cards.Discard.Any(card => card.InstanceId == cardId) &&
                before.Units.FirstOrDefault(unit => unit.Id == hostId)?.Status(EquipmentPoolGrantModel.CrownStatus) is CombatStatus crown &&
                EquipmentPoolGrantModel.CosmeticStatus(crown);
        }

        internal static bool Matches(RoomCombatState before, RoomCombatState modeled, RoomCombatState actual, int hostId, int cardId,
            CardStatisticRefreshSample[]? refreshes)
        {
            if (!CanObserve(before, hostId, cardId) || refreshes is not [{ Sequence: 1, Turn: 0, Frame: > 0, Phase: "MonsterTurn", NativePreviewMode: false } refresh] ||
                refresh.CardId != cardId || refresh.CardDataId != before.Context!.FindCard(cardId)?.DataId ||
                refresh.Caller.FirstOrDefault() is not ("CardStatistics.DMD<CardStatistics::UpdateDeckStats>" or "CardStatistics.UpdateDeckStats") ||
                !refresh.Caller.Skip(1).SequenceEqual(Caller) ||
                actual.Units.FirstOrDefault(unit => unit.Id == hostId)?.EquipmentCards?.Contains(cardId) != true)
                return false;
            var prior = before.Context.Statistics!.StoredCards!;
            var updated = prior.Append(cardId).OrderBy(id => id).ToArray();
            if (!prior.SequenceEqual(refresh.BeforeStoredCards) || !updated.SequenceEqual(refresh.AfterStoredCards) ||
                !updated.SequenceEqual(refresh.DeckCards) || modeled.Context?.Statistics?.StoredCards?.SequenceEqual(prior) != true ||
                actual.Context?.Statistics?.StoredCards?.SequenceEqual(updated) != true)
                return false;

            var expected = JObject.FromObject(modeled);
            expected["Context"]!["Statistics"]!["StoredCards"] = JArray.FromObject(updated);
            return JToken.DeepEquals(expected, JObject.FromObject(actual));
        }
    }
}
