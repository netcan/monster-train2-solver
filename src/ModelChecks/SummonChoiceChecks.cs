using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SummonChoiceChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static FixtureValue[] Items(FixtureValue value, string key) => value.GetProperty(key).EnumerateArray().ToArray();
    private static UnityRng Rng(FixtureValue value)
    { uint[] words = value.Deserialize<uint[]>()!; Require(words.Length == 4, "Invalid native RNG words."); return new(words[0], words[1], words[2], words[3]); }

    internal static void Native(string path, string battlePath)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Scope").GetString() == "SummonChoiceOffersOnly" && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Unexpected native summon-choice scope or game.");
        var definitions = Items(root, "Definitions");
        var rules = definitions.Select(row => row.GetProperty("Rule").Deserialize<SummonChoiceRule>()!).ToArray();
        string[] abilities = { "2160ff0b-3d1f-4d77-97e0-26b5df37590e", "c0d006a1-ed15-45ff-bb51-7664a8c4f11c", "24daf0a7-40fd-4f15-b6d1-60522568cad4" };
        string[][] tokens = {
            new[] { "6c3eb0bb-4871-438d-9b5c-fcef1f94bf70", "c847d9b0-5af4-4076-9d99-111b81224ef5", "0127e3c2-b60d-4ac3-acf7-6395d44eb5ef" },
            new[] { "983495d0-11cb-49d7-9f3e-8531c6f18676", "841340c7-76e3-4336-b8a2-41f308a787fb", "491acbc6-c766-4ecf-8b10-6c51b5ae94e5" },
            new[] { "462f5b87-cace-447d-8818-3d4b7bc8eb7f", "c62f38bc-3b99-4f90-8d32-59a5588466e8", "4535073a-f484-4ecd-abb9-b83d6ddfcee1" }
        };
        Require(rules.Length == 12, "Missing three original pools or mixed/empty controls.");
        for (int index = 0; index < 3; index++)
            Require(definitions[index * 2 + 1].GetProperty("AbilityDataId").GetString() == abilities[index] &&
                rules[index * 2 + 1].Count == 3 && rules[index * 2 + 1].OnePerFloorSubtype == "SubtypesData_Darkshard" &&
                rules[index * 2 + 1].Pool.Select(card => card.CardDataId).SequenceEqual(tokens[index]) &&
                rules[index * 2 + 1].Pool.All(card => card.HasSpawnCharacter && card.Size == 1), "Original rank pool or subtype limit changed.");
        Require(rules[7].Pool.Count == 2 && rules[7].Pool.Count(card => card.HasSpawnCharacter) == 1 && rules[11].Pool.Count == 0 &&
            rules[7].OnePerFloorSubtype.Length == 0 && rules[11].OnePerFloorSubtype.Length == 0 &&
            rules.Where((_, index) => index < 6 && index % 2 == 0).All(rule => rule.Pool.Count == 0), "Missing spell-filter/empty-pool controls.");
        using var battle = ModelJson.ReadFixture(battlePath);
        var actions = Items(battle.RootElement, "Actions");
        Require(actions.Length == 12, "Calibration baseline lost original control actions.");
        Require(rules[9].Pool.Select(card => card.CardDataId).SequenceEqual(new[] { tokens[0][0], tokens[0][1], tokens[0][0] }),
            "Missing original duplicate-reference pool order.");
        var frames = Items(root, "Frames");
        Require(frames.Length == 13, "Missing settled frames.");
        for (int index = 0; index < frames.Length; index++)
            Require(frames[index].GetProperty("Label").GetString() == (index == 0 ? "before-first-action" : "after-action-" + (index - 1)) &&
                frames[index].GetProperty("Before").ContentEquals(index == 0 ? actions[0].GetProperty("Before") : actions[index - 1].GetProperty("Actual")),
                "Component evidence does not match the source battle decision.");
        for (int index = 0; index < definitions.Length; index++)
            Require(definitions[index].GetProperty("Profile").GetString() == (index % 2 == 0 ? "native-platform" : "installed-control"),
                "DLC eligibility input is not explicitly labeled.");
        int samples = 0, blocked = 0, random = 0, ordered = 0, empty = 0, nonunit = 0, largeSize = 0, duplicate = 0;
        bool moved = false, unblockedAfterDeaths = false;
        var blockHistory = new List<(string Label, bool Floor0, bool Floor1)>();
        foreach (var frame in frames)
        {
            Require(frame.GetProperty("DecisionUnchanged").GetBoolean() &&
                frame.GetProperty("FrameBefore").GetInt32() == frame.GetProperty("FrameAfter").GetInt32() &&
                frame.GetProperty("RngBefore").ContentEquals(frame.GetProperty("RngAfter")), "Native calibration leaked state/RNG/frame.");
            var state = frame.GetProperty("Before").Deserialize<BattleTurnState>()!;
            string parent = JsonSerializer.Serialize(state, ModelJson.Options);
            var rows = Items(frame, "Samples");
            var slots = Items(frame, "Slots");
            Require(rows.Length == 864 && slots.Length == 12, "Missing count/seed/room/definition controls.");
            foreach (var slot in slots)
            {
                int room = slot.GetProperty("RoomIndex").GetInt32();
                bool allowed = slot.GetProperty("Allowed").GetBoolean();
                Require(allowed == (state.Spawn.Train.Context!.SpawnPoints!.FirstEmpty(room, CombatTeam.Player) >= 0), "Native physical-slot test differs.");
                if (slot.GetProperty("Size").GetInt32() == 1000 && allowed) largeSize++;
            }
            bool NativeBlock(int room) => rows.First(row => row.GetProperty("RoomIndex").GetInt32() == room &&
                row.GetProperty("RuleIndex").GetInt32() == 0).GetProperty("Blocked").GetBoolean();
            blockHistory.Add((frame.GetProperty("Label").GetString()!, NativeBlock(0), NativeBlock(1)));
            var inputs = rows.Select(row => new {
                Room = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == row.GetProperty("RoomIndex").GetInt32()),
                RuleIndex = row.GetProperty("RuleIndex").GetInt32(), Count = row.GetProperty("Count").GetInt32(),
                Seed = row.GetProperty("Seed").GetInt32(), Before = Rng(row.GetProperty("BeforeRng")), After = Rng(row.GetProperty("AfterRng")),
                Blocked = row.GetProperty("Blocked").GetBoolean(), Offers = row.GetProperty("Offers").Deserialize<string[]>()!
            }).ToArray();
            Require(inputs.Select(row => (row.Room.RoomIndex, row.RuleIndex, row.Count, row.Seed)).Distinct().Count() == 864 &&
                inputs.Select(row => row.Count).Distinct().Order().SequenceEqual(new[] { -1, 0, 1, 2, 3, 10 }) &&
                inputs.Select(row => row.Seed).Distinct().Order().SequenceEqual(new[] { 0, 41, 20261115 }), "Native sample matrix has duplicate or missing inputs.");
            void Verify()
            {
                foreach (var row in inputs)
                {
                    var plan = SummonChoiceModel.Plan(row.Room, rules[row.RuleIndex].WithCount(row.Count), row.Before);
                    Require(plan.Supported && plan.BlockedBySubtype == row.Blocked && plan.Offers.SequenceEqual(row.Offers) &&
                        plan.Rng.Equals(row.After), "Independent native summon choices, order, blocker or full RNG differ.");
                }
            }
            Verify(); Parallel.For(0, 16, _ => Verify());
            Require(parent == JsonSerializer.Serialize(state, ModelJson.Options), "Parallel offer generation mutated its captured parent.");
            foreach (var row in inputs)
            {
                samples++; if (row.Blocked) blocked++;
                if (!row.Before.Equals(row.After)) random++;
                if (!row.Blocked && row.RuleIndex < 6 && row.RuleIndex % 2 == 1 && row.Count >= 3 && row.Offers.Length == 3) ordered++;
                if (row.RuleIndex >= 10 && row.Offers.Length == 0 && row.Before.Equals(row.After)) empty++;
                if (row.RuleIndex == 7 && row.Offers.SequenceEqual(new[] { tokens[0][0] })) nonunit++;
                if (row.RuleIndex == 9 && row.Offers.Length > 0) duplicate++;
            }
        }
        Require(frames.Length == 13 && samples == 11232 && blocked > 0 && random > 0 && ordered > 0 && empty > 0 && nonunit > 0 && largeSize > 0 && duplicate > 0,
            "Missing positive blocker, random/ordered, empty/spell or size-over-capacity native coverage.");
        moved = blockHistory.Any(row => row.Floor1);
        unblockedAfterDeaths = blockHistory.Any(row => row.Label == "after-action-11" && !row.Floor0 && !row.Floor1);
        Require(!blockHistory[0].Floor0 && !blockHistory[0].Floor1 && blockHistory.Any(row => row.Floor0) && moved && unblockedAfterDeaths,
            "Missing native initial-empty, same-floor, move or final-death blocker controls.");
        OriginalUpgradeEnchantmentChecks.Native(battlePath);
        Console.WriteLine($"SUMMON-CHOICE-OFFERS PASS: {samples} native samples across {frames.Length} settled frames; {blocked} subtype skips, {random} RNG selections, {ordered} ordered triples, {nonunit} spell filters, {empty} empty pools, {duplicate} duplicate-pool selections, {largeSize} size-1000 slot approvals; 16 immutable parallel branches per frame. Offer stage only; original skill/action/UI/birth integration remains pending.");
    }
}
