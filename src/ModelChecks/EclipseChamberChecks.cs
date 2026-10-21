using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EclipseChamberChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 8)
                {
                    int spawner = ids[index < 10 ? 0 : 5];
                    CombatUnit target = source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == spawner);
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                        play.CardInstanceId == ids[index] && play.TargetUnitId == target.Id);
                }
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            }
        return null;
    }
    internal static (int Characters, int Support) Refreshes(FixtureValue root)
    {
        int characters = 0, support = 0;
        foreach (FixtureValue row in root.GetProperty("EclipseChamberRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native moon refresh.");
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            bool refresh = row.GetProperty("Characters").GetBoolean();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatState state = before;
                if (row.TryGetProperty("PendingBefore", out FixtureValue pending))
                {
                    var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
                    foreach (FixtureValue item in pending.EnumerateArray())
                    {
                        Require(!item.GetProperty("HasSpecialContext").GetBoolean() && item.GetProperty("CanAttackOrHeal").GetBoolean() &&
                            item.GetProperty("Kind").GetString() is "OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost",
                            "Uncalibrated incoming refresh queue.");
                        int roomIndex = item.GetProperty("RoomIndex").GetInt32();
                        CombatUnit unit = state.Rooms.Single(room => room.RoomIndex == roomIndex).Units.Single(unit => unit.Id == item.GetProperty("UnitId").GetInt32());
                        queue.Add(new RoomCombatModel.QueuedCharacterTrigger(roomIndex, unit, item.GetProperty("Kind").GetString()!,
                            canFireTriggers: item.GetProperty("CanFireTriggers").GetBoolean(), triggerCount: item.GetProperty("TriggerCount").GetInt32()));
                    }
                    // Monster resource refresh drains the incoming native queue
                    // before the optional character-understanding refresh.
                    TrainCombatResult drained = TrainCombatModel.ApplyCharacterQueue(state, queue);
                    Require(drained.Supported, drained.UnsupportedReason ?? "Incoming moon queue unsupported.");
                    state = drained.State!;
                }
                var result = RoomMoonPhaseModel.Refresh(state, refresh);
                Require(result.Supported, result.UnsupportedReason ?? "Refresh unsupported.");
                if (row.TryGetProperty("PendingAfter", out FixtureValue pendingAfter))
                {
                    int queued = 0;
                    foreach (CombatUnit unit in state.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0))
                    {
                        CombatUnit updated = result.State!.Rooms.SelectMany(room => room.Units).Single(item => item.Id == unit.Id);
                        foreach (int bit in new[] { 2, 1 })
                        {
                            if ((unit.MoonPhaseMask & bit) == (updated.MoonPhaseMask & bit)) continue;
                            string kind = bit == 2 ? (updated.MoonPhaseMask & bit) != 0 ? "OnMoonLit" : "OnMoonlitLost" :
                                (updated.MoonPhaseMask & bit) != 0 ? "OnMoonShade" : "OnMoonshadeLost";
                            Require(unit.Triggers.Where(trigger => trigger.Kind == kind).All(trigger => trigger.Effects.Count == 0),
                                "Intermediate refresh retains a callback with effects; use the settled component boundary.");
                            queued++;
                        }
                    }
                    Require(pendingAfter.GetInt32() == queued, "Native refresh pending no-op callback count differs.");
                }
                Equal(result.State, after, "native complete resource/character refresh");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Refresh mutated its parent.");
            Require(before.Context!.QueryFrame!.MoonPhase == after.Context!.QueryFrame!.MoonPhase && before.Context.BattleRng.Equals(after.Context.BattleRng),
                "Resource refresh must not advance global phase or RNG.");
            if (refresh) characters++; else support++;
        }
        return (characters, support);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("EclipseChamberPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Eclipse witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var (characters, support) = Refreshes(root);
        Require(characters == 2 && support > 0, "Missing original full refresh and queued support-card controls.");
        int[] ids = root.GetProperty("EclipseChamberCardIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        bool movement = root.TryGetProperty("EclipseChamberMovement", out FixtureValue movementFlag) && movementFlag.GetBoolean();
        Require(ids.Length == (movement ? 12 : 8) && actions.Length == ids.Length,
            "Missing deployment, original Moonrise, two Eclipses, replacement, later birth or calibrated movement.");
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int index in new[] { 4, 5, 7 })
        {
            BattleTurnState state = After(index);
            Require(state.MoonPhase == "Full" && state.Spawn.Train.Rooms[0].Units.All(unit => unit.MoonPhaseMask == 7 &&
                unit.Status("fullmoon")?.Stacks == 1 && unit.Status("newmoon") == null),
                "Room understanding must include both phases while icons show only the global Full phase.");
            Require(state.Spawn.Train.Rooms[1].Units.Single().MoonPhaseMask == 6, "Eclipse leaked into the other floor.");
        }
        Require(After(6).Spawn.Train.Rooms[0].Units.All(unit => unit.MoonPhaseMask == 6), "Replacing Eclipse failed to remove its extra New bit.");
        int pulses = 0, controls = 0, deployment = 0, terminal = 0;
        foreach (FixtureValue row in root.GetProperty("TrainPhases").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Combat") continue;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            foreach (CombatUnit unit in before.Rooms.SelectMany(room => room.Units).Where(unit => ids.Contains(unit.SpawnerCardId)))
            {
                CombatUnit? updated = after.Rooms.SelectMany(room => room.Units).FirstOrDefault(item => item.Id == unit.Id);
                if (updated == null) continue;
                bool turnZero = before.Context!.QueryFrame!.Turn == 0;
                bool won = after.Context!.AllScenarioBossesDead == true;
                bool positive = !turnZero && !won && (unit.SpawnerCardId == ids[0] || unit.SpawnerCardId == ids[5]);
                Require(updated.BaseAttack - unit.BaseAttack == (positive ? 15 : 0), "Native Silent/floor/deployment post-combat boundary differs.");
                if (turnZero) deployment++; else if (won) terminal++; else if (positive) pulses++; else controls++;
            }
        }
        Require(pulses >= 4 && controls >= 4 && deployment >= 3 && terminal >= 3, "Missing positive Silent growth, ordinary/outside-floor, deployment or terminal controls.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ECLIPSE-CHAMBER PASS: two original room plays, replacement, later birth, {characters} character/{support} support refreshes, {pulses} Silent growth and {controls} floor/subtype controls, exact immutable action chains.");
    }
    internal static void Callbacks(FixtureValue root)
    {
        Require(root.GetProperty("EclipseChamberCallbacks").GetBoolean(), "Missing authored original-callback carriers.");
        Native(root);
        BattleTurnState[] states = root.GetProperty("Actions").EnumerateArray()
            .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        CombatUnit[] actors = states.SelectMany(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units))
            .Where(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnMoonLit") &&
                unit.Triggers.Any(trigger => trigger.Kind == "OnMoonlitLost")).ToArray();
        Require(actors.Select(unit => unit.Id).Distinct().Count() == 3 &&
            actors.Any(unit => (unit.MoonPhaseMask & 3) == 3) && actors.Any(unit =>
                unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "SeleneAcolyte_MagicPower_Upgrade")),
            "Missing three live callback actors, mixed masks or original phase upgrades.");
        Require(states.Any(state => state.Spawn.Train.Rooms[1].Units.Any(unit => unit.Triggers.Any(trigger =>
                trigger.Kind == "OnMoonLit") && unit.MoonPhaseMask == 6)), "Missing outside-floor callback control.");
        bool shade = root.TryGetProperty("EclipseChamberShadeCallbacks", out FixtureValue shadeFlag) && shadeFlag.GetBoolean();
        if (shade)
        {
            Require(root.GetProperty("EclipseChamberRecords").EnumerateArray().Count(row =>
                row.GetProperty("PendingBefore").EnumerateArray().Any(item => item.GetProperty("Kind").GetString() == "OnMoonShade")) == 2,
                "Missing both incoming native attachment callback queues.");
            Require(actors.All(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnMoonShade") &&
                unit.Triggers.Any(trigger => trigger.Kind == "OnMoonshadeLost")), "Missing original New callback payloads.");
            foreach (int index in new[] { 4, 5, 7 })
                Require(states[index].Spawn.Train.Rooms[0].Units.Where(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnMoonShade"))
                    .All(unit => unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade")),
                    "Original New gained upgrade missing after installation or later birth.");
            Require(states[6].Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit =>
                unit.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "Nightingale_Upgrade")),
                "Original New lost upgrade retained after replacement or leaked to the control floor.");
        }
        Console.WriteLine($"ECLIPSE-CALLBACK-CHAIN PASS authoredCarriers=true nativeOriginalPayloads=true callbackActors=3 mixedMasks=true outsideFloor=true shadeCallbacks={shade}");
    }
    internal static void Movement(FixtureValue root)
    {
        Require(root.GetProperty("EclipseChamberMovement").GetBoolean(), "Missing original movement scenario.");
        Callbacks(root);
        int moved = 0, callbacks = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray().Skip(8))
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string expected = moved % 2 == 0 ? "b67ca7bf-bd65-4d98-ae53-26397de1e355" : "01f1fe24-f72b-4749-833b-46796463cdd7";
            Require(before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId == expected,
                "Missing both original paid up/down pairs.");
            int origin = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId)).RoomIndex;
            int destination = after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId)).RoomIndex;
            Require(destination - origin == (moved % 2 == 0 ? 1 : -1), "Paid original card did not move its target.");
            if (MovementWitness(before, after, action)) callbacks++;
            moved++;
        }
        Require(moved == 4 && callbacks > 0, "Missing paid movement with actual callback changes.");
        Console.WriteLine($"ECLIPSE-MOVEMENT-CHAIN PASS originalMovementCards=4 movedCallbackActors=2 changedCallbacks={callbacks} mixedMasks=true exactCompleteChain=true");
    }
    internal static bool MovementWitness(BattleTurnState before, BattleTurnState after, PlayCardAction action, bool requireCallbacks = true)
    {
        string dataId = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
        int direction = dataId == "b67ca7bf-bd65-4d98-ae53-26397de1e355" ? 1 :
            dataId == "01f1fe24-f72b-4749-833b-46796463cdd7" ? -1 : 0;
        if (direction == 0) return false;
        RoomCombatState origin = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId));
        RoomCombatState destination = after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId));
        CombatUnit old = origin.Units.Single(unit => unit.Id == action.TargetUnitId);
        CombatUnit current = destination.Units.Single(unit => unit.Id == action.TargetUnitId);
        if (old.Team != CombatTeam.Player || old.Triggers.Any(trigger => trigger.Kind == "OnMoonShade") != requireCallbacks ||
            origin.RoomIndex == destination.RoomIndex || (old.MoonPhaseMask & 1) == (current.MoonPhaseMask & 1)) return false;
        Require(destination.RoomIndex - origin.RoomIndex == direction &&
            current.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade") == (requireCallbacks && (current.MoonPhaseMask & 1) != 0) &&
            (current.MoonPhaseMask & 4) == (old.MoonPhaseMask & 4), "Paid movement callback upgrade or cached phase differs.");
        foreach (CombatUnit other in before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Id != old.Id))
        {
            CombatUnit unchanged = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == other.Id);
            Require(unchanged.MoonPhaseMask == other.MoonPhaseMask, "Paid movement refreshed a non-target's cached moon mask.");
        }
        return true;
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
