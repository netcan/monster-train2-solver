using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicPyreResurrectionModel
    {
        internal const string DataId = "d7264164-b519-43e2-bcf3-055afee3d8dd";
        internal const string AssetKey = "BoneDogTitanPyreLife";
        internal const string Effect = "RelicEffectPyreHeartResurrection";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule)
        {
            RelicConditionState? condition = rule.Conditions.Count == 1 ? rule.Conditions[0] : null;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
                rule.EffectIndex == 0 && rule.ParamInt == 25 && rule.ParamInt2 == 0 && rule.Amount == 0 &&
                rule.ParamFloat == .25f && !rule.CanApplyInPreviewMode && rule.RequiredMutatorId == null &&
                condition?.Query.Type == "PyreHeartResurrection" && condition.Query.Duration == "ThisTurn" &&
                !condition.TrackTriggerCount && condition.Comparator == 0 && condition.Value == 1 && !condition.AllowMultiple
                ? null : "Missing or changed original Pyre resurrection definition.";
        }
        // RelicManager calls IPyreDeathResurrect directly, bypassing normal condition evaluation.
        // MarkResurrected precedes notification; OnTrigger observes the consumed flag.
        internal static CombatContext TryApply(CombatContext source, out float percentage, out string? error)
        {
            percentage = 0; error = null;
            for (int index = 0; index < (source.Relics?.Count ?? 0); index++)
            {
                CombatRelicState owner = source.Relics![index];
                if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicResourceEffect? rule = owner.ResourceEffects?.SingleOrDefault(item => item.Type == Effect);
                error = rule == null ? "Missing Pyre resurrection resource payload." : Validate(owner, rule);
                if (error != null) return source;
                int? consumed = source.QueryFrame?.PyreResurrectionCount;
                if (consumed != 0 && consumed != 1) { error = "Missing native Pyre resurrection consumed flag."; return source; }
                if (consumed == 1) continue;
                CombatContext context = source.WithQueryFrame(source.QueryFrame!.With(pyreResurrectionCount: 1));
                RelicConditionRecord recorded = RelicConditionModel.Record(context, rule!.Conditions);
                if (!recorded.Supported) { error = recorded.UnsupportedReason; return source; }
                CombatRelicState[] owners = recorded.Context.Relics!.ToArray();
                owners[index] = owner.WithResourceEffects(owner.ResourceEffects!.Select(item => item.EffectIndex == rule.EffectIndex
                    ? rule.WithConditions(recorded.Conditions) : item).ToArray());
                percentage = rule.ParamFloat;
                return recorded.Context.WithRelics(owners);
            }
            return source;
        }
        internal static CombatContext CarryPreviewConditions(CombatContext source, CombatContext observed)
        {
            if (source.Relics?.Any(owner => owner.EffectTypes.Contains(Effect)) != true) return source;
            return source.WithRelics(source.Relics.Select(owner => owner.EffectTypes.Contains(Effect)
                ? owner.WithResourceEffects(observed.Relics!.Single(other => other.DataId == owner.DataId).ResourceEffects!)
                : owner).ToArray());
        }
        internal static PlayCardAction? ChoosePlay(BattleTurnState source)
        {
            if (source.Spawn.Turn != 0 || source.Spawn.Train.Rooms.Any(room => room.Units.Any(unit => unit.SpawnerCardId > 0))) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.RoomIndex == 0);
        }
    }
}
