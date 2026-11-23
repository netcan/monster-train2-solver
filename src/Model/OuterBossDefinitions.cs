using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class OuterBossDefinitions
    {
        public const string UnrulyScourge = "Boss_SoulSavior_R3_Unruly_Scourge";
        internal const string UnrulyScourgeCard = "4d1f27af-3122-4a08-87e3-5ef5a14ced94";
        public const string CaelShieldTrial = "Boss_Level6_Cael_DamageShield_TitanTrial";
        public const string CaelAtkHpTrial = "Boss_Level6_Cael_AtkHP_TitanTrial";
        public const string CaelLifestealTrial = "Boss_Level6_Cael_Lifesteal_TitanTrial";
        internal const string CaelShieldTrialOwner = "Enemy_SpawnHeroEaterFeedDamageShield_TitanTrial";
        internal const string CaelShieldTrialOwnerId = "12f05d0b-df25-4cad-b7f3-9c52eac46e04";
        internal const string CaelAtkHpTrialOwner = "Enemy_SpawnHeroEaterFeedAtkHP_TitanTrial";
        internal const string CaelAtkHpTrialOwnerId = "7e1b3da5-e6c6-408f-8d2b-055e4b286657";
        internal const string CaelLifestealTrialOwner = "Enemy_SpawnHeroEaterFeedLifesteal_TitanTrial";
        internal const string CaelLifestealTrialOwnerId = "4501ae73-0ec3-489c-b78c-3d1155af0bd4";

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

        internal static bool CalibratedAngelDuoTrialGrowth(CombatUnit unit, CombatContext? context,
            string triggerKind, CombatEffect effect)
        {
            bool incant = unit.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth" && triggerKind == "CardSpellPlayed";
            bool slay = unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" && triggerKind == "OnKill";
            if ((!incant && !slay) || unit.Team != CombatTeam.Enemy || unit.SpawnerCardId != 0 ||
                AngelDuoTrialGrowthRules(context) is not { } additions)
                return false;

            int index = incant ? 0 : 1;
            if (additions[index] is not {
                SourcePlayer: false, SourceEnemy: true, SubtypeIsNone: true, SubtypeIsPyre: false,
                NoCountModifiersAllowed: false, CharacterAssetKeys: [var character], Conditions.Count: 0,
                ExternalInteractions.Count: 0, Triggers: [{
                    Once: false, IgnoreSilence: false, SkipDuringDeployment: false, TriggerAtThreshold: 0,
                    Effects: [var authored]
                } trigger]
            } || additions[index].EffectIndex != index || character != unit.AssetKey || trigger.Kind != triggerKind)
                return false;

            string type = incant ? "CardEffectBuffMaxHealth" : "CardEffectBuffDamage";
            string actionType = incant ? "BuffHealth" : "BuffAttack";
            string subtype = incant ? "SubtypesData_SavageAngel" : "SubtypesData_EntropicAngel";
            string lifetime = incant ? "TemporaryUntilEndOfBattle" : "";
            int value = incant ? 50 : 5;
            bool Matches(CombatEffect candidate) => candidate.Type == type && candidate.Value == value &&
                candidate.Action is {
                    Target: "Tower", Range: null, AllowEnemy: true, AllowPlayer: true, UseCardTraits: false,
                    Filters: { Health: "Both", RequiredStatuses.Count: 0, ExcludedStatuses.Count: 0,
                        IgnoreBosses: false, ExcludedSubtypes.Count: 0 } filters
                } action && action.Type == actionType && action.Value == value && action.Lifetime == lifetime && filters.Subtype == subtype;
            return Matches(authored) && Matches(effect);
        }

        internal static bool CalibratedAngelDuoTrialFullMoon(CombatUnit unit, CombatContext? context) =>
            unit is { AssetKey: "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth", MoonPhaseMask: 4,
                BaseAttack: 11, Attack: 11, Health: 1250, MaxHealth: 1250 } &&
            AngelDuoTrialGrowthRules(context) is [{ Triggers: [{ Effects: [var incant] }] }, _] &&
            CalibratedAngelDuoTrialGrowth(unit, context, "CardSpellPlayed", incant);

        private static IReadOnlyList<RelicCharacterTriggers>? AngelDuoTrialGrowthRules(CombatContext? context) =>
            context?.Relics?.Where(relic => relic.DataId == "6dd04165-0c8d-42b3-9215-405e39952466").ToArray() is [{
                AssetKey: "Enemy_Boss_Level6_AngelDuo_SlayAttackIncantHealth_TitanTrial",
                IsCovenant: false, DisallowedInPlacementPhase: false,
                EffectTypes: ["RelicEffectAddTrigger", "RelicEffectAddTrigger"], TriggerAdditions: { Count: 2 } additions }]
                ? additions : null;

        public static bool IsCael(string assetKey) => assetKey is "Boss_Level6_Cael_AtkHP" or
            "Boss_Level6_Cael_DamageShield" or "Boss_Level6_Cael_Lifesteal";
        public static bool IsCaelTrial(string assetKey) => assetKey is CaelShieldTrial or CaelAtkHpTrial or CaelLifestealTrial;

        internal static bool CalibratedCaelTrialUpgrade(string assetKey, CardUpgradeModifier? upgrade) =>
            upgrade is { Stats: { Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 },
                ExternalInteractions.Count: 0, AbilityUpgrade: null, TriggerUpgrades: null } && assetKey switch
            {
                CaelShieldTrial => upgrade is {
                    DataId: "067b599f-ac23-4c99-8841-192fc5b7dd74", AssetKey: "Boss_Level6_FeedDamageShieldUpgrade_TitanTrial",
                    Stats: { Damage: 0, Health: 0 },
                    Statuses: [{ Id: "damage shield", Stacks: 1 }, { Id: "spell shield", Stacks: 1 }] },
                CaelAtkHpTrial => upgrade is {
                    DataId: "43d3af4b-44b7-4d8c-9aac-4768b7651a60", AssetKey: "Boss_Level6_FeedAtkHPUpgrade_TitanTrial",
                    Stats: { Damage: 0, Health: 50 }, Statuses: [{ Id: "spikes", Stacks: 6 }] },
                CaelLifestealTrial => upgrade is {
                    DataId: "3fde76c8-3100-4483-95e8-30dfc50eac6e", AssetKey: "Boss_Level6_FeedLifestealUpgrade_TitanTrial",
                    Stats: { Damage: 12, Health: 0 }, Statuses: [{ Id: "lifesteal", Stacks: 1 }] },
                _ => false
            };

        internal static bool CalibratedCaelShieldTrial(CombatUnit unit, CombatContext? context)
            => unit.AssetKey == CaelShieldTrial && CalibratedCaelTrial(unit, context);

        internal static bool CalibratedCaelTrial(CombatUnit unit, CombatContext? context, EnemyBirthStatAdjustment? birthStats = null)
        {
            if (unit is not { Team: CombatTeam.Enemy, BaseAttack: 22, Attack: 22,
                    Health: 1800, MaxHealth: 1800, Size: 6, CanAttack: true, IsBoss: true, EndsBattleOnDeath: true,
                    SpawnerCardId: 0, BumpRules: { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 },
                    Statuses.Count: 0, Ability: null, Modifiers.Upgrades.Count: 0, Triggers: [{
                        Kind: "PostCombat", Once: false, HasTriggered: false, IgnoreSilence: false, FireCount: 1,
                        SkipDuringDeployment: true, TriggerAtThreshold: 0, Conditions: null, RemoveOnRelentlessChange: false,
                        Origin: { UpgradeId.Length: 0, EquipmentCardId: 0, IsFromEquipment: false, OnlyIfEquipped: false },
                        Effects: [{ Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: {
                            Type: "UnitUpgrade", Target: "FrontInRoom", AllowEnemy: true, AllowPlayer: false, Value: 0,
                            Lifetime: "TemporaryUntilEndOfBattle", Range: null, UseCardTraits: false, Filters.IgnoreBosses: true,
                            Upgrade: { } upgrade } }] }] } ||
                !CalibratedCaelTrialUpgrade(unit.AssetKey, upgrade) || context?.Relics == null)
                return false;
            CombatRelicState[] covenants = context.Relics.Where(owner => owner.IsCovenant == true).ToArray();
            bool calibratedStats = covenants.Length == 0 ? birthStats == null :
                unit.AssetKey == CaelShieldTrial && birthStats is { Attack: 0, Health: 700 } &&
                TrueFinalBossCovenantModel.CalibratedCaelShieldTrialOwners(covenants) &&
                birthStats.CovenantIds.SequenceEqual(covenants.Select(owner => owner.DataId));
            if (!calibratedStats) return false;
            (string ownerId, string ownerKey) = unit.AssetKey switch
            {
                CaelShieldTrial => (CaelShieldTrialOwnerId, CaelShieldTrialOwner),
                CaelAtkHpTrial => (CaelAtkHpTrialOwnerId, CaelAtkHpTrialOwner),
                CaelLifestealTrial => (CaelLifestealTrialOwnerId, CaelLifestealTrialOwner),
                _ => (string.Empty, string.Empty)
            };
            return context.Relics.Where(owner => owner.EffectTypes.Contains("RelicEffectOnSpawnHeroSubstitution")).ToArray() is [{
                IsCovenant: false, DisallowedInPlacementPhase: false,
                EffectTypes: ["RelicEffectOnSpawnHeroSubstitution", "RelicEffectNull"] } owner] &&
                ownerId == owner.DataId && ownerKey == owner.AssetKey;
        }
        public static bool IsArkionPyre(string assetKey) => assetKey == "Boss_Level3_Arkion_DamagePyre";
        public static bool IsArkionRage(string assetKey) => assetKey == "Boss_Level3_Arkion_SiphonRage";
        public static bool IsArkionPush(string assetKey) => assetKey == "Boss_Level3_Arkion_PushAttack";
        public static bool IsFleshweaver(string assetKey) => assetKey is "Boss_Level3_Fleshweaver_Corruption" or
            "Boss_Level3_Fleshweaver_MoreMass" or "Boss_Level3_Fleshweaver_Spike";
        public static bool Supports(string assetKey) => assetKey is
            "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" or
            "Boss_Level8_AllyBuffer_TitanTrial" or "Boss_Level8_Fragile_TitanTrial" or "Boss_Level8_Corrupter_TitanTrial" or
            TriggeredStatusModel.OriginalSpurned or TriggeredStatusModel.OriginalSpurnedWitherbloom or TriggeredStatusModel.OriginalSpurnedDuality or
            TriggeredSummonModel.UnrulyBoss or UnrulyScourge ||
            IsAngelDuo(assetKey) || IsCael(assetKey) || IsCaelTrial(assetKey) || IsArkionPyre(assetKey) ||
            IsArkionRage(assetKey) || IsArkionPush(assetKey) || IsFleshweaver(assetKey);
    }
}
