using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerMoonPhaseChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        Require(source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean(),
            "Player moon-phase acceptance requires winning native runs.");
        var phases = new HashSet<string>();
        var spellPhases = new HashSet<string>();
        int shifts = 0, moonSpells = 0, units = 0;
        foreach (FixtureValue record in source.GetProperty("RollingDecisions").EnumerateArray())
        {
            BattleTurnState root = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(root.MoonPhaseTrackingActive == true && root.Spawn.Train.Context!.QueryFrame!.MoonPhaseTrackingActive == true,
                "Natural moon tracking was not captured.");
            Require(root.PlayRules!.Cards.Any(card => card.AssetKey == "LunaCovenChamp1") &&
                root.PlayRules.Cards.Any(card => card.AssetKey == "Witchweave"), "Missing native Luna Coven starter definitions.");
            phases.Add(root.MoonPhase);
            int phase = root.Spawn.Train.Context!.QueryFrame!.MoonPhase!.Value;
            foreach (CombatUnit unit in root.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => !unit.IsPyre && unit.Health > 0))
            {
                // Native None is a retained bit (4), not zero; the phase bits
                // occupy the low two bits. Compare both parts explicitly.
                Require((unit.MoonPhaseMask & 3) == phase && (unit.MoonPhaseMask & ~3) == 4 &&
                    unit.Status(phase == 1 ? "newmoon" : "fullmoon")?.Stacks == 1,
                    "Live unit moon mask/status differs from the captured phase.");
                units++;
            }
            if (record.GetProperty("IsEndTurn").GetBoolean() && actual.Spawn.Turn > root.Spawn.Turn)
            {
                Require(actual.MoonPhase != root.MoonPhase, "Native end turn did not advance the moon phase.");
                shifts++;
            }
            if (record.GetProperty("Action").ValueKind != FixtureKind.Null)
            {
                PlayCardAction action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState card = root.Spawn.Train.Context.FindCard(action.CardInstanceId)!;
                if (root.PlayRules.Cards.Any(rule => rule.DataId == card.DataId && rule.AssetKey == "Witchweave"))
                { moonSpells++; spellPhases.Add(root.MoonPhase); }
            }
        }
        Require(phases.SetEquals(new[] { "New", "Full" }) && spellPhases.SetEquals(phases) && shifts >= 2 && moonSpells > 0 && units > 0,
            "Native sequence did not exercise both phases, repeated shifts, living units and a moon-scaled starter spell.");
        Console.WriteLine($"PLAYER-MOON-PHASE PASS phases=2 shifts={shifts} moonSpells={moonSpells} liveUnitSnapshots={units}");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
