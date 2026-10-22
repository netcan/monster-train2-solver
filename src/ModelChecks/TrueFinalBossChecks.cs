using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossChecks
{
    internal static readonly string[] Assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2",
        "Boss_Level9_Entropy_Bot_V2", "Boss_Level9_Entropy_Mid_V2", "Boss_Level9_Entropy_Top_V2" };
    private static readonly int[] Health = { 1555, 2750, 3500, 3500, 3500 };
    private static readonly int[] Attack = { 3, 10, 15, 15, 15 };
    private static readonly int[] Rooms = { 3, 0, 0, 1, 2 };
    private static readonly string[] DataIds = { "3efb40d1-88e8-471b-bccf-db0dea68db93", "f8c4fab5-c674-43f8-8ef5-e09047befa92",
        "dc2743cb-0019-41c8-9823-b9b1ec80daad", "745eab49-3a04-4e5f-bb17-5a243bfd3a1a", "a805c0e7-3279-46e6-b1f6-b3676dc75312" };
    private static int[] Ints(FixtureValue value) => value.EnumerateArray().Select(item => item.GetInt32()).ToArray();
    internal static void Birth(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("EvidenceKind").GetString() == "NativeObservationOnly" &&
            root.GetProperty("Boundary").GetString() == "AfterDoTrueFinalBossSpawning" &&
            root.GetProperty("NativeReturned").GetBoolean() && root.GetProperty("ReadOnlyCapture").GetBoolean() &&
            root.GetProperty("RngBeforeCapture").ContentEquals(root.GetProperty("RngAfterCapture")),
            "Missing completed read-only original true-final-boss birth boundary.");
        FixtureValue route = root.GetProperty("Route");
        Require(route.GetProperty("Kind").GetString() == "NativeCheatJumpToTFB" &&
            !route.GetProperty("UnlockOverrideBefore").GetBoolean() && route.GetProperty("UnlockOverrideAfter").GetBoolean() &&
            route.GetProperty("DistanceBefore").GetInt32() == 0 && route.GetProperty("DistanceAfter").GetInt32() == 8 &&
            route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80,
            "True-final-boss evidence must disclose its native unlock/route control and unchanged original assets.");
        FixtureValue rngBefore = route.GetProperty("RngBefore"), rngAfter = route.GetProperty("RngAfter");
        Require(rngBefore.GetArrayLength() == 16 && rngAfter.GetArrayLength() == 16 &&
            rngBefore.EnumerateArray().Zip(rngAfter.EnumerateArray()).All(pair =>
                pair.First.GetProperty("Id").GetString() == pair.Second.GetProperty("Id").GetString() &&
                (pair.First.GetProperty("Id").GetString() == "NonDeterministic" || pair.First.ContentEquals(pair.Second))),
            "The native route control changed a gameplay RNG stream.");
        FixtureValue actual = root.GetProperty("Actual"), created = root.GetProperty("Creation"), actors = actual.GetProperty("Characters");
        int[] queued = Ints(root.GetProperty("QueuedBefore"));
        Require(queued.Order().SequenceEqual(new[] { 0, 1, 2 }) && !queued.SequenceEqual(new[] { 0, 1, 2 }) &&
            actual.GetProperty("QueuedBosses").GetArrayLength() == 0 && actual.GetProperty("OuterBossId").GetInt32() == 5 &&
            Ints(actual.GetProperty("ActiveBossIds")).SequenceEqual(new[] { 1, 2, 3, 4, 5 }) &&
            created.GetArrayLength() == 5 && actors.GetArrayLength() == 5,
            "Missing shuffled queue, original creation order, active identities or cleared queue.");
        for (int i = 0; i < Assets.Length; i++)
        {
            FixtureValue creation = created[i], actor = actors[i];
            Require(creation.GetProperty("Id").GetInt32() == i + 1 && creation.GetProperty("Requested").GetString() == Assets[i] &&
                creation.GetProperty("Used").GetString() == Assets[i] && creation.GetProperty("DataId").GetString() == DataIds[i] &&
                creation.GetProperty("OriginalAttack").GetInt32() == Attack[i] && creation.GetProperty("OriginalHealth").GetInt32() == Health[i] &&
                creation.GetProperty("Health").GetInt32() == Health[i] && creation.GetProperty("MaxHealth").GetInt32() == Health[i] &&
                creation.GetProperty("Attack").GetInt32() == Attack[i] && creation.GetProperty("Room").GetInt32() == -1 &&
                creation.GetProperty("LinkedId").GetInt32() == 0,
                "Original actor creation or pre-placement state differs: " + Assets[i]);
            Require(actor.GetProperty("Id").GetInt32() == i + 1 && actor.GetProperty("AssetKey").GetString() == Assets[i] &&
                actor.GetProperty("DataId").GetString() == DataIds[i] && actor.GetProperty("Room").GetInt32() == Rooms[i] &&
                actor.GetProperty("LinkedId").GetInt32() == (i >= 3 ? 3 : 0) &&
                actor.GetProperty("Health").GetInt32() == Health[i] && actor.GetProperty("MaxHealth").GetInt32() == Health[i] &&
                actor.GetProperty("Attack").GetInt32() == Attack[i] && actor.GetProperty("CanAttack").GetBoolean() &&
                actor.GetProperty("IsTrueFinalBoss").GetBoolean() && actor.GetProperty("IsOuterTrainBoss").GetBoolean() == (i >= 2) &&
                actor.GetProperty("Phase").GetString() == "Casting" && Ints(actor.GetProperty("PhysicalRooms")).SequenceEqual(new[] { Rooms[i] }),
                "Original placement, physical membership or Entropy link differs: " + Assets[i]);
            Require(!OuterBossDefinitions.Supports(Assets[i]), "Birth observation must not open the Level8 battle model to Level9.");
        }
        FixtureValue rooms = actual.GetProperty("Rooms");
        Require(rooms.GetArrayLength() == 4, "Missing original room layout.");
        for (int room = 0; room < 4; room++)
            Require(rooms[room].GetProperty("Index").GetInt32() == room && rooms[room].GetProperty("Enabled").GetBoolean() &&
                rooms[room].GetProperty("OuterBossId").GetInt32() == (room < 3 ? room + 3 : 0), "Native Entropy anchor registration differs.");
        Console.WriteLine("TRUE-FINAL-BOSS-BIRTH PASS: five original definitions/identities, shuffled queue vs fixed creation order, four physical rooms, Entropy links/anchors and pure RNG capture. Observation only; full battle remains guarded.");
    }
    internal static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
