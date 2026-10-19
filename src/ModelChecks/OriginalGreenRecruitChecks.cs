using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;
using System.Security.Cryptography;

internal static class OriginalGreenRecruitChecks
{
    private const string CardDataId = "c052c567-1e3c-4f36-a6cd-89b0648b9475";

    internal static void Native(FixtureValue root)
    {
        int currentRoomSupported = 0, currentRoomUnsupported = 0, currentRoomMismatches = 0;
        string? firstCurrentRoomFailure = null;
        bool nativeFallbackCaptured = false;
        // Recompute every stage from its native Before snapshot; archived Predicted values can predate this model.
        foreach (FixtureValue stage in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState beforeRoom = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? externalPyre = stage.TryGetProperty("ExternalPyre", out FixtureValue pyreValue) &&
                pyreValue.ValueKind != FixtureKind.Null ? pyreValue.Deserialize<CombatUnit>() : null;
            (int RoomIndex, CombatUnit Unit)[] externalTargets = stage.TryGetProperty("ExternalTargets", out FixtureValue targetsValue)
                ? targetsValue.EnumerateArray().Select(target => (target.GetProperty("RoomIndex").GetInt32(),
                    target.GetProperty("Unit").Deserialize<CombatUnit>()!)).ToArray()
                : Array.Empty<(int, CombatUnit)>();
            RoomCombatResult current = stage.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(beforeRoom, externalTargets: externalTargets)
                : RoomCombatModel.Resolve(beforeRoom, externalPyre: externalPyre,
                    externalPyreRoomIndex: externalPyre == null ? -1 : stage.GetProperty("ExternalPyreRoomIndex").GetInt32(),
                    externalTargets: externalTargets);
            if (!current.Supported)
            {
                currentRoomUnsupported++;
                firstCurrentRoomFailure ??= "stage " + stage.GetProperty("Index").GetInt32() + ": " + current.UnsupportedReason;
                continue;
            }
            currentRoomSupported++;
            RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            nativeFallbackCaptured |= actual.Units.Any(unit => unit.AssetKey == "MonsterFunguy" &&
                unit.Status("cardless")?.Stacks == 1 && unit.Status("horde")?.Stacks == 4);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(current.State!.Units), JsonSerializer.Serialize(actual.Units));
            if (stage.TryGetProperty("ExternalPyre", out FixtureValue pyreBefore) && pyreBefore.ValueKind != FixtureKind.Null)
            {
                CombatUnit pyreInput = pyreBefore.Deserialize<CombatUnit>()!;
                CombatUnit predictedPyre = current.RetainedUnits.Single(unit => unit.Id == pyreInput.Id);
                CombatUnit actualPyre = stage.GetProperty("ActualExternalPyre").Deserialize<CombatUnit>()!;
                difference ??= ModelJson.Difference(JsonSerializer.Serialize(predictedPyre), JsonSerializer.Serialize(actualPyre));
            }
            if (stage.TryGetProperty("ExternalTargets", out FixtureValue targets) && targets.GetArrayLength() > 0)
            {
                FixtureValue[] actualTargets = stage.GetProperty("ActualExternalTargets").EnumerateArray().ToArray();
                foreach (FixtureValue target in targets.EnumerateArray())
                {
                    int id = target.GetProperty("Unit").GetProperty("Id").GetInt32();
                    CombatUnit predictedTarget = current.RetainedUnits.Single(unit => unit.Id == id);
                    CombatUnit actualTarget = actualTargets.Single(item => item.GetProperty("Unit").GetProperty("Id").GetInt32() == id)
                        .GetProperty("Unit").Deserialize<CombatUnit>()!;
                    difference ??= ModelJson.Difference(JsonSerializer.Serialize(predictedTarget), JsonSerializer.Serialize(actualTarget));
                }
            }
            if (difference == null && current.State.Context != null && actual.Context != null)
                difference = ModelJson.Difference(JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(current.State.Context)),
                    JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual.Context)));
            if (difference != null)
            {
                currentRoomMismatches++;
                firstCurrentRoomFailure ??= "stage " + stage.GetProperty("Index").GetInt32() + ": " + difference;
            }
        }
        Console.WriteLine("GREEN-RECRUIT-CURRENT-ROOM-REPLAY supported=" + currentRoomSupported + " unsupported=" + currentRoomUnsupported +
            " mismatched=" + currentRoomMismatches +
            (firstCurrentRoomFailure == null ? "" : " first=" + firstCurrentRoomFailure));
        Require(currentRoomSupported == root.GetProperty("Stages").GetArrayLength() && currentRoomUnsupported == 0 && currentRoomMismatches == 0,
            "Current room model does not replay every native stage: " + firstCurrentRoomFailure);
        Require(nativeFallbackCaptured, "The native battle did not capture the cardless MonsterFunguy with horde=4 fallback.");
        Require(root.GetProperty("ModifierScenario").GetString() == "original-green-recruit" &&
            root.GetProperty("OriginalGreenRecruitPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Green Recruit battle.");
        foreach (string key in new[] { "CaptureFailures", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] plays = actions.Where(entry =>
        {
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == CardDataId;
        }).ToArray();
        Require(plays.Length == 1, "Expected exactly one original Green Recruit play.");

        FixtureValue entry = plays[0];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardInstanceState card = beforeContext.FindCard(action.CardInstanceId)!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CombatUnit template = rule.SpawnUnit!;
        CombatTrigger deathTrigger = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
        CombatEffect spawnOrStatus = deathTrigger.Effects.Single(effect => effect.Type == "CardEffectSpawnMonsterOrAddStatusEffect");
        CardActionEffect deathAction = spawnOrStatus.Action!;
        CombatUnit born = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
            .Single(unit => unit.SpawnerCardId == action.CardInstanceId);

        Require(card.MaskDescriptor!.SourceTraits is { Count: 0 } && rule.AssetKey == "GreenRecruit" &&
            rule.CardType == "Monster" && rule.Cost == 0 && rule.CostType == "Default" &&
            rule.Effect == "SpawnMonster" && template.AssetKey == "MonsterGenerousHost" &&
            deathAction.Type == "SpawnOrStatus" && deathAction.Target == "Room" && deathAction.AllowPlayer &&
            deathAction.Statuses.Count == 1 && deathAction.Statuses[0].Id == "horde" && deathAction.Statuses[0].Stacks == 4 &&
            deathAction.SpawnFallbackUnit?.AssetKey == "MonsterFunguy" &&
            action.CardInstanceId == card.InstanceId && action.TargetUnitId == 0 && action.PlayerPosition >= 0 &&
            born.Team == CombatTeam.Player && born.AssetKey == template.AssetKey && born.Size == template.Size &&
            before.Energy - after.Energy == rule.Cost &&
            afterContext.Statistics!.Value(card.InstanceId, "TimesPlayed", "ThisBattle") == 1,
            "Green Recruit's unchanged identity, MonsterGenerousHost spawn, player placement or cost payment changed.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "Original Green Recruit does not replay to native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate =>
            candidate.Action.CardInstanceId == action.CardInstanceId && candidate.Action.RoomIndex == action.RoomIndex &&
            candidate.Action.PlayerPosition == action.PlayerPosition),
            "Action search did not enumerate the native Green Recruit deployment.");

        RecordedBattleChecks.Native(root);
        TrainCombatChecks.Native(root);
        BattleTurnChecks.Native(root);
        Console.WriteLine("ORIGINAL-GREEN-RECRUIT PASS: spawned the unchanged MonsterGenerousHost from the original card and replayed its native deployment.");
    }

    internal static void RefreshPredictions(string inputPath, string outputPath)
    {
        if (File.Exists(outputPath)) throw new IOException("Refusing to overwrite fixture: " + outputPath);
        using FixtureDocument source = FixtureDocument.Read(inputPath);
        FixtureValue root = source.RootElement;
        Require(root.GetProperty("ModifierScenario").GetString() == "original-green-recruit",
            "Prediction refresh is restricted to the original Green Recruit capture.");
        Native(root);

        var differencePaths = new List<string>();
        FindDifferences(root, "$", differencePaths);
        Require(root.GetProperty("Mismatches").GetInt32() == differencePaths.Count &&
            differencePaths.All(path => path.StartsWith("$.Stages[", StringComparison.Ordinal) ||
                path.StartsWith("$.TrainPhases[", StringComparison.Ordinal) || path.StartsWith("$.Turns[", StringComparison.Ordinal)),
            "The archive contains mismatches outside the validated room/train/turn predictions: " + string.Join(",", differencePaths));
        foreach (string counter in new[] { "CaptureFailures", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Cannot refresh a fixture with nonzero " + counter + ".");

        var builder = new FixtureBuilder();
        int rootId = builder.Add(FixtureKind.Object, children: root.EnumerateObject().SelectMany(property => new[]
        {
            builder.Add(FixtureKind.String, property.Name), property.Name switch
            {
                "Mismatches" => builder.Add(FixtureKind.Number, "0"),
                "Stages" => RefreshRows(property.Value, builder, RefreshStage),
                "TrainPhases" => RefreshRows(property.Value, builder, RefreshTrainPhase),
                "Turns" => RefreshRows(property.Value, builder, RefreshTurn),
                _ => builder.Import(property.Value)
            }
        }).ToArray());

        using FixtureDocument refreshed = builder.Build(rootId, source.SourceLength, Convert.FromHexString(source.SourceSha256));
        string fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        string temporaryPath = fullOutputPath + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) refreshed.Write(stream);
            using FixtureDocument verified = FixtureDocument.Read(temporaryPath);
            VerifyRefreshedValues(root, verified.RootElement);
            File.Move(temporaryPath, fullOutputPath);
        }
        finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
        using FixtureDocument saved = FixtureDocument.Read(fullOutputPath);
        Console.WriteLine("GREEN-RECRUIT-PREDICTIONS-REFRESHED stages=" + saved.RootElement.GetProperty("Stages").GetArrayLength() +
            " trainPhases=" + saved.RootElement.GetProperty("TrainPhases").GetArrayLength() +
            " turns=" + saved.RootElement.GetProperty("Turns").GetArrayLength() + " mismatches=0 bytes=" + new FileInfo(fullOutputPath).Length +
            " sha256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullOutputPath))).ToLowerInvariant());
    }

    private static int RefreshRows(FixtureValue rows, FixtureBuilder builder, Func<FixtureValue, object> predict)
    {
        Require(rows.ValueKind == FixtureKind.Array, "Expected full-battle record array.");
        return builder.Add(FixtureKind.Array, children: rows.EnumerateArray().Select(row =>
        {
            object prediction = predict(row);
            return builder.Add(FixtureKind.Object, children: row.EnumerateObject().SelectMany(property => new[]
            {
                builder.Add(FixtureKind.String, property.Name), property.Name switch
                {
                    "Predicted" => AddJson(builder, JsonSerializer.SerializeToElement(prediction, prediction.GetType(), ModelJson.Options)),
                    "Difference" => builder.Add(FixtureKind.Null),
                    _ => builder.Import(property.Value)
                }
            }).ToArray());
        }).ToArray());
    }

    private static object RefreshStage(FixtureValue stage)
    {
        RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        CombatUnit? pyre = stage.TryGetProperty("ExternalPyre", out FixtureValue pyreValue) && pyreValue.ValueKind != FixtureKind.Null
            ? pyreValue.Deserialize<CombatUnit>() : null;
        (int RoomIndex, CombatUnit Unit)[] targets = stage.TryGetProperty("ExternalTargets", out FixtureValue targetValues)
            ? targetValues.EnumerateArray().Select(target => (target.GetProperty("RoomIndex").GetInt32(), target.GetProperty("Unit").Deserialize<CombatUnit>()!)).ToArray()
            : Array.Empty<(int, CombatUnit)>();
        RoomCombatResult result = stage.GetProperty("Kind").GetString() == "Exchange" ?
            RoomCombatModel.Exchange(before, externalTargets: targets) : RoomCombatModel.Resolve(before, externalPyre: pyre,
                externalPyreRoomIndex: pyre == null ? -1 : stage.GetProperty("ExternalPyreRoomIndex").GetInt32(), externalTargets: targets);
        Require(result.Supported, "Cannot refresh unsupported room stage " + stage.GetProperty("Index").GetInt32() + ".");
        return result;
    }

    private static object RefreshTrainPhase(FixtureValue phase)
    {
        string kind = phase.GetProperty("Kind").GetString()!;
        TrainCombatState before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatResult result = kind == "Combat" ? TrainCombatModel.ResolveCombat(before) : TrainCombatModel.Ascend(before);
        Require(result.Supported, "Cannot refresh unsupported train phase " + phase.GetProperty("Index").GetInt32() + ".");
        return result;
    }

    private static object RefreshTurn(FixtureValue turn)
    {
        BattleTurnState before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnResult result = BattleTurnModel.EndTurn(before);
        Require(result.Supported, "Cannot refresh unsupported EndTurn " + turn.GetProperty("Index").GetInt32() + ".");
        return result;
    }

    private static int AddJson(FixtureBuilder builder, JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => builder.Add(FixtureKind.Object, children: value.EnumerateObject().SelectMany(property => new[]
            { builder.Add(FixtureKind.String, property.Name), AddJson(builder, property.Value) }).ToArray()),
        JsonValueKind.Array => builder.Add(FixtureKind.Array, children: value.EnumerateArray().Select(item => AddJson(builder, item)).ToArray()),
        JsonValueKind.String => builder.Add(FixtureKind.String, value.GetString()),
        JsonValueKind.Number => builder.Add(FixtureKind.Number, value.GetRawText()),
        JsonValueKind.True => builder.Add(FixtureKind.True),
        JsonValueKind.False => builder.Add(FixtureKind.False),
        JsonValueKind.Null => builder.Add(FixtureKind.Null),
        _ => throw new InvalidDataException("Unsupported prediction JSON value: " + value.ValueKind)
    };

    private static void VerifyRefreshedValues(FixtureValue source, FixtureValue refreshed)
    {
        Require(refreshed.GetProperty("Mismatches").GetInt32() == 0, "Refreshed fixture still reports mismatches.");
        foreach (string name in new[] { "Actions", "NativeWon", "CaptureFailures", "Unsupported", "Pending" })
            Require(source.GetProperty(name).ContentEquals(refreshed.GetProperty(name)), "Refresh changed native field " + name + ".");
        foreach (string name in new[] { "Stages", "TrainPhases", "Turns" })
        {
            FixtureValue[] before = source.GetProperty(name).EnumerateArray().ToArray();
            FixtureValue[] after = refreshed.GetProperty(name).EnumerateArray().ToArray();
            Require(before.Length == after.Length, "Refresh changed record count for " + name + ".");
            for (int index = 0; index < before.Length; index++)
                Require(before[index].GetProperty("Before").ContentEquals(after[index].GetProperty("Before")) &&
                    before[index].GetProperty("Actual").ContentEquals(after[index].GetProperty("Actual")) &&
                    after[index].GetProperty("Difference").ValueKind == FixtureKind.Null,
                    "Refresh changed native Before/Actual or failed to clear a validated Difference in " + name + "[" + index + "].");
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static void FindDifferences(FixtureValue value, string path, List<string> paths)
    {
        if (value.ValueKind == FixtureKind.Object)
        {
            foreach (FixtureProperty property in value.EnumerateObject())
            {
                if (property.Name == "Difference" && property.Value.ValueKind != FixtureKind.Null)
                    paths.Add(path + ".Difference=" + property.Value.GetString());
                FindDifferences(property.Value, path + "." + property.Name, paths);
            }
        }
        else if (value.ValueKind == FixtureKind.Array)
        {
            int index = 0;
            foreach (FixtureValue item in value.EnumerateArray()) FindDifferences(item, path + "[" + index++ + "]", paths);
        }
    }
}
