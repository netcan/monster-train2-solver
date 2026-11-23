using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class IndulgentCastingChecks
{
    internal static void Native(FixtureValue birth, FixtureValue? phases)
    {
        OuterBossBirthChecks.Native(birth, Game231FixtureCatalog.Module);
        var placement = birth.GetProperty("Births")[0].GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        string asset = placement.Boss.AssetKey;
        Require(OuterBossDefinitions.IsIndulgentTrample(asset) &&
            !OuterBossDefinitions.Supports(asset), "Expected a scoped original Trample variant.");
        OuterBossPhaseResult guard = OuterBossPhaseModel.Apply(placement, preCombat: false);
        Require(guard.Supported && ReferenceEquals(guard.State, placement),
            "The exact original placement did not enter the contextual casting path: " + guard.UnsupportedReason);
        OuterBossProgress boss = placement.Boss;
        var editedBoss = new OuterBossProgress(boss.UnitId, boss.AssetKey, boss.Phase, 0, boss.BaseCanAttack,
            boss.Room, boss.MovementSpeed, boss.VisitedRooms, boss.Controller, boss.Actions, boss.DestroyActions);
        Require(!OuterBossPhaseModel.Apply(new OuterBossPhaseState(placement.Battle, editedBoss), preCombat: false).Supported,
            "Changed attack phases entered the scoped casting path.");
        var relentlessBoss = new OuterBossProgress(boss.UnitId, boss.AssetKey, "Relentless", boss.ValidAttackPhases,
            boss.BaseCanAttack, boss.Room, boss.MovementSpeed, boss.VisitedRooms, boss.Controller, boss.Actions, boss.DestroyActions);
        var relentless = new OuterBossPhaseState(placement.Battle, relentlessBoss);
        Require(!OuterBossPhaseModel.Apply(relentless, preCombat: true).Supported && !OuterBossPyreModel.Apply(relentless).Supported,
            "Casting recognition admitted a Relentless or Pyre path.");
        if (!phases.HasValue)
        {
            Console.WriteLine($"INDULGENT-CASTING-GUARD PASS {asset}: original placement reaches casting.");
            return;
        }

        FixtureValue capture = phases.Value;
        Require(capture.GetProperty("Schema").GetInt32() == 1 &&
            capture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            capture.GetProperty("Boundary").GetString() == "AfterBossActionRemoveDeadCharacters" &&
            !capture.GetProperty("ForcedSpawnQueueEmpty").GetBoolean() &&
            capture.GetProperty("QueueControls").GetArrayLength() == 0, "Expected the stock 2.3.1 casting capture.");
        int count = 0, preCombatCount = 0;
        foreach (FixtureValue row in capture.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            string phase = row.GetProperty("Phase").GetString()!;
            Require(before.Boss.AssetKey == asset, "The original variant changed during capture.");
            bool preCombat = phase == "BossActionPreCombat";
            if (before.Boss.Phase != "Casting" || preCombat && before.Battle.Spawn.Phase >= before.Battle.Spawn.Waves.Count)
                break;
            Require(row.GetProperty("NativeReturned").GetBoolean() && phase is "BossActionPreCombat" or "BossActionPostCombat",
                "The original casting stage did not settle.");
            string immutable = Serialize(before);
            var result = OuterBossPhaseModel.Apply(before, preCombat);
            Require(result.Supported, $"Original {phase} turn {before.Battle.Spawn.Turn} blocked: " + result.UnsupportedReason);
            var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
            string? difference = ModelJson.Difference(Serialize(result.State!), Serialize(actual));
            Require(difference == null && Serialize(before) == immutable,
                $"Original {phase} turn {before.Battle.Spawn.Turn} differs at {difference}.");
            count++;
            if (preCombat) preCombatCount++;
        }
        Require(preCombatCount > 0, "No original pre-combat casting action was captured.");
        Console.WriteLine($"NATIVE-INDULGENT-CASTING PASS {asset}: {count} exact stages, including {preCombatCount} before combat.");
    }

    private static string Serialize(OuterBossPhaseState phase) => JsonSerializer.Serialize(phase, ModelJson.Options);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
