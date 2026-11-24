using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TranscendImpChecks
{
    private const string DataId = "57c1d696-f48d-48a3-9ac5-e868f3f669fe";
    private const string AggressiveImpDataId = "5a8399dd-a958-4317-b814-de0e2972ca3d";
    private const string WelderHelperDataId = "61d9f4f1-80f5-416b-bf22-1836925c8ccd";
    private const string CheeryDeathcapDataId = "97260e75-4e11-439e-8eda-cd961143fc12";
    private const string EnthusiasticFanDataId = "adaaaa9d-56a9-4a5f-a347-ee71d4767df5";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] imps, int[] units, int recall)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        bool Played(int id) => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") > 0;
        PlayCardAction? Play(int id, int room) => plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == room);
        if (!Played(imps[0])) return Play(imps[0], 1);
        for (int index = 0; index < units.Length; index++)
            if (!Played(units[index])) return Play(units[index], units.Length > 4 && index >= 2 ? 1 : 0);
        if (!Played(imps[1])) return Play(imps[1], 0);
        if (!Played(recall))
        {
            int target = source.Spawn.Train.Rooms.SelectMany(room => room.Units).First(unit => unit.SpawnerCardId == units[0]).Id;
            return plays.FirstOrDefault(play => play.CardInstanceId == recall && play.TargetUnitId == target);
        }
        if (!Played(imps[2])) return Play(imps[2], 0);
        return plays.FirstOrDefault(play => play.RoomIndex == 0 && units.Contains(play.CardInstanceId));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("TranscendImpPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original replay witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] sources = root.GetProperty("TranscendImpSourceCardIds").Deserialize<int[]>()!;
        Require(sources.Length is 2 or 3 or 4 or 6, "Unexpected original replay source count.");
        bool hasOriginalRage = sources.Length >= 3, hasOriginalArmor = sources.Length >= 4;
        bool hasEnemyStatuses = sources.Length == 6;
        int index = 0;
        foreach (FixtureValue row in root.GetProperty("TranscendImpRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete original replay.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            int self = row.GetProperty("SelfId").GetInt32(), room = row.GetProperty("RoomIndex").GetInt32();
            string kind = row.GetProperty("Kind").GetString()!;
            BattleTriggerHistory history = before.Context!.TriggerCounts!.ReplayHistory!;
            Require(kind == "OnSpawn" && history.Batches.Count == (index == 0 ? 0 : sources.Length), "Missing empty/ordered positive history.");
            if (index > 0 && hasOriginalRage)
            {
                CombatEffect rage = history.Batches[2].Effects.Single();
                Require(before.Context.FindCard(sources[2])?.DataId == AggressiveImpDataId &&
                    history.Batches[2].SpawnerCardId == sources[2] && BattleTriggerReplayModel.Calibrated(rage),
                    "Missing original Fledgling Imp Rage source and fixed payload.");
                Require(!BattleTriggerReplayModel.Calibrated(rage.WithAction(rage.Action!.WithStatuses(
                    new[] { rage.Action.Statuses[0].WithStacks(6) }))) &&
                    !BattleTriggerReplayModel.Calibrated(new CombatEffect(rage.Type, rage.Value, rage.Counter,
                        rage.Destination, rage.Count, rage.CardPool, rage.SkipDuplicateInHand, action: rage.Action)),
                    "Altered Rage stacks or uncaptured scaling must remain unsupported.");
            }
            if (index > 0 && hasOriginalArmor)
            {
                CombatEffect armor = history.Batches[3].Effects.Single();
                Require(before.Context.FindCard(sources[3])?.DataId == WelderHelperDataId &&
                    history.Batches[3].SpawnerCardId == sources[3] && BattleTriggerReplayModel.Calibrated(armor),
                    "Missing original Welder Helper Armor source and fixed payload.");
                Require(!BattleTriggerReplayModel.Calibrated(armor.WithActionValue(99)) &&
                    !BattleTriggerReplayModel.Calibrated(armor.WithAction(armor.Action!.WithStatuses(
                        new[] { armor.Action.Statuses[0].WithStacks(21) }))),
                    "Altered Armor chance or stacks must remain unsupported.");
            }
            if (index > 0 && hasEnemyStatuses)
            {
                CombatEffect decay = history.Batches[4].Effects.Single();
                CombatEffect sap = history.Batches[5].Effects.Single();
                Require(before.Context.FindCard(sources[4])?.DataId == CheeryDeathcapDataId &&
                    before.Context.FindCard(sources[5])?.DataId == EnthusiasticFanDataId &&
                    history.Batches[4].SpawnerCardId == sources[4] &&
                    history.Batches[5].SpawnerCardId == sources[5] &&
                    BattleTriggerReplayModel.Calibrated(decay) && BattleTriggerReplayModel.Calibrated(sap),
                    "Missing original Cheery Deathcap and Enthusiastic Fan cached effects.");
                Require(!BattleTriggerReplayModel.Calibrated(decay.WithAction(decay.Action!.WithStatuses(
                    new[] { decay.Action.Statuses[0].WithStacks(8) }))) &&
                    !BattleTriggerReplayModel.Calibrated(sap.WithAction(sap.Action!.WithStatuses(
                        new[] { sap.Action.Statuses[0].WithStacks(4) }))),
                    "Altered Decay or Sap stacks must remain unsupported.");
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainSpellResult result = BattleTriggerReplayModel.Apply(before, room, kind, self);
                Require(result.Supported, result.UnsupportedReason ?? "Replay unsupported.");
                Equal(result.State, after, "complete native history replay");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its parent history.");
            Equal(history, after.Context!.TriggerCounts!.ReplayHistory, "Replayed effects must not append history");
            CombatUnit oldSelf = before.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == self);
            CombatUnit newSelf = after.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == self);
            if (index == 0) Equal(before, after, "Empty history preserves state/RNG");
            else
            {
                Require(after.Context.Gold - before.Context.Gold == 10 && newSelf.BaseAttack - oldSelf.BaseAttack == 6 &&
                    newSelf.MaxHealth - oldSelf.MaxHealth == 8 && history.Batches.Select(batch => batch.SpawnerCardId).SequenceEqual(sources),
                    "Replay lost ordering, gold or retargeted Self upgrades.");
                if (hasOriginalRage)
                {
                    Require((newSelf.Status("buff")?.Stacks ?? 0) - (oldSelf.Status("buff")?.Stacks ?? 0) == 5,
                        "Original Fledgling Imp Rage did not reach replay self.");
                    foreach (CombatUnit old in before.Rooms.SelectMany(item => item.Units))
                    {
                        CombatUnit next = after.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == old.Id);
                        int expected = old.Team == CombatTeam.Player && before.Rooms.Single(item => item.RoomIndex == room).Units.Any(unit => unit.Id == old.Id) ? 5 : 0;
                        Require((next.Status("buff")?.Stacks ?? 0) - (old.Status("buff")?.Stacks ?? 0) == expected,
                            "Original Fledgling Imp Rage affected the wrong room or team.");
                    }
                }
                if (hasOriginalArmor)
                {
                    CombatEffect armor = history.Batches[3].Effects.Single();
                    CardTargets front = CardTargetModel.Collect(before, room, armor.Action!, Array.Empty<int>());
                    Require(front.Supported && front.UnitIds.Count == 1, "Welder Helper replay needs one original friendly front target.");
                    foreach (CombatUnit old in before.Rooms.SelectMany(item => item.Units))
                    {
                        CombatUnit next = after.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == old.Id);
                        int expected = old.Id == front.UnitIds[0] ? 20 : 0;
                        Require((next.Status("armor")?.Stacks ?? 0) - (old.Status("armor")?.Stacks ?? 0) == expected,
                            "Original Welder Helper Armor affected a non-front target.");
                    }
                    Require(JsonSerializer.Serialize(before.Context.BattleRng, ModelJson.Options) !=
                        JsonSerializer.Serialize(after.Context.BattleRng, ModelJson.Options),
                        "Welder Helper's guaranteed chance must advance native Battle RNG.");
                }
                if (hasEnemyStatuses)
                {
                    CombatUnit[] enemies = before.Rooms.Single(item => item.RoomIndex == room).Units
                        .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
                    Require(index == 1 ? enemies.Length == 0 : enemies.Length >= 2 &&
                        enemies.Any(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack") &&
                        enemies.Any(unit => unit.AssetKey == "Boss_Level1_Basic"),
                        "Missing empty pre-wave room or natural turn-1 enemies.");
                    foreach (CombatUnit old in before.Rooms.SelectMany(item => item.Units))
                    {
                        CombatUnit next = after.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == old.Id);
                        bool target = enemies.Any(unit => unit.Id == old.Id);
                        Require((next.Status("decay")?.Stacks ?? 0) - (old.Status("decay")?.Stacks ?? 0) == (target ? 7 : 0) &&
                            (next.Status("debuff")?.Stacks ?? 0) - (old.Status("debuff")?.Stacks ?? 0) == (target ? 3 : 0),
                            "Original enemy-room Decay or Sap affected the wrong unit.");
                    }
                }
                if (index == 2) Require(before.Rooms.SelectMany(item => item.Units).All(unit => unit.SpawnerCardId != sources[0]) &&
                    (hasEnemyStatuses ? before.Context.FindCard(sources[0]) != null :
                        before.Context.Cards.Hand.Any(card => card.InstanceId == sources[0])),
                    "Missing retained history after source recall.");
            }
            Require(!BattleTriggerReplayModel.Apply(before, room, "OnDeath", self).Supported &&
                !BattleTriggerReplayModel.Apply(before, room, kind, int.MaxValue).Supported, "Missing uncaptured-kind/self guards.");
            index++;
        }
        Require(index == 3, "Missing three original replay casts.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("TRANSCEND-IMP PASS: original empty/ordered OnSpawn replay, retained source history after recall, Self upgrades" +
            (hasOriginalRage ? ", original Fledgling Imp room Rage" : "") +
            (hasOriginalArmor ? ", original Welder Helper front Armor and Battle RNG" : "") +
            (hasEnemyStatuses ? ", original Cheery Deathcap Decay and Enthusiastic Fan Sap on natural enemies" : "") +
            ", no recursive history growth, immutable parallel contexts and full action chains.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
