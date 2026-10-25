using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class FirstOfKinExhaustChecks
{
    private const string Reason = "Native FirstOfKin attack after paired-boss ascension.";
    private const string FinalReason = "Native FirstOfKin attack after global equipment standby return.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (source.GetProperty("Request").GetProperty("Reason").GetString() == Reason) Controls(source);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("FIRST-OF-KIN-EXHAUST PASS: full native inputs/state/forecasts/RNG; global equipment returns dispatch Etch;" +
            (replay == null ? " independent replay pending." : " independent corrected native pair."));
    }
    private static void Check(FixtureValue fixture, bool historical)
    {
        bool final = fixture.GetProperty("Request").GetProperty("Reason").GetString() == FinalReason;
        var rows = NativeBranchObservationChecks.Replay(fixture, final ? FinalReason : Reason,
            historical ? (oldState, actual) => HistoricalMismatch(oldState, actual, final) : null);
        Require(rows.Length == (final ? 30 : 42) && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete original native input prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        if (final)
        {
            Require(before.Spawn.Turn == 5 && after.Spawn.Turn == 6 && Actor(before, 21).BaseAttack == 59 &&
                Actor(after, 21).BaseAttack == 74 && Actor(after, 21).Attack == 94 &&
                Actor(after, 21).Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == "f9a9fd6b-de5b-46d9-b733-0db48a8902a7") == 1,
                "Missing final-battle single equipment Etch boundary.");
            return;
        }
        Require(before.Spawn.Turn == 8 && after.Spawn.Turn == 9 &&
            Actor(before).BaseAttack == 45 && Actor(after).BaseAttack == 75 && Actor(after).Attack == 87 &&
            Actor(before).Health == 18 && Actor(after).Health == 18 &&
            Actor(after).Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == "f9a9fd6b-de5b-46d9-b733-0db48a8902a7" && upgrade.Stats.Damage == 15) == 2,
            "Missing native Etch upgrade/attack boundary.");
    }
    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual, bool final) =>
        Require(Actor(historical, final ? 21 : 4).BaseAttack == (final ? 59 : 45) &&
            Actor(actual, final ? 21 : 4).BaseAttack == (final ? 74 : 75),
            "Historical missing Etch attacks were replaced.");
    private static CombatUnit Actor(BattleTurnState state, int id = 4) => Actor(state.Spawn.Train, id);
    private static CombatUnit Actor(TrainCombatState state, int id = 4) => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static void Controls(FixtureValue fixture)
    {
        BattleTurnState native = fixture.GetProperty("Rows").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
        TrainCombatState root = native.Spawn.Train;
        CardToken token = root.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single(card => card.InstanceId == 17);
        TrainCombatState Prepare(bool hand = false, int host = 3, int room = 1)
        {
            CombatContext context = root.Context.WithOtherPiles(root.Context.OtherPiles!.Select(pile => pile.Name == "Exhausted"
                ? CardPileModel.Remove(pile, token.InstanceId) : pile.Name == "Standby"
                ? CardPileModel.BindEquipment(CardPileModel.Add(pile, token), new EquipmentStandbyCondition(token.InstanceId, host, hand)) : pile).ToArray());
            return CardSpellModel.WithContext(root, context.WithQueryFrame(context.QueryFrame!.With(selectedRoomIndex: room)));
        }
        TrainCombatState pending = Prepare();
        string immutable = JsonSerializer.Serialize(pending, ModelJson.Options);
        TrainCombatResult once = EquipmentModel.ReturnUnattached(pending, 0);
        Require(once.Supported && Actor(once.State!).BaseAttack == 90 &&
            Actor(EquipmentModel.ReturnUnattached(once.State!, 0).State!).BaseAttack == 90 &&
            immutable == JsonSerializer.Serialize(pending, ModelJson.Options), "Global return did not fire once immutably.");
        TrainCombatResult hand = EquipmentModel.ReturnUnattached(Prepare(hand: true), 0);
        Require(hand.Supported && Actor(hand.State!).BaseAttack == 75 && hand.State!.Context!.Cards.Hand[0].InstanceId == 17,
            "Return-to-hand incorrectly fired Etch.");
        TrainCombatResult living = EquipmentModel.ReturnUnattached(Prepare(host: 4), 0);
        Require(living.Supported && Actor(living.State!).BaseAttack == 75 &&
            living.State!.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == 17),
            "Living original host returned detached equipment early.");
        TrainCombatResult otherRoom = EquipmentModel.ReturnUnattached(Prepare(room: 0), 1);
        Require(otherRoom.Supported && Actor(otherRoom.State!).BaseAttack == 75 &&
            otherRoom.State!.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == 17),
            "Global Etch did not honor the native selected room.");
        Console.WriteLine("EQUIPMENT-STANDBY-ETCH-CONTROLS PASS: exactly once, immutable parent, return to hand, living host and selected room.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
