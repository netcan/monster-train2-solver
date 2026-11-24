using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicBothSidesUnitsModel
    {
        internal const string DataId = "b75d5726-62ef-47ce-9378-ac3721594bab";
        internal const string AssetKey = "BothSidesUnitsAreStronger";
        internal const string PyreEffect = "RelicEffectTowerDamageDealtModified";

        internal static bool Applies(CombatRelicState relic) => relic.DataId == DataId && relic.AssetKey == AssetKey;

        internal static string? Validate(CombatRelicState relic)
        {
            if (relic.DataId != DataId && relic.AssetKey != AssetKey && !relic.EffectTypes.Contains(PyreEffect))
                return null;
            return relic is
            {
                DataId: DataId, AssetKey: AssetKey, IsCovenant: false, DisallowedInPlacementPhase: false,
                EffectTypes: [RelicModel.ModifyCharacterMaxHealth, RelicModel.ModifyCharacterMaxHealth,
                    RelicModel.ModifyCharacterAttackDamage, RelicModel.ModifyCharacterAttackDamage,
                    RelicModel.ModifyCardAttack, RelicModel.ModifyCardHealth, PyreEffect],
                CardAttackModifiers: [{ EffectIndex: 4, CharacterName: "", CharacterSubtypeKey: "", AttackModifier: 5,
                    AttackMultiplier: 0, UnmodifiedAttackMultiplier: 0, CardType: "Monster", MultiplyByUpgradeCount: false,
                    MultiplyByRelicCount: false, ExcludeChampions: false, ExcludeStartRelics: false, CardFilter: null }],
                CardHealthModifiers: [{ EffectIndex: 5, EffectType: RelicModel.ModifyCardHealth, CharacterName: "",
                    CharacterSubtypeKey: "", HealthModifier: 10, HealthMultiplier: 0, CardType: "Monster",
                    MultiplyByUpgradeCount: false, Conditions: [] }],
                ResourceEffects: [{ EffectIndex: 6, Type: PyreEffect, ParamInt: 10, ParamInt2: 0, Amount: 10,
                    CanApplyInPreviewMode: false, RequiredMutatorId: null, Conditions: [] }]
            } ? null : "Missing or changed original BothSidesUnitsAreStronger definition.";
        }

        internal static bool HealthApplies(CombatUnit unit, int fromCardId, int index) =>
            unit.Health > 0 && (index == 0 && unit.Team == CombatTeam.Enemy && unit.Status("cardless") == null ||
                index == 1 && unit.Team == CombatTeam.Player && !unit.IsPyre &&
                    (fromCardId == 0 || unit.Status("cardless") != null));

        internal static bool AttackApplies(CombatUnit unit, string trigger, int index) =>
            !unit.IsPyre && unit.Modifiers?.IsClone != true &&
            (index == 2 && unit.Team == CombatTeam.Enemy && trigger == "OnSpawn" ||
                index == 3 && unit.Team == CombatTeam.Player && trigger == "OnSpawnNotFromCard");

        internal static RoomCombatResult ApplyHealth(RoomCombatState source, int unitId, int fromCardId)
        {
            CombatContext? context = source.Context;
            if (fromCardId != 0)
            {
                context = UnitHealthModel.RecordCardBuff(context, fromCardId, 10, out string? error);
                if (error != null)
                    return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            }
            return UnitHealthModel.ApplyNative(source.WithContext(context), unitId, 10, triggerOnHeal: false);
        }
    }
}
