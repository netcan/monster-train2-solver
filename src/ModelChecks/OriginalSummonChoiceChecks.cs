using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSummonChoiceChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path, bool searchOnly = false, bool repeatedSearchOnly = false)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("OriginalSummonChoicePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original summon-choice native battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        if (repeatedSearchOnly) { RepeatedSearch(root); return; }
        if (searchOnly) { SelectionSearch(root); return; }
        var definitions = root.GetProperty("OriginalSummonChoiceDefinitions");
        Require(definitions.GetProperty("Profile").GetString() == "installed-control", "Missing explicit isolated DLC input.");
        string[] upgradeIds = { "823ab526-9cfe-4383-9e17-a033d49a7fa9", "6b9e1108-af4e-468f-a912-48853b0f0413", "3de67996-0562-41ce-b892-1cedabf0bffc" };
        var upgrades = definitions.GetProperty("Upgrades").EnumerateArray().ToArray();
        Require(upgrades.Length == 3 && upgrades.Select(row => row.GetProperty("Id").GetString()).SequenceEqual(upgradeIds), "Original Flight upgrade identities changed.");
        string[] abilityIds = { "2160ff0b-3d1f-4d77-97e0-26b5df37590e", "c0d006a1-ed15-45ff-bb51-7664a8c4f11c", "24daf0a7-40fd-4f15-b6d1-60522568cad4" };
        var abilities = upgrades.Select(row => row.GetProperty("Ability").Deserialize<CardPlayRule>()!).ToArray();
        for (int rank = 0; rank < 3; rank++)
        {
            var rule = abilities[rank];
            Require(rule.DataId == abilityIds[rank] && rule.ExternalInteractions.Count == 0 && rule.Effects.Count == 2 &&
                rule.Effects[0].Type == "AbilityTeleport" && rule.Effects[1] is { Type: "SummonChoice", Target: "Room",
                    SummonChoice: { Count: 3, OnePerFloorSubtype: "SubtypesData_Darkshard", CardlessStatus.Id: "cardless" } },
                "Original teleport/choice definition changed at rank " + rank);
            Require(rule.Effects[1].SummonChoice!.Pool.Count == 3 && rule.Effects[1].SummonChoice!.Pool.All(card =>
                card.HasSpawnCharacter && card.Unit is { BaseAttack: 0, Health: 10, Size: 1 }), "Missing original fragment templates.");
        }
        var tokens = definitions.GetProperty("Tokens").EnumerateArray().Select(row => row.GetProperty("Rule").Deserialize<CardPlayRule>()!).ToArray();
        Require(tokens.Length == 9 && tokens.All(rule => rule.CostType == "NonPlayable"), "Native fragments must remain nonplayable.");
        int[] ids = root.GetProperty("OriginalSummonChoiceIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var drafts = root.GetProperty("OriginalSummonChoiceDrafts").EnumerateArray().ToArray();
        Require(ids.Length == 49 && actions.Length == 49 && ids.Count(id => id == 0) == 21 && drafts.Length == 12, "Missing original inputs/dialogs.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction Action(int index) => actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        int births = 0, overCapacity = 0;
        for (int rank = 0; rank < 3; rank++)
        {
            int start = 7 + rank * 11;
            for (int choice = 0; choice < 3; choice++)
            {
                int index = start + new[] { 0, 4, 8 }[choice]; var action = Action(index);
                var before = Before(index); var actual = After(index); var rule = abilities[rank].Effects[1].SummonChoice!;
                string expectedId = rule.Pool[choice].CardDataId;
                Require(action.SummonSelections is [{ EffectIndex: 1 }] && action.SummonSelections[0].CardDataId == expectedId &&
                    before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId == abilityIds[rank], "Original selected skill/fragment differs.");
                var draft = drafts[rank * 3 + choice];
                Require(draft.GetProperty("Cards").Deserialize<string[]>()!.SequenceEqual(rule.Pool.Select(card => card.CardDataId)) &&
                    draft.GetProperty("Choice").GetString() == expectedId, "Actual native dialog differs from ordered pool/selection.");
                var oldIds = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
                var born = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !oldIds.Contains(unit.Id)).ToArray();
                Require(born.Length == 1 && born[0].AssetKey == rule.Pool[choice].Unit!.AssetKey && born[0].SpawnerCardId == 0 &&
                    born[0].Status("cardless")?.Stacks == 1 && born[0].IsSpawning == false && born[0].Modifiers!.SpawnerMatchesDefinition &&
                    actual.Spawn.Train.Context!.NextUnitId == before.Spawn.Train.Context!.NextUnitId + 1 &&
                    actual.Spawn.Train.Context.BattleRng.Equals(before.Spawn.Train.Context.BattleRng), "Native cardless birth/identity/RNG differs.");
                var room = actual.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == born[0].Id));
                Require(room.RoomIndex == action.RoomIndex && room.Units.Any(unit => unit.Id == action.ActivatorUnitId), "Birth must use the destination after teleport.");
                if (room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).Sum(unit => unit.Size) >
                    actual.PlayRules!.Rooms.Single(item => item.RoomIndex == room.RoomIndex).PlayerCapacity) overCapacity++;
                string parent = BattleTurnChecks.Comparable(before);
                Parallel.For(0, 16, _ => {
                    var replay = BattleActionModel.PlayCard(before, action);
                    Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual), "Independent choice branch differs.");
                });
                var missing = Copy(action, Array.Empty<SummonChoiceSelection>());
                var wrong = Copy(action, new[] { new SummonChoiceSelection(1, "not-an-offer") });
                var duplicate = Copy(action, new[] { action.SummonSelections[0], action.SummonSelections[0] });
                var misplaced = Copy(action, new[] { new SummonChoiceSelection(0, expectedId) });
                foreach (var invalid in new[] { missing, wrong, duplicate, misplaced })
                    Require(BattleActionModel.PlayCard(before, invalid).Rejection == ActionRejection.Illegal, "Invalid selection must be illegal, not a model coverage failure.");
                Require(parent == BattleTurnChecks.Comparable(before), "Choice exploration changed its parent."); births++;
            }
            int skip = start + 2; var skipped = Action(skip);
            Require(skipped.SummonSelections.Count == 0 && Before(skip).Spawn.Train.Context!.NextUnitId == After(skip).Spawn.Train.Context!.NextUnitId &&
                BattleActionModel.PlayCard(Before(skip), Copy(skipped, new[] { new SummonChoiceSelection(1, abilities[rank].Effects[1].SummonChoice!.Pool[0].CardDataId) })).Rejection == ActionRejection.Illegal,
                "Same-floor subtype must skip its dialog and reject extraneous selection.");
            int shatter = start + 9; var cast = Action(shatter); var old = Unit(Before(shatter), cast.TargetUnitId); var next = Unit(After(shatter), cast.TargetUnitId);
            Require(old.Health - next.Health == new[] { 40, 120, 300 }[rank] &&
                !After(shatter).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == cast.ActivatorUnitId && unit.Health > 0),
                "Original Shatter damage/self-sacrifice differs at rank " + rank);
            int bloodshed = start + 5; var wounded = Action(bloodshed);
            Require(Unit(After(bloodshed), wounded.TargetUnitId).BaseAttack - Unit(Before(bloodshed), wounded.TargetUnitId).BaseAttack == rank + 1,
                "Original Looming bloodshed upgrade did not execute.");
        }
        for (int rank = 0; rank < 3; rank++)
        {
            int summon = 41 + rank * 3, self = summon + 1;
            string id = abilities[rank].Effects[1].SummonChoice!.Pool[2].CardDataId;
            Require(Action(summon).SummonSelections.Single().CardDataId == id &&
                drafts[9 + rank].GetProperty("Choice").GetString() == id &&
                drafts[9 + rank].GetProperty("Cards").Deserialize<string[]>()!.SequenceEqual(abilities[rank].Effects[1].SummonChoice!.Pool.Select(card => card.CardDataId)),
                "Missing extra native self-damage birth/dialog.");
            var action = Action(self);
            Require(action.TargetUnitId == action.ActivatorUnitId && Unit(Before(self), action.TargetUnitId).Health == 10 &&
                !After(self).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == action.ActivatorUnitId && unit.Health > 0),
                "Missing lethal native self-target followed by retained Self sacrifice.");
            var replay = BattleActionModel.PlayCard(Before(self), action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(After(self)),
                "Self-sacrifice after completed damage death differs from native.");
        }
        Require(births == 9 && overCapacity == 3, "Missing all nine births and three native over-capacity births.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        SelectionSearch(root);
        Console.WriteLine("ORIGINAL-SUMMON-CHOICE PASS: three original champion upgrades/skills, twelve native dialogs/cardless births, three subtype skips/over-capacity births, Looming bloodshed, Shatter 40/120/300/self-sacrifice and three lethal self-targets, invalid inputs, full chain/16 branches and zero-unsupported serial/parallel search actively choosing a fragment.");
    }
    private static void SelectionSearch(FixtureValue root)
    {
        // Use an untouched native decision with the rank-three skill ready. This
        // proves the search itself chooses a fragment, beyond frontier membership.
        var searchRoot = root.GetProperty("Actions")[37].GetProperty("Before").Deserialize<BattleTurnState>()!; string searchParent = BattleTurnChecks.Comparable(searchRoot);
        var ability = searchRoot.PlayRules!.Cards.Single(rule => rule.DataId == "24daf0a7-40fd-4f15-b6d1-60522568cad4");
        var selected = ability.Effects[1].SummonChoice!.Pool[2];
        string shattering = selected.Unit!.AssetKey;
        double Score(BattleTurnState state, RoomOutcome _) => -state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Count(unit => unit.AssetKey == shattering && unit.Health > 0);
        var serial = BattleSearchModel.Minimize(searchRoot, Score, new BattleSearchOptions(1, 0, 1));
        var parallel = BattleSearchModel.Minimize(searchRoot, Score, new BattleSearchOptions(1, 0, 4));
        Require(serial.DecisionStatesVisited == 1 && parallel.DecisionStatesVisited == 1 &&
            serial.Steps.Count == 1 && parallel.Steps.Count == 1 && serial.Score == -1 && parallel.Score == -1 && !serial.HasUnsupportedAttempts && !parallel.HasUnsupportedAttempts &&
            serial.Steps.Any(step => step.Action?.SummonSelections.Any(selection => selection.CardDataId == selected.CardDataId) == true) &&
            JsonSerializer.Serialize(serial.Steps, ModelJson.Options) == JsonSerializer.Serialize(parallel.Steps, ModelJson.Options) &&
            BattleTurnChecks.Comparable(serial.BestState!) == BattleTurnChecks.Comparable(parallel.BestState!) &&
            searchParent == BattleTurnChecks.Comparable(searchRoot), "Serial/parallel search failed to select the original fragment deterministically.");
        var replay = BattleActionModel.PlayCard(searchRoot, serial.Steps.Single().Action!);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(serial.BestState!),
            "Returned fragment selection did not replay to its complete searched successor.");
        Console.WriteLine("SUMMON-CHOICE-SELECTION-SEARCH PASS: full native-root frontier, one expanded decision, serial/parallel identical selected original fragment, complete returned-plan replay and immutable parent; broader 128-state search belongs to the full checker.");
    }
    private static void RepeatedSearch(FixtureValue root)
    {
        var source = root.GetProperty("Actions")[37].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = BattleTurnChecks.Comparable(source);
        var ability = source.PlayRules!.Cards.Single(rule => rule.DataId == "24daf0a7-40fd-4f15-b6d1-60522568cad4");
        string fragment = ability.Effects[1].SummonChoice!.Pool[2].Unit!.AssetKey;
        double Score(BattleTurnState state, RoomOutcome _) => -state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Count(unit => unit.AssetKey == fragment && unit.Health > 0);
        BattleSearchResult Run(int workers)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var result = BattleSearchModel.Minimize(source, Score, new BattleSearchOptions(128, 0, workers));
            Console.WriteLine($"SUMMON-CHOICE-REPEATED-SEARCH workers={workers} nodes={result.DecisionStatesVisited} score={result.Score} " +
                $"unsupported={result.UnsupportedAttempts.Count} nodeLimit={result.ReachedNodeLimit} seconds={timer.Elapsed.TotalSeconds:F3} " +
                "path=" + JsonSerializer.Serialize(result.Steps, ModelJson.Options));
            foreach (var group in result.UnsupportedAttempts.GroupBy(attempt => attempt.Reason))
                Console.WriteLine("SUMMON-CHOICE-REPEATED-BOUNDARY count=" + group.Count() + " reason=" + group.Key);
            Require(result.BestState != null && result.Score <= -1 && result.DecisionStatesVisited <= 128 && !result.HasUnsupportedAttempts,
                "Repeated search lacks a supported fragment path within its budget.");
            var replayed = source;
            foreach (var step in result.Steps)
            {
                Require(!step.IsEndTurn && step.Action != null, "Zero-turn repeated search unexpectedly ended its turn.");
                var applied = BattleActionModel.PlayCard(replayed, step.Action!);
                Require(applied.Supported, "Returned repeated-summon action failed: " + applied.Reason);
                replayed = applied.State!;
            }
            Require(BattleTurnChecks.Comparable(replayed) == BattleTurnChecks.Comparable(result.BestState!) &&
                parent == BattleTurnChecks.Comparable(source), "Repeated search failed full-state replay or changed its parent.");
            return result;
        }
        var serial = Run(1); var parallel = Run(4);
        Require(serial.Score == parallel.Score && serial.Outcome == parallel.Outcome &&
            JsonSerializer.Serialize(serial.Steps, ModelJson.Options) == JsonSerializer.Serialize(parallel.Steps, ModelJson.Options) &&
            BattleTurnChecks.Comparable(serial.BestState!) == BattleTurnChecks.Comparable(parallel.BestState!),
            "Repeated-summon serial/parallel scores, paths or complete successors differ; see diagnostics above.");
        Console.WriteLine("SUMMON-CHOICE-REPEATED-SEARCH PASS: native decision root, 128-node serial/parallel identical result, full returned-plan replay, zero unsupported and immutable parent; node cutoff does not certify optimality.");
    }
    private static PlayCardAction Copy(PlayCardAction action, IReadOnlyList<SummonChoiceSelection> choices) =>
        new(action.CardInstanceId, action.RoomIndex, action.PlayerPosition, action.TargetUnitId, action.ActivatorUnitId,
            action.TargetSpawnPointIndex, action.RecursionSelections, action.MixSelections, action.ActivatorRoomIndex, action.Operation, choices);
}
