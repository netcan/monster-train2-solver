using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StarstruckConduitChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TripleJuiceStarstruckChecks.Reason);
        Require(rows.Length == 34, "Wrong Starstruck Conduit focus boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var original = before.Spawn.Train.Context!.FindCard(18)!;
        var card = actual.Spawn.Train.Context!.FindCard(18)!;
        Require(before.Spawn.Turn == 7 && before.SelectedRoom == 1 && actual.SelectedRoom == 2 &&
            before.Energy == 4 && actual.Energy == 2 && action.CardInstanceId == 31 && action.RoomIndex == 2 &&
            original.DataId == "753f20a2-c625-47b3-be4b-8600b673841e" &&
            original.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["TraitJuice3", "ReduceCost"]) &&
            original.Temporary.Upgrades.Count == 1 && original.Temporary.Upgrades.All(CardJuiceModel.KnownTitanCurse) &&
            card.Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual([
                "f37fb903-28d6-4d49-b50e-9226f4854816", "conduit:12", "conduit:8", "conduit:2"]) &&
            card.Temporary.Upgrades.Skip(1).All(upgrade => LazarusJuiceSpikeModel.KnownConduit(upgrade) &&
                upgrade.Stats.Damage == 5 && upgrade.Stats.Heal == 5) && CardMagicPowerModel.Multiplier(card) == 1,
            "Native Starstruck must preserve its curse and three unmultiplied original Conduit upgrades.");
        var contributors = actual.Spawn.Train.Context.MagicPower!.Single(room => room.RoomIndex == 2 && room.Team == CombatTeam.Player).Conduit!.Units;
        Require(contributors.Select(unit => unit.UnitId).SequenceEqual([12, 8, 2]) &&
            contributors.All(unit => unit.Power == 5 && unit.Cards.Contains(18) && unit.MultipliedCards.Count == 0),
            "Native Conduit trackers differ from the three five-power contributors.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var branch = BattleActionModel.PlayCard(before, action);
            Require(branch.Supported && NativeBranchObservationChecks.Key(branch.State!) == NativeBranchObservationChecks.Key(actual),
                "Starstruck Conduit branch differs from full native context/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Starstruck Conduit branch mutated its parent.");
        foreach (string control in new[] { "Owner", "Asset", "NegativePower", "Heal", "Cost", "AddedTrait", "Duplicate", "Fourth" })
        {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrade = node["Temporary"]!["Upgrades"]![1]!;
            if (control == "Owner") upgrade["DataId"] = "uncalibrated-conduit";
            if (control == "Asset") upgrade["AssetKey"] = "uncalibrated-conduit";
            if (control == "NegativePower") upgrade["Stats"]!["Damage"] = -5;
            if (control == "Heal") upgrade["Stats"]!["Heal"] = 6;
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 1;
            if (control == "AddedTrait") upgrade["Lifecycle"]!["AddedTraits"]!.AsArray().Add(node["MaskDescriptor"]!["BaseTraits"]![1]!.DeepClone());
            if (control == "Duplicate") node["Temporary"]!["Upgrades"]!.AsArray().Add(upgrade.DeepClone());
            if (control == "Fourth")
            {
                var fourth = upgrade.DeepClone();
                fourth["DataId"] = "conduit:99"; fourth["Lifecycle"]!["Values"]!["DataId"] = "conduit:99";
                node["Temporary"]!["Upgrades"]!.AsArray().Add(fourth);
            }
            Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Starstruck Conduit composition lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, TripleJuiceStarstruckChecks.Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("STARSTRUCK-CONDUIT PASS:34 native inputs/full state/forecast/RNG; three unmultiplied five-power contributors with original curse and reduced-cost triple Juice; four immutable branches/eight scope controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
