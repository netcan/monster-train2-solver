using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceTraitChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("JuiceTraitCalibration", out FixtureValue samples) || samples.GetArrayLength() == 0) return;
        int controls = 0, plays = 0;
        foreach (FixtureValue sample in samples.EnumerateArray())
        {
            int input = sample.GetProperty("Input").GetInt32(), bonus = sample.GetProperty("Bonus").GetInt32();
            Require(sample.GetProperty("UpgradeId").GetString() == CardJuiceModel.Upgrade &&
                sample.GetProperty("ParamInt").GetInt32() == 2 && sample.GetProperty("Multiplier").GetInt32() == 2 &&
                bonus == Math.Max(input, 0) && CardJuiceModel.Apply(input) == unchecked(input + bonus),
                "Original merchant Juice native callback or signed/wrapped stack composition differs.");
            controls++;
        }
        Require(controls == 6, "Missing isolated original Juice callback boundaries.");
        Require(samples.EnumerateArray().Select(sample => sample.GetProperty("Input").GetInt32())
            .SequenceEqual(new[] { int.MinValue, -3, 0, 1, 4, int.MaxValue }), "Native Juice boundaries changed.");
        BattleTurnState initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        CombatContext context = initial.Spawn.Train.Context!;
        CardInstanceState original = context.CardInstances!.Single(card => CardJuiceModel.Present(card));
        Require(original.Permanent.Upgrades.Any(upgrade => upgrade.DataId == CardJuiceModel.Upgrade) &&
            CardJuiceModel.Validate(original) == null, "Missing the original merchant upgrade on the captured spell.");
        foreach (FixtureValue sample in samples.EnumerateArray())
        foreach (string status in new[] { "lifesteal", "scorch" })
        {
            int input = sample.GetProperty("Input").GetInt32(), bonus = sample.GetProperty("Bonus").GetInt32();
            StatusScalingResult applied = StatusScalingModel.Apply(context, original.InstanceId, CombatTeam.Player, status, input);
            Require(applied.Supported && applied.Stacks == unchecked(input + bonus),
                "Actual captured Juice owner diverged from the isolated native callback.");
        }
        var unknownOwner = new CardInstanceState(original.InstanceId, "unobserved-juice-owner", original.Permanent, original.Temporary,
            0, 0, 0, [], maskDescriptor: original.MaskDescriptor);
        Require(CardJuiceModel.Validate(unknownOwner) != null, "Uncalibrated Juice ownership bypassed its guard.");
        foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
        {
            if (row.GetProperty("IsEndTurn").GetBoolean()) continue;
            BattleTurnState root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState? card = root.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            if (card == null || !CardJuiceModel.Present(card)) continue;
            Require(CardJuiceModel.Validate(card) == null &&
                StatusScalingModel.Apply(root.Spawn.Train.Context, card.InstanceId, CombatTeam.Player, "lifesteal", 4).Stacks == 8 &&
                StatusScalingModel.Apply(root.Spawn.Train.Context, card.InstanceId, CombatTeam.Player, "scorch", 2).Stacks == 4,
                "Captured Juice spell did not double positive and negative source stacks.");
            var unknown = new CardInstanceState(card.InstanceId, "unobserved-juice-owner", card.Permanent, card.Temporary,
                0, 0, 0, [], maskDescriptor: card.MaskDescriptor);
            Require(CardJuiceModel.Validate(unknown) != null, "Uncalibrated Juice ownership bypassed its guard.");
            plays++;
        }
        Console.WriteLine($"NATIVE-JUICE PASS: {controls} native callback boundaries, {plays} observed original spell plays; other card signatures remain guarded.");
    }
    private static void Require(bool value, string reason)
    { if (!value) throw new InvalidOperationException(reason); }
}
