using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Raw save/balance/definition inputs. Live energy, HP and pending draws stay
    // in their existing runtime ledgers; these values never reapply acquisitions.
    public sealed class BattleResourceState
    {
        public int BaseEnergy { get; }
        public int DeploymentExtraEnergy { get; }
        public int BaseDraw { get; }
        public int MaxEnergy { get; }
        public int MaxHandSize { get; }
        public string PyreDataId { get; }
        public int PyreBaseAttack { get; }
        public int BasePyreMaxHealth { get; }
        public int PyrePersistentDamage { get; }
        public int PyreModifiedBaseAttack { get; }
        public IReadOnlyList<int> UpgradeAttack { get; }
        public IReadOnlyList<int> UpgradeHealth { get; }
        public IReadOnlyList<string> MutatorIds { get; }
        public float GoldRewardMultiplier { get; }
        public int? BaseFloorCapacity { get; }
        public int? DragonsHoardCap { get; }
        public bool? TowerHealingDisabled { get; }
        public BattleResourceState(int baseEnergy, int deploymentExtraEnergy, int baseDraw, int maxEnergy, int maxHandSize,
            string pyreDataId, int pyreBaseAttack, int basePyreMaxHealth, int pyrePersistentDamage, int pyreModifiedBaseAttack,
            IReadOnlyList<int> upgradeAttack, IReadOnlyList<int> upgradeHealth, IReadOnlyList<string> mutatorIds,
            float goldRewardMultiplier = 1f, int? baseFloorCapacity = null, int? dragonsHoardCap = null,
            bool? towerHealingDisabled = null)
        {
            BaseEnergy = baseEnergy; DeploymentExtraEnergy = deploymentExtraEnergy; BaseDraw = baseDraw;
            MaxEnergy = maxEnergy; MaxHandSize = maxHandSize; PyreDataId = pyreDataId; PyreBaseAttack = pyreBaseAttack;
            BasePyreMaxHealth = basePyreMaxHealth; PyrePersistentDamage = pyrePersistentDamage;
            PyreModifiedBaseAttack = pyreModifiedBaseAttack;
            GoldRewardMultiplier = goldRewardMultiplier;
            BaseFloorCapacity = baseFloorCapacity;
            DragonsHoardCap = dragonsHoardCap;
            TowerHealingDisabled = towerHealingDisabled;
            UpgradeAttack = Array.AsReadOnly(upgradeAttack.ToArray()); UpgradeHealth = Array.AsReadOnly(upgradeHealth.ToArray());
            MutatorIds = Array.AsReadOnly(mutatorIds.ToArray());
        }
    }

    public sealed class RelicResourceEffect
    {
        public int EffectIndex { get; }
        public string Type { get; }
        public int ParamInt { get; }
        public int ParamInt2 { get; }
        public int Amount { get; }
        public bool CanApplyInPreviewMode { get; }
        public string? RequiredMutatorId { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public float? DrawValue { get; }
        public int? DrawMutationOrder { get; }
        public float? CapacityValue { get; }
        public int? CapacityMutationOrder { get; }
        public RelicResourceEffect(int effectIndex, string type, int paramInt, int paramInt2, int amount,
            bool canApplyInPreviewMode, string? requiredMutatorId = null, IReadOnlyList<RelicConditionState>? conditions = null,
            float? drawValue = null, int? drawMutationOrder = null,
            float? capacityValue = null, int? capacityMutationOrder = null)
        {
            EffectIndex = effectIndex; Type = type; ParamInt = paramInt; ParamInt2 = paramInt2; Amount = amount;
            CanApplyInPreviewMode = canApplyInPreviewMode; RequiredMutatorId = requiredMutatorId;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            DrawValue = drawValue; DrawMutationOrder = drawMutationOrder;
            CapacityValue = capacityValue; CapacityMutationOrder = capacityMutationOrder;
        }
        internal RelicResourceEffect WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicResourceEffect(EffectIndex, Type, ParamInt, ParamInt2, Amount, CanApplyInPreviewMode,
                RequiredMutatorId, conditions, DrawValue, DrawMutationOrder, CapacityValue, CapacityMutationOrder);
    }

    public sealed class RelicResourceEvent
    {
        public string RelicId { get; }
        public int EffectIndex { get; }
        public int Before { get; }
        public int After { get; }
        public bool Notified { get; }
        internal RelicResourceEvent(string relicId, int effectIndex, int before, int after, bool notified)
        { RelicId = relicId; EffectIndex = effectIndex; Before = before; After = after; Notified = notified; }
    }

    public sealed class RelicResourceResult
    {
        public CombatContext? Context { get; }
        public int Value { get; }
        public IReadOnlyList<RelicResourceEvent> Events { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal RelicResourceResult(CombatContext? context, int value, string? error = null,
            IReadOnlyList<RelicResourceEvent>? events = null)
        { Context = context; Value = value; UnsupportedReason = error; Events = Array.AsReadOnly((events ?? Array.Empty<RelicResourceEvent>()).ToArray()); }
    }

    public sealed class RelicPyreResourceResult
    {
        public CombatContext? Context { get; }
        public int Attack { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal RelicPyreResourceResult(CombatContext? context, int attack, int health, int maxHealth, string? error = null)
        { Context = context; Attack = attack; Health = health; MaxHealth = maxHealth; UnsupportedReason = error; }
    }

    public static partial class RelicResourceModel
    {
        public const string Energy = "RelicEffectModifyEnergy";
        public const string DeploymentEnergy = "RelicEffectModifyEnergyDuringDeployment";
        public const string Draw = "RelicEffectChangeStartingHandSize";
        public const string PyreAttack = "RelicEffectIncreasePyreAttack";
        public const string PyreHealth = "RelicEffectIncreasePyreHealth";
        public const string FloorCapacity = "RelicEffectChangeFloorCapacity";
        public const string MoonPhaseEnergy = "RelicEffectModifyEnergyOnMoonPhase";
        public static bool IsEffect(string type) => type == Energy || type == DeploymentEnergy || type == Draw ||
            type == PyreAttack || type == PyreHealth || type == FloorCapacity || type == MoonPhaseEnergy;

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index }).Where(item => IsEffect(item.type))
                .Select(item => item.index).ToArray();
            var rules = relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>();
            if (!indices.SequenceEqual(rules.Select(rule => rule.EffectIndex)))
                return "Missing or unordered relic resource definitions on " + relic.AssetKey + ".";
            foreach (RelicResourceEffect rule in rules)
            {
                if (rule.Type != relic.EffectTypes[rule.EffectIndex]) return "Relic resource runtime type differs from its effect index.";
                if (rule.RequiredMutatorId == "") return "Empty required resource mutator identity.";
                if ((rule.Type == Energy || rule.Type == DeploymentEnergy) && rule.Amount != rule.ParamInt)
                    return "Relic energy initialized amount differs from its original parameter.";
                if (rule.Type == MoonPhaseEnergy && (rule.Amount != rule.ParamInt || rule.ParamInt2 <= 0 || (rule.ParamInt2 & ~7) != 0))
                    return "Moon-phase energy amount or phase mask differs from its original parameters.";
                if (rule.Type == Draw)
                {
                    if (!rule.DrawValue.HasValue || rule.DrawValue.Value != (float)rule.ParamInt ||
                        !TryRound(rule.DrawValue.Value, out int rounded) || rounded != rule.Amount || rule.DrawMutationOrder < 0)
                        return "Missing or inconsistent native float draw mutation metadata.";
                }
                else if (rule.DrawValue.HasValue || rule.DrawMutationOrder.HasValue)
                    return "Unexpected native draw metadata on a different resource effect.";
                if (rule.Type == FloorCapacity)
                {
                    if (!rule.CapacityValue.HasValue || rule.CapacityValue.Value != (float)rule.ParamInt ||
                        !TryRound(rule.CapacityValue.Value, out int rounded) || rounded != rule.Amount || rule.CapacityMutationOrder < 0)
                        return "Missing or inconsistent native float floor capacity mutation metadata.";
                }
                else if (rule.CapacityValue.HasValue || rule.CapacityMutationOrder.HasValue)
                    return "Unexpected native floor capacity metadata on a different resource effect.";
                if ((rule.Type == Energy || rule.Type == DeploymentEnergy || rule.Type == Draw || rule.Type == FloorCapacity) && rule.RequiredMutatorId != null)
                    return "Unexpected required mutator on a resource effect without that native gate.";
                if ((rule.Type == Energy || rule.Type == DeploymentEnergy || rule.Type == PyreAttack || rule.Type == PyreHealth ||
                    rule.Type == MoonPhaseEnergy) &&
                    !relic.DisallowedInPlacementPhase.HasValue)
                    return "Missing native resource owner placement restriction.";
            }
            return null;
        }

        internal static string? Validate(CombatContext? context)
        {
            bool required = context?.Relics?.Any(relic => relic.EffectTypes.Any(IsEffect)) == true;
            if (context?.Resources == null) return required ? "Missing raw native resource inputs." : null;
            BattleResourceState raw = context.Resources;
            if (context.Relics == null || raw.BaseEnergy < 0 || raw.BaseDraw < 0 || raw.MaxEnergy < 0 || raw.MaxHandSize < 1 ||
                raw.BasePyreMaxHealth < 1 || string.IsNullOrEmpty(raw.PyreDataId) || raw.MutatorIds.Any(string.IsNullOrEmpty) ||
                raw.MutatorIds.Distinct().Count() != raw.MutatorIds.Count || context.MaxHandSize != raw.MaxHandSize ||
                context.EnergyState?.Maximum != raw.MaxEnergy || float.IsNaN(raw.GoldRewardMultiplier) || float.IsInfinity(raw.GoldRewardMultiplier))
                return "Invalid or inconsistent raw native resource inputs.";
            var orders = new List<int>();
            var capacityOrders = new List<int>();
            foreach (CombatRelicState relic in context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return error;
                foreach (RelicResourceEffect rule in relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    if (rule.DrawMutationOrder.HasValue) orders.Add(rule.DrawMutationOrder.Value);
                    if (rule.Type == FloorCapacity)
                    {
                        if (!raw.BaseFloorCapacity.HasValue) return "Missing raw native base floor capacity.";
                        if (rule.CapacityMutationOrder.HasValue) capacityOrders.Add(rule.CapacityMutationOrder.Value);
                    }
                    if (rule.Type == PyreAttack || rule.Type == PyreHealth)
                    {
                        IReadOnlyList<int> table = rule.Type == PyreAttack ? raw.UpgradeAttack : raw.UpgradeHealth;
                        int expected = rule.ParamInt2 >= 0 && rule.ParamInt2 < table.Count ? table[rule.ParamInt2] : 0;
                        if (rule.Amount != expected) return "Initialized Pyre resource amount differs from its original upgrade index/table.";
                    }
                }
            }
            if (orders.Distinct().Count() != orders.Count) return "Duplicate native draw mutation registration order.";
            return capacityOrders.Distinct().Count() != capacityOrders.Count ? "Duplicate native floor capacity mutation registration order." : null;
        }

        private static string? Ready(CombatContext source) => RelicModel.Validate(source) ??
            (source.Resources == null ? "Missing raw native resource inputs." : null);
        private static RelicResourceResult Fail(string error) => new RelicResourceResult(null, 0, error);
        private static RelicPyreResourceResult PyreFail(string error) => new RelicPyreResourceResult(null, 0, 0, 0, error);
        private static bool MutatorMatches(CombatContext context, RelicResourceEffect rule) => rule.RequiredMutatorId == null ||
            context.Resources!.MutatorIds.Contains(rule.RequiredMutatorId);

        // This getter models the display/query contract, not the ordered timing
        // effects. It evaluates conditions without notifying or recording them.
        public static RelicResourceResult GetEnergyModification(CombatContext source, bool deployment = false, bool preview = false)
        {
            string? error = Ready(source);
            if (error != null) return Fail(error);
            CombatContext context = source;
            int total = 0;
            foreach (CombatRelicState relic in source.Relics!)
            foreach (RelicResourceEffect rule in relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (rule.Type != (deployment ? DeploymentEnergy : Energy) || preview && !rule.CanApplyInPreviewMode) continue;
                RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
                context = evaluation.Context;
                if (evaluation.Passed) total = unchecked(total + rule.Amount);
            }
            return new RelicResourceResult(context, deployment ? total : Math.Max(total, 0));
        }

        public static RelicResourceResult GetDrawCount(CombatContext source)
        {
            string? error = Ready(source);
            if (error != null) return Fail(error);
            int value = source.Resources!.BaseDraw;
            // MutableRules tracks initialized object registration independently
            // of current manager order, preview, placement and outer conditions.
            foreach (RelicResourceEffect rule in source.Relics!.SelectMany(relic => relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Draw && rule.DrawMutationOrder.HasValue).OrderBy(rule => rule.DrawMutationOrder))
                value = unchecked(value + rule.Amount);
            return new RelicResourceResult(source, Math.Min(source.Resources.MaxHandSize, Math.Max(1, value)));
        }

        public static RelicResourceResult ApplyEnergyTiming(CombatContext source, bool deployment = false,
            bool preview = false, bool placement = false)
        {
            string? error = Ready(source) ?? EnergyModel.Validate(source);
            if (error != null) return Fail(error);
            CombatContext context = source;
            var events = new List<RelicResourceEvent>();
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                CombatRelicState owner = context.Relics[relicIndex];
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = context.Relics[relicIndex].ResourceEffects![ruleIndex];
                    if (rule.Type != (deployment ? DeploymentEnergy : Energy) || preview && !rule.CanApplyInPreviewMode) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
                    context = evaluation.Context;
                    if (!evaluation.Passed || rule.Amount == 0) continue;
                    // Both native effects use Math.Abs(int) on a negative amount.
                    if (rule.Amount == int.MinValue) return Fail("Native resource energy removal overflows Math.Abs(Int32.MinValue).");
                    int before = context.QueryFrame!.Energy!.Value;
                    int after = rule.Amount > 0 ? Math.Min(context.Resources!.MaxEnergy, unchecked(before + rule.Amount)) :
                        Math.Max(0, unchecked(before - Math.Abs(rule.Amount)));
                    context = context.WithQueryFrame(context.QueryFrame.With(energy: after));
                    bool notify = rule.Type == Energy;
                    if (notify)
                    {
                        RelicConditionRecord recorded = RelicConditionModel.Record(context, rule.Conditions);
                        if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                        context = ReplaceConditions(recorded.Context, relicIndex, ruleIndex, recorded.Conditions);
                    }
                    events.Add(new RelicResourceEvent(owner.DataId, rule.EffectIndex, before, after, notify));
                }
            }
            return new RelicResourceResult(context, context.QueryFrame!.Energy!.Value, events: events);
        }

        public static RelicResourceResult ApplyMoonPhaseEnergy(CombatContext source, string moonPhase,
            bool preview = false, bool placement = false)
        {
            string? error = Ready(source) ?? EnergyModel.Validate(source);
            if (error != null) return Fail(error);
            int phaseMask = moonPhase switch { "New" => 1, "Full" => 2, _ => 0 };
            if (phaseMask == 0) return Fail("Unmodeled moon phase energy transition.");
            CombatContext context = source;
            var events = new List<RelicResourceEvent>();
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                CombatRelicState owner = context.Relics[relicIndex];
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = context.Relics[relicIndex].ResourceEffects![ruleIndex];
                    if (rule.Type != MoonPhaseEnergy || preview && !rule.CanApplyInPreviewMode) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
                    context = evaluation.Context;
                    if (!evaluation.Passed || (rule.ParamInt2 & phaseMask) == 0) continue;
                    if (rule.Amount == int.MinValue)
                        return Fail("Native moon-phase energy removal overflows Int32 minimum.");
                    int before = context.QueryFrame!.Energy!.Value;
                    int after = rule.Amount > 0 ? Math.Min(context.Resources!.MaxEnergy, unchecked(before + rule.Amount)) :
                        Math.Max(0, unchecked(before - Math.Abs(rule.Amount)));
                    context = context.WithQueryFrame(context.QueryFrame.With(energy: after));
                    RelicConditionRecord recorded = RelicConditionModel.Record(context, rule.Conditions);
                    if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                    context = ReplaceConditions(recorded.Context, relicIndex, ruleIndex, recorded.Conditions);
                    events.Add(new RelicResourceEvent(owner.DataId, rule.EffectIndex, before, after, true));
                }
            }
            return new RelicResourceResult(context, context.QueryFrame!.Energy!.Value, events: events);
        }

        public static RelicResourceResult StartTurn(CombatContext source, bool deployment = false, bool preview = false)
        {
            string? error = Ready(source) ?? EnergyModel.Validate(source);
            if (error != null) return Fail(error);
            CombatContext context = EnergyModel.SetPhase(source, "MonsterTurn");
            int income = unchecked(context.Resources!.BaseEnergy + context.EnergyState!.NextTurn + context.EnergyState.EveryTurn +
                (deployment ? context.Resources.DeploymentExtraEnergy : 0));
            if (deployment || income > 0) context = context.WithQueryFrame(context.QueryFrame!.With(
                energy: Math.Min(context.Resources.MaxEnergy, unchecked(context.QueryFrame.Energy!.Value + income))));
            var events = new List<RelicResourceEvent>();
            if (deployment)
            {
                RelicResourceResult deployed = ApplyEnergyTiming(context, deployment: true, preview: preview, placement: true);
                if (!deployed.Supported) return deployed;
                context = deployed.Context!; events.AddRange(deployed.Events);
            }
            context = context.WithEnergyState(context.EnergyState!.With(nextTurn: 0));
            RelicResourceResult timed = ApplyEnergyTiming(context, preview: preview, placement: deployment);
            if (!timed.Supported) return timed;
            events.AddRange(timed.Events);
            return new RelicResourceResult(timed.Context, timed.Value, events: events);
        }

        public static RelicResourceResult GetPyreAttack(CombatContext source, bool placement = false, bool notify = false)
        {
            string? error = Ready(source);
            if (error != null) return Fail(error);
            CombatContext context = source;
            int attack = unchecked(source.Resources!.PyreBaseAttack + source.Resources.PyrePersistentDamage);
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                CombatRelicState owner = context.Relics[relicIndex];
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = context.Relics[relicIndex].ResourceEffects![ruleIndex];
                    if (rule.Type != PyreAttack) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
                    context = evaluation.Context;
                    if (!evaluation.Passed) continue;
                    int before = attack;
                    if (!MutatorMatches(context, rule)) { attack = 0; continue; }
                    attack = Math.Max(0, unchecked(attack + rule.Amount));
                    if (notify && before != attack)
                    {
                        RelicConditionRecord recorded = RelicConditionModel.Record(context, rule.Conditions);
                        if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                        context = ReplaceConditions(recorded.Context, relicIndex, ruleIndex, recorded.Conditions);
                    }
                }
            }
            return new RelicResourceResult(context, attack);
        }

        public static RelicResourceResult GetPyreMaxHealth(CombatContext source)
        {
            string? error = Ready(source);
            if (error != null) return Fail(error);
            int max = source.Resources!.BasePyreMaxHealth;
            // SaveManager enumerates all installed health effects with no outer
            // condition, preview or placement filtering.
            foreach (RelicResourceEffect rule in source.Relics!.SelectMany(relic => relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>()))
                if (rule.Type == PyreHealth && MutatorMatches(source, rule)) max = unchecked(max + rule.Amount);
            return max < 0 ? Fail("Native negative Pyre maximum health is not a supported live initialization.") : new RelicResourceResult(source, max);
        }

        public static RelicPyreResourceResult InitializePyre(CombatContext source, int currentHp, bool placement = false)
        {
            RelicResourceResult max = GetPyreMaxHealth(source);
            if (!max.Supported) return PyreFail(max.UnsupportedReason!);
            RelicResourceResult attack = GetPyreAttack(max.Context!, placement);
            return attack.Supported ? new RelicPyreResourceResult(attack.Context, attack.Value,
                Math.Min(max.Value, Math.Max(0, currentHp)), max.Value) : PyreFail(attack.UnsupportedReason!);
        }

        // Call only for a recorded acquisition, after the owner is installed.
        // StartTurn deliberately never invokes this API.
        public static RelicPyreResourceResult Acquire(CombatContext source, int relicIndex, int currentHp, bool placement = false)
        {
            RelicResourceResult max = GetPyreMaxHealth(source);
            if (!max.Supported) return PyreFail(max.UnsupportedReason!);
            if (relicIndex < 0 || relicIndex >= source.Relics!.Count) return PyreFail("Missing acquired resource owner.");
            CombatContext context = source;
            int health = Math.Min(max.Value, Math.Max(0, currentHp));
            CombatRelicState owner = source.Relics[relicIndex];
            if (!placement || owner.DisallowedInPlacementPhase != true)
                foreach (RelicResourceEffect rule in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    if (rule.Type != PyreHealth) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return PyreFail(evaluation.UnsupportedReason!);
                    context = evaluation.Context;
                    if (evaluation.Passed && MutatorMatches(context, rule) && rule.Amount > 0)
                        health = Math.Min(max.Value, Math.Max(0, unchecked(health + rule.Amount)));
                }
            return new RelicPyreResourceResult(context, 0, health, max.Value);
        }

        private static CombatContext ReplaceConditions(CombatContext context, int relicIndex, int ruleIndex,
            IReadOnlyList<RelicConditionState> conditions)
        {
            CombatRelicState owner = context.Relics![relicIndex];
            var rules = owner.ResourceEffects!.ToArray(); rules[ruleIndex] = rules[ruleIndex].WithConditions(conditions);
            var relics = context.Relics.ToArray(); relics[relicIndex] = owner.WithResourceEffects(rules);
            return context.WithRelics(relics);
        }
        private static bool TryRound(float value, out int rounded)
        {
            double result = Math.Round(value, MidpointRounding.ToEven);
            if (double.IsNaN(result) || result < int.MinValue || result > int.MaxValue) { rounded = 0; return false; }
            rounded = (int)result; return true;
        }
    }
}
