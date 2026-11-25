using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitReturnRule
    {
        public bool KeepTemporaryUpgrades { get; }
        public bool OverflowToDraw { get; }
        public bool ReturnEquipment { get; }
        public UnitReturnRule(bool keepTemporaryUpgrades, bool overflowToDraw, bool returnEquipment)
        { KeepTemporaryUpgrades = keepTemporaryUpgrades; OverflowToDraw = overflowToDraw; ReturnEquipment = returnEquipment; }
    }

    public sealed class UnitReturnResult
    {
        public TrainCombatState? State { get; }
        public CombatUnit? RetainedUnit { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal UnitReturnResult(TrainCombatState? state, CombatUnit? retained = null, string? error = null)
        { State = state; RetainedUnit = retained; UnsupportedReason = error; }
    }

    public static class UnitReturnModel
    {
        public static UnitReturnResult Apply(TrainCombatState source, int targetId, UnitReturnRule rule)
        {
            CombatContext? context = source.Context;
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == targetId));
            CombatUnit? target = room?.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (context == null || target == null) return Fail("Unit recall requires a captured live target and context.");
            if (room!.Preview) return new(source);
            CardPileState? standby = context.OtherPiles?.FirstOrDefault(pile => pile.Name == "Standby");
            CardToken? token = standby?.Cards.FirstOrDefault(card => card.InstanceId == target.SpawnerCardId);
            if (token == null) return new(source); // Native warns and skips missing spawners/standby membership.
            bool handSpace = context.Cards.Hand.Count < context.MaxHandSize;
            if (!handSpace && !rule.OverflowToDraw) return new(source);
            if (target.EquipmentCards == null || target.EquipmentCards.Count > 0 || target.Ability?.HasAbility == true ||
                context.Enchantments?.EnchanterIds.Count > 0 || target.Status("horde")?.Stacks > 0)
                return Fail("Recall with equipment, ability cleanup, physical points, enchantments or Horde requires native calibration.");
            if (context.Relics?.Any(relic => relic.EffectTypes.Any(effect => effect.Contains("PostDiscard", StringComparison.Ordinal) ||
                effect.Contains("MonsterCardReturned", StringComparison.Ordinal) ||
                effect is "RelicEffectPurgeExhaustedMonsterCard" or "RelicEffectPurgeEquipmentExhaustedMonsterCard")) == true)
                return Fail("Recall relic return/discard callbacks require native calibration.");
            CardInstanceState? card = context.FindCard(token.InstanceId);
            if (card == null || target.DeathState == null) return Fail("Recall requires complete spawner and unit lifecycle state.");
            BattleSpawnPoints? points = context.SpawnPoints;
            if (points != null)
            {
                SpawnPointReference? point = points.Units.FirstOrDefault(unit => unit.UnitId == targetId)?.Current;
                bool supportedStatuses = target.Statuses.Count == 0 ||
                    (rule is { KeepTemporaryUpgrades: true, OverflowToDraw: true, ReturnEquipment: true } &&
                    target.Statuses is [{ Id: "armor", DisplayCategory: "Persistent", MarkedForRemovalAfterCardPlayed: false }]);
                if (!handSpace || target.Team != CombatTeam.Player || target.Health <= 0 || !supportedStatuses ||
                    point == null || point.RoomIndex != room.RoomIndex || point.Team != target.Team ||
                    point.PreviewCopyId.HasValue || point.OuterBossAnchor || context.Enchantments != null ||
                    context.RoomCapacities?.Any(floor => floor.RoomIndex == room.RoomIndex && floor.Attachments?.Count > 0) == true)
                    return Fail("Physical recall with nonstandard unit, room, or hand state requires native calibration.");
                var removed = BattleSpawnPointModel.Apply(points, room, "Remove", target.Team, targetId);
                if (!removed.Supported) return Fail(removed.Error!);
                var compacted = BattleSpawnPointModel.Apply(removed.State!, room, "Compact", target.Team);
                if (!compacted.Supported) return Fail(compacted.Error!);
                context = context.WithSpawnPoints(compacted.State!);
            }
            if (!rule.KeepTemporaryUpgrades)
            {
                if (card.MaskDescriptor?.TemporaryTraits.Count > 0)
                    return Fail("Clearing temporary recall traits requires native lifecycle calibration.");
                card = card.WithModifiers(card.Permanent, new CardModifiers(new CardStatModifier(),
                    Array.Empty<CardUpgradeModifier>(), 0, card.Temporary.ExternalInteractions, card.Temporary.BoxedCards));
            }
            CardCycleState piles = context.Cards;
            context = context.WithCard(CardMagicPowerModel.Refresh(card)).WithCards(new CardCycleState(
                handSpace ? new[] { token }.Concat(piles.Hand).ToArray() : piles.Hand,
                handSpace ? piles.Draw : piles.Draw.Concat(new[] { token }).ToArray(), piles.Discard, piles.Rng,
                piles.DrawModifier, piles.ExternalInteractions, piles.BonusDraw, piles.NextDrawUpgrades, piles.NextUpgradeReferenceId, piles.DeckDrawDisabledThisTurn))
                .WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name is "Standby" or "Exhausted" or "Eaten"
                    ? CardPileModel.Remove(pile, token.InstanceId) : pile).ToArray());
            CombatUnit retained = target.WithDeathState(target.DeathState.WithLifecycle(true, true));
            var rooms = source.Rooms.Select(item => new RoomCombatState(item.RoomIndex, item.Deployment,
                item.RoomIndex == room.RoomIndex
                    ? BattleSpawnPointModel.Order(context.SpawnPoints, item.RoomIndex, item.Units.Where(unit => unit.Id != targetId).ToArray())
                    : item.Units,
                item.ExternalInteractions, context, item.Preview, item.ActiveCompanionBossIds)).ToArray();
            return new(new TrainCombatState(rooms, source.Movement.Where(move => move.UnitId != targetId).ToArray(),
                source.EnemySlotsPerRoom, context), retained);
        }
        private static UnitReturnResult Fail(string error) => new(null, error: error);
    }
}
