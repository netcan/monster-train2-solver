using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class IndirectUnitModel
    {
        internal static TrainSpellResult Apply(TrainCombatState source, CardInstanceState card, CardPlayRule raw,
            BattlePlayRules definitions, int roomIndex)
        {
            TrainSpellResult Unsupported(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported, Array.Empty<CombatEvent>(), reason);
            TrainSpellResult Unplayed() => new TrainSpellResult(source, RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
            RoomCombatState? room = source.Rooms.SingleOrDefault(value => value.RoomIndex == roomIndex);
            RoomPlayRule? capacity = definitions.Rooms.SingleOrDefault(value => value.RoomIndex == roomIndex);
            if (room == null || capacity == null || source.Context == null)
                return Unsupported("Indirect unit casting requires its selected native room.");
            if (room.Preview) return Unsupported("Indirect unit casting during native preview requires its birth-copy integration.");
            if (raw.Effect != "SpawnMonster" || raw.SpawnUnit == null || raw.Summon != null || raw.Effects.Count != 0)
                return Unsupported("Indirect pooled or compound unit casting requires its native effect sequence.");
            CardPlayRule resolved = CardModifierModel.Resolve(raw, card);
            if (resolved.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", resolved.ExternalInteractions));
            var occupants = room.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
            // Native GetCanSpawnMonster(dropLocation:null) tests only an empty
            // physical point. Automatic Treasure births can exceed room capacity.
            if (!capacity.Enabled || capacity.IsPyre || occupants.Length >= capacity.PlayerSlots) return Unplayed();
            int position = source.Context.SpawnPoints?.FirstEmpty(roomIndex, CombatTeam.Player) ?? occupants.Length;
            if (position < 0 || position >= capacity.PlayerSlots) return Unplayed();
            CardPileState? standby = source.Context.OtherPiles?.SingleOrDefault(pile => pile.Name == "Standby");
            if (standby?.UnitConditions == null || source.Context.OtherPiles!.All(pile => pile.Name != "DiscardBuffer"))
                return Unsupported("Indirect unit casting requires the original standby binding and discard buffer.");
            var cached = room.Units.Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).ToArray();
            CombatContext context = source.Context.WithAbilityActivator(source.Context.LastAbilityActivatorUnitId.HasValue ? 0 : (int?)null);
            if (card.PlayedRoomUnitIds != null) context = context.WithCard(card.WithPlayedRoomUnits(cached));
            TrainCombatState state = CardSpellModel.WithContext(source, context);
            void Import(RoomCombatState frame) => state = new TrainCombatState(state.Rooms.Select(value => value.RoomIndex == roomIndex
                ? frame : value.WithContext(frame.Context)).ToArray(), state.Movement, state.EnemySlotsPerRoom, frame.Context);
            RoomCombatResult Drain(RoomCombatState frame, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> queue)
            {
                Import(frame);
                TrainCombatResult drained = TrainCombatModel.ApplyCharacterQueue(state, queue.ToList());
                if (!drained.Supported) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), drained.UnsupportedReason);
                state = drained.State!;
                return new RoomCombatResult(state.Rooms.Single(value => value.RoomIndex == roomIndex), drained.Outcome, 0,
                    drained.RoomResults.SelectMany(result => result.Events).ToList());
            }
            UnitBirthResult birth = UnitBirthModel.SpawnIndirectCard(state.Rooms.Single(value => value.RoomIndex == roomIndex),
                raw, card.InstanceId, position, Drain);
            if (!birth.Supported) return Unsupported(birth.Result.UnsupportedReason!);
            Import(birth.Result.State!);
            var events = birth.Result.Events.ToList(); RoomOutcome outcome = birth.Result.Outcome;
            context = state.Context!;
            state = CardSpellModel.WithContext(state, context.WithStatistics(context.LiveStatistics?.Increment(card.InstanceId, "TimesPlayed")).AfterCardEffects());
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            {
                TrainCombatResult rally = CardPlayedTriggerModel.Rally(state, team, cached);
                if (!rally.Supported) return Unsupported(rally.UnsupportedReason!);
                state = rally.State!; events.AddRange(rally.RoomResults.SelectMany(result => result.Events));
                if (rally.Outcome != RoomOutcome.Exchanged) outcome = rally.Outcome;
            }
            state = IndirectSpellModel.ApplyPlayedTraits(state, card.InstanceId);
            TrainCombatResult delayed = TrainCombatModel.ProcessDelayedStatusRemovalsAfterCardPlayed(state);
            if (!delayed.Supported) return Unsupported(delayed.UnsupportedReason!);
            state = delayed.State!; events.AddRange(delayed.RoomResults.SelectMany(result => result.Events));
            if (delayed.Outcome != RoomOutcome.Exchanged) outcome = delayed.Outcome;
            context = state.Context!;
            // CardTraitTreasure moves a successfully summoned source out of the discard
            // buffer and binds it to its live unit after the indirect effect queue.
            if (state.Rooms.SelectMany(value => value.Units).Any(unit => unit.Id == birth.UnitId))
            {
                var token = new CardToken(card.InstanceId, card.DataId); var cards = context.Cards;
                context = context.WithCards(new CardCycleState(cards.Hand, cards.Draw,
                    cards.Discard.Where(value => value.InstanceId != card.InstanceId).ToArray(), cards.Rng, cards.DrawModifier,
                    cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn));
                context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby"
                    ? CardPileModel.BindUnit(CardPileModel.Add(pile, token), new UnitStandbyCondition(card.InstanceId, birth.UnitId))
                    : pile.Name == "DiscardBuffer" ? CardPileModel.Remove(pile, card.InstanceId) : pile).ToArray());
            }
            context = context.WithStatistics(context.Statistics?.WithPlayedCost(card.InstanceId, null));
            return new TrainSpellResult(CardSpellModel.WithContext(state, context), outcome, events, otherPiles: context.OtherPiles);
        }
    }
}
