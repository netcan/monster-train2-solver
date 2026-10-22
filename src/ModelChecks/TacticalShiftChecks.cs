using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TacticalShiftChecks
{
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
        Console.WriteLine($"TACTICAL-SHIFT PASS: ability then tank restoration; unaffordable/missing-card controls; immutable winning full-state/RNG replay in {plan.ExpandedNodes} nodes.");
    }

    private static int? Front(BattleTurnState state, int room) => state.Spawn.Train.Rooms.Single(item => item.RoomIndex == room)
        .Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)?.Id;
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
