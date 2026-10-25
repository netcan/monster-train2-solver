using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceRitualChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Pair(FixtureValue refused, FixtureValue replay, FixtureValue observed)
    {
        var old = NativeBranchObservationChecks.Replay(refused, Reason);
        var first = NativeBranchObservationChecks.Replay(observed, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(old.Length == 1 && old[0].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            NativeBranchObservationChecks.Key(old[0].GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Original deployment refusal or native root was replaced.");
        Require(first.Length == 19 && second.Length == 19 &&
            observed.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent Ritual request changed.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent Ritual input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent Ritual state/RNG differs at " + index + ": " + field);
        }
        foreach (var rows in new[] { first, second })
        {
            var last = rows[^1];
            var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(11)!;
            var target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
            var changed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target.Id);
            Require(before.Spawn.Turn == 3 && action.CardInstanceId == 11 && action.RoomIndex == 0 && target.Id == 8 &&
                target.Team == CombatTeam.Player && !target.IsPyre && card.DataId == "f9f20326-dccb-44af-894b-467534afab4a" &&
                card.Permanent.Upgrades.Single(upgrade => upgrade.DataId == CardJuiceModel.Upgrade).AssetKey == "TraitJuice" &&
                card.StatusScalingTraits?.Count > 0 != true && CardJuiceModel.Validate(card) == null &&
                before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).Effects.Single().Statuses.Single().Stacks == 12 &&
                changed.Status("buff")!.Stacks == (target.Status("buff")?.Stacks ?? 0) + 24 &&
                changed.Attack == target.Attack + 48 && changed.Health == target.Health &&
                after.Spawn.Train.Context!.Statistics!.Value(11, "AnyStatusEffectStacksAdded") ==
                    before.Spawn.Train.Context.Statistics!.Value(11, "AnyStatusEffectStacksAdded") + 24,
                "Missing the native12->24 Rage/+48attack merchant Juice boundary.");
            var unknown = new CardInstanceState(card.InstanceId, "unknown-juice", card.Permanent, card.Temporary, 0, 0, 0, [], maskDescriptor: card.MaskDescriptor);
            var ordered = new CardInstanceState(card.InstanceId, card.DataId, card.Permanent, card.Temporary, 0, 0, 0, [],
                statusScalingTraits: [new(new("Gold"), 1, false, 0, ["buff"])], maskDescriptor: card.MaskDescriptor);
            Require(CardJuiceModel.Validate(unknown) == Reason && CardJuiceModel.Validate(ordered) == Reason,
                "Unknown Juice owners or ordered scaling callbacks bypassed their guard.");
        }
        Console.WriteLine("JUICE-RITUAL PASS: original deployment refusal preserved;19 independent paired native inputs/full state/RNG; Rage12->24/+48attack; unknown/ordered scaling guards retained.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
