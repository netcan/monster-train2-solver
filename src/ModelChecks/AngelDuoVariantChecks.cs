using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoVariantChecks
{
    internal static void Birth(FixtureValue fixture, string variant = "LoopResolve")
    {
        bool revenge = variant == "RevengeHarvest";
        Require(revenge || variant == "LoopResolve", "Unknown Angel Duo birth variant.");
        string outerAsset = revenge ? "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" :
            "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth";
        string innerAsset = OuterBossDefinitions.AngelDuoCompanion(outerAsset)!;
        OuterBossBirthChecks.Native(fixture);
        FixtureValue row = fixture.GetProperty("Births")[0];
        var created = row.GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        var actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
        CombatUnit outer = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actual.Boss.UnitId);
        CombatUnit inner = actual.Battle.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit =>
            unit.AssetKey == innerAsset);
        Require(created.CreatedUnit.AssetKey == outerAsset &&
            created.CreatedUnit.Health == 1250 && created.CreatedUnit.BaseAttack == 11 &&
            created.BirthStats is { Attack: 3, Health: 250 } &&
            outer.Health == 1500 && outer.MaxHealth == 1500 && outer.BaseAttack == 14 && outer.Status("titanskin")?.Stacks == 10 &&
            inner.Health == 1350 && inner.MaxHealth == 1350 && inner.BaseAttack == 16 &&
            inner.Status("multistrike")?.Stacks == 1 && inner.Status("pyrebane")?.Stacks == 1 &&
            outer.BumpRules!.PairedBossUnitId == inner.Id && inner.BumpRules!.PairedBossUnitId == outer.Id &&
            !outer.EndsBattleOnDeath && !inner.EndsBattleOnDeath &&
            outer.Triggers.Single(trigger => trigger.Kind == (revenge ? "OnAnyHeroDeathOnFloor" : "PostCombat"))
                .Effects.Single().Value == (revenge ? 50 : 60) &&
            inner.Triggers.Single(trigger => trigger.Kind == (revenge ? "OnHit" : "OnTrainRoomLoop"))
                .Effects.Single().Value == (revenge ? 1 : 8),
            "Angel Duo birth changed native authored stats, cumulative adjustments, pairing or installed growth triggers.");
        Console.WriteLine($"ANGEL-DUO-{variant}-BIRTH PASS: original rank-two paired births and native battle-relic installation.");
    }

    internal static void Input(FixtureValue fixture, string variant = "LoopResolve")
    {
        string scenario = variant == "LoopResolve" ? "Level6AngelDuo_LoopAttackResolveHealth" :
            variant == "RevengeHarvest" ? "Level6AngelDuo_RevengeAttackHarvestHealth" :
                throw new InvalidOperationException("Unknown Angel Duo variant check.");
        string outer = variant == "LoopResolve" ? "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" :
            "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth";
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("Scenario").GetString() == scenario &&
            input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 5 &&
            input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("StartingLevel").GetInt32() == 2 &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("CompletedBattles").GetArrayLength() == 5 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()),
            "Angel Duo variant requires the original five-win rank-two checkpoint.");
        FixtureValue route = fixture.GetProperty("OriginalOuterBossRoute");
        Require(route.GetProperty("Kind").GetString() == "SelectOriginalAngelDuoScenario" &&
            route.GetProperty("Distance").GetInt32() == 5 && route.GetProperty("SelectedScenario").GetString() == scenario &&
            route.GetProperty("Boss").GetString() == outer &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")),
            "Angel Duo variant requires its explicit original-scenario selection without asset or RNG changes.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Angel Duo variant native battle failed: " + counter);
        Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            fixture.GetProperty("AngelDuoDeathTransition").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("AngelDuoRelentlessEntry").ValueKind == FixtureKind.Null,
            "Angel Duo variant must retain original health and phase transitions.");
    }

    internal static void Battle(FixtureValue fixture, string path)
    {
        Input(fixture);
        int resolve = 0, remoteResolve = 0, loops = 0;
        foreach (FixtureValue phase in fixture.GetProperty("TrainPhases").EnumerateArray())
        {
            string kind = phase.GetProperty("Kind").GetString()!;
            TrainCombatState before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit? outer = before.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit =>
                unit.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" && unit.Health > 0);
            CombatUnit? inner = before.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit =>
                unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant1_LoopAttack" && unit.Health > 0);
            if (outer == null || inner == null) continue;
            CombatUnit? nextOuter = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == outer.Id);
            CombatUnit? nextInner = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == inner.Id);
            if (nextOuter == null || nextInner == null) continue;
            if (kind == "Combat" && nextInner.MaxHealth > inner.MaxHealth)
            {
                CombatTrigger trigger = outer.Triggers.Single(value => value.Kind == "PostCombat");
                CombatEffect effect = trigger.Effects.Single();
                Require(effect.Type == "CardEffectBuffMaxHealth" && effect.Value == 60 && effect.Action is
                    { Type: "BuffHealth", Target: "Tower", Lifetime: "TemporaryUntilEndOfBattle", UseCardTraits: false } &&
                    effect.Action.Filters?.Subtype == "SubtypesData_SavageAngel" &&
                    nextInner.MaxHealth == inner.MaxHealth + 60 * trigger.FireCount && nextOuter.MaxHealth == outer.MaxHealth,
                    "Original Resolve must exclusively increase the Savage companion's maximum health by60.");
                resolve++;
                if (before.Rooms.Single(room => room.Units.Any(unit => unit.Id == outer.Id)).RoomIndex !=
                    before.Rooms.Single(room => room.Units.Any(unit => unit.Id == inner.Id)).RoomIndex) remoteResolve++;
            }
            if (kind == "Ascend" && nextOuter.BaseAttack > outer.BaseAttack)
            {
                CombatTrigger trigger = inner.Triggers.Single(value => value.Kind == "OnTrainRoomLoop");
                CombatEffect effect = trigger.Effects.Single();
                Require(effect.Type == "CardEffectBuffDamage" && effect.Value == 8 && effect.Action is
                    { Type: "BuffAttack", Target: "Tower", AllowPlayer: true, AllowEnemy: true, UseCardTraits: false } &&
                    effect.Action.Filters?.Subtype == "SubtypesData_EntropicAngel" &&
                    nextOuter.BaseAttack == outer.BaseAttack + 8 * trigger.FireCount && nextInner.BaseAttack == inner.BaseAttack &&
                    before.Rooms.Single(room => room.Units.Any(unit => unit.Id == inner.Id)).RoomIndex == 2 &&
                    actual.Rooms.Single(room => room.Units.Any(unit => unit.Id == inner.Id)).RoomIndex == 0,
                    "Original train loop must grant only the Entropic companion +8 attack on the top-to-bottom move.");
                loops++;
            }
        }
        Require(resolve > 0 && remoteResolve > 0 && loops > 0,
            "Angel Duo variant needs native positive Resolve, remote-health and train-loop witnesses.");
        BattleTurnChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"ANGEL-DUO-LOOP-RESOLVE PASS: original selected variant, {resolve} health60 phases ({remoteResolve} cross-floor), {loops} attack8 loops, full native state/RNG and sixteen complete immutable branches.");
    }

    internal static void RevengeHarvest(FixtureValue fixture, string path, bool requireGrowth, bool requireHarvest = true)
    {
        Input(fixture, "RevengeHarvest");
        int revenge = 0, harvest = 0, remoteRevenge = 0, remoteHarvest = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Observe(before.Spawn.Train, actual.Spawn.Train);
        }
        foreach (FixtureValue phase in fixture.GetProperty("TrainPhases").EnumerateArray())
            Observe(phase.GetProperty("Before").Deserialize<TrainCombatState>()!,
                phase.GetProperty("Actual").Deserialize<TrainCombatState>()!);
        Require(!requireGrowth || revenge > 0 && remoteRevenge > 0 && (!requireHarvest || harvest > 0 && remoteHarvest > 0),
            "Revenge/Harvest requires positive native +1 attack and +50 health, including cross-floor subtype controls.");
        BattleTurnChecks.Native(fixture);
        BattleActionChecks.Native(fixture);
        TrainCombatChecks.Native(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"ANGEL-DUO-REVENGE-HARVEST PASS: growthRequired={requireGrowth}, harvestRequired={requireHarvest}, attack1={revenge} ({remoteRevenge} remote), health50={harvest} ({remoteHarvest} remote), complete native actions/turns/rooms/train state and RNG.");

        void Observe(TrainCombatState before, TrainCombatState actual)
        {
            const string outerAsset = "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth";
            CombatUnit? outer = before.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit =>
                unit.AssetKey == outerAsset && unit.Health > 0);
            CombatUnit? inner = before.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit =>
                unit.AssetKey == OuterBossDefinitions.AngelDuoCompanion(outerAsset) && unit.Health > 0);
            if (outer == null || inner == null) return;
            CombatUnit? nextOuter = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == outer.Id);
            CombatUnit? nextInner = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == inner.Id);
            if (nextOuter == null || nextInner == null) return;
            CombatEffect attack = inner.Triggers.Single(trigger => trigger.Kind == "OnHit").Effects.Single();
            CombatEffect health = outer.Triggers.Single(trigger => trigger.Kind == "OnAnyHeroDeathOnFloor").Effects.Single();
            Require(attack.Type == "CardEffectBuffDamage" && attack.Value == 1 && attack.Action is
                { Type: "BuffAttack", Target: "Tower", AllowPlayer: true, AllowEnemy: true, UseCardTraits: false } &&
                attack.Action.Filters?.Subtype == "SubtypesData_EntropicAngel" &&
                health.Type == "CardEffectBuffMaxHealth" && health.Value == 50 && health.Action is
                { Type: "BuffHealth", Target: "Tower", Lifetime: "TemporaryUntilEndOfBattle", UseCardTraits: false } &&
                health.Action.Filters?.Subtype == "SubtypesData_SavageAngel",
                "Revenge/Harvest native trigger signatures or subtype filters changed.");
            bool remote = before.Rooms.Single(room => room.Units.Any(unit => unit.Id == outer.Id)).RoomIndex !=
                before.Rooms.Single(room => room.Units.Any(unit => unit.Id == inner.Id)).RoomIndex;
            int attackDelta = nextOuter.BaseAttack - outer.BaseAttack;
            int healthDelta = nextInner.MaxHealth - inner.MaxHealth;
            Require(attackDelta >= 0 && healthDelta >= 0 && healthDelta % 50 == 0 &&
                nextInner.BaseAttack == inner.BaseAttack && nextOuter.MaxHealth == outer.MaxHealth,
                "Revenge/Harvest changed the excluded boss or native companion growth increments.");
            revenge += attackDelta;
            harvest += healthDelta / 50;
            if (remote) { remoteRevenge += attackDelta; remoteHarvest += healthDelta / 50; }
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
