using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TacticalShiftChecks
{
    internal static void LethalRestoration(FixtureValue fixture)
    {
        FixtureValue[] rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var root = rows[9].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var ability = rows[9].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var restore = rows[10].GetProperty("Action").Deserialize<PlayCardAction>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        CombatUnit champion = root.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == ability.ActivatorUnitId);
        Require(root.Spawn.Turn == 1 && champion.Modifiers?.IsChampion == true && champion.Health == 45 &&
            Front(root, ability.RoomIndex) == restore.TargetUnitId,
            "Missing native champion-behind-tank lethal restoration control.");
        BattleActionResult shifted = UnitAbilityModel.Activate(root, ability);
        Require(shifted.Supported && keys.Key(shifted.State!) == keys.Key(rows[9].GetProperty("Actual").Deserialize<BattleTurnState>()!) &&
            shifted.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == champion.Id).Health == 15,
            "First equipment shift must match the complete native successor/RNG and leave fifteen health.");
        BattleActionResult restored = BattleActionModel.PlayCard(shifted.State!, restore);
        Require(restored.Supported && keys.Key(restored.State!) == keys.Key(rows[10].GetProperty("Actual").Deserialize<BattleTurnState>()!) &&
            Front(restored.State!, ability.RoomIndex) == restore.TargetUnitId &&
            !restored.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == champion.Id && unit.Health > 0),
            "Restoration must match the native lethal successor despite putting the tank back in front.");
        for (int floor = 0; floor < 3; floor++)
        {
            SupportedActionCandidate? chosen = BattleTacticalPolicy.Choose(root, out var omitted, floor);
            Require(chosen != null && chosen.Action.ActivatorUnitId != champion.Id && omitted.Count == 0 &&
                chosen.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == champion.Id && unit.Health > 0),
                "Tank restoration must not justify a champion ability whose follow-up kills its champion.");
        }
        Require(UnitAbilityModel.EnumerateActivations(root).SupportedCandidates.Any(candidate =>
            candidate.Action.ActivatorUnitId == champion.Id) && keys.Key(root) == parent,
            "The optional seed must keep the legal ability in full search and preserve the parent.");
        Console.WriteLine("TACTICAL-SHIFT-SAFETY PASS: native 45/15/dead counterexample and complete successor/RNG, three deployment floors avoid fatal restoration; legal search branch and immutable parent preserved.");
    }

    internal static void Run(FixtureValue fixture)
    {
        FixtureValue row = fixture.GetProperty("RollingDecisions").EnumerateArray().Last(record =>
        {
            if (record.GetProperty("Action").ValueKind == FixtureKind.Null) return false;
            var action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var state = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
            return state.Spawn.Turn == 8 && action.ActivatorUnitId != 0 && state.Spawn.Train.Rooms
                .SelectMany(room => room.Units).Any(unit => unit.Id == action.ActivatorUnitId && unit.Modifiers?.IsChampion == true);
        });
        var root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var expected = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        RoomCombatState room = root.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == expected.ActivatorUnitId));
        CombatUnit tank = room.Units.First(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0);
        Require(tank.Id != expected.ActivatorUnitId && room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss == true),
            "Missing natural champion-behind-tank boss control.");
        var chosen = BattleTacticalPolicy.Choose(root, out var omitted, 0);
        Require(chosen?.Action.ActivatorUnitId == expected.ActivatorUnitId && omitted.Count == 0,
            "Tactical policy skipped the legal champion shift before restoring its tank.");
        BattleTurnState shifted = chosen!.Result.State!;
        Require(Front(shifted, room.RoomIndex) == expected.ActivatorUnitId,
            "The ability must actually expose its champion in this control.");
        var restore = BattleTacticalPolicy.Choose(shifted, out omitted, 0);
        Require(restore != null && restore.Action.TargetUnitId == tank.Id && omitted.Count == 0 &&
            Front(restore.Result.State!, room.RoomIndex) == tank.Id,
            "The next legal card must restore the original tank.");
        var noEnergyJson = JsonNode.Parse(JsonSerializer.Serialize(root, ModelJson.Options))!;
        noEnergyJson["Energy"] = 0;
        noEnergyJson["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["Energy"] = 0;
        var noEnergy = noEnergyJson.Deserialize<BattleTurnState>(ModelJson.Options)!;
        string noEnergyParent = keys.Key(noEnergy);
        var freeAbility = UnitAbilityModel.Activate(noEnergy, expected);
        Require(freeAbility.Supported, "The no-energy control must keep the free ability legal: " + freeAbility.Reason);
        var unavailable = BattleTacticalPolicy.Choose(noEnergy, out omitted, 0);
        Require(unavailable?.Action.ActivatorUnitId != expected.ActivatorUnitId && omitted.Count == 0 && keys.Key(noEnergy) == noEnergyParent,
            "An unaffordable restoration must not bypass champion protection.");
        var noRestoreJson = JsonNode.Parse(JsonSerializer.Serialize(root, ModelJson.Options))!;
        var restoreDataIds = root.PlayRules!.Cards.Where(rule => rule.Effects.Any(effect => effect.Type == "FloorRearrange"))
            .Select(rule => rule.DataId).ToHashSet();
        var cards = noRestoreJson["Spawn"]!["Train"]!["Context"]!["Cards"]!;
        var hand = cards["Hand"]!.AsArray();
        for (int index = hand.Count - 1; index >= 0; index--)
            if (restoreDataIds.Contains(hand[index]!["DataId"]!.GetValue<string>()))
            {
                JsonNode card = hand[index]!;
                hand.RemoveAt(index);
                cards["Discard"]!.AsArray().Add(card);
            }
        var noRestore = noRestoreJson.Deserialize<BattleTurnState>(ModelJson.Options)!;
        string noRestoreParent = keys.Key(noRestore);
        freeAbility = UnitAbilityModel.Activate(noRestore, expected);
        Require(freeAbility.Supported, "The missing-card control must keep the free ability legal: " + freeAbility.Reason);
        unavailable = BattleTacticalPolicy.Choose(noRestore, out omitted, 0);
        Require(unavailable?.Action.ActivatorUnitId != expected.ActivatorUnitId && omitted.Count == 0 && keys.Key(noRestore) == noRestoreParent,
            "Missing restoration must retain champion protection.");
        var plan = BattlePlanner.Plan(root, keys.Key,
            new BattlePlannerOptions(16, 1, 8, 12, TimeSpan.FromSeconds(30), 0, useTacticalSeed: true));
        Require(plan.CompletedHorizon && plan.Outcome == RoomOutcome.BattleWon && plan.UnsupportedAttempts.Count == 0,
            "The bounded shift continuation did not win.");
        BattleTurnState replayed = BattlePlannerChecks.Replay(root, plan.Steps, out RoomOutcome outcome);
        using var independent = new BattleStateKeys();
        Require(outcome == RoomOutcome.BattleWon && independent.Key(replayed) == independent.Key(plan.State) &&
            independent.Key(root) == parent, "Winning shift continuation changed its root or failed full-state/RNG replay.");
        var deployment = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string deploymentParent = independent.Key(deployment);
        var bounded = BattlePlanner.Plan(deployment, keys.Key,
            new BattlePlannerOptions(56, 12, 8, 12, TimeSpan.FromSeconds(30), 0, useTacticalSeed: true, allowShorterHorizon: true));
        Require(bounded.CompletedHorizon && bounded.Outcome == RoomOutcome.BattleWon && bounded.ExpandedNodes <= 56 &&
            bounded.UnsupportedAttempts.Count == 0, "Long-window fallback exhausted the node budget before the known complete win.");
        replayed = BattlePlannerChecks.Replay(deployment, bounded.Steps, out outcome);
        Require(outcome == RoomOutcome.BattleWon && independent.Key(replayed) == independent.Key(bounded.State) &&
            independent.Key(deployment) == deploymentParent, "Whole-battle bounded win failed immutable full-state/RNG replay.");
        var shorter = BattlePlanner.Plan(deployment, keys.Key,
            new BattlePlannerOptions(2, 12, 8, 12, TimeSpan.FromSeconds(30), 0, useTacticalSeed: true, allowShorterHorizon: true));
        Require(shorter.ReachedNodeLimit && shorter.UsedShorterHorizon && !shorter.CompletedHorizon &&
            shorter.TurnsCompleted == 1 && shorter.Steps.Count == 2 && !shorter.Steps[0].IsEndTurn && shorter.Steps[1].IsEndTurn,
            "A tiny budget must retain an honestly labeled complete deployment turn.");
        replayed = BattlePlannerChecks.Replay(deployment, shorter.Steps, out outcome);
        Require(outcome == shorter.Outcome && independent.Key(replayed) == independent.Key(shorter.State) &&
            independent.Key(deployment) == deploymentParent, "Short fallback failed immutable full-state/RNG replay.");
        Console.WriteLine($"TACTICAL-SHIFT PASS: ability then tank restoration; unaffordable/missing-card controls; immutable winning full-state/RNG replay in {plan.ExpandedNodes} nodes.");
        Console.WriteLine($"TACTICAL-BUDGET PASS: whole-battle win in {bounded.ExpandedNodes}/56 nodes; honest one-turn fallback in 2 nodes.");
    }

    private static int? Front(BattleTurnState state, int room) => state.Spawn.Train.Rooms.Single(item => item.RoomIndex == room)
        .Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)?.Id;
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
