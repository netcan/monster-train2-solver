using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherReanimateChecks
{
    private const string Scenario = "SoulSavior_R5_BossBattle_Lifemother_Reanimate";
    private const string BossKey = "Boss_SoulSavior_Final_Lifemother_HealReanimate";
    private const string Boundary = "NativeLifemotherReanimatePostCombat";

    internal static void Export(string inputPath, string outputPath)
    {
        using var document = ModelJson.ReadFixture(inputPath);
        FixtureValue battle = document.RootElement;
        Require(battle.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            battle.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            battle.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0,
            "Expected the completed original 2.3.1 native capture.");
        using var input = File.OpenRead(inputPath);
        string sourceHash = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Schema"), builder.Add(FixtureKind.Number, "1"), Key("Boundary"), Key(Boundary),
            Key("SourceBattleSha256"), Key(sourceHash),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("SourceMismatches"), builder.Import(battle.GetProperty("Mismatches")),
            Key("SourceUnsupported"), builder.Import(battle.GetProperty("Unsupported")),
            Key("Observation"), builder.Import(battle.GetProperty("OriginalUnrulyBattle")),
            Key("TriggeredStatuses"), builder.Import(battle.GetProperty("TriggeredStatuses"))
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        Native(component.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);
        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static void Native(FixtureValue component)
    {
        string? sourceHash = component.GetProperty("SourceBattleSha256").GetString();
        Require(component.GetProperty("Schema").GetInt32() == 1 && component.GetProperty("Boundary").GetString() == Boundary &&
            sourceHash?.Length == 64 && sourceHash.All(Uri.IsHexDigit) &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            component.GetProperty("SourceMismatches").GetInt32() >= 0 && component.GetProperty("SourceUnsupported").GetInt32() >= 0,
            "Expected an original Lifemother Reanimate component with binary source provenance.");
        FixtureValue observation = component.GetProperty("Observation");
        Require(observation.GetProperty("Scenario").GetString() == Scenario && observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected the installed original Lifemother Reanimate controlled scenario.");

        FixtureValue[] rows = component.GetProperty("TriggeredStatuses").EnumerateArray().ToArray();
        Require(rows.Length >= 2 && rows.Length % 2 == 0, "Expected ordered Witherbloom and Undying pairs.");
        int previousSequence = 0, witherbloom = 0, emptyWitherbloom = 0, undying = 0;
        foreach (FixtureValue[] pair in rows.Chunk(2))
        {
            Require(pair[0].GetProperty("ActorId").ContentEquals(pair[1].GetProperty("ActorId")) &&
                pair[0].GetProperty("Before").GetProperty("RoomIndex").ContentEquals(pair[1].GetProperty("Before").GetProperty("RoomIndex")),
                "Paired effects must share their native actor and room.");
            witherbloom += Replay(pair[0], "witherbloom", stacks: 10, CombatTeam.Player, ref previousSequence);
            if (pair[0].GetProperty("Targets").GetArrayLength() == 0) emptyWitherbloom++;
            undying += Replay(pair[1], "undying", stacks: 2, CombatTeam.Enemy, ref previousSequence);
        }
        Require(undying > 0 && witherbloom > 0 && emptyWitherbloom > 0,
            "Expected native enemy Undying, player Witherbloom and an empty Witherbloom target control.");
        Console.WriteLine($"NATIVE-LIFEMOTHER-REANIMATE PASS: {rows.Length} exact PostCombat effects, " +
            $"{undying} Undying and {witherbloom} Witherbloom grants, {emptyWitherbloom} empty Witherbloom targets.");
    }

    private static int Replay(FixtureValue row, string statusId, int stacks, CombatTeam team, ref int previousSequence)
    {
        int sequence = row.GetProperty("Sequence").GetInt32();
        int actorId = row.GetProperty("ActorId").GetInt32();
        int[] targets = row.GetProperty("Targets").Deserialize<int[]>()!;
        var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var initial = row.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!;
        var observed = row.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!;
        CombatEffect effect = row.GetProperty("Effect").Deserialize<CombatEffect>()!;
        Require(sequence > previousSequence && row.GetProperty("Completed").GetBoolean() &&
            row.GetProperty("Kind").GetString() == "PostCombat" && row.GetProperty("SourceCardId").GetInt32() == 0 &&
            row.GetProperty("Interactions").GetArrayLength() == 0 && targets.Length <= 1 &&
            initial.Single(unit => unit.Id == actorId) is { AssetKey: BossKey, Team: CombatTeam.Enemy } &&
            effect is { Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "FrontInRoom", Range: null,
                Value: 0, Statuses: [var status] } action } && status.Id == statusId && status.Stacks == stacks &&
            action.AllowEnemy == (team == CombatTeam.Enemy) && action.AllowPlayer == (team == CombatTeam.Player) &&
            initial.Select(unit => unit.Id).SequenceEqual(new[] { actorId }.Concat(targets).Distinct()) &&
            observed.Select(unit => unit.Id).SequenceEqual(initial.Select(unit => unit.Id)) &&
            initial.Where(unit => targets.Contains(unit.Id)).All(unit => unit.Team == team),
            $"Changed Lifemother {statusId} callback, source, target order or payload at sequence {sequence}.");
        previousSequence = sequence;

        var scope = new RoomCombatState(before.RoomIndex, before.Deployment,
            before.Units.Concat(initial.Where(unit => before.Units.All(live => live.Id != unit.Id))).ToArray(),
            before.ExternalInteractions, before.Context, before.Preview, before.ActiveCompanionBossIds);
        string immutable = Serialize(scope);
        RoomCombatResult predicted = TriggeredStatusModel.Apply(scope, actorId, effect, targets, 0);
        Require(predicted.Supported, "Lifemother effect replay failed: " + predicted.UnsupportedReason);
        Match(predicted.State!.Context, actual.Context, "context and RNG");
        foreach (CombatUnit unit in observed)
            Match(predicted.State.Units.Single(expected => expected.Id == unit.Id), unit, $"actor or target {unit.Id}");
        Require(Serialize(scope) == immutable, "Lifemother effect replay changed its input.");
        return targets.Count(target => (observed.Single(unit => unit.Id == target).Status(statusId)?.Stacks ?? 0) -
            (initial.Single(unit => unit.Id == target).Status(statusId)?.Stacks ?? 0) == stacks);

        void Match<T>(T predictedValue, T actualValue, string detail)
        {
            string? difference = ModelJson.Difference(Serialize(predictedValue), Serialize(actualValue));
            Require(difference == null, $"Lifemother effect {sequence} differs in {detail} at {difference}.");
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
