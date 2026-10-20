using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitRoomStatusChecks
{
    internal static void Native(FixtureValue root, string[] originals)
    {
        var requested = new Dictionary<string, string> {
            ["3cb0976c-7cf5-4470-bb5a-7eacfb404e9a"] = "buff",
            ["1362aa8c-d7b4-49b9-a4f7-268f47de4aa6"] = "spikes",
            ["a5b05b40-45be-41ea-8c56-dd18f3feb1b4"] = "valor" };
        string[] statuses = requested.Where(item => originals.Contains(item.Key)).Select(item => item.Value).ToArray();
        if (statuses.Length == 0) return;
        Require(root.TryGetProperty("UnitRoomStatusQueries", out FixtureValue records) && records.GetArrayLength() > 0,
            "Room status modifiers require independent native attack and room bonus queries.");
        var positive = new HashSet<string>();
        bool enemySpikesControl = false;
        bool weldlingArmor = false;
        int count = 0;
        foreach (FixtureValue row in records.EnumerateArray())
        {
            RoomCombatState room = row.GetProperty("Room").Deserialize<RoomCombatState>()!;
            foreach (FixtureValue query in row.GetProperty("Queries").EnumerateArray())
            {
                CombatUnit unit = room.Units.Single(unit => unit.Id == query.GetProperty("UnitId").GetInt32());
                int Bonus(string id) => UnitRoomStatusModel.Modifier(room.Units, unit.Team, id) * (unit.Status(id)?.Stacks ?? 0);
                int buff = Bonus("buff"), valor = Bonus("valor"), spikes = Bonus("spikes");
                Require(query.GetProperty("BuffBonus").GetInt32() == buff && query.GetProperty("ValorBonus").GetInt32() == valor &&
                    query.GetProperty("SpikesBonus").GetInt32() == spikes,
                    "Native room bonuses differ from current live owners, status stacks or target team.");
                int attack = UnitAttackOverrideModel.Resolve(unit.Statuses, unit.MaxHealth) ?? unchecked(unit.Attack + buff + valor);
                Require(query.GetProperty("NativeAttack").GetInt32() == attack,
                    "Native attack differs from its status attack and room aura contributions.");
                if (buff > 0) positive.Add("buff");
                if (valor > 0) positive.Add("valor");
                if (spikes > 0) positive.Add("spikes");
                weldlingArmor |= unit.AssetKey == "MonsterWeldling" && buff > 0 && unit.Status("armor")?.Stacks == 13 && attack == 26;
                enemySpikesControl |= unit.Team == CombatTeam.Enemy && unit.Status("spikes")?.Stacks > 0 && spikes == 0 &&
                    room.Units.Any(owner => owner.Team == CombatTeam.Player && owner.Modifiers?.RoomStatuses?.Any(rule => rule.StatusId == "spikes") == true);
                count++;
            }
        }
        Require(statuses.All(positive.Contains) && (!statuses.Contains("spikes") || enemySpikesControl),
            "Each original aura needs a positive native status bonus; Shard Channeler also needs an opposing-team control.");
        if (originals.Contains("acb9a1cb-f6fc-40df-b287-fc82b8df156b"))
            Require(weldlingArmor, "Weldling must survive and convert its room-modified 26 attack into thirteen Armor after Rage decay.");
        Console.WriteLine($"UNIT-ROOM-STATUS-QUERIES PASS: {count} native attacks and room bonuses; positive {string.Join(',', statuses)}.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
