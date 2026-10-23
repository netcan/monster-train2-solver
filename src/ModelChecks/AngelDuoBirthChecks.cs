using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoBirthChecks
{
    internal static void Battle(FixtureValue fixture, string path)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Angel duo native battle failed: " + counter);
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 5 &&
            input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("StartingLevel").GetInt32() == 2 &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("Scenario").GetString() == "Level6AngelDuo_RallyAttackIncantHealth" &&
            input.GetProperty("CompletedBattles").GetArrayLength() == 5 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            !fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Angel duo baseline must retain the original rank-two five-win checkpoint and its native no-card loss.");
        BattleTurnChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        // The first EndTurn root precedes outer-boss creation during deployment.
        BattleTurnState initial = fixture.GetProperty("Turns").EnumerateArray()
            .Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey)));
        CombatUnit[] companions = initial.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.BumpRules?.CompanionBoss == true).ToArray();
        Require(companions.Length == 2 && companions.All(unit => !unit.EndsBattleOnDeath &&
            companions.Any(other => other.Id == unit.BumpRules!.PairedBossUnitId && other.Id != unit.Id)) &&
            companions.Any(unit => unit.Status("pyrebane")?.Stacks == 1) &&
            companions.Any(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey)),
            "Angel duo baseline lost a paired original boss or its authored status.");
        Console.WriteLine("ANGEL-DUO-BATTLE PASS: original progressed input, paired bosses, full rooms/train/turns and sixteen independent complete no-card branches; death phase and player victory remain separate.");
    }

    internal static void Native(FixtureValue fixture)
    {
        OuterBossBirthChecks.Native(fixture);
        FixtureValue row = fixture.GetProperty("Births")[0];
        var input = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        CombatUnit partner = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit =>
            unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack");
        CombatTrigger incant = born.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
        CombatEffect health = incant.Effects.Single();
        Require(OuterBossDefinitions.IsAngelDuo(born.AssetKey) && input.Definition.Actor.Unit.Health == 1250 &&
            input.Definition.Actor.Unit.BaseAttack == 11 && born.Health == 1500 && born.BaseAttack == 14 &&
            born.Status("titanskin")?.Stacks == 10 && created.BirthStats?.Attack == 3 && created.BirthStats.Health == 250 &&
            created.BirthStats.CovenantIds.SequenceEqual(new[] {
                "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e" }),
            "Angel duo birth must retain its original authored definition and native rank-two Covenant adjustments.");
        Require(created.CreatedUnit.BumpRules!.PairedBossUnitId == partner.Id &&
            !created.CreatedUnit.EndsBattleOnDeath && !created.CreatedUnit.Triggers.Any(trigger => trigger.Kind == "CardSpellPlayed") &&
            !born.EndsBattleOnDeath && !partner.EndsBattleOnDeath &&
            born.BumpRules!.PairedBossUnitId == partner.Id && partner.BumpRules!.PairedBossUnitId == born.Id &&
            actual.Battle.Spawn.Train.Rooms.All(room => room.ActiveCompanionBossIds!.OrderBy(id => id)
                .SequenceEqual(new[] { born.Id, partner.Id }.OrderBy(id => id))),
            "Angel duo creation/registration must preserve its native directed pair and live companion roster boundaries.");
        Require(health.Type == "CardEffectBuffMaxHealth" && health.Value == 30 && health.Action is
            { Type: "BuffHealth", Target: "Tower", Lifetime: "TemporaryUntilEndOfBattle" } &&
            health.Action.Filters?.Subtype == "SubtypesData_SavageAngel" &&
            born.Triggers.Single(trigger => trigger.Kind == "OnDeath").Effects.Any(effect => effect.Type == "CardEffectForceRelentless"),
            "Original battle relic must install the native Incant trigger after creation; death phase switching remains separately guarded.");
        string immutableInput = JsonSerializer.Serialize(input, ModelJson.Options);
        string immutableCreated = JsonSerializer.Serialize(created, ModelJson.Options);
        string expectedCreated = immutableCreated;
        string expectedBorn = JsonSerializer.Serialize(actual, ModelJson.Options);
        Parallel.For(0, 16, _ => {
            OuterBossInstantiationResult instance = OuterBossInstantiationModel.Apply(input);
            OuterBossPhaseResult birth = OuterBossBirthModel.Apply(created);
            Require(instance.Supported && birth.Supported &&
                JsonSerializer.Serialize(instance.State, ModelJson.Options) == expectedCreated &&
                JsonSerializer.Serialize(birth.State, ModelJson.Options) == expectedBorn &&
                JsonSerializer.Serialize(input, ModelJson.Options) == immutableInput &&
                JsonSerializer.Serialize(created, ModelJson.Options) == immutableCreated,
                "Angel duo birth branches changed full native state/RNG or their shared roots.");
        });
        Console.WriteLine("ANGEL-DUO-BIRTH PASS: original rank-two raw/adjusted stats, titanskin, directed companion registration, native battle-relic trigger installation and sixteen immutable creation/placement branches.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
