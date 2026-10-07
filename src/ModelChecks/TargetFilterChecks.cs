using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TargetFilterChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(92);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, int health, string subtype, bool? boss = false, CombatStatus[]? statuses = null, bool attacks = true) =>
            new(id, "unit" + id, CombatTeam.Enemy, attacks ? 3 : 0, health, 10, attacks, false, false, statuses ?? [],
                subtypes: [subtype], modifiers: new(attacks ? 3 : 0, 0, 0, 1, 1, true, false, []), isBoss: boss);
        CombatUnit[] units = [Unit(1, 3, "warrior", statuses: [new("armor", 2, 1), new("regen", 1)]),
            Unit(2, 10, "warrior", statuses: [new("immune", 1)]), Unit(3, 7, "mage", true), Unit(4, 6, "warrior", statuses: [new("armor", 1, 1)]),
            Unit(6, 4, "shield", attacks: false)];
        var room = new RoomCombatState(0, false, units, [], context);
        var train = new TrainCombatState([room, new(1, false, [Unit(5, 5, "warrior")], [], context), new(2, false, [], [], context),
            new(3, false, [new(7, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [], isBoss: false)], [], context)], [], 7, context);
        CardTargetFilters Filter(string health = "Both", string[]? required = null, string[]? excluded = null, bool boss = false,
            string subtype = "", string[]? excludeTypes = null) => new(health, required ?? [], excluded ?? [], boss, subtype, excludeTypes ?? []);
        CardActionEffect Effect(string mode, CardTargetFilters filter, string type = "Damage") => new(type, mode, 0, true, false, [], filters: filter);
        int[] Targets(string mode, CardTargetFilters filter, int[]? last = null, int position = -1) =>
            CardTargetModel.Collect(train, 0, Effect(mode, filter), last ?? [], CombatTeam.Enemy, position).UnitIds.ToArray();
        Require(Targets("Room", Filter(required: ["armor", "regen"])).SequenceEqual([1]), "Required status filters are not ANDed.");
        Require(Targets("FrontInRoom", Filter("Undamaged")).SequenceEqual([2]) && Targets("BackInRoom", Filter("Damaged", subtype: "warrior")).SequenceEqual([4]),
            "Health/subtype filters ran after positional selection.");
        Require(Targets("Weakest", Filter(excluded: ["armor"])).SequenceEqual([6]), "Excluded statuses ran after HP selection.");
        Require(Targets("Tower", Filter(subtype: "warrior", excludeTypes: ["warrior"])).SequenceEqual([1, 5, 2, 4]),
            "Required subtype did not bypass native full-filter exclusions or changed tower ordering.");
        Require(Targets("DropTargetCharacter", Filter("Undamaged", required: ["silenced"]), position: 0).SequenceEqual([1]),
            "Drop override incorrectly applied health/status masks.");
        Require(Targets("DropTargetCharacter", Filter(subtype: "warrior", excludeTypes: ["warrior"]), position: 0).Length == 0,
            "Drop override bypassed excluded subtype checks.");
        Require(Targets("Room", Filter(boss: true)).SequenceEqual([1, 2, 4, 6]) && units[2].EndsBattleOnDeath == false,
            "Boss exclusion was inferred from terminal death instead of IsAnyBoss state.");
        Require(Targets("DropTargetCharacter", Filter(boss: true), position: 2).Length == 0,
            "Drop override admitted an excluded boss.");
        var impossible = Filter("Undamaged", ["silenced"], ["armor"], true, "missing");
        Require(Targets("LastTargetedCharacters", impossible, [1, 3]).SequenceEqual([1, 3]) &&
            Targets("StrongestLastTargetedCharacters", impossible, [1, 3]).SequenceEqual([3]), "Last-target modes applied masks they natively bypass.");
        Require(Targets("FrontInAllRooms", impossible).SequenceEqual([1, 5]) &&
            Targets("FrontInRoomAndRoomAbove", Filter("Undamaged")).SequenceEqual([2]), "Physical fronts and filtered room/above fronts were conflated.");
        Require(Targets("StrongestLastTargetedCharactersRoom", Filter(boss: true), [3]).SequenceEqual([2]),
            "Strongest-last-room did not filter its room candidates.");
        var random = Effect("RandomFromAnyRoom", Filter("Undamaged"));
        TrainSpellResult randomChild = CardSpellModel.Apply(train, 0, [random], 0);
        Require(randomChild.Supported && randomChild.TargetCollections[0].UnitIds.SequenceEqual([2]) && randomChild.State!.Context!.BattleRng.Equals(rng.Next()),
            "Filtered single-candidate random targeting skipped or misused the gameplay draw.");
        Require(CardSpellModel.Apply(train, 0, [Effect("RandomFromAnyRoom", Filter(required: ["silenced"]))], 0).State!.Context!.BattleRng.Equals(rng),
            "A filtered empty random pool consumed gameplay RNG.");
        Require(CardSpellModel.TestPlay(train, 0, [Effect("RandomInRoom", Filter(subtype: "warrior"), "BuffAttack")], 0).Supported &&
            !CardSpellModel.TestPlay(train, 0, [Effect("RandomInRoom", Filter(), "BuffAttack")], 0).Supported,
            "Random attack legality inspected filtered-out incapable candidates.");
        var unknown = new RoomCombatState(0, false, [Unit(8, 7, "warrior", null)], [], context);
        Require(!CardTargetModel.Collect(unknown, Effect("Room", Filter(boss: true)), []).Supported &&
            CardTargetModel.Collect(unknown, Effect("LastTargetedCharacters", Filter(boss: true)), [8]).Supported,
            "Missing boss metadata was guessed or required in a bypassing mode.");
        var sourceList = new[] { "armor" }; var immutable = Filter(required: sourceList); sourceList[0] = "silenced";
        Require(immutable.RequiredStatuses[0] == "armor" && !CardTargetModel.Collect(room, Effect("Room", Filter("unknown")), []).Supported,
            "Target filters retained a caller-owned list or accepted an unknown health rule.");
        Require(UnitAttackModel.Apply(room, 3, 1).State!.Units.Single(unit => unit.Id == 3).IsBoss == true &&
            UnitHealthModel.Apply(room, 3, 1).State!.Units.Single(unit => unit.Id == 3).IsBoss == true &&
            RoomCombatModel.ApplyCardDamage(room, 3, 1).State!.Units.Single(unit => unit.Id == 3).IsBoss == true,
            "Unit changes lost independent boss identity.");
        string parent = JsonSerializer.Serialize(train), expected = JsonSerializer.Serialize(randomChild.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(train, 0, [random], 0).State) == expected,
            "Parallel filtered target branches differ."));
        Require(JsonSerializer.Serialize(train) == parent, "Filtered collection or spell simulation mutated its parent.");
        Console.WriteLine("TARGET-FILTER-CHECKS PASS: health/status/subtype/boss masks, native bypasses/precedence, candidate ordering, random legality/draws, metadata and parallel isolation.");
    }

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] native = fixture.GetProperty("FilteredTargets").EnumerateArray().ToArray();
        int collections = 0, plays = 0, dropBypass = 0, frontBypass = 0, lastBypass = 0, subtypePrecedence = 0, bossExcluded = 0, emptyRandom = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == dataId);
            if (rule.Effect != "Spell") continue;
            plays++;
            TrainSpellResult result = CardSpellModel.Apply(before.Spawn.Train, action.RoomIndex, rule.Effects, action.TargetUnitId, action.CardInstanceId, before.PlayRules);
            Require(result.Supported, "Filtered native spell replay unsupported: " + result.UnsupportedReason);
            CombatUnit[] units = before.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            foreach (SpellTargetCollection collection in result.TargetCollections)
            {
                Require(collections < native.Length, "The native filtered target oracle is missing a collection.");
                FixtureValue record = native[collections++];
                CardActionEffect effect = rule.Effects[collection.EffectIndex];
                Require(record.GetProperty("CardId").GetInt32() == action.CardInstanceId && record.GetProperty("EffectIndex").GetInt32() == collection.EffectIndex &&
                    record.GetProperty("UnitIds").Deserialize<int[]>()!.SequenceEqual(collection.UnitIds),
                    $"Native filtered targets differ at action {entry.GetProperty("Index")}, effect {collection.EffectIndex}.");
                if (effect.Target == "DropTargetCharacter" && effect.Filters!.RequiredStatuses.Contains("silenced") && collection.UnitIds.Count > 0) dropBypass++;
                if (effect.Target == "FrontInAllRooms" && effect.Filters!.RequiredStatuses.Contains("silenced") && collection.UnitIds.Count > 0) frontBypass++;
                if (effect.Target == "LastTargetedCharacters" && effect.Filters!.Subtype.Length > 0 && collection.UnitIds.Count > 0) lastBypass++;
                if (effect.Filters!.Subtype.Length > 0 && effect.Filters.ExcludedSubtypes.Contains(effect.Filters.Subtype) && collection.UnitIds.Count > 0) subtypePrecedence++;
                if (effect.Filters.IgnoreBosses && effect.Target == "RandomFromAnyRoom" && units.Any(unit => unit.IsBoss == true) && collection.UnitIds.Count == 0) bossExcluded++;
                if (CardTargetModel.IsRandom(effect.Target) && collection.UnitIds.Count == 0) emptyRandom++;
            }
        }
        Require(plays > 0 && collections == native.Length && dropBypass > 0 && frontBypass > 0 && lastBypass > 0 && subtypePrecedence > 0 && bossExcluded > 0 && emptyRandom > 0,
            "The filtered oracle lacks actual bypasses, subtype precedence, boss exclusion or empty random pools.");
        Console.WriteLine($"NATIVE-TARGET-FILTER-COVERAGE PASS: {plays} spells, {collections} exact live collections, {dropBypass} drop bypasses, {frontBypass} front bypasses, {lastBypass} last bypasses, {subtypePrecedence} subtype precedence, {bossExcluded} boss exclusions and {emptyRandom} empty random pools.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
