using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThikDeckChangeChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        if (root.TryGetProperty("OriginalThikPrepared", out var prepared) && prepared.GetBoolean())
        {
            Battle(path, root);
            return;
        }
        var definition = root.GetProperty("Definition");
        Require(Text(root, "Boundary") == "NativeOriginalThikDeckChange" && Text(root, "GameVersion") == "2.3.1" &&
            Text(root, "GameModuleMvid") == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            Text(definition, "Id") == "490d737b-fa0d-4f16-bb90-4ff22af11530" &&
            Text(definition, "Key") == "ThikT3" && Int(definition, "Tier") == 3, "Missing the original Thik T3 capture.");
        var effects = definition.GetProperty("Effects").EnumerateArray().ToArray();
        var copies = effects.Single(effect => Text(effect, "Type") == "RelicEffectGetCardCopiesOnDeck");
        var growth = effects.Single(effect => Text(effect, "Type") == "RelicEffectAddPyreAttackAndHealthPerCardInDeck");
        Require(effects.Length == 2 && Int(copies, "Int") == 1 && Int(growth, "Int") == 2 && Int(growth, "Int2") == 5 &&
            Int(growth, "Conditions") == 0, "The original Thik T3 payload differs.");

        var acquisitionBefore = root.GetProperty("AcquisitionBefore");
        var acquisitionAfter = root.GetProperty("AcquisitionAfter");
        int count = Int(acquisitionBefore, "RawDeckCount");
        Require(acquisitionBefore.GetProperty("CachedDeckCount").ValueKind == FixtureKind.Null &&
            !Flag(acquisitionBefore, "HealthBuffApplied") && Flag(acquisitionAfter, "HealthBuffApplied") &&
            Int(acquisitionAfter, "CachedDeckCount") == count && Int(acquisitionAfter, "RawDeckCount") == count,
            "Initial soul attachment did not settle the permanent starting deck.");
        CheckDelta(acquisitionBefore, acquisitionAfter, count, inBattle: false);

        var callbacks = root.GetProperty("Callbacks").EnumerateArray().ToArray();
        var changes = callbacks.Where(row => Int(row, "Delta") != 0).ToArray();
        Require(changes.Select(row => Int(row, "Delta")).SequenceEqual(new[] { 1, -1 }) &&
            changes.Select(row => Int(row, "PlayNumber")).SequenceEqual(new[] { 1, 2 }),
            "A permanent card must be added once and then removed once.");
        foreach (var callback in callbacks)
        {
            var before = callback.GetProperty("Before");
            var after = callback.GetProperty("After");
            int delta = Int(callback, "Delta");
            Require(Int(before, "RawDeckCount") - Int(before, "CachedDeckCount") == delta &&
                Int(after, "CachedDeckCount") == Int(after, "RawDeckCount") &&
                Int(after, "RawDeckCount") == Int(before, "RawDeckCount") &&
                !Flag(after, "SourceInSavedDeck"), "Native deck membership or the callback cache differs.");
            CheckDelta(before, after, delta, inBattle: true);
        }

        var settled = root.GetProperty("SettledPlays").EnumerateArray().ToArray();
        Require(settled.Length == 2 && Flag(settled[0], "PurgeCardInSavedDeck") && !Flag(settled[1], "PurgeCardInSavedDeck") &&
            settled.All(row => Flag(row, "RunningCombat") && !Flag(row, "AllScenarioBossesDead") && Int(row, "PyreHP") > 0),
            "Both native plays must settle while the train is active.");
        Console.WriteLine($"THIK-DECK-CHANGE PASS callbacks={callbacks.Length} permanentChanges=2");
    }

    private static void Battle(string path, FixtureValue root)
    {
        var definition = root.GetProperty("OriginalThikDefinition");
        Require(Text(definition, "Id") == "490d737b-fa0d-4f16-bb90-4ff22af11530" && Int(definition, "Tier") == 3,
            "Expected the original Thik T3 in the full battle.");
        var callbacks = root.GetProperty("OriginalThikCallbacks").EnumerateArray().ToArray();
        Require(callbacks.Select(row => Int(row, "Delta")).SequenceEqual(new[] { 1, 0, -1 }),
            "The native battle must record an add, an unchanged temporary removal, and a permanent removal.");
        foreach (var callback in callbacks)
            CheckDelta(callback.GetProperty("Before"), callback.GetProperty("After"), Int(callback, "Delta"), inBattle: true);

        var cards = root.GetProperty("OriginalThikCards");
        int generator = Int(cards, "GeneratorId"), purge = Int(cards, "PurgeCardId");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        CheckAction(generator, 1, 5, 2);
        CheckAction(purge, -1, -5, -2);
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine("THIK-DECK-CHANGE-BATTLE PASS: permanent addition, temporary removal and permanent purge agree with native successors.");

        void CheckAction(int cardId, int delta, int healthDelta, int attackDelta)
        {
            var row = actions.Single(action => Int(action.GetProperty("Action"), "CardInstanceId") == cardId);
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train;
            CombatUnit pyreBefore = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
            CombatUnit pyreAfter = after.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
            Require(after.Context!.Statistics!.DeckCards!.Count - before.Context!.Statistics!.DeckCards!.Count == delta &&
                pyreAfter.Health - pyreBefore.Health == healthDelta && pyreAfter.MaxHealth == pyreBefore.MaxHealth &&
                after.Context.Resources!.PyreModifiedBaseAttack - before.Context.Resources!.PyreModifiedBaseAttack == attackDelta,
                "The modeled decision states lost a native deck mutation or Pyre update.");
        }
    }

    private static void CheckDelta(FixtureValue before, FixtureValue after, int delta, bool inBattle)
    {
        int hpChange = delta * 5;
        int lastBaseMaximum = Int(before, "RawSaveMaxHP");
        int lastMaximum = Int(before, "SaveMaxHP");
        var (rawMaximum, health, attack) = RelicResourceModel.AdjustPyreForDeckChange(lastBaseMaximum,
            lastMaximum, Int(before, "SaveHP"), Int(before, "SaveModifiedAttack"), delta, healthPerCard: 5, attackPerCard: 2);
        int maximum = lastMaximum - lastBaseMaximum + rawMaximum;
        int rawHealth = Int(before, "RawSaveHP");
        if (hpChange > 0) rawHealth = Math.Clamp(Math.Clamp(rawHealth, 0, maximum) + hpChange, 0, maximum);
        Require(Int(after, "RawSaveMaxHP") == rawMaximum && Int(after, "SaveMaxHP") == maximum &&
            Int(after, "RawSaveHP") == rawHealth && Int(after, "SaveHP") == health &&
            Int(after, "SaveModifiedAttack") == attack, "Native deck change did not settle the expected save resources.");
        if (inBattle)
            Require(Int(after, "PyreHP") == health && Int(after, "PyreMaxHP") == Int(before, "PyreMaxHP") &&
                Int(after, "PyreAttack") == Int(before, "PyreAttack") + attack - Int(before, "SaveModifiedAttack"),
                "The live Pyre did not reflect the observed save update.");
    }

    private static string? Text(FixtureValue value, string key) => value.GetProperty(key).GetString();
    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static bool Flag(FixtureValue value, string key) => value.GetProperty(key).GetBoolean();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
