using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PersistentEnchantmentChecks
{
    internal static void Run()
    {
        var armor = new CombatStatus("armor", 3, 1, removeWhenTriggered: true);
        var unit = new CombatUnit(1, "starting-immunity", CombatTeam.Player, 1, 10, 10, true, false, false,
            [armor], statusImmunities: ["armor"], statusRegistry: [armor]);
        var rng = UnityRng.Seed(42);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 0, 2);
        var room = new RoomCombatState(0, false, [unit], [], context);
        context = context.WithEnchantments(new EnchantmentWorld([room], [], 7, [], [], true, false, false, rng, true));
        room = new RoomCombatState(0, false, [unit], [], context);
        RoomCombatState? initialized = null;
        string? error = StatusCallbackModel.Initialize(room, unit, [armor], [], updateRoom: value => initialized = value);
        Require(error == null && initialized?.Units.Single().Status("armor")?.Stacks == 3 &&
            initialized.Units.Single().StatusImmunities.SequenceEqual(new[] { "armor" }) &&
            initialized.Context!.Enchantments!.Rooms.Single().Units.Single().StatusImmunities.SequenceEqual(new[] { "armor" }),
            "Starting statuses must precede authored immunities, then preserve the immunities in the shared world.");
        Require(room.Units.Single().Statuses.Single().Stacks == 3 && room.Context == context,
            "Shared-world status initialization mutated its parent.");
        Console.WriteLine("PERSISTENT-ENCHANTMENT-INITIALIZATION-CHECKS PASS: starting statuses before authored immunities and synchronized immutable world.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) ||
            scenario.GetString() is not ("persistent-enchantment" or "persistent-enchantment-deaths" or "persistent-enchantment-revivals" or
                "persistent-enchantment-random" or "persistent-enchantment-random-deaths" or "persistent-enchantment-random-revivals")) return;
        bool randomPools = scenario.GetString()!.StartsWith("persistent-enchantment-random", StringComparison.Ordinal);
        bool sourceDeaths = scenario.GetString()!.EndsWith("-deaths", StringComparison.Ordinal) || scenario.GetString()!.EndsWith("-revivals", StringComparison.Ordinal);
        Require(fixture.GetProperty("Schema").GetInt32() >= 103 &&
            fixture.GetProperty("GameVersion").GetString() == "2.2.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(key => fixture.GetProperty(key).GetInt32() == 0),
            "The persistent-aura oracle is incomplete or differs from the native game.");
        var actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        var turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        Require(actions.Length >= 2 && turns.Length >= 3 &&
            actions.All(action => !action.TryGetProperty("ScenarioAction", out var authored) || !authored.GetBoolean()),
            "Persistent auras require an ordinary paid policy over multiple turns.");
        BattleTurnState[] states = actions.Concat(turns)
            .Select(entry => entry.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        Require(states.All(state => state.BattlePreviewEnabled && state.CanonicalPhysicalReferences &&
            state.Spawn.Train.Context?.IsolatedBattlePreview == true &&
            state.Spawn.Train.Context.Enchantments?.AutomaticLifecycle == true),
            "The persistent-aura oracle lost preview, physical points or its shared world.");
        EnchantmentWorld[] worlds = states.Select(state => state.Spawn.Train.Context!.Enchantments!).ToArray();
        Require(worlds.Any(world => Rules(world).Count(rule => rule.Bound) >= 2) &&
            (sourceDeaths || Rules(worlds[^1]).Any(rule => rule.Bound)),
            "Paid aura sources were not bound or did not persist through the terminal battle.");
        Require(worlds.Any(world => Rules(world).Any(rule => rule.Bound && rule.Targeting.AllowEnemy &&
            rule.Targeting.AllowPlayer && rule.StatusPool.Count == (randomPools ? 3 : 1) && rule.StatusPool[0].Id == "armor" &&
            rule.StatusPool[0].Stacks == 2 && rule.State.PrimaryTargets.Any(target => target.IsEnchanted &&
                Units(world).Any(unit => unit.Id == target.UnitId && unit.Team == CombatTeam.Enemy)))) &&
            worlds.Any(world => Rules(world).Any(rule => rule.State.PrimaryTargets.Any(target => target.IsEnchanted &&
                Units(world).Any(unit => unit.Id == target.UnitId && unit.Team == CombatTeam.Player)))),
            "The persistent-aura oracle did not enchant both teams with the intended status pool.");
        Require(worlds.Any(world => Rules(world).Any(rule => rule.State.PreviewTargets.Count > 0)) &&
            worlds.Any(world => Rules(world).Any(rule => rule.State.PreviewTargets.Any(target => target.NextAction == 2))) &&
            worlds.Any(world => Rules(world).Any(rule => !rule.State.PreviewRequiresSync)) &&
            worlds.Any(world => Rules(world).Any(rule => rule.State.PreviewRequiresSync)),
            "Native preview target removal or preview preparation was not observed.");
        Require(worlds.Any(world => world.RetainedUnits.Any(actor => actor.Unit.DeathState?.IsDestroyed == true &&
            actor.RoomIndex == -1 && Rules(world).Any(rule => rule.State.PrimaryTargets.Concat(rule.State.PreviewTargets)
                .Any(target => target.UnitId == actor.Unit.Id)))),
            "The persistent-aura oracle did not retain a destroyed target referenced by an effect map.");
        Require(worlds.Any(world => Units(world).Any(unit => unit.Triggers.Any(trigger =>
            trigger.Kind == "OnStatusEffectChanged"))) && actions.Any(entry =>
            entry.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Gold >
            entry.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Gold),
            "The paid aura policy did not drain its status-change child effects.");
        if (randomPools) VerifyRandomCoverage(fixture, actions, turns, worlds);
        if (scenario.GetString() == "persistent-enchantment-random-revivals") VerifyRemovalSettlement(fixture, turns.Length);
        if (sourceDeaths)
        {
            int[] sources = worlds.SelectMany(world => world.RetainedUnits).Where(actor =>
                actor.Unit.Health == 0 && actor.Unit.DeathState?.IsDestroyed == true && actor.RoomIndex == -1 &&
                actor.Unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment != null &&
                    !effect.Enchantment.Bound && effect.Enchantment.State.PrimaryTargets.Count > 0 &&
                    effect.Enchantment.State.PrimaryTargets.Any(target => !target.IsEnchanted)))
                .Select(actor => actor.Unit.Id).Distinct().ToArray();
            Require(sources.Length >= 2 && sources.All(id => worlds.Any(world => world.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id == id && unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.Bound == true)))) &&
                worlds.All(world => world.RetainedUnits.Where(actor => sources.Contains(actor.Unit.Id))
                    .SelectMany(actor => actor.Unit.Triggers).SelectMany(trigger => trigger.Effects)
                    .Where(effect => effect.Enchantment != null && !effect.Enchantment.Bound)
                    .All(effect => effect.Enchantment!.State.PrimaryTargets.Where(target => world.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == target.UnitId && unit.Health > 0)).All(target => !target.IsEnchanted))) &&
                worlds.Any(world => Rules(world).Any(rule => !rule.Bound && rule.State.PrimaryTargets.Any(target =>
                    target.IsEnchanted && target.NextAction == 2 && world.RetainedUnits.Any(actor =>
                        actor.Unit.Id == target.UnitId && actor.Unit.DeathState?.IsDestroyed == true)))) &&
                worlds[^1].RetainedUnits.Where(actor => sources.Contains(actor.Unit.Id))
                    .All(actor => actor.Unit.Triggers.SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                        .All(effect => !effect.Enchantment!.Bound && !effect.Enchantment.State.PreviewRequiresSync)),
                "The source-death oracle omitted released sources, withdrawn live targets, skipped corpse entries or dormant preview maps.");
            Console.WriteLine($"NATIVE-PERSISTENT-ENCHANTMENT-DEATH-COVERAGE PASS: {sources.Length} formerly bound sources, " +
                "withdrawn live-target statuses, retained skipped corpse entries, binding release and no subsequent preview preparation.");
        }
        Console.WriteLine($"NATIVE-PERSISTENT-ENCHANTMENT-COVERAGE PASS: {actions.Length} paid actions, {turns.Length} EndTurns, " +
            "persistent bound sources, both teams, preview maps, retained destroyed targets and drained status children.");
    }

    private static void VerifyRandomCoverage(FixtureValue fixture, FixtureValue[] actions, FixtureValue[] turns, EnchantmentWorld[] worlds)
    {
        Require(worlds.SelectMany(Rules).Where(rule => rule.Bound).All(rule =>
            rule.StatusPool.Select(status => status.Id + ":" + status.Stacks).SequenceEqual(new[] { "armor:2", "regen:1", "buff:1" })) &&
            worlds.SelectMany(Rules).Select(rule => rule.State.CachedStatus?.Id).Where(id => id != null).Distinct().Count() == 3,
            "The random aura oracle did not retain and select all three authored pool entries.");
        Require(actions.Concat(turns).Any(entry =>
            !entry.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.BattleRng.Equals(
            entry.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.BattleRng)),
            "The random aura oracle did not advance the real Battle stream.");
        var previews = fixture.GetProperty("PreviewRngIsolation").EnumerateArray().ToArray();
        Require(previews.Length > 0 && previews.All(entry => entry.GetProperty("Completed").GetBoolean() &&
            entry.GetProperty("BattleBefore").Deserialize<UnityRng>().Equals(entry.GetProperty("BattleAfter").Deserialize<UnityRng>()) &&
            entry.GetProperty("TestBefore").Deserialize<UnityRng>().Equals(entry.GetProperty("TestAfter").Deserialize<UnityRng>())) &&
            previews.Any(entry => !entry.GetProperty("BattleBefore").Deserialize<UnityRng>().Equals(entry.GetProperty("TestObserved").Deserialize<UnityRng>())),
            "Random aura previews must consume BattleTest and restore both original streams.");
        var battlePreviews = previews.Where(entry => entry.GetProperty("Kind").GetString() == "Battle").ToArray();
        Require(battlePreviews.Length > 0 && battlePreviews.All(entry => entry.GetProperty("PrimaryRoomOrderCompleted").GetBoolean()) &&
            battlePreviews.Any(entry => !entry.GetProperty("BattleBefore").Deserialize<UnityRng>().Equals(entry.GetProperty("BattleObserved").Deserialize<UnityRng>())),
            "Random aura isolation must cover the actual primary room-order update after preview restoration.");
        Console.WriteLine($"NATIVE-PERSISTENT-ENCHANTMENT-RANDOM-COVERAGE PASS: three selected statuses, real Battle advancement, " +
            $"{previews.Length} isolated native previews and complete primary/preview caches.");
    }

    private static void VerifyRemovalSettlement(FixtureValue fixture, int turns)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 104 && fixture.GetProperty("DeathDissolveSettlementEnabled").GetBoolean(),
            "Random aura revival requires the explicitly enabled stable removal protocol.");
        var records = fixture.GetProperty("DeathDissolveSettlements").EnumerateArray().ToArray();
        var callbacks = fixture.GetProperty("DeathDissolveCallbacks").EnumerateArray().ToArray();
        Require(records.Length >= turns - 1 && records.All(item => item.GetProperty("Completed").GetBoolean() &&
            item.GetProperty("PendingAfter").GetInt32() == 0 && item.GetProperty("Error").ValueKind == FixtureKind.Null) &&
            records.Any(item => item.GetProperty("FramesWaited").GetInt32() > 0 && item.GetProperty("PendingUnitIds").GetArrayLength() > 0),
            "Every native nonterminal removal flush must wait for original pending callbacks.");
        Require(callbacks.Length > 0 && callbacks.All(item => item.GetProperty("Error").ValueKind == FixtureKind.Null) &&
            callbacks.Where(item => item.GetProperty("CompletedFrame").ValueKind != FixtureKind.Null).All(item =>
                item.GetProperty("CompletedFrame").GetInt32() - item.GetProperty("RegisteredFrame").GetInt32() >= item.GetProperty("MinimumFrames").GetInt32()) &&
            callbacks.Any(item => item.GetProperty("CompletedFrame").ValueKind != FixtureKind.Null && item.GetProperty("MinimumFrames").GetInt32() >= 10) &&
            records.All(record => record.GetProperty("PendingUnitIds").Deserialize<int[]>()!.All(id => callbacks.Any(callback =>
                callback.GetProperty("UnitId").GetInt32() == id && callback.GetProperty("CompletedFrame").ValueKind != FixtureKind.Null &&
                callback.GetProperty("CompletedFrame").GetInt32() <= record.GetProperty("StartedFrame").GetInt32() + record.GetProperty("FramesWaited").GetInt32()))),
            "Actual original dissolve completion and its minimum frame delay were not observed.");
        Console.WriteLine($"NATIVE-REMOVAL-SETTLEMENT-CHECKS PASS: {records.Length} original turn-end flushes, " +
            $"{records.Sum(item => item.GetProperty("FramesWaited").GetInt32())} awaited frames, unchanged minimum-frame callbacks and no premature queue processing.");
    }

    private static IEnumerable<CombatUnit> Units(EnchantmentWorld world) =>
        world.Rooms.SelectMany(room => room.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit));
    private static IEnumerable<EnchantmentRule> Rules(EnchantmentWorld world) => Units(world)
        .SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects)
        .Where(effect => effect.Enchantment != null).Select(effect => effect.Enchantment!);
    private static void Require(bool condition, string error)
    { if (!condition) throw new InvalidOperationException(error); }
}
