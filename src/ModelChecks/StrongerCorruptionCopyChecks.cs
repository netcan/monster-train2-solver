using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StrongerCorruptionCopyChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, UndyingCopyOnPlayChecks.Reason);
        Require(rows.Length == 1 && source.GetProperty("Request").GetProperty("Prefix").GetArrayLength() == 0,
            "Expected the original deployment rejection.");
        BattleTurnState before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(14)!;
        Require(card.DataId == CardCopyOnPlayModel.StrongerCorruptionSpellId &&
            CardCopyOnPlayModel.Present(card) && CardCopyOnPlayModel.Validate(card) == null &&
            before.Spawn.Train.Context.Cards.Draw.Any(token => token.InstanceId == card.InstanceId),
            "Missing the native stronger-magic/corruption spell with merchant copy upgrade.");
        foreach (string control in new[] { "magic", "corruption", "runtime", "card", "XCost", "status" })
        {
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var descriptor = json["MaskDescriptor"]!;
            if (control == "magic") descriptor["SourceTraits"]![0]!["Int"] = 6;
            else if (control == "corruption") descriptor["SourceTraits"]![1]!["Int"] = 3;
            else if (control == "runtime") descriptor["BaseTraits"]![0]!["ParamInt"] = 6;
            else if (control == "card") descriptor["Definition"]!["DataId"] = "uncalibrated-card";
            else if (control == "XCost") descriptor["Definition"]!["XCost"] = true;
            else descriptor["SourceTraits"]![0]!["StatusEffectIds"] = JsonSerializer.SerializeToNode(new[] { "poison" });
            Require(CardCopyOnPlayModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == UndyingCopyOnPlayChecks.Reason,
                "Uncalibrated source signature accepted: " + control);
        }
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, UndyingCopyOnPlayChecks.Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("STRONGER-CORRUPTION-COPY PASS: native deployment/full state/RNG; exact5x magic/corruption2 source, six rejection controls; spell-copy execution requires separate native evidence; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
