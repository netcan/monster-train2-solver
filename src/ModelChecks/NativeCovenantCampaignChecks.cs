using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NativeCovenantCampaignChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay, int level, int distance)
        => Pair(source, replay, level, distance, requireVictory: true);

    internal static void AngelDuoPair(FixtureValue source, FixtureValue replay)
    {
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue input = fixture.GetProperty("NativeRunInput");
            Require(input.GetProperty("Scenario").GetString() == "Level6AngelDuo_RallyAttackIncantHealth" &&
                input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("StartingLevel").GetInt32() == 2 &&
                fixture.GetProperty("AngelDuoDeathTransition").ValueKind == FixtureKind.Null &&
                fixture.GetProperty("AngelDuoRelentlessEntry").ValueKind == FixtureKind.Null,
                "Player Angel Duo evidence requires the unmodified original encounter without native death/phase controls.");
            var initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
            var born = fixture.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit[] companions = born.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.BumpRules?.CompanionBoss == true).ToArray();
            Require(initial.Spawn.QueuedOuterBoss?.Actor.Unit is { Health: 1250, BaseAttack: 11 } outer &&
                OuterBossDefinitions.IsAngelDuo(outer.AssetKey) &&
                born.Spawn.Turn == 1 && born.Spawn.QueuedOuterBoss == null && companions.Length == 2 &&
                companions.All(unit => !unit.EndsBattleOnDeath && companions.Any(partner =>
                    partner.Id != unit.Id && partner.Id == unit.BumpRules!.PairedBossUnitId)) &&
                companions.Any(unit => unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" &&
                    unit.Health == 1350 && unit.MaxHealth == 1350 && unit.BaseAttack == 16) &&
                companions.Any(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey) &&
                    unit.Health == 1500 && unit.MaxHealth == 1500 && unit.BaseAttack == 14) &&
                initial.Spawn.Train.Context!.QueryFrame!.ForcedRelentless!.RemainingTurns == 0,
                "Player Angel Duo input changed its original queued definition, paired native rank-two births or phase counters.");
        }
        Pair(source, replay, 2, 5, requireVictory: false);
        Console.WriteLine("PLAYER-ANGEL-DUO PASS: original five-win native checkpoint, unmodified paired bosses, actual advice/UI and independent full-state/RNG replay; outcome=" +
            (source.GetProperty("NativeWon").GetBoolean() ? "victory" : "bounded loss") + ".");
    }

    internal static void AngelDuoVariantPair(FixtureValue source, FixtureValue replay, string variant = "LoopResolve")
    {
        string asset = variant == "LoopResolve" ? "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" :
            variant == "RevengeHarvest" ? "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" :
                throw new InvalidOperationException("Unknown selected Angel Duo player variant.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            AngelDuoVariantChecks.Input(fixture, variant);
            var initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
            Require(initial.Spawn.QueuedOuterBoss?.Actor.Unit is { Health: 1250, BaseAttack: 11 } outer &&
                outer.AssetKey == asset &&
                initial.Spawn.Train.Context!.QueryFrame!.ForcedRelentless!.RemainingTurns == 0,
                "Selected Angel Duo player input changed original queued health, attack or turn counters.");
        }
        Pair(source, replay, 2, 5, requireVictory: false, selectedAngelDuo: true);
        Console.WriteLine($"PLAYER-ANGEL-DUO-{variant} PASS: explicit original variant selection, prior five-win native history, original bosses, actual player advice/UI and independent full-state/RNG replay.");
    }

    private static void Pair(FixtureValue source, FixtureValue replay, int level, int distance, bool requireVictory, bool selectedAngelDuo = false)
    {
        Require(level is >= 1 and <= 10 && distance is >= 1 and <= 7,
            "Natural cumulative campaign acceptance requires a declared rank and progressed ordinary battle.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            Require((!requireVictory || fixture.GetProperty("NativeWon").GetBoolean()) &&
                fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null &&
                fixture.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null &&
                (selectedAngelDuo || fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null),
                "Natural campaign evidence requires the declared outcome and actual player capture from an unmodified native checkpoint.");
            Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
                fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
                "Natural campaign terminal effects did not settle at the native boundary.");
            FixtureValue input = fixture.GetProperty("NativeRunInput");
            if (input.TryGetProperty("AscensionLevel", out FixtureValue actualLevel))
                Require(actualLevel.GetInt32() == level && input.GetProperty("StartingLevel").GetInt32() == level,
                    "Native campaign observation changed its difficulty or starting conditions.");
            BattleTurnState initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
            int pyre = initial.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
            NativeCampaignInputChecks.Native(fixture, distance, 424242, pyre, input.GetProperty("Scenario").GetString()!);
            foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
            foreach (string field in new[] { "Root", "Actual" })
            {
                BattleTurnState state = row.GetProperty(field).Deserialize<BattleTurnState>()!;
                CombatRelicState[] owners = state.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
                Require(owners.Length == level && TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(owners) &&
                    owners.Select((owner, index) => owner.AssetKey!.StartsWith($"Covenant{index + 1:D2}_", StringComparison.Ordinal)).All(match => match),
                    "Natural campaign lost an original cumulative owner or changed its calibrated effect signature/order.");
            }
            TrainCombatChecks.Native(fixture);
        }
        // The first source predates these additive read-only observations. Full
        // root/RNG identity below ties it to the newly observed native resume.
        FixtureValue resumed = replay.GetProperty("NativeRunInput");
        Require(resumed.GetProperty("AscensionLevel").GetInt32() == level &&
            resumed.GetProperty("StartingLevel").GetInt32() == level,
            "Native resume changed its actual difficulty or original starting conditions.");
        foreach (string field in new[] { "Seed", "Distance", "Scenario", "WasDevCheatUsed", "BattlesWon", "Gold", "PyreHealth", "CompletedBattles" })
            Require(source.GetProperty("NativeRunInput").GetProperty(field).ContentEquals(resumed.GetProperty(field)),
                "Fresh natural campaign replay changed the original native input: " + field);
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        Require(source.GetProperty("Actions").GetArrayLength() > 0 &&
            source.GetProperty("RollingDecisions").EnumerateArray().Any(row => row.GetProperty("Nodes").GetInt32() > 0),
            "Natural campaign source must execute card plays and actual player search.");
        Console.WriteLine($"PLAYER-NATIVE-COVENANT-CAMPAIGN PASS rank={level} ring={distance + 1}: original cumulative owners, prior native victories, player UI, full forecasts/state/RNG and independent native replay; victory={source.GetProperty("NativeWon").GetBoolean()}.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
