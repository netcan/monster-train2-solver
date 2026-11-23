using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicUnitSpawnCountRule
    {
        public string Team { get; }
        public string Target { get; }

        public RelicUnitSpawnCountRule(string team, string target)
        { Team = team; Target = target; }
    }

    public static class RelicUnitSpawnCountModel
    {
        public const string DataId = "559f443d-bbeb-40fc-aa86-772ef4c2c2dc";
        public const string AssetKey = "DoubleUnits";
        public const string Effect = "RelicEffectExtraSpawnsFirstUnitPerTurn";

        internal static string? CheckOtherSummon(CombatContext? source)
        {
            if (source?.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) != true) return null;
            RelicResourceResult count = Evaluate(source, CombatTeam.Player, preview: true);
            return count.UnsupportedReason ?? (count.Value > 0 ? "Additional units or status stacks in this summon path require native calibration." : null);
        }

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.IsCovenant != false ||
                owner.DisallowedInPlacementPhase != false || !owner.EffectTypes.SequenceEqual(new[] { Effect }) ||
                owner.ResourceEffects?.Count != 1 || effect.EffectIndex != 0 || effect.Type != Effect ||
                effect.ParamInt != 1 || effect.Amount != 1 || effect.ParamInt2 != 0 || effect.ParamMinInt != 0 ||
                effect.ParamFloat != 0 || !effect.CanApplyInPreviewMode || effect.RequiredMutatorId != null ||
                effect.UnitSpawnCount is not { Team: "Monsters", Target: "FrontInRoom" } || effect.Conditions.Count != 1 ||
                effect.Conditions[0] is not { TrackTriggerCount: true, Comparator: 6, Value: 1, AllowMultiple: false } condition ||
                condition.Query is not { Type: "SubtypeInDeck", Duration: "ThisTurn", SubtypeIsNone: true,
                    TypeMatchDataIds: null, SubtypeMatchDataIds: null, Subtype: null, SpecificCardDataId: null,
                    SourceRawCost: null, VariableCost: false, TargetTeam: "None", CurrentCostCardModifiersOnly: false,
                    CurrentCostSourceIsSpell: false, StatusIds: null })
                return "Missing or changed original DoubleUnits spawn-count projection.";
            return null;
        }

        // The caller owns the ordered birth operation. The count query updates
        // its context before any subsequent spawn notification or birth callback.
        public static RelicResourceResult Evaluate(CombatContext source, CombatTeam team, bool preview = false)
        {
            if (source.Relics == null) return new RelicResourceResult(source, 0);
            CombatContext context = source;
            int extra = 0;
            for (int ownerIndex = 0; ownerIndex < source.Relics.Count; ownerIndex++)
            {
                CombatRelicState owner = context.Relics![ownerIndex];
                if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicResourceEffect? effect = owner.ResourceEffects?.Count == 1 ? owner.ResourceEffects[0] : null;
                string? error = effect == null ? "Missing original DoubleUnits spawn-count projection." : Validate(owner, effect);
                if (error != null) return new RelicResourceResult(null, 0, error);
                if (team != CombatTeam.Player) continue;

                RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, effect!.Conditions);
                if (!evaluation.Supported) return new RelicResourceResult(null, 0, evaluation.UnsupportedReason);
                context = evaluation.Context;
                if (!evaluation.Passed) continue;
                extra += effect.Amount;
                if (preview) continue;

                RelicConditionRecord record = RelicConditionModel.Record(context, effect.Conditions);
                if (!record.Supported) return new RelicResourceResult(null, 0, record.UnsupportedReason);
                context = RelicResourceModel.ReplaceConditions(record.Context, ownerIndex, 0, record.Conditions);
            }
            return new RelicResourceResult(context, extra);
        }
    }
}
