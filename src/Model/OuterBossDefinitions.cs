using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class OuterBossDefinitions
    {
        public const string UnrulyScourge = "Boss_SoulSavior_R3_Unruly_Scourge";
        public const string UnrulySniperAdvance = "Boss_SoulSavior_R3_Unruly_SniperAdvance";
        public const string DutifulBurst = "Boss_SoulSavior_R1_Dutiful_Burst";
        public const string DutifulArmor = "Boss_SoulSavior_R1_Dutiful_Armor";
        public const string DutifulAscendDescend = "Boss_SoulSavior_R1_Dutiful_AscendDescend";
        public const string IndulgentTitanskin = "Boss_SoulSavior_R2_Indulgent_Titanskin";
        internal const string IndulgentReduceCapacity = "Boss_SoulSavior_R2_Indulgent_ReduceCapacityResolve";
        internal const string IndulgentGorgeOnSlay = "Boss_SoulSavior_R2_Indulgent_GorgeOnSlay";
        public const string LifemotherReanimate = "Boss_SoulSavior_Final_Lifemother_HealReanimate";
        internal const string LifemotherInfested = "Boss_SoulSavior_Final_Lifemother_Infested";
        internal const string LifemotherDebuffsOwner = "Boss_Railforged_Lifemother_Debuff";
        internal const string LifemotherDebuffsOwnerId = "93efdaa9-7496-46d3-ae86-7a08223b8f94";
        internal static bool IsLifemotherDebuffsBirth(CombatUnit unit) =>
            unit is { AssetKey: TriggeredStatusTransferModel.LifemotherDebuffer, Team: CombatTeam.Enemy, BaseAttack: 50, Attack: 50,
                Health: 9000, MaxHealth: 9000, Size: 5, Statuses: [{ Id: "trample", Stacks: 1 }],
                Triggers: [var attack, var death, var turn] } &&
            unit.Triggers.All(trigger => !trigger.Once && !trigger.HasTriggered && !trigger.IgnoreSilence &&
                trigger.FireCount == 1 && trigger.TriggerAtThreshold == 0 && trigger.Conditions == null &&
                trigger.RemoveOnRelentlessChange != true && trigger.Effects.Count == 1) &&
            attack is { Kind: "OnAttacking", Effects: [{ Type: "CardEffectAddStatusEffect", Action:
                { Type: "AddStatus", Target: "LastAttackedCharacter", Range: null, Value: 0, AllowEnemy: false, AllowPlayer: true,
                    Statuses: [{ Id: "malaise", Stacks: 12 }] } }] } &&
            death.Effects[0].Type == TriggeredStatusTransferModel.EffectType &&
            TriggeredStatusTransferModel.IsLifemotherDeath(unit, death.Kind, death.Effects[0].Action) &&
            turn is { Kind: "OnTeamTurnBegin", Effects: [{ Type: "CardEffectRemoveStatusEffect", Action:
                { Type: "RemoveStatus", Target: "Self", Range: null, Value: 5, AllowEnemy: true, AllowPlayer: true,
                    Statuses: [{ Id: "dazed", Stacks: 5 }] } }] };
        internal const string IndulgentTitanskinOwnerId = "d029a392-703b-4b96-ae89-f89c2c38aaac";
        internal const string IndulgentTitanskinOwnerKey = "Boss_Railforged_Region2_BossBattle_3";
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

        public static bool IsDutiful(string assetKey) => assetKey is DutifulBurst or DutifulArmor or DutifulAscendDescend;

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
        internal static bool IsUnrulySniperAdvance(CombatUnit unit, CombatTrigger trigger, CombatEffect effect) =>
            unit.AssetKey == UnrulySniperAdvance && unit.Team == CombatTeam.Enemy &&
            trigger is { Kind: "OnAttacking", Once: false, IgnoreSilence: false, Effects.Count: 1 } &&
            effect is { Type: "CardEffectFloorRearrange", Action: { Type: "FloorRearrange",
                Target: "LastAttackedCharacter", Value: 0, AllowEnemy: false, AllowPlayer: true,
                Range: null, UseCardTraits: false, Statuses.Count: 0 } };
        internal static bool IsIndulgentTitanskinBirth(CombatUnit unit) =>
            unit is { AssetKey: IndulgentTitanskin, Team: CombatTeam.Enemy, Attack: 12, BaseAttack: 12,
                Health: 1000, MaxHealth: 1000, Size: 5, Triggers: [{ Kind: "OnFeed", Once: false, IgnoreSilence: false, Effects: [var rage, var skin] }] } &&
            unit.Statuses.Count == 2 && unit.Status("trample")?.Stacks == 1 && unit.Status("titanskin")?.Stacks == 3 &&
            OriginalGrant(rage, "buff") && OriginalGrant(skin, "titanskin");

        internal static bool IsIndulgentTrampleBirth(CombatUnit unit, CombatContext? context, bool beforePlacement = false)
        {
            bool startingStatuses = beforePlacement
                ? unit is { MoonPhaseMask: 5, Statuses: [{ Id: "newmoon", Stacks: 1 }, { Id: "trample", Stacks: 1 }] }
                : unit.Statuses is [{ Id: "trample", Stacks: 1 }];
            if (!startingStatuses || unit is not { Health: 1100, Triggers: [_, var additional] } ||
                unit.Attack != unit.BaseAttack || context?.Relics == null) return false;
            return unit.AssetKey switch
            {
                IndulgentReduceCapacity => IsIndulgentCapacityFeed(unit, additional) && context.Relics.Count(relic => relic is
                    { DataId: "0809140a-a5c5-41f7-ab7c-375e6d88fb14", AssetKey: "Boss_Railforged_Region2_BossBattle_2",
                        IsCovenant: false, EffectTypes: [RelicModel.Null] }) == 1,
                IndulgentGorgeOnSlay => IsIndulgentSlayFeed(unit, additional) && context.Relics.Count(relic => relic is
                    { DataId: "ce0711e4-7113-43cc-9cbf-ca92bbf1e4b3", AssetKey: "Boss_Railforged_Region2_BossBattle_1",
                        IsCovenant: false, EffectTypes.Count: 0 }) == 1,
                _ => false
            };
        }

        internal static bool IsIndulgentCapacityFeed(CombatUnit unit, CombatTrigger trigger) =>
            unit is { AssetKey: IndulgentReduceCapacity, Team: CombatTeam.Enemy, BumpRules.OuterBoss: true,
                BaseAttack: 12, MaxHealth: 1100, Size: 5, SpawnerCardId: 0,
                Triggers: [{ Kind: "OnFeed", Once: false, IgnoreSilence: false, FireCount: 1,
                    TriggerAtThreshold: 0, Conditions: null, Effects: [var rage] }, var postCombat] } &&
            ReferenceEquals(trigger, postCombat) &&
            trigger is { Kind: "PostCombat", Once: false, IgnoreSilence: false, FireCount: 1,
                TriggerAtThreshold: 0, Conditions: null, Effects: [var capacity, var replay] } &&
            capacity is { Type: "CardEffectAdjustRoomCapacity", Value: -1, Action: { Type: "AdjustCapacity", Target: "Room",
                Value: -1, Range: null, AllowEnemy: true, AllowPlayer: true, Statuses.Count: 0 } } &&
            replay is { Type: "CardEffectPlayUnitTrigger", Value: -1, Action: { Type: "PlayUnitTrigger", Target: "Self",
                UnitTriggerKind: "OnFeed", Value: -1, Range: null, AllowEnemy: true, AllowPlayer: true, Statuses.Count: 0 } } &&
            rage.Action?.Value == 0 && OriginalGrant(rage, "buff");

        internal static bool IsLifemotherInfestedBirth(CombatUnit unit, OuterBossProgress boss, CombatContext? context,
            bool beforePlacement = false) =>
            unit.MoonPhaseMask == (beforePlacement ? 6 : 4) && IsLifemotherInfestedOpeningActor(unit, context) &&
            boss is { AssetKey: LifemotherInfested, ValidAttackPhases: -1, BaseCanAttack: true,
                Actions: [[{ Behavior: "RandomOccupiedBossActionBehavior", TargetTeam: CombatTeam.Player, Empty: true, DestroyRoom: false }]] };

        internal static bool HasDormantLifemotherInfestedDeathGrant(CombatUnit unit, CombatTrigger trigger, CombatContext? context) =>
            context?.QueryFrame is { Turn: 1, MoonPhase: 2, MoonPhaseTrackingActive: false } &&
            unit.MoonPhaseMask == 6 && IsLifemotherInfestedOpeningActor(unit, context) && ReferenceEquals(trigger, unit.Triggers[0]);

        private static bool IsLifemotherInfestedOpeningActor(CombatUnit unit, CombatContext? context) => unit is
            { AssetKey: LifemotherInfested, Team: CombatTeam.Enemy, BumpRules.OuterBoss: true, BaseAttack: 40,
                Attack: 40, MaxHealth: 9000, Health: 9000, Size: 5, SpawnerCardId: 0, Statuses: [{ Id: "trample", Stacks: 1 }],
                Triggers: [{ Kind: "OnAnyUnitDeathOnFloor", Once: false, IgnoreSilence: false, FireCount: 1,
                    TriggerAtThreshold: 0, Conditions: null, Effects: [{ Type: "CardEffectAddStatusEffect",
                        Action: { Type: "AddStatus", Target: "Self", Value: 0, Range: null, AllowEnemy: false,
                            AllowPlayer: true, Statuses: [{ Id: "hivemind", Stacks: 2 }] } }] },
                    { Kind: "OnTeamTurnBegin", Once: false, IgnoreSilence: false, FireCount: 1,
                    TriggerAtThreshold: 0, Conditions: null, Effects: [{ Type: "CardEffectRemoveStatusEffect",
                        Action: { Type: "RemoveStatus", Target: "Self", Value: 0, Range: null, AllowEnemy: true,
                            AllowPlayer: true, Statuses: [{ Id: "dazed", Stacks: 5 }] } }] }] } &&
            context?.Relics?.Count(owner => owner is { DataId: "72f9f4e3-df23-4954-99e6-9040ab573590",
                AssetKey: "Boss_Railforged_Lifemother_Infested", IsCovenant: false,
                EffectTypes: [RelicModel.Null, RelicModel.Null, RelicModel.Null, RelicModel.Null] }) == 1;

        internal static bool IsIndulgentSlayFeed(CombatUnit unit, CombatTrigger trigger) =>
            unit is { AssetKey: IndulgentGorgeOnSlay, Team: CombatTeam.Enemy, BumpRules.OuterBoss: true,
                BaseAttack: 13, MaxHealth: 1100, Size: 5, SpawnerCardId: 0,
                Triggers: [{ Kind: "OnFeed", Once: false, IgnoreSilence: false, FireCount: 1,
                    TriggerAtThreshold: 0, Conditions: null, Effects: [var rage] }, var kill] } &&
            ReferenceEquals(trigger, kill) &&
            trigger is { Kind: "OnKill", Once: false, IgnoreSilence: false, FireCount: 1,
                TriggerAtThreshold: 0, Conditions: null, Effects: [var replay] } &&
            replay is { Type: "CardEffectPlayUnitTrigger", Value: 1, Action: { Type: "PlayUnitTrigger", Target: "Self",
                UnitTriggerKind: "OnFeed", Value: 1, Range: null, AllowEnemy: true, AllowPlayer: true, Statuses.Count: 0 } } &&
            rage.Action?.Value == 0 && OriginalGrant(rage, "buff");

        private static bool OriginalGrant(CombatEffect effect, string status) =>
            effect is { Type: "CardEffectAddStatusEffect", Action:
                { Type: "AddStatus", Target: "Self", AllowEnemy: true, AllowPlayer: true, Range: null, Statuses: [var grant] } } &&
            grant.Id == status && grant.Stacks == 3;
        public static bool Supports(string assetKey) => assetKey is
            "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" or
            "Boss_Level8_AllyBuffer_TitanTrial" or "Boss_Level8_Fragile_TitanTrial" or "Boss_Level8_Corrupter_TitanTrial" or
            TriggeredStatusModel.OriginalSpurned or TriggeredStatusModel.OriginalSpurnedWitherbloom or TriggeredStatusModel.OriginalSpurnedDuality or
            TriggeredSummonModel.UnrulyBoss or UnrulyScourge or UnrulySniperAdvance or IndulgentTitanskin or LifemotherReanimate or
            TriggeredStatusTransferModel.LifemotherDebuffer || IsDutiful(assetKey) ||
            IsAngelDuo(assetKey) || IsCael(assetKey) || IsCaelTrial(assetKey) || IsArkionPyre(assetKey) ||
            IsArkionRage(assetKey) || IsArkionPush(assetKey) || IsFleshweaver(assetKey);
    }
}
