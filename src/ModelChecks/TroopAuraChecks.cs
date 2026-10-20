using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopAuraChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("54a24002-b7b2-4e1c-967b-59d40b67231c")) return;
        int births = 0, increases = 0, decreases = 0, departures = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit? owner = after.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.AssetKey == "MonsterSquadLeader");
            if (owner == null) continue;
            TroopAuraState rule = owner.Modifiers!.TroopAura!;
            Require(rule.Upgrade.Stats.Damage == 2 && rule.Statuses is [{ Id: "trample", Stacks: 1 }], "Missing original Squad Leader upgrade/status definition.");
            string identity = "troop-aura:" + owner.Id;
            foreach (RoomCombatState room in after.Spawn.Train.Rooms)
            foreach (CombatUnit unit in room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(rule.Subtype)))
            {
                int stacks = unit.Status("horde")?.Stacks ?? 0;
                CardUpgradeModifier[] upgrades = unit.Modifiers!.Upgrades.Where(upgrade => upgrade.DataId == identity).ToArray();
                bool inRange = room.RoomIndex == 0;
                Require(upgrades.Length == (inRange ? stacks : 0) && upgrades.All(upgrade => upgrade.Stats.Damage == 2) &&
                    (unit.Status("trample")?.Stacks ?? 0) == (inRange ? 1 : 0) && rule.Targets.Contains(unit.Id) == inRange,
                    "Squad Leader must apply two attack per troop and one Trample, retain owner identity and clean departures.");
                RoomCombatState? oldRoom = before.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(old => old.Id == unit.Id));
                CombatUnit? old = oldRoom?.Units.Single(old => old.Id == unit.Id);
                if (old == null && inRange) births++;
                if (old != null && inRange && stacks > (old.Status("horde")?.Stacks ?? 0)) increases++;
                if (old != null && inRange && stacks < (old.Status("horde")?.Stacks ?? 0)) decreases++;
                if (oldRoom?.RoomIndex == 0 && !inRange) departures++;
            }
        }
        Require(births > 0 && increases > 0 && decreases > 0 && departures > 0, "Missing troop aura birth/growth/loss/movement witnesses.");
        Console.WriteLine($"ORIGINAL-TROOP-AURA PASS: {births} births, {increases} troop increases, {decreases} troop losses and {departures} departures; per-troop attack, single Trample and owner identities.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
