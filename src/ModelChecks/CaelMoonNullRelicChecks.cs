using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CaelMoonNullRelicChecks
{
    internal const string Reason = "Uncalibrated moon hand-refresh relic: Enemy_SpawnHeroEaterFeedLifesteal.";
    private const string Owner = "2424d14a-6fd6-4458-836e-75ad7315a22a";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 4 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong natural Cael Moon hand-refresh boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var owner = context.Relics!.Single(relic => relic.DataId == Owner);
        var card = context.FindCard(12)!;
        Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 && before.MoonPhase == "New" && actual.MoonPhase == "Full" &&
            owner.AssetKey == "Enemy_SpawnHeroEaterFeedLifesteal" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicModel.Null, RelicModel.Null]) &&
            context.Cards.Hand.Any(token => token.InstanceId == 12) && card.DataId == "753f20a2-c625-47b3-be4b-8600b673841e" &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["TraitJuice3", "ReduceCost", "StarstruckRoom_Upgrade"]),
            "Native null Cael owner must preserve the naturally drawn triple/reduced-cost Starstruck Full Moon upgrade.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var branch = BattleTurnModel.EndTurn(before);
            Require(branch.Supported && NativeBranchObservationChecks.Key(branch.State!) == NativeBranchObservationChecks.Key(actual),
                "Cael null relic branch differs from complete native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Cael null relic branch mutated its parent.");
        foreach (string control in new[] { "Owner", "Asset", "Covenant", "Placement", "MissingEffect", "ExtraEffect", "Callback" })
        {
            var node = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            var changed = node["Relics"]!.AsArray().Single(relic => relic!["DataId"]!.GetValue<string>() == Owner)!;
            if (control == "Owner") changed["DataId"] = "uncalibrated-cael-owner";
            if (control == "Asset") changed["AssetKey"] = "uncalibrated-cael-owner";
            if (control == "Covenant") changed["IsCovenant"] = true;
            if (control == "Placement") changed["DisallowedInPlacementPhase"] = true;
            if (control == "MissingEffect") changed["EffectTypes"]!.AsArray().RemoveAt(1);
            if (control == "ExtraEffect") changed["EffectTypes"]!.AsArray().Add(RelicModel.Null);
            if (control == "Callback") changed["EffectTypes"]![0] = RelicModel.AddTrigger;
            CardMoonUpgradeModel.RefreshHand(node.Deserialize<CombatContext>(ModelJson.Options)!, out string? error);
            Require(error != null, "Unknown Cael Moon relic signature lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("CAEL-MOON-NULL-RELIC PASS:4 native inputs/full state/forecast/RNG; original two-null owner and naturally drawn Full Moon triple/reduced-cost Starstruck; four immutable branches/seven scope controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
