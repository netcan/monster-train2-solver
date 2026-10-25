using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicPyreCostPlan
    {
        internal TrainCombatState State { get; }
        internal bool CanPay { get; }
        internal bool PaidWithPyre { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> Callbacks { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicPyreCostPlan(TrainCombatState state, bool canPay, bool paidWithPyre = false,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? callbacks = null, string? unsupportedReason = null)
        {
            State = state; CanPay = canPay; PaidWithPyre = paidWithPyre;
            Callbacks = Array.AsReadOnly((callbacks ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToArray());
            UnsupportedReason = unsupportedReason;
        }
    }

    internal static class RelicPayCardCostWithPyreHealthModel
    {
        internal const string Effect = RelicResourceModel.PayCardCostWithPyreHealth;
        internal const string DataId = "7f4c8a90-2518-4a41-8c8c-0f5ca0b0a475";
        internal const string AssetKey = "PyreHealthForEmberCardCost";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule)
        {
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.EffectTypes.Count != 1 ||
                !owner.EffectTypes.SequenceEqual(new[] { Effect }) || owner.DisallowedInPlacementPhase != false ||
                owner.ResourceEffects?.Count != 1 || rule.EffectIndex != 0 || rule.Type != Effect ||
                rule.ParamInt <= 0 || rule.Amount != rule.ParamInt || rule.ParamInt2 != 0 || rule.ParamMinInt != 0 ||
                rule.RequiredMutatorId != null || rule.CanApplyInPreviewMode || rule.Conditions.Count != 0 ||
                rule.DrawValue.HasValue || rule.DrawMutationOrder.HasValue || rule.CapacityValue.HasValue ||
                rule.CapacityMutationOrder.HasValue || rule.ParamFloat != 0 || rule.CardGeneration != null ||
                rule.UnitTrigger != null || rule.OpeningDraw != null || rule.PileStartTurn != null ||
                rule.MoonPhaseStatus != null || rule.StartTurnHeal != null)
                return "Missing or changed native PayCardCostWithPyreHealth payload.";
            return null;
        }

        internal static RelicPyreCostPlan Plan(TrainCombatState source, int cardCost, int energy,
            string costType, int turn, bool preview)
        {
            CombatContext? context = source.Context;
            if (context == null) return Fail(source, "Pyre-health card payment requires battle context.");
            if (costType == "ConsumeRemainingEnergy" || energy >= cardCost)
                return new RelicPyreCostPlan(source, true);

            RelicResourceEffect[] rules = (context.Relics ?? Array.Empty<CombatRelicState>())
                .SelectMany(owner => owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).ToArray();
            if (rules.Length == 0 || preview) return new RelicPyreCostPlan(source, false);
            if (turn <= 0) return Fail(source, "Deployment-phase Pyre payment callbacks are not captured.");
            if (cardCost <= 0 || energy < 0) return Fail(source, "Invalid unaffordable card cost or energy for Pyre payment.");

            long totalCost = 0;
            var callbackCosts = new List<int>(rules.Length);
            foreach (RelicResourceEffect rule in rules)
            {
                long effectCost = (long)rule.ParamInt * cardCost;
                if (effectCost <= 0 || effectCost > int.MaxValue || totalCost + effectCost > int.MaxValue)
                    return Fail(source, "Native Pyre payment exceeds the supported health-cost range.");
                callbackCosts.Add((int)effectCost);
                totalCost += effectCost;
            }

            var pyres = source.Rooms.SelectMany(room => room.Units
                .Where(unit => unit.IsPyre).Select(unit => (Room: room, Unit: unit))).ToArray();
            if (pyres.Length != 1) return Fail(source, "Pyre payment requires exactly one captured Pyre Heart.");
            (RoomCombatState pyreRoom, CombatUnit pyre) = pyres[0];
            if (pyre.Health <= totalCost || pyre.MaxHealth < pyre.Health)
                return Fail(source, "Lethal or invalid Pyre payments require native resurrection and terminal-state coverage.");

            CombatUnit paidPyre = pyre.WithHealth(pyre.Health - (int)totalCost);
            // Later room effects resume the enchantment world. Preserve the
            // payment there as well as on the live Pyre before queuing OnHit.
            TrainCombatState paidState = UnitCloneModel.ReplaceUnit(source, paidPyre);
            var callbacks = callbackCosts.Select(amount => new RoomCombatModel.QueuedCharacterTrigger(
                pyreRoom.RoomIndex, paidPyre, "OnHit", paramInt: amount, paramInt2: 0)).ToArray();
            return new RelicPyreCostPlan(paidState, true, true, callbacks);
        }

        internal static TrainCombatResult DrainCallbacks(TrainCombatState source, RelicPyreCostPlan plan) =>
            plan.Callbacks.Count == 0
                ? new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>())
                : TrainCombatModel.ApplyCharacterQueue(source, plan.Callbacks.ToList());

        private static RelicPyreCostPlan Fail(TrainCombatState source, string reason) =>
            new(source, false, unsupportedReason: reason);
    }
}
