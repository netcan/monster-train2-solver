using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnboxConduitChecks
{
    private const string Reason = "Unboxed hand cards miss active Conduit modifiers.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 10, "Wrong unboxing native prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(rows[^1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 15 &&
            before.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 0 &&
            before.Spawn.Train.Context.FindCard(15)!.Permanent.BoxedCards!.Count == 3,
            "Missing the three-card box on the active Conduit floor.");
        CheckPower(actual, present: true);
        CheckPower(historical, present: false);
        BattleActionResult predicted = BattleActionModel.PlayCard(before,
            rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!);
        Require(predicted.Supported, predicted.Reason ?? "Unboxing unsupported.");
        CheckPower(predicted.State!, present: true);
        CardUpgradeModifier template = actual.Spawn.Train.Context!.MagicPower!
            .Single(room => room.RoomIndex == 0 && room.Team == CombatTeam.Player).Conduit!.Upgrade;
        ConduitChecks.Run(template);
        if (replay is FixtureValue second)
        {
            var other = NativeBranchObservationChecks.Replay(second, Reason);
            Require(source.GetProperty("Request").ContentEquals(second.GetProperty("Request")) && other.Length == rows.Length,
                "Independent replay changed its request.");
            for (int i = 0; i < rows.Length; i++)
            {
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(other[i].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent full state/RNG changed at " + i);
                foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                    Require(rows[i].GetProperty(field).ContentEquals(other[i].GetProperty(field)), "Independent input changed.");
            }
            CheckPower(other[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, present: true);
            CheckPower(other[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!, present: true);
        }
        Console.WriteLine("UNBOX-CONDUIT PASS:10 native inputs/full state/RNG; three copied hand cards gain10 damage/heal; original missing modifiers and immutable parents retained; Conduit controls pass; independentPair=" + replay.HasValue);
    }

    private static void CheckPower(BattleTurnState state, bool present)
    {
        CombatContext context = state.Spawn.Train.Context!;
        ConduitUnitState owner = context.MagicPower!.Single(room => room.RoomIndex == 0 && room.Team == CombatTeam.Player)
            .Conduit!.Units.Single(unit => unit.UnitId == 12);
        Require(owner.Power == 10 && owner.Active && owner.Cards.OrderBy(id => id).SequenceEqual(present ? new[] { 23, 24, 25 } : []),
            "Unboxed card membership/power differs.");
        foreach (int id in new[] { 23, 24, 25 })
        {
            CardUpgradeModifier[] upgrades = context.FindCard(id)!.Temporary.Upgrades.Where(upgrade => upgrade.DataId == "conduit:12").ToArray();
            Require(present ? upgrades.Length == 1 && upgrades[0].Stats.Damage == 10 && upgrades[0].Stats.Heal == 10 : upgrades.Length == 0,
                "Unboxed card magic power differs at " + id);
        }
        // Raw tracker dictionary order is not a gameplay identity; the normal
        // battle comparable clears selection. Check its raw membership above.
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
