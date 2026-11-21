using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicShowtimeModel
    {
        internal const string Effect = "RelicEffectShowtime";
        internal const int HiddenAttachment = 4;

        internal static string? AttachmentId(string soulId) => soulId switch
        {
            "20d57085-8651-49c0-8c66-5530c314ff21" => "31c8e4b4-5b8c-448f-a582-95dbe2fb95b4",
            "103f4378-9ab0-45dd-9de8-5f4c561a9799" => "b60cb2b9-d35c-4c00-aa95-50475ecba9e7",
            "9638b7dd-461e-4810-a029-b7d24cf1e7af" => "7bb3243f-0ef9-410b-baa3-edb19ae4abcb",
            _ => null
        };

        internal static int AttachmentCapacity(string id) => id switch
        {
            "31c8e4b4-5b8c-448f-a582-95dbe2fb95b4" or "b60cb2b9-d35c-4c00-aa95-50475ecba9e7" => 3,
            "7bb3243f-0ef9-410b-baa3-edb19ae4abcb" => 5,
            _ => 0
        };

        internal static bool IsAttachment(RoomCapacityAttachmentState attachment) =>
            attachment.AttachmentType == HiddenAttachment && AttachmentCapacity(attachment.DataId) is int capacity && capacity != 0 &&
            capacity == attachment.CapacityDelta && attachment.AffectsMonsters && !attachment.AffectsHeroes &&
            attachment.AttachmentId.Length == 0 && attachment.PostCombat != null &&
            IsPostCombat(attachment.PostCombat);

        internal static bool IsPostCombat(RoomPostCombatRule rule) => rule.DisallowedInDeployment &&
            rule.Effects.Count == 1 && rule.Effects[0] is { Type: "UnitUpgrade", Target: "FrontInRoom",
                AllowPlayer: true, AllowEnemy: false, Value: 0, Range: null, Upgrade: not null };

        internal static string? Validate(CombatContext? context)
        {
            var owners = context?.Relics?.Where(owner => owner.EffectTypes.Contains(Effect)).ToArray();
            if (owners == null || owners.Length == 0) return null;
            var rooms = context!.RoomCapacities;
            if (rooms == null) return "Showtime requires the settled native hidden room attachment.";
            var attachments = rooms.Where(room => !room.IsPyre)
                .SelectMany(room => room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()).Where(IsAttachment).ToArray();
            foreach (var family in owners.GroupBy(owner => owner.DataId))
            {
                string? id = AttachmentId(family.Key);
                if (id == null || family.Any(owner => owner.DisallowedInPlacementPhase != false ||
                    !owner.EffectTypes.SequenceEqual(new[] { Effect })) || attachments.Count(room => room.DataId == id) != family.Count())
                    return "Showtime owner and settled native room attachment differ.";
            }
            return null;
        }
    }
}
