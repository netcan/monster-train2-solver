using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ArkionPushChecks
{
    private const string Boss = "Boss_Level3_Arkion_PushAttack";
    internal static void Input(FixtureValue fixture)
    {
        FixtureValue input = fixture.GetProperty("NativeRunInput"), route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == "Level3BattleHeavyHitter_PushAttack" &&
            input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 2 &&
            input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("StartingLevel").GetInt32() == 2 &&
            input.GetProperty("BattlesWon").GetInt32() == 2 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("CompletedBattles").GetArrayLength() == 2 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            route.GetProperty("Kind").GetString() == "SelectOriginalEarlyOuterBossScenario" &&
            route.GetProperty("Boss").GetString() == Boss &&
            route.GetProperty("SelectedScenario").GetString() == "Level3BattleHeavyHitter_PushAttack" &&
            route.GetProperty("Distance").GetInt32() == 2 && route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Arkion Push requires its original rank-two two-win checkpoint and selected original encounter without asset/RNG edits.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(field).GetInt32() == 0, "Arkion Push native failure: " + field);
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
        Require(raw.AssetKey == Boss && raw.BaseAttack == 13 && raw.Health == 425 && raw.MaxHealth == 425 && raw.Statuses.Count == 0 &&
            created.BirthStats is { Attack: 0, Health: 200 } && created.BirthStats.CovenantIds.SequenceEqual(new[] {
                "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e" }) &&
            born.BaseAttack == 13 && born.Health == 625 && born.MaxHealth == 625 && born.EndsBattleOnDeath &&
            born.BumpRules is { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 } &&
            actual.Boss.Actions[0][0].Behavior == "RandomOccupiedBossActionBehavior",
            "Arkion Push birth changed original stats, Covenant owners, single-boss death routing or controller.");
        Trigger(born);
        Console.WriteLine("ARKION-PUSH-BIRTH PASS: raw13/425, native13/625, exact original attack-before-damage/back targeting and complete creation/placement/controller/RNG.");
    }
    internal static void Battle(FixtureValue fixture, string path, bool requirePush)
    {
        Input(fixture);
        int positive = 0;
        foreach (FixtureValue row in fixture.GetProperty("Stages").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Exchange") continue;
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit[] actual = row.GetProperty("Actual").GetProperty("Units").Deserialize<CombatUnit[]>()!;
            CombatUnit? boss = before.Units.SingleOrDefault(unit => unit.AssetKey == Boss);
            CombatUnit[] players = before.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).ToArray();
            if (boss == null || players.Length < 2) continue;
            CombatUnit[] afterPlayers = actual.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).ToArray();
            CombatUnit? victim = afterPlayers.SingleOrDefault(unit => unit.Id == players[0].Id);
            if (afterPlayers.Length < 2 || victim == null || afterPlayers[^1].Id != victim.Id ||
                victim.Health >= players[0].Health || victim.LastAttackerId != boss.Id) continue;
            Trigger(boss);
            Require(victim.Status("dazed")?.Stacks == players[0].Status("dazed")?.Stacks,
                "FloorRearrange must not apply its stale authored dazed editor field.");
            positive++;
        }
        Require(!requirePush || positive > 0, "Arkion Push needs a positive native surviving original victim moved to the back and then hit by the boss.");
        BattleTurnChecks.Native(fixture);
        if (requirePush) BattleActionChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"ARKION-PUSH-BATTLE PASS: pushRequired={requirePush}, {positive} original-victim back/hit witnesses and complete native state/RNG.");
    }
    private static void Trigger(CombatUnit boss)
    {
        CombatEffect effect = boss.Triggers.Single(trigger => trigger.Kind == "OnAttackingBeforeDamage").Effects.Single();
        Require(effect.Type == "CardEffectFloorRearrange" && effect.Action is
            { Type: "FloorRearrange", Target: "LastAttackedCharacter", Value: 1, AllowEnemy: true, AllowPlayer: true,
                Range: null, UseCardTraits: false } && effect.Action.Statuses.Count == 0,
            "Original Arkion attacked-target/back rearrangement signature changed.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
