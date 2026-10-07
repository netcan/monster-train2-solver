using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CrossRoomSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, CombatTeam team, int health, CombatStatus[]? statuses = null) =>
            new(id, "unit" + id, team, 3, health, 10, true, false, false, statuses ?? [], size: 1,
                modifiers: new(3, 0, 0, 1, 1, true, false, []));
        var root = new TrainCombatState([
            new(0, false, [Unit(1, CombatTeam.Enemy, 8), Unit(2, CombatTeam.Enemy, 3), Unit(3, CombatTeam.Player, 7), Unit(4, CombatTeam.Player, 3)], [], context),
            new(1, false, [Unit(5, CombatTeam.Enemy, 8), Unit(6, CombatTeam.Player, 7), Unit(7, CombatTeam.Player, 3)], [], context),
            new(2, false, [Unit(8, CombatTeam.Enemy, 8, [new("untouchable", 1)]), Unit(9, CombatTeam.Enemy, 3),
                Unit(10, CombatTeam.Player, 7, [new("stealth", 1)]), Unit(11, CombatTeam.Player, 3)], [], context),
            new(3, false, [new(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context)],
            [new(1, 1, true, false), new(2, 1, true, false), new(5, 1, true, false), new(9, 1, true, false)], 5, context);
        CardActionEffect Effect(string target, bool enemy = true, bool player = true) => new("Damage", target, 1, enemy, player, []);
        int[] Collect(string mode, int room = 0, int[]? last = null, bool enemy = true, bool player = true) =>
            CardTargetModel.Collect(root, room, Effect(mode, enemy, player), last ?? []).UnitIds.ToArray();
        Require(Collect("Tower").SequenceEqual([1, 5, 2, 9, 3, 6, 10, 4, 7, 11]), "Tower order or Pyre/untouchable/stealth filtering differs.");
        Require(Collect("FrontInAllRooms").SequenceEqual([1, 5, 9, 3, 6, 10]) &&
            Collect("FrontInRoomAndRoomAbove").SequenceEqual([1, 5, 3, 6]) &&
            Collect("FrontInRoomAndRoomAbove", 2).SequenceEqual([9, 10]), "Front room ranges, per-team fronts or ordering differ.");
        Require(Collect("WeakestAllRooms").SequenceEqual([9]) && Collect("StrongestAllRooms").SequenceEqual([5]),
            "Current-HP ties did not follow native team/position sorting and room insertion order.");
        Require(Collect("StrongestLastTargetedCharactersRoom", last: [9, 5]).SequenceEqual([10]) &&
            Collect("StrongestLastTargetedCharactersRoom", last: [9], player: false).SequenceEqual([9]) &&
            Collect("StrongestLastTargetedCharactersRoom", last: [999, 5]).Length == 0 &&
            CardTargetModel.Collect(root, 0, Effect("StrongestLastTargetedCharactersRoom"), [9], firstEffect: true).UnitIds.Count == 0,
            "Strongest-last-room used the last group instead of the first reference's live room or ignored team filters.");
        RngDraw draw = rng.Range(0, 10);
        CardTargets random = CardTargetModel.Collect(root, 0, Effect("RandomFromAnyRoom"), []);
        Require(random.UnitIds.Single() == Collect("Tower")[draw.Value] && random.BattleRng.HasValue && random.BattleRng.Value.Equals(draw.State) &&
            !CardTargetModel.Collect(root, 0, Effect("RandomFromAnyRoom"), [], isTesting: true).BattleRng.HasValue,
            "Global random selection used a different candidate order or consumed gameplay RNG during testing.");
        var one = new TrainCombatState([new(0, false, [], [], context), new(1, false, [Unit(6, CombatTeam.Player, 7)], [], context)], [], 5, context);
        Require(CardTargetModel.Collect(one, 0, Effect("RandomFromAnyRoom", false, true), []).BattleRng!.Value.Equals(rng.Next()) &&
            !CardTargetModel.Collect(one, 0, Effect("RandomFromAnyRoom", true, false), []).BattleRng.HasValue,
            "Remote single-candidate selection skipped a live draw or an empty global selection consumed one.");
        var withoutPyre = new TrainCombatState(root.Rooms.Select(room => room.RoomIndex == 3 ? new RoomCombatState(3, false, [], [], context) : room).ToArray(),
            root.Movement, 5, context);
        var definitions = new BattlePlayRules([new(0, 2, 5, true, false, false), new(1, 3, 5, true, false, false),
            new(2, 3, 5, true, false, false), new(3, 0, 0, true, false, true)], []);
        Require(CardSpellModel.TestPlay(withoutPyre, 0, [Effect("FrontInAllRooms")], 0, definitions).CanPlay &&
            CardSpellModel.Apply(withoutPyre, 0, [Effect("FrontInAllRooms")], 0, definitions: definitions).Supported &&
            !CardSpellModel.Apply(withoutPyre, 0, [Effect("FrontInAllRooms")], 0).Supported &&
            !CardSpellModel.Apply(root.Rooms[0], [Effect("Tower")], 0).Supported,
            "Static room ranges were lost after Pyre removal or incomplete inputs silently produced a branch.");
        CardActionEffect[] chain = [new("Damage", "Tower", 3, true, false, []),
            new("AddStatus", "LastTargetedCharacters", 0, true, false, [new("armor", 2, 1, removeWhenTriggered: true)]),
            new("Damage", "FrontInRoomAndRoomAbove", 1, false, true, []), new("Heal", "Tower", 2, false, true, [])];
        string parent = JsonSerializer.Serialize(root);
        var child = CardSpellModel.Apply(root, 0, chain, 0);
        Require(child.Supported && child.State!.Movement.Select(move => move.UnitId).SequenceEqual([1, 5]) &&
            child.State.Rooms.SelectMany(room => room.Units).Where(unit => unit.Id is 1 or 5).All(unit => unit.Health == 5 && unit.Statuses.Single().Stacks == 2) &&
            child.State.Rooms[1].Units.Single(unit => unit.Id == 6).Health == 9 && child.State.Rooms[2].Units.Single(unit => unit.Id == 10).Health == 8 &&
            child.State.Rooms[3].Units.Single().Health == 80 && child.State.Rooms.All(room => ReferenceEquals(room.Context, child.State.Context)),
            "Cross-room deaths, movement, global last references, remote heal or shared context differ: " + child.UnsupportedReason);
        var size = new CardUpgradeModifier("size", "size", new(size: 1), [], false, false, false, 0, 0, [], true);
        var upgraded = CardSpellModel.Apply(root, 0, [new("UnitUpgrade", "Tower", 0, false, true, [], size, "TemporaryUntilUnitDeath")],
            0, definitions: definitions);
        Require(upgraded.Supported && upgraded.State!.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Size == 1) &&
            upgraded.State.Rooms[1].Units.Single(unit => unit.Id == 6).Size == 2 && upgraded.State.Rooms[1].Units.Single(unit => unit.Id == 7).Size == 1,
            "Global size upgrades used selected-room capacity or failed to see a prior target's changed size.");
        var terminalContext = new CombatContext(context.Cards, rng, 0, 1, 10, cardInstances: [], allScenarioBossesDead: false);
        var terminal = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Enemy, 8), Unit(3, CombatTeam.Player, 3)], [], terminalContext),
            new(1, false, [new(13, "boss", CombatTeam.Enemy, 7, 1, 125, true, false, true, []), Unit(6, CombatTeam.Player, 3)], [], terminalContext),
            new(2, false, root.Rooms[3].Units, [], terminalContext)], [new(1, 1, true, false)], 5, terminalContext);
        var won = CardSpellModel.Apply(terminal, 0, [new("Damage", "Tower", 1, true, false, []),
            new("Heal", "FrontInAllRooms", 2, false, true, [])], 0);
        Require(won.Supported && won.Outcome == RoomOutcome.BattleWon && won.State!.Context!.AllScenarioBossesDead == true &&
            won.State.Rooms[0].Units.Single(unit => unit.Id == 3).Health == 5 && won.State.Rooms[1].Units.Single().Health == 5 &&
            won.State.Rooms[2].Units.Single().Health == 80,
            "A remote boss kill stopped global effects, lost the terminal result or affected the Pyre: " + won.UnsupportedReason);
        var references = CardSpellModel.Apply(root, 0, [new("Damage", "StrongestAllRooms", 999, true, false, []),
            new("AddStatus", "StrongestLastTargetedCharactersRoom", 0, false, true, [new("armor", 2, 1, removeWhenTriggered: true)]),
            new("AddStatus", "StrongestLastTargetedCharactersRoom", 0, false, true, [new("armor", 1, 1, removeWhenTriggered: true)])], 0);
        Require(references.Supported && references.State!.Rooms[1].Units.Single(unit => unit.Id == 6).Statuses.Single().Id == "armor" &&
            references.TargetCollections.Single(item => item.EffectIndex == 1).UnitIds.SequenceEqual([6]) &&
            references.TargetCollections.Single(item => item.EffectIndex == 2).UnitIds.Count == 0,
            "Pending death retained its room beyond a status queue drain or lost it before that drain: " + references.UnsupportedReason);
        var focusRoot = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Player, 8, [new("armor", 1, 1, removeWhenTriggered: true)])], [], context),
            new(1, false, [Unit(2, CombatTeam.Player, 8)], [], context), new(2, false, root.Rooms[3].Units, [], context)], [], 5, context);
        var focused = CardSpellModel.Apply(focusRoot, 1, [new("Damage", "Tower", 1, false, true, []),
            new("Damage", "FrontInRoomAndRoomAbove", 0, false, true, [])], 0);
        Require(focused.Supported && focused.TargetCollections[1].UnitIds.SequenceEqual([1, 2]),
            "Armor triggering on another floor did not change room-and-above targeting.");
        var vfxRoot = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Enemy, 8,
                [new("fragile", 1, triggerVfxEnemy: true, triggerVfxPlayer: true)]), Unit(2, CombatTeam.Player, 8)], [], context),
            new(1, false, [Unit(3, CombatTeam.Player, 8)], [], context), new(2, false, root.Rooms[3].Units, [], context)], [], 5, context);
        var vfx = CardSpellModel.Apply(vfxRoot, 1, [new("Damage", "Tower", 1, true, false, []),
            new("Damage", "FrontInRoomAndRoomAbove", 0, false, true, [])], 0);
        Require(vfx.Supported && vfx.TargetCollections[1].UnitIds.SequenceEqual([2, 3]),
            "Captured status VFX focus was lost before the pending death position drained.");
        var auxiliary = new TrainCombatState([root.Rooms[0], new(1, false, [Unit(5, CombatTeam.Enemy, 8)], [], context),
            new(2, false, root.Rooms[3].Units, [], context)], [], 5, context);
        Require(!CardSpellModel.TestPlay(auxiliary, 0, [new("Damage", "RandomFromAnyRoom", 0, true, false, []),
            new("Heal", "StrongestLastTargetedCharactersRoom", 1, false, true, [], tests: new(true, true, false, false))], 0).Supported,
            "Auxiliary-dependent mandatory global-random room tests returned a modeled cast.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, 0, chain, 0).State) == JsonSerializer.Serialize(child.State),
            "Parallel cross-room spell branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Cross-room spells mutated their parent.");
        Console.WriteLine("CROSS-ROOM-SPELL-CHECKS PASS: tower/front/global HP/random ordering, first-reference rooms, static bounds, remote deaths/heals/capacity and parallel isolation.");
    }

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] native = fixture.GetProperty("CrossRoomTargets").EnumerateArray().ToArray();
        int plays = 0, collections = 0, remoteHeals = 0, multipleEnemyFloors = 0, survivingEnemies = 0;
        var modes = new HashSet<string>();
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardToken card = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            if (rule.Effects.FirstOrDefault()?.Target != "Tower") continue;
            plays++;
            var result = CardSpellModel.Apply(before.Spawn.Train, action.RoomIndex, rule.Effects, action.TargetUnitId, action.CardInstanceId, before.PlayRules);
            Require(result.Supported, "Cross-room target replay was unsupported: " + result.UnsupportedReason);
            // IDs can repeat on later turns, so consume a contiguous live collection group per play.
            int start = collections;
            foreach (SpellTargetCollection predicted in result.TargetCollections)
            {
                Require(collections < native.Length, "The oracle lacks a live target collection.");
                FixtureValue recorded = native[collections++];
                Require(recorded.GetProperty("CardId").GetInt32() == action.CardInstanceId &&
                    recorded.GetProperty("EffectIndex").GetInt32() == predicted.EffectIndex &&
                    recorded.GetProperty("UnitIds").Deserialize<int[]>()!.SequenceEqual(predicted.UnitIds),
                    $"Native cross-room targets differ at action {entry.GetProperty("Index")}, effect {predicted.EffectIndex}: " +
                    $"model [{string.Join(",", predicted.UnitIds)}], native {recorded.GetProperty("UnitIds")}.");
                modes.Add(recorded.GetProperty("Mode").GetString()!);
            }
            Require(collections > start, "The cross-room play did not apply any effects.");
            if (before.Spawn.Train.Rooms.Count(room => room.Units.Any(unit => unit.Team == CombatTeam.Enemy)) >= 2) multipleEnemyFloors++;
            if (actual.Spawn.Train.Rooms.Any(room => room.Units.Any(unit => unit.Team == CombatTeam.Enemy))) survivingEnemies++;
            foreach (RoomCombatState room in before.Spawn.Train.Rooms.Where(room => room.RoomIndex != action.RoomIndex))
                foreach (CombatUnit old in room.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre))
                {
                    CombatUnit? next = actual.Spawn.Train.Rooms.SelectMany(item => item.Units).FirstOrDefault(unit => unit.Id == old.Id);
                    if (next != null && next.MaxHealth == old.MaxHealth + 6 && next.Health == next.MaxHealth) remoteHeals++;
                }
        }
        bool terminal = fixture.GetProperty("ModifierScenario").GetString() == "cross-room-spells";
        Require(plays > 0 && collections == native.Length && remoteHeals > 0 && multipleEnemyFloors > 0 &&
            new[] { "Tower", "FrontInAllRooms", "FrontInRoomAndRoomAbove", "WeakestAllRooms", "StrongestAllRooms", "RandomFromAnyRoom",
                "StrongestLastTargetedCharactersRoom" }.All(modes.Contains), "The cross-room oracle lacks live modes, remote changes or enemies on multiple floors.");
        if (terminal) Require(native.Any(record => record.GetProperty("Mode").GetString() == "StrongestLastTargetedCharactersRoom" &&
            record.GetProperty("LastDead").GetArrayLength() > 0 && record.GetProperty("LastDead")[0].GetBoolean() &&
            record.GetProperty("LastRooms")[0].GetInt32() >= 0 && record.GetProperty("UnitIds").GetArrayLength() > 0) &&
            native.Any(record => record.GetProperty("Mode").GetString() == "StrongestLastTargetedCharactersRoom" &&
                record.GetProperty("LastRooms").GetArrayLength() > 0 && record.GetProperty("LastRooms")[0].GetInt32() == -1 &&
                record.GetProperty("UnitIds").GetArrayLength() == 0), "The oracle lacks retained and cleared dead first references.");
        else Require(survivingEnemies > 0, "The target oracle killed every enemy before verifying intermediate targeting results.");
        Console.WriteLine($"NATIVE-CROSS-ROOM-COVERAGE PASS: {plays} plays, {collections} exact live target collections, {remoteHeals} remote heals/upgrades and {multipleEnemyFloors} multiple-enemy floors.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
