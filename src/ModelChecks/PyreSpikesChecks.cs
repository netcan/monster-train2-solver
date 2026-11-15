using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreSpikesChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(IReadOnlyList<string> paths)
    {
        var modes = new HashSet<string>(StringComparer.Ordinal);
        int liveWrites = 0, liveEntries = 0, exchanges = 0;
        foreach (string path in paths)
        {
            using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
            string mode = root.GetProperty("ModifierScenario").GetString()!;
            Require(modes.Add(mode) && mode is "pyre-spikes-sweep" or "pyre-spikes-single" or "pyre-spikes-sustain",
                "Duplicate or unexpected native Pyre spikes mode.");
            bool sweep = mode != "pyre-spikes-single", sustain = mode == "pyre-spikes-sustain";
            Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
                root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
                root.GetProperty("PyreSpikesPrepared").GetBoolean() && root.GetProperty("PyreSpikesSweep").GetBoolean() == sweep &&
                !root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
                "Unexpected native version, controls or incomplete terminal loss.");
            foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
            Require(root.GetProperty("Actions").GetArrayLength() == 0 && root.GetProperty("Turns").GetArrayLength() == 4,
                "Pyre spikes must use four ordinary EndTurns without card actions.");
            var initial = root.GetProperty("PyreSpikesInitial").Deserialize<RoomCombatState>()!;
            var pyre = initial.Units.Single(unit => unit.IsPyre);
            Require(pyre.Health == (sustain ? 6 : 3) && pyre.MaxHealth == 80 && pyre.Attack == 45 &&
                (pyre.Status("sweep")?.Stacks > 0) == sweep, "Native Pyre controls changed.");
            var writes = root.GetProperty("PyreSpikesWrites").EnumerateArray().ToArray();
            Require(writes.Length > 0 && writes.All(row => row.GetProperty("Actual").GetInt32() == row.GetProperty("Damage").GetInt32()),
                "An observed native damage setter did not store its argument.");
            var entries = root.GetProperty("PyreSpikesEntries").EnumerateArray().ToArray();
            liveWrites += writes.Count(row => !row.GetProperty("Preview").GetBoolean());
            liveEntries += entries.Count(row => !row.GetProperty("Preview").GetBoolean());
            bool InStage(FixtureValue row, int stage) => !row.GetProperty("Preview").GetBoolean() &&
                row.GetProperty("Stages").Deserialize<int[]>()!.Contains(stage);
            var attacks = root.GetProperty("Stages").EnumerateArray().Where(row => row.GetProperty("Kind").GetString() == "Exchange" &&
                row.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == 3 &&
                row.GetProperty("Before").GetProperty("Units").Deserialize<CombatUnit[]>()!.Any(unit => unit.Team == CombatTeam.Enemy)).ToArray();
            Require(attacks.Length == (sustain ? 3 : 1), "Missing nonlethal/lethal Pyre attack exchanges.");
            for (int i = 0; i < attacks.Length; i++)
            {
                var stage = attacks[i]; int index = stage.GetProperty("Index").GetInt32();
                var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
                var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
                var guard = before.Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
                var nextGuard = actual.Units.Single(unit => unit.Id == guard.Id);
                bool lethal = i == attacks.Length - 1;
                Require(guard.Attack == 0 && guard.MaxHealth == 1000 && guard.Status("spikes")?.Stacks == 2 &&
                    nextGuard.Health == guard.Health - (i == 0 ? 20 : 45),
                    "Guard must retain its native two Spikes and first-hit armor, taking exactly one Pyre attack per exchange.");
                var nextPyre = actual.Units.SingleOrDefault(unit => unit.IsPyre);
                Require(lethal ? nextPyre == null : nextPyre?.Health == (i == 0 ? 3 : 1),
                    "Native Pyre health or terminal room membership differs.");
                Require(actual.Context!.Statistics!.LastAttackDamageDealt == (lethal ? 2 : 45),
                    "Lethal retaliation must keep two damage; surviving sweep must drain its queued 45-damage callbacks.");
                if (!sustain)
                    Require(actual.Units.Single(unit => unit.AssetKey == "MageT1_Junker").Health == 1,
                        "Lethal first-target retaliation must stop before attacking the second sweep target.");
                var localWrites = writes.Where(row => InStage(row, index)).OrderBy(row => row.GetProperty("Sequence").GetInt32()).ToArray();
                var spikesWrite = localWrites.Last(row => row.GetProperty("Damage").GetInt32() == 2);
                int spikeSequence = spikesWrite.GetProperty("Sequence").GetInt32();
                Require(spikesWrite.GetProperty("QueueRunning").GetBoolean() == sweep &&
                    spikesWrite.GetProperty("Caller").GetString()!.StartsWith("<ApplyDamage>", StringComparison.Ordinal),
                    "Native retaliation must write two damage inside the correct sweep queue gate.");
                var fires = entries.Where(row => InStage(row, index) && row.GetProperty("Phase").GetString() == "Fire").ToArray();
                var attackFires = fires.Where(row => row.GetProperty("UnitId").GetInt32() == pyre.Id &&
                    row.GetProperty("Trigger").GetString() is "OnAttackingBeforeDamage" or "OnAttacking").ToArray();
                if (lethal)
                {
                    Require(localWrites[^1].GetProperty("Damage").GetInt32() == 2 &&
                        !fires.Any(row => row.GetProperty("Sequence").GetInt32() > spikeSequence),
                        "Native main combat must stop before draining any callbacks after lethal Pyre retaliation.");
                    Require(attackFires.Length == (sweep ? 0 : 2),
                        "Lethal sweep must cancel both queued attack callbacks; single attack must fire both before Spikes.");
                    Require(entries.Count(row => InStage(row, index) && row.GetProperty("Phase").GetString() == "Queue" &&
                        row.GetProperty("UnitId").GetInt32() == pyre.Id &&
                        row.GetProperty("Trigger").GetString() is "OnAttackingBeforeDamage" or "OnAttacking") == 2,
                        "Native Pyre must enqueue both attack callbacks even when the lethal sweep never fires them.");
                }
                else
                {
                    Require(attackFires.Length == (i == 0 ? 4 : 2) && attackFires.All(row => row.GetProperty("Sequence").GetInt32() > spikeSequence) &&
                        localWrites[^1].GetProperty("Damage").GetInt32() == 45 &&
                        localWrites[^1].GetProperty("Caller").GetString()!.StartsWith("<FireTriggers>", StringComparison.Ordinal),
                        "A surviving sweep must still fire every attack callback and restore its captured attack damage.");
                }
                string parent = JsonSerializer.Serialize(before, ModelJson.Options);
                void Replay()
                {
                    var replay = RoomCombatModel.Exchange(before);
                    Require(replay.Supported && ModelJson.Difference(JsonSerializer.Serialize(replay.State!.Units), JsonSerializer.Serialize(actual.Units)) == null &&
                        ModelJson.Difference(JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(replay.State.Context)),
                            JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual.Context))) == null,
                        "Independent native Pyre attack exchange replay differs.");
                }
                Replay(); Parallel.For(0, 32, _ => Replay());
                Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Pyre exchange branches mutated their shared parent.");
                exchanges++;
            }
            RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        }
        Require(modes.SetEquals(new[] { "pyre-spikes-sweep", "pyre-spikes-single", "pyre-spikes-sustain" }),
            "All three sweep/single/nonlethal native controls are required.");
        Console.WriteLine($"PYRE-SPIKES PASS: three terminal losses/four EndTurns each, {exchanges} direct attacks/32 immutable branches each, first-target sweep cancellation, surviving attack callbacks, {liveWrites} live statistic writes/{liveEntries} callback entries, full rooms/train/chains and 16 independent branches each.");
    }
}
