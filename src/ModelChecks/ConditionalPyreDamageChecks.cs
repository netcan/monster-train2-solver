using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConditionalPyreDamageChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.Spawn.Turn == 0)
        {
            if (source.Spawn.Train.Rooms[1].Units.Any(unit => unit.Team == CombatTeam.Player)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.RoomIndex == 1 && source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId ==
                "d14a50f3-728d-43e1-87f0-ef1b013f6678");
        }
        if (source.Spawn.Turn == 1) return null;
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "conditional-pyre-damage" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Original conditional Pyre damage battle did not finish with a complete match.");
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 7, "Conditional Pyre battle did not record seven EndTurns.");
        BattleTurnState openBefore = turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState openAfter = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState blockedBefore = turns[2].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState blockedAfter = turns[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        EnemyDefinition[] original = openBefore.Spawn.Waves[0].Candidates.SelectMany(group => group.Units)
            .Where(unit => unit.Unit.AssetKey == "HeavyT3_PyreDamageIfUnblocked").ToArray();
        Require(original.Length > 0 && original.All(unit =>
        {
            CombatEffect effect = unit.Unit.Triggers.Single(trigger => trigger.Kind == "PostCombat").Effects.Single();
            return effect.Type == "CardEffectPyreDamageIfNoPlayerUnits" && effect.Action is
                { Type: "Damage", Target: "Pyre", Value: 5 };
        }), "Native wave did not retain the original conditional Pyre damage effect.");
        static int Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.IsPyre).Health;
        Require(Pyre(openBefore) == 80 && Pyre(openAfter) == 75 &&
            Pyre(blockedBefore) == 75 && Pyre(blockedAfter) == 75 &&
            openBefore.Spawn.Train.Rooms[0].Units.All(unit => unit.Team != CombatTeam.Player) &&
            blockedBefore.Spawn.Train.Rooms[1].Units.Any(unit => unit.Team == CombatTeam.Player),
            "Open and blocked floors did not distinguish conditional Pyre damage.");
        foreach ((BattleTurnState before, BattleTurnState actual) in new[]
            { (openBefore, openAfter), (blockedBefore, blockedAfter) })
        {
            string expected = BattleTurnChecks.Comparable(actual);
            Parallel.For(0, 16, _ =>
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(before);
                Require(result.Supported && BattleTurnChecks.Comparable(result.State!) == expected,
                    "Parallel conditional Pyre damage branches diverged.");
            });
        }
        FixtureValue[] remoteStages = fixture.GetProperty("Stages").EnumerateArray().Where(stage =>
            stage.TryGetProperty("ExternalPyre", out FixtureValue pyre) && pyre.ValueKind != FixtureKind.Null).ToArray();
        Require(remoteStages.Any(stage => stage.GetProperty("ExternalPyre").GetProperty("Health").GetInt32() == 80 &&
            stage.GetProperty("ActualExternalPyre").GetProperty("Health").GetInt32() == 75) &&
            remoteStages.Any(stage => stage.GetProperty("ExternalPyre").GetProperty("Health").GetInt32() == 75 &&
            stage.GetProperty("ActualExternalPyre").GetProperty("Health").GetInt32() == 75),
            "Single-room captures did not record both remote Pyre outcomes.");
        Console.WriteLine("NATIVE-CONDITIONAL-PYRE-DAMAGE-CHECKS PASS: original enemy, open/blocked floors, remote target and 32 parallel EndTurns.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
