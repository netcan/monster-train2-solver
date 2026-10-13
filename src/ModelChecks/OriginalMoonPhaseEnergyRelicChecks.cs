using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoonPhaseEnergyRelicChecks
{
    private const string DataId = "70096a91-220a-4ab3-b8d0-b8fe8f3a819c";
    private const string AssetKey = "MoonPhaseModifyEmber";

    private sealed class Snapshot
    {
        public BattleResourceState Resources { get; set; } = null!;
        public CombatRelicState[] Relics { get; set; } = [];
        public int Energy { get; set; }
        public string MoonPhase { get; set; } = "";
        public int Turn { get; set; }
        public bool Preview { get; set; }
    }

    private sealed class Operation
    {
        public string Kind { get; set; } = "";
        public string OwnerDataId { get; set; } = "";
        public int EffectIndex { get; set; }
        public bool Deployment { get; set; }
        public int Notifications { get; set; }
        public Snapshot Before { get; set; } = null!;
        public Snapshot? After { get; set; }
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 146 &&
            root.GetProperty("ModifierScenario").GetString() == "original-moon-phase-energy-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Actions").GetArrayLength() > 0 && root.GetProperty("Turns").GetArrayLength() >= 2 &&
            root.GetProperty("Stages").GetArrayLength() > 0,
            "Original moon-phase relic battle is incomplete or from an unexpected game build.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Moon-phase relic capture has nonzero " + field + ".");

        Operation[] operations = root.GetProperty("RelicResourceOperations").EnumerateArray()
            .Select(ReadOperation).ToArray();
        Operation acquisition = operations.Single(item => item.Kind == "Acquire");
        Require(operations.All(item => item.Kind is "Acquire" or "MoonPhaseEnergy") &&
            acquisition.After != null && !acquisition.Before.Relics.Any(item => item.DataId == DataId),
            "Moon-phase relic acquisition or callback sequence is unexpected.");
        Operation[] callbacks = operations.Where(item => item.Kind == "MoonPhaseEnergy").ToArray();
        Require(callbacks.Length == 6 && callbacks.All(item => item.OwnerDataId == DataId && item.EffectIndex == 0 &&
            !item.Deployment && item.After != null), "Original owner has incomplete native moon-phase callbacks.");
        CombatRelicState owner = acquisition.After!.Relics.Single(item => item.DataId == DataId);
        CheckOwner(owner);
        RelicConditionState acquiredCondition = Condition(owner);
        Require(!acquiredCondition.Triggered && acquiredCondition.ValueAtLastTrigger == 0 &&
            acquiredCondition.DurationTriggerCount == 0, "Original condition was already consumed at acquisition.");

        int full = 0;
        int newMoon = 0;
        foreach (Operation callback in callbacks)
        {
            Snapshot before = callback.Before;
            Snapshot after = callback.After!;
            Require(!before.Preview && !after.Preview && before.Turn == after.Turn,
                "Moon-phase callback crossed preview or turn boundaries.");
            CombatRelicState beforeOwner = before.Relics.Single(item => item.DataId == DataId);
            CombatRelicState afterOwner = after.Relics.Single(item => item.DataId == DataId);
            CheckOwner(beforeOwner);
            CheckOwner(afterOwner);
            CheckConditionTransition(Condition(beforeOwner), Condition(afterOwner), before.MoonPhase);
            if (before.MoonPhase == "Full")
            {
                full++;
                Require(after.Energy == Math.Min(before.Resources.MaxEnergy, before.Energy + 1) && callback.Notifications == 1,
                    "The original relic's Full-moon callback did not add one energy and notify once.");
            }
            else if (before.MoonPhase == "New")
            {
                newMoon++;
                Require(after.Energy == before.Energy && callback.Notifications == 0,
                    "The original Full-only relic changed energy or notified on New moon.");
            }
            else throw new InvalidDataException("Native moon-phase callback has an unmodeled phase: " + before.MoonPhase);
        }
        Require(full == 3 && newMoon == 3, "Capture lacks the three Full and three New moon callback witnesses.");

        foreach (FixtureValue action in root.GetProperty("Actions").EnumerateArray())
        {
            CheckTracking(action.GetProperty("Before").Deserialize<BattleTurnState>()!);
            CheckTracking(action.GetProperty("Actual").Deserialize<BattleTurnState>()!);
        }
        foreach (FixtureValue turn in root.GetProperty("Turns").EnumerateArray())
        {
            CheckTracking(turn.GetProperty("Before").Deserialize<BattleTurnState>()!);
            CheckTracking(turn.GetProperty("Actual").Deserialize<BattleTurnState>()!);
        }
        Console.WriteLine($"NATIVE-ORIGINAL-MOON-PHASE-ENERGY-RELIC PASS: unchanged owner payload; {full} Full and {newMoon} New callbacks; {root.GetProperty("Actions").GetArrayLength()} card plays and {root.GetProperty("Turns").GetArrayLength()} EndTurns retain exact phase markers.");
    }

    private static void CheckTracking(BattleTurnState state)
    {
        Require(state.MoonPhaseTrackingActive == true, "Battle decision state lost active native moon-phase tracking.");
        string expected = state.MoonPhase switch
        {
            "New" => "newmoon",
            "Full" => "fullmoon",
            _ => throw new InvalidDataException("Unmodeled tracked moon phase: " + state.MoonPhase)
        };
        string other = expected == "newmoon" ? "fullmoon" : "newmoon";
        foreach (CombatUnit unit in state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre))
            Require(unit.Status(expected)?.Stacks == 1 && unit.Status(other) == null,
                "Native decision state has missing or stale moon marker on unit " + unit.Id + ".");
    }

    private static void CheckOwner(CombatRelicState owner)
    {
        RelicResourceEffect effect = owner.ResourceEffects!.Single();
        RelicConditionState condition = effect.Conditions.Single();
        Require(owner.AssetKey == AssetKey && owner.EffectTypes.SequenceEqual([RelicResourceModel.MoonPhaseEnergy]) &&
            effect.EffectIndex == 0 && effect.ParamInt == 1 && effect.ParamInt2 == 2 && effect.Amount == 1 &&
            condition.Query.Type == "MoonPhase" && condition.Query.Duration == "ThisTurn" &&
            condition.TrackTriggerCount && condition.Comparator == 6 && condition.Value == 1 && !condition.AllowMultiple,
            "The captured original Full-moon +1 and once-per-turn payload changed.");
    }

    private static RelicConditionState Condition(CombatRelicState owner) =>
        owner.ResourceEffects!.Single().Conditions.Single();

    private static void CheckConditionTransition(RelicConditionState before, RelicConditionState after, string phase)
    {
        if (phase == "Full")
            Require(after.Triggered && after.DurationTriggerCount == before.DurationTriggerCount + 1 &&
                after.ValueAtLastTrigger == before.DurationTriggerCount,
                "Successful Full callback did not record its original once-per-turn condition.");
        else
            Require(after.Triggered == before.Triggered && after.DurationTriggerCount == before.DurationTriggerCount &&
                after.ValueAtLastTrigger == before.ValueAtLastTrigger,
                "No-op New callback changed the relic condition history.");
    }

    private static Operation ReadOperation(FixtureValue value) => new()
    {
        Kind = value.GetProperty("Kind").GetString()!,
        OwnerDataId = value.GetProperty("OwnerDataId").GetString()!,
        EffectIndex = value.GetProperty("EffectIndex").GetInt32(),
        Deployment = value.GetProperty("Deployment").GetBoolean(),
        Notifications = value.GetProperty("Notifications").GetInt32(),
        Before = ReadSnapshot(value.GetProperty("Before")),
        After = value.GetProperty("After").ValueKind == FixtureKind.Null ? null : ReadSnapshot(value.GetProperty("After"))
    };

    private static Snapshot ReadSnapshot(FixtureValue value) => new()
    {
        Resources = value.GetProperty("Resources").Deserialize<BattleResourceState>()!,
        Relics = value.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
        Energy = value.GetProperty("Energy").GetInt32(),
        MoonPhase = value.GetProperty("MoonPhase").GetString()!,
        Turn = value.GetProperty("Turn").GetInt32(),
        Preview = value.GetProperty("Preview").GetBoolean()
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
