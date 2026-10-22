using System.Text.Json;
using System.Text.Json.Nodes;
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
        bool movement = root.TryGetProperty("MovementControls", out FixtureValue movementFlag) && movementFlag.GetBoolean();
        bool enemy = root.TryGetProperty("EnemyControls", out FixtureValue enemyFlag) && enemyFlag.GetBoolean();
        int refreshes = 0, shifts = 0, attachments = 0, mixed = 0, changed = 0, shadeChanges = 0, bumps = 0, maskChanges = 0;
        int enemyBumps = 0, enemyUpgradeChanges = 0, ordinaryEnemyChanges = 0;
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require((!enemy || movement) && (!movement || shade) && rows.Length == (enemy ? 39 : movement ? 37 : shade ? 15 : 13) &&
            root.GetProperty("ActorIds").GetArrayLength() == (enemy ? 12 : shade ? 6 : 3), "Missing native controls.");
        foreach (FixtureValue row in rows)
        {
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            if (enemy)
            {
                int[] actorIds = root.GetProperty("ActorIds").Deserialize<int[]>()!;
                CombatUnit[] carriers = before.Rooms.SelectMany(room => room.Units).Where(unit => actorIds.Contains(unit.Id)).ToArray();
                Require(carriers.Length == 12 && carriers.Count(unit => unit.Team == CombatTeam.Player) == 6 &&
                    carriers.Count(unit => unit.Team == CombatTeam.Enemy) == 6 &&
                    carriers.Count(unit => unit.Team == CombatTeam.Enemy && unit.Triggers.Any(trigger => trigger.Kind == "OnMoonLit")) == 3 &&
                    carriers.Count(unit => unit.Team == CombatTeam.Enemy && unit.Triggers.Any(trigger => trigger.Kind == "OnMoonShade")) == 3,
                    "Missing both original moon callback definitions on both native teams.");
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            string operation = row.GetProperty("Operation").GetString()!;
            bool refresh = operation == "Refresh";
            MoonPhaseTransitionResult Replay()
            {
                if (refresh) return RoomMoonPhaseModel.Refresh(before, row.GetProperty("Characters").GetBoolean());
                if (operation == "SetMoonPhase") return MoonPhaseTransitionModel.Apply(before, new CardActionEffect("SetMoonPhase", "Room",
                    row.GetProperty("Phase").GetInt32(), false, false, []), playerActionOnly: false);
                if (operation == "Bump")
                {
                    Require(movement, "Unexpected movement outside its native calibration.");
                    BumpResult moved = BumpModel.Apply(before, [row.GetProperty("UnitId").GetInt32()],
                        row.GetProperty("Amount").GetInt32(), row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!);
                    return new MoonPhaseTransitionResult(moved.State, moved.UnsupportedReason);
                }
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
            if (refresh) refreshes++;
            else if (operation == "SetMoonPhase") shifts++;
            else if (operation == "Bump")
            {
                bumps++;
                int id = row.GetProperty("UnitId").GetInt32();
                CombatUnit prior = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
                RoomCombatState room = actual.Rooms.Single(room => room.Units.Any(unit => unit.Id == id));
                CombatUnit moved = room.Units.Single(unit => unit.Id == id);
                if (moved.Team == CombatTeam.Enemy) enemyBumps++;
                if (row.GetProperty("Label").GetString() == "full-enters-mask")
                    GuardedMovement(before, row, id);
                Require(!before.Rooms.Single(old => old.Units.Any(unit => unit.Id == id)).RoomIndex.Equals(room.RoomIndex) &&
                    (moved.MoonPhaseMask & 3) == RoomMoonPhaseModel.Effective(actual.Context, room.RoomIndex,
                        actual.Context!.QueryFrame!.MoonPhase!.Value),
                    "Native Bump did not refresh its moved character after changing rooms.");
                if (prior.MoonPhaseMask != moved.MoonPhaseMask) maskChanges++;
                if (enemy && id == root.GetProperty("OrdinaryEnemyId").GetInt32())
                {
                    Require(moved.Team == CombatTeam.Enemy && moved.Triggers.All(trigger => !trigger.Kind.Contains("Moon")) &&
                        prior.MoonPhaseMask != moved.MoonPhaseMask &&
                        JsonSerializer.Serialize(prior.Modifiers) == JsonSerializer.Serialize(moved.Modifiers),
                        "Ordinary enemy movement must change understanding without moon upgrade callbacks.");
                    ordinaryEnemyChanges++;
                }
                if (row.GetProperty("Label").GetString() == "only-moved-character-refreshes-stale-phase")
                {
                    CombatUnit[] others = before.Rooms.SelectMany(item => item.Units).Where(unit => unit.Id != id).ToArray();
                    Require(others.Any(unit => unit.Team == CombatTeam.Player && (unit.MoonPhaseMask & 2) != 0) &&
                        actual.Context.QueryFrame.MoonPhase == 1 && (moved.MoonPhaseMask & 2) == 0,
                        "Missing stale-phase target-only refresh control.");
                    foreach (CombatUnit other in others)
                    {
                        CombatUnit current = actual.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == other.Id);
                        Require(current.MoonPhaseMask == other.MoonPhaseMask &&
                            JsonSerializer.Serialize(current.Modifiers) == JsonSerializer.Serialize(other.Modifiers),
                            "Bump refreshed a non-target's understanding or upgrades.");
                    }
                }
            }
            else attachments++;
            if (actual.Rooms.SelectMany(room => room.Units).Any(unit => (unit.MoonPhaseMask & 3) == 3)) mixed++;
            changed += actual.Rooms.SelectMany(room => room.Units).Count(unit => before.Rooms.SelectMany(room => room.Units)
                .Any(prior => prior.Id == unit.Id && prior.Modifiers!.Upgrades.Count != unit.Modifiers!.Upgrades.Count));
            enemyUpgradeChanges += actual.Rooms.SelectMany(room => room.Units).Count(unit => unit.Team == CombatTeam.Enemy &&
                before.Rooms.SelectMany(room => room.Units).Any(prior => prior.Id == unit.Id &&
                    prior.Modifiers!.Upgrades.Count != unit.Modifiers!.Upgrades.Count));
            shadeChanges += actual.Rooms.SelectMany(room => room.Units).Count(unit => before.Rooms.SelectMany(room => room.Units)
                .Any(prior => prior.Id == unit.Id && prior.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade") !=
                    unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade")));
        }
        Require(refreshes == (movement ? 15 : 9) && shifts == (movement ? 7 : 4) && mixed > 0 && changed > 0,
            "Missing refresh, shift, mixed mask or actual callback witnesses.");
        Require(!shade || attachments == (movement ? 4 : 2) && shadeChanges >= 12,
            "Missing explicit attachment/removal and original Nightingale callback changes.");
        Require(!movement || bumps == (enemy ? 13 : 11) && maskChanges >= (enemy ? 10 : 8), "Missing physical movement and understanding-change witnesses.");
        Require(!enemy || enemyBumps == 13 && enemyUpgradeChanges >= 40 && ordinaryEnemyChanges == 2,
            "Missing opposing callback carriers or ordinary enemy mask-change controls.");
        Console.WriteLine($"ROOM-MOON-CALLBACK PASS refreshes={refreshes} shifts={shifts} attachments={attachments} bumps={bumps} maskChanges={maskChanges} mixed={mixed} changedUpgrades={changed} shadeChanges={shadeChanges} enemyBumps={enemyBumps} enemyUpgradeChanges={enemyUpgradeChanges} ordinaryEnemyChanges={ordinaryEnemyChanges} parallelBranches=8 immutable=true");
    }
    private static void GuardedMovement(TrainCombatState before, FixtureValue row, int id)
    {
        void Reject(Action<JsonNode> change, string boundary)
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            change(altered);
            TrainCombatState state = altered.Deserialize<TrainCombatState>(ModelJson.Options)!;
            BumpResult result = BumpModel.Apply(state, [id], row.GetProperty("Amount").GetInt32(),
                row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!);
            Require(!result.Supported, "Uncalibrated movement accepted: " + boundary);
        }
        Reject(state => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
            .Single(unit => unit!["Id"]!.GetValue<int>() == id)!["MoonPhaseMask"] = null, "missing prior mask");
        Reject(state =>
        {
            foreach (JsonNode? room in state["Rooms"]!.AsArray()) room!["Preview"] = true;
        }, "moon understanding preview");
        Reject(state => state["Context"]!["RoomCapacities"]!.AsArray()
            .Single(room => room!["RoomIndex"]!.GetValue<int>() == 0)!["Attachments"]![0]!["MoonPhase"]!["AdditionalMask"] = 2,
            "unknown room moon rule");
        Reject(state => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
            .SelectMany(unit => unit!["Triggers"]!.AsArray())
            .First(trigger => trigger!["Kind"]!.GetValue<string>().Contains("Moon") && trigger["Effects"]!.AsArray().Count > 0)!
            ["Effects"]![0]!["Type"] = "UncalibratedMoonCallback", "unknown callback payload");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
