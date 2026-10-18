using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMooncycleFaeChecks
{
    private static RelicResourceEffect Resource(int damage = 5, int conduit = 1)
    {
        var upgrade = new CardUpgradeModifier(RelicMooncycleRoomEffectModel.UpgradeId, "MooncycleFaeAddMagicPower_UpgradeData",
            new(damage, 0, 0, 0, 0, 0), [], false, false, false, 0, 0, []);
        var tests = new CardEffectTests(true, false, false, false, true);
        var filters = new CardTargetFilters("Both", [], [], false, RelicMooncycleRoomEffectModel.FaeSubtype, []);
        CardActionEffect[] actions = [new("UnitUpgrade", "Room", 0, false, true, [], upgrade, "TemporaryUntilEndOfBattle", tests, filters: filters),
            new("AddStatus", "Room", 0, false, true, [new("conduit", conduit, 1, stackable: true)], tests: tests, filters: filters)];
        return new(0, RelicMooncycleRoomEffectModel.Effect, 1, 0, 1, false,
            mooncycleRoomEffect: new(1, 0, 0, false, false, false, false, actions, [], false, 0));
    }
    private static CombatRelicState Owner(int damage = 5, int conduit = 1) => new(RelicMooncycleRoomEffectModel.DataId,
        RelicMooncycleRoomEffectModel.AssetKey, [RelicMooncycleRoomEffectModel.Effect], isCovenant: false,
        disallowedInPlacementPhase: false, resourceEffects: [Resource(damage, conduit)]);
    internal static void Run()
    {
        var rng = UnityRng.Seed(232);
        var conduitUpgrade = new CardUpgradeModifier("conduit", "ConduitUpgrade", new(0, 0, 0, 0, 0, 0), [], false, false, false, 0, 0, [],
            lifecycle: new(new("conduit", new(0, 0, 0, 0, 0, 0), [], false, false, false, false), "ConduitUpgrade", false, false, [], [], false, [], [], []));
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 10, 10, relics: [Owner()], cardInstances: [],
            statusRules: [new("conduit", 0, 1, stackable: true)], statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
            queryFrame: new(energy: 0, runningCombat: true, turn: 1, selectedRoomIndex: 0), energyState: new(10, 0, 0, "MonsterTurn", true),
            magicPower: Enumerable.Range(0, 4).SelectMany(room => new[] { CombatTeam.Player, CombatTeam.Enemy }.Select(team =>
                new RoomMagicPower(room, team, 0, new(conduitUpgrade, [])))).ToArray(),
            resources: new(3, 1, 5, 10, 10, "pyre", 0, 20, 0, 0, [], [], []));
        CombatUnit Unit(int id, CombatTeam team, bool fae, bool pyre = false) => new(id, "mooncycle-unit-" + id, team, 30, 20, 20,
            false, pyre, false, [], modifiers: new(30, 0, 0, 1, 0, true, false, []),
            subtypes: fae ? [RelicMooncycleRoomEffectModel.FaeSubtype] : []);
        var first = Unit(1, CombatTeam.Player, true); var other = Unit(2, CombatTeam.Player, true);
        var control = Unit(3, CombatTeam.Player, false); var enemy = Unit(4, CombatTeam.Enemy, true); var pyre = Unit(5, CombatTeam.Player, true, true);
        var train = new TrainCombatState([new(0, false, [first, control, enemy], [], context), new(1, false, [other], [], context),
            new(2, false, [], [], context), new(3, false, [pyre], [], context)], [], 7, context);
        Require(RelicModel.Validate(context.Relics) == null && RelicModel.Validate([Owner(6)]) != null && RelicModel.Validate([Owner(conduit: 2)]) != null,
            "Mooncycle original payload guards failed.");
        string parent = JsonSerializer.Serialize(train, ModelJson.Options);
        var result = RelicMooncycleRoomEffectModel.Apply(train);
        Require(result.Supported, "Pure mooncycle failed: " + result.UnsupportedReason);
        foreach (int id in new[] { 1, 2 })
        {
            CombatUnit after = result.State!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            Require(after.Modifiers!.AttackDamage == 35 && after.Modifiers.Upgrades.Count == 1 && after.Status("conduit")?.Stacks == 1,
                "Mooncycle must add the original +5 upgrade then Conduit to friendly Fae on both floors.");
        }
        foreach (int id in new[] { 3, 4, 5 }) Equal(train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id),
            result.State!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id), "non-Fae/enemy/Pyre controls");
        Require(result.State!.Context!.BattleRng.Equals(rng), "Mooncycle consumed Battle RNG.");
        var twice = RelicMooncycleRoomEffectModel.Apply(result.State);
        Require(twice.Supported && twice.State!.Rooms[0].Units[0].Modifiers!.AttackDamage == 40 &&
            twice.State.Rooms[0].Units[0].Modifiers!.Upgrades.Count == 2 && twice.State.Rooms[0].Units[0].Status("conduit")?.Stacks == 2,
            "Mooncycle repeat must accumulate non-unique upgrades and Conduit.");
        Require(ReferenceEquals(RelicMooncycleRoomEffectModel.Apply(train, preview: true).State, train), "Native mooncycle is disabled in preview.");
        Equal(result.State, RelicMooncycleRoomEffectModel.Apply(train, placement: true).State, "placement permission");
        var absent = CardSpellModel.WithContext(train, context.WithRelics([]));
        Require(ReferenceEquals(RelicMooncycleRoomEffectModel.Apply(absent).State, absent), "Absent mooncycle owner changed state.");
        var empty = new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, false,
            room.RoomIndex == 3 ? [pyre] : [], [], context)).ToArray(), [], 7, context);
        Equal(empty, RelicMooncycleRoomEffectModel.Apply(empty).State, "empty floors");
        Parallel.For(0, 16, _ => Equal(result.State, RelicMooncycleRoomEffectModel.Apply(train).State, "parallel branch"));
        Require(JsonSerializer.Serialize(train, ModelJson.Options) == parent, "Mooncycle mutated its parent.");
        Console.WriteLine("MOONCYCLE-FAE-PURE PASS: all-floor friendly Fae, ordered +5 upgrade/Conduit, non-Fae/enemy/Pyre/empty controls, repeated non-unique stacking, preview/placement, strict payload and immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 232 && root.GetProperty("OriginalMooncycleFaeSchema").GetInt32() == 232 &&
            root.GetProperty("ModifierScenario").GetString() == "original-mooncycle-fae" && root.GetProperty("OriginalMooncycleFaePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalMooncycleFaeCaptureFailures", "OriginalMooncycleFaeMismatches",
                "OriginalMooncycleFaeUnsupported", "OriginalMooncycleFaePending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Native mooncycle acceptance failed.");
        if (root.GetProperty("Actions").GetArrayLength() != 3)
        {
            BattleTurnState first = root.GetProperty("Turns").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(first);
            throw new InvalidOperationException("Incomplete controlled mooncycle plays: " + frontier.SupportedActions.Count +
                " supported; " + string.Join("; ", frontier.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()));
        }
        Require(root.GetProperty("Actions").EnumerateArray().All(row => row.GetProperty("ScenarioAction").GetBoolean()), "Incomplete marked mooncycle plays.");
        string[] frontierErrors = root.GetProperty("Actions").EnumerateArray().SelectMany(row => BattleActionModel.EnumerateActions(
            row.GetProperty("Before").Deserialize<BattleTurnState>()!).UnsupportedAttempts).Select(attempt => attempt.Action?.CardInstanceId + ": " + attempt.Reason).Distinct().ToArray();
        Require(frontierErrors.Length == 0, "Mooncycle action frontier remains unsupported: " + string.Join("; ", frontierErrors));
        int tests = 0, emptyControls = 0;
        foreach (var row in root.GetProperty("RelicMooncycleFaeTests").EnumerateArray())
        {
            Require(row.GetProperty("Result").GetBoolean(), "Original mooncycle Test unexpectedly rejected its non-strict status action.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "native Test state/RNG");
            if (row.GetProperty("EmptyControl").GetBoolean())
            {
                emptyControls++; Require(row.GetProperty("Before").Deserialize<TrainCombatState>()!.Rooms.Where(room => room.RoomIndex < 3).All(room => room.Units.Count == 0), "Empty Test control has actors.");
            }
            tests++;
        }
        int applications = 0, upgrades = 0, controls = 0, spawnerUpgrades = 0, spellWitnesses = 0;
        int spellId = root.GetProperty("OriginalMooncycleFae").GetProperty("SpellCard").GetInt32();
        var floors = new HashSet<int>(); var phases = new HashSet<int>();
        foreach (var row in root.GetProperty("RelicMooncycleFaeApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(row.GetProperty("Completed").GetBoolean() && RelicModel.Validate(before.Context!.Relics!) == null, "Incomplete native mooncycle or original payload.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay() { var result = RelicMooncycleRoomEffectModel.Apply(before); Require(result.Supported, "Unsupported native mooncycle: " + result.UnsupportedReason); Equal(result.State, actual, "native full state"); }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(before.Context!.BattleRng.Equals(actual.Context!.BattleRng), "Native mooncycle consumed RNG.");
            CardInstanceState spell = actual.Context.FindCard(spellId) ?? throw new InvalidOperationException("Missing Persistent Conduit spell.");
            Require(actual.Context.Cards.Hand.Any(card => card.InstanceId == spellId), "Persistent Conduit spell left hand.");
            int selectedPower = actual.Context.MagicPower!.Where(room => room.RoomIndex == actual.Context.QueryFrame!.SelectedRoomIndex && room.Team == CombatTeam.Player)
                .SelectMany(room => room.Conduit!.Units).Where(unit => unit.Active).Sum(unit => unit.Power);
            Require(spell.Temporary.Upgrades.Where(upgrade => upgrade.AssetKey == "ConduitUpgrade").Sum(upgrade => upgrade.Stats.Damage) == selectedPower,
                "Native Conduit must update the Persistent spell for the selected floor.");
            if (selectedPower > 0) spellWitnesses++;
            foreach (RoomCombatState room in before.Rooms)
                foreach (CombatUnit unit in room.Units)
                {
                    CombatUnit after = actual.Rooms.SelectMany(item => item.Units).Single(item => item.Id == unit.Id);
                    if (unit.IsPyre || unit.Team != CombatTeam.Player || !unit.Subtypes.Contains(RelicMooncycleRoomEffectModel.FaeSubtype))
                    { Equal(unit, after, "native non-Fae/enemy/Pyre control"); controls++; continue; }
                    Require(after.Modifiers!.AttackDamage == unit.Modifiers!.AttackDamage + 5 &&
                        after.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == RelicMooncycleRoomEffectModel.UpgradeId) ==
                        unit.Modifiers.Upgrades.Count(upgrade => upgrade.DataId == RelicMooncycleRoomEffectModel.UpgradeId) + 1 &&
                        after.Status("conduit")!.Stacks == (unit.Status("conduit")?.Stacks ?? 0) + 1, "Native Fae upgrade/Conduit differs.");
                    var card = before.Context.FindCard(unit.SpawnerCardId); var afterCard = actual.Context.FindCard(unit.SpawnerCardId);
                    Require(card != null && afterCard != null && afterCard.Temporary.Upgrades.Count == card.Temporary.Upgrades.Count + 1,
                        "TemporaryUntilEndOfBattle must also upgrade the spawner card.");
                    upgrades++; spawnerUpgrades++; floors.Add(room.RoomIndex);
                }
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native mooncycle replay mutated parent.");
            phases.Add(row.GetProperty("Phase").GetInt32()); applications++;
        }
        Require(applications >= 2 && tests >= applications + 1 && emptyControls == 1 && upgrades >= 4 && controls >= 3 &&
            floors.SetEquals([0, 1]) && phases.SetEquals([1, 2]) && spellWitnesses >= 2, "Missing repeated Full/New, multi-floor, spawner, Conduit hand or excluded controls.");
        Console.WriteLine($"ORIGINAL-MOONCYCLE-FAE-NATIVE PASS: {applications} Full/New applications, {upgrades} exact +5/Conduit targets, {spawnerUpgrades} spawner upgrades, {spellWitnesses} selected-floor spell consumers, {controls} exclusions, empty-floor Test, unchanged RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string phase)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Mooncycle " + phase + " differs: " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
