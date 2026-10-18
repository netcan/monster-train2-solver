using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicOpeningDeckSpawnRule
    {
        public int RoomIndex { get; }
        public IReadOnlyList<int> CachedCardIds { get; }
        public RelicOpeningDeckSpawnRule(int roomIndex, IReadOnlyList<int> cachedCardIds)
        { RoomIndex = roomIndex; CachedCardIds = Array.AsReadOnly(cachedCardIds.ToArray()); }
    }
    internal static class RelicOpeningDeckSpawnModel
    {
        internal const string Effect = "RelicEffectSpawnUnitFromDeck";
        internal const string DataId = "297c5b7b-efb1-4834-96e0-de4a7e42ccbc";
        internal const string AssetKey = "RandomUnitEveryFloor";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect) =>
            owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == true &&
            owner.EffectTypes.SequenceEqual(new[] { "RelicEffectNull", Effect, Effect, Effect }) && effect.EffectIndex >= 1 && effect.EffectIndex <= 3 &&
            effect.ParamInt == effect.EffectIndex - 1 && effect.ParamInt2 == 0 && effect.Amount == 0 && !effect.CanApplyInPreviewMode &&
            effect.Conditions.Count == 0 && effect.RequiredMutatorId == null && effect.DeckSpawn != null &&
            effect.DeckSpawn.CachedCardIds.All(id => id > 0) && effect.DeckSpawn.CachedCardIds.Distinct().Count() == effect.DeckSpawn.CachedCardIds.Count
            ? null : "Missing or changed original opening deck-spawn definition/cache.";
        internal static TrainCombatResult Test(TrainCombatState source, string ownerId, int index, out bool passed)
        {
            passed = false;
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == ownerId);
            RelicResourceEffect? effect = owner?.ResourceEffects?.SingleOrDefault(item => item.EffectIndex == index);
            string? error = effect == null ? "Missing opening deck-spawn owner/effect." : Validate(owner!, effect);
            if (error != null) return Fail(error);
            if (!RelicOpeningSpawnModel.Test(effect!.ParamInt, source.Rooms.Count)) return Match(source);
            var candidates = new List<int>();
            foreach (CardToken token in source.Context!.Cards.Draw)
            {
                string? type = source.Context.FindCard(token.InstanceId)?.MaskDescriptor?.Definition.CardType;
                if (type == null) return Fail("Opening deck-spawn requires native card-type metadata.");
                if (type == "Monster") candidates.Add(token.InstanceId);
            }
            passed = candidates.Count > 0;
            return Match(Replace(source, owner!, effect.WithDeckSpawn(new(effect.ParamInt, candidates))));
        }
        internal static TrainCombatResult Apply(TrainCombatState source, BattlePlayRules definitions, string ownerId, int index)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == ownerId);
            RelicResourceEffect? effect = owner?.ResourceEffects?.SingleOrDefault(item => item.EffectIndex == index);
            string? error = effect == null ? "Missing opening deck-spawn owner/effect." : Validate(owner!, effect);
            if (error != null) return Fail(error);
            if (effect!.DeckSpawn!.CachedCardIds.Count == 0 || effect.DeckSpawn.RoomIndex != effect.ParamInt)
                return Fail("Opening deck-spawn Apply requires a successful native Test cache.");
            var shuffled = source.Context!.Cards.Rng.Shuffle(effect.DeckSpawn.CachedCardIds);
            TrainCombatState state = Replace(source, owner!, effect.WithDeckSpawn(new(effect.ParamInt, shuffled.Items)));
            CombatContext context = state.Context!;
            context = context.WithCards(Cycle(context.Cards, context.Cards.Draw, context.Cards.Discard, shuffled.State));
            if (context.QueryFrame?.SelectedRoomIndex != null) context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: effect.ParamInt));
            state = CardSpellModel.WithContext(state, context);
            int selected = shuffled.Items[0]; CardToken? token = context.Cards.Draw.SingleOrDefault(item => item.InstanceId == selected);
            CardPlayRule? rule = token == null ? null : definitions.Cards.SingleOrDefault(item => item.DataId == token.DataId);
            if (rule?.CardType != "Monster" || rule.SpawnUnit == null || rule.ExternalInteractions.Count > 0 ||
                rule.OnCastEffects.Count > 0 || rule.HandDiscardInteractions?.Count > 0)
                return Fail("Opening deck-spawn requires an eligible captured unit definition and supported discard callbacks.");
            RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == effect.ParamInt);
            int position = context.SpawnPoints?.FirstEmpty(room.RoomIndex, CombatTeam.Player) ?? -1;
            if (position < 0) return Fail("Opening deck-spawn full-slot failure needs separate native evidence.");
            UnitBirthResult born = UnitBirthModel.SpawnFront(room, rule, selected, position, false, null);
            if (!born.Supported) return Fail(born.Result.UnsupportedReason!);
            context = born.Result.State!.Context!;
            state = CardSpellModel.WithContext(new(state.Rooms.Select(item => item.RoomIndex == room.RoomIndex ? born.Result.State : item).ToArray(),
                state.Movement, state.EnemySlotsPerRoom, context), context);
            CardInstanceState card = context.FindCard(selected)!;
            if (card.MaskDescriptor != null && CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitCorrupt"))
                return Fail("Corrupt-trait opening units need separate native corruption evidence.");
            CardPileState? standby = context.OtherPiles?.SingleOrDefault(pile => pile.Name == "Standby");
            if (standby?.UnitConditions == null) return Fail("Opening deck-spawn requires native standby bindings.");
            CombatUnit? unit = state.Rooms.SelectMany(item => item.Units).SingleOrDefault(item => item.SpawnerCardId == selected);
            if (unit == null) return Fail("Opening deck-spawn removal during birth needs separate native evidence.");
            var discarded = RelicCheaperSpellUpgradeModel.CardDiscarded(context, selected);
            if (!discarded.Supported) return Fail(discarded.UnsupportedReason!);
            context = discarded.Context;
            context = context.WithCards(Cycle(context.Cards, context.Cards.Draw.Where(item => item.InstanceId != selected).ToArray(),
                context.Cards.Discard, context.Cards.Rng));
            context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby" ?
                CardPileModel.BindUnit(CardPileModel.Add(pile, token!), new UnitStandbyCondition(selected, unit.Id, false)) :
                CardPileModel.Remove(pile, selected)).ToArray());
            context = context.WithStatistics(context.LiveStatistics?.Increment(selected, "TimesDiscarded"));
            context = context.WithCard(context.FindCard(selected)!.OnDiscard(true));
            context = UnitStandbyModel.ReturnReady(CardExhaustModel.ReturnReady(context));
            return Match(CardSpellModel.WithContext(state, context));
        }
        private static TrainCombatState Replace(TrainCombatState source, CombatRelicState owner, RelicResourceEffect effect) =>
            CardSpellModel.WithContext(source, source.Context!.WithRelics(source.Context.Relics!.Select(item => item.DataId == owner.DataId ?
                item.WithResourceEffects(item.ResourceEffects!.Select(rule => rule.EffectIndex == effect.EffectIndex ? effect : rule).ToArray()) : item).ToArray()));
        private static CardCycleState Cycle(CardCycleState source, IReadOnlyList<CardToken> draw, IReadOnlyList<CardToken> discard, UnityRng rng) =>
            new(source.Hand, draw, discard, rng, source.DrawModifier, source.ExternalInteractions, source.BonusDraw, source.NextDrawUpgrades, source.NextUpgradeReferenceId);
        private static TrainCombatResult Match(TrainCombatState state) => new(state, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        private static TrainCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
