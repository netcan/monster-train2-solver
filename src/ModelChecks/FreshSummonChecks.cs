using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FreshSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var setups = fixture.GetProperty("FreshSpawners").EnumerateArray().ToArray();
        var births = fixture.GetProperty("UnitBirths").EnumerateArray().ToArray();
        var upgrades = fixture.GetProperty("SpawnUpgrades").EnumerateArray().ToArray();
        var globals = fixture.GetProperty("GlobalStandbyChecks").EnumerateArray().ToArray();
        var phases = fixture.GetProperty("RallyPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("RallyTriggers").EnumerateArray().ToArray();
        Require(setups.Length >= 7 && upgrades.Length == setups.Length && globals.Length > 0 &&
            fixture.GetProperty("DetachedCardClones").GetArrayLength() == 0, "Fresh source creation was not exercised independently of cloning.");
        var freshIds = setups.Select(item => item.GetProperty("CardId").GetInt32()).ToHashSet();
        var freshBirths = births.Where(item => freshIds.Contains(item.GetProperty("SpawnerCardId").GetInt32())).ToArray();
        Require(freshBirths.Length == setups.Length && freshBirths.Any(item => !item.GetProperty("IsCardless").GetBoolean()) &&
            freshBirths.Any(item => item.GetProperty("IsCardless").GetBoolean()), "Fresh sources incorrectly imply cardless for the first birth.");
        Require(freshBirths.All(item => !item.GetProperty("Before").Deserialize<RoomCombatState>()!.Context!
            .FindCard(item.GetProperty("SpawnerCardId").GetInt32())!.Temporary.Upgrades.Any()),
            "A new source inherited an earlier birth's temporary upgrades.");
        var actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Any(item =>
        {
            var after = item.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            return after.OtherPiles.Any(pile => pile.UnitConditions?.Any(binding => after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id == binding.HostUnitId && unit.SpawnerCardId != binding.CardId)) == true);
        }), "Original resolving card was not bound to a unit with a different source.");
        if (fixture.GetProperty("ModifierScenario").GetString() == "multi-summon-fresh-deaths")
            Require(globals.Any(item => item.GetProperty("Before").Deserialize<CombatContext>()!.OtherPiles!
                .Any(pile => pile.UnitConditions?.Any(binding => binding.Ready) == true)), "No delayed bound-unit death was recorded.");
        Verify();
        Parallel.For(0, 32, _ => Verify());
        void Verify()
        {
            foreach (var sample in setups) VerifySetup(sample);
            foreach (var sample in births) UnitSummonChecks.VerifyBirth(sample);
            foreach (var sample in upgrades) UnitSummonChecks.VerifyExtra(sample);
            foreach (var sample in globals) VerifyGlobal(sample);
            foreach (var sample in phases) RallyChecks.VerifyPhase(sample);
            foreach (var sample in triggers) RallyChecks.VerifyTrigger(sample);
        }
        Console.WriteLine($"NATIVE-FRESH-SUMMON-CHECKS PASS: {setups.Length} fresh setups, {births.Length} births, {upgrades.Length} extra upgrades, {globals.Length} global standby checks, original-card bindings, fresh histories, source/cardless separation and 32 branches.");
    }
    private static void VerifySetup(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native fresh setup.");
        var before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
        var after = sample.GetProperty("After").Deserialize<CombatContext>()!;
        string parent = Serialize(before);
        var result = CardGenerationModel.CreateDetached(before, sample.GetProperty("Creation").Deserialize<CardCreationRule>()!);
        Require(result.Supported && Serialize(result.Context) == Serialize(after) && result.AddedCards.Single().InstanceId == sample.GetProperty("CardId").GetInt32(),
            "Independent fresh setup differs: " + result.UnsupportedReason);
        var card = after.FindCard(sample.GetProperty("CardId").GetInt32())!;
        Require(card.Temporary.Upgrades.Count == 0 && card.PlayCount == 0 && card.LastPlayedCost == 0 &&
            !after.CardInstances!.Any(item => item.InstanceId == card.InstanceId) &&
            Serialize(before.Cards) == Serialize(after.Cards) && Serialize(before.OtherPiles) == Serialize(after.OtherPiles) &&
            Serialize(before.Statistics) == Serialize(after.Statistics) && Serialize(before.BattleRng) == Serialize(after.BattleRng) &&
            Serialize(before.NextAddedTemporaryUpgrades) == Serialize(after.NextAddedTemporaryUpgrades),
            "Fresh setup copied history, acquired ownership or changed unrelated generation state.");
        Require(Serialize(before) == parent, "Fresh setup mutated its parent.");
    }
    private static void VerifyGlobal(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native global standby check.");
        var before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
        var after = sample.GetProperty("After").Deserialize<CombatContext>()!;
        string parent = Serialize(before);
        Require(Serialize(UnitStandbyModel.ReturnReady(before)) == Serialize(after), "Independent global standby return differs from native.");
        Require(Serialize(before) == parent, "Global standby return mutated its parent.");
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
