using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LazarusJuiceConduitChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 27, "Expected26 native prefix inputs plus the Conduit/Juice cast.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var selected = CardSpellModel.WithContext(before.Spawn.Train,
            context.WithQueryFrame(context.QueryFrame!.With(selectedRoomIndex: 2)));
        var during = selected.Context!.FindCard(30)!;
        var temporary = during.Temporary.Upgrades.Single();
        var played = after.Spawn.Train.Context!.FindCard(30)!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        Require(before.Spawn.Turn == 3 && before.Energy == 3 && after.Energy == 0 && action.CardInstanceId == 30 &&
            action.RoomIndex == 2 && action.TargetUnitId == 2 && context.QueryFrame.SelectedRoomIndex != 2 &&
            context.FindCard(30)!.Temporary.Upgrades.Count == 0 && temporary.DataId == "conduit:3" &&
            temporary.Stats.Damage == 5 && temporary.Stats.Heal == 5 && LazarusJuiceSpikeModel.Known(during) &&
            CardJuiceModel.Validate(during) == null && Target(after).Status("undying")!.Stacks == (Target(before).Status("undying")?.Stacks ?? 0) + 6 &&
            played.LastPlayedCost == 3 && played.PlayCount == 1 && played.Temporary.Upgrades.Count == 0 &&
            played.ExhaustState!.BaseTraits.Single().IsExhausted &&
            after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == 30),
            "Conduit5 must not increase scaled3/double6 Undying; preserve actual payment, Consume and cleared tracker upgrade.");
        foreach (string control in new[] { "Identity", "Asset", "Cost", "Heal", "Health", "Negative", "Lifecycle", "AddedTrait", "Callback", "Duplicate", "Offset" })
        {
            var changed = JsonSerializer.SerializeToNode(during, ModelJson.Options)!;
            var upgrade = changed["Temporary"]!["Upgrades"]![0]!;
            if (control == "Identity") upgrade["DataId"] = "unknown-conduit";
            if (control == "Asset") upgrade["AssetKey"] = "unknown-conduit";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 1;
            if (control == "Heal") upgrade["Stats"]!["Heal"] = 6;
            if (control == "Health") upgrade["Stats"]!["Health"] = 1;
            if (control == "Negative") { upgrade["Stats"]!["Damage"] = -1; upgrade["Stats"]!["Heal"] = -1; }
            if (control == "Lifecycle") upgrade["Lifecycle"] = null;
            if (control == "AddedTrait") upgrade["Lifecycle"]!["AddedTraits"]!.AsArray().Add(changed["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["AddedTraits"]![0]!.DeepClone());
            if (control == "Callback") upgrade["ExternalInteractions"]!.AsArray().Add("unknown-callback");
            if (control == "Duplicate") changed["Temporary"]!["Upgrades"]!.AsArray().Add(upgrade.DeepClone());
            if (control == "Offset") changed["Temporary"]!["Offsets"]!["Cost"] = 1;
            Require(!LazarusJuiceSpikeModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!), "Unknown Conduit/Juice payload admitted: " + control);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Conduit/Juice cast differs from its native full state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Conduit/Juice mutated its parent.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("LAZARUS-JUICE-CONDUIT PASS:27 native states/RNG, temporary Conduit5, paid3/scaled3/double6 Undying and Consume; four immutable branches/eleven guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
