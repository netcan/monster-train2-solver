using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerTrueFinalBossChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    internal static void Pair(FixtureValue source, FixtureValue replay, bool ordinaryAlly = false, bool upgradedAlly = false, bool triggeredAlly = false)
    {
        Require(!triggeredAlly || ordinaryAlly, "Triggered player ally requires its declared original source profile.");
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
            Require(added == ordinaryAlly && (!added || addition.GetProperty("Kind").GetString() == (triggeredAlly ? "Avenger" : "Warrior") &&
                addition.GetProperty("CardDataId").GetString() == (triggeredAlly ? "69fcbe85-a5b7-4a83-bf6d-21888604d99e" : "5de24977-6800-4015-a20e-9949e79e057c")),
                "The player route must contain only its explicitly declared original allied card.");
            if (added)
            {
                string upgrades = addition.TryGetProperty("UpgradeKind", out FixtureValue kind) ? kind.GetString()! : "";
                Require(upgrades == (upgradedAlly ? "Both" : ""), "Player ordinary ally upgrade control differs.");
                BattleTurnState prepared = addition.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CardInstanceState card = prepared.Spawn.Train.Context!.FindCard(addition.GetProperty("CardId").GetInt32())!;
                Require(card.Permanent.Upgrades.Count == (upgradedAlly ? 2 : 0) &&
                    (!upgradedAlly || card.Permanent.Upgrades[0].DataId == "32c1b067-d47d-4841-b166-1a227378a937" && card.Permanent.Upgrades[0].Stats.Health == 20 &&
                        card.Permanent.Upgrades[1].DataId == "1f0a08f2-ab63-442e-a089-a1063bf7e7aa" && card.Permanent.Upgrades[1].Stats.Damage == 10),
                    "The player ally must retain exactly its declared original permanent upgrades.");
            }
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
        if (triggeredAlly)
        {
            int cardId = source.GetProperty("TrueFinalBossOrdinaryAllyControl").GetProperty("CardId").GetInt32();
            BattleTurnState? deployed = states.FirstOrDefault(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                unit.SpawnerCardId == cardId && unit.AssetKey == "MonsterAvenger" && unit.BaseAttack == (upgradedAlly ? 30 : 20) && unit.MaxHealth == (upgradedAlly ? 60 : 40) &&
                unit.Triggers.Count == 1 && TrueFinalBossCombatModel.SupportsAlly(unit, state.Spawn.Train.Context)));
            Require(deployed != null, "Real player advice must actually summon the original OnHit ally with complete source metadata.");
            CombatUnit ally = deployed!.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
            TrueFinalBossOrdinaryAllyChecks.VerifyOnHit(source, ally, deployed.Spawn.Train.Context!);
        }
        else if (ordinaryAlly)
            Require(states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                unit.AssetKey == "BasicDemon" && unit.BaseAttack == (upgradedAlly ? 25 : 15) && unit.MaxHealth == (upgradedAlly ? 28 : 8) && unit.Status("multistrike")?.Stacks == 1 &&
                TrueFinalBossCombatModel.SupportsAlly(unit, state.Spawn.Train.Context))),
                "Real player advice must actually summon the added ordinary Warrior with complete source metadata.");
        Console.WriteLine($"PLAYER-TRUE-FINAL-BOSS PASS: {records.Length} real player inputs and fresh frozen replay; complete forecasts/state/RNG, original queued birth, five-body casting/link state and generated Scourges. Controlled route{(triggeredAlly ? " and original OnHit Avenger" : ordinaryAlly ? " and original ordinary Warrior" : "")}{(upgradedAlly ? " with two numeric upgrades" : "")}; nativeWon={source.GetProperty("NativeWon").GetBoolean()}.");
    }
}
