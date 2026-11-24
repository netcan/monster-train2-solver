using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSilverPostBattleChecks
{
    internal static void Pair(FixtureValue health, FixtureValue attack)
    {
        Check(health, distance: 1, attack: false);
        Check(attack, distance: 3, attack: true);
        Console.WriteLine("ORIGINAL-SILVER-POSTBATTLE PASS: current and max Pyre HP +5, persistent attack +5, one Battle RNG draw in each original controlled dispatch.");
    }

    private static void Check(FixtureValue entry, int distance, bool attack)
    {
        FixtureValue owner = entry.GetProperty("Owner"), victory = entry.GetProperty("CompletedVictory");
        string? scenario = Text(victory, "Id");
        var relic = new CombatRelicState(Text(owner, "Id")!, Text(owner, "Key")!,
            owner.GetProperty("Effects").EnumerateArray().Select(effect => effect.GetString()!).ToArray(),
            isCovenant: false, disallowedInPlacementPhase: false);
        Require(Text(entry, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(entry, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(entry, "Boundary") == "OriginalSilverPostBattleControlledDispatch" &&
            Number(entry, "CheckpointDistance") == distance && relic.AssetKey == "SilverT1" &&
            RelicModel.IsOriginalSilverPostBattleOwner(relic) && Number(owner, "Attack") == 5 && Number(owner, "Health") == 5 &&
            !owner.GetProperty("OnlyMiniboss").GetBoolean() && Guid.TryParse(scenario, out Guid scenarioId) && scenarioId != Guid.Empty &&
            Number(victory, "Index") < distance && victory.GetProperty("Won").GetBoolean() &&
            !victory.GetProperty("OuterBoss").GetBoolean(), "Expected the original SilverT1 and genuine prior ordinary victory.");
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
            Number(after, "PersistentAttack") == Number(before, "PersistentAttack") + (attack ? 5 : 0) &&
            Number(after, "Health") == Number(before, "Health") + (attack ? 0 : 5) &&
            Number(after, "Maximum") == Number(before, "Maximum") + (attack ? 0 : 5) &&
            Number(after, "ModifiedBaseAttack") == Number(before, "ModifiedBaseAttack"),
            "The original SilverT1 persistent Pyre fields differed after dispatch.");
    }

    private static UnityRng Rng(FixtureValue words) => new(words[0].GetUInt32(), words[1].GetUInt32(),
        words[2].GetUInt32(), words[3].GetUInt32());
    private static int Number(FixtureValue value, string field) => value.GetProperty(field).GetInt32();
    private static string? Text(FixtureValue value, string field) => value.GetProperty(field).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
