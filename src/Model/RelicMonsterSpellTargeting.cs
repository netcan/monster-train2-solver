using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicMonsterSpellTargetingModifier
    {
        public int EffectIndex { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicMonsterSpellTargetingModifier(int effectIndex, IReadOnlyList<RelicConditionState> conditions)
        { EffectIndex = effectIndex; Conditions = Array.AsReadOnly(conditions.ToArray()); }

        internal RelicMonsterSpellTargetingModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicMonsterSpellTargetingModifier(EffectIndex, conditions);
    }

    internal sealed class RelicMonsterSpellTargetingResult
    {
        internal TrainCombatState State { get; }
        internal RoomOutcome Outcome { get; }
        internal int CopiedTargets { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicMonsterSpellTargetingResult(TrainCombatState state, RoomOutcome outcome = RoomOutcome.Exchanged,
            int copiedTargets = 0, string? unsupportedReason = null)
        { State = state; Outcome = outcome; CopiedTargets = copiedTargets; UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicMonsterSpellTargetingModel
    {
        internal const string RelicId = "070c87f3-a48f-49d6-a288-04854ab3b0cf";
        internal const string RelicKey = "SingleToTeamMonsterSpellTargeting";
        internal const string Effect = "RelicEffectMonsterSpellSingleToTeamTargeting";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.MonsterSpellTargetingModifiers?.Count > 0 ? "Unexpected Monster-spell-targeting definitions." : null;
            if (relic.DataId != RelicId || relic.AssetKey != RelicKey || indices.Length != 1 || indices[0] != 0 ||
                relic.EffectTypes.Count != 1 || relic.MonsterSpellTargetingModifiers?.Count != 1 ||
                relic.MonsterSpellTargetingModifiers[0].EffectIndex != 0)
                return "Only the exact original SingleToTeamMonsterSpellTargeting owner is modeled.";
            RelicConditionState[] conditions = relic.MonsterSpellTargetingModifiers[0].Conditions.ToArray();
            if (conditions.Length != 1 || conditions[0].Query.Type != "SubtypeInDeck" ||
                conditions[0].Query.Duration != "ThisTurn" || !conditions[0].TrackTriggerCount ||
                conditions[0].Comparator != 6 || conditions[0].Value != 1 || conditions[0].AllowMultiple)
                return "The native once-per-turn Monster spell condition is missing or changed.";
            return null;
        }

        internal static RelicMonsterSpellTargetingResult Apply(TrainCombatState source, int relicIndex, int effectIndex,
            CardPlayRule playedRule, int playedCardId, int roomIndex, IReadOnlyList<int> lastTargets,
            CombatTeam? originalTargetTeam, BattlePlayRules definitions, int currentTurn)
        {
            CombatContext context = source.Context!;
            CombatRelicState relic = context.Relics![relicIndex];
            RelicMonsterSpellTargetingModifier modifier = relic.MonsterSpellTargetingModifiers!
                .Single(rule => rule.EffectIndex == effectIndex);
            if (playedRule.Effect != "Spell" || playedRule.CardType != "Spell" || playedRule.IsAnyAbility != false ||
                playedRule.Ability != null || !playedRule.Effects.Any(effect => effect.AllowPlayer) ||
                !playedRule.Effects.Any(effect => effect.Target == "DropTargetCharacter") || lastTargets.Count == 0 ||
                originalTargetTeam != CombatTeam.Player)
                return new RelicMonsterSpellTargetingResult(source);

            bool hasPositionChange = playedRule.Effects.Any(effect => effect.Type is "Bump" or "FloorRearrange" or "AbilityTeleport");
            if (hasPositionChange && !playedRule.Effects.Any(effect => effect.Type is not ("Bump" or "FloorRearrange" or "AbilityTeleport")))
                return new RelicMonsterSpellTargetingResult(source);
            if (hasPositionChange || playedRule.Effects.Count != 1 || playedRule.OnCastEffects.Count != 0 ||
                playedRule.Effects[0].Type != "Damage" || playedRule.Effects[0].Target != "DropTargetCharacter" ||
                playedRule.Effects[0].AllowEnemy || !playedRule.Effects[0].AllowPlayer || playedRule.Effects[0].Range != null)
                return Unsupported(source, "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.");

            CardInstanceState? card = context.FindCard(playedCardId);
            if (card == null || card.MaskDescriptor == null || card.Permanent.Upgrades.Count != 0 || card.Temporary.Upgrades.Count != 0)
                return Unsupported(source, "Monster spell replication requires an owned, unupgraded card with captured mask metadata.");
            try
            {
                CardTraitRefreshState traits = CardBranchMaskModel.OwnedState(card).Traits;
                if (CardTraitRefreshModel.Ensure(traits).Composition.CombinedTraits?.Count > 0)
                    return Unsupported(source, "Monster spell replication does not yet model copied card trait callbacks.");
            }
            catch (InvalidOperationException error) { return Unsupported(source, error.Message); }

            RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, modifier.Conditions);
            if (!evaluation.Supported) return Unsupported(source, evaluation.UnsupportedReason!);
            context = evaluation.Context;
            source = CardSpellModel.WithContext(source, context);
            if (!evaluation.Passed) return new RelicMonsterSpellTargetingResult(source);

            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
            if (room == null) return Unsupported(source, "Monster spell replication requires its selected room.");
            int originalTargetId = lastTargets[0];
            int[] teammateTargets = room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && !unit.IsPyre &&
                unit.Id != originalTargetId && unit.Status("untouchable") == null &&
                unit.DeathState?.IsBeingRemoved != true && unit.DeathState?.IsDestroyed != true &&
                unit.DeathState?.IsDespawned != true).Select(unit => unit.Id).ToArray();
            if (teammateTargets.Length == 0) return new RelicMonsterSpellTargetingResult(source);

            RelicConditionRecord recorded = RelicConditionModel.Record(context, modifier.Conditions);
            if (!recorded.Supported) return Unsupported(source, recorded.UnsupportedReason!);
            RelicMonsterSpellTargetingModifier[] updatedRules = relic.MonsterSpellTargetingModifiers!.ToArray();
            int ruleOffset = Array.FindIndex(updatedRules, item => item.EffectIndex == effectIndex);
            updatedRules[ruleOffset] = modifier.WithConditions(recorded.Conditions);
            CombatRelicState[] updatedRelics = recorded.Context.Relics!.ToArray();
            updatedRelics[relicIndex] = updatedRelics[relicIndex].WithMonsterSpellTargetingModifiers(updatedRules);
            source = CardSpellModel.WithContext(source, recorded.Context.WithRelics(updatedRelics));

            int copied = 0;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (int targetId in teammateTargets)
            {
                room = source.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
                if (room?.Units.Any(unit => unit.Id == targetId && unit.Health > 0) != true) continue;
                SpellCastCheck test = CardSpellModel.TestPlay(source, roomIndex, playedRule.Effects, targetId,
                    definitions, sourceCardId: playedCardId);
                if (!test.Supported) return Unsupported(source, test.UnsupportedReason!);
                if (!test.CanPlay) continue;
                source = CardSpellModel.WithContext(source, source.Context!.WithBattleRng(test.BattleRngAfterTests!.Value));
                TrainSpellResult applied = CardSpellModel.ApplyForCardPlay(source, roomIndex, playedRule.Effects,
                    targetId, playedCardId, definitions, source.Context!.OtherPiles, priorTargets: [targetId]);
                if (!applied.Supported) return Unsupported(source, applied.UnsupportedReason!);
                source = applied.State!;
                outcome = applied.Outcome;
                if (applied.PendingCallbacks.Count > 0)
                {
                    TrainCombatResult callbacks = TrainCombatModel.ApplyCharacterQueue(source,
                        applied.PendingCallbacks.ToList());
                    if (!callbacks.Supported) return Unsupported(source, callbacks.UnsupportedReason!);
                    source = callbacks.State!;
                    if (callbacks.Outcome != RoomOutcome.Exchanged) outcome = callbacks.Outcome;
                }
                copied++;
                if (outcome != RoomOutcome.Exchanged) break;
            }
            return new RelicMonsterSpellTargetingResult(source, outcome, copied);
        }

        private static RelicMonsterSpellTargetingResult Unsupported(TrainCombatState source, string error) =>
            new RelicMonsterSpellTargetingResult(source, unsupportedReason: error);
    }
}
