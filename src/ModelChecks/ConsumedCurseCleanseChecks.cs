using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConsumedCurseCleanseChecks
{
    internal const string Reason = "Triple Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null, bool terminal = false)
    {
        var outcome = terminal ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged;
        var rows = NativeBranchObservationChecks.Replay(source, Reason, boundaryOutcome: outcome);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == (terminal ? 52 : 30) && before.Spawn.Turn == (terminal ? 8 : 4) &&
            rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong natural consumed-card curse boundary.");
        var card = before.Spawn.Train.Context!.FindCard(11)!;
        Require(card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && card.PlayCount == 1 &&
            card.ExhaustState!.BaseTraits.Single() is { IsExhausted: true, ExhaustedCardId: 11 } &&
            card.Permanent.Upgrades.Select(item => item.AssetKey).SequenceEqual(["TraitJuice3", "ChaosScepter"]) &&
            card.Temporary.Upgrades.Count == 0 && before.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == 13),
            "Missing unchanged consumed Cleanse and unplayed TitanCurse.");
        if (!terminal) {
            var after = actual.Spawn.Train.Context!.FindCard(11)!;
            Require(actual.Spawn.Turn == 5 && after.PlayCount == 1 &&
                after.ExhaustState!.BaseTraits.Single().IsExhausted && after.Temporary.Upgrades.Count == 1 &&
                CardJuiceModel.KnownTitanCurse(after.Temporary.Upgrades[0]) && CardJuiceModel.Validate(after) == null,
                "Native curse must upgrade the retained consumed card without casting it again.");
            foreach (string control in new[] { "Owner", "Curse", "Cost", "Damage", "Heal", "Unique", "Active", "Count", "Modifier", "ExtraUpgrade" }) {
                var node = JsonSerializer.SerializeToNode(after, ModelJson.Options)!;
                if (control == "Owner") node["DataId"] = "unknown-juice-card";
                if (control == "Curse") node["Temporary"]!["Upgrades"]![0]!["DataId"] = "unknown-curse";
                if (control == "Cost") node["Temporary"]!["Upgrades"]![0]!["Stats"]!["Cost"] = 2;
                if (control == "Damage") node["Temporary"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 1;
                if (control == "Heal") node["Temporary"]!["Upgrades"]![0]!["Stats"]!["Heal"] = 1;
                if (control == "Unique") node["Temporary"]!["Upgrades"]![0]!["Unique"] = true;
                if (control == "Active") node["ExhaustState"]!["BaseTraits"]![0]!["IsExhausted"] = false;
                if (control == "Count") node["PlayCount"] = 0;
                if (control == "Modifier") node["ExhaustState"]!["PlayCountModifier"] = 1;
                if (control == "ExtraUpgrade") node["Temporary"]!["Upgrades"]!.AsArray().Add(node["Temporary"]!["Upgrades"]![0]!.DeepClone());
                Require(CardJuiceModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                    "Changed consumed-curse signature lost guard: " + control);
            }
        }
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == outcome &&
                NativeBranchObservationChecks.Key(result.State!, terminal) == NativeBranchObservationChecks.Key(actual, terminal),
                "Consumed-curse branch differs from full native successor/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Consumed-curse branch mutated its parent.");
        if (replay.HasValue) {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine($"CONSUMED-CURSE-CLEANSE PASS: {rows.Length} natural inputs/full state/forecast/RNG; four immutable branches; terminal={terminal}; unknownSignatureGuards={(terminal ? 0 : 10)}; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
