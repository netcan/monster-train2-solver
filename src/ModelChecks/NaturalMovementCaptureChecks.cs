using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NaturalMovementCaptureChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Expected ordinary settled capture without a movement scenario.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Nonzero " + counter);
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var states = actions.Concat(root.GetProperty("Turns").EnumerateArray())
            .SelectMany(row => new[] { row.GetProperty("Before"), row.GetProperty("Actual") })
            .Select(value => value.Deserialize<BattleTurnState>()!).ToArray();
        BattleTurnState first = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(first.CanonicalPhysicalReferences && first.Spawn.Train.Context?.SpawnPoints != null,
            "Missing native physical decision references.");
        var definitions = first.Spawn.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units)
            .Concat(first.Spawn.Treasures).ToArray();
        Require(definitions.Length > 0 && definitions.All(definition => definition.Unit.BumpRules != null &&
            definition.Unit.BumpRules.Loops == definition.Loops && definition.Unit.BumpRules.CompanionBoss == definition.CompanionBoss),
            "Future native births lost their movement properties.");
        var summons = first.PlayRules!.Cards.Where(rule => rule.SpawnUnit != null).ToArray();
        Require(summons.Length > 0 && summons.All(rule => rule.SpawnUnit!.BumpRules != null),
            "Native deployment definitions lost their movement properties.");
        int actors = 0;
        foreach (var state in states)
        foreach (var unit in state.Spawn.Train.Rooms.SelectMany(room => room.Units))
        {
            Require(unit.BumpRules != null, "Native actor lost movement properties: " + unit.Id);
            actors++;
        }
        const string demonize = "f21a04ea-2d1d-4f40-a36b-da4e9239b808";
        int nativeMoves = 0;
        foreach (var row in actions)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId != demonize) continue;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var origin = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId));
            var destination = after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.TargetUnitId));
            var oldActor = origin.Units.Single(unit => unit.Id == action.TargetUnitId);
            var movedActor = destination.Units.Single(unit => unit.Id == action.TargetUnitId);
            Require(destination.RoomIndex == origin.RoomIndex - 1 &&
                movedActor.Status("melee weakness")?.Stacks == (oldActor.Status("melee weakness")?.Stacks ?? 0) + 2,
                "Natural Demonize missed its native descent or chained weakness.");
            nativeMoves++;
        }
        Require(nativeMoves > 0, "Missing actual original Demonize play outside a movement scenario.");
        foreach (var state in states)
        {
            var card = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(token => token.DataId == demonize);
            if (card == null || state.Energy < 3) continue;
            using var keys = new BattleStateKeys();
            string parent = keys.Key(state);
            var frontier = BattleActionModel.EnumerateActions(state, card.InstanceId, includeAbilities: false);
            Require(frontier.UnsupportedAttempts.Count == 0, "Natural movement frontier remains unsupported: " +
                string.Join("; ", frontier.UnsupportedAttempts.Select(item => item.Reason)));
            var targets = frontier.SupportedCandidates.Select(candidate => candidate.Action.TargetUnitId).ToHashSet();
            var units = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => targets.Contains(unit.Id)).ToArray();
            if (!units.Any(unit => unit.Team == CombatTeam.Player) || !units.Any(unit => unit.Team == CombatTeam.Enemy)) continue;
            using var independent = new BattleStateKeys();
            Require(independent.Key(state) == parent, "Movement enumeration changed its native parent.");
            Console.WriteLine($"NATURAL-MOVEMENT-CAPTURE PASS: {actors} actor snapshots, {definitions.Length} future enemy definitions, {summons.Length} deployment definitions; {nativeMoves} actual original Demonize descents; {targets.Count} legal targets across both teams, no unsupported frontier; alternative model branches require native replay.");
            return;
        }
        throw new InvalidOperationException("No affordable natural Demonize frontier across both teams.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
