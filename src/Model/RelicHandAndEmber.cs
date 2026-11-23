using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicHandAndEmberModel
    {
        internal const string HandOwnerId = "2dd0e035-96a9-46f3-8c52-eacbe82e871b";
        internal const string EmberOwnerId = "0421d885-09a3-48ec-ad5e-762127ac336e";
        internal const string ConserveOwnerId = "3e200d1f-7ba9-4a6e-a09d-5625f25b01ac";
        internal const string SuppressDiscardEffect = "RelicEffectSuppressHandDiscard";
        internal const string SuppressBaseEnergyEffect = "RelicEffectConserveEnergyStartOfTurn";
        internal const string StartingRelicEffect = "RelicEffectAddRelicStartOfRun";

        internal static bool OriginalHand(CombatRelicState owner) =>
            owner.DataId == HandOwnerId && owner.AssetKey == "HandDoesntDiscard" &&
            owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes is [RelicResourceModel.Draw, SuppressDiscardEffect] &&
            owner.ResourceEffects is [{ EffectIndex: 0, Type: RelicResourceModel.Draw, ParamInt: -2, Amount: -2, Conditions.Count: 0 }];

        internal static bool OriginalEmber(CombatRelicState owner) =>
            owner.DataId == EmberOwnerId && owner.AssetKey == "EmberAtStartOfTurnOnly" &&
            owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes is [RelicResourceModel.StartCombatEnergy, SuppressBaseEnergyEffect, StartingRelicEffect] &&
            owner.ResourceEffects is [{ EffectIndex: 0, Type: RelicResourceModel.StartCombatEnergy, ParamInt: 20, Amount: 20, Conditions.Count: 0 }];

        internal static bool OriginalConserve(CombatRelicState owner) =>
            owner.DataId == ConserveOwnerId && owner.AssetKey == "ConserveEmber" &&
            owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes is [RelicResourceModel.ConserveEnergyEndOfTurn] &&
            owner.ResourceEffects is [{ EffectIndex: 0, Type: RelicResourceModel.ConserveEnergyEndOfTurn, ParamInt: 0, Amount: 0, Conditions.Count: 0 }];

        internal static bool SupportsEnergyOwner(IEnumerable<CombatRelicState> owners) =>
            owners.Count(OriginalEmber) == 1 && owners.Count(OriginalConserve) == 1;
    }
}
