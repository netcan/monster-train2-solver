using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TrueFinalBossCombatModel
    {
        // Native full-route captures include authored physical anchors and the
        // complete aura world. Ordinary summon/attack signatures and two
        // original Steward upgrades and one fixed OnHit weakness signature
        // are calibrated alongside the baseline actors. Other callbacks and
        // relentless layouts retain their guards.
        internal static bool SupportsAlly(CombatUnit unit) => SupportsAlly(unit, null);
        internal static bool SupportsAlly(CombatUnit unit, CombatContext? context)
        {
            bool ordinary = SupportsOrdinaryAlly(unit, context);
            return unit.Team == CombatTeam.Player && !unit.IsPyre &&
            (unit.AssetKey == "TrainStewardBig" && unit.BaseAttack == 8 && unit.MaxHealth == 25 && unit.Size == 3 ||
                unit.AssetKey == "MonsterBanishedChampion1" && unit.BaseAttack == 1 && unit.MaxHealth == 10 && unit.Size == 2 ||
                SupportsUpgradedSteward(unit, context) || ordinary) &&
            unit.CanAttack && !unit.EndsBattleOnDeath && unit.Ability == null &&
            (unit.Triggers.Count == 0 || ordinary &&
                (unit.Status("valor") == null || context?.FindCard(unit.SpawnerCardId)?.Permanent.Upgrades.Count == 0 &&
                    context.LifeLinks is { Actors.Count: 5 } links && links.Actors.All(actor => actor.Health == 1)) &&
                SupportsOnHitMeleeWeakness(unit, context)) && unit.EquipmentCards.Count == 0 &&
            unit.Modifiers?.Upgrades.Count == 0 && unit.Statuses.All(status => status.Id == "corruption" ||
                ordinary && status.Id == "valor" && status.Stacks == 5 && status.ParamInt == 1 ||
                status.Id == "multistrike" && status.ParamInt == 2 &&
                    (unit.AssetKey == "MonsterBanishedChampion1" && status.Stacks == 1 || ordinary && status.Stacks is >= 1 and <= 2));
        }

        private static bool SupportsOnHitMeleeWeakness(CombatUnit unit, CombatContext? context)
        {
            CardInstanceState? card = context?.FindCard(unit.SpawnerCardId);
            if (card == null || !OrdinaryPermanent(card.Permanent) || card.Temporary.Upgrades.Count != 0 || unit.Triggers.Count != 1) return false;
            CombatTrigger trigger = unit.Triggers[0];
            if (trigger.Kind != "OnHit" || trigger.Once || trigger.IgnoreSilence || trigger.FireCount != 1 ||
                trigger.SkipDuringDeployment != false || trigger.TriggerAtThreshold != 0 || trigger.StateId == null ||
                trigger.DeathwishActive != null || trigger.Conditions != null || trigger.RemoveOnRelentlessChange ||
                trigger.NoCountModifiersAllowed == true || trigger.SuppressTriggerNotification != null ||
                trigger.Origin is not { UpgradeId.Length: 0, EquipmentCardId: 0, IsFromEquipment: false, OnlyIfEquipped: false } ||
                trigger.Effects.Count != 1) return false;
            CombatEffect effect = trigger.Effects[0];
            CardActionEffect? action = effect.Action;
            TriggeredStatusScaling? scaling = effect.StatusScaling;
            return effect.Type == "CardEffectAddStatusEffect" && effect.Value == 0 && effect.Counter == 0 && effect.Count == 0 &&
                effect.CardPool.Count == 0 && !effect.SkipDuplicateInHand && effect.Generation == null && effect.UnitUpgrade == null &&
                effect.DamageStatusMultiplier == null && effect.Summon == null && effect.Enchantment == null && effect.BoxedPool == null &&
                scaling is { ActorStatus: null, MissingHealth: false, MagicPower: false, IgnoreDualism: false,
                    Subtype.Length: 0, HealthChoicePercent: null, HealthChoiceAbove: false } &&
                action is { Type: "AddStatus", Target: "LastAttackerCharacter", Value: 0, AllowEnemy: true, AllowPlayer: false,
                    Statuses.Count: 1, Upgrade: null, Range: null, Generation: null, Lifetime.Length: 0, OnlyIfNoEnemies: false,
                    CooldownParameter: null, AbilityChange: null, CopyHeroStats: null, ScaleUpgrade: false,
                    SourceStatusMultiplier: null, StatusAdjustmentFilter: null, RemoveHalfStatusEffects: null } &&
                action.Statuses[0] is { Id: "melee weakness", Stacks: 1, ParamInt: 0, RemoveWhenTriggered: true } &&
                action.Tests is { ShouldTest: true, FailToCast: false, CancelSubsequent: false, StrictTargets: false, MustHaveValidTargets: false } &&
                action.Filters is { Health: "Both", RequiredStatuses.Count: 0, ExcludedStatuses.Count: 0, IgnoreBosses: false,
                    Subtype.Length: 0, ExcludedSubtypes.Count: 0 };
        }

        private static bool SupportsOrdinaryAlly(CombatUnit unit, CombatContext? context)
        {
            CardInstanceState? card = context?.FindCard(unit.SpawnerCardId);
            CardMaskDefinition? definition = card?.MaskDescriptor?.Definition;
            UnitModifiers? modifiers = unit.Modifiers;
            if (card == null || definition == null || modifiers == null || !modifiers.SpawnerMatchesDefinition || modifiers.IsClone ||
                card.ExternalInteractions.Count != 0 || !OrdinaryPermanent(card.Permanent) || !Unmodified(card.Temporary) ||
                definition.DataId != card.DataId || definition.CardType != "Monster" || !definition.IsSpawner || !definition.HasSpawnData || !definition.CanAttack ||
                definition.AuthoredUnitAbility || definition.EquipmentUnitAbility || definition.AuthoredGraft ||
                definition.MainEffectNames.Count != 1 || definition.MainEffectNames[0] != "CardEffectSpawnMonster" ||
                !definition.UnmodifiedSpawnAttack.HasValue || !definition.UnmodifiedSpawnHealth.HasValue ||
                CardModifierModel.UpgradedStat(definition.UnmodifiedSpawnAttack.Value, "Damage", true, card.Permanent) != unit.BaseAttack ||
                CardModifierModel.UpgradedStat(definition.UnmodifiedSpawnHealth.Value, "Health", true, card.Permanent) != unit.MaxHealth || definition.BaseSize != unit.Size ||
                modifiers.SelfAttackRules?.Count > 0 || modifiers.RoomResources?.Count > 0 || modifiers.RoomStatuses?.Count > 0 ||
                modifiers.RoomTriggerCounts?.Count > 0 || modifiers.SpawnCopySubtype != null || modifiers.Graft != null ||
                modifiers.SynergizeStatuses?.Count > 0 || modifiers.Sentry != null || modifiers.TroopAura != null ||
                modifiers.StatusAttackModifiers?.Count > 0) return false;
            return definition.StartingStatuses.All(status => status.Id == "multistrike" && status.Count is >= 1 and <= 2) &&
                unit.Statuses.Where(status => status.Id is not ("corruption" or "valor")).All(status =>
                    definition.StartingStatuses.Any(original => original.Id == status.Id && original.Count == status.Stacks)) &&
                definition.StartingStatuses.All(original => unit.Status(original.Id)?.Stacks == original.Count);
        }
        private static bool Unmodified(CardModifiers modifiers) => modifiers.Upgrades.Count == 0 && NoOffsets(modifiers);
        private static bool OrdinaryPermanent(CardModifiers modifiers) => NoOffsets(modifiers) && modifiers.Upgrades.Count <= 2 &&
            modifiers.Upgrades.All(PureNumericUpgrade);
        private static bool PureNumericUpgrade(CardUpgradeModifier upgrade)
        {
            CardLifecycleUpgrade? lifecycle = upgrade.Lifecycle;
            return upgrade.DataId.Length > 0 && upgrade.AssetKey.Length > 0 && Numeric(upgrade.Stats) &&
                upgrade.Statuses.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null &&
                upgrade.ExternalInteractions.Count == 0 && !upgrade.RemoveOnDiscard && !upgrade.Unique && !upgrade.ExcludeFromClones &&
                !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly && !upgrade.DoNotReplaceExistingAbility &&
                upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
                (upgrade.CloneDamageBase == null || upgrade.CloneDamageBase == upgrade.Stats.Damage) && upgrade.CloneHealBase == null &&
                upgrade.EquipmentSourceCardId == null && upgrade.EquipmentSourceUpgradeIndex == null &&
                upgrade.MaskMetadata is { UnitAbility: false, Statuses.Count: 0 } &&
                lifecycle != null && lifecycle.AssetKey == upgrade.AssetKey && lifecycle.Values.DataId == upgrade.DataId &&
                Numeric(lifecycle.Values.Stats) && lifecycle.Values.Stats.Damage == upgrade.Stats.Damage &&
                lifecycle.Values.Stats.Health == upgrade.Stats.Health && lifecycle.Values.Statuses.Count == 0 && !lifecycle.Values.UnitAbility &&
                !lifecycle.RemoveOnDiscard && !lifecycle.Unique && !lifecycle.AvoidClobbering && lifecycle.AddedTraits.Count == 0 &&
                lifecycle.RemovedRuntimeTypes.Count == 0 && lifecycle.TraitsModified.Count == 0 && lifecycle.ReplacedAssets.Count == 0 &&
                lifecycle.Triggers.Count == 0 && (lifecycle.OriginalDamage == null || lifecycle.OriginalDamage == upgrade.Stats.Damage) && lifecycle.OriginalHeal == null;
        }
        private static bool Numeric(CardStatModifier stats) => stats.Damage >= 0 && stats.Health >= 0 &&
            (stats.Damage > 0 || stats.Health > 0) && stats.Cost == 0 && stats.Heal == 0 && stats.Size == 0 &&
            stats.XCost == 0 && stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0;
        private static bool NoOffsets(CardModifiers modifiers) => modifiers.PersistentHealth == 0 &&
            modifiers.ExternalInteractions.Count == 0 && modifiers.MergedEquipment == null && modifiers.BoxedCards?.Count is not > 0 &&
            modifiers.Offsets.Damage == 0 && modifiers.Offsets.Health == 0 && modifiers.Offsets.Cost == 0 && modifiers.Offsets.Heal == 0 &&
            modifiers.Offsets.Size == 0 && modifiers.Offsets.XCost == 0 && modifiers.Offsets.EquipmentLimit == 0 && modifiers.Offsets.UpgradeSlotCount == 0;

        private static bool SupportsUpgradedSteward(CombatUnit unit, CombatContext? context)
        {
            CardInstanceState? card = context?.FindCard(unit.SpawnerCardId);
            if (unit.AssetKey != "TrainStewardBig" || unit.Size != 3 || card?.Permanent.Upgrades.Count != 1 ||
                card.Temporary.Upgrades.Count != 0) return false;
            CardUpgradeModifier upgrade = card.Permanent.Upgrades[0];
            bool health = upgrade.DataId == "32c1b067-d47d-4841-b166-1a227378a937" && upgrade.AssetKey == "BlessingBonusHealth" &&
                upgrade.Stats.Health == 20 && upgrade.Stats.Damage == 0;
            bool damage = upgrade.DataId == "1f0a08f2-ab63-442e-a089-a1063bf7e7aa" && upgrade.AssetKey == "EnhanceAttackOnConsume" &&
                upgrade.Stats.Health == 0 && upgrade.Stats.Damage == 10;
            return (health || damage) && unit.BaseAttack == 8 + upgrade.Stats.Damage && unit.MaxHealth == 25 + upgrade.Stats.Health &&
                upgrade.Stats.Cost == 0 && upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 &&
                upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 && upgrade.Statuses.Count == 0 &&
                upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null && upgrade.ExternalInteractions.Count == 0;
        }

        internal static bool Supports(RoomCombatState room)
        {
            LifeLinkContext? links = room.Context?.LifeLinks;
            EnchantmentWorld? world = room.Context?.Enchantments;
            if (links == null || LifeLinkModel.Validate(links) != null || room.Preview != links.CalculatingPreview ||
                !room.Preview && links.SharedPreviewHpChange != 0 || links.Actors.Count != 5 ||
                links.Actors.Any(actor => !actor.Initialized || !actor.TrueFinalBoss || !actor.HasLifeLink || actor.Health <= 0 || actor.Health != actor.PreviewHealth) ||
                world?.AutomaticLifecycle != true || world.Preview != room.Preview || world.Rooms.Count != 4 ||
                world.Rooms.Where((floor, index) => floor.RoomIndex != index || floor.Preview != room.Preview || floor.ExternalInteractions.Count > 0).Any()) return false;
            CombatUnit[] units = world.Rooms.SelectMany(floor => floor.Units).ToArray();
            int livingPyres = units.Count(unit => unit.IsPyre && unit.Health > 0);
            if (units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && !SupportsAlly(unit, room.Context)) ||
                livingPyres > 1 || !room.Preview && livingPyres != 1)
                return false;
            string[] assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2",
                "Boss_Level9_Entropy_Mid_V2", "Boss_Level9_Entropy_Top_V2" };
            int[] health = { 1555, 2750, 3500, 3500, 3500 };
            int[] floors = { 3, -1, 0, 1, 2 };
            CombatUnit[] bodies = units.Where(unit => links.Actors.Any(actor => actor.Id == unit.Id)).ToArray();
            if (bodies.Length != 5 || assets.Any(asset => bodies.Count(unit => unit.AssetKey == asset) != 1)) return false;
            int bottom = bodies.Single(unit => unit.AssetKey == assets[2]).Id;
            for (int index = 0; index < 5; index++)
            {
                CombatUnit unit = bodies.Single(actor => actor.AssetKey == assets[index]);
                LifeLinkActor actor = links.Actors.Single(link => link.Id == unit.Id);
                int floor = world.Rooms.Single(candidate => candidate.Units.Any(member => member.Id == unit.Id)).RoomIndex;
                if (!TrueFinalBossCovenantModel.Adjustment(room.Context, unit.AssetKey, out _, out int extraHealth) ||
                    unit.Team != CombatTeam.Enemy || unit.Health != actor.Health || unit.MaxHealth != health[index] + extraHealth || unit.Status("lifelink") == null ||
                    actor.OuterBoss != (index >= 2) || actor.LinkedId != (index >= 3 ? bottom : 0) ||
                    index != 1 && floor != floors[index] || index == 1 && floor > 2 ||
                    index >= 2 && (unit.BumpRules?.OuterBoss != true || unit.BumpRules.OuterBossRelentlessPhase &&
                        (index != 4 || unit.Status("relentless")?.Stacks != 1) ||
                        unit.BumpRules.CastingAnchorSlots?.Count != 4 || unit.BumpRules.RelentlessAnchorSlots?.Count != 4)) return false;
            }
            return true;
        }
    }
}
