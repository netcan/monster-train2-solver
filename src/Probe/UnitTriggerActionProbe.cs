using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTriggerActionProbe
    {
        private static bool StatusEffect(string type) =>
            type is "CardEffectAddStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or
                "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectPerLastAttackDamage" or "CardEffectAddStatusEffectBasedOnHealth";
        private static bool DamageEffect(string type) =>
            type is "CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits";
        private static bool HealthEffect(string type) => type is "CardEffectAdjustMaxHealth" or "CardEffectBuffMaxHealth";
        private static bool HatchEffect(string type) => type is
            "CardEffectRemoveStatusEffectPerCorrupt" or "CardEffectSacrifice" or "CardEffectForceHatchCheck";
        private static bool StatsFromSelf(string type) => type == "CardEffectAddStatsFromSelf";

        internal static CardActionEffect? Capture(CardEffectState state, List<string>? interactions = null)
        {
            if (state.GetCardEffect() is CardEffectSynergizeStatusEffects) return Synergy(state.GetSourceCardEffectData());
            if (state.GetCardEffect() is CardEffectFeederRules)
                return Feeding(state.GetSourceCardEffectData());
            if (state.GetCardEffect() is CardEffectAdjustDragonsHoardPerStatusEffect)
                return HoardPerStatus(state.GetSourceCardEffectData());
            if (state.GetCardEffect() is CardEffectRemoveEquipment)
                return BattleActionProbe.DescribeTriggerEffect(state.GetSourceCardEffectData(), interactions ?? new List<string>());
            if (state.GetCardEffect() is CardEffectAdjustAllStatusEffects or CardEffectAdjustCorruption or CardEffectPlayUnitTrigger or CardEffectDamagePerHp or CardEffectHealTrain or CardEffectAddTempCardUpgradeToSpellsInDeck or CardEffectDamagePerSourceAttack or CardEffectRecursion or CardEffectAddTempCardUpgradeToNextDrawnCard or CardEffectFloorRearrange)
                return BattleActionProbe.DescribeTriggerEffect(state.GetSourceCardEffectData(), interactions ?? new List<string>());
            if (state.GetCardEffect() is CardEffectDrawType) return TypedDraw(state.GetSourceCardEffectData(), state.GetParamInt());
            if (AbilityLifecycleProbe.Known(state.GetCardEffect().GetType().Name)) return AbilityLifecycleProbe.Capture(state);
            if (AbilityCooldownProbe.Known(state.GetCardEffect().GetType().Name)) return AbilityCooldownProbe.Describe(state.GetSourceCardEffectData(), state.GetParamInt(), state.GetParamBool());
            if (state.GetCardEffect() is CardEffectSpawnMonsterOrAddStatusEffect)
                return SpawnOrStatus(state.GetSourceCardEffectData(), state.GetParamStatusEffectStackData()
                    .Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(), interactions);
            if (!(state.GetCardEffect() is CardEffectNULL) && !(state.GetCardEffect() is CardEffectHeal) && !(state.GetCardEffect() is CardEffectDamage) && !(state.GetCardEffect() is CardEffectAddStatusEffect) &&
                !(state.GetCardEffect() is CardEffectRemoveAllStatusEffects) &&
                !EnergyModel.IsNativeEffect(state.GetCardEffect().GetType().Name) && !(state.GetCardEffect() is CardEffectDrawAdditionalNextTurn) &&
                !(state.GetCardEffect() is CardEffectAdjustRoomCapacity) && !(state.GetCardEffect() is CardEffectAdjustForgePoints) &&
                !(state.GetCardEffect() is CardEffectAdvanceMoonPhase) && !HealthEffect(state.GetCardEffect().GetType().Name) &&
                state.GetCardEffect().GetType().Name != RelicResourceModel.PermanentPyreAttackAdjustment &&
                state.GetCardEffect().GetType().Name != "CardEffectPermanentlyAdjustMaxPyreHealth" &&
                state.GetCardEffect().GetType().Name != TriggeredStatusTransferModel.EffectType &&
                !HatchEffect(state.GetCardEffect().GetType().Name) && !StatsFromSelf(state.GetCardEffect().GetType().Name) &&
                !(state.GetCardEffect() is CardEffectAddStatusEffectBasedOnHealth)) return null;
            CardEffectData effect = state.GetSourceCardEffectData();
            return Describe(effect, state.GetParamInt(), state.GetUseIntRange() ?
                new CardEffectRange(state.GetParamMinInt(), state.GetParamMaxInt(), state.GetParamMultiplier()) : null,
                state.GetCardEffect() is CardEffectAddStatusEffect ? state.GetParamStatusEffectStackData().Select(status =>
                    BattleActionProbe.Status(status.statusId, status.count)).ToArray() : null);
        }
        internal static CardActionEffect? Definition(CardEffectData effect, List<string>? interactions = null) =>
            effect.GetEffectStateName() == "CardEffectSynergizeStatusEffects" ? Synergy(effect) :
            effect.GetEffectStateName() == "CardEffectFeederRules" ? Feeding(effect) :
            effect.GetEffectStateName() == "CardEffectAdjustDragonsHoardPerStatusEffect" ? HoardPerStatus(effect) :
            effect.GetEffectStateName() == "CardEffectRemoveEquipment" ? BattleActionProbe.DescribeTriggerEffect(effect, interactions ?? new List<string>()) :
            effect.GetEffectStateName() is "CardEffectAdjustAllStatusEffects" or "CardEffectAdjustCorruption" or "CardEffectPlayUnitTrigger" or "CardEffectDamagePerHp" or "CardEffectHealTrain" or "CardEffectAddTempCardUpgradeToSpellsInDeck" or "CardEffectDamagePerSourceAttack" or "CardEffectRecursion" or "CardEffectAddTempCardUpgradeToNextDrawnCard" or "CardEffectFloorRearrange" ? BattleActionProbe.DescribeTriggerEffect(effect, interactions ?? new List<string>()) :
            effect.GetEffectStateName() == "CardEffectDrawType" ? TypedDraw(effect, effect.GetParamInt()) :
            AbilityLifecycleProbe.Known(effect.GetEffectStateName()) ? AbilityLifecycleProbe.Describe(effect) :
            AbilityCooldownProbe.Known(effect.GetEffectStateName()) ? AbilityCooldownProbe.Describe(effect, effect.GetParamInt(), effect.GetParamBool()) :
            effect.GetEffectStateName() == "CardEffectSpawnMonsterOrAddStatusEffect" ? SpawnOrStatus(effect,
                effect.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(), interactions) :
            StatsFromSelf(effect.GetEffectStateName()) ? Describe(effect, effect.GetParamInt(), null) :
            effect.GetEffectStateName() != "CardEffectNULL" && effect.GetEffectStateName() != "CardEffectHeal" &&
                effect.GetEffectStateName() != "CardEffectRemoveAllStatusEffects" && !DamageEffect(effect.GetEffectStateName()) && !StatusEffect(effect.GetEffectStateName()) &&
                !EnergyModel.IsNativeEffect(effect.GetEffectStateName()) && effect.GetEffectStateName() != "CardEffectDrawAdditionalNextTurn" && effect.GetEffectStateName() != "CardEffectAdjustRoomCapacity" && effect.GetEffectStateName() != "CardEffectAdjustForgePoints" &&
                effect.GetEffectStateName() != "CardEffectAdvanceMoonPhase" && !HealthEffect(effect.GetEffectStateName()) && effect.GetEffectStateName() != RelicResourceModel.PermanentPyreAttackAdjustment && effect.GetEffectStateName() != "CardEffectPermanentlyAdjustMaxPyreHealth" && effect.GetEffectStateName() != TriggeredStatusTransferModel.EffectType &&
                !HatchEffect(effect.GetEffectStateName()) && !StatsFromSelf(effect.GetEffectStateName()) ? null :
            // Effect states have no parent card; native damage/heal getters still clamp endpoints.
            Describe(effect, Numeric(effect, effect.GetParamInt()), effect.GetUseIntRange() ?
                new CardEffectRange(Numeric(effect, effect.GetParamMinInt()), Numeric(effect, effect.GetParamMaxInt()), effect.GetParamMultiplier()) : null);
        private static CardActionEffect Synergy(CardEffectData effect) => new CardActionEffect("SynergizeStatuses",
            effect.GetTargetMode().ToString(), effect.GetParamBool() ? 1 : 0,
            effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
            effect.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
            tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                effect.GetShouldCancelSubsequentEffectsIfTestFails(), false));
        private static CardActionEffect Feeding(CardEffectData effect) => new CardActionEffect(
            effect.GetParamBool() ? "SelfFeederRules" : "FeederRules", effect.GetTargetMode().ToString(), effect.GetParamInt(),
            effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
            Array.Empty<CombatStatus>(), tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                effect.GetShouldCancelSubsequentEffectsIfTestFails(), false));
        private static CardActionEffect HoardPerStatus(CardEffectData effect) => new CardActionEffect(
            "HoardPerStatus", effect.GetTargetMode().ToString(), effect.GetParamInt(),
            effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
            effect.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray());

        internal static TriggeredStatusScaling? Scaling(CardEffectState effect) => effect.GetCardEffect() is CardEffectAddStatusEffectBasedOnHealth ?
            new TriggeredStatusScaling(healthChoicePercent: effect.GetParamInt(), healthChoiceAbove: effect.GetParamBool()) : effect.GetCardEffect() is CardEffectAddStatusEffect ?
            new TriggeredStatusScaling(effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null,
                effect.GetUseHealthMissingStackMultiplier(), effect.GetUseMagicPowerMultiplier(), effect.GetParamBool2(),
                effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key) : null;
        internal static TriggeredStatusScaling? Scaling(CardEffectData effect) => effect.GetEffectStateName() == "CardEffectAddStatusEffectBasedOnHealth" ?
            new TriggeredStatusScaling(healthChoicePercent: effect.GetParamInt(), healthChoiceAbove: effect.GetParamBool()) : StatusEffect(effect.GetEffectStateName()) ?
            new TriggeredStatusScaling(effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null,
                effect.GetUseHealthMissingStackMultiplier(), effect.GetUseMagicPowerMultiplier(), effect.GetParamBool2(),
                effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key) : null;
        private static int Numeric(CardEffectData effect, int value) => effect.GetEffectStateName() == "CardEffectNULL" || StatusEffect(effect.GetEffectStateName()) ||
            effect.GetEffectStateName() == RelicResourceModel.PermanentPyreAttackAdjustment || effect.GetEffectStateName() == "CardEffectAdjustForgePoints" ||
            EnergyModel.IsNativeEffect(effect.GetEffectStateName()) || HealthEffect(effect.GetEffectStateName()) || (effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn" || effect.GetEffectStateName() == "CardEffectAdjustRoomCapacity" ||
                effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase") ? value : Math.Max(0, value);

        private static CardActionEffect SpawnOrStatus(CardEffectData effect, CombatStatus[] statuses, List<string>? interactions)
        {
            CharacterData? fallback = effect.GetParamCharacterData();
            CombatUnit? spawnFallback = fallback == null ? null : BattleActionProbe.SpawnTemplate(fallback, true,
                interactions ?? new List<string>());
            if (fallback == null) interactions?.Add("Missing triggered spawn-or-status fallback character.");
            return new CardActionEffect("SpawnOrStatus", effect.GetTargetMode().ToString(), effect.GetParamInt(),
                allowEnemy: false, allowPlayer: true, statuses, tests: new CardEffectTests(effect.GetShouldTest(),
                    effect.GetShouldFailToCastIfTestFails(), effect.GetShouldCancelSubsequentEffectsIfTestFails(), false, false),
                range: effect.GetUseIntRange() ? new CardEffectRange(effect.GetParamMinInt(), effect.GetParamMaxInt(), effect.GetParamMultiplier()) : null,
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key, Array.Empty<string>()),
                spawnFallbackUnit: spawnFallback,
                disallowStatusStackModifiers: effect.GetDisallowStatusEffectStackModifiers());
        }

        private static CardActionEffect TypedDraw(CardEffectData effect, int value) => new CardActionEffect("DrawType", "Self", value,
            false, false, Array.Empty<CombatStatus>(), typedDraw: TypedDrawProbe.DrawRule(effect),
            tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                effect.GetShouldCancelSubsequentEffectsIfTestFails(), false, false),
            range: effect.GetUseIntRange() ? new CardEffectRange(effect.GetParamMinInt(), effect.GetParamMaxInt(), effect.GetParamMultiplier()) : null);
        private static CardActionEffect Describe(CardEffectData effect, int value, CardEffectRange? range, CombatStatus[]? statuses = null)
        {
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            bool energy = EnergyModel.IsNativeEffect(effect.GetEffectStateName());
            bool capacity = effect.GetEffectStateName() == "CardEffectAdjustRoomCapacity";
            bool drawNext = effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn";
            bool forgePoints = effect.GetEffectStateName() == "CardEffectAdjustForgePoints";
            bool moonAdvance = effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase";
            bool transferStatuses = effect.GetEffectStateName() == TriggeredStatusTransferModel.EffectType;
            bool nullEffect = effect.GetEffectStateName() == "CardEffectNULL";
            bool hatchRemove = effect.GetEffectStateName() == "CardEffectRemoveStatusEffectPerCorrupt";
            bool sacrifice = effect.GetEffectStateName() == "CardEffectSacrifice";
            bool forceHatch = effect.GetEffectStateName() == "CardEffectForceHatchCheck";
            bool statsFromSelf = StatsFromSelf(effect.GetEffectStateName());
            bool pyreAttackAdjustment = effect.GetEffectStateName() == RelicResourceModel.PermanentPyreAttackAdjustment;
            bool pyreHealthAdjustment = effect.GetEffectStateName() == "CardEffectPermanentlyAdjustMaxPyreHealth";
            bool adjustHealth = effect.GetEffectStateName() == "CardEffectAdjustMaxHealth";
            bool buffHealth = effect.GetEffectStateName() == "CardEffectBuffMaxHealth";
            bool removeAllStatuses = effect.GetEffectStateName() == "CardEffectRemoveAllStatusEffects";
            return new CardActionEffect(nullEffect ? "Null" : removeAllStatuses ? "RemoveAllStatusEffects" : statsFromSelf ? "AddStatsFromSelf" : capacity ? "AdjustCapacity" : drawNext ? "DrawNextTurn" : forgePoints ? ForgePointsModel.Effect : moonAdvance ? "AdvanceMoonPhase" :
                adjustHealth ? "AdjustMaxHealth" : buffHealth ? "BuffHealth" : pyreAttackAdjustment ? "AdjustPyreAttack" : pyreHealthAdjustment ? "AdjustPyreMaxHealth" : transferStatuses ? "TransferAllStatusesFromUnit" : hatchRemove ? "RemoveStatusPerCorrupt" : sacrifice ? "Sacrifice" : forceHatch ? "ForceHatchCheck" : energy ? EnergyEffectProbe.Type(effect) : DamageEffect(effect.GetEffectStateName()) ? "Damage" : effect.GetEffectStateName() == "CardEffectHeal" ? "Heal" : "AddStatus",
                pyreAttackAdjustment || pyreHealthAdjustment ? "Self" : effect.GetTargetMode().ToString(), value,
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                statuses ?? (StatusEffect(effect.GetEffectStateName()) || hatchRemove || transferStatuses ?
                    effect.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray() : Array.Empty<CombatStatus>()),
                upgrade: drawNext ? BonusDrawProbe.Upgrade(effect) : null,
                lifetime: adjustHealth && effect.GetParamBool() ? "ParentUpgrade" : buffHealth ? ((UnitUpgradeLifetime)effect.GetAdditionalParamInt1()).ToString() : "",
                tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), effect.GetEffectStateName() != "CardEffectAddStatusEffectBasedOnHealth" && StatusEffect(effect.GetEffectStateName()) && effect.GetParamBool(), forgePoints ? true : energy || drawNext || capacity ? false : (bool?)null),
                range: range,
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()), onlyIfNoEnemies: capacity && effect.GetParamBool(),
                doNotFullyRemove: sacrifice ? effect.GetParamBool() : null,
                removeHalfStatusEffects: removeAllStatuses ? effect.GetParamBool() : null,
                nullTargetless: nullEffect && effect.GetParamBool2(), nullForwardSelf: nullEffect && effect.GetAdditionalParamInt() == 1,
                statusTransfer: transferStatuses ? new StatusTransferRule(((CardEffectTransferAllStatusEffectsFromUnit.TransferTarget)effect.GetParamInt()).ToString(),
                    ((CardEffectTransferAllStatusEffectsFromUnit.AllowedStatusEffectType)effect.GetAdditionalParamInt()).ToString(),
                    ((CardEffectTransferAllStatusEffectsFromUnit.TransferRoomType)effect.GetAdditionalParamInt1()).ToString(),
                    effect.GetParamBool(), effect.GetParamBool2(), effect.GetParamFloat()) : null);
        }
    }
}
