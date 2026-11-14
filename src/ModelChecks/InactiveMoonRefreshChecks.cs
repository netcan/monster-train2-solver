using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveMoonRefreshChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("InactiveRefresh").GetBoolean() &&
            root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("PreviewUpdatesSuppressed").GetBoolean(),
            "Inactive moon refresh is missing an accepted native component capture.");
        int[] ids = root.GetProperty("ActorIds").Deserialize<int[]>()!;
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(ids.Length == 2 && rows.Length == 4 && rows.Select(row => row.GetProperty("Label").GetString())
            .SequenceEqual(["inactive-new-character-refresh", "inactive-full-resource-only",
                "inactive-full-character-refresh", "inactive-full-noop"]),
            "Inactive moon refresh rows or original actors are missing.");
        int[] phases = [1, 2, 2, 2];
        bool[] characters = [true, false, true, true];
        int[][] masks = [[6, 6], [5, 5], [5, 5], [6, 6], [6, 6]];
        int[][] upgrades = [[1, 0], [0, 1], [0, 1], [1, 0], [1, 0]];
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            Require(row.GetProperty("Operation").GetString() == "Refresh" &&
                row.GetProperty("Characters").GetBoolean() == characters[index] &&
                before.Context?.QueryFrame is { MoonPhaseTrackingActive: false } &&
                before.Context.QueryFrame.MoonPhase == phases[index] &&
                row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                "Native refresh changed tracking, phase or gameplay RNG.");
            CheckActors(before, ids, masks[index], upgrades[index]);
            CheckActors(actual, ids, masks[index + 1], upgrades[index + 1]);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                MoonPhaseTransitionResult result = RoomMoonPhaseModel.Refresh(before, characters[index]);
                Require(result.Supported, "Inactive moon refresh was rejected: " + result.UnsupportedReason);
                string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options));
                Require(difference == null, "Inactive native moon refresh differs: " + difference);
                TrainSpellResult spell = CardSpellModel.Apply(before, 0,
                    [new CardActionEffect("RefreshMoonPhase", "Room", characters[index] ? 1 : 0, false, false, [])], 0);
                Require(spell.Supported && ModelJson.Difference(JsonSerializer.Serialize(spell.State, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options)) == null,
                    "Inactive moon refresh differs through the card-effect pipeline: " + spell.UnsupportedReason);
            }
            Replay(); Parallel.For(0, 8, _ => Replay());
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options),
                "Inactive refresh mutated its parent.");
            if (index == 0)
            {
                JsonNode missing = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                missing["Context"]!["QueryFrame"]!["MoonPhaseTrackingActive"] = null;
                MoonPhaseTransitionResult guarded = RoomMoonPhaseModel.Refresh(
                    missing.Deserialize<TrainCombatState>(ModelJson.Options)!, characters: true);
                Require(!guarded.Supported && guarded.UnsupportedReason == "Moon refresh requires captured tracking state.",
                    "Missing native tracking state was treated as inactive.");
            }
        }
        Console.WriteLine("INACTIVE-MOON-REFRESH PASS: four original callbacks, both teams, Full/New and resource-only controls, exact context/RNG and eight immutable branches.");
    }

    private static void CheckActors(TrainCombatState state, int[] ids, int[] masks, int[] upgrades)
    {
        CombatUnit[] actors = ids.Select(id => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id)).ToArray();
        Require(actors[0].Team == CombatTeam.Player && actors[1].Team == CombatTeam.Enemy &&
            actors.Select(unit => unit.MoonPhaseMask).SequenceEqual(masks.Select(mask => (int?)mask)) &&
            actors.Select(unit => unit.Modifiers?.Upgrades.Count ?? -1).SequenceEqual(upgrades),
            "Native inactive refresh lost its paired player/enemy callback or understanding witness.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
