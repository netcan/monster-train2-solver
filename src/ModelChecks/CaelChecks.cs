using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CaelChecks
{
    private const string TrialMvid = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
    private const string TrialScenario = "Level6BattleEaterDamageShield";
    private const string TrialFeeder = "Boss_Level6_FeedDamageShield_TitanTrial";
    private const string TrialUpgrade = "067b599f-ac23-4c99-8841-192fc5b7dd74";

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

    internal static void ShieldTrial(FixtureValue substitution, FixtureValue birth)
    {
        FixtureValue intro = substitution.GetProperty("Intro");
        Require(substitution.GetProperty("GameVersion").GetString() == "2.3.1" &&
            substitution.GetProperty("GameModuleMvid").GetString() == TrialMvid &&
            substitution.GetProperty("Boundary").GetString() == "NativeOriginalEarlyTitanTrialSubstitution" &&
            substitution.GetProperty("Scenario").GetString() == TrialScenario &&
            substitution.GetProperty("OwnerId").GetString() == OuterBossDefinitions.CaelShieldTrialOwnerId &&
            substitution.GetProperty("OwnerKey").GetString() == OuterBossDefinitions.CaelShieldTrialOwner &&
            substitution.GetProperty("OwnerEffects").Deserialize<string[]>()!.SequenceEqual(["RelicEffectOnSpawnHeroSubstitution", "RelicEffectNull"]) &&
            substitution.GetProperty("AscensionLevel").GetInt32() == 0 &&
            substitution.GetProperty("Source").GetString() == Boss("DamageShield") &&
            substitution.GetProperty("SourceId").GetString() == "168b33b6-361b-4691-bc64-aa58c66eb5db" &&
            substitution.GetProperty("ReplacementId").GetString() == "3794c878-797d-4e81-8f0b-644869661970" &&
            substitution.GetProperty("Replacement").GetString() == OuterBossDefinitions.CaelShieldTrial &&
            substitution.GetProperty("OuterBoss").GetBoolean() && substitution.GetProperty("TitanTrial").GetBoolean() &&
            intro.GetProperty("EnabledAfter").GetBoolean() && intro.GetProperty("UiAfter").GetBoolean() &&
            intro.GetProperty("ChangedRngIds").EnumerateArray().All(stream => stream.GetString() == "NonDeterministic"),
            "Expected the installed rank-zero Cael Shield trial and active native substitution source.");
        FixtureValue projection = substitution.GetProperty("Projection");
        Require(projection.GetProperty("Key").GetString() == OuterBossDefinitions.CaelShieldTrial &&
            projection.GetProperty("ExternalInteractions").Deserialize<string[]>()!.SequenceEqual(["Spawned outer Boss actions"]) &&
            substitution.GetProperty("Unsupported").GetInt32() == 0 && substitution.GetProperty("Mismatches").GetInt32() == 0,
            "The original Cael Shield Trial still has a projection or execution deferral.");
        FixtureValue mappings = substitution.GetProperty("Mappings");
        Require(mappings.EnumerateArray().Count(map =>
            map.GetProperty("Source").GetString() == Boss("DamageShield") &&
            map.GetProperty("Replacement").GetString() == OuterBossDefinitions.CaelShieldTrial) == 1 &&
            mappings.EnumerateArray().Count(map => map.GetProperty("Source").GetString() == "Boss_Level6_FeedDamageShield" &&
                map.GetProperty("SourceId").GetString() == "43bbea4c-c1e8-4744-a4b1-e5b5f135c059" &&
                map.GetProperty("Replacement").GetString() == TrialFeeder &&
                map.GetProperty("ReplacementId").GetString() == "b29a1359-c39b-48d6-9649-1740667ed97e") == 1,
            "The installed table must contain both original Shield replacements.");

        FixtureValue row = birth.GetProperty("Births").EnumerateArray().Single();
        OuterBossInstantiationState before = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        OuterBossPhaseState placed = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit callback = substitution.GetProperty("Unit").Deserialize<CombatUnit>()!;
        CombatContext context = before.Battle.Spawn.Train.Context!;
        Require(callback.Id == placed.Boss.UnitId && callback.AssetKey == placed.Boss.AssetKey &&
            OuterBossDefinitions.CalibratedCaelShieldTrial(before.Definition.Actor.Unit, context) &&
            !OuterBossDefinitions.CalibratedCaelShieldTrial(before.Definition.Actor.Unit,
                context.WithRelics(context.Relics!.Where(owner => owner.DataId != OuterBossDefinitions.CaelShieldTrialOwnerId).ToArray())),
            "The captured replacement actor, exact owner requirement or native callback identity differs.");
        int selected = before.Battle.Spawn.SelectedGroups[0];
        EnemyDefinition food = before.Battle.Spawn.Waves[0].Candidates[selected].Units.Single(entry => entry.Unit.AssetKey == TrialFeeder);
        CombatUnit actualFood = placed.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == TrialFeeder);
        Require(food.ExternalInteractions.Count == 0 && food.BirthStats == null && food.Unit.Id == 0 && actualFood.Id > 0 &&
            actualFood.Id < callback.Id && Feeder(food.Unit) && Feeder(actualFood),
            "The original first wave must already contain the resolved Feed actor and ordered Shield/SelfDamage OnEaten.");
        OuterBossBirthChecks.Native(birth, TrialMvid);
        Console.WriteLine("CAEL-SHIELD-TRIAL PASS: original outer callback, resolved first wave and complete immutable placement, controller and RNG.");
    }

    private static bool Feeder(CombatUnit unit) =>
        unit is { Team: CombatTeam.Enemy, BaseAttack: 0, Attack: 0, Health: 60, MaxHealth: 60, Size: 1,
            EndsBattleOnDeath: false, BumpRules.OuterBoss: false, Statuses: [{ Id: "eatmany", Stacks: 3 }],
            Triggers: [{ Kind: "OnEaten", Effects: [
                { Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: {
                    Target: "LastFeederCharacter", Lifetime: "TemporaryUntilEndOfBattle", AllowEnemy: true, AllowPlayer: false,
                    Upgrade: { DataId: TrialUpgrade, Statuses: [{ Id: "damage shield", Stacks: 1 }, { Id: "spell shield", Stacks: 1 }] } } },
                { Type: "CardEffectDamage", Action: { Target: "Self", Value: 20 } }] }] };

    internal static void ShieldTrialBattle(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        FixtureValue route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == TrialScenario &&
            input.GetProperty("Distance").GetInt32() == 5 && input.GetProperty("AscensionLevel").GetInt32() == 0 &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            route.GetProperty("Boss").GetString() == Boss("DamageShield") &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            fixture.GetProperty("OriginalOuterBossTrialIntro").GetProperty("EnabledAfter").GetBoolean(),
            "Expected native rank-zero Cael Shield Trial from a five-win original checkpoint.");
        int upgrades = 0;
        int feedings = 0;
        var births = new HashSet<int>();
        foreach (FixtureValue row in fixture.GetProperty("TrainPhases").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Combat") continue;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit[] beforeUnits = before.Rooms.SelectMany(room => room.Units).ToArray();
            Dictionary<int, CombatUnit> actualUnits = actual.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
            if (beforeUnits.All(unit => unit.AssetKey != OuterBossDefinitions.CaelShieldTrial)) continue;
            foreach (CombatUnit old in beforeUnits)
            {
                if (old.AssetKey == TrialFeeder) births.Add(old.Id);
                if (!actualUnits.TryGetValue(old.Id, out CombatUnit? next)) continue;
                if (old.AssetKey == TrialFeeder && next.LastFeederId is int eaterId && eaterId > 0 &&
                    old.Health - next.Health == 20 && (old.Status("eatmany")?.Stacks ?? 0) - (next.Status("eatmany")?.Stacks ?? 0) == 1 &&
                    actualUnits.TryGetValue(eaterId, out CombatUnit? eater) &&
                    beforeUnits.SingleOrDefault(unit => unit.Id == eaterId) is { Team: CombatTeam.Enemy } oldEater &&
                    UpgradeCount(eater) > UpgradeCount(oldEater) &&
                    eater.Status("damage shield") is { Stacks: > 0 } && eater.Status("spell shield") is { Stacks: > 0 })
                    feedings++;
                int delta = UpgradeCount(next) - UpgradeCount(old);
                if (delta <= 0) continue;
                Require(old.Team == CombatTeam.Enemy, "The original Shield Trial's enemy upgrade reached a player unit.");
                if ((next.Status("damage shield")?.Stacks ?? 0) >= delta && (next.Status("spell shield")?.Stacks ?? 0) >= delta)
                    upgrades += delta;
            }
        }
        Require(births.Count > 0 && upgrades > 0 && feedings > 0,
            "Expected a native wave feeder, consumption and positive application of both shields.");
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine($"CAEL-SHIELD-TRIAL-BATTLE PASS: {births.Count} original wave feeders, {feedings} witnessed feedings and {upgrades} dual-shield upgrades.");

        static int UpgradeCount(CombatUnit unit) => unit.Modifiers?.Upgrades.Count(value => value.DataId == TrialUpgrade) ?? 0;
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
