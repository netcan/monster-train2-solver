using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicRoomAttachmentReplication
    {
        public int EffectIndex { get; }
        public int AttachmentType { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicRoomAttachmentReplication(int effectIndex, int attachmentType,
            IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0)
        {
            EffectIndex = effectIndex;
            AttachmentType = attachmentType;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }

        internal RelicRoomAttachmentReplication WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicRoomAttachmentReplication(EffectIndex, AttachmentType, conditions);
    }

    internal static class RelicRoomAttachmentReplicationModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.FirstRoomCardOnAllFloors)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.RoomAttachmentReplications?.Count > 0 ? "Unexpected room-attachment replication definitions." : null;
            if (relic.DataId != RelicModel.FirstRoomCardRelicId || relic.AssetKey != RelicModel.FirstRoomCardRelicKey ||
                relic.RoomAttachmentReplications == null ||
                !indices.SequenceEqual(relic.RoomAttachmentReplications.Select(effect => effect.EffectIndex)))
                return "Missing ordered native room-attachment replication definition.";
            if (relic.RoomAttachmentReplications.Any(effect => effect.AttachmentType != 0 ||
                effect.ConditionCount != effect.Conditions.Count || effect.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7)))
                return "Incomplete native room-attachment replication definition.";
            return null;
        }

        internal static RoomCapacityResult ApplyOne(CombatContext source, int playedCardId, BattlePlayRules? definitions,
            int relicIndex, int effectIndex, int selectedRoomIndex)
        {
            if (source.Relics == null || relicIndex < 0 || relicIndex >= source.Relics.Count)
                return Fail("Missing room-attachment replication relic state.");
            CombatRelicState relic = source.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= relic.EffectTypes.Count ||
                relic.EffectTypes[effectIndex] != RelicModel.FirstRoomCardOnAllFloors)
                return Fail("Room-attachment replication effect order changed.");
            RelicRoomAttachmentReplication? effect = relic.RoomAttachmentReplications?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (effect == null) return Fail("Missing ordered room-attachment replication definition.");

            CombatContext context = source;
            if (effect.Conditions.Count > 0)
            {
                RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, effect.Conditions);
                if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
                context = evaluation.Context;
                if (!evaluation.Passed) return new RoomCapacityResult(context, 0);
            }

            CardInstanceState? played = context.FindCard(playedCardId);
            CardPlayRule? card = played == null ? null : definitions?.Cards.FirstOrDefault(item => item.DataId == played.DataId);
            if (played == null || card?.CardType == null)
                return Fail("Room-attachment replication requires the played card type.");
            if (card.CardType != "TrainRoomAttachment") return new RoomCapacityResult(context, 0);

            RoomCapacityState[]? rooms = context.RoomCapacities?.ToArray();
            if (rooms == null || rooms.Any(room => !room.IsPyre && room.AttachmentsAddedFromCards == null))
                return Fail("Room-attachment replication requires native per-floor card attachment counters.");
            int attachmentCount = rooms.Sum(room => room.AttachmentsAddedFromCards ?? 0);
            if (attachmentCount > 1) return new RoomCapacityResult(context, 0);

            IReadOnlyList<CardActionEffect> attachments = card.Effects.Where(item => item.Type == "AttachRoomCapacity").ToArray();
            if (attachments.Count > 1)
                return Fail("Multiple attachment upgrades on one card are not modeled as a single native room attachment.");

            bool limitedRange;
            try
            {
                CardUpgradeMaskCard mask = CardBranchMaskModel.Resolve(played);
                limitedRange = mask.Traits.Contains("CardTraitLimitedRange", StringComparer.Ordinal);
            }
            catch (InvalidOperationException error) { return Fail(error.Message); }

            if (effect.Conditions.Count > 0)
            {
                RelicConditionRecord record = RelicConditionModel.Record(context, effect.Conditions);
                if (!record.Supported) return Fail(record.UnsupportedReason!);
                context = record.Context;
                CombatRelicState[] relics = context.Relics!.ToArray();
                RelicRoomAttachmentReplication[] definitionsForRelic = relics[relicIndex].RoomAttachmentReplications!.ToArray();
                int offset = Array.FindIndex(definitionsForRelic, item => item.EffectIndex == effectIndex);
                if (offset < 0) return Fail("Room-attachment replication effect order changed while recording its condition.");
                definitionsForRelic[offset] = effect.WithConditions(record.Conditions);
                relics[relicIndex] = relics[relicIndex].WithRoomAttachmentReplications(definitionsForRelic);
                context = context.WithRelics(relics);
            }

            if (limitedRange || attachments.Count == 0) return new RoomCapacityResult(context, 0);
            RoomCapacityAttachmentState? attachment = attachments[0].RoomAttachment;
            if (attachment == null || attachment.AttachmentType != effect.AttachmentType)
                return Fail("Room-attachment replication requires a matching primary capacity attachment.");
            if (selectedRoomIndex < 0 || !rooms.Any(room => room.RoomIndex == selectedRoomIndex && !room.IsPyre))
                return Fail("Room-attachment replication requires the selected non-Pyre floor.");

            int replicated = 0;
            foreach (RoomCapacityState room in rooms.Where(room => room.RoomIndex != selectedRoomIndex && !room.IsPyre))
            {
                RoomCapacityResult attached = RoomCapacityModel.Attach(context, room.RoomIndex, attachment);
                if (!attached.Supported) return Fail(attached.UnsupportedReason!);
                context = attached.Context!;
                replicated++;
            }
            return new RoomCapacityResult(context, replicated);
        }

        private static RoomCapacityResult Fail(string error) => new(null, 0, error);
    }
}
