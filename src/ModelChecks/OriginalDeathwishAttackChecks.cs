using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDeathwishAttackChecks
{
    private const string OwnerId = "437ca81a-8c1b-4e53-adfd-e64381e1b0d9";
    private const string UpgradeId = "f9a9fd6b-de5b-46d9-b733-0db48a8902a7";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "original-deathwish-attack" &&
            root.GetProperty("NativeWon").ValueKind is FixtureKind.True or FixtureKind.False &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing versioned original relic battle terminal.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete original relic battle: " + counter);
        FixtureValue evidence = root.GetProperty("OriginalDeathwishAttack");
        int[] cards = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("OwnerId").GetString() == OwnerId &&
            evidence.GetProperty("CompletedControlPlays").GetInt32() == 7 && cards.Length == 7 && cards.Distinct().Count() == 7 &&
            actions.Length >= 7 && turns.Length > 1 && actions.Take(7).Select(row => row.GetProperty("Action")
                .GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards), "The original relic's native controls were not played.");
        BattleTurnState initial = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState owner = initial.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicCharacterTriggers rule = owner.TriggerAdditions!.Single();
        Require(owner.AssetKey == "DeathwishAttack" && owner.EffectTypes.SequenceEqual(new[] { "RelicEffectAddTrigger" }) &&
            rule.SourcePlayer && !rule.SourceEnemy && rule.SubtypeIsNone && !rule.NoCountModifiersAllowed && rule.Conditions.Count == 0 &&
            rule.Triggers.Select(trigger => trigger.Kind).SequenceEqual(new[] { "OnDeathwish", "OnDeathwishLost" }) &&
            rule.Triggers.Select(trigger => trigger.Effects.Single().Type).SequenceEqual(new[] {
                "CardEffectAddTempCardUpgradeToUnits", "CardEffectRemoveTempUpgradeFromUnit" }) &&
            rule.Triggers.All(trigger => HalfHealthDeathwishModel.IsTrigger(trigger) && trigger.IgnoreSilence &&
                !trigger.Once && trigger.Effects.Count == 1 && trigger.Effects[0].UnitUpgrade is CardActionEffect action &&
                action.Target == "Self" && action.Lifetime == "TemporaryUntilEndOfBattle" && action.Upgrade is CardUpgradeModifier upgrade &&
                upgrade.DataId == UpgradeId && upgrade.AssetKey == "Plus15Attack" && upgrade.Stats.Damage == 15 && upgrade.Stats.Health == 0),
            "Original DeathwishAttack source/upgrade definitions differ.");
        int[] hp = { 20, 20, 10, 11, 10, 10, 11 };
        int[] attackAdded = { 0, 0, 15, 0, 15, 15, 0 };
        for (int index = 0; index < 7; index++)
        {
            int carrier = index == 1 || index >= 5 ? cards[1] : cards[0];
            CombatUnit actor = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == carrier);
            Require(actor.Health == hp[index] && actor.MaxHealth == 20 && actor.Modifiers!.AttackDamageAdded == attackAdded[index] &&
                actor.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == UpgradeId) == (attackAdded[index] == 15 ? 1 : 0),
                "Missing exact native +15 / removal / reentry control at play " + index);
        }
        FixtureValue[] transitions = evidence.GetProperty("Transitions").EnumerateArray().ToArray();
        foreach (FixtureValue row in transitions)
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("After").ValueKind == FixtureKind.Object &&
                row.GetProperty("GoldBefore").GetInt32() == row.GetProperty("GoldAfter").GetInt32() &&
                row.GetProperty("Dispatch").GetProperty("ExclusiveTriggerStateId").ValueKind != FixtureKind.Null,
                "Incomplete original half-health upgrade callback.");
        foreach (bool preview in new[] { false, true })
        {
            FixtureValue[] rows = transitions.Where(row => row.GetProperty("Preview").GetBoolean() == preview).ToArray();
            Require(rows.Any(row => Kind(row) == "OnDeathwish" && !HasUpgrade(row, "Before") && HasUpgrade(row, "After")) &&
                rows.Any(row => Kind(row) == "OnDeathwishLost" && HasUpgrade(row, "Before") && !HasUpgrade(row, "After")),
                "Missing original gain/loss effects in " + (preview ? "preview" : "real battle"));
        }
        CombatUnit[] actors = root.GetProperty("Stages").EnumerateArray().SelectMany(stage => new[] { "Before", "Actual" }
            .Select(field => stage.GetProperty(field).Deserialize<RoomCombatState>()!).SelectMany(room => room.Units))
            .Where(unit => cards.Take(2).Contains(unit.SpawnerCardId)).ToArray();
        Require(actors.Any(unit => unit.Status("undying")?.Stacks > 0) &&
            actors.Any(unit => unit.Health > 0 && (unit.Status("undying")?.Stacks ?? 0) == 0), "Native carrier revival was not observed.");
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(root);
        RecordedBattleChecks.Native(root);
        int previewCount = transitions.Count(row => row.GetProperty("Preview").GetBoolean());
        Console.WriteLine($"ORIGINAL-DEATHWISH-ATTACK PASS: unchanged owner and +15 upgrade, seven exact controls, {transitions.Length} completed callbacks ({previewCount} preview); gain/removal/reentry, natural Undying, full native phases/RNG and 16 immutable multi-turn branches.");
    }
    private static string? Kind(FixtureValue row) => row.GetProperty("Dispatch").GetProperty("Kind").GetString();
    private static bool HasUpgrade(FixtureValue row, string field) => row.GetProperty(field).Deserialize<CombatUnit>()!.Modifiers!
        .Upgrades.Any(upgrade => upgrade.DataId == UpgradeId);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
