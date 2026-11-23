using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicGoldLossOnHitModel
    {
        internal const string OwnerId = "4c17065c-6391-45ce-a009-39ed89e1ba9c";
        internal const string OwnerKey = "LoseMoneyOnHit";
        internal const string Effect = "CardEffectRewardGold";

        internal static bool OwnsTrigger(CombatContext? context, CombatUnit unit, CombatTrigger trigger) =>
            context?.EnergyState?.PyreAlive != null && unit.Team == CombatTeam.Player && !unit.IsPyre &&
            trigger.NoCountModifiersAllowed == true && IsTrigger(trigger) && context.Relics?.Any(owner => owner is
            {
                DataId: OwnerId, AssetKey: OwnerKey, IsCovenant: false, DisallowedInPlacementPhase: false,
                EffectTypes: [RelicModel.AddTrigger], TriggerAdditions: [var addition]
            } && addition is { EffectIndex: 0, SourcePlayer: true, SourceEnemy: false,
                NoCountModifiersAllowed: true, SubtypeIsNone: true, CharacterAssetKeys.Count: 0, Conditions.Count: 0,
                Triggers: [var definition] } && definition.NoCountModifiersAllowed == false && IsTrigger(definition)) == true;

        private static bool IsTrigger(CombatTrigger trigger) => trigger is
        {
            Kind: "OnHit", Once: false, IgnoreSilence: true,
            TriggerAtThreshold: 0, Conditions: null,
            Origin: { UpgradeId: "", EquipmentCardId: 0, IsFromEquipment: false, OnlyIfEquipped: false },
            Effects: [{ Type: Effect, Value: -1, Counter: 0, Count: 0, CardPool.Count: 0,
                SkipDuplicateInHand: false, Generation: null, UnitUpgrade: null, Action: null,
                DamageStatusMultiplier: null, StatusScaling: null, Summon: null, Enchantment: null }]
        };
    }
}
