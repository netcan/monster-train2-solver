using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AttackBuffDamageTargetChecks
{
    private const string DarkDealId = "6a6c4a72-3e32-4995-8070-68d9240de0d5";

    internal static void Native(FixtureValue fixture)
    {
        var (row, before, action) = fixture.GetProperty("Actions").EnumerateArray()
            .Select(row => (Row: row, Before: row.GetProperty("Before").Deserialize<BattleTurnState>()!,
                Action: row.GetProperty("Action").Deserialize<PlayCardAction>()!))
            .Single(play => play.Before.Spawn.Train.Context!.FindCard(play.Action.CardInstanceId)?.DataId == DarkDealId);
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var rule = before.PlayRules!.Cards.Single(card => card.DataId == DarkDealId);
        Require(rule.Effects is [
            { Type: "AddStatus", Target: "DropTargetCharacter", AllowPlayer: true, AllowEnemy: true, Value: 100,
                Statuses: [{ Id: "buff", Stacks: 7, ParamInt: 2 }] },
            { Type: "Damage", Target: "LastTargetedCharacters", AllowPlayer: true, AllowEnemy: true, Value: 5, Range: null }],
            "Missing unchanged native Dark Deal definition with ordered Rage and damage.");
        CombatStatus rage = rule.Effects[0].Statuses[0];
        int damage = rule.Effects[1].Value;
        int cardId = action.CardInstanceId;
        var a = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
            .Units.Single(unit => unit.Id == action.TargetUnitId);
        var b = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
            .Units.Single(unit => unit.Id == action.TargetUnitId);
        Require(a is { Team: CombatTeam.Player, IsPyre: false } &&
            (b.Status(rage.Id)?.Stacks ?? 0) - (a.Status(rage.Id)?.Stacks ?? 0) == rage.Stacks &&
            b.Attack == a.Attack + rage.Stacks * rage.ParamInt && b.Health == a.Health - damage,
            "Dark Deal did not grant its original Rage and deal its original damage to the recorded ally.");
        var recorded = BattleActionModel.PlayCard(before, action);
        Require(recorded.Supported && NativeBranchObservationChecks.Key(recorded.State!) == NativeBranchObservationChecks.Key(actual),
            "Recorded Dark Deal full native successor/RNG differs.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        foreach (bool tactical in new[] { false, true })
        {
            BattleTurnState current = before;
            SupportedActionCandidate? choice = null;
            int prefix = 0;
            while (prefix < 16 && (choice = Choose(current, tactical)) != null && choice.Action.CardInstanceId != cardId)
            {
                current = choice.Result.State!;
                prefix++;
            }
            Console.WriteLine($"ATTACK-BUFF-DAMAGE-CHOICE tactical={tactical} prefix={prefix} card={choice?.Action.CardInstanceId} target={choice?.Action.TargetUnitId}");
            var target = current.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == choice?.Action.TargetUnitId);
            Require(choice?.Action.CardInstanceId == cardId && target is { Team: CombatTeam.Player, IsPyre: false } &&
                choice.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == target.Id && unit.Health > 0 && unit.Attack > target.Attack),
                "A positive Rage spell with a damage cost still buffs an enemy or kills its ally.");
            var direct = BattleActionModel.PlayCard(current, choice.Action);
            Require(direct.Supported && keys.Key(direct.State!) == keys.Key(choice.Result.State!) &&
                BattleActionModel.EnumerateActions(current, cardId, includeAbilities: false).SupportedCandidates.Any(candidate =>
                    current.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == candidate.Action.TargetUnitId && unit.Team == CombatTeam.Enemy)),
                "Buff-damage seed pruned its legal enemy branch or changed its complete successor/RNG.");
            string immutable = keys.Key(current);
            Parallel.For(0, 4, _ => Require(Choose(current, tactical)?.Action.TargetUnitId == target.Id, "Parallel buff-damage target differs."));
            Require(keys.Key(current) == immutable && keys.Key(before) == parent, "Buff-damage policy mutated its native parent.");
        }
        Console.WriteLine($"ATTACK-BUFF-DAMAGE-TARGET PASS: unchanged native Rage{rage.Stacks}/damage{damage} full successor/RNG. Both seeds choose a surviving strengthened ally after legal model prefixes, retain legal enemy branches, and repeat eight immutable choices.");
        LethalControls();
    }

    private static void LethalControls()
    {
        foreach (bool safeAlly in new[] { false, true })
        {
            var rng = UnityRng.Seed(424245);
            var context = new CombatContext(new CardCycleState([new(1, "paid-buff")], [], [], rng, 0, []), rng, 0, 2, 10);
            var fragile = new CombatUnit(2, "fragile", CombatTeam.Player, 100, 5, 5, true, false, false, []);
            var durable = new CombatUnit(3, "durable", CombatTeam.Player, 10, 10, 10, true, false, false, []);
            var enemy = new CombatUnit(4, "enemy", CombatTeam.Enemy, 10, 100, 100, true, false, false, []);
            var rooms = Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == 0 ? safeAlly ? [fragile, durable, enemy] : [fragile, enemy] : index == 3 ?
                    [new CombatUnit(5, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])] : [], [], context)).ToArray();
            var train = new TrainCombatState(rooms, [], 6, context);
            var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0,
                false, rng, 6, [], 0, false, 2, 1, 1, []);
            var rule = new CardPlayRule("paid-buff", "paid-buff", 0, "Spell", "Discard", null, [],
                [new("AddStatus", "DropTargetCharacter", 0, true, true, [new("buff", 7, 2, displayCategory: "Positive")]),
                new("Damage", "LastTargetedCharacters", 5, true, true, [])], cardType: "Spell");
            var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [],
                [new("Standby", []), new("Exhausted", []), new("Purged", [])], [],
                new BattlePlayRules(Enumerable.Range(0, 4).Select(index => new RoomPlayRule(index,
                    index == 3 ? 0 : 5, 7, true, index == 3, index == 3)).ToArray(), [rule]));
            using var keys = new BattleStateKeys();
            string parent = keys.Key(root);
            foreach (bool tactical in new[] { false, true })
            {
                var choice = Choose(root, tactical);
                Require(safeAlly ? choice?.Action.TargetUnitId == 3 : choice == null,
                    "Buff damage must skip the lethal100ATK ally and defer if no surviving recipient exists.");
                if (choice != null)
                {
                    var target = choice.Result.State!.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 3);
                    Require(target.Health == 5 && target.Attack == 24, "Surviving buff recipient lost its actual5HP/24ATK successor.");
                }
                var legal = BattleActionModel.EnumerateActions(root);
                Require(legal.SupportedActions.Any(action => action.TargetUnitId == 2) &&
                    legal.SupportedActions.Any(action => action.TargetUnitId == 4) && keys.Key(root) == parent,
                    "Seed filter pruned legal lethal/enemy casts or mutated its root.");
            }
        }
        Console.WriteLine("ATTACK-BUFF-DAMAGE-LETHAL PASS: both seeds skip a lethal high-attack recipient, choose an actual surviving gain or defer; all lethal/enemy branches remain legal, complete roots unchanged.");
    }
    private static SupportedActionCandidate? Choose(BattleTurnState source, bool tactical) => tactical
        ? BattleTacticalPolicy.Choose(source, out _) : BattleHeuristicPolicy.Choose(source, out _);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
