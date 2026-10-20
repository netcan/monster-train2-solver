using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConditionalUnitUpgradeChecks
{
    internal static void Native(FixtureValue root, string[] originals)
    {
        const string greed = "0060b286-4716-49e0-9714-3fd7811b9f72", shade = "a99ae26b-3ed8-4187-b027-46d07830fef1";
        foreach (string id in originals.Where(id => id == greed || id == shade))
        {
            FixtureValue action = root.GetProperty("Actions").EnumerateArray().Single(row => {
                BattleTurnState state = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                int card = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                return state.Spawn.Train.Context!.FindCard(card)!.DataId == id;
            });
            BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!, after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int spawner = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CombatUnit unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == spawner);
            if (id == greed)
            {
                int hoard = before.Spawn.Train.Context!.QueryFrame!.DragonsHoard!.Value;
                Require(hoard is 0 or 2 && unit.Attack == 20 + hoard * 10 && unit.MaxHealth == 35 + hoard * 10 &&
                    unit.Modifiers!.Upgrades.Count(upgrade => upgrade.Stats.Damage == 10 && upgrade.Stats.Health == 10) == hoard &&
                    unit.Triggers.Single(trigger => trigger.Kind == "OnSpawn").HasTriggered == (hoard > 0),
                    "Greed Dragon must apply one +10/+10 upgrade per Hoard and suppress its zero-Hoard trigger.");
                Console.WriteLine($"ORIGINAL-GREED-DRAGON PASS: native Hoard {hoard}, {unit.Attack}/{unit.MaxHealth} and {hoard} separate upgrades.");
                continue;
            }
            int positive = 0, control = 0;
            foreach (FixtureValue turn in root.GetProperty("Turns").EnumerateArray())
            {
                BattleTurnState prior = turn.GetProperty("Before").Deserialize<BattleTurnState>()!, next = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? oldUnit = prior.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == spawner),
                    newUnit = next.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == spawner);
                if (oldUnit == null || newUnit == null || next.Spawn.Train.Context!.AllScenarioBossesDead == true) continue;
                bool match = prior.Spawn.Train.Context!.QueryFrame!.MoonPhase == 1;
                Require(newUnit.Attack - oldUnit.Attack == (match ? 10 : 0) && newUnit.MaxHealth - oldUnit.MaxHealth == (match ? 15 : 0) &&
                    newUnit.Modifiers!.Upgrades.Count - oldUnit.Modifiers!.Upgrades.Count == (match ? 1 : 0),
                    "Shadeguard must apply its +10/+15 PostCombat upgrade only during the native New Moon phase.");
                if (match) positive++; else control++;
            }
            Require(positive > 0 && control > 0, "Shadeguard needs both matching and nonmatching Moon phases.");
            Console.WriteLine($"ORIGINAL-SHADEGUARD PASS: {positive} native phase-matched upgrades and {control} opposite-phase controls.");
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
