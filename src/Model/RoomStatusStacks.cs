using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomStatusStackRule
    {
        public string StatusId { get; }
        public int Amount { get; }
        public RoomStatusStackRule(string statusId, int amount) { StatusId = statusId; Amount = amount; }
    }
    internal static class RoomStatusStackModel
    {
        internal const string Type = "RoomStateStatusEffectStackModifier";
        internal static int Modifier(CombatContext? context, int roomIndex, string statusId) =>
            context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == roomIndex)?.Attachments?
                .Sum(attachment => attachment.StatusStackBonuses?.Where(rule => rule.StatusId == statusId)
                    .Sum(rule => rule.Amount) ?? 0) ?? 0;
    }
}
