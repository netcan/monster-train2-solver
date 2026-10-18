using System;
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

        internal static bool IsAfterDraw(string type) => type == Effect ||
            type == RelicCardGenerationModel.AfterDraw || type == RelicCardGenerationModel.PrecachedPlayerClan ||
            type == RelicCardGenerationModel.ChampionCardAfterDraw;

        // These effects share one native timing interface. Preserve owner/effect
        // order so generated cards and extra draws see the same hand capacity.
        internal static RelicResourceResult ApplyAfterDraw(CombatContext source, BattlePlayRules? definitions = null)
        {
            CombatContext context = source;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicResourceEffect effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (!IsAfterDraw(effect.Type)) continue;
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
