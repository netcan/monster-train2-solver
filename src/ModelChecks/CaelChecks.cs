using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CaelChecks
{
    private static string Boss(string variant) => variant is "AtkHP" or "DamageShield" or "Lifesteal"
        ? "Boss_Level6_Cael_" + variant : throw new InvalidOperationException("Unknown original Cael variant.");

    internal static void Input(FixtureValue fixture, string variant)
    {
        string boss = Boss(variant), scenario = "Level6BattleEater" + variant;
        FixtureValue input = fixture.GetProperty("NativeRunInput"), route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == scenario && input.GetProperty("Seed").GetInt32() == 424242 &&
            input.GetProperty("Distance").GetInt32() == 5 && input.GetProperty("AscensionLevel").GetInt32() == 2 &&
            input.GetProperty("StartingLevel").GetInt32() == 2 && input.GetProperty("BattlesWon").GetInt32() == 5 &&
            !input.GetProperty("WasDevCheatUsed").GetBoolean() && input.GetProperty("CompletedBattles").GetArrayLength() == 5 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            route.GetProperty("Kind").GetString() == "SelectOriginalEarlyOuterBossScenario" &&
            route.GetProperty("Boss").GetString() == boss && route.GetProperty("SelectedScenario").GetString() == scenario &&
            route.GetProperty("Distance").GetInt32() == 5 && route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Cael calibration changed the original rank-two five-win input, selected scenario, authored assets or RNG.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(field).GetInt32() == 0, "Cael native battle failed: " + field);
    }

    internal static void Birth(FixtureValue fixture, string variant)
    {
        OuterBossBirthChecks.Native(fixture);
        FixtureValue row = fixture.GetProperty("Births")[0];
        var definition = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        Require(definition.Definition.Actor.Unit.AssetKey == Boss(variant) &&
            definition.Definition.Actor.Unit.BaseAttack == 22 && definition.Definition.Actor.Unit.MaxHealth == 1800 &&
            created.BirthStats is { Attack: 0, Health: 700 } && created.BirthStats.CovenantIds.SequenceEqual(new[] {
                "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e" }) &&
            born.BaseAttack == 22 && born.Health == 2500 && born.MaxHealth == 2500 && born.EndsBattleOnDeath &&
            born.BumpRules is { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 },
            "Cael birth changed authored stats, native +700 HP, original owners, boss identity or terminal death routing.");
        Upgrade(born, variant);
        Console.WriteLine($"CAEL-BIRTH PASS {variant}: raw22/1800, native rank-two22/2500, exact original upgrade and complete creation/placement/controller/RNG.");
    }

    internal static void Battle(FixtureValue fixture, string path, string variant)
    {
        Input(fixture, variant);
        int upgrades = 0;
        foreach (FixtureValue row in fixture.GetProperty("TrainPhases").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Combat") continue;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit? boss = before.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.AssetKey == Boss(variant));
            if (boss == null) continue;
            CardUpgradeModifier upgrade = Upgrade(boss, variant).Upgrade!;
            foreach (CombatUnit unit in before.Rooms.SelectMany(room => room.Units))
            {
                CombatUnit? next = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(value => value.Id == unit.Id);
                if (next == null) continue;
                int delta = (next.Modifiers?.Upgrades.Count(value => value.DataId == upgrade.DataId) ?? 0) -
                    (unit.Modifiers?.Upgrades.Count(value => value.DataId == upgrade.DataId) ?? 0);
                if (delta <= 0) continue;
                Require(unit.Team == CombatTeam.Enemy && next.MaxHealth == unit.MaxHealth + upgrade.Stats.Health * delta &&
                    upgrade.Statuses.All(status => (next.Status(status.Id)?.Stacks ?? 0) ==
                        (unit.Status(status.Id)?.Stacks ?? 0) + status.Stacks * delta),
                    "Cael native enemy upgrade changed its health/status increments.");
                upgrades += delta;
            }
        }
        Require(upgrades > 0, "Cael baseline needs a positive original enemy-upgrade witness.");
        BattleTurnChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"CAEL-BATTLE PASS {variant}: {upgrades} original enemy upgrades, settled no-card battle, complete rooms/train/turns/RNG and sixteen immutable full-battle branches.");
    }

    private static CardActionEffect Upgrade(CombatUnit boss, string variant)
    {
        CombatEffect effect = boss.Triggers.Single(trigger => trigger.Kind == "PostCombat").Effects.Single();
        CardActionEffect action = effect.UnitUpgrade!;
        Require(effect.Type == "CardEffectAddTempCardUpgradeToUnits" && action is
            { Type: "UnitUpgrade", Target: "FrontInRoom", AllowEnemy: true, AllowPlayer: false,
                Lifetime: "TemporaryUntilEndOfBattle", UseCardTraits: false } && action.Filters?.IgnoreBosses == true &&
            action.Upgrade?.AssetKey == "Boss_Level6_Feed" + variant + "Upgrade" && action.Upgrade.ExternalInteractions.Count == 0,
            "Cael native PostCombat enemy upgrade signature changed.");
        if (variant == "AtkHP") Require(action.Upgrade!.Stats.Health == 35 &&
            action.Upgrade.Statuses.Single() is { Id: "spikes", Stacks: 4 }, "Original Cael AtkHP payload changed.");
        else Require(action.Upgrade!.Stats.Health == 0 && action.Upgrade.Statuses.Single().Stacks == 1 &&
            action.Upgrade.Statuses.Single().Id == (variant == "DamageShield" ? "damage shield" : "lifesteal"),
            "Original Cael shield/lifesteal payload changed.");
        return action;
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
