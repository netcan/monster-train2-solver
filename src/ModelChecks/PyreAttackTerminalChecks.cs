using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreAttackTerminalChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Terminal Pyre attack evidence has nonzero " + name);
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().Where(row =>
            row.GetProperty("Kind").GetString() == "Exchange").ToArray();
        bool fatal = false, ordinary = false;
        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit? attacker = before.Units.FirstOrDefault(unit => unit.AssetKey == "AttackerT3_CorruptionAdder" &&
                unit.Triggers.Any(trigger => trigger.Kind == "OnAttacking" && !trigger.HasTriggered));
            if (attacker == null) continue;
            CombatUnit? observed = actual.Units.FirstOrDefault(unit => unit.Id == attacker.Id);
            if (observed == null) continue;
            CombatTrigger trigger = observed.Triggers.Single(value => value.Kind == "OnAttacking");
            ordinary |= trigger.HasTriggered;
            if (before.Units.Any(unit => unit.IsPyre && unit.Health > 0) && !actual.Units.Any(unit => unit.IsPyre))
            {
                Require(!trigger.HasTriggered, "Native lethal Pyre damage must stop before the attacker's post-hit callback.");
                fatal = true;
            }
        }
        Require(fatal && ordinary && !root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Expected both a normal original corruption attack and a lethal Pyre attack with its callback suppressed.");
        Console.WriteLine("PYRE-ATTACK-TERMINAL PASS: original corruption attacker fires after ordinary hits; lethal Pyre damage stops before its post-hit callback in a complete native loss, with four zero failure counters.");
    }
}
