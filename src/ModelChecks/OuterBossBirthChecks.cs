using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossBirthChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Boundary").GetString() == "AfterCreateHeroStateBeforeInitialBossAction" &&
            fixture.GetProperty("Errors").GetArrayLength() == 0 && fixture.GetProperty("Births").GetArrayLength() == 1,
            "Initial boss placement requires one complete native creation boundary.");
        FixtureValue row = fixture.GetProperty("Births")[0];
        Require(row.GetProperty("NativeReturned").GetBoolean(), "Native boss birth did not return.");
        OuterBossCreationState before = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        if (row.TryGetProperty("Instantiation", out FixtureValue creation) && creation.ValueKind != FixtureKind.Null)
        {
            OuterBossInstantiationState input = creation.Deserialize<OuterBossInstantiationState>()!;
            string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
            OuterBossInstantiationResult instantiated = OuterBossInstantiationModel.Apply(input);
            Require(instantiated.Supported, "Boss instance creation unsupported: " + instantiated.UnsupportedReason);
            string? creationDiff = ModelJson.Difference(JsonSerializer.Serialize(instantiated.State, ModelJson.Options),
                JsonSerializer.Serialize(before, ModelJson.Options));
            Require(creationDiff == null && JsonSerializer.Serialize(input, ModelJson.Options) == immutable,
                "Boss instance creation differs or mutated input: " + creationDiff);
            Console.WriteLine("OUTER-BOSS-INSTANTIATION PASS: authored definition, shared identity, unplaced physical registration and complete created-actor/battle equality.");
        }
        OuterBossPhaseState actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        OuterBossPhaseResult result = OuterBossBirthModel.Apply(before);
        Require(result.Supported, "Initial boss placement unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, "Initial boss placement differs: " + difference);
        Require(before.CreatedBoss.Room == -1 && before.CreatedBoss.VisitedRooms.Count == 0 &&
            actual.Boss.Controller.InitialAction != null && actual.Boss.Actions[0][0].SetupAsInitialAction &&
            JsonSerializer.Serialize(before, ModelJson.Options) == parent,
            "Missing native unplaced creation, initial-action latch or immutable source.");
        int occupiedFloors = before.Battle.Spawn.Train.Rooms.Count(room => room.Units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre));
        int chosenPlayers = actual.Battle.Spawn.Train.Rooms.Single(room => room.RoomIndex == actual.Boss.Room).Units
            .Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
        CombatUnit born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        EnchantmentRule[] auras = born.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
            .Select(effect => effect.Enchantment!).ToArray();
        bool deferredAura = born.AssetKey is "Boss_Level8_Corrupter" or "Boss_Level8_Corrupter_TitanTrial" ||
            OuterBossDefinitions.IsAngelDuo(born.AssetKey) || OuterBossDefinitions.IsCael(born.AssetKey) ||
            OuterBossDefinitions.IsArkionPyre(born.AssetKey) || OuterBossDefinitions.IsArkionRage(born.AssetKey) ||
            OuterBossDefinitions.IsArkionPush(born.AssetKey) || OuterBossDefinitions.IsFleshweaver(born.AssetKey) ? auras.Length == 0 :
            auras.Length == 1 && auras[0].Bound && auras[0].State.PrimaryTargets.Count == 0 && auras[0].State.CachedStatus == null;
        Require(deferredAura && born.Triggers.Where(trigger => trigger.Effects.Any(effect => effect.Enchantment != null))
                .All(trigger => !trigger.HasTriggered) &&
                (before.CreatedBoss.Actions[0][0].Behavior == "RandomBossActionBehavior" || occupiedFloors == 0 || chosenPlayers > 0),
            "Native birth must bind its aura without firing AfterSpawnEnchant or eagerly applying targets.");
        Console.WriteLine($"OUTER-BOSS-BIRTH PASS: initial action, room {actual.Boss.Room}, {occupiedFloors} occupied floors/{chosenPlayers} selected allies, {auras.Length} deferred auras, physical birth and full battle/controller/RNG equality; immutable boundary.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
