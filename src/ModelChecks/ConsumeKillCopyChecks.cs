using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConsumeKillCopyChecks
{
    private const string Reason = "Card OnKill self-copy requires unchanged original base traits, no starting upgrades and a simple spell creation definition.";
    private const string Surgical = "e48d1711-fdff-41d4-b2a0-bc8f8b2bf8dd";
    internal static void Pair(FixtureValue refused, FixtureValue replay, FixtureValue observed)
    {
        var old = NativeBranchObservationChecks.Replay(refused, Reason);
        var first = NativeBranchObservationChecks.Replay(observed, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(old.Length == 1 && old[0].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            NativeBranchObservationChecks.Key(old[0].GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Original deployment refusal or source root was overwritten.");
        Require(first.Length == 18 && second.Length == 18 &&
            observed.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent self-copy request differs.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent self-copy input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent self-copy state/RNG differs at " + index + ": " + field);
        }
        foreach (var rows in new[] { first, second })
        {
            var last = rows[^1];
            var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var actual = after.Spawn.Train.Context!;
            var card = context.FindCard(24)!;
            var added = actual.Cards.Hand.Single(token => token.InstanceId >= context.NextCardId);
            var child = actual.FindCard(added.InstanceId)!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Surgical);
            var resolved = CardModifierModel.Resolve(rule, child);
            Require(before.Spawn.Turn == 3 && action.CardInstanceId == 24 && action.RoomIndex == 2 && action.TargetUnitId == 15 &&
                after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 15) &&
                child.InstanceId == 33 && child.DataId == Surgical && actual.NextCardId == context.NextCardId + 1 &&
                child.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(new[] { "ChaosScepter", "ReduceCost" }) &&
                child.Temporary.Upgrades.Single().AssetKey == "SurgicalStrike_Upgrade" &&
                child.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(new[] {
                    CardKillTriggerModel.SuppressTrait, "CardTraitIgnoreArmor", "CardTraitExhaustState" }) &&
                child.MaskDescriptor.SourceTraits!.Count == 2 && child.ExhaustState!.BaseTraits.Single() is
                    { TraitIndex: 2, IsExhausted: false, ExhaustedCardId: null } &&
                child.ExhaustState.UpgradeTrait == null && CardExhaustModel.HasConsume(child) &&
                CardBranchMaskModel.Resolve(child).Traits.Contains("CardTraitEphemeral") &&
                resolved.Cost == 0 && resolved.Effects.Single().Value == 24 &&
                actual.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 24),
                "Missing lethal24damage/zero-cost self-copy, inherited upgrades, one reset Consume trait or Ephemeral child.");
            var d = card.MaskDescriptor!;
            var unknown = new CardInstanceState(card.InstanceId, "unknown-self-copy", card.Permanent, card.Temporary, 0, 0, 0, [], maskDescriptor: d);
            var duplicate = card.WithMaskDescriptor(new(d.Definition, d.BaseCost, d.BaseTraits.Concat(new[] { d.BaseTraits.Last() }).ToArray(),
                d.AuthoredCastEffects, d.Purified, d.PermanentGraft, d.TemporaryTraits, d.PermanentReplacements, d.SourceTraits, d.KillTriggers));
            Require(CardKillTriggerModel.Validate(unknown) == Reason && CardKillTriggerModel.Validate(duplicate) == Reason,
                "Unknown self-copy owners or extra Consume traits bypassed their guard.");
        }
        Console.WriteLine("CONSUME-KILL-COPY PASS: original refusal preserved;18 paired native inputs/full state/RNG; child33 inherits +20power/zero cost/Consume and Ephemeral, one reset exhaust state; unknown/duplicate guards retained.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
