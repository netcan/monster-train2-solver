using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InherentJuiceCurseChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 37 && rows[^1].GetProperty("IsEndTurn").GetBoolean(),
            "Missing36 native inputs plus the inherent Spike curse EndTurn.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 6 && after.Spawn.Turn == 7, "Wrong inherent curse turn.");
        foreach (int id in new[] { 17, 24, 26 })
        {
            var old = before.Spawn.Train.Context!.FindCard(id)!;
            var card = after.Spawn.Train.Context!.FindCard(id)!;
            Require(old.Temporary.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 1 &&
                card.Temporary.Upgrades[0].DataId == "f37fb903-28d6-4d49-b50e-9226f4854816" &&
                card.Temporary.Upgrades[0].Stats.Cost == 1 && CardJuiceModel.Validate(card) == null &&
                CardModifierModel.UpgradedStat(0, "Cost", true, card.Permanent, card.Temporary) == (id == 24 ? 0 : 1),
                "Native curse on the three independently upgraded Spike owners differs.");
        }
        var inherent = after.Spawn.Train.Context!.FindCard(24)!;
        Require(inherent.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey)
            .SequenceEqual(new[] { "TraitJuice", "TraitInherentSpellReduceCost" }) &&
            CardModifierModel.UpgradedStat(0, "XCost", true, inherent.Permanent, inherent.Temporary) == 1,
            "Curse changed the inherent X upgrade or its order.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(after),
                "Detached inherent curse successor differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Inherent curse changed its parent.");
        foreach (string control in new[] { "Id", "Asset", "Cost", "Damage", "Health", "XCost", "Trait", "External" })
        {
            var changed = JsonSerializer.SerializeToNode(inherent, ModelJson.Options)!;
            var upgrade = changed["Temporary"]!["Upgrades"]![0]!;
            if (control == "Id") upgrade["DataId"] = "unknown-curse";
            if (control == "Asset") upgrade["AssetKey"] = "OtherCurse";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 2;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 1;
            if (control == "Health") upgrade["Stats"]!["Health"] = 1;
            if (control == "XCost") upgrade["Stats"]!["XCost"] = 1;
            if (control == "Trait") upgrade["Lifecycle"]!["AddedTraits"]!.AsArray().Add(
                changed["Permanent"]!["Upgrades"]![1]!["Lifecycle"]!["AddedTraits"]![0]!.DeepClone());
            if (control == "External") upgrade["ExternalInteractions"]!.AsArray().Add("unknown-curse-payload");
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown inherent curse signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("INHERENT-JUICE-CURSE PASS:37 native inputs/full state/forecast/RNG; simultaneous triple/double/inherent Spike cost+1; inherent cost0/X+1; four immutable branches/eight guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
