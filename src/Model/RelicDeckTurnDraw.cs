using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicDeckTurnDrawModel
    {
        internal const string Effect = "RelicEffectCardDrawPerPileStartOfTurn";
        internal const string DataId = "ef08bc24-6016-4d30-8ba7-c42534e3e1f7";
        internal const string AssetKey = "DrawCardPerDeckSize";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.DataId == DataId && owner.AssetKey == AssetKey && owner.DisallowedInPlacementPhase == true &&
            owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) &&
            rule.EffectIndex == 0 && rule.Type == Effect && rule.ParamInt == 25 && rule.ParamInt2 == 0 &&
            rule.ParamMinInt == 0 && rule.Amount == 25 && !rule.CanApplyInPreviewMode &&
            rule.RequiredMutatorId == null && rule.Conditions.Count == 0
                ? null : "Missing or changed original DrawCardPerDeckSize payload.";

        internal static RelicResourceResult Count(CombatContext source)
        {
            CombatRelicState[] owners = source.Relics?.Where(owner => owner.EffectTypes.Contains(Effect)).ToArray()
                ?? Array.Empty<CombatRelicState>();
            if (owners.Length != 1 || owners[0].ResourceEffects?.Count != 1)
                return Fail("Deck-scaled draw requires one captured original owner and timing effect.");
            string? error = Validate(owners[0], owners[0].ResourceEffects![0]);
            if (error != null) return Fail(error);
            if (source.Resources == null || source.Resources.VisibleDeckCount < 0)
                return Fail("Deck-scaled draw requires the native visible save-deck count.");
            return new RelicResourceResult(source, source.Resources.VisibleDeckCount / 25);
        }

        internal static RelicResourceResult Apply(CombatContext source, BattlePlayRules? definitions = null,
            bool placement = false, bool preview = false)
        {
            RelicResourceResult count = Count(source);
            if (!count.Supported) return count;
            if (placement || preview || count.Value == 0) return new RelicResourceResult(source, 0);
            TypedDrawResult drawn = NextDrawUpgradeModel.DrawCards(source, count.Value, 0, definitions);
            return drawn.Supported ? new RelicResourceResult(drawn.Context, drawn.DrawnCardIds.Count)
                : Fail(drawn.UnsupportedReason!);
        }

        internal static bool IsAfterDraw(string type) => type == Effect || type == RelicOccupiedFloorDrawModel.Effect ||
            type == RelicCardGenerationModel.AfterDraw || type == RelicCardGenerationModel.PrecachedPlayerClan ||
            type == RelicCardGenerationModel.ChampionCardAfterDraw || type == RelicAfterDrawSpawnModel.Effect ||
            type == RelicHoldoverGrantModel.Effect;

        internal static RelicResourceResult ApplyAfterDraw(CombatContext source, BattlePlayRules? definitions = null, TrainCombatState? train = null)
            => ApplyAfterDraw(source, definitions, ref train, null, out _);

        internal static TrainCombatResult ApplyAfterDraw(TrainCombatState source, BattlePlayRules? definitions)
        {
            TrainCombatState? train = source;
            var roomResults = new List<RoomCombatResult>();
            var applied = ApplyAfterDraw(source.Context!, definitions, ref train, roomResults, out RoomOutcome outcome);
            return applied.Supported
                ? new TrainCombatResult(CardSpellModel.WithContext(train!, applied.Context!), outcome, roomResults)
                : new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), applied.UnsupportedReason);
        }

        // These effects share one native timing interface. Preserve owner/effect
        // order and carry both the live hand and train to the next callback.
        private static RelicResourceResult ApplyAfterDraw(CombatContext source, BattlePlayRules? definitions,
            ref TrainCombatState? train, List<RoomCombatResult>? roomResults, out RoomOutcome outcome)
        {
            CombatContext context = source;
            outcome = RoomOutcome.Exchanged;
            for (int ownerIndex = 0; ownerIndex < (source.Relics?.Count ?? 0); ownerIndex++)
            foreach (RelicResourceEffect effect in source.Relics![ownerIndex].ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                CombatRelicState owner = source.Relics![ownerIndex];
                if (!IsAfterDraw(effect.Type)) continue;
                if (effect.Type == RelicHoldoverGrantModel.Effect)
                {
                    var applied = RelicHoldoverGrantModel.AfterDraw(context, owner, effect,
                        train?.Rooms.Any(room => room.Preview) == true);
                    if (!applied.Supported) return applied;
                    context = applied.Context!;
                    continue;
                }
                if (effect.Type == RelicAfterDrawSpawnModel.Effect)
                {
                    if (train == null) return Fail("After-draw unit birth requires the complete train.");
                    var born = RelicAfterDrawSpawnModel.Apply(CardSpellModel.WithContext(train, context), owner, effect, definitions);
                    if (!born.Supported) return Fail(born.UnsupportedReason!);
                    train = born.State!;
                    context = train.Context!;
                    roomResults?.AddRange(born.RoomResults);
                    if (born.Outcome != RoomOutcome.Exchanged)
                    {
                        outcome = born.Outcome;
                        return new RelicResourceResult(context, 0);
                    }
                    continue;
                }
                if (effect.Type == RelicOccupiedFloorDrawModel.Effect)
                {
                    if (train == null) return Fail("Enemy-occupied after-draw refresh requires the complete train.");
                    var refreshed = RelicOccupiedFloorDrawModel.RefreshOne(CardSpellModel.WithContext(train, context),
                        ownerIndex, effect.EffectIndex, "AfterDraw");
                    if (!refreshed.Supported) return refreshed;
                    context = refreshed.Context!;
                    continue;
                }
                if (effect.Type == Effect)
                {
                    RelicResourceResult drawn = Apply(context, definitions);
                    if (!drawn.Supported) return drawn;
                    context = drawn.Context!;
                    continue;
                }
                CardGenerationResult generated = effect.Type == RelicCardGenerationModel.AfterDraw
                    ? RelicCardGenerationModel.ApplyOne(context, owner.DataId, effect.EffectIndex)
                    : effect.Type == RelicCardGenerationModel.PrecachedPlayerClan
                        ? RelicCardGenerationModel.ApplyPrecachedPlayerClanOne(context, owner.DataId, effect.EffectIndex)
                        : RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(context, owner.DataId, effect.EffectIndex);
                if (!generated.Supported) return Fail(generated.UnsupportedReason!);
                context = generated.Context!;
            }
            return new RelicResourceResult(context, 0);
        }

        private static RelicResourceResult Fail(string reason) => new RelicResourceResult(null, 0, reason);
    }
}
