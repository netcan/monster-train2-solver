using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BossPreviewSweepChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0 && root.GetProperty("Mismatches").GetInt32() == 3,
            "The historical negative control must retain its three original mismatches.");
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        Require(stages.Where(stage => stage.GetProperty("Difference").ValueKind != FixtureKind.Null)
                .Select(stage => stage.GetProperty("Index").GetInt32()).SequenceEqual(new[] { 48, 49 }),
            "Unexpected historical room mismatch set.");
        FixtureValue stage = stages.Single(value => value.GetProperty("Index").GetInt32() == 49);
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var old = stage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
        CombatUnit sweep = before.Units.First();
        CombatUnit boss = before.Units.Single(unit => unit.AssetKey == "Boss_Level8_Fragile");
        Require(sweep.Status("sweep")?.Stacks == 1 && boss.BumpRules?.OuterBoss == true &&
            before.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnHit" &&
                trigger.Effects.Any(effect => effect.Type == "CardEffectDamagePerHp"))),
            "Missing the original sweep, retaliating ally or outer boss.");
        EnchantmentState Aura(RoomCombatState state) => state.Units.Single(unit => unit.Id == boss.Id)
            .Triggers.SelectMany(trigger => trigger.Effects).Single(effect => effect.Enchantment != null).Enchantment!.State;
        var native = Aura(actual).PreviewTargets.ToDictionary(target => target.UnitId);
        int[] differing = Aura(old).PreviewTargets.Where(target => target.NextAction != native[target.UnitId].NextAction)
            .Select(target => target.UnitId).ToArray();
        var remote = before.Context!.Enchantments!.Rooms.Where(room => room.RoomIndex != before.RoomIndex)
            .SelectMany(room => room.Units).Where(unit => unit.Health > 0).Select(unit => unit.Id).ToHashSet();
        Require(differing.Length == 3 && differing.All(id => remote.Contains(id) && native[id].NextAction == 0) &&
            Aura(old).PreviewTargets.Where(target => differing.Contains(target.UnitId)).All(target => target.NextAction == 2),
            "Missing the native remote preview-action witness.");

        FixtureValue[] observations = root.GetProperty("BossPreviewEnchantments").EnumerateArray().ToArray();
        var temporary = observations.Where(record => record.GetProperty("Operation").GetString() == "Temporary:True").ToArray();
        int[] sweepFrames = temporary.Where(record => record.GetProperty("Units").EnumerateArray().Last()
            .GetProperty("Id").GetInt32() == sweep.Id).Select(record => record.GetProperty("Frame").GetInt32()).ToArray();
        Require(sweepFrames.Length == 3 && sweepFrames.Distinct().Count() == 1 &&
            !observations.Any(record => record.GetProperty("Operation").GetString() == "AfterUpdate" &&
                record.GetProperty("SavePreview").GetBoolean() && record.GetProperty("Frame").GetInt32() == sweepFrames[0]),
            "Native sweep previews unexpectedly ran the queued aura-changing retaliation.");
        Require(observations.Any(record => record.GetProperty("Operation").GetString() == "AfterUpdate" &&
            record.GetProperty("SavePreview").GetBoolean() && record.GetProperty("Preview").EnumerateArray().Any(target =>
                target.GetProperty("Temporary").GetBoolean() && target.GetProperty("Preview").GetBoolean() &&
                (target.GetProperty("Destroyed").GetBoolean() || target.GetProperty("Dead").GetBoolean()))),
            "Missing temporary death removal while callbacks remain held.");

        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = Comparable(actual);
        Parallel.For(0, 8, _ =>
        {
            var result = RoomCombatModel.Exchange(before);
            Require(result.Supported, result.UnsupportedReason ?? "Sweep exchange unsupported.");
            string? difference = ModelJson.Difference(Comparable(result.State!), expected);
            Require(difference == null, "Native sweep preview difference: " + difference);
        });
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == immutable, "Sweep branches mutated their parent.");
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(root);
        Console.WriteLine("BOSS-PREVIEW-SWEEP PASS: held callbacks, temporary death removal, native remote aura maps, all room/train stages and eight immutable branches; historical failure counters preserved.");
    }

    private static string Comparable(RoomCombatState state) => JsonSerializer.Serialize(new
    {
        state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, state.Preview,
        state.ActiveCompanionBossIds, Context = BattleTurnChecks.ComparableContext(state.Context)
    });

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
