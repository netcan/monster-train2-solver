using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RoomRecursionModel
    {
        public static TrainSpellResult Apply(TrainCombatState source, int roomIndex, CardActionEffect effect,
            BattlePlayRules? definitions, int sourceCardId)
        {
            TrainCombatState state = source;
            var events = new List<CombatEvent>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            CombatContext? context = state.Context;
            if (context?.SpawnPoints == null || definitions == null || source.Rooms.Any(room => room.Preview))
                return Fail("Room recursion requires physical spawn points, a complete birth catalog and primary execution.");
            if (effect.Recursion?.SelectionMode != "RandomToRoomUntilCapacityFull" || effect.Recursion.CardType != "Monster")
                return Fail("Room recursion requires the calibrated random-to-capacity Monster selection.");
            if (context.Relics?.Any(relic => relic.EffectTypes.Count > 0) == true)
                return Fail("Room recursion with relic birth and discard callbacks requires native calibration.");
            IReadOnlyList<int> eligible = CardRecursionModel.EligibleCards(context.Cards, context.OtherPiles,
                effect, definitions, sourceCardId, out string? error, context.CardRegistry);
            if (error != null) return Fail(error);
            if (eligible.Count == 0) return Result();
            ShuffleResult<int> shuffled = context.BattleRng.Shuffle(eligible);
            state = CardSpellModel.WithContext(state, context.WithBattleRng(shuffled.State));
            foreach (int cardId in shuffled.Items)
            {
                context = state.Context!;
                RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == roomIndex);
                int? capacity = RoomCapacityModel.Maximum(context, roomIndex, CombatTeam.Player);
                if (!capacity.HasValue) return Fail("Room recursion requires captured friendly capacity.");
                // Native checks strictly greater before each birth, allowing one overflow.
                if (room.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Size) > capacity.Value) break;
                int position = context.SpawnPoints!.FirstEmpty(roomIndex, CombatTeam.Player);
                if (position < 0) break;
                CardInstanceState? card = context.FindCard(cardId);
                CardPlayRule? definition = definitions.Cards.SingleOrDefault(rule => rule.DataId == card?.DataId);
                if (card?.MaskDescriptor == null || definition?.SpawnUnit == null || definition.Effect != "SpawnMonster")
                    return Fail("Room recursion requires retained card metadata and its native monster birth.");
                if (card.MaskDescriptor.BaseTraits.Count > 0 || definition.OnCastEffects.Count > 0 || definition.Effects.Count > 0)
                    return Fail("Room recursion with card-trait or post-spawn callbacks requires native calibration.");
                if (effect.Upgrade?.Lifecycle?.ReplacedAssets.Count > 0)
                    return Fail("Room recursion upgrade replacement requires native calibration.");
                CardPileState[] piles = context.OtherPiles!.ToArray();
                string sourcePile = effect.Target == "Exhaust" ? "Exhausted" : "Eaten";
                CardToken? token = piles.Single(pile => pile.Name == sourcePile).Cards.SingleOrDefault(item => item.InstanceId == cardId);
                if (token == null) return Fail("Room recursion selected a card that left its source pile.");
                piles = piles.Select(pile => pile.Name is "Exhausted" or "Eaten" or "Standby" ? CardPileModel.Remove(pile, cardId) : pile).ToArray();
                card = CardExhaustModel.ResetTraits(card);
                if (effect.Upgrade != null)
                    card = card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, effect.Upgrade));
                context = context.WithOtherPiles(piles).WithCard(card);
                state = CardSpellModel.WithContext(state, context);
                room = state.Rooms.Single(item => item.RoomIndex == roomIndex);
                UnitBirthResult birth = UnitBirthModel.SpawnFront(room, definition, cardId, position, false, null);
                if (!birth.Supported) return Fail(birth.Result.UnsupportedReason!);
                RoomCombatState born = birth.Result.State!;
                context = born.Context!;
                piles = context.OtherPiles!.Select(pile => pile.Name == "Standby" ?
                    CardPileModel.BindUnit(CardPileModel.Add(pile, token), new UnitStandbyCondition(cardId, birth.UnitId)) : pile).ToArray();
                state = new TrainCombatState(state.Rooms.Select(item => item.RoomIndex == roomIndex ? born :
                    new RoomCombatState(item.RoomIndex, item.Deployment, item.Units, item.ExternalInteractions,
                        context, item.Preview, item.ActiveCompanionBossIds)).ToArray(), state.Movement, state.EnemySlotsPerRoom, context.WithOtherPiles(piles));
                state = CardSpellModel.WithContext(state, state.Context!);
                events.AddRange(birth.Result.Events);
                if (birth.Result.Outcome != RoomOutcome.Exchanged) outcome = birth.Result.Outcome;
                TrainCombatResult ordered = EnchantmentWorldModel.UpdateAll(state);
                if (!ordered.Supported) return Fail(ordered.UnsupportedReason!);
                state = ordered.State!;
                if (ordered.Outcome != RoomOutcome.Exchanged) outcome = ordered.Outcome;
            }
            return Result();
            TrainSpellResult Result() => new TrainSpellResult(state, outcome, events, null,
                otherPiles: state.Context!.OtherPiles, playRules: definitions);
            TrainSpellResult Fail(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported, events, reason);
        }
    }
}
