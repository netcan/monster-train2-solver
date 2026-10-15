using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static partial class RelicResourceModel
    {
        public const string MaxCorruptionStart = "RelicEffectModifyMaxCorruption";
        public const string PermanentCorruptionStart = "RelicEffectAddPermanentCorruptionStartOfCombat";
        public const string CorruptionPostOpeningDraw = "RelicEffectModifyCorruptionStartOfCombat";
        public static bool IsOpeningCorruption(string type) => type == MaxCorruptionStart || type == PermanentCorruptionStart || type == CorruptionPostOpeningDraw;

        public static TrainCombatResult ApplyOpeningCorruption(TrainCombatState source, bool postDraw = false,
            bool placement = false, bool preview = false)
        {
            TrainCombatResult Failed(string reason) => new TrainCombatResult(null, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>(), reason);
            if (source.Context == null) return Failed("Opening corruption requires a captured context.");
            string? error = Ready(source.Context);
            if (error != null) return Failed(error);
            TrainCombatState train = source;
            for (int relicIndex = 0; relicIndex < train.Context!.Relics!.Count; relicIndex++)
            {
                CombatRelicState owner = train.Context.Relics[relicIndex];
                if (owner.DisallowedInPlacementPhase == placement) continue;
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = train.Context!.Relics![relicIndex].ResourceEffects![ruleIndex];
                    if (!(IsOpeningCorruption(rule.Type) || rule.Type == StartCombatEnergy) || (rule.Type == CorruptionPostOpeningDraw) != postDraw ||
                        preview && !rule.CanApplyInPreviewMode) continue;
                    var evaluation = RelicConditionModel.Evaluate(train.Context, rule.Conditions);
                    if (!evaluation.Supported) return Failed(evaluation.UnsupportedReason!);
                    train = CardSpellModel.WithContext(train, evaluation.Context);
                    if (!evaluation.Passed || rule.Amount == 0) continue;
                    // The start-battle manager dispatches energy and corruption
                    // together in owner/effect order, before any timing income.
                    if (rule.Type == StartCombatEnergy)
                    {
                        error = EnergyModel.Validate(train.Context);
                        if (error != null) return Failed(error);
                        if (rule.Amount == int.MinValue) return Failed("Native start-combat energy absolute value overflows.");
                        int before = train.Context!.QueryFrame!.Energy!.Value;
                        int after = rule.Amount > 0 ? Math.Min(train.Context.Resources!.MaxEnergy, unchecked(before + rule.Amount)) :
                            Math.Max(0, unchecked(before - Math.Abs(rule.Amount)));
                        train = CardSpellModel.WithContext(train, train.Context.WithQueryFrame(train.Context.QueryFrame.With(energy: after)));
                        continue;
                    }
                    string effectType = rule.Type == MaxCorruptionStart ? "AdjustMaxCorruption" :
                        rule.Type == PermanentCorruptionStart ? "AddPermanentCorruption" : "AdjustCorruption";
                    int[] rooms = rule.Type == PermanentCorruptionStart ? new[] { (int)rule.ParamFloat } :
                        train.Rooms.Select(room => room.RoomIndex).ToArray();
                    foreach (int room in rooms)
                    {
                        // Start-of-battle relics pass signed amounts directly to
                        // RoomState; card negative-all semantics do not apply.
                        if (rule.Type == CorruptionPostOpeningDraw && rule.Amount < 0)
                            return Failed("Signed negative opening corruption needs direct room adjustment semantics.");
                        var applied = RoomCorruptionModel.Apply(train, room,
                            new CardActionEffect(effectType, "Room", rule.Amount, false, false, Array.Empty<CombatStatus>(), corruptionAdd: true));
                        if (!applied.Supported) return Failed(applied.UnsupportedReason!);
                        train = CardSpellModel.WithContext(train, applied.Context!);
                    }
                    if (rule.Type == PermanentCorruptionStart)
                    {
                        var recorded = RelicConditionModel.Record(train.Context!, rule.Conditions);
                        if (!recorded.Supported) return Failed(recorded.UnsupportedReason!);
                        train = CardSpellModel.WithContext(train, ReplaceConditions(recorded.Context, relicIndex, ruleIndex, recorded.Conditions));
                    }
                }
            }
            return new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        }
    }
}
