using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CaelChecks
{
    private const string TrialMvid = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
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

    internal static void ShieldTrial(FixtureValue substitution, FixtureValue birth) => Trial(substitution, birth, "DamageShield");

    private static (string OuterId, string ReplacementId, string FeederId, string FeederReplacementId,
        string OwnerId, string UpgradeId, int FeederHealth, int SelfDamage) TrialIdentity(string variant) => variant switch
        {
            "DamageShield" => ("168b33b6-361b-4691-bc64-aa58c66eb5db", "3794c878-797d-4e81-8f0b-644869661970",
                "43bbea4c-c1e8-4744-a4b1-e5b5f135c059", "b29a1359-c39b-48d6-9649-1740667ed97e",
                OuterBossDefinitions.CaelShieldTrialOwnerId, "067b599f-ac23-4c99-8841-192fc5b7dd74", 60, 20),
            "AtkHP" => ("e88fbe2c-a90d-457d-9d87-3246547a72ac", "7545c2d9-53ba-49d7-b937-a5018ef32e27",
                "104e4d49-1c74-4fc1-92ec-73fd69842a28", "85380a63-dbe3-4db4-98ec-a266a68a60e5",
                OuterBossDefinitions.CaelAtkHpTrialOwnerId, "43d3af4b-44b7-4d8c-9aac-4768b7651a60", 30, 10),
            "Lifesteal" => ("dee70287-3dd8-4b9b-8d4d-0a20344b92ea", "a7f04f71-f121-4471-85d7-2c7860e1ca50",
                "e795417c-eb95-470d-b8ff-46ff8bcb166a", "0a0b9e16-8f55-4f11-898b-9dbeb26b6fe3",
                OuterBossDefinitions.CaelLifestealTrialOwnerId, "3fde76c8-3100-4483-95e8-30dfc50eac6e", 30, 10),
            _ => throw new ArgumentException("Unknown original Cael trial variant.")
        };

    internal static void Trial(FixtureValue substitution, FixtureValue birth, string variant, bool rankedShield = false)
    {
        Require(!rankedShield || variant == "DamageShield", "Only the Shield Trial has a ranked native sample.");
        var identity = TrialIdentity(variant);
        string scenario = "Level6BattleEater" + variant;
        string bossSource = Boss(variant), bossReplacement = bossSource + "_TitanTrial";
        string feederSource = "Boss_Level6_Feed" + variant, feederReplacement = feederSource + "_TitanTrial";
        string ownerKey = "Enemy_SpawnHeroEaterFeed" + variant + "_TitanTrial";
        FixtureValue intro = substitution.GetProperty("Intro");
        Require(substitution.GetProperty("GameVersion").GetString() == "2.3.1" &&
            substitution.GetProperty("GameModuleMvid").GetString() == TrialMvid &&
            substitution.GetProperty("Boundary").GetString() == "NativeOriginalEarlyTitanTrialSubstitution" &&
            substitution.GetProperty("Scenario").GetString() == scenario &&
            substitution.GetProperty("OwnerId").GetString() == identity.OwnerId &&
            substitution.GetProperty("OwnerKey").GetString() == ownerKey &&
            substitution.GetProperty("OwnerEffects").Deserialize<string[]>()!.SequenceEqual(["RelicEffectOnSpawnHeroSubstitution", "RelicEffectNull"]) &&
            substitution.GetProperty("AscensionLevel").GetInt32() == (rankedShield ? 2 : 0) &&
            substitution.GetProperty("Source").GetString() == bossSource &&
            substitution.GetProperty("SourceId").GetString() == identity.OuterId &&
            substitution.GetProperty("ReplacementId").GetString() == identity.ReplacementId &&
            substitution.GetProperty("Replacement").GetString() == bossReplacement &&
            substitution.GetProperty("OuterBoss").GetBoolean() && substitution.GetProperty("TitanTrial").GetBoolean() &&
            intro.GetProperty("EnabledAfter").GetBoolean() && intro.GetProperty("UiAfter").GetBoolean() &&
            (!rankedShield || !intro.GetProperty("EligibleBefore").GetBoolean() && !intro.GetProperty("EnabledBefore").GetBoolean()) &&
            intro.GetProperty("ChangedRngIds").EnumerateArray().All(stream => stream.GetString() == "NonDeterministic"),
            "Expected the installed Cael trial and active native substitution source; the rank-two control must record the eligibility override.");
        FixtureValue projection = substitution.GetProperty("Projection");
        Require(projection.GetProperty("Key").GetString() == bossReplacement &&
            projection.GetProperty("ExternalInteractions").Deserialize<string[]>()!.SequenceEqual(["Spawned outer Boss actions"]) &&
            substitution.GetProperty("Unsupported").GetInt32() == 0 && substitution.GetProperty("Mismatches").GetInt32() == 0,
            "The original Cael trial still has a projection or execution deferral.");
        FixtureValue mappings = substitution.GetProperty("Mappings");
        Require(Mapped(bossSource, identity.OuterId, bossReplacement, identity.ReplacementId) &&
            Mapped(feederSource, identity.FeederId, feederReplacement, identity.FeederReplacementId),
            "The installed table must contain both original Cael trial replacements.");

        FixtureValue row = birth.GetProperty("Births").EnumerateArray().Single();
        OuterBossInstantiationState before = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        OuterBossCreationState created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        OuterBossPhaseState placed = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit callback = substitution.GetProperty("Unit").Deserialize<CombatUnit>()!;
        CombatContext context = before.Battle.Spawn.Train.Context!;
        bool Calibrated(CombatContext input) => variant == "DamageShield" && !rankedShield
            ? OuterBossDefinitions.CalibratedCaelShieldTrial(before.Definition.Actor.Unit, input)
            : OuterBossDefinitions.CalibratedCaelTrial(before.Definition.Actor.Unit, input, before.Definition.Actor.BirthStats);
        Require(callback.Id == placed.Boss.UnitId && callback.AssetKey == placed.Boss.AssetKey && Calibrated(context) &&
            !Calibrated(context.WithRelics(context.Relics!.Where(owner => owner.DataId != identity.OwnerId).ToArray())),
            "The captured replacement actor, exact owner requirement or native callback identity differs.");
        int selected = before.Battle.Spawn.SelectedGroups[0];
        EnemyDefinition food = before.Battle.Spawn.Waves[0].Candidates[selected].Units.Single(entry => entry.Unit.AssetKey == feederReplacement);
        CombatUnit actualFood = placed.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == feederReplacement);
        Require(food.ExternalInteractions.Count == 0 && food.Unit.Id == 0 && actualFood.Id > 0 &&
            actualFood.Id < callback.Id &&
            Feeder(food.Unit, bossReplacement, identity.FeederHealth, identity.SelfDamage) &&
            Feeder(actualFood, bossReplacement, identity.FeederHealth, identity.SelfDamage),
            "The original first wave must contain its resolved Feed actor with the ordered OnEaten effects.");
        if (rankedShield)
        {
            CombatRelicState[] covenants = context.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
            CombatUnit born = placed.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == callback.Id);
            Require(TrueFinalBossCovenantModel.CalibratedCaelShieldTrialOwners(covenants) &&
                !TrueFinalBossCovenantModel.CalibratedCaelShieldTrialOwners(covenants.Reverse().ToArray()) &&
                OrderedStats(before.Definition.Actor.BirthStats, 700) && OrderedStats(created.BirthStats, 700) &&
                OrderedStats(food.BirthStats, 0) && before.Definition.Actor.Unit.Health == 1800 && created.CreatedUnit.Health == 1800 &&
                callback is { Health: 2500, MaxHealth: 2500, BaseAttack: 22, Attack: 22 } &&
                born is { Health: 2500, MaxHealth: 2500, BaseAttack: 22, Attack: 22 },
                "Rank-two birth must apply the original ordered +700 HP at placement and leave the feeder's 60 HP unchanged.");
            Require(!OuterBossDefinitions.CalibratedCaelTrial(before.Definition.Actor.Unit, context,
                    new EnemyBirthStatAdjustment(0, 701, covenants.Select(owner => owner.DataId).ToArray())),
                "Ranked trial accepted an uncalibrated health adjustment.");
        }
        else Require(food.BirthStats == null, "Rank-zero feeder unexpectedly includes Covenant birth stats.");
        OuterBossBirthChecks.Native(birth, TrialMvid);
        Console.WriteLine($"CAEL-TRIAL PASS {variant}: original outer callback, resolved first wave and complete immutable placement, controller and RNG.");

        bool Mapped(string source, string sourceId, string replacement, string replacementId) =>
            mappings.EnumerateArray().Count(map => map.GetProperty("Source").GetString() == source &&
                map.GetProperty("SourceId").GetString() == sourceId &&
                map.GetProperty("Replacement").GetString() == replacement &&
                map.GetProperty("ReplacementId").GetString() == replacementId) == 1;

        static bool OrderedStats(EnemyBirthStatAdjustment? stats, int health) => stats is { Attack: 0 } &&
            stats.Health == health && stats.CovenantIds.SequenceEqual([
                "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e"]);
    }

    private static bool Feeder(CombatUnit unit, string boss, int health, int selfDamage) =>
        unit.Health == health && unit.MaxHealth == health &&
        unit is { Team: CombatTeam.Enemy, BaseAttack: 0, Attack: 0, Size: 1,
            EndsBattleOnDeath: false, BumpRules.OuterBoss: false, Statuses: [{ Id: "eatmany", Stacks: 3 }],
            Triggers: [{ Kind: "OnEaten", Effects: [
                { Type: "CardEffectAddTempCardUpgradeToUnits", UnitUpgrade: {
                    Target: "LastFeederCharacter", Lifetime: "TemporaryUntilEndOfBattle", AllowEnemy: true, AllowPlayer: false,
                    Upgrade: { } upgrade } },
                { Type: "CardEffectDamage", Action: { Target: "Self", Value: var damage } }] }] } &&
        damage == selfDamage && OuterBossDefinitions.CalibratedCaelTrialUpgrade(boss, upgrade);

    internal static void ShieldTrialBattle(string path) => TrialBattle(path, "DamageShield");

    internal static void TrialBattle(string path, string variant, bool rankedShield = false)
    {
        Require(!rankedShield || variant == "DamageShield", "Only the Shield Trial has a ranked native sample.");
        string scenario = "Level6BattleEater" + variant, baseBoss = Boss(variant);
        string bossReplacement = baseBoss + "_TitanTrial", feederReplacement = "Boss_Level6_Feed" + variant + "_TitanTrial";
        var identity = TrialIdentity(variant);
        using var document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        FixtureValue route = fixture.GetProperty("OriginalOuterBossRoute");
        FixtureValue intro = fixture.GetProperty("OriginalOuterBossTrialIntro");
        Require(input.GetProperty("Scenario").GetString() == scenario &&
            input.GetProperty("Distance").GetInt32() == 5 && input.GetProperty("AscensionLevel").GetInt32() == (rankedShield ? 2 : 0) &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            route.GetProperty("Boss").GetString() == baseBoss &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")) &&
            intro.GetProperty("EnabledAfter").GetBoolean() &&
            (!rankedShield || !intro.GetProperty("EligibleBefore").GetBoolean() && !intro.GetProperty("EnabledBefore").GetBoolean()),
            "Expected the native Cael trial from a five-win original checkpoint with explicit ranked eligibility evidence.");
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
            if (beforeUnits.All(unit => unit.AssetKey != bossReplacement)) continue;
            foreach (CombatUnit old in beforeUnits)
            {
                if (old.AssetKey == feederReplacement) births.Add(old.Id);
                if (!actualUnits.TryGetValue(old.Id, out CombatUnit? next)) continue;
                if (old.AssetKey == feederReplacement && next.LastFeederId is int eaterId && eaterId > 0 &&
                    old.Health - next.Health == identity.SelfDamage &&
                    (old.Status("eatmany")?.Stacks ?? 0) - (next.Status("eatmany")?.Stacks ?? 0) == 1 &&
                    actualUnits.TryGetValue(eaterId, out CombatUnit? eater) &&
                    beforeUnits.SingleOrDefault(unit => unit.Id == eaterId) is { Team: CombatTeam.Enemy } oldEater &&
                    UpgradeCount(eater) > UpgradeCount(oldEater) && AppliedTrialStatus(eater, 1))
                    feedings++;
                int delta = UpgradeCount(next) - UpgradeCount(old);
                if (delta <= 0) continue;
                Require(old.Team == CombatTeam.Enemy, "The original trial's enemy upgrade reached a player unit.");
                if (AppliedTrialStatus(next, delta))
                    upgrades += delta;
            }
        }
        Require(births.Count > 0 && upgrades > 0 && feedings > 0,
            "Expected a native wave feeder, consumption and positive application of its trial upgrade.");
        SolverStageChecks.Native(path, requireVictory: false);
        Console.WriteLine($"CAEL-TRIAL-BATTLE PASS {variant}: {births.Count} original wave feeders, {feedings} witnessed feedings and {upgrades} trial upgrades.");

        int UpgradeCount(CombatUnit unit) => unit.Modifiers?.Upgrades.Count(value => value.DataId == identity.UpgradeId) ?? 0;
        bool AppliedTrialStatus(CombatUnit unit, int count) => variant switch
        {
            "DamageShield" => (unit.Status("damage shield")?.Stacks ?? 0) >= count && (unit.Status("spell shield")?.Stacks ?? 0) >= count,
            "AtkHP" => (unit.Status("spikes")?.Stacks ?? 0) >= 6 * count,
            "Lifesteal" => (unit.Status("lifesteal")?.Stacks ?? 0) >= count,
            _ => false
        };
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
