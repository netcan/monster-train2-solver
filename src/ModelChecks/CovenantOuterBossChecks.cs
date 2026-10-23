using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CovenantOuterBossChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);

    private static void Evidence(FixtureValue root, string variant, bool ftue)
    {
        Require(variant is "AllyBuffer" or "Fragile" or "Corrupter", "Uncalibrated Covenant outer-boss variant.");
        CovenantEnemyStatsChecks.Evidence(root, 7);
        string boss = "Boss_Level8_" + variant;
        FixtureValue route = root.GetProperty("OriginalOuterBossRoute");
        Require(route.GetProperty("Kind").GetString() == "SelectOriginalOuterBossScenario" &&
            route.GetProperty("Distance").GetInt32() == 7 && route.GetProperty("Boss").GetString() == boss &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("TitanTrial").ValueKind == FixtureKind.Null &&
            route.GetProperty("RngBefore").GetArrayLength() == 16 && route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")),
            "Covenant Ring8 control must retain its original explicitly selected scenario and all RNG streams.");
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState[] owners = before.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
        Require(owners.Length == (ftue ? 2 : 1) && owners.Any(owner => owner.AssetKey == "Covenant00_FTUE") == ftue,
            "Covenant Ring8 control has the wrong original context.");
        EnemyDefinition actor = before.Spawn.QueuedOuterBoss!.Actor;
        int rawHealth = variant == "AllyBuffer" ? 3800 : variant == "Fragile" ? 3000 : 2800;
        int rawAttack = variant == "AllyBuffer" ? 35 : variant == "Fragile" ? 30 : 20;
        int healthAdjustment = ftue ? 3000 : 1000;
        FixtureValue query = root.GetProperty("CovenantEnemyStatsControl").GetProperty("StatQueries").EnumerateArray()
            .Single(row => row.GetProperty("AssetKey").GetString() == boss);
        Require(actor.Unit.AssetKey == boss && actor.Unit.BaseAttack == rawAttack && actor.Unit.MaxHealth == rawHealth &&
            actor.Unit.HordeDefinition?.Health == rawHealth && actor.BirthStats?.Attack == 0 && actor.BirthStats.Health == healthAdjustment &&
            TrueFinalBossCovenantModel.MatchesOrigins(before.Spawn.Train.Context, actor.BirthStats) &&
            query.GetProperty("RawAttack").GetInt32() == rawAttack && query.GetProperty("RawHealth").GetInt32() == rawHealth &&
            query.GetProperty("Attack").GetInt32() == 0 && query.GetProperty("Health").GetInt32() == healthAdjustment,
            "Covenant outer-boss projection must preserve original authored/Horde stats and its native +1000/+3000 HP adjustment.");
        BattleTurnState born = root.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit unit = born.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == boss);
        Require(unit.MaxHealth == rawHealth + healthAdjustment && unit.BaseAttack == rawAttack,
            "The original boss must receive the Covenant adjustment exactly once after creation.");
        JsonNode invalid = JsonSerializer.SerializeToNode(before.Spawn.QueuedOuterBoss, ModelJson.Options)!;
        invalid["Actor"]!["BirthStats"]!["CovenantIds"]![0] = "different-covenant";
        OuterBossInstantiationResult rejected = OuterBossInstantiationModel.Apply(new OuterBossInstantiationState(before,
            invalid.Deserialize<OuterBossSpawnDefinition>(ModelJson.Options)!));
        Require(!rejected.Supported && rejected.UnsupportedReason!.Contains("Enemy birth stats"),
            "Spoofed Covenant outer-boss origins must remain guarded.");
    }

    internal static void Native(FixtureValue root, FixtureValue birth, string variant, bool ftue)
    {
        Evidence(root, variant, ftue);
        Require(root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("Actions").GetArrayLength() == 0,
            "The isolated Covenant outer-boss birth control must not play cards.");
        OuterBossBirthChecks.Native(birth);
        OuterBossCreationState created = birth.GetProperty("Births")[0].GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(created.BirthStats?.Attack == 0 && created.BirthStats.Health == (ftue ? 3000 : 1000) &&
            created.CreatedUnit.MaxHealth == (variant == "AllyBuffer" ? 3800 : variant == "Fragile" ? 3000 : 2800),
            "Native creation must precede the Covenant attribute callback.");
        OuterBossTurnChecks.Deployment(root, "Boss_Level8_" + variant, 2);
        BattleTurnChecks.Native(root);
        JsonNode missing = JsonSerializer.SerializeToNode(created, ModelJson.Options)!;
        missing["BirthStats"] = null;
        OuterBossPhaseResult guarded = OuterBossBirthModel.Apply(missing.Deserialize<OuterBossCreationState>(ModelJson.Options)!);
        Require(!guarded.Supported && guarded.UnsupportedReason!.Contains("native stats"),
            "An outer-boss Covenant callback without its stat evidence must remain guarded.");
        Console.WriteLine($"COVENANT-OUTER-BOSS PASS variant={variant} ftue={ftue}: raw creation, native +1000/+3000 HP after placement, full battle/state/RNG, immutable chains and spoofed/missing-origin rejection. Explicit isolated route/activation only.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, string variant, bool ftue)
    {
        Evidence(source, variant, ftue);
        Evidence(replay, variant, ftue);
        CovenantEnemyStatsChecks.Pair(source, replay, ftue, 7);
        Console.WriteLine($"PLAYER-COVENANT-OUTER-BOSS PASS variant={variant} ftue={ftue}: actual advice/UI and independent frozen native replay, complete forecasts/state/RNG and zero native failure counters.");
    }

    internal static void CuratedNative(string path)
    {
        string suffix = Path.GetFileNameWithoutExtension(path)["full-battle-covenant-outer-boss-".Length..];
        string variant = suffix.Split('-')[0] switch
        {
            "allybuffer" => "AllyBuffer", "fragile" => "Fragile", "corrupter" => "Corrupter",
            _ => throw new InvalidOperationException("Unknown curated Covenant outer-boss variant.")
        };
        bool ftue = suffix == variant.ToLowerInvariant() + "-00-01";
        Require(ftue || suffix == variant.ToLowerInvariant() + "-01", "Unknown curated Covenant context.");
        using var battle = ModelJson.ReadFixture(path);
        using var birth = ModelJson.ReadFixture(Path.Combine(Path.GetDirectoryName(path)!, "outer-boss-birth-covenant-" + suffix + ".mt2f"));
        Native(battle.RootElement, birth.RootElement, variant, ftue);
    }

    internal static void CuratedPair(string path)
    {
        string suffix = Path.GetFileNameWithoutExtension(path)["full-battle-player-covenant-outer-boss-".Length..^"-search".Length];
        string variant = suffix.Split('-')[0] switch
        {
            "allybuffer" => "AllyBuffer", "fragile" => "Fragile", "corrupter" => "Corrupter",
            _ => throw new InvalidOperationException("Unknown player Covenant outer-boss variant.")
        };
        bool ftue = suffix == variant.ToLowerInvariant() + "-00-01";
        Require(ftue || suffix == variant.ToLowerInvariant() + "-01", "Unknown player Covenant context.");
        using var source = ModelJson.ReadFixture(path);
        using var replay = ModelJson.ReadFixture(Path.Combine(Path.GetDirectoryName(path)!,
            "full-battle-player-covenant-outer-boss-" + suffix + "-replay.mt2f"));
        Pair(source.RootElement, replay.RootElement, variant, ftue);
    }
}
