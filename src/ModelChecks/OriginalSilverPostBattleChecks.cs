using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSilverPostBattleChecks
{
    internal static void Natural(FixtureValue entry)
    {
        var owner = entry.GetProperty("Owner");
        var relic = new CombatRelicState(Text(owner, "Id")!, Text(owner, "Key")!,
            owner.GetProperty("Effects").EnumerateArray().Select(effect => effect.GetString()!).ToArray(),
            isCovenant: false, disallowedInPlacementPhase: false);
        var callbacks = entry.GetProperty("CallbackScenarioIds");
        Require(Text(entry, "GameVersion") == Game231FixtureCatalog.Version && Text(entry, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(entry, "Boundary") == "OriginalSilverNaturalPostBattle" && relic.AssetKey == "SilverT1" &&
            RelicModel.IsOriginalSilverPostBattleOwner(relic) && entry.GetProperty("Won").GetBoolean() &&
            Number(entry, "BattleCompletions") == 1 && callbacks.GetArrayLength() == 1 &&
            callbacks[0].GetString() == Text(entry, "CompletedScenarioId") &&
            Text(entry, "CompletedScenario") == "SoulSavior_R0_Battle_Morsels" &&
            Text(entry, "NextScenario") == "SoulSavior_R1_Battle_HealOnShiftHeavy" && Number(entry, "NextDistance") == 1,
            "Expected one Silver callback from the actual prologue victory and the next original battle intro.");
        var before = entry.GetProperty("BeforeCallback");
        var after = entry.GetProperty("AfterCallback");
        var next = entry.GetProperty("NextIntro");
        FixtureValue rngBefore = before.GetProperty("Rng"), rngAfter = after.GetProperty("Rng");
        RngDraw draw = Rng(rngBefore.GetProperty("Battle")).Range(0, 2);
        Require(draw.Value == 1 && Rng(rngAfter.GetProperty("Battle")).Equals(draw.State) &&
            rngBefore.EnumerateObject().Where(stream => stream.Name != "Battle").All(stream =>
                stream.Value.ContentEquals(rngAfter.GetProperty(stream.Name))), "Silver must advance only Battle RNG during its callback.");
        Require(Text(entry, "Outcome") == "CurrentAndMaxHealth" &&
            Number(before, "Health") == 50 && Number(before, "Maximum") == 50 &&
            Number(after, "Health") == 55 && Number(after, "Maximum") == 55 &&
            new[] { "Health", "Maximum", "PersistentAttack", "ModifiedBaseAttack" }.All(field =>
                after.GetProperty(field).ContentEquals(next.GetProperty(field))) &&
            Number(entry, "SavedPersistentAttack") == Number(next, "PersistentAttack"),
            "Expected the 50-to-55 Pyre HP change to persist through the original next intro.");
        Console.WriteLine("ORIGINAL-SILVER-NATURAL PASS: one native callback after prologue victory; 55/55 Pyre reaches the next battle intro.");
    }

    internal static void Pair(FixtureValue health, FixtureValue attack)
    {
        var healthOwner = Check(health, distance: 1, attack: false);
        var attackOwner = Check(attack, distance: 3, attack: true);
        Require(healthOwner == attackOwner, "Silver receipts must have the same original tier.");
        Console.WriteLine($"ORIGINAL-SILVER-POSTBATTLE PASS: {healthOwner.Key} current and max Pyre HP +{healthOwner.Amount}, persistent attack +{attackOwner.Amount}, one Battle RNG draw in each original controlled dispatch.");
    }

    private static (string Key, int Amount) Check(FixtureValue entry, int distance, bool attack)
    {
        FixtureValue owner = entry.GetProperty("Owner"), victory = entry.GetProperty("CompletedVictory");
        string? scenario = Text(victory, "Id");
        var relic = new CombatRelicState(Text(owner, "Id")!, Text(owner, "Key")!,
            owner.GetProperty("Effects").EnumerateArray().Select(effect => effect.GetString()!).ToArray(),
            isCovenant: false, disallowedInPlacementPhase: false);
        (int tier, int amount) = relic.AssetKey switch
        {
            "SilverT1" => (1, 5),
            "SilverT2" => (2, 7),
            "SilverT3" => (3, 10),
            _ => throw new InvalidOperationException("Expected an original Silver owner.")
        };
        int observedTier = owner.TryGetProperty("Tier", out FixtureValue explicitTier) ? explicitTier.GetInt32() : 1;
        Require(Number(entry, "Schema") == 1 && Text(entry, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(entry, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(entry, "Boundary") == "OriginalSilverPostBattleControlledDispatch" &&
            Number(entry, "CheckpointDistance") == distance && observedTier == tier &&
            RelicModel.IsOriginalSilverPostBattleOwner(relic) && Number(owner, "Attack") == amount && Number(owner, "Health") == amount &&
            !owner.GetProperty("OnlyMiniboss").GetBoolean() && Guid.TryParse(scenario, out Guid scenarioId) && scenarioId != Guid.Empty &&
            Number(victory, "Index") < distance && victory.GetProperty("Won").GetBoolean() &&
            !victory.GetProperty("OuterBoss").GetBoolean(), "Expected the exact original Silver tier and genuine prior ordinary victory.");
        var callbacks = entry.GetProperty("CallbackScenarioIds");
        var before = entry.GetProperty("Before");
        var after = entry.GetProperty("AfterVictory");
        Require(callbacks.GetArrayLength() == 2 && callbacks[0].ValueKind == FixtureKind.Null &&
            callbacks[1].GetString() == scenario && before.ContentEquals(entry.GetProperty("AfterNull")),
            "Expected exact native null and completed-scenario dispatches.");
        FixtureValue rngBefore = before.GetProperty("Rng"), rngAfter = after.GetProperty("Rng");
        RngDraw draw = Rng(rngBefore.GetProperty("Battle")).Range(0, 2);
        Require(draw.Value == (attack ? 0 : 1) && Rng(rngAfter.GetProperty("Battle")).Equals(draw.State) &&
            rngBefore.EnumerateObject().Where(stream => stream.Name != "Battle").All(stream =>
                stream.Value.ContentEquals(rngAfter.GetProperty(stream.Name))),
            "Expected only one Battle RNG draw with the corresponding stat selection.");
        Require(Text(entry, "Outcome") == (attack ? "PersistentAttack" : "CurrentAndMaxHealth") &&
            Number(after, "PersistentAttack") == Number(before, "PersistentAttack") + (attack ? amount : 0) &&
            Number(after, "Health") == Number(before, "Health") + (attack ? 0 : amount) &&
            Number(after, "Maximum") == Number(before, "Maximum") + (attack ? 0 : amount) &&
            Number(after, "ModifiedBaseAttack") == Number(before, "ModifiedBaseAttack"),
            "The original Silver persistent Pyre fields differed after dispatch.");
        return (relic.AssetKey, amount);
    }

    private static UnityRng Rng(FixtureValue words) => new(words[0].GetUInt32(), words[1].GetUInt32(),
        words[2].GetUInt32(), words[3].GetUInt32());
    private static int Number(FixtureValue value, string field) => value.GetProperty(field).GetInt32();
    private static string? Text(FixtureValue value, string field) => value.GetProperty(field).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
