using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusMagnitudeWitnessChecks
{
    private static readonly (string Key, string Id, string Status)[] Owners =
    [
        ("DoubleMagebladeDamage", "622860d3-b2ca-45b0-991a-b7745e1dcd5c", "mageblade"),
        ("StrongerSpikes", "aa94ad34-ee09-464e-89ff-2a2c2867c097", "spikes"),
        ("IncreaseRageDamage", "b9bddf11-acaa-40fb-bc40-eee83e478424", "buff"),
        ("AwokenRegen", "1fd244de-7d7b-4345-974b-4e5bf287a3df", "regen")
    ];

    internal static void Check(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalStatusMagnitudeWitness" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Original magnitude component source differs.");

        var rows = root.GetProperty("Plays").EnumerateArray().ToArray();
        Require(rows.Select(row => row.GetProperty("Card").GetString()).SequenceEqual(
            new[] { "TrainStewardShield", "LunaCovenChamp2", "RageSerum", "Sharpen", "StarterHeal" }),
            "Original magnitude carrier plays are missing or unordered.");
        int[] costs = [1, 0, 0, 2, 1];
        using var keys = new BattleStateKeys();
        for (int i = 0; i < rows.Length; i++)
        {
            var before = State(rows[i], "Before");
            var after = State(rows[i], "After");
            var action = rows[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var projected = BattleActionModel.PlayCard(before, action);
            Require(before.MoonPhaseTrackingActive == false && after.MoonPhaseTrackingActive == false &&
                action.RoomIndex == 0 && before.Energy - after.Energy == costs[i] && projected.Supported &&
                keys.Key(projected.State!) == keys.Key(after),
                "Original magnitude carrier successor differs for " + rows[i].GetProperty("Card").GetString() + ": " + projected.Reason);
        }

        FixtureValue exchange = root.GetProperty("Exchange");
        var start = State(exchange, "Before");
        var finish = State(exchange, "After");
        var combat = BattleTurnModel.EndTurn(start);
        Require(start.Spawn.Turn == 1 && finish.Spawn.Turn == 2 && combat.Supported && keys.Key(combat.State!) == keys.Key(finish),
            "Original magnitude exchange successor differs: " + combat.UnsupportedReason);
        var context = start.Spawn.Train.Context!;
        var allies = start.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        var mageblade = allies.Single(unit => unit.AssetKey == "MonsterChampionLunaCoven2");
        var steward = allies.Single(unit => unit.AssetKey == "TrainStewardBig");
        Require(Stacks(mageblade, "mageblade") == 2 && Stacks(mageblade, "buff") == 5 &&
            Stacks(steward, "spikes") == 6 && Stacks(steward, "regen") == 2,
            "Original live carriers lack their starting or paid status stacks.");

        var nativeQueries = root.GetProperty("Queries").EnumerateArray().ToArray();
        foreach (var owner in Owners)
        {
            var relic = context.Relics!.Single(item => item.DataId == owner.Id);
            Require(relic.AssetKey == owner.Key && relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
                relic.EffectTypes.SequenceEqual([RelicModel.ModifyStatusMagnitude]) &&
                relic.StatusMagnitudes is [{ EffectIndex: 0, SourceTeam: 2, Modifier: 1, Conditions.Count: 0 } rule] &&
                rule.StatusId == owner.Status, "Original magnitude relic definition differs: " + owner.Key);

            var hits = nativeQueries.Where(row => row.GetProperty("StatusId").GetString() == owner.Status &&
                row.GetProperty("NativeTeam").GetInt32() == 2 && !row.GetProperty("Preview").GetBoolean() &&
                !row.GetProperty("Matrix").GetBoolean() && row.GetProperty("Actual").GetInt32() == 1).ToArray();
            Require(hits.Length > 0 && (owner.Status is not ("spikes" or "regen") ||
                hits.Any(row => row.GetProperty("WitnessPhase").GetString() == "exchange")),
                "Missing positive native magnitude callback: " + owner.Key);

            foreach (var hit in hits)
            {
                var observed = hit.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
                var projected = RelicStatusMagnitudeModel.GetModifier(context.WithRelics(observed), owner.Status, 2);
                var ablated = RelicStatusMagnitudeModel.GetModifier(
                    context.WithRelics(observed.Where(item => item.DataId != owner.Id).ToArray()), owner.Status, 2);
                Require(projected.Supported && projected.Stacks == 1 && ablated.Supported && ablated.Stacks == 0,
                    "Original magnitude query and owner ablation differ: " + owner.Key);
            }
        }
        Console.WriteLine("STATUS-MAGNITUDE PASS: four original owners, positive native queries, five legal plays and one complete exchange.");
    }

    private static BattleTurnState State(FixtureValue row, string key) => row.GetProperty(key).Deserialize<BattleTurnState>()!;
    private static int Stacks(CombatUnit unit, string status) => unit.Statuses.Single(item => item.Id == status).Stacks;
    private static void Require(bool condition, string error)
    {
        if (!condition) throw new InvalidDataException(error);
    }
}
