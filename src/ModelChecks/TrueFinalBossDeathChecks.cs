using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossDeathChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
        int[] Ids(FixtureValue value) => value.EnumerateArray().Select(actor => actor.GetProperty("Id").GetInt32()).ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "ControlledOriginalTrueFinalBossLethalDamageAndRemoval" &&
            root.GetProperty("Setup").GetString() == "OriginalDebuffHPToOne" && !root.GetProperty("TargetProtected").GetBoolean() &&
            root.GetProperty("NativeDamageReturned").GetBoolean() && root.GetProperty("NativeRemovalReturned").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("AllScenarioBossesDead").GetBoolean() &&
            root.GetProperty("PyreHealth").GetInt32() == 80 && root.GetProperty("RngBeforeControl").GetArrayLength() == 16 &&
            root.GetProperty("RngAfterDamage").GetArrayLength() == 16,
            "Missing completed controlled native lethal damage plus separate removal phase.");
        string[] names = { "Dominion", "Savagery", "EntropyBot", "EntropyMid", "EntropyTop" };
        int targetIndex = Array.IndexOf(names, root.GetProperty("Target").GetString());
        FixtureValue[] original = root.GetProperty("Original").EnumerateArray().ToArray();
        FixtureValue[] controlled = root.GetProperty("Controlled").EnumerateArray().ToArray();
        Require(targetIndex >= 0 && original.Length == 5 && controlled.Length == 5 &&
            original.Select(actor => actor.GetProperty("AssetKey").GetString()).SequenceEqual(TrueFinalBossChecks.Assets) &&
            original.Select(actor => actor.GetProperty("Health").GetInt32()).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }) &&
            original.Select(actor => actor.GetProperty("Room").GetInt32()).SequenceEqual(new[] { 3, 0, 0, 1, 2 }) &&
            Ids(root.GetProperty("Original")).SequenceEqual(Ids(root.GetProperty("Controlled"))) &&
            controlled.All(actor => actor.GetProperty("Health").GetInt32() == 1) &&
            controlled.Select(actor => actor.GetProperty("MaxHealth").GetInt32()).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }),
            "Controlled death changed original definitions/max HP/placement or omitted its explicit one-HP setup.");
        int target = root.GetProperty("TargetId").GetInt32();
        Require(target == original[targetIndex].GetProperty("Id").GetInt32(), "Native death target identity differs.");
        FixtureValue afterDamage = root.GetProperty("AfterDamageBeforeRemoval");
        FixtureValue[] damaged = afterDamage.GetProperty("Actors").EnumerateArray().ToArray();
        Require(afterDamage.GetProperty("Roots").GetInt32() == 0 && afterDamage.GetProperty("Steps").GetInt32() == 0 &&
            afterDamage.GetProperty("AllScenarioBossesDead").GetBoolean() &&
            afterDamage.GetProperty("Active").EnumerateArray().Select(value => value.GetInt32()).SequenceEqual(Ids(root.GetProperty("Original"))) &&
            afterDamage.GetProperty("FinishedDying").EnumerateArray().Select(value => value.GetInt32()).SequenceEqual(new[] { target }) &&
            damaged.Select((actor, index) => actor.GetProperty("Health").GetInt32() == (targetIndex >= 2 ? index >= 2 ? 0 : 1 : index == targetIndex ? 0 : 1)).All(value => value),
            "Native lethal damage/removal phases were conflated or linked owner damage differs.");
        Require(root.GetProperty("Roots").GetArrayLength() == 1 && root.GetProperty("Steps").GetArrayLength() == 5 &&
            root.GetProperty("ActiveAfter").GetArrayLength() == 0, "Native true-final-boss group did not remove all five actors.");
        FixtureValue removal = root.GetProperty("Roots")[0];
        var actors = removal.GetProperty("Before").Deserialize<TrueFinalBossRemovalActor[]>()!;
        string immutable = Json(actors);
        TrueFinalBossRemovalPlan Plan() => TrueFinalBossRemovalModel.Plan(actors, removal.GetProperty("TargetId").GetInt32(),
            removal.GetProperty("Death").GetBoolean(), removal.GetProperty("Vanish").GetBoolean(), removal.GetProperty("LastDeath").GetBoolean(),
            removal.GetProperty("IgnoreTriggers").GetBoolean(), removal.GetProperty("DoNotFullyRemove").GetBoolean(), removal.GetProperty("FireRemovedSignal").GetBoolean());
        TrueFinalBossRemovalPlan result = Plan();
        TrueFinalBossRemovalStep[] actual = root.GetProperty("Steps").Deserialize<TrueFinalBossRemovalStep[]>()!;
        Require(removal.GetProperty("NativeReturned").GetBoolean() && removal.GetProperty("TargetId").GetInt32() == target &&
            removal.GetProperty("StartStep").GetInt32() == 0 && removal.GetProperty("EndStep").GetInt32() == 5 &&
            result.Supported && Json(result.Steps) == Json(actual) && result.ActiveAfter.Count == 0 &&
            result.Steps.Select(step => step.Room).SequenceEqual(new[] { 3, 2, 1, 0, 0 }) &&
            actual.Count(step => step.LastDeath) == (removal.GetProperty("LastDeath").GetBoolean() ? 1 : 0),
            "Native removal membership, top-down order, equal-floor peers, active list or forwarded flags differs.");
        Parallel.For(0, 64, _ => Require(Json(Plan().Steps) == Json(actual), "Concurrent removal plan differs from native group order."));
        Require(Json(actors) == immutable && !TrueFinalBossRemovalModel.Plan(actors, 999, true, true, true).Supported &&
            !TrueFinalBossRemovalModel.Plan(actors.Skip(1).ToArray(), target, true, true, true).Supported &&
            TrueFinalBossChecks.Assets.All(asset => !OuterBossDefinitions.Supports(asset)),
            "Removal planning mutated its parent, accepted missing group/target or opened the Level8 controller.");
        Console.WriteLine($"TRUE-FINAL-BOSS-DEATH PASS target={names[targetIndex]}: explicit original one-HP control, native lethal/deferred cleanup phases, all five removed in 3/2/1/0/0 order, flags/active lists and 64 immutable branches. NativeWon={root.GetProperty("NativeWon").ValueKind}; controlled component only.");
    }
}
