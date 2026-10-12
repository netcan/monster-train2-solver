using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicStatusMagnitudeChecks
{
    internal static void Run()
    {
        CombatRelicState[] teams =
        [
            Relic("either", new(0, 0, "buff", 1)),
            Relic("heroes", new(0, 1, "buff", 10)),
            Relic("monsters", new(0, 2, "buff", 100)),
            Relic("all-flags", new(0, 255, "buff", 1000)),
            Relic("different-status", new(0, 0, "spikes", 7000))
        ];
        CombatContext root = Context(teams);
        string parent = Serialize(root);
        Require(Get(root, "buff", 0).Stacks == 1111 && Get(root, "buff", 1).Stacks == 11 &&
            Get(root, "buff", 2).Stacks == 101 && Get(root, "buff", 255).Stacks == 1111,
            "Native target.HasFlag(source) direction, None target or None source differs.");
        Require(Get(root, "spikes", 2).Stacks == 7000 && Get(root, "regen", 2).Stacks == 0 &&
            Get(null, "buff", 2).Stacks == 0 && Get(Context([]), "buff", 2).Stacks == 0,
            "Magnitude status filters or empty-context behavior differ.");

        var ordered = new CombatRelicState("ordered", "ordered",
            [RelicModel.ModifyStatusMagnitude, RelicModel.ModifyStatusMagnitude],
            statusMagnitudes: [new(0, 2, "buff", int.MaxValue), new(1, 2, "buff", 2)]);
        Require(Get(Context([ordered]), "buff", 2).Stacks == int.MinValue + 1 &&
            Get(Context([Relic("minimum", new(0, 2, "buff", int.MinValue)),
                Relic("negative", new(0, 2, "buff", -1))]), "buff", 2).Stacks == int.MaxValue,
            "Magnitude accumulation clamped or rejected native signed overflow.");

        var first = new RelicConditionState(new("SubtypeInDeck"), true, 6, 1, false);
        var limit = new RelicConditionState(new("SubtypeInDeck"), true, 6, 1, true);
        CombatContext limited = Context([Relic("first", new(0, 2, "buff", 3, [first])),
            Relic("limit", new(0, 2, "buff", 5, [limit]))]);
        string limitedParent = Serialize(limited);
        for (int index = 0; index < 12; index++)
        {
            StatusScalingResult repeated = Get(limited, "buff", 2);
            Require(repeated.Stacks == 8 && Serialize(repeated.Context) == limitedParent,
                "A magnitude value query notified or recorded a first/limited outer condition.");
            limited = repeated.Context!;
        }
        var spentFirst = first.Recorded(0);
        var spentLimit = limit.Recorded(0);
        Require(Get(Context([Relic("spent-first", new(0, 2, "buff", 3, [spentFirst])),
            Relic("spent-limit", new(0, 2, "buff", 5, [spentLimit]))]), "buff", 2).Stacks == 0,
            "Spent first/limited outer conditions still contributed a magnitude.");
        var reset = RelicConditionModel.EndDuration(Context([Relic("spent", new(0, 2, "buff", 3, [spentFirst]))]), "ThisTurn");
        Require(Get(reset, "buff", 2).Stacks == 3 &&
            reset.Relics![0].StatusMagnitudes![0].Conditions[0].DurationTriggerCount == 0,
            "Magnitude condition duration reset was lost.");

        var queryCondition = new RelicConditionState(new("AnyCardPlayed"), false, 2, 0, true);
        var staleStatistics = new BattleStatistics([new(9, "ThisTurn", "TimesPlayed", 99)], [], [], [], [], [], [],
            0, 0, 0, 0, 0, trackedCards: [1, 9], deckCards: [1]);
        CombatContext stale = new(new([new(1, "spell")], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13),
            0, 2, 10, statistics: staleStatistics, otherPiles: [],
            relics: [Relic("filtered-query", new(0, 1, "regen", 4, [queryCondition]))]);
        string staleParent = Serialize(stale);
        StatusScalingResult filteredQuery = Get(stale, "buff", 2);
        Require(filteredQuery.Stacks == 0 && filteredQuery.Context!.Statistics!.TrackedCards.SequenceEqual([1]) &&
            filteredQuery.Context.Statistics.Values.All(value => value.CardId != 9) &&
            !filteredQuery.Context.Relics![0].StatusMagnitudes![0].Conditions[0].Triggered,
            "Status/team filtering skipped condition evaluation or discarded its refreshed context.");
        var unsupportedCondition = new RelicConditionState(new("NotCaptured"), false, 2, 0, true);
        Require(!RelicStatusMagnitudeModel.GetModifier(Context([Relic("bad-query",
            new(0, 1, "regen", 4, [unsupportedCondition]))]), "buff", 2).Supported,
            "A mismatching status/team hid an unsupported outer condition query.");

        Require(RelicStatusMagnitudeModel.Validate(new("missing", "missing", [RelicModel.ModifyStatusMagnitude])) != null &&
            !RelicStatusMagnitudeModel.GetModifier(Context([new("missing", "missing", [RelicModel.ModifyStatusMagnitude])]), "buff", 2).Supported,
            "Missing native magnitude payload was guessed.");
        Require(RelicStatusMagnitudeModel.Validate(new("unordered", "unordered",
            [RelicModel.ModifyStatusMagnitude, RelicModel.ModifyStatusMagnitude],
            statusMagnitudes: [new(1, 2, "buff", 1), new(0, 2, "buff", 1)])) != null &&
            RelicStatusMagnitudeModel.Validate(new("unexpected", "unexpected", [], statusMagnitudes: [new(0, 2, "buff", 1)])) != null,
            "Unordered or unexpected native magnitude payload was accepted.");
        foreach (string status in new[] { "", "not-a-status", "armor" })
            Require(!RelicStatusMagnitudeModel.GetModifier(Context([Relic("unsupported", new(0, 2, status, 1))]), "buff", 2).Supported,
                "An uncaptured or unmodeled magnitude consumer was accepted: " + status);
        foreach (string status in new[] { "buff", "debuff", "spikes", "regen", "mageblade", "titanskin" })
            Require(RelicStatusMagnitudeModel.Validate(Relic("known", new(0, 2, status, 1))) == null,
                "A native modeled magnitude consumer was rejected: " + status);

        Parallel.For(0, 32, _ =>
        {
            Require(Get(root, "buff", 2).Stacks == 101 &&
                Serialize(Get(stale, "buff", 2)) == Serialize(filteredQuery),
                "Parallel immutable magnitude queries diverged.");
        });
        Require(Serialize(root) == parent && Serialize(stale) == staleParent &&
            Get(stale, "buff", 2).Context!.BattleRng.Equals(stale.BattleRng),
            "Magnitude queries mutated parent state or consumed RNG.");
        Console.WriteLine("RELIC-STATUS-MAGNITUDE-CHECKS PASS: native team masks, ordered overflow, unrecorded outer conditions, query context propagation, payload guards and 32 immutable branches.");
    }

    private static CombatRelicState Relic(string id, RelicStatusMagnitude rule) =>
        new(id, id, [RelicModel.ModifyStatusMagnitude], statusMagnitudes: [rule]);
    private static CombatContext Context(IReadOnlyList<CombatRelicState> relics) =>
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: relics);
    private static StatusScalingResult Get(CombatContext? context, string statusId, int characterTeam)
    {
        StatusScalingResult result = RelicStatusMagnitudeModel.GetModifier(context, statusId, characterTeam);
        Require(result.Supported, result.UnsupportedReason ?? "Magnitude getter failed.");
        return result;
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
