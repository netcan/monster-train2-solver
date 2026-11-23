using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpellDamageMoveChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (fixture.GetProperty("Schema").GetInt32() != 1 ||
            fixture.GetProperty("Boundary").GetString() != "NativeMoveUpOnSpellDamage" ||
            fixture.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            fixture.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module ||
            fixture.GetProperty("OwnerId").GetString() != RelicSpellDamageMoveModel.OwnerId ||
            fixture.GetProperty("OwnerKey").GetString() != RelicSpellDamageMoveModel.OwnerKey ||
            fixture.GetProperty("Chance").GetInt32() != RelicSpellDamageMoveModel.Chance ||
            fixture.GetProperty("Bump").GetInt32() != RelicSpellDamageMoveModel.BumpAmount)
            throw new InvalidDataException("Expected the original 2.3.1 spell-damage movement component.");

        FixtureValue[] cases = fixture.GetProperty("Cases").EnumerateArray().ToArray();
        string[] kinds = ["Survivor", "NonDamage", "Lethal"];
        if (!cases.Select(row => row.GetProperty("Kind").GetString()).SequenceEqual(kinds))
            throw new InvalidDataException("Expected separate surviving, non-damage and lethal paid casts.");
        int survivorId = 0;
        for (int index = 0; index < cases.Length; index++)
        {
            FixtureValue row = cases[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("After").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            int target = action.TargetUnitId;
            if (index == 0) survivorId = target;
            if (index == 1 && target != survivorId || index == 2 && target == survivorId ||
                before.Spawn.Train.Context?.Relics?.Count(RelicSpellDamageMoveModel.IsOriginal) != 1)
                throw new InvalidDataException("Original cast owner or target control differs.");
            bool moved = index == 0;
            FixtureValue[] tests = row.GetProperty("Tests").EnumerateArray().ToArray();
            if (tests.Length == 0 || tests.Any(test => !test.GetProperty("RngUnchanged").GetBoolean()) ||
                tests.Any(test => test.GetProperty("Result").GetBoolean()) != moved ||
                moved && !tests.Any(test => test.GetProperty("Result").GetBoolean() &&
                    test.GetProperty("Targets").EnumerateArray().Select(id => id.GetInt32()).SequenceEqual([target])) ||
                row.GetProperty("Applications").GetInt32() != (moved ? 1 : 0))
                throw new InvalidDataException("Original ordered native tests or application count differs for " + kinds[index]);
            int[] actualRooms = after.Spawn.Train.Rooms.Where(room => room.Units.Any(unit => unit.Id == target && unit.Health > 0))
                .Select(room => room.RoomIndex).ToArray();
            if (!actualRooms.SequenceEqual(index == 2 ? [] : new[] { action.RoomIndex + (moved ? 1 : 0) }))
                throw new InvalidDataException("Original target floor or lethal removal differs for " + kinds[index]);
            if (moved)
            {
                CombatUnit prior = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
                    .Units.Single(unit => unit.Id == target);
                CombatUnit settled = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == actualRooms[0])
                    .Units.Single(unit => unit.Id == target);
                if (prior.Health != settled.Health || (prior.Status("armor")?.Stacks ?? 0) - (settled.Status("armor")?.Stacks ?? 0) != 3)
                    throw new InvalidDataException("Expected an original three-point Armor-only hit.");
            }
            BattleActionResult model = BattleActionModel.PlayCard(before, action);
            if (!model.Supported || model.State == null)
                throw new InvalidDataException(kinds[index] + " model transition failed: " + model.Reason);
            RecordedBattleChecks.Compare(model.State, after, terminal: false, kinds[index]);
        }
        Console.WriteLine("NATIVE-SPELL-DAMAGE-MOVE PASS three paid successors, exact native callback targeting and Battle RNG.");
    }
}
