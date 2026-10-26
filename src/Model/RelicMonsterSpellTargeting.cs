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

            // Native relic conditions gate both the private pre-play copy and
            // its later recasts. Once spent, this relic cannot reject an
            // otherwise supported spell merely because its copy is unmodeled.
            RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, modifier.Conditions);
            if (!evaluation.Supported) return Unsupported(source, evaluation.UnsupportedReason!);
            context = evaluation.Context;
            source = CardSpellModel.WithContext(source, context);
            if (!evaluation.Passed) return new RelicMonsterSpellTargetingResult(source);

            bool hasPositionChange = playedRule.Effects.Any(effect => effect.Type is "Bump" or "FloorRearrange" or "AbilityTeleport");
            if (hasPositionChange && !playedRule.Effects.Any(effect => effect.Type is not ("Bump" or "FloorRearrange" or "AbilityTeleport")))
                return new RelicMonsterSpellTargetingResult(source);
            bool justCause = playedRule.DataId == "c8b39244-83c1-4eb3-b474-8898226d4280" && playedRule.AssetKey == "JustCause" &&
                playedRule.OnCastEffects.Count == 0 && playedRule.Effects.Count == 2 &&
                playedRule.Effects[0] is { Type: "FloorRearrange", Target: "DropTargetCharacter", Value: 0, AllowEnemy: true, AllowPlayer: true, Range: null } &&
                playedRule.Effects[1] is { Type: "AddStatus", Target: "LastTargetedCharacters", AllowEnemy: true, AllowPlayer: true, Range: null } valor &&
                valor.Statuses.Count == 1 && valor.Statuses[0].Id == "valor" && valor.Statuses[0].Stacks == 2;
            bool liquidCourage = playedRule.DataId == "a6dfc0d2-9bfe-499a-b521-0a2c4cefc1a2" && playedRule.AssetKey == "LiquidCourage" &&
                playedRule.OnCastEffects.Count == 0 && playedRule.Effects.Count == 2 &&
                playedRule.Effects[0] is { Type: "AddStatus", Target: "DropTargetCharacter", AllowEnemy: true, AllowPlayer: true, Range: null } shield &&
                shield.Statuses.Count == 1 && shield.Statuses[0].Id == "damage shield" && shield.Statuses[0].Stacks == 1 &&
                playedRule.Effects[1] is { Type: "AddStatus", Target: "LastTargetedCharacters", AllowEnemy: true, AllowPlayer: true, Range: null } courage &&
                courage.Statuses.Count == 1 && courage.Statuses[0].Id == "valor" && courage.Statuses[0].Stacks == 4;
            if (!justCause && !liquidCourage && (hasPositionChange || playedRule.Effects.Count != 1 || playedRule.OnCastEffects.Count != 0 ||
                playedRule.Effects[0].Type != "Damage" || playedRule.Effects[0].Target != "DropTargetCharacter" ||
                playedRule.Effects[0].AllowEnemy || !playedRule.Effects[0].AllowPlayer || playedRule.Effects[0].Range != null))
                return Unsupported(source, "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.");

            // GenerateCardStateCopy replaces position-changing effects with a
            // Monster-targeted CardEffectNULL, retaining LastTargetedCharacters
            // for the following Valor effect without rearranging teammates.
            IReadOnlyList<CardActionEffect> copiedEffects = justCause ?
                [new CardActionEffect("Null", "DropTargetCharacter", 0, false, true, []), playedRule.Effects[1]] : playedRule.Effects;

            CardInstanceState? card = context.FindCard(playedCardId);
            bool corruptJustCause = justCause && card != null && card.Permanent.Upgrades.Count is 1 or 2 &&
                card.Permanent.Upgrades.Count(upgrade => upgrade.DataId == "83a6c9ec-0e40-48e6-8a4c-695a68b7e990") == 1 &&
                card.Permanent.Upgrades.All(upgrade =>
                    (upgrade.DataId == "83a6c9ec-0e40-48e6-8a4c-695a68b7e990" && upgrade.AssetKey == "TraitCorrupt" ||
                     upgrade.DataId == "f43e7cdb-fa8d-4760-88bf-c6f34f72d918" && upgrade.AssetKey == "TraitRetain") &&
                    new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                        .All(stat => upgrade.Stats.Value(stat) == 0) &&
                    upgrade is { Statuses.Count: 0, UnhealedHealth: 0, DamageBuff: 0, AbilityUpgrade: null, ExternalInteractions.Count: 0 } &&
                    upgrade.TriggerUpgrades?.Count is not > 0);
            if (card == null || card.MaskDescriptor == null || !corruptJustCause && card.Permanent.Upgrades.Count != 0 || card.Temporary.Upgrades.Count != 0)
                return Unsupported(source, "Monster spell replication requires an owned, unupgraded card with captured mask metadata.");
            try
            {
                CardTraitRefreshState traits = CardBranchMaskModel.OwnedState(card).Traits;
                var combined = CardTraitRefreshModel.Ensure(traits).Composition.CombinedTraits;
                // Corrupt only has OnPreCardPlayed. Native recasts run OnCast
                // and OnCardDiscarded, without calling that pre-play callback.
                // Retain and Consume ignore OnCardDiscarded when isRecastCopy.
                bool recastConsume = liquidCourage && combined?.Count == 1 && combined[0].RuntimeType == "CardTraitExhaustState";
                if (combined?.Count > 0 && !recastConsume && !(corruptJustCause && combined.Count == card!.Permanent.Upgrades.Count &&
                    combined.Count(trait => trait.RuntimeType == "CardTraitCorruptState") == 1 &&
                    combined.Count(trait => trait.RuntimeType == "CardTraitRetain") == combined.Count - 1))
                    return Unsupported(source, "Monster spell replication does not yet model copied card trait callbacks.");
            }
            catch (InvalidOperationException error) { return Unsupported(source, error.Message); }

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
                SpellCastCheck test = CardSpellModel.TestPlay(source, roomIndex, copiedEffects, targetId,
                    definitions, sourceCardId: playedCardId);
                if (!test.Supported) return Unsupported(source, test.UnsupportedReason!);
                if (!test.CanPlay) continue;
                source = CardSpellModel.WithContext(source, source.Context!.WithBattleRng(test.BattleRngAfterTests!.Value));
                TrainSpellResult applied = CardSpellModel.ApplyForCardPlay(source, roomIndex, copiedEffects,
                    targetId, justCause || liquidCourage ? 0 : playedCardId, definitions, source.Context!.OtherPiles, priorTargets: [targetId],
                    resolveLiveEffects: !justCause && !liquidCourage);
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
