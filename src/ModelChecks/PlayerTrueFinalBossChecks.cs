using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerTrueFinalBossChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    internal static void Pair(FixtureValue source, FixtureValue replay, bool ordinaryAlly = false)
    {
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source);
        Require(source.GetProperty("PlayerAdvisor").GetProperty("InteractionsPassed").GetBoolean(),
            "Player true-final-boss overlay must pass native pointer, pause/resume and refresh interactions.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue player = fixture.GetProperty("PlayerAdvisor");
            FixtureValue overlay = player.GetProperty("Overlay");
            FixtureValue route = fixture.GetProperty("OriginalTrueFinalBossRoute");
            Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
                overlay.GetProperty("Failures").GetInt32() == 0 && overlay.GetProperty("Horizon").GetInt32() == 3 &&
                overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 && overlay.GetProperty("TacticalSeed").GetBoolean(),
                "True-final-boss acceptance requires the actual bounded player assembly and pure captures.");
            Require(route.GetProperty("Kind").GetString() == "NativeCheatJumpToTFB" &&
                route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
                route.GetProperty("DistanceAfter").GetInt32() == 8 && route.GetProperty("PyreHealth").GetInt32() == 80 &&
                route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
                route.GetProperty("StartingDeckUnchanged").GetBoolean() == !ordinaryAlly,
                "Missing the explicit original route selection and unchanged assets/Pyre with the declared deck control.");
            bool added = fixture.TryGetProperty("TrueFinalBossOrdinaryAllyControl", out FixtureValue addition) && addition.ValueKind != FixtureKind.Null;
            Require(added == ordinaryAlly && (!added || addition.GetProperty("Kind").GetString() == "Warrior" &&
                addition.GetProperty("CardDataId").GetString() == "5de24977-6800-4015-a20e-9949e79e057c"),
                "The ordinary player route must contain only its explicitly added original Warrior card.");
            foreach (string field in new[] { "TrueFinalBossLethalControl", "TrueFinalBossRelentlessControl", "TrueFinalBossDormantControl", "TrueFinalBossAllyUpgradeControl" })
                Require(!fixture.TryGetProperty(field, out FixtureValue control) || control.ValueKind == FixtureKind.Null,
                    "Player acceptance must not contain health or phase controls.");
        }
        FixtureValue[] records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        BattleTurnState deployment = records[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        TrueFinalBossSpawnDefinition? queued = deployment.Spawn.QueuedTrueFinalBoss;
        Require(queued?.Definitions.Count == 5 && queued.ExternalInteractions.Count == 0 &&
            deployment.Spawn.Train.Rooms.SelectMany(room => room.Units).Single().IsPyre &&
            deployment.Spawn.Train.Rooms.SelectMany(room => room.Units).Single().Health == 80,
            "Player capture omitted the five-body queued birth at deployment.");
        BattleTurnState[] states = records.SelectMany(record => new[] { "Root", "Actual" }
            .Select(field => record.GetProperty(field).Deserialize<BattleTurnState>()!)).ToArray();
        BattleTurnState[] live = states.Where(state => state.Spawn.TrueFinalBoss != null).ToArray();
        Require(live.Length > 0 && live.All(state => state.Spawn.TrueFinalBoss is { HellforgedBoss: true, WaitedOnRelentless: false, QueuedBossCount: 0 } &&
            state.Spawn.OuterBoss?.AssetKey == "Boss_Level9_Entropy_Top_V2" && state.Spawn.OuterBoss.Phase == "Casting" &&
            state.Spawn.Train.Context!.LifeLinks?.Actors.Count == 5), "Player capture omitted the casting controller or five live health owners.");
        BattleTurnState born = live.First();
        Require(born.Spawn.Train.Context!.LifeLinks!.Actors.Select(actor => actor.Health).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }),
            "Player route changed original true-final-boss starting HP.");
        Require(records.Any(record => record.GetProperty("Nodes").GetInt32() > 0) &&
            records.Any(record => !record.GetProperty("IsEndTurn").GetBoolean()) &&
            source.GetProperty("Turns").GetArrayLength() > 1 && states.Any(state => state.Spawn.Train.Context!.CardInstances!.Any(card =>
                card.MaskDescriptor?.SourceTraits != null && state.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.AssetKey == "TFB_EntropyScourge"))),
            "Missing real search, card play, continued combat or original generated Scourge metadata.");
        if (ordinaryAlly)
            Require(states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                unit.AssetKey == "BasicDemon" && unit.BaseAttack == 15 && unit.MaxHealth == 8 && unit.Status("multistrike")?.Stacks == 1 &&
                TrueFinalBossCombatModel.SupportsAlly(unit, state.Spawn.Train.Context))),
                "Real player advice must actually summon the added ordinary Warrior with complete source metadata.");
        Console.WriteLine($"PLAYER-TRUE-FINAL-BOSS PASS: {records.Length} real player inputs and fresh frozen replay; complete forecasts/state/RNG, original queued birth, five-body casting/link state and generated Scourges. Controlled route{(ordinaryAlly ? " and original ordinary Warrior" : "")}; nativeWon={source.GetProperty("NativeWon").GetBoolean()}.");
    }
}
