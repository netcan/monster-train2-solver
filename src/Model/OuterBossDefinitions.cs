using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class OuterBossDefinitions
    {
        public const string CaelShieldTrial = "Boss_Level6_Cael_DamageShield_TitanTrial";
        internal const string CaelShieldTrialOwner = "Enemy_SpawnHeroEaterFeedDamageShield_TitanTrial";
        internal const string CaelShieldTrialOwnerId = "12f05d0b-df25-4cad-b7f3-9c52eac46e04";

        public static string? AngelDuoCompanion(string assetKey) => assetKey switch
        {
            "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" => "Boss_Level6_AngelDuo_Savage_Variant1_LoopAttack",
            "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth" => "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack",
            "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" => "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack",
            _ => null
        };
        public static bool IsAngelDuo(string assetKey) => AngelDuoCompanion(assetKey) != null;
        public static bool IsAngelDuoInner(string assetKey) => assetKey is
            "Boss_Level6_AngelDuo_Savage_Variant1_LoopAttack" or "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" or
            "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack";
        public static bool IsAngelDuoActor(string assetKey) => IsAngelDuo(assetKey) || IsAngelDuoInner(assetKey);
        public static bool IsAngelDuoScenario(string? assetKey) => assetKey is
            "Level6AngelDuo_LoopAttackResolveHealth" or "Level6AngelDuo_RallyAttackIncantHealth" or
            "Level6AngelDuo_RevengeAttackHarvestHealth";
        public static bool IsCael(string assetKey) => assetKey is "Boss_Level6_Cael_AtkHP" or
            "Boss_Level6_Cael_DamageShield" or "Boss_Level6_Cael_Lifesteal";
        internal static bool CalibratedCaelShieldTrial(CombatUnit unit, CombatContext? context)
        {
            if (unit is not { AssetKey: CaelShieldTrial, Team: CombatTeam.Enemy, BaseAttack: 22, Attack: 22,
                    Health: 1800, MaxHealth: 1800, Size: 6, CanAttack: true, IsBoss: true, EndsBattleOnDeath: true,
                    SpawnerCardId: 0, BumpRules: { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 },
                    Statuses.Count: 0, Ability: null, Modifiers.Upgrades.Count: 0, Triggers: [{
                        Kind: "PostCombat", Once: false, HasTriggered: false, IgnoreSilence: false, FireCount: 1,
                        SkipDuringDeployment: true, TriggerAtThreshold: 0, Conditions: null, RemoveOnRelentlessChange: false,
                        Origin: { UpgradeId.Length: 0, EquipmentCardId: 0, IsFromEquipment: false, OnlyIfEquipped: false },
                        Effects: [{ Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: {
                            Type: "UnitUpgrade", Target: "FrontInRoom", AllowEnemy: true, AllowPlayer: false, Value: 0,
                            Lifetime: "TemporaryUntilEndOfBattle", Range: null, UseCardTraits: false, Filters.IgnoreBosses: true,
                            Upgrade: {
                                DataId: "067b599f-ac23-4c99-8841-192fc5b7dd74", AssetKey: "Boss_Level6_FeedDamageShieldUpgrade_TitanTrial",
                                Stats: { Damage: 0, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                                ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null,
                                Statuses: [{ Id: "damage shield", Stacks: 1 }, { Id: "spell shield", Stacks: 1 }] } } }] }] } ||
                context?.Relics == null || context.Relics.Any(owner => owner.IsCovenant == true))
                return false;
            return context.Relics.Where(owner => owner.EffectTypes.Contains("RelicEffectOnSpawnHeroSubstitution")).ToArray() is [{
                DataId: CaelShieldTrialOwnerId, AssetKey: CaelShieldTrialOwner, IsCovenant: false, DisallowedInPlacementPhase: false,
                EffectTypes: ["RelicEffectOnSpawnHeroSubstitution", "RelicEffectNull"] }];
        }
        public static bool IsArkionPyre(string assetKey) => assetKey == "Boss_Level3_Arkion_DamagePyre";
        public static bool IsArkionRage(string assetKey) => assetKey == "Boss_Level3_Arkion_SiphonRage";
        public static bool IsArkionPush(string assetKey) => assetKey == "Boss_Level3_Arkion_PushAttack";
        public static bool IsFleshweaver(string assetKey) => assetKey is "Boss_Level3_Fleshweaver_Corruption" or
            "Boss_Level3_Fleshweaver_MoreMass" or "Boss_Level3_Fleshweaver_Spike";
        public static bool Supports(string assetKey) => assetKey is
            "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" or
            "Boss_Level8_AllyBuffer_TitanTrial" or "Boss_Level8_Fragile_TitanTrial" or "Boss_Level8_Corrupter_TitanTrial" or
            CaelShieldTrial or
            TriggeredStatusModel.OriginalSpurned or TriggeredStatusModel.OriginalSpurnedWitherbloom or TriggeredStatusModel.OriginalSpurnedDuality ||
            IsAngelDuo(assetKey) || IsCael(assetKey) || IsArkionPyre(assetKey) || IsArkionRage(assetKey) || IsArkionPush(assetKey) || IsFleshweaver(assetKey);
    }
}
