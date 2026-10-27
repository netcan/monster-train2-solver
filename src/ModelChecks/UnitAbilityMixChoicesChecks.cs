using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitAbilityMixChoicesChecks
{
    private const string Reason = "Unit ability Mix choices were omitted from the action frontier.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 13, "Expected12 native prefix inputs plus the chosen champion ability.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 1 && before.Energy == 1 && after.Energy == 1 &&
            action.CardInstanceId == 34 && action.ActivatorUnitId == 2 && action.RoomIndex == 2 &&
            action.MixSelections.Count == 2 && action.MixSelections.Select(choice => choice.EffectIndex).SequenceEqual([0, 1]) &&
            action.MixSelections.All(choice => choice.MixNumber == 0 && choice.ChoiceIndex == 0),
            "Expected the free two-draft Lazarus champion ability with explicit first offers.");
        var actor = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        var rule = before.PlayRules!.Cards.Single(card => card.DataId == actor.Ability!.DataId);
        Require(rule.DataId == "90366301-c0b5-4639-8a0a-387eddff089b" && rule.Effects.Count == 2 &&
            rule.Effects.All(effect => effect.Type == "Mix" && effect.Value == 2), "Wrong natural two-draft ability.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var choices = UnitAbilityModel.EnumerateSupportedActivations(before).Where(candidate =>
                candidate.ActivatorUnitId == 2 && candidate.RoomIndex == 2).ToArray();
            Require(choices.Length == 4 && choices.All(candidate => candidate.MixSelections.Count == 2) &&
                choices.Select(candidate => string.Join(",", candidate.MixSelections.Select(choice => choice.ChoiceIndex)))
                    .Order().SequenceEqual(["0,0", "0,1", "1,0", "1,1"]),
                "Ability frontier must retain all four ordered two-draft offer combinations.");
            var chosen = choices.Single(candidate => candidate.MixSelections.All(choice => choice.ChoiceIndex == 0));
            var result = BattleActionModel.PlayCard(before, chosen);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Enumerated ability choice differs from its complete native successor/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Ability choices mutated their parent.");
        var missing = BattleActionModel.PlayCard(before, new PlayCardAction(34, 2, activatorUnitId: 2));
        var outside = BattleActionModel.PlayCard(before, new PlayCardAction(34, 2, activatorUnitId: 2,
            mixSelections: [new ElixirMixSelection(0, 0, 2), new ElixirMixSelection(1, 0, 0)]));
        Require(!missing.Supported && !outside.Supported && missing.Reason == "Mix draft selection is missing or outside its offers." &&
            outside.Reason == missing.Reason, "Missing/out-of-range offers must stay guarded instead of choosing a default.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("UNIT-ABILITY-MIX-CHOICES PASS:13 native states/RNG; all four ordered draft pairs enter the frontier; four immutable parallel enumerations; missing/out-of-range guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
