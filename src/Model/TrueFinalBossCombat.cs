using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TrueFinalBossCombatModel
    {
        // Native full-route captures include authored physical anchors and the
        // complete aura world. Ordinary summon/attack signatures and two
        // original Steward upgrades are calibrated alongside the baseline
        // actors. Other callbacks and relentless layouts retain their guards.
        internal static bool SupportsAlly(CombatUnit unit) => SupportsAlly(unit, null);
        internal static bool SupportsAlly(CombatUnit unit, CombatContext? context)
        {
            bool ordinary = SupportsOrdinaryAlly(unit, context);
            return unit.Team == CombatTeam.Player && !unit.IsPyre &&
            (unit.AssetKey == "TrainStewardBig" && unit.BaseAttack == 8 && unit.MaxHealth == 25 && unit.Size == 3 ||
                unit.AssetKey == "MonsterBanishedChampion1" && unit.BaseAttack == 1 && unit.MaxHealth == 10 && unit.Size == 2 ||
                SupportsUpgradedSteward(unit, context) || ordinary) &&
            unit.CanAttack && !unit.EndsBattleOnDeath && unit.Ability == null &&
            unit.Triggers.Count == 0 && unit.EquipmentCards.Count == 0 &&
            unit.Modifiers?.Upgrades.Count == 0 && unit.Statuses.All(status => status.Id == "corruption" ||
                status.Id == "multistrike" && status.ParamInt == 2 &&
                    (unit.AssetKey == "MonsterBanishedChampion1" && status.Stacks == 1 || ordinary && status.Stacks is >= 1 and <= 2));
        }

        private static bool SupportsOrdinaryAlly(CombatUnit unit, CombatContext? context)
        {
            CardInstanceState? card = context?.FindCard(unit.SpawnerCardId);
            CardMaskDefinition? definition = card?.MaskDescriptor?.Definition;
            UnitModifiers? modifiers = unit.Modifiers;
            if (card == null || definition == null || modifiers == null || !modifiers.SpawnerMatchesDefinition || modifiers.IsClone ||
                card.ExternalInteractions.Count != 0 || !Unmodified(card.Permanent) || !Unmodified(card.Temporary) ||
                definition.CardType != "Monster" || !definition.IsSpawner || !definition.HasSpawnData || !definition.CanAttack ||
                definition.AuthoredUnitAbility || definition.EquipmentUnitAbility || definition.AuthoredGraft ||
                definition.MainEffectNames.Count != 1 || definition.MainEffectNames[0] != "CardEffectSpawnMonster" ||
                definition.UnmodifiedSpawnAttack != unit.BaseAttack || definition.UnmodifiedSpawnHealth != unit.MaxHealth || definition.BaseSize != unit.Size ||
                modifiers.SelfAttackRules?.Count > 0 || modifiers.RoomResources?.Count > 0 || modifiers.RoomStatuses?.Count > 0 ||
                modifiers.RoomTriggerCounts?.Count > 0 || modifiers.SpawnCopySubtype != null || modifiers.Graft != null ||
                modifiers.SynergizeStatuses?.Count > 0 || modifiers.Sentry != null || modifiers.TroopAura != null ||
                modifiers.StatusAttackModifiers?.Count > 0) return false;
            return definition.StartingStatuses.All(status => status.Id == "multistrike" && status.Count is >= 1 and <= 2) &&
                unit.Statuses.Where(status => status.Id != "corruption").All(status =>
                    definition.StartingStatuses.Any(original => original.Id == status.Id && original.Count == status.Stacks)) &&
                definition.StartingStatuses.All(original => unit.Status(original.Id)?.Stacks == original.Count);
        }
        private static bool Unmodified(CardModifiers modifiers) => modifiers.Upgrades.Count == 0 && modifiers.PersistentHealth == 0 &&
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
                if (unit.Team != CombatTeam.Enemy || unit.Health != actor.Health || unit.MaxHealth != health[index] || unit.Status("lifelink") == null ||
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
