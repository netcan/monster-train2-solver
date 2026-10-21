using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomMoonCallbackChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("PreviewUpdatesSuppressed").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "AfterOriginalCoroutineAndTriggerQueue", "Incomplete native room moon capture.");
        bool shade = root.TryGetProperty("ShadeCallbacks", out FixtureValue shadeFlag) && shadeFlag.GetBoolean();
        int refreshes = 0, shifts = 0, attachments = 0, mixed = 0, changed = 0, shadeChanges = 0;
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == (shade ? 15 : 13) && root.GetProperty("ActorIds").GetArrayLength() == (shade ? 6 : 3), "Missing native controls.");
        foreach (FixtureValue row in rows)
        {
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            string operation = row.GetProperty("Operation").GetString()!;
            bool refresh = operation == "Refresh";
            MoonPhaseTransitionResult Replay()
            {
                if (refresh) return RoomMoonPhaseModel.Refresh(before, row.GetProperty("Characters").GetBoolean());
                if (operation == "SetMoonPhase") return MoonPhaseTransitionModel.Apply(before, new CardActionEffect("SetMoonPhase", "Room",
                    row.GetProperty("Phase").GetInt32(), false, false, []), playerActionOnly: false);
                if (operation == "Attach")
                {
                    CardActionEffect effect = root.GetProperty("AttachmentEffect").Deserialize<CardActionEffect>()!;
                    TrainSpellResult cast = CardSpellModel.Apply(before, 0, [effect], 0);
                    return new MoonPhaseTransitionResult(cast.State, cast.UnsupportedReason);
                }
                Require(operation == "Detach", "Unknown native operation.");
                CombatContext context = before.Context!;
                RoomCapacityState selected = context.RoomCapacities!.Single(room => room.RoomIndex == 0);
                Require(selected.Attachments!.Single().CapacityDelta == 0, "Unexpected removal capacity payload.");
                var removed = new RoomCapacityState(0, selected.PlayerMaximum, selected.EnemyMaximum, selected.IsPyre,
                    selected.Corruption, [], selected.MaxCorruption, selected.PermanentCorruption, selected.AttachmentsAddedFromCards);
                var capacities = context.RoomCapacities.Select(room => room.RoomIndex == 0 ? removed : room).ToArray();
                var detached = CardSpellModel.WithContext(before, context.WithRoomCapacities(capacities));
                RoomAttachmentUnitSyncResult synced = RoomAttachmentUnitModel.Synchronize(detached, context.RoomCapacities, capacities);
                return new MoonPhaseTransitionResult(synced.State, synced.UnsupportedReason);
            }
            void Check()
            {
                MoonPhaseTransitionResult result = Replay();
                Require(result.Supported, row.GetProperty("Label").GetString() + ": " + result.UnsupportedReason);
                string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options));
                Require(difference == null, row.GetProperty("Label").GetString() + ": " + difference);
            }
            Check(); Parallel.For(0, 8, _ => Check());
            if (refresh)
            {
                TrainSpellResult cast = CardSpellModel.Apply(before, 0, [new CardActionEffect("RefreshMoonPhase", "Room",
                    row.GetProperty("Characters").GetBoolean() ? 1 : 0, false, false, [])], 0);
                Require(cast.Supported, "Full-train card pipeline: " + cast.UnsupportedReason);
                Require(ModelJson.Difference(JsonSerializer.Serialize(cast.State, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options)) == null, "Full-train card refresh differs.");
            }
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Room moon callback mutated its parent.");
            Require(row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                "Room moon callbacks consumed gameplay RNG.");
            if (refresh) refreshes++; else if (operation == "SetMoonPhase") shifts++; else attachments++;
            if (actual.Rooms.SelectMany(room => room.Units).Any(unit => (unit.MoonPhaseMask & 3) == 3)) mixed++;
            changed += actual.Rooms.SelectMany(room => room.Units).Count(unit => before.Rooms.SelectMany(room => room.Units)
                .Any(prior => prior.Id == unit.Id && prior.Modifiers!.Upgrades.Count != unit.Modifiers!.Upgrades.Count));
            shadeChanges += actual.Rooms.SelectMany(room => room.Units).Count(unit => before.Rooms.SelectMany(room => room.Units)
                .Any(prior => prior.Id == unit.Id && prior.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade") !=
                    unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade")));
        }
        Require(refreshes == 9 && shifts == 4 && mixed > 0 && changed > 0, "Missing refresh, shift, mixed mask or actual callback witnesses.");
        Require(!shade || attachments == 2 && shadeChanges >= 12, "Missing explicit attachment/removal and original Nightingale callback changes.");
        Console.WriteLine($"ROOM-MOON-CALLBACK PASS refreshes={refreshes} shifts={shifts} attachments={attachments} mixed={mixed} changedUpgrades={changed} shadeChanges={shadeChanges} parallelBranches=8 immutable=true");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
