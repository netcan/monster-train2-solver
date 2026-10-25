using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BoxedCopyTraitChecks
{
    private const string Reason = "Copy-on-play requires the original single-copy spell upgrade and captured purge/replacement definition.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 25, "Changed native boxed-copy prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 23 && action.RoomIndex == 1 &&
            before.Spawn.Train.Context!.FindCard(33) == null &&
            actual.Spawn.Train.Context!.FindCard(33)?.DataId == CardCopyOnPlayModel.StrongerCorruptionSpellId,
            "Missing naturally unboxed stronger-corruption spell.");
        Require(CardCopyOnPlayModel.Validate(actual.Spawn.Train.Context!.FindCard(33)!) == null,
            "The native initialized copy trait must already be supported.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel unboxing differs from complete native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Unboxing mutated its parent.");
        JsonNode missing = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode box = missing["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
            .Single(card => card!["InstanceId"]!.GetValue<int>() == action.CardInstanceId)!;
        JsonNode creation = box["Permanent"]!["BoxedCards"]!.AsArray().Single(content =>
            content!["Creation"]!["DataId"]!.GetValue<string>() == CardCopyOnPlayModel.StrongerCorruptionSpellId)!["Creation"]!;
        Require(creation["CanBeCopied"]!.GetValue<bool>(), "Native boxed copyability was not captured.");
        creation["CanBeCopied"] = null;
        var unknown = missing.Deserialize<BattleTurnState>(ModelJson.Options)!;
        string unknownParent = JsonSerializer.Serialize(unknown, ModelJson.Options);
        var rejected = BattleActionModel.PlayCard(unknown, action);
        Require(!rejected.Supported && rejected.Reason == Reason &&
            JsonSerializer.Serialize(unknown, ModelJson.Options) == unknownParent,
            "Missing copyability must retain its existing guard and immutable parent.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("BOXED-COPY-TRAIT PASS: twenty-five native inputs, unboxed stronger-corruption copy trait/full state/RNG and eight immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
