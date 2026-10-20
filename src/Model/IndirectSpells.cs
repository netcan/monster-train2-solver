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
        public static TrainSpellResult Apply(TrainCombatState source, int cardId, BattlePlayRules definitions)
        {
            TrainSpellResult Unsupported(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported, Array.Empty<CombatEvent>(), reason);
            CombatContext? context = source.Context;
            CardInstanceState? card = context?.FindCard(cardId);
            CardPlayRule? raw = definitions.Cards.FirstOrDefault(rule => rule.DataId == card?.DataId);
            if (context == null || card == null || raw == null || raw.CardType != "Spell" || !IsTreasure(card))
                return Unsupported("Indirect Treasure casting requires a captured original spell and trait.");
            if (context.Relics?.Any(relic => relic.EffectTypes.Count > 0) == true || raw.OnCastEffects.Count > 0 ||
                raw.OnUnplayedNegativeEffects.Count > 0 || CardBranchMaskModel.Resolve(card).Traits.Any(trait =>
                    trait is not (TreasureTrait or "CardTraitStrongerMagicPower" or "CardTraitFreeze")))
                return Unsupported("Indirect Treasure casting with additional relic or card callbacks requires native calibration.");
            CardPlayRule rule = CardModifierModel.Resolve(raw, card);
            int roomIndex = context.QueryFrame?.SelectedRoomIndex ?? -1;
            RoomCombatState? room = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (room == null || rule.Effects.Any(effect => effect.Target == "DropTargetCharacter"))
                return Unsupported("Indirect Treasure casting requires its selected room and automatic targets.");
            SpellCastCheck tested = CardSpellModel.TestPlay(source, roomIndex, rule.Effects, 0, definitions, sourceCardId: cardId);
            if (!tested.Supported) return Unsupported(tested.UnsupportedReason!);
            if (!tested.CanPlay) return new TrainSpellResult(source, RoomOutcome.Exchanged, Array.Empty<CombatEvent>());
            context = context.WithBattleRng(tested.BattleRngAfterTests!.Value).WithAbilityActivator(
                context.LastAbilityActivatorUnitId.HasValue ? 0 : (int?)null);
            int[] cached = room.Units.Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).ToArray();
            if (card.PlayedRoomUnitIds != null) context = context.WithCard(card.WithPlayedRoomUnits(cached));
            TrainSpellResult applied = CardSpellModel.ApplyForCardPlay(CardSpellModel.WithContext(source, context), roomIndex,
                rule.Effects, 0, cardId, definitions, context.OtherPiles);
            if (!applied.Supported) return applied;
            TrainCombatState state = applied.State!;
            RoomOutcome outcome = applied.Outcome;
            var events = applied.Events.ToList();
            context = state.Context!;
            BattleStatistics? statistics = context.LiveStatistics?.Increment(cardId, "TimesPlayed", requireTrackedCard: true);
            state = CardSpellModel.WithContext(state, context.WithStatistics(statistics).AfterCardEffects());
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
            context = state.Context!.WithStatistics(state.Context.Statistics?.WithPlayedCost(cardId, null));
            return new TrainSpellResult(CardSpellModel.WithContext(state, context), outcome, events,
                otherPiles: context.OtherPiles);
        }
    }
}
