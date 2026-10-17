using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoonPhaseStatusChecks
{
    private const string DataId = "e013b17d-753b-47b8-a837-02af85addb4b";
    private const string AssetKey = "MoonPhaseAddArmorToSilent";
    private const string Effect = "RelicEffectAddStatusEffectOnMoonPhase";
    private const string Silent = "SubtypesData_Silent";

    private sealed class Operation
    {
        public string OwnerDataId { get; set; } = "";
        public int EffectIndex { get; set; }
        public string Phase { get; set; } = "";
        public string SourceTeam { get; set; } = "";
        public string TargetMode { get; set; } = "";
        public string? RequiredSubtype { get; set; }
        public bool DoNotAnimateCamera { get; set; }
        public int MoonPhaseMask { get; set; }
        public string[] StatusIds { get; set; } = [];
        public int[] StatusStacks { get; set; } = [];
        public int[] TargetIds { get; set; } = [];
        public TrainCombatState Before { get; set; } = null!;
        public TrainCombatState? After { get; set; }
    }

    internal static void Run()
    {
        CombatRelicState Owner(bool preview = false)
        {
            var rule = new RelicMoonPhaseStatusRule("Monsters", "Tower",
                [new CombatStatus("armor", 10, paramInt: 1, removeWhenTriggered: true,
                    removeDuringDeployment: true, triggerVfxEnemy: true, triggerVfxPlayer: true,
                    stackable: true, hidden: false, displayCategory: "Persistent", propagatable: false,
                    excludeHeroPropagation: false, excludeMonsterPropagation: false)], Silent, true, 1);
            var resource = new RelicResourceEffect(0, Effect, 0, 1, 0, preview,
                conditions: [], moonPhaseStatus: rule);
            return new CombatRelicState(DataId, AssetKey, [Effect], isCovenant: false,
                disallowedInPlacementPhase: false, resourceEffects: [resource]);
        }

        TrainCombatState Train(CombatRelicState owner, bool previewRoom = false, bool targetCallback = false)
        {
            UnityRng rng = UnityRng.Seed(911);
            var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
                queryFrame: new StatisticQueryFrame(energy: 3, runningCombat: true, turn: 1,
                    moonPhase: 1, moonPhaseTrackingActive: true),
                energyState: new BattleEnergyState(10, 0, 0, "MonsterTurn", true), relics: [owner]);
            CombatTrigger[] callbacks = targetCallback
                ? [new CombatTrigger("OnArmorAdded", false, false, false, 1, [])] : [];
            CombatUnit Unit(int id, string key, CombatTeam team, int hp, bool pyre = false,
                string[]? subtypes = null, CombatStatus[]? statuses = null, string[]? immunities = null,
                CombatTrigger[]? triggers = null) => new(id, key, team, 5, hp, Math.Max(hp, 20), true,
                    pyre, false, statuses ?? [], triggers ?? [], size: 1, statusImmunities: immunities,
                    subtypes: subtypes ?? []);
            var rooms = new[]
            {
                new RoomCombatState(0, false, [
                    Unit(1, "silent-first", CombatTeam.Player, 20, subtypes: [Silent], statuses: [new CombatStatus("armor", 3)]),
                    Unit(2, "silent-stealth", CombatTeam.Player, 20, subtypes: [Silent], statuses: [new CombatStatus("stealth", 1)]),
                    Unit(3, "silent-untouchable", CombatTeam.Player, 20, subtypes: [Silent], statuses: [new CombatStatus("untouchable", 1)]),
                    Unit(4, "silent-immune", CombatTeam.Player, 20, subtypes: [Silent], immunities: ["armor"]),
                    Unit(5, "silent-purified", CombatTeam.Player, 20, subtypes: [Silent], statuses: [new CombatStatus("purify", 1)]),
                    Unit(6, "not-silent", CombatTeam.Player, 20, subtypes: ["SubtypesData_Awoken"]),
                    Unit(7, "enemy-silent", CombatTeam.Enemy, 20, subtypes: [Silent]),
                    Unit(8, "silent-pyre", CombatTeam.Player, 20, pyre: true, subtypes: [Silent]),
                    Unit(9, "dead-silent", CombatTeam.Player, 0, subtypes: [Silent])
                ], [], context, previewRoom),
                new RoomCombatState(1, false, [], [], context, previewRoom),
                new RoomCombatState(2, false, [Unit(10, "silent-second", CombatTeam.Player, 20,
                    subtypes: [Silent], triggers: callbacks)], [], context, previewRoom)
            };
            return new TrainCombatState(rooms, [], 1, context);
        }

        var owner = Owner();
        TrainCombatState source = Train(owner);
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        RelicMoonPhaseStatusResult newMoon = RelicMoonPhaseStatusModel.Apply(source, 1);
        RelicMoonPhaseStatusResult fullMoon = RelicMoonPhaseStatusModel.Apply(source, 2);
        var newMoonUnits = newMoon.State!.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
        Require(newMoon.Supported && fullMoon.Supported &&
            newMoonUnits[1].Status("armor")?.Stacks == 13 &&
            newMoon.State.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2).Status("armor")?.Stacks == 10 &&
            newMoon.State.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 10).Status("armor")?.Stacks == 10 &&
            newMoon.State.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 4).Status("armor") == null &&
            newMoon.State.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 5).Status("armor") == null &&
            Enumerable.Range(3, 7).All(id => !newMoonUnits.TryGetValue(id, out CombatUnit? unit) || unit.Status("armor") == null) &&
            fullMoon.State!.Rooms.SelectMany(room => room.Units).All(unit => unit.Status("armor")?.Stacks != 10) &&
            JsonSerializer.Serialize(source, ModelJson.Options) == parent,
            "Moon-phase armor did not respect the New-only team/subtype/target filters or immutable parent state.");

        MoonPhaseTransitionResult full = MoonPhaseTransitionModel.Apply(source,
            new CardActionEffect("SetMoonPhase", "Room", 2, false, false, []));
        MoonPhaseTransitionResult nextNew = MoonPhaseTransitionModel.Apply(full.State!,
            new CardActionEffect("AdvanceMoonPhase", "Room", 1, false, false, []));
        Require(full.Supported && nextNew.Supported &&
            nextNew.State!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 1).Status("armor")?.Stacks == 13 &&
            RelicMoonPhaseStatusModel.Apply(Train(Owner(preview: false), previewRoom: true), 1, preview: true).Supported &&
            RelicMoonPhaseStatusModel.Apply(Train(Owner(preview: false), previewRoom: true), 1, preview: true).State!
                .Rooms.SelectMany(room => room.Units).All(unit => unit.Status("armor")?.Stacks != 10) &&
            RelicMoonPhaseStatusModel.Apply(Train(Owner(preview: true), previewRoom: true), 1, preview: true).State!
                .Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 10).Status("armor")?.Stacks == 10 &&
            !RelicMoonPhaseStatusModel.Apply(Train(owner, targetCallback: true), 1).Supported,
            "Moon-phase transition, preview gate or unsupported status-callback guard changed.");
        Console.WriteLine("ORIGINAL-MOON-PHASE-STATUS-CHECKS PASS: phase mask, tower targeting, status controls, preview gate, callback rejection and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 190 &&
            root.GetProperty("ModifierScenario").GetString() == "original-moon-phase-status-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Turns").GetArrayLength() >= 1,
            "Original moon-phase status battle is incomplete or from an unexpected game build.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Moon-phase status capture has nonzero " + field + ".");

        FixtureValue controls = root.GetProperty("OriginalMoonPhaseStatusScenario");
        int[] acolytes = controls.GetProperty("AcolyteCardIds").Deserialize<int[]>()!;
        int[] faes = controls.GetProperty("FaeCardIds").Deserialize<int[]>()!;
        Require(acolytes.Length == 2 && faes.Length == 2, "Moon-phase status fixture lost its multi-room or phase controls.");
        PlayCardAction[] actions = root.GetProperty("Actions").EnumerateArray()
            .Select(item => item.GetProperty("Action").Deserialize<PlayCardAction>()!).ToArray();
        int[] expectedActions = [acolytes[0], acolytes[1], faes[0], faes[1]];
        Require(actions.Take(expectedActions.Length).Select(action => action.CardInstanceId).SequenceEqual(expectedActions),
            "Native battle did not play both Silent witnesses before the two original Fae phase advances.");

        Operation[] operations = root.GetProperty("RelicMoonPhaseStatusOperations").EnumerateArray()
            .Select(ReadOperation).ToArray();
        Require(operations.Length >= 2 && operations.All(operation => operation.After != null &&
            operation.OwnerDataId == DataId && operation.EffectIndex == 0 && operation.SourceTeam == "Monsters" &&
            operation.TargetMode == "Tower" && operation.RequiredSubtype == Silent && operation.DoNotAnimateCamera &&
            operation.MoonPhaseMask == 1 && operation.StatusIds.SequenceEqual(["armor"]) &&
            operation.StatusStacks.SequenceEqual([10]) && operation.Phase is "Full" or "New"),
            "Original moon-phase status owner has missing callbacks or changed native effect data.");
        Require(operations.Any(operation => operation.Phase == "Full") && operations.Any(operation => operation.Phase == "New"),
            "Native callback witnesses do not include Full and New moon.");

        int newMoonTargets = 0;
        int crossRoomWitnesses = 0;
        foreach (Operation operation in operations)
        {
            TrainCombatState before = operation.Before;
            TrainCombatState after = operation.After!;
            int phase = operation.Phase == "New" ? 1 : 2;
            RelicMoonPhaseStatusResult expected = RelicMoonPhaseStatusModel.Apply(before, phase,
                before.Rooms.Any(room => room.Preview), before.Rooms.Any(room => room.Deployment));
            Require(expected.Supported, "Captured callback pre-state was rejected by the model: " + expected.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected.State, ModelJson.Options),
                JsonSerializer.Serialize(after, ModelJson.Options));
            Require(difference == null, "Moon-phase status callback differs from full-state replay: " + difference);

            CombatUnit[] units = before.Rooms.SelectMany(room => room.Units).ToArray();
            int[] modelTargets = phase == 1
                ? before.Rooms.OrderBy(room => room.RoomIndex).SelectMany(room => room.Units)
                    .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 &&
                    unit.DeathState?.IsDestroyed != true && unit.Status("untouchable") == null && unit.Subtypes.Contains(Silent))
                    .Select(unit => unit.Id).ToArray()
                : [];
            // Native keeps the previous target list when the phase mask returns early.
            int[] observedTargets = phase == 1
                ? operation.TargetIds.Where(id => units.Any(unit => unit.Id == id && !unit.IsPyre)).ToArray()
                : [];
            Require(modelTargets.OrderBy(id => id).SequenceEqual(observedTargets.OrderBy(id => id)) &&
                observedTargets.Distinct().Count() == observedTargets.Length,
                "Native tower target membership differs from the calibrated live Silent target selector.");
            if (phase == 1)
            {
                int[] acrossRooms = modelTargets.Select(id => before.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex)
                    .Distinct().ToArray();
                if (acrossRooms.Length >= 2) crossRoomWitnesses++;
                newMoonTargets += modelTargets.Length;
            }
        }
        Require(newMoonTargets >= 2 && crossRoomWitnesses > 0,
            "New-moon callbacks lack two positive Silent targets across multiple rooms.");

        foreach (FixtureValue item in root.GetProperty("Actions").EnumerateArray().Take(expectedActions.Length))
        {
            BattleTurnState before = item.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = item.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = item.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleActionResult replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "Original moon-phase status action differs from independent card replay: " + replay.Reason);
        }
        Console.WriteLine($"NATIVE-ORIGINAL-MOON-PHASE-STATUS PASS: {operations.Length} Full/New callbacks, {newMoonTargets} New-moon Silent targets across {crossRoomWitnesses} multi-room witnesses, four replayed card actions.");
    }

    private static Operation ReadOperation(FixtureValue value) => new()
    {
        OwnerDataId = value.GetProperty("OwnerDataId").GetString()!,
        EffectIndex = value.GetProperty("EffectIndex").GetInt32(),
        Phase = value.GetProperty("Phase").GetString()!,
        SourceTeam = value.GetProperty("SourceTeam").GetString()!,
        TargetMode = value.GetProperty("TargetMode").GetString()!,
        RequiredSubtype = value.GetProperty("RequiredSubtype").GetString(),
        DoNotAnimateCamera = value.GetProperty("DoNotAnimateCamera").GetBoolean(),
        MoonPhaseMask = value.GetProperty("MoonPhaseMask").GetInt32(),
        StatusIds = value.GetProperty("StatusIds").Deserialize<string[]>()!,
        StatusStacks = value.GetProperty("StatusStacks").Deserialize<int[]>()!,
        TargetIds = value.GetProperty("TargetIds").Deserialize<int[]>()!,
        Before = value.GetProperty("Before").Deserialize<TrainCombatState>()!,
        After = value.GetProperty("After").ValueKind == FixtureKind.Null
            ? null : value.GetProperty("After").Deserialize<TrainCombatState>()
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
