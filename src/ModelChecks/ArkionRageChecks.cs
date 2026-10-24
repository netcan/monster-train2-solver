using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ArkionRageChecks
{
    private const string Boss = "Boss_Level3_Arkion_SiphonRage";
    internal static void Input(FixtureValue fixture, bool natural = false)
    {
        FixtureValue input = fixture.GetProperty("NativeRunInput"), route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(input.GetProperty("Scenario").GetString() == "Level3BattleHeavyHitter_SiphonRage" &&
            input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 2 &&
            input.GetProperty("AscensionLevel").GetInt32() == (natural ? 3 : 2) &&
            input.GetProperty("StartingLevel").GetInt32() == (natural ? 3 : 2) &&
            input.GetProperty("BattlesWon").GetInt32() == 2 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("CompletedBattles").GetArrayLength() == 2 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            (natural ? route.ValueKind == FixtureKind.Null &&
                fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null &&
                fixture.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null :
            route.GetProperty("Kind").GetString() == "SelectOriginalEarlyOuterBossScenario" &&
            route.GetProperty("Boss").GetString() == Boss &&
            route.GetProperty("SelectedScenario").GetString() == "Level3BattleHeavyHitter_SiphonRage" &&
            route.GetProperty("Distance").GetInt32() == 2 && route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter"))) &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Arkion Rage requires its declared original two-win checkpoint and exact encounter without asset/RNG edits.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(field).GetInt32() == 0, "Arkion Rage native failure: " + field);
    }
    internal static void Birth(FixtureValue fixture, bool natural = false)
    {
        OuterBossBirthChecks.Native(fixture);
        FixtureValue row = fixture.GetProperty("Births")[0];
        var definition = row.GetProperty("Instantiation").Deserialize<OuterBossInstantiationState>()!;
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit raw = definition.Definition.Actor.Unit;
        CombatUnit born = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        Require(raw.AssetKey == Boss && raw.BaseAttack == 7 && raw.Health == 425 && raw.MaxHealth == 425 &&
            raw.Statuses.Count == 0 && raw.Triggers.Count == 0 &&
            created.BirthStats is { Attack: 0, Health: 200 } && created.BirthStats.CovenantIds.SequenceEqual(
                new[] { "d3b53481-f26b-4344-ae8e-ec80b28d6004", "92c18b99-e4c0-4953-ba2c-4815f570a66e" }
                    .Concat(natural ? new[] { "dcb31cd7-4293-4b07-b396-5456a0ce0b6e" } : Array.Empty<string>())) &&
            born.BaseAttack == 7 && born.Health == 625 && born.MaxHealth == 625 &&
            born.BumpRules is { OuterBoss: true, CompanionBoss: false, PairedBossUnitId: 0 } && born.EndsBattleOnDeath &&
            actual.Boss.Actions[0][0].Behavior == "RandomOccupiedBossActionBehavior" &&
            actual.Battle.Spawn.Train.Context!.Relics!.Any(owner => owner.AssetKey == "Enemy_Boss_Level3SiphonRage" &&
                owner.DataId == "85ee2dcc-aa59-4be3-90ad-2a1d6ecba3df"),
            "Arkion Rage birth changed original stats, Covenant owners, death routing, controller or original trigger relic.");
        Trigger(born);
        if (natural)
        {
            CombatRelicState[] owners = actual.Battle.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
            Require(TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners, Boss), "Natural Rage owner gate rejected its native input.");
            CombatRelicState merchant = owners[2];
            var changedMerchant = new CombatRelicState(merchant.DataId, merchant.AssetKey,
                merchant.EffectTypes.Append("RelicEffectAddStatusEffectOnSpawn").ToArray(), isCovenant: true);
            Require(!TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners.Take(2).Append(changedMerchant).ToArray(), Boss) &&
                !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners.Reverse().ToArray(), Boss) &&
                !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners.Append(merchant).ToArray(), Boss) &&
                !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners, "Boss_Level3_Arkion_PushAttack") &&
                !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(owners, "Boss_Level6_Cael_AtkHP"),
                "Natural Rage must not admit changed combat effects, owner order/duplicates or uncalibrated boss combinations.");
        }
        Console.WriteLine("ARKION-RAGE-BIRTH PASS: raw7/425, native7/625, exact original relic-installed self Rage2 Harvest and complete creation/placement/controller/RNG.");
    }
    internal static void Battle(FixtureValue fixture, string path, bool requireGrowth, bool natural = false)
    {
        Input(fixture, natural);
        int positive = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit? boss = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.AssetKey == Boss);
            CombatUnit? next = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == boss?.Id);
            if (boss == null || next == null) continue;
            int delta = (next.Status("buff")?.Stacks ?? 0) - (boss.Status("buff")?.Stacks ?? 0);
            if (delta <= 0) continue;
            Trigger(boss);
            Require(delta % 2 == 0 && next.BaseAttack == boss.BaseAttack,
                "Original Arkion Harvest must grant self Rage in exact increments of2 without changing base attack.");
            positive += delta / 2;
        }
        foreach (FixtureValue row in fixture.GetProperty("Stages").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Exchange") continue;
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit[] actual = row.GetProperty("Actual").GetProperty("Units").Deserialize<CombatUnit[]>()!;
            CombatUnit? boss = before.Units.SingleOrDefault(unit => unit.AssetKey == Boss);
            CombatUnit? next = actual.SingleOrDefault(unit => unit.Id == boss?.Id);
            if (boss == null || next == null) continue;
            int delta = (next.Status("buff")?.Stacks ?? 0) - (boss.Status("buff")?.Stacks ?? 0);
            if (delta <= 0) continue;
            Trigger(boss);
            int deaths = before.Units.Count(unit => unit.Team == CombatTeam.Enemy && unit.Id != boss.Id && unit.Health > 0 &&
                !actual.Any(value => value.Id == unit.Id && value.Health > 0));
            Require(deaths > 0 && delta == 2 * deaths && next.BaseAttack == boss.BaseAttack,
                "Original combat Harvest must grant precisely Rage2 per same-floor enemy death before room-end decay.");
            positive += deaths;
        }
        Require(!requireGrowth || positive > 0, "Arkion Rage needs a positive legal native enemy-death Harvest witness.");
        BattleTurnChecks.Native(fixture);
        if (requireGrowth) BattleActionChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"ARKION-RAGE-BATTLE PASS: growthRequired={requireGrowth}, {positive} positive Rage2 increments, complete native state/RNG; no-card immutable full chains and legal spell replay remain separate.");
    }
    private static void Trigger(CombatUnit boss)
    {
        CombatEffect effect = boss.Triggers.Single(trigger => trigger.Kind == "OnAnyHeroDeathOnFloor").Effects.Single();
        Require(effect.Type == "CardEffectAddStatusEffect" && effect.Action is
            { Type: "AddStatus", Target: "Self", Range: null, UseCardTraits: false } &&
            effect.Action.Statuses.Single() is { Id: "buff", Stacks: 2 }, "Original Arkion self Rage2 signature changed.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
