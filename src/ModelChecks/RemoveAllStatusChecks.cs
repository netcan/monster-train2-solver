using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RemoveAllStatusChecks
{
    private const string CardId = "974f9021-d1e3-4b8b-b348-d737a1868ed1";
    private static readonly string[] Categories = ["Positive", "Negative", "Persistent", "Ability"];

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 164 &&
            fixture.GetProperty("ModifierScenario").GetString() == "remove-all-status-effects" &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 && fixture.GetProperty("Pending").GetInt32() == 0,
            "The remove-all-status native battle is incomplete or contains model failures.");
        FixtureValue[] records = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(records.Length == 2, $"Expected two marked ResinRemoval plays, found {records.Length}.");
        var observedModes = new HashSet<bool>();
        int fullVisibleRemovals = 0, halfVisibleRemovals = 0, preservedPersistent = 0, preservedHidden = 0;
        foreach (FixtureValue record in records)
        {
            BattleTurnState before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.CardInstances!.Single(value => value.InstanceId == action.CardInstanceId);
            Require(card.DataId == CardId, "A marked remove-all-status action is not the native ResinRemoval card.");
            CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == CardId);
            CardActionEffect[] effects = rule.Effects.ToArray();
            Require(effects.Length == 2 && effects.All(effect => effect.Type == "RemoveAllStatusEffects" &&
                effect.Statuses.Count == 0 && effect.RemoveHalfStatusEffects.HasValue &&
                (uint)effect.Value < (uint)Categories.Length), "Native ResinRemoval effect definitions are incomplete.");
            bool half = effects.All(effect => effect.RemoveHalfStatusEffects == true);
            Require(effects.All(effect => effect.RemoveHalfStatusEffects == half), "ResinRemoval mixed full and half removal parameters.");
            observedModes.Add(half);

            BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
            Require(predicted.Supported, "Remove-all-status action was unsupported: " + predicted.Reason);
            TrainSpellResult spell = CardSpellModel.Apply(before.Spawn.Train, action.RoomIndex, effects,
                action.TargetUnitId, action.CardInstanceId, before.PlayRules);
            Require(spell.Supported && spell.TargetCollections.Count == effects.Length,
                "Remove-all-status target collection was unsupported: " + spell.UnsupportedReason);
            bool terminal = predicted.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(predicted.Outcome == (RoomOutcome)record.GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(predicted.State!, terminal) == BattleTurnChecks.Comparable(actual, terminal),
                "Remove-all-status replay differs from the native action snapshot.");

            RoomCombatState roomBefore = before.Spawn.Train.Rooms.Single(value => value.RoomIndex == action.RoomIndex);
            RoomCombatState roomAfter = actual.Spawn.Train.Rooms.Single(value => value.RoomIndex == action.RoomIndex);
            foreach (CombatUnit unit in roomBefore.Units)
            {
                CombatUnit after = roomAfter.Units.SingleOrDefault(value => value.Id == unit.Id) ??
                    throw new InvalidOperationException("Remove-all-status spell unexpectedly removed a room unit.");
                foreach (CombatStatus status in unit.Statuses)
                {
                    bool selected = status.Hidden == false && effects.Select((effect, index) => (effect, index)).Any(item =>
                        spell.TargetCollections[item.index].UnitIds.Contains(unit.Id) &&
                        Categories[item.effect.Value] == status.DisplayCategory);
                    int expected = !selected ? status.Stacks : half ? status.Stacks / 2 : 0;
                    int observed = after.Statuses.FirstOrDefault(value => value.Id == status.Id)?.Stacks ?? 0;
                    Require(observed == expected, $"Status {status.Id} on unit {unit.Id} expected {expected} stacks after {(half ? "half" : "full")} removal, found {observed}.");
                    if (selected && !half && observed == 0) fullVisibleRemovals++;
                    if (selected && half && observed < status.Stacks) halfVisibleRemovals++;
                    if (status.DisplayCategory == "Persistent" && observed == status.Stacks) preservedPersistent++;
                    if (status.Hidden == true && observed == status.Stacks) preservedHidden++;
                }
            }
        }
        Require(observedModes.SetEquals([false, true]) && fullVisibleRemovals > 0 && halfVisibleRemovals > 0 &&
            preservedPersistent > 0 && preservedHidden > 0,
            $"Native status-removal coverage incomplete: modes={string.Join(',', observedModes)}, full={fullVisibleRemovals}, half={halfVisibleRemovals}, persistent={preservedPersistent}, hidden={preservedHidden}.");
        Console.WriteLine($"NATIVE-REMOVE-ALL-STATUS-CHECKS PASS: full/half casts, {fullVisibleRemovals} fully removed and {halfVisibleRemovals} halved visible statuses, {preservedPersistent} persistent and {preservedHidden} hidden statuses preserved.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
