using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnboxMoonRelicChecks
{
    private const string Reason = "Unboxing with relic modifiers after copied upgrades requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 13, "Missing12 native inputs plus unboxing.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var relic = context.Relics!.Single(owner => owner.DataId == RelicMoonPhaseHandUpgradeModel.OwnerId);
        Require(before.Spawn.Turn == 2 && action.CardInstanceId == 28 && action.RoomIndex == 0 &&
            context.NextCardId == 37 && context.FindCard(28)!.Permanent.BoxedCards!.Count == 3 &&
            RelicMoonPhaseHandUpgradeModel.CompatibleMoonSpells(relic), "Wrong natural unboxing/moon relic signature.");
        var generated = after.Spawn.Train.Context!;
        Require(generated.NextCardId == 40 && generated.Cards.Hand.Take(3).Select(card => card.InstanceId).SequenceEqual(new[] { 39, 38, 37 }) &&
            Enumerable.Range(37, 3).Select(id => generated.FindCard(id)!.DataId).SequenceEqual(new[] {
                "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84", "1450e56b-ca09-453a-937a-97bd44bcb3de", "1450e56b-ca09-453a-937a-97bd44bcb3de" }) &&
            Enumerable.Range(37, 3).All(id => generated.FindCard(id)!.Permanent.Upgrades.Count == 0 &&
                generated.FindCard(id)!.Temporary.Upgrades.Count == 0), "Native boxed spell allocation/order or premature moon upgrade differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(after),
                "Detached moon-relic unboxing differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Unboxing changed parent.");
        int ownerIndex = context.Relics!.ToList().FindIndex(owner => owner.DataId == relic.DataId);
        foreach (string control in new[] { "Owner", "Asset", "Phase", "Damage", "MissingModifier" })
        {
            var changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            // All room contexts are the same captured world. Replace each copy
            // so rejection tests exercise a consistent altered input.
            var train = changed["Spawn"]!["Train"]!;
            foreach (var changedContext in new[] { train["Context"]! }.Concat(train["Rooms"]!.AsArray().Select(room => room!["Context"]!)))
            {
                var owner = changedContext["Relics"]![ownerIndex]!;
                if (control == "Owner") owner["DataId"] = "unknown-moon-owner";
                if (control == "Asset") owner["AssetKey"] = "OtherMoonRelic";
                if (control == "Phase") owner["CardModifiers"]![0]!["MoonPhaseHandUpgrade"]!["MoonPhaseMask"] = 4;
                if (control == "Damage") owner["CardModifiers"]![0]!["MoonPhaseHandUpgrade"]!["Upgrade"]!["Stats"]!["Damage"] = 11;
                if (control == "MissingModifier") owner["CardModifiers"] = new System.Text.Json.Nodes.JsonArray();
            }
            Require(!BattleActionModel.PlayCard(changed.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Unknown unboxing/moon combination accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("UNBOX-MOON-RELIC PASS:13 native inputs/full state/forecast/RNG;37..39 Witchweave/VineGrasp copies without premature moon upgrades; four immutable branches/five signature guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
