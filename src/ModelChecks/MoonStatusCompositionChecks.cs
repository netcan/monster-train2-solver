using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonStatusCompositionChecks
{
    internal static void Owner(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == RelicMoonPhaseStatusModel.DataId);
        Require(owner.EffectTypes.SequenceEqual([RelicMoonPhaseStatusModel.Effect]) &&
            RelicMoonPhaseStatusModel.Validate(owner) == null && RelicModel.Validate(context) == null,
            "Original Silent armor relic signature changed.");
    }

    internal static void Callbacks(FixtureValue fixture)
    {
        int full = 0, newMoon = 0, positive = 0, excluded = 0;
        foreach (FixtureValue row in fixture.GetProperty("RelicMoonPhaseStatusOperations").EnumerateArray())
        {
            Require(row.GetProperty("OwnerDataId").GetString() == RelicMoonPhaseStatusModel.DataId &&
                row.GetProperty("EffectIndex").GetInt32() == 0 && row.GetProperty("SourceTeam").GetString() == "Monsters" &&
                row.GetProperty("TargetMode").GetString() == "Tower" && row.GetProperty("RequiredSubtype").GetString() == "SubtypesData_Silent" &&
                row.GetProperty("DoNotAnimateCamera").GetBoolean() && row.GetProperty("MoonPhaseMask").GetInt32() == 1 &&
                row.GetProperty("StatusIds").Deserialize<string[]>()!.SequenceEqual(["armor"]) &&
                row.GetProperty("StatusStacks").Deserialize<int[]>()!.SequenceEqual([10]) &&
                row.GetProperty("After").ValueKind == FixtureKind.Object, "Missing original settled moon armor callback.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            Owner(before.Context!); Owner(after.Context!);
            string phase = row.GetProperty("Phase").GetString()!;
            Require(phase is "New" or "Full" && !before.Rooms.Any(room => room.Preview), "Unexpected moon armor phase/preview.");
            string parent = Json(before);
            void Check()
            {
                RelicMoonPhaseStatusResult result = RelicMoonPhaseStatusModel.Apply(before, phase == "New" ? 1 : 2,
                    placement: before.Rooms.Any(room => room.Deployment));
                Require(result.Supported, "Moon armor callback rejected: " + result.UnsupportedReason);
                string? difference = ModelJson.Difference(Json(result.State!), Json(after));
                Require(difference == null, "Complete moon armor callback differs: " + difference);
            }
            Check(); Parallel.For(0, 8, _ => Check());
            Require(parent == Json(before), "Moon armor callback mutated its parent.");
            int[] targets = phase == "New" ? before.Rooms.OrderBy(room => room.RoomIndex).SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 &&
                    unit.DeathState?.IsDestroyed != true && unit.Status("untouchable") == null && unit.Subtypes.Contains("SubtypesData_Silent"))
                .Select(unit => unit.Id).ToArray() : [];
            if (phase == "New")
                Require(targets.Order().SequenceEqual(row.GetProperty("TargetIds").Deserialize<int[]>()!.Order()),
                    "Original New armor target membership differs.");
            foreach (CombatUnit prior in before.Rooms.SelectMany(room => room.Units))
            {
                CombatUnit next = after.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == prior.Id);
                if ((next.Status("armor")?.Stacks ?? 0) > (prior.Status("armor")?.Stacks ?? 0)) positive++;
                if (prior.Team == CombatTeam.Player && !prior.IsPyre && !prior.Subtypes.Contains("SubtypesData_Silent")) excluded++;
            }
            if (phase == "New") newMoon++; else full++;
        }
        Require(full > 0 && newMoon > 0 && positive > 0 && excluded > 0,
            "Missing Full/New callbacks, positive Silent armor or non-Silent exclusions.");
        Console.WriteLine($"MOON-STATUS-CALLBACKS PASS: {newMoon} New/{full} Full callbacks, {positive} positive armor applications, {excluded} non-Silent exclusions; full immutable states match.");
    }
    private static string Json(TrainCombatState state) => JsonSerializer.Serialize(state, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
