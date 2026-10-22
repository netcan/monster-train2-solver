using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerEventGraftChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        foreach (var fixture in new[] { source, replay })
        {
            var player = fixture.GetProperty("PlayerAdvisor");
            Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                fixture.GetProperty("NativeWon").GetBoolean(), "Missing actual player assembly or native victory.");
            Require(player.GetProperty("Overlay").GetProperty("PurityFailures").GetInt32() == 0 &&
                player.GetProperty("Overlay").GetProperty("Failures").GetInt32() == 0,
                "Player capture was impure or failed.");
        }
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        int attached = 0, reversedEnemyOrders = 0, retainedWins = 0;
        var decisions = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        foreach (var row in decisions)
        {
            var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (row.GetProperty("ReusedWinningContinuation").GetBoolean()) retainedWins++;
            Require(row.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon,
                "A player publication lost the verified winning forecast.");
            if (row.GetProperty("Action").ValueKind != FixtureKind.Null)
            {
                var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
                var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
                if (card.DataId == CardGraftedEquipmentModel.EventHostId)
                {
                    Require(card.MaskDescriptor?.GraftedEquipment?.Card.DataId == CardGraftedEquipmentModel.EventEquipmentId &&
                        card.GraftedEquipmentCardId > 0, "Player omitted instance-owned event graft metadata.");
                    var unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(actor => actor.SpawnerCardId == card.InstanceId);
                    var gear = after.Spawn.Train.Context!.FindCard(card.GraftedEquipmentCardId!.Value)!;
                    if (gear.EquippedUnitId == unit.Id)
                    {
                        Require(gear.MaskDescriptor!.TemporaryTraits.Any(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait &&
                            trait.GraftSevered == false) && unit.EquipmentCards!.Contains(gear.InstanceId),
                            "Player omitted the actual temporary graft attachment.");
                        attached++;
                    }
                }
            }
            foreach (var room in after.Spawn.Train.Rooms)
            {
                int[] enemies = room.Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
                if (enemies.Length > 1 && !enemies.SequenceEqual(enemies.OrderBy(id => id))) reversedEnemyOrders++;
            }
        }
        Require(attached > 0 && reversedEnemyOrders > 0 && retainedWins == decisions.Length - 1 &&
            !decisions[0].GetProperty("ReusedWinningContinuation").GetBoolean(),
            "Missing actual event graft, enemy creation/physical-order distinction or retained winning plan.");
        Console.WriteLine($"PLAYER-EVENT-GRAFT PASS attachments={attached} reversedEnemyOrders={reversedEnemyOrders} retainedWins={retainedWins}: natural winning player advice and independent complete native replay.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
