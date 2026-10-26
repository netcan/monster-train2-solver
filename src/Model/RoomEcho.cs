using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomEchoState
    {
        public int OriginalCardId { get; }
        public int CopyCardId { get; }
        public int RoomIndex { get; }
        public SpawnPointReference? DropLocation { get; }
        public int PaidCost { get; }
        public int Corruption { get; }
        public IReadOnlyList<CardActionEffect>? Effects { get; }
        public RoomEchoState(int originalCardId = 0, int copyCardId = 0, int roomIndex = 0,
            SpawnPointReference? dropLocation = null, int paidCost = 0, int corruption = -1,
            IReadOnlyList<CardActionEffect>? effects = null)
        { OriginalCardId = originalCardId; CopyCardId = copyCardId; RoomIndex = roomIndex; DropLocation = dropLocation;
            PaidCost = paidCost; Corruption = corruption; Effects = effects == null ? null : Array.AsReadOnly(effects.ToArray()); }
    }
    internal static class RoomEchoModel
    {
        internal static TrainSpellResult Remember(TrainCombatState source, BattleTurnState before, PlayCardAction action, CardPlayRule rule)
        {
            TrainSpellResult Fail(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported, Array.Empty<CombatEvent>(), reason);
            if (source.Rooms.Any(room => room.Deployment) || rule.CardType != "Spell" || rule.IsAnyAbility == true || rule.Ability != null)
                return new TrainSpellResult(source, RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
            CombatContext context = source.Context!;
            foreach (RoomCapacityState room in context.RoomCapacities ?? Array.Empty<RoomCapacityState>())
            foreach (RoomCapacityAttachmentState attachment in room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
            {
                if (room.RoomIndex != action.RoomIndex || attachment.Echo == null || attachment.Echo.OriginalCardId == action.CardInstanceId) continue;
                CardInstanceState card = RelicFreezeHandUpgradeModel.Played(context.FindCard(action.CardInstanceId)!);
                context = context.WithCard(card);
                bool waxen = EchoWaxenSpikeModel.Known(card, rule.Effects) && EchoWaxenSpikeModel.KnownRelics(context);
                bool juice = EchoJuiceSpikeModel.Known(card, rule.Effects) && EchoJuiceSpikeModel.KnownOnCast(rule.OnCastEffects) &&
                    EchoJuiceSpikeModel.KnownRelics(context);
                bool justCause = EchoJustCauseModel.Known(card, rule.Effects) && EchoJuiceSpikeModel.KnownRelics(context);
                if (!juice && rule.OnCastEffects.Count > 0 || rule.OnUnplayedNegativeEffects.Count > 0 || rule.ExternalInteractions.Count > 0 ||
                    !waxen && !juice && !justCause && (CardBranchMaskModel.Resolve(card).Traits.Any(trait => trait != "CardTraitFreeze") || context.Relics?.Any(relic => relic.EffectTypes.Count > 0) == true))
                    return Fail("Echo replay needs native calibration: onCast=" + rule.OnCastEffects.Count + ", unplayed=" +
                        rule.OnUnplayedNegativeEffects.Count + ", interactions=" + string.Join(";", rule.ExternalInteractions) +
                        ", traits=" + string.Join(";", CardBranchMaskModel.Resolve(card).Traits) + ", relics=" +
                        string.Join(";", context.Relics?.SelectMany(relic => relic.EffectTypes) ?? Array.Empty<string>()));
                var creation = new CardCreationRule(card.DataId, CardModifiers.Empty(), card.EffectCounters, card.ExternalInteractions,
                    card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits, card.CapacityScalingTraits,
                    card.EquippedUnitId, card.MaskDescriptor, card.ForgeCountScalingTraits, card.CanBeCopied, card.TrainHealScalingTraits);
                CardGenerationResult copied = CardGenerationModel.CloneDetached(context, creation, card.InstanceId);
                if (!copied.Supported) return Fail(copied.UnsupportedReason!);
                context = copied.Context!;
                CardInstanceState clone = context.FindCard(copied.AddedCards.Single().InstanceId)!;
                context = context.WithCard(clone.WithPlayedCost(card.LastPlayedCost));
                SpawnPointReference? point = before.Spawn.Train.Context!.SpawnPoints?.Units.SingleOrDefault(unit => unit.UnitId == action.TargetUnitId)?.Current;
                int corruption = before.Spawn.Train.Context.RoomCapacities!.Single(value => value.RoomIndex == action.RoomIndex).Corruption ?? 0;
                context = Put(context, room.RoomIndex, attachment, new RoomEchoState(card.InstanceId, clone.InstanceId,
                    action.RoomIndex, point, card.LastPlayedCost, corruption, rule.Effects));
            }
            return new TrainSpellResult(CardSpellModel.WithContext(source, context), RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
        }
        private static CombatContext Put(CombatContext context, int roomIndex, RoomCapacityAttachmentState owner, RoomEchoState echo) =>
            context.WithRoomCapacities(context.RoomCapacities!.Select(room => room.RoomIndex != roomIndex ? room :
                new RoomCapacityState(room.RoomIndex, room.PlayerMaximum, room.EnemyMaximum, room.IsPyre, room.Corruption,
                    room.Attachments!.Select(attachment => attachment == owner ? attachment.WithEcho(echo) : attachment).ToArray(),
                    room.MaxCorruption, room.PermanentCorruption, room.AttachmentsAddedFromCards)).ToArray());

        internal static RoomCombatResult PreCombat(RoomCombatState source)
        {
            RoomCombatState current = source;
            var events = new List<CombatEvent>();
            foreach (RoomCapacityAttachmentState attachment in source.Context?.RoomCapacities?
                .Single(room => room.RoomIndex == source.RoomIndex).Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
            {
                RoomEchoState? echo = attachment.Echo;
                if (echo?.CopyCardId is not > 0) continue;
                RoomCombatResult Fail(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, reason);
                CombatContext context = current.Context!;
                if (context.Enchantments == null || context.SpawnPoints == null || echo.Effects == null)
                    return Fail("Echo replay requires its captured physical train and original spell effects.");
                if (context.RoomCapacities!.Single(room => room.RoomIndex == echo.RoomIndex).Corruption < echo.Corruption)
                    return Fail("Echo temporary Corruption restoration needs its own native witness.");
                int target = 0;
                if (echo.DropLocation != null)
                {
                    SpawnPointGroupState? group = context.SpawnPoints.Group(echo.RoomIndex, echo.DropLocation.Team, echo.DropLocation.PreviewCopyId);
                    if (group == null || echo.DropLocation.Index >= group.GroupCount) return Fail("Missing remembered Echo drop location.");
                    target = group.Occupants[echo.DropLocation.Index];
                    if (target == 0 && echo.DropLocation.Index > 0) target = group.Occupants[0];
                }
                context = context.WithStatistics(context.Statistics?.WithPlayedCost(echo.CopyCardId, echo.PaidCost));
                current = current.WithContext(context);
                var definitions = new BattlePlayRules(context.RoomCapacities!.Select(room => new RoomPlayRule(room.RoomIndex,
                    room.PlayerMaximum, context.SpawnPoints.SelectedGroup(room.RoomIndex, CombatTeam.Player)!.GroupCount,
                    true, false, room.IsPyre, room.EnemyMaximum)).ToArray(), new[] { new CardPlayRule(context.FindCard(echo.CopyCardId)!.DataId,
                        "EchoCopy", echo.PaidCost, "Spell", "Discard", null, Array.Empty<string>(), effects: echo.Effects, cardType: "Spell", isAnyAbility: false) }, context.StatusRules);
                TrainCombatState train = EnchantmentWorldModel.Rebase(new TrainCombatState(context.Enchantments.Rooms.Select(room =>
                    room.RoomIndex == current.RoomIndex ? current : EnchantmentWorldModel.Room(room, context)).ToArray(),
                    context.Enchantments.Movement, context.Enchantments.EnemySlotsPerRoom, context));
                TrainSpellResult replay = IndirectSpellModel.Apply(train, echo.CopyCardId, definitions, echo.RoomIndex, target, echo: true);
                if (!replay.Supported) return Fail(replay.UnsupportedReason!);
                context = replay.State!.Context!;
                if (!source.Preview) context = Put(context, current.RoomIndex,
                    context.RoomCapacities!.Single(room => room.RoomIndex == current.RoomIndex).Attachments!.Single(value => value.DataId == attachment.DataId),
                    new RoomEchoState(roomIndex: -1));
                train = EnchantmentWorldModel.Rebase(CardSpellModel.WithContext(replay.State, context));
                current = train.Rooms.Single(room => room.RoomIndex == source.RoomIndex);
                events.AddRange(replay.Events);
                if (replay.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
                    return new RoomCombatResult(current, replay.Outcome, 0, events);
            }
            return new RoomCombatResult(current, RoomOutcome.Exchanged, 0, events);
        }
    }
}
