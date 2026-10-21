using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossPyreChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Boundary").GetString() == "AfterDoBossTowerAttackOrTerminalStop" &&
            fixture.GetProperty("Errors").GetArrayLength() == 0 && fixture.GetProperty("Actions").GetArrayLength() > 0 &&
            fixture.GetProperty("QueueControls").GetArrayLength() == 1,
            "Missing original Pyre-entry capture and explicit native queue control.");
        int count = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            Require(row.GetProperty("NativeReturned").GetBoolean() || row.GetProperty("TerminalStopped").GetBoolean() &&
                (RoomOutcome)row.GetProperty("Outcome").GetInt32() is RoomOutcome.PlayerDefeated or RoomOutcome.BattleWon,
                "Native Pyre attack neither returned nor reached the terminal stop boundary.");
            OuterBossPhaseState before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            OuterBossPyreResult result = OuterBossPyreModel.Apply(before);
            Require(result.Supported, "Outer-boss Pyre entry unsupported: " + result.UnsupportedReason);
            string Compare(BattleTurnState state)
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(state, ModelJson.Options))!;
                // Native deletes its card manager at terminal stop. Cached card
                // definitions are no longer gameplay state; retain all room rules.
                if (result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
                    node["PlayRules"]?.AsObject().Remove("Cards");
                return node.ToJsonString();
            }
            string? diff = ModelJson.Difference(Compare(result.State!), Compare(actual));
            Require(diff == null, "Outer-boss Pyre entry differs: " + diff);
            Require(result.Outcome == (RoomOutcome)row.GetProperty("Outcome").GetInt32() && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Pyre-entry outcome differs or mutated its source.");
            count++;
        }
        Console.WriteLine($"OUTER-BOSS-PYRE PASS: {count} native moves/callbacks/immediate Pyre combats; full battle/physical/controller/aura/RNG equality, explicit isolated queue control and immutable inputs.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
