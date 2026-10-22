using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TrueFinalBossCombatModel
    {
        // Native full-route captures include authored physical anchors and the
        // complete aura world. Earlier health-only projections intentionally do
        // not opt into full combat. Other allied definitions, dormant Savagery and
        // Hellforged relentless/destruction require their own calibration.
        internal static bool SupportsAlly(CombatUnit unit) => unit.Team == CombatTeam.Player && !unit.IsPyre &&
            unit.AssetKey == "TrainStewardBig" && unit.BaseAttack == 8 && unit.MaxHealth == 25 &&
            unit.Size == 3 && unit.CanAttack && !unit.EndsBattleOnDeath && unit.Ability == null &&
            unit.Triggers.Count == 0 && unit.EquipmentCards.Count == 0 &&
            unit.Modifiers?.Upgrades.Count == 0 && unit.Statuses.All(status => status.Id == "corruption");

        internal static bool Supports(RoomCombatState room)
        {
            LifeLinkContext? links = room.Context?.LifeLinks;
            EnchantmentWorld? world = room.Context?.Enchantments;
            if (links == null || LifeLinkModel.Validate(links) != null || room.Preview != links.CalculatingPreview ||
                links.SharedPreviewHpChange != 0 || links.Actors.Count != 5 ||
                links.Actors.Any(actor => !actor.Initialized || !actor.TrueFinalBoss || !actor.HasLifeLink || actor.Health <= 0 || actor.Health != actor.PreviewHealth) ||
                world?.AutomaticLifecycle != true || world.Preview != room.Preview || world.Rooms.Count != 4 ||
                world.Rooms.Where((floor, index) => floor.RoomIndex != index || floor.Preview != room.Preview || floor.ExternalInteractions.Count > 0).Any()) return false;
            CombatUnit[] units = world.Rooms.SelectMany(floor => floor.Units).ToArray();
            int livingPyres = units.Count(unit => unit.IsPyre && unit.Health > 0);
            if (units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && !SupportsAlly(unit)) ||
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
                    index != 1 && floor != floors[index] || index == 1 && (floor > 2 || unit.Health == 1) ||
                    index >= 2 && (unit.BumpRules?.OuterBoss != true || unit.BumpRules.OuterBossRelentlessPhase ||
                        unit.BumpRules.CastingAnchorSlots?.Count != 4 || unit.BumpRules.RelentlessAnchorSlots?.Count != 4)) return false;
            }
            return true;
        }
    }
}
