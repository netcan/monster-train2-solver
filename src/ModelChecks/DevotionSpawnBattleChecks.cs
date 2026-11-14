using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DevotionSpawnBattleChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "devotion-spawn-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing settled current-version Devotion spawn victory.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete battle: " + counter);
        FixtureValue evidence = root.GetProperty("DevotionSpawnBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 12 &&
            ids.Length == 12 && ids.Distinct().Count() == 12 && actions.Length == 12 && actions.Select(row =>
                row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing twelve ordered native plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext initial = Before(0).Spawn.Train.Context!;
        CombatRelicState owner = initial.Relics!.Single(item => item.DataId == "ecb009cc-30c5-47ba-93dd-91b8eab5c78f");
        Require(owner.AssetKey == "MonolithDevotion" && owner.EffectTypes.SequenceEqual([RelicModel.AddStatusPerDevotionOnSpawn]) &&
            owner.SpawnStatuses!.Single().Statuses.Single().Stacks == 4 && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && initial.Relics.Any(item => item.DataId == "ee429978-fa41-4c66-93c6-3d3c7d5a4733" &&
                item.AssetKey == "ZealDamageIncrease" && item.EffectTypes.SequenceEqual(["RelicEffectModifyStatusMagnitude"])),
            "Missing unchanged original Devotion/Zeal owners.");
        Require(initial.QueryFrame!.Devotion is { Level: 0, Points: 0, FirstThreshold: 3, ThresholdIncrease: 3,
            PassiveStacksPerLevel: 2, PassiveMultiplier: 1 }, "Missing actual Devotion configuration and native global multiplier.");
        FixtureValue[] definitions = evidence.GetProperty("OwnerDefinitions").EnumerateArray().ToArray();
        Require(definitions.Length == 2 && definitions[0].GetProperty("Id").GetString() == owner.DataId &&
            definitions[0].GetProperty("Type").GetString() == RelicModel.AddStatusPerDevotionOnSpawn &&
            definitions[0].GetProperty("Statuses").EnumerateArray().Single().GetProperty("count").GetInt32() == 4 &&
            definitions[1].GetProperty("Id").GetString() == "ee429978-fa41-4c66-93c6-3d3c7d5a4733" &&
            definitions[1].GetProperty("Type").GetString() == "RelicEffectModifyStatusMagnitude" &&
            definitions[1].GetProperty("Int").GetInt32() == 1 && definitions.All(definition =>
                !definition.GetProperty("Placement").GetBoolean() && definition.GetProperty("Conditions").GetInt32() == 0 &&
                definition.GetProperty("Team").GetString() == "Monsters"), "Changed original owner definitions.");
        FixtureValue[] originals = evidence.GetProperty("OriginalCards").EnumerateArray().ToArray();
        Require(originals.Length == 3, "Missing original card metadata.");
        Original(0, "96c9ed37-5e59-4c4f-a914-ae7040d24755", "Venerate", 3, "CardEffectGainDevotionPoints", 6, 0,
            ["CardTraitExhaustState"]);
        Original(1, "6dc0af49-3ede-4091-afeb-8676eca15ca2", "InitiateSermon", 2, "CardEffectAddStatusEffect", 0, 1,
            ["CardTraitTreasure", "CardTraitScalingAddStatusEffect"]);
        Original(2, "b64904e9-ab9c-44d8-ac40-f53a9c9edd03", "Agonize", 2, "CardEffectDamage", 0, 0,
            ["CardTraitScalingAddDamage"]);
        Require(originals[1].GetProperty("Traits").EnumerateArray().Last().GetProperty("Int").GetInt32() == 2 &&
            originals[1].GetProperty("Traits").EnumerateArray().Last().GetProperty("Tracked").GetString() == "MonolithLevel" &&
            originals[2].GetProperty("Traits").EnumerateArray().Single().GetProperty("Int").GetInt32() == 30 &&
            originals[2].GetProperty("Traits").EnumerateArray().Single().GetProperty("Tracked").GetString() == "MonolithLevel",
            "Changed original level-scaled status or damage traits.");
        int[][] expected = [[0], [0], [0], [0, 2], [2, 4], [2, 4], [2, 4, 4], [6, 8, 8], [6, 0, 8], [6, 3, 8]];
        int[] levels = [0, 0, 1, 1, 1, 2, 2, 2, 2, 2];
        int[] points = [0, 0, 3, 3, 3, 3, 3, 3, 3, 3];
        int[] births = [ids[0], ids[3], ids[6]];
        for (int index = 0; index < 10; index++)
        {
            BattleTurnState after = After(index);
            Require(after.Spawn.Train.Context!.QueryFrame!.Devotion!.Level == levels[index] &&
                after.Spawn.Train.Context.QueryFrame.Devotion.Points == points[index], "Resource level/remainder differs at " + index);
            for (int unit = 0; unit < expected[index].Length; unit++)
            {
                CombatUnit actor = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(actor => actor.SpawnerCardId == births[unit]);
                int stacks = expected[index][unit];
                Require((actor.Status("zeal")?.Stacks ?? 0) == stacks && actor.Attack == 60 + 2 * stacks &&
                    actor.MaxHealth == 100 + stacks && actor.Health == 100, "Zeal spawn/scaling/removal, magnitude or no-heal witness differs at " + index);
            }
        }
        foreach (int index in new[] { 2, 5, 4, 7, 11 })
        {
            int cost = index is 2 or 5 ? 3 : 2;
            CombatContext after = After(index).Spawn.Train.Context!;
            bool routed = index is 2 or 5
                ? after.OtherPiles!.Single(item => item.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[index])
                : after.Cards.Discard.Any(card => card.InstanceId == ids[index]);
            Require(Before(index).Energy - After(index).Energy == cost &&
                routed,
                "Original card cost/Consume/Discard differs at " + index);
        }
        int enemyId = actions[10].GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
        CombatUnit enemyBefore = Before(10).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemyId);
        CombatUnit enemyAfter = After(10).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemyId);
        Require(enemyBefore.Team == CombatTeam.Enemy && enemyAfter.Attack - enemyBefore.Attack == 2 &&
            enemyAfter.MaxHealth - enemyBefore.MaxHealth == 2 && enemyAfter.Health == enemyBefore.Health,
            "Player Zeal magnitude incorrectly affected an enemy or healed it.");
        BattleTurnState damageBefore = Before(11), damageAfter = After(11);
        Require(damageBefore.Spawn.Train.Context!.QueryFrame!.Devotion!.Level == 2 &&
            damageBefore.Spawn.Train.Context.Cards.Hand.Single(card => card.InstanceId == ids[11]).DataId == "b64904e9-ab9c-44d8-ac40-f53a9c9edd03",
            "Missing original level-two Agonize play.");
        Require(damageBefore.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(prior => prior.Team == CombatTeam.Enemy &&
            prior.Health >= 60 && damageAfter.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == prior.Id &&
                prior.Health - unit.Health + (prior.Status("armor")?.Stacks ?? 0) - (unit.Status("armor")?.Stacks ?? 0) == 60)),
            "Missing native sixty-damage Agonize witness across armor and health.");
        int playerBirths = 0, enemyBirths = 0;
        foreach (FixtureValue row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                row.GetProperty("UnsupportedQueueReason").ValueKind == FixtureKind.Null, "Incomplete native birth.");
            TrainCombatState train = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            string parent = Serialize(train);
            int id = row.GetProperty("UnitId").GetInt32();
            CombatUnit actor = train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            var result = RelicBirthModel.CharacterAdded(train, id, row.GetProperty("FromCardId").GetInt32(),
                row.GetProperty("OnlyCovenants").GetBoolean(), row.GetProperty("DeferCallbacks").GetBoolean(),
                row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(result.Supported, result.UnsupportedReason ?? "Native birth unsupported.");
            Equal(result.State, actual, "complete native birth train");
            Equal(result.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "native birth queue");
            Equal(result.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "native dispatched queue");
            if (actor.Team == CombatTeam.Player)
            {
                playerBirths++;
                int stacks = 2 * train.Context!.QueryFrame!.Devotion!.Level;
                CombatUnit successor = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
                Require((successor.Status("zeal")?.Stacks ?? 0) - (actor.Status("zeal")?.Stacks ?? 0) == stacks &&
                    actual.Context!.BattleRng.Equals(train.Context.BattleRng.Range(0, 1).State),
                    "Native spawn used authored four stacks, missed a level or skipped the level-zero RNG draw.");
                DevotionState resource = train.Context.QueryFrame.Devotion!;
                var missingMultiplier = CardSpellModel.WithContext(train, train.Context.WithQueryFrame(train.Context.QueryFrame.With(
                    devotion: new(resource.Level, resource.Points, resource.FirstThreshold, resource.ThresholdIncrease, resource.PassiveStacksPerLevel))));
                Require(!RelicBirthModel.CharacterAdded(missingMultiplier, id, row.GetProperty("FromCardId").GetInt32(),
                    row.GetProperty("OnlyCovenants").GetBoolean(), row.GetProperty("DeferCallbacks").GetBoolean(),
                    row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!).Supported,
                    "Missing native global multiplier was inferred at birth.");
            }
            else { enemyBirths++; Equal(train.Context!.BattleRng, actual.Context!.BattleRng, "enemy birth RNG control"); }
            Require(parent == Serialize(train), "Birth replay mutated its parent.");
        }
        Require(playerBirths == 3 && enemyBirths > 0, "Missing level-zero/one/two births or enemy controls.");
        Gates(evidence.GetProperty("Gates"));
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"DEVOTION-SPAWN-BATTLE PASS: unchanged MonolithDevotion/ZealDamageIncrease and Venerate/Sermon/Agonize, levels 0/1/2, no retroactive/healing buffs, enemy magnitude control, sixty-damage scaling, 27 native gate boundaries, {playerBirths + enemyBirths} full births, complete battle/RNG and 16 immutable branches. Live multiplier-provider lifecycle remains separate.");

        void Original(int index, string id, string key, int cost, string type, int value, int minimum, string[] traits)
        {
            FixtureValue card = originals[index], effect = card.GetProperty("Effects").EnumerateArray().Single();
            Require(card.GetProperty("Id").GetString() == id && card.GetProperty("Key").GetString() == key &&
                card.GetProperty("Cost").GetInt32() == cost && effect.GetProperty("Type").GetString() == type &&
                effect.GetProperty("Int").GetInt32() == value && effect.GetProperty("PerLevel").GetInt32() == 0 &&
                effect.GetProperty("DevotionMinimum").GetInt32() == minimum && effect.GetProperty("DevotionMaximum").GetInt32() == 0 &&
                card.GetProperty("Traits").EnumerateArray().Select(trait => trait.GetProperty("Type").GetString()).SequenceEqual(traits),
                "Changed original card definition: " + key);
        }
    }
    private static void Gates(FixtureValue evidence)
    {
        Require(evidence.GetProperty("Completed").GetBoolean() && evidence.GetProperty("LiveContextUnchanged").GetBoolean() &&
            evidence.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Native gate calibration was not restored.");
        Equal(evidence.GetProperty("RngBefore").ToObjectGraph(), evidence.GetProperty("RngAfter").ToObjectGraph(), "gate RNG restoration");
        FixtureValue[] samples = evidence.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == 27 && samples.Count(sample => sample.GetProperty("OriginalSermon").GetBoolean()) == 2 &&
            samples.Count(sample => !sample.GetProperty("OriginalSermon").GetBoolean() && sample.GetProperty("Minimum").GetInt32() >= 0) == 24 &&
            samples.Where(sample => !sample.GetProperty("OriginalSermon").GetBoolean()).Select(sample =>
                (sample.GetProperty("Level").GetInt32(), sample.GetProperty("Minimum").GetInt32(), sample.GetProperty("Maximum").GetInt32())).Distinct().Count() == 25,
            "Missing native gate boundary matrix and original Sermon controls.");
        foreach (FixtureValue sample in samples) Verify(sample);
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        FixtureValue gated = samples.First(sample => sample.GetProperty("Minimum").GetInt32() == 1);
        TrainCombatState source = gated.GetProperty("Before").Deserialize<TrainCombatState>()!;
        CardActionEffect action = gated.GetProperty("Action").Deserialize<CardActionEffect>()!;
        Require(!CardSpellModel.TestPlay(CardSpellModel.WithContext(source, source.Context!.WithQueryFrame(new StatisticQueryFrame())), 2, [action], 0).Supported,
            "Missing Devotion was inferred for a gated effect.");
        using var keys = new BattleStateKeys();
        TrainCombatState multiplied = CardSpellModel.WithContext(source, source.Context!.WithQueryFrame(
            source.Context.QueryFrame!.With(devotion: new(0, 0, 3, 3, 2, 2))));
        Require(keys.SnapshotKey(action) != keys.SnapshotKey(samples[0].GetProperty("Action").Deserialize<CardActionEffect>()) &&
            keys.SnapshotKey(source) != keys.SnapshotKey(multiplied), "Pruning merged effect gates or passive multipliers.");

        static void Verify(FixtureValue sample)
        {
            int level = sample.GetProperty("Level").GetInt32(), min = sample.GetProperty("Minimum").GetInt32(), max = sample.GetProperty("Maximum").GetInt32();
            bool expected = (min <= 0 || level >= min) && (max <= 0 || level <= max);
            TrainCombatState before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
            CardActionEffect action = sample.GetProperty("Action").Deserialize<CardActionEffect>()!;
            string parent = Serialize(before);
            SpellCastCheck result = CardSpellModel.TestPlay(before, 2, [action], 0);
            Require(result.Supported && result.CanPlay == expected && result.CanPlay == sample.GetProperty("NativeTest").GetBoolean() &&
                sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                action.Tests?.ShouldTest == true && action.Tests.DevotionRequirement == min && action.Tests.DevotionRequirementMax == max &&
                before.Context!.QueryFrame!.Devotion!.Level == level &&
                (!sample.GetProperty("OriginalSermon").GetBoolean() || min == 1 && max == 0 && level is 0 or 1),
                "Native inclusive/disabled Devotion gate differs.");
            Equal(before, sample.GetProperty("Actual").Deserialize<TrainCombatState>(), "native test has no gameplay successor");
            Equal(sample.GetProperty("RngBefore").ToObjectGraph(), sample.GetProperty("RngAfter").ToObjectGraph(), "gate test RNG");
            Require(parent == Serialize(before), "A gate branch mutated its parent.");
        }
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? a, object? b, string label)
    {
        string? difference = ModelJson.Difference(Serialize(a), Serialize(b));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
