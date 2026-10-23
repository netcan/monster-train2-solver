using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ArkionPyreChecks
{
    private const string Boss = "Boss_Level3_Arkion_DamagePyre";
    internal static void Input(FixtureValue fixture)
    {
        FixtureValue input = fixture.GetProperty("NativeRunInput"), route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == "Level3BattleHeavyHitter_PyreDamage" &&
            input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 2 &&
            input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("StartingLevel").GetInt32() == 2 &&
            input.GetProperty("BattlesWon").GetInt32() == 2 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("CompletedBattles").GetArrayLength() == 2 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            route.GetProperty("Kind").GetString() == "SelectOriginalEarlyOuterBossScenario" &&
            route.GetProperty("Boss").GetString() == Boss &&
            route.GetProperty("SelectedScenario").GetString() == "Level3BattleHeavyHitter_PyreDamage" &&
            route.GetProperty("Distance").GetInt32() == 2 && route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Arkion Pyre requires its original rank-two two-win checkpoint and explicit original route selection without asset/RNG edits.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(field).GetInt32() == 0, "Arkion Pyre native failure: " + field);
    }

    internal static void Birth(FixtureValue fixture)
    {
        OuterBossBirthChecks.Native(fixture);
        FixtureValue row = fixture.GetProperty("Births")[0];
        var definition = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit raw = definition.Definition.Actor.Unit;
        CombatUnit born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        Require(raw.AssetKey == Boss && raw.BaseAttack == 4 && raw.Health == 425 && raw.MaxHealth == 425 &&
            raw.Statuses.Single() is { Id: "multistrike", Stacks: 1 } &&
            created.BirthStats is { Attack: 0, Health: 200 } && created.BirthStats.CovenantIds.SequenceEqual(new[] {
                "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e" }) &&
            born.BaseAttack == raw.BaseAttack + created.BirthStats.Attack &&
            born.Health == raw.Health + created.BirthStats.Health && born.MaxHealth == born.Health &&
            born.BumpRules is { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 } && born.EndsBattleOnDeath &&
            actual.Boss.Actions[0][0].Behavior == "RandomBossActionBehavior",
            "Arkion Pyre birth changed authored stats, multistrike, original owners, boss death routing or random-room targeting.");
        Trigger(born);
        Console.WriteLine($"ARKION-PYRE-BIRTH PASS: raw4/425, native {born.BaseAttack}/{born.Health}, exact multistrike1, damage5, original random-room controller, complete state/RNG.");
    }

    internal static void Battle(FixtureValue fixture, string path)
    {
        Input(fixture);
        int positive = 0;
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            if (stage.GetProperty("Kind").GetString() != "Room" ||
                !stage.TryGetProperty("ExternalPyre", out FixtureValue pyre) || pyre.ValueKind != FixtureKind.Object) continue;
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? boss = before.Units.SingleOrDefault(unit => unit.AssetKey == Boss && unit.Health > 0);
            if (boss == null || before.Units.Any(unit => unit.Team == CombatTeam.Player)) continue;
            Trigger(boss);
            int damage = pyre.GetProperty("Health").GetInt32() - stage.GetProperty("ActualExternalPyre").GetProperty("Health").GetInt32();
            int eligible = before.Units.Count(unit => unit.Health > 0 && unit.Triggers.Any(trigger =>
                trigger.Kind == "PostCombat" && trigger.Effects.Any(effect => effect.Type == "CardEffectPyreDamageIfNoPlayerUnits")));
            Require(damage == Math.Min(pyre.GetProperty("Health").GetInt32(), 5 * eligible),
                "Unblocked Arkion room did not apply its exact original remote Pyre damage.");
            if (damage > 0) positive++;
        }
        Require(positive > 0, "Arkion Pyre baseline needs positive native remote damage.");
        BattleTurnChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"ARKION-PYRE-BATTLE PASS: {positive} positive remote-Pyre stages, complete native rooms/train/EndTurns/RNG and sixteen immutable full chains.");
    }

    private static void Trigger(CombatUnit boss)
    {
        CombatEffect effect = boss.Triggers.Single(trigger => trigger.Kind == "PostCombat").Effects.Single();
        Require(effect.Type == "CardEffectPyreDamageIfNoPlayerUnits" && effect.Action is
            { Type: "Damage", Target: "Pyre", Value: 5, Range: null, UseCardTraits: false },
            "Arkion Pyre original conditional damage signature changed.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
