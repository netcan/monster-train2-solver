using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LazarusJuiceMultiConduitChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? cast = null, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 20, "Expected19 native prefix inputs and the Aurora summon.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = actual.Spawn.Train.Context!.FindCard(13)!;
        Require(before.Spawn.Turn == 2 && action.CardInstanceId == 35 && action.RoomIndex == 2 &&
            before.Spawn.Train.Context!.FindCard(13)!.Temporary.Upgrades.Count == 0 &&
            card.DataId == LazarusJuiceSpikeModel.DataId && card.Temporary.Upgrades.Count == 2 &&
            card.Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(new[] { "conduit:4", "conduit:3" }) &&
            card.Temporary.Upgrades.All(upgrade => upgrade.Stats.Damage == 5 && upgrade.Stats.Heal == 5) &&
            LazarusJuiceSpikeModel.Known(card) && CardJuiceModel.Validate(card) == null,
            "Missing the natural twin-source Conduit5+5/Lazarus Juice hand refresh.");
        foreach (var control in new[] { "Duplicate", "Third", "Cost", "Health", "Heal", "Unknown" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrades = changed["Temporary"]!["Upgrades"]!.AsArray();
            if (control == "Duplicate") upgrades[1] = upgrades[0]!.DeepClone();
            if (control == "Third") { var third = upgrades[0]!.DeepClone(); third["DataId"] = "conduit:999"; upgrades.Add(third); }
            if (control == "Cost") upgrades[0]!["Stats"]!["Cost"] = 1;
            if (control == "Health") upgrades[0]!["Stats"]!["Health"] = 1;
            if (control == "Heal") upgrades[0]!["Stats"]!["Heal"] = 6;
            if (control == "Unknown") upgrades[0]!["DataId"] = "unknown-conduit";
            Require(!LazarusJuiceSpikeModel.Known(changed.Deserialize<CardInstanceState>(ModelJson.Options)!),
                "Uncalibrated twin-source Conduit admitted: " + control);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Twin-source hand refresh differs from complete native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Twin-source refresh mutated its parent.");
        if (cast.HasValue)
        {
            CheckCast(cast.Value);
            var castRows = cast.Value.GetProperty("Rows").EnumerateArray().ToArray();
            for (int index = 0; index < rows.Length; index++)
            {
                foreach (var field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(castRows[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "The dual-source cast changed its original native prefix: " + index);
                foreach (var field in new[] { "Action", "IsEndTurn" })
                    Require(rows[index].GetProperty(field).ContentEquals(castRows[index].GetProperty(field)), "The cast changed its prefix input.");
            }
            if (replay.HasValue)
            {
                CheckCast(replay.Value);
                NativeBranchObservationChecks.Pair(cast.Value, replay.Value, originalPredictionPresent: true, originalPredictionMatched: true);
            }
        }
        Require(!replay.HasValue || cast.HasValue, "A cast replay needs its source.");
        Console.WriteLine("LAZARUS-JUICE-MULTI-CONDUIT PASS:20 original native inputs/state/RNG, distinct5+5 sources, six guards/four immutable branches; castObserved=" + cast.HasValue + " independentCastPair=" + replay.HasValue);
    }
    private static void CheckCast(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 21, "Expected20 native inputs followed by the actual twin-Conduit Juice cast.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = after.Spawn.Train.Context!.FindCard(13)!;
        Require(before.Spawn.Turn == 2 && before.Energy == 3 && action.CardInstanceId == 13 &&
            action.RoomIndex == 2 && action.TargetUnitId == 3 && before.Spawn.Train.Context!.FindCard(13)!.Temporary.Upgrades.Count == 2 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3).Status("undying")?.Stacks ==
                (before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3).Status("undying")?.Stacks ?? 0) + 6 &&
            card.LastPlayedCost == 3 && card.PlayCount == 1 && card.Temporary.Upgrades.Count == 0 &&
            card.ExhaustState!.BaseTraits.Single().IsExhausted &&
            after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 13),
            "Two Conduit5 upgrades must preserve paid3/double6 Undying, Consume and cleared temporary modifiers.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "The dual-source cast differs from full native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "The dual-source cast mutated its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
