using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifeLinkChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "NativeLifeLinkProtectionAndDamageStage" &&
            root.GetProperty("NativeStateReferencesRestored").GetBoolean() && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("WorldBefore").ContentEquals(root.GetProperty("WorldAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) &&
            root.GetProperty("RngBefore").GetProperty("Streams").GetArrayLength() == 16 &&
            root.GetProperty("RngBefore").GetProperty("Unity").GetArrayLength() == 4 &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")),
            "Life-link controls changed native world/state references, preview settings, frame or RNG.");
        Require(root.GetProperty("WorldBefore").GetProperty("Characters").EnumerateArray()
            .Select(actor => actor.GetProperty("AssetKey").GetString()).SequenceEqual(TrueFinalBossChecks.Assets),
            "Life-link controls did not use the five original native actors.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 1640, "Missing native life-link matrix/dead controls.");
        var seen = new HashSet<(int, int, int, int, int, int)>();
        int stages = 0, dead = 0, sameGroup = 0, predictionDifference = 0, previewDeltaWitness = 0, protectedLethal = 0;
        LifeLinkContext[] sharedRoots = rows.Select(row => row.GetProperty("Context").Deserialize<LifeLinkContext>()!).ToArray();
        string[] immutable = sharedRoots.Select(context => Json(context)).ToArray();
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index]; LifeLinkContext context = sharedRoots[index];
            int target = row.GetProperty("TargetId").GetInt32(), mode = row.GetProperty("Mode").GetInt32(), mask = row.GetProperty("Mask").GetInt32();
            bool protection = row.GetProperty("Protected").GetBoolean(), prediction = row.GetProperty("ProtectedForPrediction").GetBoolean();
            Require(context.Actors.Count == 5 && context.Actors.Select(actor => actor.Id).SequenceEqual(new[] { 1, 2, 3, 4, 5 }) &&
                context.Actors.Select(actor => actor.LinkedId).SequenceEqual(new[] { 0, 0, 0, 3, 3 }) &&
                context.Actors.All(actor => actor.Initialized && actor.TrueFinalBoss && actor.OuterBoss == (actor.Id >= 3) &&
                    actor.HasLifeLink == ((mask & (1 << (actor.Id - 1))) != 0) && actor.PreviewHealth == (mode == 0 ? actor.Health : 1)) &&
                context.CalculatingPreview == (mode >= 2) && context.SharedPreviewHpChange == (mode == 2 ? 1 : mode == 3 ? 3500 : 0),
                "Native life-link identity, role, preview or detached status control differs.");
            Require(LifeLinkModel.Validate(context) == null && LifeLinkModel.Protected(context, target) == protection &&
                LifeLinkModel.Protected(context, target, true) == prediction, "Native life-link protection query differs.");
            FixtureValue[] damage = row.GetProperty("Damage").EnumerateArray().ToArray();
            Require(damage.Select(item => item.GetProperty("Input").GetInt32()).SequenceEqual(new[] { 0, 1, 9, 4000, int.MaxValue, int.MinValue }),
                "Native life-link damage inputs differ.");
            foreach (FixtureValue item in damage)
            {
                int amount = item.GetProperty("Input").GetInt32();
                LifeLinkDamageResult actual = item.GetProperty("Actual").Deserialize<LifeLinkDamageResult>()!;
                LifeLinkDamageResult result = LifeLinkModel.Test(context, target, amount, -77, -88);
                Require(Json(actual) == Json(result), "Native life-link damage/trigger/prevented output differs.");
                if (protection && amount == 4000 && context.Actors[target - 1].Health > 0)
                { Require(result.Damage == context.Actors[target - 1].Health - 1, "Protected lethal damage did not retain one health."); protectedLethal++; }
                stages++;
            }
            if (row.GetProperty("Label").GetString() == "matrix")
                Require(seen.Add((context.Actors[0].Health, context.Actors[1].Health, context.Actors[2].Health, mask, mode, target)),
                    "Duplicate native life-link matrix row.");
            else
            {
                Require(row.GetProperty("Label").GetString() == "dead-targets" && context.Actors.All(actor => actor.Health == 0) &&
                    damage.All(item => item.GetProperty("Actual").GetProperty("Damage").GetInt32() == -77 &&
                        item.GetProperty("Actual").GetProperty("Prevented").GetInt32() == -88 &&
                        !item.GetProperty("Actual").GetProperty("Triggered").GetBoolean()), "Dead life-link target changed output sentinels.");
                dead++;
            }
            if (mask == 28 && target >= 3) { Require(!protection && !prediction, "Entropy body parts incorrectly protected their own health group."); sameGroup++; }
            if (protection != prediction) predictionDifference++;
            if (mode == 3 && mask == 31 && target == 1 && context.Actors[1].Health == 1 && context.Actors[2].Health == 3500)
            { Require(!protection && !prediction, "Shared preview damage was not subtracted before checking other Entropy health."); previewDeltaWitness++; }
        }
        var expected = (from dominion in new[] { 1, 2, 1555 } from savagery in new[] { 1, 2, 2750 }
            from entropy in new[] { 1, 2, 3500 } from mask in new[] { 31, 28, 0 } from mode in Enumerable.Range(0, 4)
            from target in Enumerable.Range(1, 5) select (dominion, savagery, entropy, mask, mode, target)).ToHashSet();
        Require(seen.SetEquals(expected) && stages == 9840 && dead == 20 && sameGroup > 0 && predictionDifference > 0 &&
            previewDeltaWitness > 0 && protectedLethal > 0, "Incomplete native life-link protection, alias exclusion or preview witnesses.");
        Parallel.For(0, 512, index =>
        {
            int rowIndex = (index % 32) * 47 % rows.Length;
            FixtureValue row = rows[rowIndex]; LifeLinkContext context = sharedRoots[rowIndex];
            int target = row.GetProperty("TargetId").GetInt32();
            FixtureValue damage = row.GetProperty("Damage")[(index / 32) % 6];
            Require(LifeLinkModel.Protected(context, target) == row.GetProperty("Protected").GetBoolean() &&
                Json(LifeLinkModel.Test(context, target, damage.GetProperty("Input").GetInt32(), -77, -88)) ==
                    Json(damage.GetProperty("Actual").Deserialize<LifeLinkDamageResult>()!), "Concurrent life-link branch differs from native output.");
        });
        Require(sharedRoots.Select(context => Json(context)).SequenceEqual(immutable) &&
            !LifeLinkModel.Protected(sharedRoots[0], 999) && !LifeLinkModel.Test(sharedRoots[0], 999, 1).Triggered &&
            LifeLinkModel.Validate(new LifeLinkContext([new(1, 2, 2, 2, true, true, true, true)], false, 0)) != null &&
            LifeLinkModel.Validate(new LifeLinkContext([new(1, 0, 2, 2, true, true, true, true), new(2, 1, 3, 2, true, true, true, true)], false, 0)) != null &&
            TrueFinalBossChecks.Assets.All(asset => !OuterBossDefinitions.Supports(asset)), "Life-link source mutation, malformed group acceptance or premature full-battle admission.");
        Console.WriteLine("LIFE-LINK PASS: 1640 native contexts/3280 protection queries/9840 damage stages, Entropy alias exclusion, real vs prediction HP, shared preview damage, dead-output preservation and 512 concurrent immutable branches. Full battle remains guarded.");
    }
}
