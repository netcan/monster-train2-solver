using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEndlessFirstWaveChecks
{
    internal static void Loop2AdvisorHandoff(FixtureValue receipt)
    {
        Require(Text(receipt, "GameVersion") == Game231FixtureCatalog.Version && Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "EvidenceKind") == "NativeEndlessLoop2AdvisorHandoff" &&
            Text(receipt, "Assembly") == "MonsterTrain2Poju.Advisor" && Text(receipt, "Scenario") == "TestEndless_Level1Battle1" &&
            Number(receipt, "Loop") == 2 && Number(receipt, "Distance") == 0 &&
            Number(receipt, "AcceptedCardInputs") == 1 && Number(receipt, "EndTurnInputs") == 0 &&
            Number(receipt, "PurityFailures") == 0 && Flag(receipt, "ReusedContinuation") &&
            receipt.GetProperty("FinalSearches").GetInt64() == receipt.GetProperty("OriginalSearches").GetInt64() &&
            Number(receipt, "FinalPublications") == Number(receipt, "OriginalPublications") + 1 &&
            receipt.GetProperty("FinalGeneration").GetInt64() > receipt.GetProperty("OriginalGeneration").GetInt64() &&
            Text(receipt, "TentativeStatusText")?.Contains("待确认", StringComparison.Ordinal) == true &&
            Text(receipt, "TentativePanelText")?.Contains("待核对", StringComparison.Ordinal) == true &&
            !string.IsNullOrEmpty(Text(receipt, "FinalPanelText")),
            "Expected a tentative next step and one fresh publication after the single accepted input, without another search.");

        var input = receipt.GetProperty("Input");
        var nativeBefore = input.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var nativeAction = input.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var nativePlay = BattleActionModel.PlayCard(nativeBefore, nativeAction);
        Require(nativePlay.Supported && nativePlay.Outcome == input.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() &&
            input.GetProperty("Difference").ValueKind == FixtureKind.Null, nativePlay.Reason ?? "Expected the recorded paid action to settle.");
        RecordedBattleChecks.RecordedBoundary(nativePlay.State!, input.GetProperty("Actual"), terminal: false,
            "original Loop 2 Advisor matching play", trackPreviewPosition: true);

        var oldRoot = receipt.GetProperty("OriginalRoot").Deserialize<BattleTurnState>()!;
        var final = receipt.GetProperty("FinalRoot").Deserialize<BattleTurnState>()!;
        var published = receipt.GetProperty("FinalPublishedRoot").Deserialize<BattleTurnState>()!;
        FixtureValue[] originalSteps = receipt.GetProperty("OriginalSteps").EnumerateArray().ToArray();
        FixtureValue[] finalSteps = receipt.GetProperty("FinalSteps").EnumerateArray().ToArray();
        Require(originalSteps.Length > 1 && originalSteps.Skip(1).Zip(finalSteps).All(pair => pair.First.ContentEquals(pair.Second)) &&
            finalSteps.Length == originalSteps.Length - 1 && oldRoot.Spawn.Turn == 0 && final.Spawn.Turn == 0,
            "The confirmed plan must be precisely the original suffix at deployment.");
        var first = originalSteps[0].Deserialize<BattleSearchStep>()!;
        Require(!first.IsEndTurn && first.Action != null, "Expected a paid first recommendation.");
        var projected = BattleActionModel.PlayCard(oldRoot, first.Action!);
        using var continuationKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        using var fullKeys = new BattleStateKeys();
        Require(projected.Supported && continuationKeys.Key(projected.State!) == continuationKeys.Key(final) &&
            fullKeys.Key(final) == fullKeys.Key(published), "Expected the native replacement publication and independent projected player successor.");
        Console.WriteLine($"NATIVE-ENDLESS-LOOP2-ADVISOR-HANDOFF PASS: one paid play with {finalSteps.Length} retained steps and no new search.");
    }

    internal static void Loop2Advisor(FixtureValue receipt)
    {
        Require(Text(receipt, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "EvidenceKind") == "NativeEndlessLoop2AdvisorObservation" &&
            Text(receipt, "Assembly") == "MonsterTrain2Poju.Advisor" && Text(receipt, "Scenario") == "TestEndless_Level1Battle1" &&
            Number(receipt, "Loop") == 2 && Number(receipt, "Distance") == 0 &&
            Number(receipt, "CardInputs") == 0 && Number(receipt, "EndTurnInputs") == 0 &&
            Number(receipt, "Publications") >= 1 && Number(receipt, "PublicationRootChecks") >= 1 &&
            Number(receipt, "PurityFailures") == 0 && Flag(receipt, "OpeningStateAndGameplayRngUnchanged") &&
            Flag(receipt, "RenderStateAndRngUnchanged"), "Expected a pure native publication from the separately built player Advisor.");
        var root = receipt.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var published = receipt.GetProperty("PublishedRoot").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        Require(root.Spawn.Turn == 0 && Text(receipt, "RootKey") is { Length: 64 } issuedKey && issuedKey.All(Uri.IsHexDigit) &&
            (keys.Key(root) == keys.Key(published) || AbilityCacheAdviceReuse.Matches(published, root, keys)),
            "Expected the matching native deployment root of the displayed recommendation.");
        var first = receipt.GetProperty("First").Deserialize<BattleSearchStep>()!;
        string? panel = Text(receipt, "PanelText");
        Require(!Flag(receipt, "Terminal") && !Flag(receipt, "CurrentActionOnly") && Number(receipt, "Steps") > 0 &&
            Number(receipt, "PanelPixels") > 0 && Number(receipt, "PanelVertices") > 0 &&
            panel?.StartsWith("部分方案", StringComparison.Ordinal) == true && panel.Contains("整场战损尚未确定") &&
            !first.IsEndTurn && first.Action is { } action && panel.Contains("第 " + (action.RoomIndex + 1) + " 层") &&
            BattleActionModel.PlayCard(root, action).Supported, "Expected a rendered partial forecast with a supported first paid action.");
        Console.WriteLine($"NATIVE-ENDLESS-LOOP2-ADVISOR CHECK PASS: rendered partial forecast, {Number(receipt, "Steps")} steps, zero native inputs and purity failures.");
    }

    internal static void Loop2HordeDeath(FixtureValue opening, FixtureValue receipt)
    {
        Require(Text(receipt, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "Boundary") == "OriginalEndlessLoop2PaidHordeDeath" &&
            receipt.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            Text(opening, "Boundary") == "OriginalEndlessEnemyScalingControlledLoop2",
            "Expected the original Loop 2 paid Corruption receipt and its first wave.");
        var root = opening.GetProperty("FirstDecision").Deserialize<BattleTurnState>()!;
        BattleTurnState state = RecordedBattleChecks.RecordedBoundary(root, receipt.GetProperty("Root"),
            terminal: false, "Loop 2 opening", trackPreviewPosition: true);
        FixtureValue[] rows = receipt.GetProperty("Steps").EnumerateArray().ToArray();
        Require(rows.Length == 16 && rows.Count(row => Flag(row.GetProperty("Input"), "IsEndTurn")) == 2,
            "Expected the sixteen native inputs and two EndTurns.");
        foreach ((FixtureValue row, int index) in rows.Select((row, index) => (row, index)))
        {
            FixtureValue input = row.GetProperty("Input");
            BattleTurnState? result;
            if (Flag(input, "IsEndTurn"))
            {
                IReadOnlyList<RelentlessDecision>? choices = input.TryGetProperty("RelentlessDecisions", out var decisions) &&
                    decisions.ValueKind != FixtureKind.Null ? decisions.Deserialize<IReadOnlyList<RelentlessDecision>>() : null;
                BattleTurnResult turn = BattleTurnModel.EndTurn(state, choices);
                Require(turn.Supported && turn.Outcome == RoomOutcome.Exchanged, turn.UnsupportedReason ?? "Expected a settled Loop 2 exchange.");
                result = turn.State;
            }
            else
            {
                PlayCardAction action = input.GetProperty("Action").Deserialize<PlayCardAction>()!;
                BattleActionResult play = BattleActionModel.PlayCard(state, action);
                Require(play.Supported && play.Outcome == RoomOutcome.Exchanged, play.Reason ?? "Expected a settled Loop 2 card play.");
                result = play.State;
                if (index == rows.Length - 1)
                    Require(state.Spawn.Turn == 3 && action.CardInstanceId == 10 && action.TargetUnitId == 8 &&
                        state.Spawn.Train.Rooms[1].Units.Any(actor => actor is { Id: 8, AssetKey: "HordeT2_DeathAddsCorruption", Health: 3 }) &&
                        state.Spawn.Train.Rooms[1].Units.Single(actor => actor.Id == 18).Status("corruption") == null,
                        "The last original paid input must kill the three-HP Horde.");
            }
            RecordedBattleChecks.RecordedBoundary(result!, row.GetProperty("Expected"), terminal: false, $"Loop 2 forecast {index + 1}", trackPreviewPosition: true);
            state = RecordedBattleChecks.RecordedBoundary(result!, row.GetProperty("Actual"), terminal: false, $"Loop 2 native input {index + 1}", trackPreviewPosition: true);
        }
        Require(state.Spawn.Train.Rooms.SelectMany(room => room.Units).All(actor => actor.Id != 8) &&
            state.Spawn.Train.Rooms[1].Units.Single(actor => actor.Id == 18).Status("corruption")?.Stacks == 15,
            "The original Horde death must grant fifteen Corruption to the front player unit.");
        Console.WriteLine("NATIVE-ENDLESS-LOOP2-HORDE-DEATH PASS: 14 paid actions, two EndTurns and the original 15-stack death callback.");
    }

    internal static void ArkionTerminal(FixtureValue archive)
    {
        FixtureValue run = archive.GetProperty("NativeRunInput");
        Require(Text(archive, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(run, "Scenario") == "TestEndless_Level3Battle1" && Number(run, "Distance") == 2 &&
            Text(run, "RunType") == "Class" && !Flag(run, "WasDevCheatUsed") &&
            Flag(archive, "TerminalEffectsSettled") && !Flag(archive, "NativeWon") &&
            archive.GetProperty("Actions").GetArrayLength() == 0,
            "Expected the settled original Ring 3 Arkion defeat without paid card actions.");
        OuterBossTurnChecks.Deployment(archive, "Boss_Level3_Arkion_PushAttack", minimumTurns: 2,
            module: Game231FixtureCatalog.Module);
    }

    internal static void Factors(FixtureValue archive)
    {
        Require(Text(archive, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(archive, "Boundary") == "OriginalEndlessEnemyScalingFactors" &&
            Number(archive, "RunLength") == 8 && archive.GetProperty("RngBefore").ContentEquals(archive.GetProperty("RngAfter")),
            "Expected the pinned native factor queries with unchanged gameplay RNG.");
        FixtureValue[] rows = archive.GetProperty("Factors").EnumerateArray().ToArray();
        string[] fields = { "Attack", "Health", "Status", "BuffDamage", "Heal", "Damage" };
        Require(rows.Length == 24 && Enumerable.Range(0, rows.Length).All(index =>
            Number(rows[index], "Loop") == index / 8 + 1 && Number(rows[index], "Distance") == index % 8 &&
            fields.All(field => rows[index].GetProperty(field).GetDouble() is >= 1 and < 1000)),
            "Expected six native balance queries across each of the 24 loop/distance pairs.");
        Require(rows[0].GetProperty("Health").GetDouble() == 1.5 &&
            rows[1].GetProperty("Health").GetDouble() == 2 &&
            rows[8].GetProperty("Damage").GetDouble() > rows[7].GetProperty("Damage").GetDouble(),
            "Expected the measured early-ring health and the next-loop damage increase.");
        var eligibility = archive.GetProperty("StatusEligibility").EnumerateArray()
            .ToDictionary(row => Text(row, "Id")!, row => Flag(row, "ScalesInEndless"));
        Require(eligibility.Count == 90 && eligibility.Count(entry => entry.Value) == 26 &&
            eligibility["armor"] && eligibility["titanskin"] && eligibility["buff"] && !eligibility["relentless"],
            "The calibrated status eligibility changed.");
        Console.WriteLine("NATIVE-ENDLESS-FACTORS PASS: 24 original balance points, 90 status records and unchanged gameplay RNG.");
    }

    internal static void Native(FixtureValue transition, FixtureValue natural, FixtureValue controlled)
    {
        foreach (FixtureValue item in new[] { transition, natural, controlled })
            Require(Text(item, "GameVersion") == Game231FixtureCatalog.Version &&
                Text(item, "GameModuleMvid") == Game231FixtureCatalog.Module, "Expected the pinned game for all Endless observations.");

        FixtureValue scenario = natural.GetProperty("Scenario");
        FixtureValue control = controlled.GetProperty("Scenario");
        Require(Text(transition, "Boundary") == "NativeEndlessTransition" && Text(transition, "RunType") == "Class" &&
            Flag(transition, "IsEndlessRun") && !Flag(transition, "InTestScenario") && !Flag(transition, "WasDevCheatUsed") &&
            Number(transition, "Loop") == 1 && Number(transition, "Distance") == 0 &&
            Text(natural, "Boundary") == "OriginalEndlessEnemyScaling" && Text(controlled, "Boundary") == "OriginalEndlessEnemyScaling" &&
            !Flag(scenario, "ControlledFirstLoop") && Flag(control, "ControlledFirstLoop") &&
            Text(scenario, "OwnerKey") == "Enemy_EndlessScaling", "Expected a genuine game-over transition and a separate controlled loop.");
        foreach (string field in new[] { "Scenario", "Loop", "Distance", "RunLength" })
            Require(transition.GetProperty(field).ContentEquals(scenario.GetProperty(field)), "The resumed battle changed the original transition " + field);
        foreach (string field in new[] { "Scenario", "Loop", "Distance", "RunLength", "OwnerId", "OwnerKey", "Attack",
            "Health", "Status", "BuffDamage", "Heal", "Damage", "ExcludedSubtypes" })
            Require(scenario.GetProperty(field).ContentEquals(control.GetProperty(field)), "Native first-loop configuration differs for " + field);

        FixtureValue[] observed = natural.GetProperty("Births").EnumerateArray().ToArray();
        FixtureValue[] controls = controlled.GetProperty("Births").EnumerateArray().ToArray();
        Require(observed.Length == 3 && controls.Length == observed.Length, "Expected all three original enemy births.");
        for (int index = 0; index < observed.Length; index++)
        {
            FixtureValue birth = observed[index];
            FixtureValue other = controls[index];
            bool heavy = index < 2;
            Require(Text(birth, "AssetKey") == (heavy ? "HeavyT1_LoseHealthOnMove" : "ClericT1_OminousAcolyte_Chosen") &&
                !Flag(birth, "FromCard") && Number(birth, "BaselineAttack") == (heavy ? 4 : 0) &&
                Number(birth, "BaselineHealth") == 80, "The original first wave changed at birth " + index);
            foreach (string field in new[] { "AssetKey", "BaselineAttack", "BaselineHealth", "RelicAttackBonus", "LateAttackMultiplier" })
                Require(birth.GetProperty(field).ContentEquals(other.GetProperty(field)), "Birth input differs for " + field);
            FixtureValue before = birth.GetProperty("Before"), after = birth.GetProperty("After");
            Require(Number(before, "Attack") == (heavy ? 3 : 0) && Number(before, "Health") == (heavy ? 90 : 30) &&
                Number(after, "Attack") == (heavy ? 5 : 0) && Number(after, "Health") == 120 && Number(after, "MaximumHealth") == 120 &&
                before.GetProperty("Rng").ContentEquals(after.GetProperty("Rng")), "Unexpected native scaling or RNG at birth " + index);
            foreach (string checkpoint in new[] { "Before", "After" })
                foreach (string field in new[] { "Attack", "Health", "MaximumHealth", "Statuses", "Triggers" })
                    Require(birth.GetProperty(checkpoint).GetProperty(field).ContentEquals(other.GetProperty(checkpoint).GetProperty(field)),
                        "Scaled actor differs for " + checkpoint + " " + field);
        }
        Console.WriteLine("NATIVE-ENDLESS-FIRST-WAVE PASS: original victory transition, six factor queries, three births and unchanged callback RNG.");
    }

    internal static void Forecast(FixtureValue archive, FixtureValue? outerBirth = null)
    {
        FixtureValue scenario = archive.GetProperty("Scenario");
        string? boundary = Text(archive, "Boundary");
        int loop = Number(scenario, "Loop"), distance = Number(scenario, "Distance");
        Require(Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            !Flag(scenario, "ControlledFirstLoop") && (boundary, loop, distance) is
                ("OriginalEndlessEnemyScaling", 1, 0) or
                ("OriginalEndlessEnemyScalingControlledNextRing", 1, 1 or 2) or
                ("OriginalEndlessEnemyScalingControlledLoop2", 2, 0),
            "Expected one of the pinned original Endless opening waves.");
        BattleTurnState deployment = archive.GetProperty("Deployment").Deserialize<BattleTurnState>()!;
        BattleTurnResult advanced = BattleTurnModel.EndTurn(deployment);
        Require(advanced.Supported, advanced.UnsupportedReason ?? "The original first wave must be supported.");
        RecordedBattleChecks.RecordedBoundary(advanced.State!, archive.GetProperty("FirstDecision"),
            terminal: false, "original Endless first wave", trackPreviewPosition: true);
        if (distance == 1)
        {
            var births = archive.GetProperty("Births").EnumerateArray().ToArray();
            Require(births.Length == 4 && births.All(row => row.GetProperty("Before").GetProperty("Rng").ContentEquals(
                row.GetProperty("After").GetProperty("Rng"))), "Expected four original birth callbacks with unchanged Battle and BattleTest.");
            CombatUnit[] enemies = advanced.State!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(actor => actor.Team == CombatTeam.Enemy).ToArray();
            CombatUnit enchanter = enemies.Single(actor => actor.AssetKey == "ShieldGuardT3_Enchanter_Chosen");
            Require(enchanter.Health == 600 && enchanter.Status("armor")?.Stacks == 240 &&
                enchanter.Status("titanskin")?.Stacks == 30 && enemies.Count(actor => actor.Status("titanskin")?.Stacks == 10) == 3,
                "Original starting statuses and aura recipients must match.");
        }
        if (distance == 2)
        {
            var births = archive.GetProperty("Births").EnumerateArray().ToArray();
            Require(births.Select(row => (Text(row, "AssetKey"), Number(row.GetProperty("After"), "Attack"),
                Number(row.GetProperty("After"), "Health"))).SequenceEqual(new[] {
                    ("HeavyT1_Basic", 10, 250), ("ClericT1_OminousAcolyte", 0, 20), ("ClericT3_IncantAttack_Chosen", 0, 600)
                }) && births.All(row => row.GetProperty("Before").GetProperty("Rng").ContentEquals(
                    row.GetProperty("After").GetProperty("Rng"))), "Expected the three original Ring 3 births and unchanged callback RNG.");
            OuterBossProgress? boss = advanced.State!.Spawn.OuterBoss;
            CombatUnit? actor = advanced.State.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == boss?.UnitId);
            Require(boss is { AssetKey: "Boss_Level3_Arkion_PushAttack", Room: 2 } &&
                actor is { Attack: 40, Health: 6250, MaxHealth: 6250 }, "The original Endless Arkion birth must settle at 40 attack and 6250 HP.");
            if (outerBirth is { } nativeBoss)
            {
                OuterBossBirthChecks.Native(nativeBoss, Game231FixtureCatalog.Module);
                FixtureValue scale = nativeBoss.GetProperty("Births")[0].GetProperty("EndlessScaling");
                FixtureValue before = scale.GetProperty("Before"), after = scale.GetProperty("After");
                Require(Text(scale, "AssetKey") == boss!.AssetKey && Number(scale, "BaselineAttack") == 25 &&
                    Number(scale, "BaselineHealth") == 2500 && Number(before, "Attack") == 13 && Number(before, "Health") == 425 &&
                    Number(after, "Attack") == actor!.Attack && Number(after, "Health") == actor.Health &&
                    before.GetProperty("Rng").ContentEquals(after.GetProperty("Rng")),
                    "The live Arkion callback must record the ordered before/after stats and unchanged RNG.");
            }
        }
        if (loop == 2)
        {
            var births = archive.GetProperty("Births").EnumerateArray().ToArray();
            Require(Text(scenario, "Scenario") == "TestEndless_Level1Battle1" &&
                births.Select(row => (Text(row, "AssetKey"), Number(row.GetProperty("After"), "Attack"),
                    Number(row.GetProperty("After"), "Health"))).SequenceEqual(new[] {
                        ("HordeT2_DeathAddsCorruption", 24, 60), ("HordeT2_DeathAddsCorruption", 24, 60),
                        ("HeavyT1_LoseHealthOnMove_Chosen", 32, 1867), ("AttackerT1_Spearman", 40, 60), ("AttackerT1_Spearman", 40, 60)
                    }) && births.All(row => row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("After").GetProperty("Rng"))),
                "Expected all five original Loop 2 births and unchanged callback RNG.");
        }
        Console.WriteLine("NATIVE-ENDLESS-FORECAST PASS: complete deployment successor and original first-wave RNG.");
    }

    internal static void Waves(FixtureValue archive)
    {
        Require(Text(archive, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(archive, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(archive, "Boundary") == "OriginalEndlessEnemyScalingWaves" &&
            !Flag(archive.GetProperty("Scenario"), "ControlledFirstLoop"), "Expected the original post-victory Endless wave capture.");
        FixtureValue[] waves = archive.GetProperty("Waves").EnumerateArray().ToArray();
        FixtureValue[] turns = archive.GetProperty("Turns").EnumerateArray().ToArray();
        Require(waves.Length == 6 && turns.Length == waves.Length && Number(archive, "CompletedWaves") == waves.Length,
            "Expected a native EndTurn for every scheduled wave.");

        int births = 0;
        for (int index = 0; index < waves.Length; index++)
        {
            FixtureValue wave = waves[index], turn = turns[index];
            Require(Number(wave, "Index") == index && Number(turn, "Index") == index &&
                Flag(wave, "PhaseAdvanced"), "The scheduled wave order changed.");
            FixtureValue actual = turn.GetProperty("Actual");
            Require(actual.GetProperty("Spawn").GetProperty("Phase").GetInt32() == index + 1,
                "The native turn must settle exactly one wave.");
            var root = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var decisions = turn.GetProperty("RelentlessDecisions").Deserialize<IReadOnlyList<RelentlessDecision>>();
            BattleTurnResult next = BattleTurnModel.EndTurn(root, decisions);
            Require(next.Supported, "Wave " + (index + 1) + ": " + next.UnsupportedReason);
            RecordedBattleChecks.RecordedBoundary(next.State!, actual,
                terminal: false, "original Endless wave " + (index + 1), trackPreviewPosition: true);
            foreach (FixtureValue birth in wave.GetProperty("Births").EnumerateArray())
            {
                births++;
                Require(birth.GetProperty("Before").GetProperty("Rng").ContentEquals(
                    birth.GetProperty("After").GetProperty("Rng")), "An observed birth changed callback RNG.");
            }
        }
        Require(births == 21, "The original six-wave roster changed.");
        Console.WriteLine("NATIVE-ENDLESS-WAVES PASS: six exact EndTurns and 21 ordered native birth callbacks.");
    }

    private static int Number(FixtureValue value, string field) => value.GetProperty(field).GetInt32();
    private static string? Text(FixtureValue value, string field) => value.GetProperty(field).GetString();
    private static bool Flag(FixtureValue value, string field) => value.GetProperty(field).GetBoolean();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
