using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MixedConduitChecks
{
    // Independent native snapshots must show both modifiers on the very same
    // card; separate friendly/enemy rows alone do not establish composition.
    internal static int SharedHandCards(BattleTurnState state)
    {
        CombatContext context = state.Spawn.Train.Context!;
        Require(context.ConduitRetained != null && ConduitModel.Validate(context) == null,
            "Mixed Conduit omitted complete private trackers.");
        var contributors = context.MagicPower!.SelectMany(room => room.Conduit!.Units
            .Where(unit => unit.Active).Select(unit => (room.Team, Unit: unit))).ToArray();
        int shared = 0;
        foreach (var token in context.Cards.Hand)
        {
            var owners = contributors.Where(owner => owner.Unit.Cards.Contains(token.InstanceId)).ToArray();
            if (!owners.Any(owner => owner.Team == CombatTeam.Player) || !owners.Any(owner => owner.Team == CombatTeam.Enemy)) continue;
            CardInstanceState card = context.FindCard(token.InstanceId)!;
            foreach (var owner in owners)
            {
                int expected = owner.Team == CombatTeam.Player ? 3 : 4;
                Require(owner.Unit.Power == expected, "Native mixed contributor power differs.");
                var upgrade = card.Temporary.Upgrades.Single(value => value.DataId == "conduit:" + owner.Unit.UnitId);
                int amount = expected * (owner.Unit.MultipliedCards.Contains(token.InstanceId) ? CardMagicPowerModel.Multiplier(card) : 1);
                Require(upgrade.Stats.Damage == amount && upgrade.Stats.Heal == amount,
                    "One mixed modifier replaced or changed another contributor's hand upgrade.");
            }
            shared++;
        }
        return shared;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("EclipseChamberMixedConduit").GetBoolean(), "Missing mixed Conduit scene.");
        EclipseChamberChecks.EnemyConduit(root);
        int shared = 0, preserved = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            shared += SharedHandCards(before) + SharedHandCards(after);
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit? target = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            if (target?.Team != CombatTeam.Enemy || target.Status("conduit") == null) continue;
            foreach (RoomMagicPower room in before.Spawn.Train.Context!.MagicPower!.Where(room => room.Team == CombatTeam.Player))
                foreach (ConduitUnitState friendly in room.Conduit!.Units.Where(unit => unit.Active))
                    foreach (int id in friendly.Cards.Where(id => after.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id)))
                    {
                        var prior = before.Spawn.Train.Context.FindCard(id)!.Temporary.Upgrades.Single(upgrade => upgrade.DataId == "conduit:" + friendly.UnitId);
                        var current = after.Spawn.Train.Context!.FindCard(id)!.Temporary.Upgrades.Single(upgrade => upgrade.DataId == prior.DataId);
                        Require(prior.Stats.Damage == current.Stats.Damage && prior.Stats.Heal == current.Stats.Heal,
                            "Opposing movement cleared a friendly contributor's upgrade.");
                        preserved++;
                    }
        }
        Require(shared > 0 && preserved > 0, "Missing simultaneous card upgrades or friendly preservation after opposing movement.");
        Console.WriteLine($"MIXED-CONDUIT PASS sharedHandCardSnapshots={shared} preservedFriendlyUpgrades={preserved} completeImmutableBattle=true");
    }

    internal static void PlayerPair(FixtureValue source, FixtureValue replay)
    {
        Require(source.GetProperty("EclipseChamberMixedConduit").GetBoolean() && replay.GetProperty("EclipseChamberMixedConduit").GetBoolean(),
            "Missing mixed Conduit player pair.");
        PlayerRoomMoonChecks.EnemyConduitPair(source, replay);
        int shared = 0;
        foreach (FixtureValue fixture in new[] { source, replay })
            foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
                foreach (string boundary in new[] { "Root", "Actual" })
                    shared += SharedHandCards(row.GetProperty(boundary).Deserialize<BattleTurnState>()!);
        Require(shared > 0, "Player pair never tracked the same hand card on both teams.");
        Console.WriteLine($"PLAYER-MIXED-CONDUIT PASS sharedHandCardSnapshots={shared} exactFrozenReplay=true");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
