using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TrueFinalBossTerminalCombatModel
    {
        internal static bool SupportsRoot(RoomCombatState source, int targetId, bool externalPyreAttack = false, bool plainSpellDamage = false,
            bool triggeredCommand = false, bool ordinaryPyreAttack = false, bool maximumHealthSacrifice = false)
        {
            LifeLinkContext? links = source.Context?.LifeLinks;
            EnchantmentWorld? world = source.Context?.Enchantments;
            if (links == null || LifeLinkModel.Validate(links) != null ||
                links.Actors.Count != 5 || links.Actors.Any(actor => !actor.Initialized || !actor.TrueFinalBoss || !actor.HasLifeLink) ||
                world?.AutomaticLifecycle != true || world.Rooms.Count != 4 ||
                !source.Preview && !TrueFinalBossCombatModel.Supports(source) || source.Preview && !links.CalculatingPreview)
                return false;
            string[] assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2",
                "Boss_Level9_Entropy_Mid_V2", "Boss_Level9_Entropy_Top_V2" };
            CombatUnit[] bodies = world.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy &&
                links.Actors.Any(actor => actor.Id == unit.Id)).ToArray();
            if (bodies.Length != 5 || assets.Any(asset => bodies.Count(unit => unit.AssetKey == asset) != 1)) return false;
            bool earlyEntropy = maximumHealthSacrifice && SupportsEarlyEntropySacrifice(source, links, world, bodies, targetId);
            if (!earlyEntropy && links.Actors.Any(actor => actor.Health != 1)) return false;
            return world.Rooms.SelectMany(room => room.Units).All(unit => unit.IsPyre ||
                unit.Team == CombatTeam.Player && (unit.Triggers.Count == 0 || plainSpellDamage && !source.Preview || triggeredCommand ||
                    ordinaryPyreAttack && source.RoomIndex == 3 && source.Units.Any(pyre => pyre.IsPyre)) &&
                    TrueFinalBossCombatModel.SupportsAlly(unit, source.Context) &&
                    (unit.Triggers.Count == 0 || source.Context!.FindCard(unit.SpawnerCardId)?.Permanent.Upgrades.Count == 0) ||
                unit.Team == CombatTeam.Enemy && unit.IsBoss != true && !unit.EndsBattleOnDeath ||
                unit.Team == CombatTeam.Enemy && unit.EquipmentCards.Count == 0 && unit.Modifiers?.Upgrades.Count == 0 &&
                links.Actors.Any(actor => actor.Id == unit.Id) &&
                unit.Triggers.All(trigger => trigger.Kind is "OnSpawn" or "PostCombat" or "OnTrainRoomLoop") &&
                unit.Statuses.All(status => status.Id is "buff" or "immobile" or "lifelink" or "pyrebound" or "spell shield" or "pyrebane" or "trample")) &&
                source.Context!.Relics?.Any(relic => relic.IsCovenant == true
                    ? !TrueFinalBossCovenantModel.KnownOwner(relic) : relic.EffectTypes.Count > 0) != true &&
                source.Units.Any(unit => unit.Id == targetId && assets.Contains(unit.AssetKey) && (unit.Health == 1 || earlyEntropy) &&
                    (!source.Preview || source.RoomIndex == 3 && unit.AssetKey == "Boss_Level9_Dominion_V2" ||
                        source.RoomIndex == 0 && unit.AssetKey == "Boss_Level9_Entropy_Bot_V2" &&
                        (externalPyreAttack || source.Units.Any(ally => TrueFinalBossCombatModel.SupportsAlly(ally, source.Context) &&
                            ally.Status("valor") is { Stacks: 5, ParamInt: 1 }))));
        }

        private static bool SupportsEarlyEntropySacrifice(RoomCombatState source, LifeLinkContext links,
            EnchantmentWorld world, CombatUnit[] bodies, int targetId)
        {
            string? targetAsset = source.RoomIndex switch
            {
                0 => "Boss_Level9_Entropy_Bot_V2",
                1 => "Boss_Level9_Entropy_Mid_V2",
                2 => "Boss_Level9_Entropy_Top_V2",
                _ => null
            };
            if (source.Preview || links.CalculatingPreview || links.TemporaryPreviewIds != null || targetAsset == null ||
                world.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre) ||
                !source.Units.Any(unit => unit.Id == targetId && unit.AssetKey == targetAsset)) return false;

            int ownerId = bodies.Single(unit => unit.AssetKey == "Boss_Level9_Entropy_Bot_V2").Id;
            return bodies.All(body =>
            {
                LifeLinkActor actor = links.Actors.Single(item => item.Id == body.Id);
                if (body.AssetKey is "Boss_Level9_Dominion_V2" or "Boss_Level9_Savagery_V2")
                    return actor.LinkedId == 0 && actor.Health == 1 && body.Health == 1 && body.MaxHealth == 1;
                return actor.LinkedId == (body.Id == ownerId ? 0 : ownerId) && actor.Health == 8 && body.Health == 8 && body.MaxHealth == 8;
            }) && !LifeLinkModel.Protected(links, targetId);
        }
    }
}
