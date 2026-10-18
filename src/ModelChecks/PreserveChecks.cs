using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreserveChecks
{
    private const string DataId = "3ca50b9d-9c52-49c8-884f-f4409a4489c9";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] cards, int junkId, int spellId)
    {
        CombatContext context = source.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        var unit = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == StewardId);
        if (unit != null) return unit;
        for (int index = 0; index < 2; index++)
            if (context.Statistics!.Value(cards[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int selected = index == 0 ? junkId : spellId;
                return plays.FirstOrDefault(play => play.CardInstanceId == cards[index] && play.RoomIndex == 0 &&
                    play.RecursionSelections.Any(selection => selection.EffectIndex == 0 && selection.CardInstanceIds.SequenceEqual(new[] { selected })));
            }
        var spell = plays.FirstOrDefault(play => play.CardInstanceId == spellId && play.RoomIndex == 0);
        if (spell != null) return spell;
        if (context.Statistics!.Value(cards[2], "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == cards[2] && play.RoomIndex == 0 && play.RecursionSelections.Count == 0);
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("PreservePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Preserve witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int junkId = root.GetProperty("PreserveJunkCardId").GetInt32(), spellId = root.GetProperty("PreserveSpellCardId").GetInt32();
        var filterContext = root.GetProperty("PreserveFilterContext").Deserialize<CombatContext>()!;
        int filterCount = 0;
        foreach (FixtureValue test in root.GetProperty("PreserveFilters").EnumerateArray())
        {
            int id = test.GetProperty("CardId").GetInt32(), source = test.GetProperty("SourceId").GetInt32();
            IReadOnlyList<int> eligible = CardFreezeModel.EligibleCards(filterContext, source, out string? error);
            Require(error == null && !eligible.Contains(id) == test.GetProperty("Rejected").GetBoolean(), "Native freeze filter differs.");
            filterCount++;
        }
        Require(filterCount == 4, "Missing native ordinary/Junk/self/frozen filters.");
        int positive = 0, empty = 0;
        foreach (FixtureValue row in root.GetProperty("PreserveRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native freeze effect.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!, after = row.GetProperty("After").Deserialize<CombatContext>()!;
            int source = row.GetProperty("SourceId").GetInt32();
            int? selected = row.GetProperty("SelectedId").Deserialize<int?>();
            Require(before.FindCard(source)!.DataId == DataId, "Freeze source is not the original Preserve.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = CardFreezeModel.Apply(before, source, selected);
                Require(result.Supported, result.UnsupportedReason ?? "Chosen freeze unsupported.");
                Equal(result.Context, after, "native freeze complete context/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Chosen freeze mutated its parent.");
            Require(!CardFreezeModel.Apply(before, source, source).Supported && !CardFreezeModel.Apply(before, source, int.MaxValue).Supported,
                "Chosen freeze accepted its source or an absent card.");
            if (selected.HasValue)
            {
                Require(selected == junkId || selected == spellId, "Unexpected native freeze choice.");
                Require(!Frozen(before, selected.Value) && Frozen(after, selected.Value), "Selected card did not acquire temporary Freeze.");
                Require(!CardFreezeModel.Apply(before, source, null).Supported && !CardFreezeModel.Apply(after, source, selected).Supported,
                    "Chosen freeze accepted a missing choice or refroze an already frozen card.");
                positive++;
            }
            else { Equal(before, after, "No eligible cards must produce an unchanged legal effect"); empty++; }
        }
        Require(positive == 2 && empty == 1, "Missing two selections or the empty eligible-hand no-op.");
        bool thawed = false;
        int originals = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            if (before.FindCard(id)!.DataId == DataId) originals++;
            if (id != spellId) continue;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            Require(Frozen(before, id) && !Frozen(after, id), "Playing the selected frozen spell did not clear temporary Freeze."); thawed = true;
        }
        Require(originals == 3 && thawed, "Missing three original casts or the thaw-on-play control.");
        int retained = 0;
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            if (!before.Cards.Hand.Any(card => card.InstanceId == junkId) || !after.Cards.Hand.Any(card => card.InstanceId == junkId)) continue;
            Require(Frozen(before, junkId) && Frozen(after, junkId), "Retained Junk lost Freeze across EndTurn."); retained++;
        }
        Require(retained > 0, "Frozen Junk was not retained across a native turn.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("PRESERVE PASS: three original casts, Junk/spell selections, self/frozen filters, legal no-op, thaw-on-play, retained frozen Junk, full context/RNG and 16 immutable branches.");
    }
    private static bool Frozen(CombatContext context, int id) => CardBranchMaskModel.Resolve(context.FindCard(id)!).Traits.Contains("CardTraitFreeze");
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
