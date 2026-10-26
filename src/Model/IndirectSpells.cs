using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class IndirectSpellModel
    {
        public const string TreasureTrait = "CardTraitTreasure";
        public static bool IsTreasure(CardInstanceState? card) => card?.MaskDescriptor != null &&
            CardBranchMaskModel.Resolve(card).Traits.Contains(TreasureTrait, StringComparer.Ordinal);

        // CardManager.PlayAnyCard(ignoreCost:true, discardCard:false, fromDirectPlay:false).
        public static TrainSpellResult Apply(TrainCombatState source, int cardId, BattlePlayRules definitions,
            int? selectedRoom = null, int targetId = 0, bool echo = false)
        {
            TrainSpellResult Unsupported(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported, Array.Empty<CombatEvent>(), reason);
            CombatContext? context = source.Context;
            CardInstanceState? card = context?.FindCard(cardId);
            CardPlayRule? raw = definitions.Cards.FirstOrDefault(rule => rule.DataId == card?.DataId);
            if (context == null || card == null || raw == null || raw.CardType != "Spell" || !echo && !IsTreasure(card))
                return Unsupported("Indirect Treasure casting requires a captured original spell and trait.");
            bool waxenEcho = echo && EchoWaxenSpikeModel.Known(card, raw.Effects) && EchoWaxenSpikeModel.KnownRelics(context);
            CardPlayRule? juiceRule = echo && EchoJuiceSpikeModel.Known(card, raw.Effects) && EchoJuiceSpikeModel.KnownRelics(context)
                ? CardModifierModel.Resolve(raw, card) : null;
            bool juiceEcho = juiceRule != null && EchoJuiceSpikeModel.KnownOnCast(juiceRule.OnCastEffects);
            if (!juiceEcho && raw.OnCastEffects.Count > 0 || raw.OnUnplayedNegativeEffects.Count > 0 || !waxenEcho && !juiceEcho &&
                (context.Relics?.Any(relic => relic.EffectTypes.Count > 0) == true || CardBranchMaskModel.Resolve(card).Traits.Any(trait =>
                    trait is not (TreasureTrait or "CardTraitStrongerMagicPower" or "CardTraitFreeze"))))
                return Unsupported("Indirect Treasure casting with additional relic or card callbacks requires native calibration.");
            CardPlayRule rule = juiceRule ?? CardModifierModel.Resolve(raw, card);
            int roomIndex = selectedRoom ?? context.QueryFrame?.SelectedRoomIndex ?? -1;
            RoomCombatState? room = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (room == null || !echo && rule.Effects.Any(effect => effect.Target == "DropTargetCharacter"))
                return Unsupported("Indirect Treasure casting requires its selected room and automatic targets.");
            SpellCastCheck tested = CardSpellModel.TestPlay(source, roomIndex, rule.Effects, targetId, definitions, sourceCardId: cardId);
            if (!tested.Supported) return Unsupported(tested.UnsupportedReason!);
            if (!tested.CanPlay) return new TrainSpellResult(source, RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
            context = context.WithBattleRng(tested.BattleRngAfterTests!.Value).WithAbilityActivator(
                context.LastAbilityActivatorUnitId.HasValue ? 0 : (int?)null);
            int[] cached = room.Units.Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).ToArray();
            if (card.PlayedRoomUnitIds != null) context = context.WithCard(card.WithPlayedRoomUnits(cached));
            TrainSpellResult applied = CardSpellModel.ApplyForCardPlay(CardSpellModel.WithContext(source, context), roomIndex,
                rule.Effects, targetId, cardId, definitions, context.OtherPiles);
            if (!applied.Supported) return applied;
            TrainCombatState state = applied.State!;
            RoomOutcome outcome = applied.Outcome;
            var events = applied.Events.ToList();
            context = state.Context!;
            BattleStatistics? statistics = context.LiveStatistics?.Increment(cardId, "TimesPlayed", requireTrackedCard: !echo);
            state = CardSpellModel.WithContext(state, context.WithStatistics(statistics).AfterCardEffects());
            if (juiceEcho && source.Rooms.Any(value => value.Preview))
            {
                // PreviewCardRecast fires OnCast too. The native heal writes
                // preview Pyre HP from live tower HP and ignores the live heal
                // prohibition. Limit this witness to the top-floor Echo with
                // an untouched, otherwise empty Pyre room above it.
                RoomCombatState? tower = state.Rooms.SingleOrDefault(value => value.RoomIndex == 3);
                CombatUnit? pyre = tower?.Units.SingleOrDefault(value => value.IsPyre);
                if (roomIndex != 2 || tower?.Units.Count != 1 || pyre == null || pyre.Triggers.Count != 0)
                    return Unsupported("Triple Juice Echo preview requires its calibrated empty Pyre room above the top floor.");
                StatisticQueryResult paid = StatisticQueryModel.Evaluate(state.Context!,
                    new CardStatisticQuery("UnmodifiedPlayedCost", variableCost: true), cardId);
                if (!paid.Supported) return Unsupported(paid.UnsupportedReason!);
                RelicResourceResult maximum = RelicResourceModel.GetPyreMaxHealth(paid.Context!);
                if (!maximum.Supported) return Unsupported(maximum.UnsupportedReason!);
                state = CardSpellModel.WithContext(state, paid.Context!);
                var healed = EnchantmentWorldModel.Sync(tower!.WithContext(state.Context!).WithUnits(new[] {
                    pyre.WithHealth(Math.Min(maximum.Value, unchecked(pyre.Health + paid.Value))) }));
                state = new TrainCombatState(state.Rooms.Select(value => value.RoomIndex == 3 ? healed : value).ToArray(),
                    state.Movement, state.EnemySlotsPerRoom, healed.Context);
            }
            else if (juiceEcho)
            {
                TrainSpellResult cast = CardSpellModel.ApplyForCardPlay(state, roomIndex, rule.OnCastEffects, targetId, cardId,
                    definitions, state.Context!.OtherPiles, onCastEffects: true);
                if (!cast.Supported) return cast;
                state = cast.State!;
                events.AddRange(cast.Events);
                if (cast.Outcome != RoomOutcome.Exchanged) outcome = cast.Outcome;
            }
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
            {
                TrainCombatResult played = CardPlayedTriggerModel.Spell(state, team, cached,
                    team == CombatTeam.Player ? applied.PendingCallbacks : null);
                if (!played.Supported) return Unsupported(played.UnsupportedReason!);
                state = played.State!;
                events.AddRange(played.RoomResults.SelectMany(result => result.Events));
                if (played.Outcome != RoomOutcome.Exchanged) outcome = played.Outcome;
            }
            TrainCombatResult delayed = TrainCombatModel.ProcessDelayedStatusRemovalsAfterCardPlayed(state);
            if (!delayed.Supported) return Unsupported(delayed.UnsupportedReason!);
            state = delayed.State!;
            events.AddRange(delayed.RoomResults.SelectMany(result => result.Events));
            if (delayed.Outcome != RoomOutcome.Exchanged) outcome = delayed.Outcome;
            context = echo && source.Rooms.Any(room => room.Preview) ? state.Context! :
                state.Context!.WithStatistics(state.Context.Statistics?.WithPlayedCost(cardId, null));
            return new TrainSpellResult(CardSpellModel.WithContext(state, context), outcome, events,
                otherPiles: context.OtherPiles);
        }
    }
}
