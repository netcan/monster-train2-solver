using System.Text.Json;
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

    internal static void Check(FixtureValue root, bool moon = false)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == (moon ? "OriginalMoonStatusMagnitudeWitness" : "OriginalStatusMagnitudeWitness") &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Original magnitude component source differs.");

        var rows = root.GetProperty("Plays").EnumerateArray().ToArray();
        Require(rows.Select(row => row.GetProperty("Card").GetString()).SequenceEqual(
            new[] { "TrainStewardShield", moon ? "SeleneAcolyte" : "LunaCovenChamp2", "RageSerum", "Sharpen", "StarterHeal" }),
            "Original magnitude carrier plays are missing or unordered.");
        int[] costs = [1, moon ? 2 : 0, 0, 2, 1];
        using var keys = new BattleStateKeys();
        for (int i = 0; i < rows.Length; i++)
        {
            var before = State(rows[i], "Before");
            var after = State(rows[i], "After");
            var action = rows[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var projected = BattleActionModel.PlayCard(before, action);
            Require(before.MoonPhaseTrackingActive == moon && after.MoonPhaseTrackingActive == moon &&
                action.RoomIndex == 0 && before.Energy - after.Energy == costs[i] && projected.Supported &&
                keys.Key(projected.State!) == keys.Key(after),
                "Original magnitude carrier successor differs for " + rows[i].GetProperty("Card").GetString() + ": " +
                (projected.Reason ?? Difference(projected.State, after)));
        }

        FixtureValue exchange = root.GetProperty("Exchange");
        var start = State(exchange, "Before");
        var finish = State(exchange, "After");
        var combat = BattleTurnModel.EndTurn(start);
        Require(start.Spawn.Turn == 1 && finish.Spawn.Turn == 2 && combat.Supported && keys.Key(combat.State!) == keys.Key(finish),
            "Original magnitude exchange successor differs: " + (combat.UnsupportedReason ?? Difference(combat.State, finish)));
        var context = start.Spawn.Train.Context!;
        var allies = start.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        var mageblade = allies.Single(unit => unit.AssetKey == (moon ? "MonsterSeleneAcolyte" : "MonsterChampionLunaCoven2"));
        var steward = allies.Single(unit => unit.AssetKey == "TrainStewardBig");
        Require(Stacks(mageblade, "mageblade") == 2 && Stacks(mageblade, "buff") == 5 &&
            Stacks(steward, "spikes") == 6 && Stacks(steward, "regen") == 2,
            "Original live carriers lack their starting or paid status stacks.");
        if (moon) CheckMoon(rows, start, finish, mageblade);

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
        Console.WriteLine("STATUS-MAGNITUDE PASS: four original owners, positive native queries, five legal plays and one complete exchange. Moon=" + moon);
    }

    private static void CheckMoon(FixtureValue[] rows, BattleTurnState start, BattleTurnState finish, CombatUnit selene)
    {
        var deployment = State(rows[1], "After");
        var deployed = deployment.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == selene.Id);
        var shifted = finish.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == selene.Id);
        Require(deployment.MoonPhase == "New" && deployed.MoonPhaseMask == 5 && Stacks(deployed, "newmoon") == 1 &&
            deployed.Status("sweep") == null && start.MoonPhase == "Full" && selene.MoonPhaseMask == 6 &&
            Stacks(selene, "fullmoon") == 1 && Stacks(selene, "sweep") == 1 && selene.Status("newmoon") == null &&
            finish.MoonPhase == "New" && shifted.MoonPhaseMask == 5 && Stacks(shifted, "newmoon") == 1 &&
            shifted.Status("sweep") == null && shifted.Status("fullmoon") == null &&
            selene.Modifiers?.MagebladeDamagePerMagicPower == 4 && shifted.Modifiers?.MagebladeDamagePerMagicPower == 4,
            "Native Selene masks, temporary sweep and retained Mageblade coefficient differ.");

        var train = start.Spawn.Train;
        var advance = new CardActionEffect("AdvanceMoonPhase", "Room", 1, false, false, []);
        var owner = train.Context!.Relics!.Single(relic => relic.AssetKey == "DoubleMagebladeDamage");
        foreach (string control in new[] { "Amount", "Marker", "Team", "EffectIndex", "ExtraEffect", "Placement" })
        {
            var node = JsonSerializer.SerializeToNode(owner, ModelJson.Options)!;
            var rule = node["StatusMagnitudes"]![0]!;
            switch (control)
            {
                case "Amount": rule["Modifier"] = 2; break;
                case "Marker": rule["StatusId"] = "newmoon"; break;
                case "Team": rule["SourceTeam"] = 1; break;
                case "EffectIndex": rule["EffectIndex"] = 1; break;
                case "ExtraEffect": node["EffectTypes"]!.AsArray().Add(RelicModel.Null); break;
                case "Placement": node["DisallowedInPlacementPhase"] = true; break;
            }
            var changed = node.Deserialize<CombatRelicState>(ModelJson.Options)!;
            var context = train.Context!.WithRelics(train.Context.Relics!.Select(item => ReferenceEquals(item, owner) ? changed : item).ToArray());
            var rejected = MoonPhaseTransitionModel.Apply(CardSpellModel.WithContext(train, context), advance, playerActionOnly: false);
            Require(!rejected.Supported, "Unwitnessed Moon magnitude rule bypassed its guard: " + control);
        }
    }

    private static string? Difference(BattleTurnState? predicted, BattleTurnState actual) => predicted == null ? null :
        ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));

    private static BattleTurnState State(FixtureValue row, string key) => row.GetProperty(key).Deserialize<BattleTurnState>()!;
    private static int Stacks(CombatUnit unit, string status) => unit.Statuses.Single(item => item.Id == status).Stacks;
    private static void Require(bool condition, string error)
    {
        if (!condition) throw new InvalidDataException(error);
    }
}
