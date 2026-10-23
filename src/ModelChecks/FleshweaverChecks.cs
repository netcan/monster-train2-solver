using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FleshweaverChecks
{
    internal static string Variant(string variant) => variant switch
    { "Mass" => "MoreMass", "Corruption" => "Corruption", "Spikes" => "Spike", _ => throw new ArgumentException("Unknown original Fleshweaver variant.") };
    internal static void Input(FixtureValue fixture, string variant)
    {
        string suffix = Variant(variant), scenario = "Level3ImmortalMass_" + (suffix == "Spike" ? "Spikes" : suffix);
        FixtureValue input = fixture.GetProperty("NativeRunInput"), route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == scenario && input.GetProperty("Seed").GetInt32() == 424242 &&
            input.GetProperty("Distance").GetInt32() == 2 && input.GetProperty("AscensionLevel").GetInt32() == 2 &&
            input.GetProperty("StartingLevel").GetInt32() == 2 && input.GetProperty("BattlesWon").GetInt32() == 2 &&
            !input.GetProperty("WasDevCheatUsed").GetBoolean() && input.GetProperty("CompletedBattles").GetArrayLength() == 2 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            route.GetProperty("Kind").GetString() == "SelectOriginalEarlyOuterBossScenario" &&
            route.GetProperty("Boss").GetString() == "Boss_Level3_Fleshweaver_" + suffix &&
            route.GetProperty("SelectedScenario").GetString() == scenario && route.GetProperty("Distance").GetInt32() == 2 &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Fleshweaver requires the original selected rank-two two-win checkpoint without asset/RNG edits.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(field).GetInt32() == 0, "Fleshweaver native failure: " + field);
    }
    internal static void Birth(FixtureValue fixture, string variant)
    {
        OuterBossBirthChecks.Native(fixture);
        string suffix = Variant(variant);
        FixtureValue row = fixture.GetProperty("Births")[0];
        var definition = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit raw = definition.Definition.Actor.Unit;
        CombatUnit born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        int health = suffix == "MoreMass" ? 600 : 500, mass = suffix == "MoreMass" ? 8 : 5;
        Require(raw.AssetKey == "Boss_Level3_Fleshweaver_" + suffix && raw.BaseAttack == 6 && raw.Health == health &&
            raw.MaxHealth == health && raw.Statuses.Single() is { Id: "mass", ParamInt: 2 } status && status.Stacks == mass &&
            created.BirthStats is { Attack: 1, Health: 180 } stats &&
            stats.CovenantIds.SequenceEqual(new[] { "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e" }) &&
            born.BaseAttack == 7 && born.Health == health + 180 && born.MaxHealth == born.Health && born.Attack == 7 + mass * 2 &&
            born.Status("mass")?.Stacks == mass && born.EndsBattleOnDeath &&
            born.BumpRules is { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 } &&
            actual.Boss.Actions[0][0].Behavior == "RandomOccupiedBossActionBehavior",
            "Fleshweaver original stats, Mass attack, native Covenant adjustment, death routing or controller changed.");
        Trigger(born, suffix);
        Console.WriteLine($"FLESHWEAVER-BIRTH PASS {variant}: raw6/{health}/Mass{mass}, native7/{born.Health}/attack{born.Attack}, original trigger and complete creation/placement/controller/RNG.");
    }
    internal static void Battle(FixtureValue fixture, string path, string variant, bool requireBirth)
    {
        Input(fixture, variant);
        string suffix = Variant(variant), asset = "Boss_Level3_Fleshweaver_" + suffix, mini = "Boss_Level3_MiniFleshweaver_" + suffix;
        int positive = 0, massRemoved = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Observe(before.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray(), actual.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray());
        }
        foreach (FixtureValue row in fixture.GetProperty("Stages").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Exchange") continue;
            Observe(row.GetProperty("Before").Deserialize<RoomCombatState>()!.Units,
                row.GetProperty("Actual").GetProperty("Units").Deserialize<CombatUnit[]>()!);
        }
        Require(!requireBirth || positive > 0 && massRemoved > 0,
            "Fleshweaver requires a positive legal native hit, enemy mini birth and Mass removal witness.");
        BattleTurnChecks.Native(fixture);
        if (requireBirth) BattleActionChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"FLESHWEAVER-BATTLE PASS {variant}: positiveRequired={requireBirth}, {positive} surviving mini births/{massRemoved} Mass removed, complete native state/RNG.");

        void Observe(IReadOnlyList<CombatUnit> before, IReadOnlyList<CombatUnit> after)
        {
            CombatUnit? boss = before.SingleOrDefault(unit => unit.AssetKey == asset);
            CombatUnit? next = after.SingleOrDefault(unit => unit.Id == boss?.Id);
            if (boss == null || next == null) return;
            int delta = (boss.Status("mass")?.Stacks ?? 0) - (next.Status("mass")?.Stacks ?? 0);
            if (delta <= 0) return;
            Trigger(boss, suffix);
            Require(next.BaseAttack == boss.BaseAttack && next.AttackFromMass() == boss.AttackFromMass() - 2 * delta,
                "Mass removal must reduce its original attack contribution by two per stack without changing base attack.");
            massRemoved += delta;
            foreach (CombatUnit born in after.Where(unit => unit.AssetKey == mini && !before.Any(old => old.Id == unit.Id)))
            {
                Require(born.Team == CombatTeam.Enemy && born.SpawnerCardId == 0 && born.BumpRules?.OuterBoss == false &&
                    born.Status("rooted")?.Stacks == 1 && !born.EndsBattleOnDeath,
                    "Original mini birth changed its enemy team, physical actor, rooted status or death ownership.");
                positive++;
            }
        }
    }
    private static int AttackFromMass(this CombatUnit unit) => (unit.Status("mass")?.Stacks ?? 0) * (unit.Status("mass")?.ParamInt ?? 0);
    private static void Trigger(CombatUnit boss, string suffix)
    {
        CombatTrigger trigger = boss.Triggers.Single(value => value.Kind == "OnHit");
        Require(trigger.Conditions!.RequiredStatuses.SequenceEqual(new[] { "mass" }) && trigger.Effects.Count == 2 &&
            trigger.Effects[0] is { Type: "CardEffectSpawnHero", Summon.EnemyHero: true } &&
            trigger.Effects[1].Action is { Type: "RemoveStatus", Target: "Self" } removal &&
            removal.Statuses.Single() is { Id: "mass", Stacks: 1 }, "Original Fleshweaver hit/birth/Mass signature changed: " + suffix);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
