using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerConduitChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue player = fixture.GetProperty("PlayerAdvisor");
            Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                player.GetProperty("Version").GetString() == "0.2.3.0" && fixture.GetProperty("NativeWon").GetBoolean(),
                "Conduit acceptance requires the actual 0.2.3 player DLL and two native victories.");
        }
        // The three original relics are unchanged; carriers are authored. Keep
        // the natural-encounter checks strict for all other acceptance cases.
        RollingAdviceChecks.FrozenPair(source, replay, conduitCalibration: true);
        BattleAdviceChecks.Native(source, requireBoss: false, conduitCalibration: true);
        int contributors = 0, multiple = 0, upgradedSpells = 0, births = 0;
        foreach (FixtureValue record in source.GetProperty("RollingDecisions").EnumerateArray())
        {
            BattleTurnState root = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
            CombatContext context = root.Spawn.Train.Context!;
            Require(context.ConduitRetained != null && context.SpawnPoints != null && ConduitModel.Validate(context) == null,
                "Player capture omitted complete physical Conduit state.");
            Require(context.Relics!.Any(relic => relic.AssetKey == "Inkbound_Clepsydranium") &&
                context.Relics.Any(relic => relic.AssetKey == "EquipmentBoon_DH") &&
                context.Relics.Any(relic => relic.AssetKey == "AddConduitOnSpawn"), "Missing unchanged native Conduit owners.");
            foreach (RoomMagicPower room in context.MagicPower!)
            {
                Require(room.Conduit != null, "Incomplete room/team Conduit capture.");
                contributors += room.Conduit.Units.Count;
                if (room.Conduit.Units.Count >= 2) multiple++;
            }
            if (record.GetProperty("Action").ValueKind != FixtureKind.Null)
            {
                PlayCardAction action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState card = context.FindCard(action.CardInstanceId)!;
                if (card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ConduitUpgrade" && upgrade.Stats.Damage > 0))
                    upgradedSpells++;
            }
        }
        foreach (FixtureValue row in source.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            RelicBirthResult result = RelicBirthModel.CharacterAdded(before, row.GetProperty("UnitId").GetInt32(),
                row.GetProperty("FromCardId").GetInt32(), row.GetProperty("OnlyCovenants").GetBoolean(),
                row.GetProperty("DeferCallbacks").GetBoolean(), row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(result.Supported && ModelJson.Difference(System.Text.Json.JsonSerializer.Serialize(result.State, ModelJson.Options),
                System.Text.Json.JsonSerializer.Serialize(row.GetProperty("AfterTrain").Deserialize<TrainCombatState>(), ModelJson.Options)) == null,
                "Independent player Conduit relic birth differs.");
            births++;
        }
        Require(contributors > 0 && multiple > 0 && upgradedSpells > 0 && births > 0,
            "Missing live/multiple contributors, upgraded spell plays or native relic births.");
        Console.WriteLine($"PLAYER-CONDUIT PASS authoredCarriers=true liveContributorSnapshots={contributors} multiContributorRooms={multiple} upgradedPlays={upgradedSpells} births={births}");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
