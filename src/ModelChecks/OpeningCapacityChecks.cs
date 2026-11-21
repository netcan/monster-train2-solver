using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OpeningCapacityChecks
{
    internal static void Carryover(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalPyreCapacityCarryoverDefinition");
        string? owner = definition.GetProperty("Id").GetString();
        Require(root.GetProperty("OriginalPyreCapacityCarryoverPrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalPyreCapacityCarryoverDefinitionAfter")) &&
            owner != null && RelicOpeningCapacityModel.IsPyreOwner(owner), "Missing unchanged original Pyre capacity artifact.");

        var callbacks = root.GetProperty("OriginalPyreCapacityCarryoverCallbacks").EnumerateArray().ToArray();
        var tests = callbacks.Where(row => row.GetProperty("Kind").GetString() == "Test").ToArray();
        Require(callbacks.All(row => row.GetProperty("Completed").GetBoolean()) && tests.Length > 1 &&
            tests.All(row => row.GetProperty("Before").ContentEquals(row.GetProperty("After")) &&
                row.GetProperty("Passed").GetBoolean() == (Int(row.GetProperty("Before"), "Turn") == 0)),
            "Pyre opening Test must be pure and pass at deployment only.");
        var application = callbacks.Single(row => row.GetProperty("Kind").GetString() == "Apply");
        var before = application.GetProperty("Before");
        var after = application.GetProperty("After");
        var previousRooms = before.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!;
        var nextRooms = after.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!;
        int[] bonuses = { 2, 0, 1 };
        Require(Int(before, "Turn") == 0 && Int(after, "SelectedRoom") == 0 &&
            !before.GetProperty("Applied").GetBoolean() && after.GetProperty("Applied").GetBoolean() &&
            before.GetProperty("BattleRng").ContentEquals(after.GetProperty("BattleRng")) &&
            before.GetProperty("Bonuses").Deserialize<int[]>()!.SequenceEqual(bonuses) &&
            before.GetProperty("Bonuses").ContentEquals(after.GetProperty("Bonuses")) &&
            nextRooms.All(room => room.PlayerMaximum == previousRooms.Single(old => old.RoomIndex == room.RoomIndex).PlayerMaximum +
                (room.IsPyre ? 0 : bonuses[room.RoomIndex])), "Native carryover did not apply the saved capacity once before deployment.");

        var witness = root.GetProperty("OriginalPyreCapacityCarryoverWitness");
        bool controlled = owner == RelicOpeningCapacityModel.LifeId;
        Require(controlled == (definition.TryGetProperty("ControlledAttachment", out var definitionControl) && definitionControl.GetBoolean()) &&
            controlled == (witness.TryGetProperty("ControlledAttachment", out var witnessControl) && witnessControl.GetBoolean()),
            "Pyre carryover evidence must identify the controlled Life attachment.");
        int expanded = Int(witness, "ExpandedFloor"), control = Int(witness, "ControlFloor"), id = Int(witness, "CardId");
        var first = root.GetProperty("Actions").EnumerateArray().First();
        var state = first.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var context = state.Spawn.Train.Context!;
        var choices = BattleActionModel.EnumerateSupportedPlays(state, id);
        Require(state.Spawn.Turn == 0 && Int(first.GetProperty("Action"), "CardInstanceId") == id &&
            Int(first.GetProperty("Action"), "RoomIndex") == expanded &&
            witness.GetProperty("NativeFits").GetBoolean() && witness.GetProperty("NativeControlBlocked").GetBoolean() &&
            Int(witness, "CardSize") > Int(witness, "OriginalMaximum") &&
            RelicModel.Validate(context) == null && context.Resources?.OpeningCapacitySettled == true &&
            context.RoomCapacities!.Single(room => room.RoomIndex == expanded).PlayerMaximum == nextRooms.Single(room => room.RoomIndex == expanded).PlayerMaximum &&
            choices.Any(action => action.RoomIndex == expanded) && choices.All(action => action.RoomIndex != control),
            "Settled Pyre capacity did not preserve the additional legal deployment in the model.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"PYRE-CAPACITY-CARRYOVER PASS owner={definition.GetProperty("Mode").GetString()}, floors +2/0/+1, native deployment and complete search successors agree.");
    }

    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalRandomRoomCapacityDefinition");
        Require(root.GetProperty("OriginalRandomRoomCapacityPrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalRandomRoomCapacityDefinitionAfter")) &&
            definition.GetProperty("Id").GetString() == RelicOpeningCapacityModel.MutatorId &&
            Int(definition, "Amount") == -3, "Missing unchanged original capacity mutator.");

        var callbacks = root.GetProperty("OriginalRandomRoomCapacityCallbacks").EnumerateArray().ToArray();
        var tests = callbacks.Where(row => row.GetProperty("Kind").GetString() == "Test").ToArray();
        Require(callbacks.All(row => row.GetProperty("Completed").GetBoolean()) && tests.Length > 1 &&
            tests.All(row => row.GetProperty("Before").ContentEquals(row.GetProperty("After")) &&
                row.GetProperty("Passed").GetBoolean() == (Int(row.GetProperty("Before"), "Turn") == 0)),
            "Native capacity Test must be pure and pass at deployment only.");
        var application = callbacks.Single(row => row.GetProperty("Kind").GetString() == "Apply");
        var before = application.GetProperty("Before");
        var after = application.GetProperty("After");
        var witness = root.GetProperty("OriginalRandomRoomCapacityWitness");
        int reduced = Int(witness, "ReducedFloor"), alternate = Int(witness, "AlternateFloor");
        int previousMaximum = Int(witness, "OriginalMaximum"), maximum = Int(witness, "Maximum");
        var previousRooms = before.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!;
        var nextRooms = after.GetProperty("Rooms").Deserialize<RoomCapacityState[]>()!;
        RngDraw draw = before.GetProperty("BattleRng").Deserialize<UnityRng>().Range(0, previousRooms.Length - 1);
        Require(Int(before, "Turn") == 0 && Int(after, "SelectedRoom") == reduced &&
            draw.Value == reduced && draw.State.Equals(after.GetProperty("BattleRng").Deserialize<UnityRng>()) &&
            previousRooms.Single(room => room.RoomIndex == reduced).PlayerMaximum == previousMaximum &&
            nextRooms.Single(room => room.RoomIndex == reduced).PlayerMaximum == maximum && previousMaximum - maximum == 3 &&
            nextRooms.All(room => room.RoomIndex == reduced || room.PlayerMaximum == previousRooms.Single(old => old.RoomIndex == room.RoomIndex).PlayerMaximum),
            "Native opening capacity did not adjust exactly one selected floor and advance Battle RNG.");

        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var first = actions[0]; var second = actions[1];
        int firstId = Int(witness, "FirstCardId"), secondId = Int(witness, "BlockedCardId");
        Require(Int(first.GetProperty("Action"), "CardInstanceId") == firstId && Int(first.GetProperty("Action"), "RoomIndex") == reduced &&
            Int(second.GetProperty("Action"), "CardInstanceId") == secondId && Int(second.GetProperty("Action"), "RoomIndex") == alternate &&
            witness.GetProperty("NativeBlocked").GetBoolean() && witness.GetProperty("NativeOtherFits").GetBoolean() &&
            Int(witness, "Occupied") == maximum && maximum + Int(witness, "CardSize") <= previousMaximum,
            "Native deployment did not isolate rejection caused by the reduced floor capacity.");

        var settled = second.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var context = settled.Spawn.Train.Context!;
        Require(settled.Spawn.Turn == 0 && RelicModel.Validate(context) == null &&
            context.Resources?.OpeningCapacitySettled == true &&
            context.RoomCapacities?.Single(room => room.RoomIndex == reduced).PlayerMaximum == maximum &&
            RelicModel.Validate(context.WithRoomCapacities(Array.Empty<RoomCapacityState>())) != null,
            "Model must require settled native capacity state and preserve it at deployment.");
        var choices = BattleActionModel.EnumerateSupportedPlays(settled, secondId);
        Require(choices.Any(action => action.RoomIndex == alternate) && choices.All(action => action.RoomIndex != reduced),
            "Model admitted a deployment that exceeded the native capacity.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"OPENING-CAPACITY PASS floor={reduced} capacity={previousMaximum}->{maximum}; native placement, complete successors and serial/parallel plans agree.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
