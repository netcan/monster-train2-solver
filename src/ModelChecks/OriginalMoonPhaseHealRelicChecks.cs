using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoonPhaseHealRelicChecks
{
    private const string DataId = "6d977136-5aac-44fd-9843-54cf2a0bd01d";
    private const string AssetKey = "MooncycleRestoreFriendlyHealth";

    private sealed class Operation
    {
        public string OwnerDataId { get; set; } = "";
        public int EffectIndex { get; set; }
        public string Phase { get; set; } = "";
        public string SourceTeam { get; set; } = "";
        public string TargetMode { get; set; } = "";
        public int HealthAmount { get; set; }
        public int MoonPhaseMask { get; set; }
        public UnitHealth[] Before { get; set; } = [];
        public UnitHealth[]? After { get; set; }
    }

    private sealed class UnitHealth
    {
        public int UnitId { get; set; }
        public int RoomIndex { get; set; }
        public int TargetOrder { get; set; }
        public string Team { get; set; } = "";
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool IsPyre { get; set; }
        public bool IsDead { get; set; }
        public bool IsDestroyed { get; set; }
        public bool IsTargetable { get; set; }
        public bool CanBeHealed { get; set; }
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 166 &&
            root.GetProperty("ModifierScenario").GetString() == "original-moon-phase-heal-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Actions").GetArrayLength() >= 5 && root.GetProperty("Turns").GetArrayLength() >= 2,
            "Original moon-phase healing battle is incomplete or from an unexpected game build.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Moon-phase healing capture has nonzero " + field + ".");

        var controls = root.GetProperty("OriginalMoonPhaseHealScenario");
        int acolyte = controls.GetProperty("AcolyteCardId").GetInt32();
        int[] wounds = controls.GetProperty("WoundCardIds").Deserialize<int[]>()!;
        int[] faes = controls.GetProperty("FaeCardIds").Deserialize<int[]>()!;
        Require(wounds.Length == 2 && faes.Length == 2, "Moon-phase healing fixture lost one or more control cards.");
        PlayCardAction[] actions = root.GetProperty("Actions").EnumerateArray()
            .Select(action => action.GetProperty("Action").Deserialize<PlayCardAction>()!).ToArray();
        int[] expectedActions = [acolyte, wounds[0], faes[0], wounds[1], faes[1]];
        Require(actions.Take(expectedActions.Length).Select(action => action.CardInstanceId).SequenceEqual(expectedActions),
            "Native battle did not execute the Acolyte, controlled wound and original Fae phase-advance sequence.");

        Operation[] operations = root.GetProperty("RelicMoonPhaseHealOperations").EnumerateArray()
            .Select(ReadOperation).ToArray();
        Require(operations.Length >= 2 && operations.All(operation => operation.After != null &&
            operation.OwnerDataId == DataId && operation.EffectIndex == 0 && operation.SourceTeam == "Monsters" &&
            operation.TargetMode == "WeakestAllRooms" && operation.HealthAmount == 10 && operation.MoonPhaseMask == -1 &&
            operation.Phase is "Full" or "New"), "Original owner has missing callbacks or changed native effect data.");
        Require(operations.Any(operation => operation.Phase == "Full") && operations.Any(operation => operation.Phase == "New"),
            "Native callbacks do not cover both moon phases.");

        int positive = 0;
        int noTarget = 0;
        int crossRoomCandidateSets = 0;
        foreach (Operation operation in operations)
        {
            UnitHealth[] before = operation.Before;
            UnitHealth[] after = operation.After!;
            var afterById = after.ToDictionary(unit => unit.UnitId);
            Require(before.Length == after.Length && before.Select(unit => unit.UnitId).ToHashSet().SetEquals(afterById.Keys),
                "Moon-phase healing callback changed the unit set while applying the relic effect.");
            UnitHealth[] targets = before.Where(unit => unit.Team == "Monsters" && !unit.IsPyre && !unit.IsDead &&
                !unit.IsDestroyed && unit.IsTargetable && unit.Health < unit.MaxHealth)
                .OrderBy(unit => unit.Health).ThenBy(unit => unit.TargetOrder).ToArray();
            if (targets.Select(unit => unit.RoomIndex).Distinct().Count() > 1) crossRoomCandidateSets++;
            UnitHealth? target = targets.FirstOrDefault();
            if (target == null) noTarget++;
            foreach (UnitHealth unit in before)
            {
                UnitHealth result = afterById[unit.UnitId];
                Require(result.RoomIndex == unit.RoomIndex && result.Team == unit.Team &&
                    result.MaxHealth == unit.MaxHealth && result.IsPyre == unit.IsPyre &&
                    result.IsDead == unit.IsDead && result.IsDestroyed == unit.IsDestroyed &&
                    result.IsTargetable == unit.IsTargetable && result.CanBeHealed == unit.CanBeHealed,
                    "Moon-phase healing changed a non-HP unit field.");
                int heal = target?.UnitId == unit.UnitId && unit.CanBeHealed
                    ? Math.Min(operation.HealthAmount, unit.MaxHealth - unit.Health)
                    : 0;
                Require(result.Health == unit.Health + heal,
                    "Native moon-phase callback healed the wrong unit or amount; unit=" + unit.UnitId + ".");
                if (heal > 0) positive++;
            }
        }
        Require(positive > 0 && crossRoomCandidateSets > 0,
            "Native callbacks lack a positive heal or a weakest-target comparison across multiple rooms.");

        foreach (FixtureValue item in root.GetProperty("Actions").EnumerateArray().Take(expectedActions.Length))
        {
            BattleTurnState before = item.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = item.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = item.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleActionResult replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "The five native moon-phase control plays differ from independent model replay: " + replay.Reason);
        }
        Console.WriteLine($"NATIVE-ORIGINAL-MOON-PHASE-HEAL-RELIC PASS: {operations.Length} exact Full/New callbacks, {positive} selected heals, {crossRoomCandidateSets} cross-room target comparisons, {noTarget} no-target controls and five independent card-play replays.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static Operation ReadOperation(FixtureValue value) => new()
    {
        OwnerDataId = value.GetProperty("OwnerDataId").GetString()!,
        EffectIndex = value.GetProperty("EffectIndex").GetInt32(),
        Phase = value.GetProperty("Phase").GetString()!,
        SourceTeam = value.GetProperty("SourceTeam").GetString()!,
        TargetMode = value.GetProperty("TargetMode").GetString()!,
        HealthAmount = value.GetProperty("HealthAmount").GetInt32(),
        MoonPhaseMask = value.GetProperty("MoonPhaseMask").GetInt32(),
        Before = value.GetProperty("Before").EnumerateArray().Select(ReadUnit).ToArray(),
        After = value.GetProperty("After").ValueKind == FixtureKind.Null
            ? null
            : value.GetProperty("After").EnumerateArray().Select(ReadUnit).ToArray()
    };

    private static UnitHealth ReadUnit(FixtureValue value) => new()
    {
        UnitId = value.GetProperty("UnitId").GetInt32(),
        RoomIndex = value.GetProperty("RoomIndex").GetInt32(),
        TargetOrder = value.GetProperty("TargetOrder").GetInt32(),
        Team = value.GetProperty("Team").GetString()!,
        Health = value.GetProperty("Health").GetInt32(),
        MaxHealth = value.GetProperty("MaxHealth").GetInt32(),
        IsPyre = value.GetProperty("IsPyre").GetBoolean(),
        IsDead = value.GetProperty("IsDead").GetBoolean(),
        IsDestroyed = value.GetProperty("IsDestroyed").GetBoolean(),
        IsTargetable = value.GetProperty("IsTargetable").GetBoolean(),
        CanBeHealed = value.GetProperty("CanBeHealed").GetBoolean()
    };
}
