using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomAttachmentBirthChecks
{
    // Failed traces remain observations, never accepted historical forecasts.
    internal static void Observed(FixtureValue fixture)
    {
        Require(fixture.GetProperty("CaptureFailures").GetInt32() == 0, "Missing pure native action captures.");
        int witnesses = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState root = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int[] existing = root.Spawn.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToArray();
            CombatUnit? born = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit =>
                !existing.Contains(unit.Id) && unit.Modifiers?.Upgrades.Any(upgrade => upgrade.AssetKey == "StewardsQuarters") == true &&
                unit.Status("endless")?.Stacks == 1 && unit.Status("ambush")?.Stacks == 1 && unit.Status("multistrike")?.Stacks == 1);
            if (born == null) continue;
            Require(root.Spawn.Train.Context?.Enchantments != null, "Missing enchantment world birth interaction.");
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            using var keys = new BattleStateKeys();
            string original = keys.Key(root);
            BattleActionResult next = BattleActionModel.PlayCard(root, action);
            Require(next.Supported, next.Reason ?? "Unsupported attachment birth.");
            string predicted = BattleTurnChecks.Comparable(next.State!);
            string captured = BattleTurnChecks.Comparable(actual);
            Require(next.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() && predicted == captured,
                "Attachment birth differs from the complete native successor: " + ModelJson.Difference(predicted, captured));
            CombatUnit modeled = next.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == born.Id);
            Require(modeled.Attacks == 2 && modeled.StatusDictionary!.Slots.SequenceEqual(born.StatusDictionary!.Slots),
                "Birth lost attachment multistrike or native status insertion order.");
            Parallel.For(0, 16, _ =>
            {
                BattleActionResult branch = BattleActionModel.PlayCard(root, action);
                Require(branch.Supported && BattleTurnChecks.Comparable(branch.State!) == captured,
                    "Independent attachment birth branch differs.");
            });
            using var after = new BattleStateKeys();
            Require(after.Key(root) == original, "Attachment birth mutated its captured root.");
            witnesses++;
        }
        Require(witnesses > 0, "No native Endless/Quick/StewardsQuarters multistrike birth witness.");
        Console.WriteLine($"ROOM-ATTACHMENT-BIRTH PASS: {witnesses} observed native births, complete successor/state/RNG, correct status order and 16 immutable branches; historical plans/counters are not accepted.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
