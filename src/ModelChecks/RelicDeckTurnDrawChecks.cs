using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicDeckTurnDrawChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(619);
        var effect = new RelicResourceEffect(0, RelicDeckTurnDrawModel.Effect, 25, 0, 25, false);
        var owner = new CombatRelicState(RelicDeckTurnDrawModel.DataId, RelicDeckTurnDrawModel.AssetKey,
            [RelicDeckTurnDrawModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: true);
        CardToken Token(int id) => new(id, "draw-card-" + id);
        CombatContext Context(int deckCount, int maximum = 10, bool shuffle = false) => new(
            new([Token(1)], shuffle ? [] : [Token(2), Token(3), Token(4)],
                shuffle ? [Token(2), Token(3), Token(4)] : [], rng, 0, []), rng, 0, 1, maximum,
            relics: [owner], resources: new(3, 0, 5, 99, maximum, "pyre", 0, 80, 0, 0, [], [], [],
                visibleDeckCount: deckCount));
        foreach ((int count, int expected) in new[] { (0, 0), (24, 0), (25, 1), (49, 1), (50, 2), (74, 2), (75, 3) })
        {
            RelicResourceResult result = RelicDeckTurnDrawModel.Count(Context(count));
            Require(result.Supported && result.Value == expected, "Visible-deck draw threshold differs at " + count);
        }
        CombatContext source = Context(50);
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        RelicResourceResult draw = RelicDeckTurnDrawModel.Apply(source);
        Require(draw.Supported && draw.Value == 2 &&
            draw.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([3, 4, 1]) &&
            draw.Context.Cards.Draw.Single().InstanceId == 2 && draw.Context.Cards.Rng.Equals(rng),
            "Deck-scaled extra draw must use the ordinary draw order without inventing RNG consumption.");
        Require(RelicDeckTurnDrawModel.Apply(Context(50, 1)).Value == 0 &&
            ReferenceEquals(RelicDeckTurnDrawModel.Apply(source, placement: true).Context, source) &&
            ReferenceEquals(RelicDeckTurnDrawModel.Apply(source, preview: true).Context, source) &&
            ReferenceEquals(RelicDeckTurnDrawModel.ApplyAfterDraw(source.WithRelics([])).Context!.Cards, source.Cards),
            "Full hand, deployment, preview or removed-owner control changed draw state.");
        RelicResourceResult shuffled = RelicDeckTurnDrawModel.Apply(Context(50, shuffle: true));
        Require(shuffled.Supported && shuffled.Value == 2 && shuffled.Context!.Cards.Discard.Count == 0 &&
            shuffled.Context.Cards.Draw.Count == 1 && !shuffled.Context.Cards.Rng.Equals(rng),
            "Extra draws must reshuffle discard using the independent card-draw RNG.");
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent,
            "Deck-scaled draw mutated its parent.");
        Require(!RelicDeckTurnDrawModel.Count(Context(-1)).Supported &&
            RelicModel.Validate([new CombatRelicState("wrong-owner", RelicDeckTurnDrawModel.AssetKey,
                [RelicDeckTurnDrawModel.Effect], resourceEffects: [effect], disallowedInPlacementPhase: true)]) != null,
            "Missing native deck metadata or a changed owner must remain unsupported.");
        Console.WriteLine("DECK-TURN-DRAW-PURE PASS: visible save-deck thresholds, draw order, full-hand limit, reshuffle RNG, deployment/preview/removed-owner gates and parent immutability.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 219 &&
            root.GetProperty("OriginalDeckTurnDrawSchema").GetInt32() == 219 &&
            root.GetProperty("ModifierScenario").GetString() == "original-deck-turn-draw" &&
            root.GetProperty("OriginalDeckTurnDrawPrepared").GetBoolean() &&
            root.GetProperty("OriginalDeckTurnDraw").GetProperty("Restored").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalDeckTurnDrawCaptureFailures",
                "OriginalDeckTurnDrawMismatches", "OriginalDeckTurnDrawUnsupported", "OriginalDeckTurnDrawPending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Deck-scaled draw fixture is incomplete or has differential failures.");
        var controls = new List<int>();
        int natural = 0;
        foreach (FixtureValue row in root.GetProperty("RelicDeckTurnDrawTests").EnumerateArray())
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = row.GetProperty("After").Deserialize<CombatContext>()!;
            Require(RelicModel.Validate(before) == null, "Native deck-draw owner or resource definition changed.");
            RelicResourceResult count = RelicDeckTurnDrawModel.Count(before);
            Require(count.Supported && count.Value == row.GetProperty("NativeCount").GetInt32() &&
                row.GetProperty("Result").GetBoolean() == (count.Value > 0),
                "Native Test or private draw count differs from visible save-deck division.");
            Equal(before, after, "side-effect-free native Test");
            if (row.GetProperty("Phase").GetString() == "threshold-control")
                controls.Add(before.Resources!.VisibleDeckCount);
            else
            {
                Require(!row.GetProperty("Placement").GetBoolean() && before.Resources!.VisibleDeckCount == 50 &&
                    row.GetProperty("Turn").GetInt32() >= 1 && count.Value == 2,
                    "Natural deck draw ran outside the post-draw player-turn timing.");
                natural++;
            }
        }
        Require(controls.SequenceEqual([0, 24, 25, 49, 50, 24, 50]) && natural >= 2,
            "Missing threshold, repeat/reset or repeated natural timing controls.");
        int positive = 0;
        foreach (FixtureValue row in root.GetProperty("RelicDeckTurnDrawApplications").EnumerateArray())
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            Require(row.GetProperty("Completed").GetBoolean() && !row.GetProperty("Placement").GetBoolean() &&
                row.GetProperty("NativeCount").GetInt32() == 2 && before.Resources!.VisibleDeckCount == 50,
                "Native after-draw callback did not complete on the restored deck.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            RelicResourceResult predicted = RelicDeckTurnDrawModel.Apply(before);
            Require(predicted.Supported, "Native deck draw is unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.Context, actual, "native draw callback");
            Require(actual.Cards.Hand.Count - before.Cards.Hand.Count == predicted.Value,
                "Native hand-count delta differs from extra draws.");
            if (predicted.Value == 2) positive++;
            Parallel.For(0, 16, _ => Equal(RelicDeckTurnDrawModel.Apply(before).Context, actual, "parallel draw branch"));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Draw branches mutated their parent.");
        }
        Require(positive >= 2, "Missing repeated natural +2 hand transitions.");
        Console.WriteLine($"ORIGINAL-DECK-TURN-DRAW-NATIVE PASS: unchanged owner, seven native threshold/reset Tests, {natural} natural Tests, {positive} positive +2 after-draw callbacks, complete context/RNG and 16 immutable parallel branches.");
    }

    private static void Equal<T>(T a, T b, string label)
    {
        string predicted = JsonSerializer.Serialize(a, ModelJson.Options);
        string actual = JsonSerializer.Serialize(b, ModelJson.Options);
        Require(predicted == actual, "Deck draw " + label + " differs: " + ModelJson.Difference(predicted, actual));
    }

    private static void Require(bool condition, string error)
    { if (!condition) throw new InvalidOperationException(error); }
}
