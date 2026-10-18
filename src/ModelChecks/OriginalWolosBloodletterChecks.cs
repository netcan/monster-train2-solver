using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalWolosBloodletterChecks
{
    private static CombatRelicState Owner(int damage = 1) => new(RelicEndTurnHealthModel.DataId, RelicEndTurnHealthModel.AssetKey,
        [RelicEndTurnHealthModel.Effect, RelicModel.ModifyCardAttack], isCovenant: false, disallowedInPlacementPhase: true,
        cardAttackModifiers: [new(1, "", "", 12, 1, 0, "Monster", false, false, false, false)],
        resourceEffects: [new(0, RelicEndTurnHealthModel.Effect, damage, 0, damage, true, endTurnHealth: new("Monsters", false))]);
    private static CombatUnit Unit(int id, int hp = 4, CombatTeam team = CombatTeam.Player, bool pyre = false, params CombatStatus[] statuses) =>
        new(id, "BloodletterControl" + id, team, 0, hp, hp, false, pyre, false, statuses, size: 1);
    private static CombatContext Context() => new(new([], [], [], UnityRng.Seed(91), 0, []), UnityRng.Seed(92), 0, 1, 10, [],
        relics: [Owner()], queryFrame: new(energy: 0, runningCombat: true, turn: 1, selectedRoomIndex: 1),
        energyState: new(99, 0, 0, "MonsterTurn", true), resources: new(3, 0, 5, 99, 10, "pyre", 20, 80, 0, 20, [], [], []));
    internal static void Run()
    {
        Require(RelicModel.Validate([Owner()]) == null && RelicModel.Validate([Owner(2)]) != null, "Bloodletter original definition gate failed.");
        RoomCombatState selected = new(1, false, [Unit(1, team: CombatTeam.Enemy), Unit(2), Unit(3, statuses: [new("untouchable", 1)]), Unit(4, pyre: true)], [], Context());
        Require(RelicEndTurnHealthModel.Targets(selected).SequenceEqual([2]), "Bloodletter targets must exclude enemies, Pyre and untouchable units.");
        RoomCombatState armored = new(1, false, [Unit(2, statuses: [new("armor", 1, 1, removeWhenTriggered: true)])], [], Context());
        string parent = JsonSerializer.Serialize(armored, ModelJson.Options);
        var first = RoomCombatModel.Resolve(armored);
        Require(first.Supported && first.State!.Units.Single().Health == 4 && !(first.State.Units.Single().Status("armor")?.Stacks > 0),
            "Bloodletter damage must consume armor before health: " + first.UnsupportedReason);
        var second = RoomCombatModel.Resolve(first.State!);
        Require(second.Supported && second.State!.Units.Single().Health == 3, "Bloodletter must recur after armor is gone.");
        var placement = RoomCombatModel.Resolve(new(1, true, armored.Units, [], armored.Context));
        Require(placement.Supported && placement.State!.Units.Single().Health == 4 && placement.State.Units.Single().Status("armor")?.Stacks == 1,
            "Deployment must gate end-turn damage.");
        var removed = RoomCombatModel.Resolve(new(1, false, [Unit(2)], [], Context().WithRelics([])));
        Require(removed.Supported && removed.State!.Units.Single().Health == 4, "Owner ablation must remove end-turn damage.");
        var lethal = RoomCombatModel.Resolve(new(1, false, [Unit(2, hp: 1)], [], Context()));
        Require(lethal.Supported && lethal.State!.Units.Count == 0, "Full room completion must settle a lethal end-turn hit.");
        Parallel.For(0, 16, _ => Equal(RoomCombatModel.Resolve(armored).State, first.State, "parallel armor damage"));
        Require(JsonSerializer.Serialize(armored, ModelJson.Options) == parent, "End-turn damage mutated its parent.");
        Console.WriteLine("WOLOS-BLOODLETTER-PURE PASS: exact owner, enemy/Pyre/untouchable exclusions, armor, repeated damage, deployment/ablation gates, lethal room cleanup and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 236 && root.GetProperty("OriginalWolosBloodletterPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Bloodletter battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalWolosBloodletterCaptureFailures", "OriginalWolosBloodletterMismatches",
            "OriginalWolosBloodletterUnsupported", "OriginalWolosBloodletterPending" }) Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int tests = 0, applications = 0, notifications = 0, armor = 0, lethal = 0, untouchable = 0, empty = 0;
        var floors = new HashSet<int>();
        foreach (var row in root.GetProperty("RelicWolosBloodletterRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            string kind = row.GetProperty("Kind").GetString()!; int room = row.GetProperty("Room").GetInt32(); floors.Add(room);
            RoomCombatState scope = before.Rooms.Single(item => item.RoomIndex == room);
            int[] targets = RelicEndTurnHealthModel.Targets(scope);
            Require(targets.SequenceEqual(row.GetProperty("CachedTargets").Deserialize<int[]>()!), "Native target cache differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                if (kind == "Test") { Equal(before, actual, "Test must leave gameplay state unchanged"); Require(row.GetProperty("Returned").GetBoolean() == (targets.Length > 0), "Native Test bool differs."); }
                else { var predicted = RelicEndTurnHealthModel.ApplyCallback(before, room); Require(predicted.Supported, predicted.UnsupportedReason!); Equal(predicted.State, actual, "native Apply callback before outer death cleanup"); }
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Bloodletter callback mutated its parent.");
            if (kind == "Test") { tests++; if (targets.Length == 0) empty++; }
            else
            {
                applications++; int count = row.GetProperty("Notifications").GetArrayLength(); notifications += count;
                Require(count == targets.Count(id => scope.Units.Single(unit => unit.Id == id).DeathState?.IsDestroyed != true), "Missing per-target trigger notification.");
                foreach (CombatUnit unit in scope.Units.Where(unit => targets.Contains(unit.Id)))
                {
                    CombatUnit? changed = actual.Rooms.SelectMany(item => item.Units).FirstOrDefault(item => item.Id == unit.Id);
                    if (unit.Status("armor")?.Stacks == 1 && changed?.Health == unit.Health && !(changed.Status("armor")?.Stacks > 0)) armor++;
                    if (unit.Health == 1 && changed == null)
                    {
                        Require(actual.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == unit.SpawnerCardId) &&
                            !actual.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == unit.SpawnerCardId),
                            "Raw damage must defer spawner exhaustion until the outer room cleanup."); lethal++;
                    }
                }
            }
            if (!row.GetProperty("Control").GetBoolean()) Require(before.Context!.QueryFrame!.Turn > 0, "Native manager did not gate deployment.");
            foreach (CombatUnit unit in scope.Units.Where(unit => unit.Status("untouchable")?.Stacks > 0))
            { Require(!targets.Contains(unit.Id) && actual.Rooms.SelectMany(item => item.Units).Single(item => item.Id == unit.Id).Health == unit.Health, "Untouchable control changed."); untouchable++; }
        }
        Require(tests >= 12 && applications >= 5 && armor > 0 && lethal == 1 && untouchable > 0 && empty >= 4 && floors.Count == 4,
            "Missing repeated/all-floor, armor, lethal, untouchable or empty/Pyre controls.");
        int[] carriers = root.GetProperty("OriginalWolosBloodletter").GetProperty("Cards").Deserialize<int[]>()!;
        Require(root.GetProperty("Actions").GetArrayLength() == 5, "Expected five natural deployments.");
        Require(root.GetProperty("Turns").EnumerateArray().Any(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.OtherPiles!
            .Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == carriers[4])), "Outer room cleanup did not exhaust the lethal carrier.");
        Manager(root);
        Console.WriteLine($"ORIGINAL-WOLOS-BLOODLETTER-NATIVE PASS: {tests} exact Tests, {applications} applications, {notifications} per-target notifications, armor/untouchable/Pyre/empty controls, deferred lethal spawner routing and 16 immutable branches.");
    }
    private static void Manager(FixtureValue root)
    {
        int count = 0, monsters = 0, spells = 0, resets = 0;
        foreach (var row in root.GetProperty("RelicCardModifiers").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            var context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!;
            var after = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!;
            var relics = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            var dispatches = row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!;
            bool reset = row.GetProperty("ResetTemporary").GetBoolean();
            void Replay()
            {
                var result = RelicCardModifierModel.Apply(before, relics, reset, context); Require(result.Supported, result.UnsupportedReason!);
                Equal(result.Card, actual, "native +12 manager card"); Equal(result.Dispatches, dispatches, "native manager dispatch");
                Equal(result.Context!.Relics, after.Relics, "native manager tracking state"); Equal(result.Context.Statistics, after.Statistics, "native manager statistics");
                var expected = result.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex)).ToArray();
                var notified = row.GetProperty("Notifications").EnumerateArray().Select(item => (item.GetProperty("RelicIndex").GetInt32(), item.GetProperty("EffectIndex").GetInt32())).ToArray();
                Require(expected.SequenceEqual(notified), "Manager notifications differ.");
            }
            string parent = JsonSerializer.Serialize(new { before, context, relics }, ModelJson.Options);
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(new { before, context, relics }, ModelJson.Options) == parent, "Attack manager mutated its source.");
            if (before.MaskDescriptor!.Definition.CardType == "Monster") { monsters++; if (reset) { Require(actual.Temporary.Offsets.Damage == 12, "Reset must install exactly +12."); resets++; } }
            else { Require(actual.Temporary.Offsets.Damage == 0, "Spell received Monster attack."); spells++; } count++;
        }
        Require(monsters >= 5 && spells >= 2 && resets >= 5, "Missing attack/reset/wrong-type manager controls.");
        Console.WriteLine($"WOLOS-BLOODLETTER-MANAGER PASS: {count} exact calls, +12/reset/repeat/Spell exclusion and immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, label + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
