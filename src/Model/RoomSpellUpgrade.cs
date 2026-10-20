using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomSpellUpgradeState
    {
        public CardUpgradeModifier Upgrade { get; }
        public string TrackerId { get; }
        public IReadOnlyList<int> Cards { get; }
        public RoomSpellUpgradeState(CardUpgradeModifier upgrade, string trackerId, IReadOnlyList<int> cards)
        { Upgrade = upgrade; TrackerId = trackerId; Cards = Array.AsReadOnly(cards.ToArray()); }
        internal RoomSpellUpgradeState WithIdentity(string id) => new RoomSpellUpgradeState(Upgrade, id, Cards);
    }

    internal static class RoomSpellUpgradeModel
    {
        internal static string? ValidateContext(CombatContext context) => Entries(context).Any() &&
            (context.QueryFrame?.SelectedRoomIndex == null || context.CardInstances == null ||
             context.CardInstances.Any(card => card.MaskDescriptor?.Definition.CanScaleWithMagicPower == null))
                ? "Room spell upgrades require captured room selection and card magic-power eligibility." : null;
        internal static string? Validate(RoomSpellUpgradeState value) =>
            value.Upgrade.DataId != "0ce30902-963a-4662-b9e7-fca70d79735c" ||
            value.Upgrade.Stats.Damage != 15 || value.Upgrade.Stats.Heal != 15 ||
            value.Upgrade.ExternalInteractions.Count != 0 || value.Upgrade.Lifecycle?.AddedTraits.Count > 0
                ? "Uncalibrated room spell upgrade." : null;

        private static IEnumerable<(int Room, RoomCapacityAttachmentState Attachment)> Entries(CombatContext context) =>
            (context.RoomCapacities ?? Array.Empty<RoomCapacityState>()).SelectMany(room =>
                (room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>()).Where(value => value.SpellUpgrade != null)
                    .Select(value => (room.RoomIndex, value)));

        private static CombatContext Put(CombatContext context, int room, RoomCapacityAttachmentState attachment, IReadOnlyList<int> cards) =>
            context.WithRoomCapacities(context.RoomCapacities!.Select(value => value.RoomIndex != room ? value :
                new RoomCapacityState(value.RoomIndex, value.PlayerMaximum, value.EnemyMaximum, value.IsPyre,
                    value.Corruption, value.Attachments!.Select(item => item.AttachmentId != attachment.AttachmentId ? item :
                        item.WithSpellUpgrade(new RoomSpellUpgradeState(item.SpellUpgrade!.Upgrade, item.SpellUpgrade.TrackerId, cards))).ToArray(),
                    value.MaxCorruption, value.PermanentCorruption, value.AttachmentsAddedFromCards)).ToArray());

        private static CombatContext Clear(CombatContext context, int room, RoomCapacityAttachmentState attachment, int? only = null)
        {
            RoomSpellUpgradeState tracker = attachment.SpellUpgrade!;
            foreach (int id in tracker.Cards.Where(id => !only.HasValue || id == only))
            {
                CardInstanceState? card = context.FindCard(id);
                if (card == null) continue;
                context = context.WithCard(card.WithModifiers(card.Permanent, new CardModifiers(card.Temporary.Offsets,
                    card.Temporary.Upgrades.Where(upgrade => upgrade.DataId != tracker.TrackerId).ToArray(),
                    card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions), refreshTraits: false));
            }
            return Put(context, room, attachment, tracker.Cards.Where(id => only.HasValue && id != only).ToArray());
        }

        private static CombatContext Add(CombatContext context, int room, RoomCapacityAttachmentState attachment, IEnumerable<int> ids)
        {
            RoomSpellUpgradeState tracker = attachment.SpellUpgrade!;
            var tracked = tracker.Cards.ToList();
            foreach (int id in ids)
            {
                CardInstanceState? card = context.FindCard(id);
                if (tracked.Contains(id) || card?.MaskDescriptor?.Definition.CanScaleWithMagicPower != true) continue;
                if (!card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == tracker.TrackerId))
                {
                    // Multiplying-magic-power relics and additive magic traits remain rejected
                    // by the existing relic and trait validators.
                    CardUpgradeModifier upgrade = tracker.Upgrade.WithRuntimeIdentity(tracker.TrackerId);
                    if (CardMagicPowerModel.Multiplier(card) > 1)
                        upgrade = CardMagicPowerModel.Scale(upgrade, CardMagicPowerModel.Multiplier(card));
                    context = context.WithCard(card.WithModifiers(card.Permanent,
                        UnitModifierModel.Add(card.Temporary, upgrade), refreshTraits: false));
                }
                tracked.Add(id);
            }
            return Put(context, room, attachment, tracked);
        }

        internal static CombatContext Select(CombatContext context, int? before, int? after)
        {
            if (before == after) return context;
            foreach (var entry in Entries(context).ToArray())
                if (entry.Room == before) context = Clear(context, entry.Room, entry.Attachment);
            foreach (var entry in Entries(context).ToArray())
                if (entry.Room == after) context = Add(context, entry.Room, entry.Attachment, context.Cards.Hand.Select(card => card.InstanceId));
            return context;
        }
        internal static CombatContext Drawn(CombatContext context, IEnumerable<int> cards)
        {
            foreach (var entry in Entries(context).Where(entry => entry.Room == context.QueryFrame?.SelectedRoomIndex).ToArray())
                context = Add(context, entry.Room, entry.Attachment, cards);
            return context;
        }
        internal static CombatContext Discarded(CombatContext context, int id)
        {
            foreach (var entry in Entries(context).Where(entry => entry.Room == context.QueryFrame?.SelectedRoomIndex).ToArray())
                context = Clear(context, entry.Room, entry.Attachment, id);
            return context;
        }
        internal static CombatContext Updated(CombatContext context, int id) => Drawn(Discarded(context, id), [id]);

        internal static CombatContext Replace(CombatContext context, IReadOnlyList<RoomCapacityState> after)
        {
            var before = Entries(context).ToArray();
            foreach (var entry in before)
                if (!after.Any(room => room.Attachments?.Any(value => value.AttachmentId == entry.Attachment.AttachmentId) == true))
                    context = Clear(context, entry.Room, entry.Attachment);
            context = context.WithRoomCapacities(after);
            if (context.MagicPower != null)
                context = context.WithMagicPower(context.MagicPower.Select(power => new RoomMagicPower(power.RoomIndex, power.Team,
                    unchecked(power.Value - before.Where(entry => entry.Room == power.RoomIndex).Sum(entry => entry.Attachment.SpellUpgrade!.Upgrade.Stats.Damage) +
                        Entries(context).Where(entry => entry.Room == power.RoomIndex).Sum(entry => entry.Attachment.SpellUpgrade!.Upgrade.Stats.Damage)), power.Conduit)).ToArray());
            return Drawn(context, context.Cards.Hand.Select(card => card.InstanceId));
        }
    }
}
