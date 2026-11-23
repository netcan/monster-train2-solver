using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherInfestedChecks
{
    private const string Scenario = "SoulSavior_R5_BossBattle_Lifemother_Infested";
    private const string BossKey = "Boss_SoulSavior_Final_Lifemother_Infested";
    private const string Boundary = "NativeLifemotherInfestedStatusAddition";

    internal static void Export(string inputPath, string outputPath)
    {
        using var battleDocument = ModelJson.ReadFixture(inputPath);
        FixtureValue battle = battleDocument.RootElement;
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0,
            "Expected a completed original 2.3.1 native capture.");
        using var stream = File.OpenRead(inputPath);
        string hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Boundary"), Key(Boundary), Key("SourceBattleSha256"), Key(hash),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
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
        FixtureValue observation = component.GetProperty("Observation");
        string? hash = component.GetProperty("SourceBattleSha256").GetString();
        Require(component.GetProperty("Boundary").GetString() == Boundary && hash?.Length == 64 && hash.All(Uri.IsHexDigit) &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario && observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected a pinned original Lifemother Infested component.");

        var rows = component.GetProperty("TriggeredStatuses").EnumerateArray().ToArray();
        Require(rows.Length == 3, "Expected three original Hivemind additions.");
        int actorId = rows[0].GetProperty("ActorId").GetInt32();
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var effect = row.GetProperty("Effect").Deserialize<CombatEffect>()!;
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var actor = before.Units.Single(unit => unit.Id == actorId);
            var observed = row.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!.Single(unit => unit.Id == actorId);
            Require(row.GetProperty("Sequence").GetInt32() == index + 1 && row.GetProperty("ActorId").GetInt32() == actorId &&
                row.GetProperty("Completed").GetBoolean() && row.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor" &&
                row.GetProperty("SourceCardId").GetInt32() == 0 && row.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual([actorId]) &&
                actor is { AssetKey: BossKey, Team: CombatTeam.Enemy } &&
                (actor.Status("hivemind")?.Stacks ?? 0) == index * 2 && observed.Status("hivemind")?.Stacks == (index + 1) * 2 &&
                actor.Attack == observed.Attack && effect is { Type: "CardEffectAddStatusEffect", Action: {
                    Type: "AddStatus", Target: "Self", AllowEnemy: false, AllowPlayer: true, Value: 0, Range: null,
                    Statuses: [{ Id: "hivemind", Stacks: 2 }] } }, "Changed original Hivemind addition at step " + (index + 1));

            string immutable = Serialize(before);
            RoomCombatResult applied = StatusApplicationModel.ApplyRetained(before, actorId, effect.Action!.Statuses[0], 0);
            Require(applied.Supported, "Could not apply captured Hivemind: " + applied.UnsupportedReason);
            var predicted = applied.State!.Units.Single(unit => unit.Id == actorId);
            string? difference = ModelJson.Difference(Serialize(predicted), Serialize(observed));
            Require(difference == null && Serialize(before) == immutable &&
                applied.State.Context!.BattleRng.Equals(actual.Context!.BattleRng) &&
                applied.State.Context.Enchantments!.TestRng.Equals(actual.Context.Enchantments!.TestRng) &&
                applied.State.Context.Cards.Rng.Equals(actual.Context.Cards.Rng),
                "Hivemind actor or RNG differs: " + difference);
        }
        Console.WriteLine("NATIVE-LIFEMOTHER-INFESTED PASS: 3 original status additions, full actor state and all three RNG streams.");
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidDataException(message);
    }
}
