using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FleshweaverChecks
{
    private const string SpikeTrialScenario = "Level3ImmortalMass_Spikes";

    internal static void SpikeTrialSubstitution(FixtureValue fixture)
    {
        Require(fixture.GetProperty("GameVersion").GetString() == "2.3.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            fixture.GetProperty("Boundary").GetString() == "NativeOriginalEarlyTitanTrialSubstitution" &&
            fixture.GetProperty("Scenario").GetString() == SpikeTrialScenario &&
            fixture.GetProperty("OwnerId").GetString() == TriggeredSummonModel.FleshweaverSpikeTrialOwnerId &&
            fixture.GetProperty("OwnerKey").GetString() == TriggeredSummonModel.FleshweaverSpikeTrialOwnerKey &&
            fixture.GetProperty("TitanTrial").GetBoolean() && !fixture.GetProperty("OuterBoss").GetBoolean(),
            "Expected the installed Ring 3 Titan Trial summon witness.");
        const string source = "Boss_Level3_MiniFleshweaver_Spike";
        Require(fixture.GetProperty("Source").GetString() == source &&
            fixture.GetProperty("Replacement").GetString() == TriggeredSummonModel.FleshweaverSpikeTrialMini &&
            fixture.GetProperty("Mappings").EnumerateArray().Count(entry =>
                entry.GetProperty("Source").GetString() == source &&
                entry.GetProperty("Replacement").GetString() == TriggeredSummonModel.FleshweaverSpikeTrialMini) == 1,
            "Expected the original mini source and exact installed replacement.");
        FixtureValue intro = fixture.GetProperty("Intro");
        Require(intro.GetProperty("EnabledAfter").GetBoolean() && intro.GetProperty("UiAfter").GetBoolean() &&
            intro.GetProperty("ChangedRngIds").EnumerateArray().All(stream => stream.GetString() == "NonDeterministic"),
            "The original Trial toggle changed gameplay RNG.");

        CombatUnit mini = fixture.GetProperty("Unit").Deserialize<CombatUnit>()!;
        Require(mini is { Id: > 0, AssetKey: TriggeredSummonModel.FleshweaverSpikeTrialMini, Team: CombatTeam.Enemy,
            Health: 9, MaxHealth: 9, BaseAttack: 3, Attack: 3, Size: 1, SpawnerCardId: 0, EndsBattleOnDeath: false,
            IsBoss: false, BumpRules.OuterBoss: false, Triggers: [{ Kind: "OnSpawn", HasTriggered: true }] } &&
            mini.Statuses.Count == 1 && mini.Status("spikes")?.Stacks == 2 && mini.RegisteredStatus("rooted")?.Stacks == 0 &&
            mini.AbilityRules?.AuthoredStartingStatuses.Any(status => status.Id == "rooted" && status.Stacks == 1) == true,
            "The original replacement's completed spawn state differs.");
        Console.WriteLine("FLESHWEAVER-SPIKE-TRIAL-SUBSTITUTION PASS: native trial owner, exact source/replacement, Spikes2 and settled Rooted removal.");
    }

    internal static void TrialSubstitution(FixtureValue fixture, FixtureValue placement, string variant)
    {
        string suffix = Variant(variant);
        var identity = suffix switch
        {
            "Corruption" => ("52dc060c-bd51-4b7b-865a-6d70f9806e53", TriggeredSummonModel.FleshweaverCorruptionMiniId,
                "c05429e3-c6de-49fc-870c-6124fcc87076", 5),
            "MoreMass" => ("28c0e907-d882-4052-93f4-0a07d88496af", TriggeredSummonModel.FleshweaverMoreMassMiniId,
                "4159e8ac-faec-4dae-a55f-dde1320b7c5b", 7),
            _ => throw new ArgumentException("Expected Corruption or Mass trial evidence.")
        };
        (string ownerId, string sourceId, string replacementId, int stat) = identity;
        const string mvid = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
        string scenario = "Level3ImmortalMass_" + suffix;
        string source = "Boss_Level3_MiniFleshweaver_" + suffix;
        string replacement = source + "_TitanTrial";
        FixtureValue intro = fixture.GetProperty("Intro");
        Require(fixture.GetProperty("GameVersion").GetString() == "2.3.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == mvid &&
            fixture.GetProperty("Boundary").GetString() == "NativeOriginalEarlyTitanTrialSubstitution" &&
            fixture.GetProperty("Scenario").GetString() == scenario &&
            fixture.GetProperty("OwnerId").GetString() == ownerId &&
            fixture.GetProperty("OwnerKey").GetString() == "Enemy_Boss_Level3_ImmortalMass_" + suffix + "_TitanTrial" &&
            fixture.GetProperty("Source").GetString() == source && fixture.GetProperty("SourceId").GetString() == sourceId &&
            fixture.GetProperty("Replacement").GetString() == replacement && fixture.GetProperty("ReplacementId").GetString() == replacementId &&
            fixture.GetProperty("Mappings").EnumerateArray().Count(entry =>
                entry.GetProperty("Source").GetString() == source && entry.GetProperty("Replacement").GetString() == replacement) == 1 &&
            fixture.GetProperty("TitanTrial").GetBoolean() && !fixture.GetProperty("OuterBoss").GetBoolean() &&
            intro.GetProperty("EnabledAfter").GetBoolean() && intro.GetProperty("UiAfter").GetBoolean() &&
            intro.GetProperty("ChangedRngIds").EnumerateArray().All(stream => stream.GetString() == "NonDeterministic"),
            "Expected the native Trial input, exact active owner, source and resolved replacement.");

        CombatUnit mini = fixture.GetProperty("Unit").Deserialize<CombatUnit>()!;
        Require(mini is { Id: > 0, Team: CombatTeam.Enemy, Size: 1, SpawnerCardId: 0, EndsBattleOnDeath: false,
            IsBoss: false, BumpRules.OuterBoss: false, Statuses.Count: 0 } && mini.AssetKey == replacement &&
            mini.BaseAttack == stat && mini.Attack == stat && mini.Health == stat && mini.MaxHealth == stat &&
            mini.RegisteredStatus("rooted")?.Stacks == 0 &&
            mini.AbilityRules is { AuthoredStartingStatuses: [{ Id: "rooted", Stacks: 1 }] } &&
            mini.Triggers.FirstOrDefault() is { Kind: "OnSpawn", HasTriggered: true } &&
            mini.Triggers.Count == (suffix == "Corruption" ? 2 : 1),
            "The native birth must have the replacement's stats, settled Rooted and original callbacks.");

        Require(placement.GetProperty("GameModuleMvid").GetString() == mvid &&
            placement.GetProperty("Errors").GetArrayLength() == 0, "Expected the paired native placement sidecar.");
        FixtureValue nativePlacement = placement.GetProperty("Births").EnumerateArray().Single();
        Require(nativePlacement.GetProperty("NativeReturned").GetBoolean(), "The original outer boss placement failed.");
        BattleTurnState placed = nativePlacement.GetProperty("Actual").GetProperty("Battle").Deserialize<BattleTurnState>()!;
        RoomCombatState room = placed.Spawn.Train.Rooms.Single(candidate =>
            candidate.RoomIndex == fixture.GetProperty("Room").GetInt32());
        CombatUnit boss = room.Units.Single(unit => unit.AssetKey == "Boss_Level3_Fleshweaver_" + suffix);
        Trigger(boss, suffix);
        TriggeredSummonRule summon = boss.Triggers.Single(trigger => trigger.Kind == "OnHit").Effects[0].Summon!;
        CombatContext context = placed.Spawn.Train.Context!;
        TriggeredSummonCatalog catalog = context.SummonCatalog!;
        SummonUnitDefinition projected = catalog.Units.Single(unit => unit.CharacterId == sourceId);
        Require(summon.CharacterId == sourceId && projected.HeroDefinition?.Unit.AssetKey == mini.AssetKey &&
            projected.HeroDefinition.Unit.Triggers.Select(trigger => trigger.Kind).SequenceEqual(mini.Triggers.Select(trigger => trigger.Kind)),
            "The authored hit must retain the source GUID and capture the native replacement template.");

        // Older captures contain the resolved native template with the adapter's original refusal attached.
        const string oldRefusal = "Uncalibrated native hero creation substitution";
        RoomCombatState Scope(CombatContext input) => new(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
            input, room.Preview, room.ActiveCompanionBossIds);
        if (projected.ExternalInteractions.Count != 0)
        {
            Require(projected.ExternalInteractions.SequenceEqual(new[] { oldRefusal }) &&
                TriggeredSummonModel.Validate(Scope(context), summon, boss, "OnHit") ==
                    "Unmodeled triggered summon definition: " + oldRefusal, "The older native template must retain its exact projection refusal.");
            EnemyDefinition old = projected.HeroDefinition;
            var resolvedHero = new EnemyDefinition(old.Unit, old.Ascends, old.Loops,
                old.ExternalInteractions.Where(error => error != oldRefusal).ToArray(), old.CompanionBoss, old.BirthStats);
            var resolved = new SummonUnitDefinition(projected.CharacterId, projected.Unit, projected.Fallback,
                Array.Empty<string>(), resolvedHero);
            context = context.WithSummonCatalog(new TriggeredSummonCatalog(
                catalog.Units.Select(unit => unit == projected ? resolved : unit).ToArray(), catalog.Cards, catalog.Rooms));
        }
        Require(TriggeredSummonModel.Validate(Scope(context), summon, boss, "OnHit") == null &&
            TriggeredSummonModel.Validate(Scope(context), summon, boss, "OnSpawn") != null &&
            TriggeredSummonModel.Validate(Scope(context.WithRelics(context.Relics!.Where(owner => owner.DataId != ownerId).ToArray())),
                summon, boss, "OnHit") != null,
            "The exact replacement must be accepted only for the matching native holder and hit.");
        Console.WriteLine($"FLESHWEAVER-{suffix.ToUpperInvariant()}-TRIAL-SUBSTITUTION PASS: original source/replacement GUIDs, captured template, native birth and wrong-owner controls.");
    }

    internal static void SpikeTrialBattle(string path)
    {
        using var fixture = ModelJson.ReadFixture(path);
        FixtureValue root = fixture.RootElement;
        FixtureValue input = root.GetProperty("NativeRunInput");
        FixtureValue route = root.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == SpikeTrialScenario &&
            input.GetProperty("Distance").GetInt32() == 2 && input.GetProperty("AscensionLevel").GetInt32() == 0 &&
            input.GetProperty("BattlesWon").GetInt32() == 2 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            route.GetProperty("Boss").GetString() == TriggeredSummonModel.FleshweaverSpike &&
            route.GetProperty("SelectedScenario").GetString() == SpikeTrialScenario &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            root.GetProperty("OriginalOuterBossTrialIntro").GetProperty("EnabledAfter").GetBoolean(),
            "Expected the original Ring 3 Trial selected from the progressed native checkpoint.");
        int births = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit[] previous = before.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            CombatUnit[] current = after.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            foreach (CombatUnit mini in current.Where(unit => unit.AssetKey == TriggeredSummonModel.FleshweaverSpikeTrialMini &&
                previous.All(old => old.Id != unit.Id)))
            {
                CombatUnit boss = previous.Single(unit => unit.AssetKey == TriggeredSummonModel.FleshweaverSpike);
                CombatUnit nextBoss = current.Single(unit => unit.Id == boss.Id);
                Trigger(boss, "Spike");
                Require(boss.Triggers.Single(trigger => trigger.Kind == "OnHit").Effects[0].Summon!.CharacterId ==
                    TriggeredSummonModel.FleshweaverSpikeMiniId &&
                    before.Spawn.Train.Context!.SummonCatalog!.Units.Any(unit =>
                        unit.CharacterId == TriggeredSummonModel.FleshweaverSpikeMiniId &&
                        unit.HeroDefinition?.Unit.AssetKey == TriggeredSummonModel.FleshweaverSpikeTrialMini) &&
                    boss.AttackFromMass() > nextBoss.AttackFromMass() &&
                    mini.Status("spikes")?.Stacks == 2 && mini.BumpRules?.OuterBoss == false && !mini.EndsBattleOnDeath,
                    "The original hit must retain the source GUID, create the trial mini and consume Mass.");
                births++;
            }
        }
        Require(births > 0, "The native trial must contain a successful substituted summon.");
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine($"FLESHWEAVER-SPIKE-TRIAL-BATTLE PASS: {births} substituted births after native card actions.");
    }

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
