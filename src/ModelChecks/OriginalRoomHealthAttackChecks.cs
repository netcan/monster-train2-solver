using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRoomHealthAttackChecks
{
    private const string Modifier = "RoomStateFriendlyDamagePerOwnMissingHp";
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    internal static void Native(string path, bool withSearch = true, bool deepSearch = false)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d", "Unexpected native version/module.");
        Require(root.GetProperty("OriginalRoomHealthAttackPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original room-health aura battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalRoomHealthAttackDefinitions");
        Require(definitions.ContentEquals(root.GetProperty("OriginalRoomHealthAttackDefinitionsAfter")), "Original definitions changed during the battle.");
        var champion = definitions.GetProperty("Champion").Deserialize<CardPlayRule>()!;
        Require(champion.DataId == "c58f8d77-8578-4d97-bcf1-beb4cdbe2f27" &&
            champion.SpawnUnit is { BaseAttack: 10, Health: 10 }, "Wrong original champion.");
        var upgrades = definitions.GetProperty("Upgrades").EnumerateArray().ToArray();
        Require(upgrades.Length == 3, "Missing original upgrade ranks.");
        var expectedIds = new[] { "da5a3d33-19ee-404c-90c7-1810b08137f8", "fe4cf3be-2c0f-44ad-bdd2-f0418c14e4e0", "93f6e1ca-fc6e-4e23-ab59-207a397c42a0" };
        for (int rank = 0; rank < 3; rank++)
        {
            var upgrade = upgrades[rank].GetProperty("Rule").Deserialize<CardUpgradeModifier>()!;
            var native = upgrades[rank].GetProperty("Modifiers").EnumerateArray().Single();
            Require(upgrade.DataId == expectedIds[rank] && upgrade.ExternalInteractions.Count == 0 &&
                upgrade.SelfAttackRules is { Count: 1 } && upgrade.SelfAttackRules[0].Type == Modifier &&
                upgrade.SelfAttackRules[0].Coefficient == rank + 1 && upgrade.SelfAttackRules[0].Divisor == 1 &&
                native.GetProperty("Type").GetString() == Modifier && native.GetProperty("Int").GetInt32() == rank + 1 &&
                native.GetProperty("Int2").GetInt32() == 1 && !native.GetProperty("DisallowedInDeployment").GetBoolean(),
                "Changed original aura upgrade " + rank);
        }
        int[] ids = root.GetProperty("OriginalRoomHealthAttackIds").Deserialize<int[]>()!;
        var inputs = root.GetProperty("OriginalRoomHealthAttackInputs").EnumerateArray().ToArray();
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 37 && inputs.Length == ids.Length && ids.Distinct().Count() == ids.Length && actions.Length == ids.Length &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing ordered aura lifecycle inputs.");
        var tags = inputs.Select((input, index) => (Tag: input.GetProperty("Tag").GetString()!, Index: index)).ToDictionary(item => item.Tag, item => item.Index);
        BattleTurnState After(string tag) => actions[tags[tag]].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[card]);
        void Attack(string tag, int card, int expected)
        {
            var state = After(tag); var unit = Unit(state, card);
            var room = state.Spawn.Train.Rooms.Single(value => value.Units.Any(actor => actor.Id == unit.Id));
            Require(RelicResourceModel.AttackDamage(unit, room.Context, room) == expected, "Changed contextual attack after " + tag + " for card " + card);
        }
        foreach (var (tag, card, attack) in new[] {
            ("hurt-host-0", 1, 5), ("hurt-source-0", 4, 15), ("hurt-host-1", 2, 10), ("hurt-source-1", 5, 35),
            ("hurt-host-2", 3, 15), ("hurt-source-2", 6, 55), ("heal-host-0", 1, 2), ("reset-source-0", 4, 10),
            ("stack-ranks-1-2", 2, 15), ("move-recipient-in", 1, 6), ("stack-all-ranks", 2, 30),
            ("stack-all-ranks", 1, 12), ("silence-source", 2, 30), ("unsilence-source", 2, 30),
            ("reset-host-1", 2, 0), ("hurt-stacked-host", 2, 30), ("unstack-rank-1", 2, 25),
            ("move-recipient-out", 1, 2), ("kill-rank-1", 1, 0), ("hurt-no-source-0", 1, 0),
            ("move-rank-2", 2, 15), ("move-rank-2", 3, 10), ("kill-rank-3", 2, 0),
            ("hurt-no-source-1", 2, 0), ("reset-host-2", 3, 0), ("hurt-remaining-aura", 3, 10),
            ("hurt-remaining-source", 5, 45), ("late-host", 33, 0), ("hurt-late-host", 33, 10) }) Attack(tag, card, attack);
        Require(Unit(After("silence-source"), 5).Status("silenced")?.Stacks == 1 &&
            Unit(After("unsilence-source"), 5).Status("silenced") == null, "Missing silence/reset controls.");
        for (int rank = 0; rank < 3; rank++)
            Require(Unit(After("rank-" + (rank + 1)), 4 + rank).Modifiers?.SelfAttackRules?.Single().Coefficient == rank + 1,
                "Birth lost the original rank modifier.");
        int queries = 0, positiveAllies = 0; var coefficients = new HashSet<int>();
        foreach (var record in root.GetProperty("UnitSelfAttackQueries").EnumerateArray())
        {
            var room = record.GetProperty("Room").Deserialize<RoomCombatState>()!;
            foreach (var query in record.GetProperty("Attacks").EnumerateArray())
            {
                var unit = room.Units.Single(actor => actor.Id == query.GetProperty("UnitId").GetInt32());
                int actual = query.GetProperty("Attack").GetInt32();
                Require(RelicResourceModel.AttackDamage(unit, room.Context, room) == actual, "Independent native aura getter differs for unit " + unit.Id);
                queries++;
                if (!RoomHealthAttackModel.HasSource(unit) && actual > unit.Attack) positiveAllies++;
            }
            foreach (var unit in room.Units.Where(RoomHealthAttackModel.HasSource))
                foreach (var rule in unit.Modifiers!.SelfAttackRules!.Where(rule => rule.Type == Modifier)) coefficients.Add(rule.Coefficient);
        }
        Require(queries > 0 && positiveAllies > 0 && coefficients.SetEquals(new[] { 1, 2, 3 }), "Missing independent original-rank and recipient queries.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        if (withSearch)
        {
            if (deepSearch) BattleSearchChecks.Native(root, requireZeroUnsupported: true);
            else BattleSearchChecks.RecordedFrontiers(root, requireZeroUnsupported: true);
            FocusedSearch(root);
        }
        Console.WriteLine($"ORIGINAL-ROOM-HEALTH-ATTACK PASS: three unchanged original ranks; {queries} independent native getters ({positiveAllies} positive allies); damage/healing, additive sources, source/recipient movement, silence/reset, source death, late birth, full chain/16 branches; " +
            (withSearch ? "all recorded frontiers and focused serial/parallel search" + (deepSearch ? " plus 128-state stress search." : ".") :
                "lifecycle-only run; frontier/search checks deferred."));
    }

    internal static void SearchOnly(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FocusedSearch(document.RootElement);
    }

    private static void FocusedSearch(FixtureValue root)
    {
        var inputs = root.GetProperty("OriginalRoomHealthAttackInputs").EnumerateArray().ToArray();
        int index = Array.FindIndex(inputs, input => input.GetProperty("Tag").GetString() == "hurt-stacked-host");
        Require(index >= 0 && root.GetProperty("NativeWon").GetBoolean(), "Missing native stacked-source decision.");
        var state = root.GetProperty("Actions")[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        int hostCard = root.GetProperty("OriginalRoomHealthAttackIds")[2].GetInt32();
        var host = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == hostCard);
        var room = state.Spawn.Train.Rooms.Single(value => value.Units.Any(unit => unit.Id == host.Id));
        Require(host.Health == host.MaxHealth && room.Units.SelectMany(unit => unit.Modifiers?.SelfAttackRules ?? Array.Empty<UnitSelfAttackRule>())
            .Where(rule => rule.Type == Modifier).Sum(rule => rule.Coefficient) == 6, "Focused search lost its three original aura sources.");
        string parent = BattleTurnChecks.Comparable(state);
        double Score(BattleTurnState candidate, RoomOutcome _)
        {
            var source = candidate.Spawn.Train.Rooms.SingleOrDefault(value => value.Units.Any(unit => unit.Id == host.Id && unit.Health > 0));
            return source == null ? 0 : -RelicResourceModel.AttackDamage(source.Units.Single(unit => unit.Id == host.Id), source.Context, source);
        }
        var serial = BattleSearchModel.Minimize(state, Score, new BattleSearchOptions(1, 0, 1));
        var parallel = BattleSearchModel.Minimize(state, Score, new BattleSearchOptions(1, 0, 4));
        Require(serial.DecisionStatesVisited == 1 && parallel.DecisionStatesVisited == 1 && serial.Score == -30 && parallel.Score == -30 &&
            !serial.HasUnsupportedAttempts && !parallel.HasUnsupportedAttempts && serial.Steps.Count == 1 && parallel.Steps.Count == 1 &&
            serial.Steps.Single().Action?.TargetUnitId == host.Id &&
            JsonSerializer.Serialize(serial.Steps, ModelJson.Options) == JsonSerializer.Serialize(parallel.Steps, ModelJson.Options) &&
            BattleTurnChecks.Comparable(serial.BestState!) == BattleTurnChecks.Comparable(parallel.BestState!) &&
            parent == BattleTurnChecks.Comparable(state), "Focused aura serial/parallel search differs or mutated its parent.");
        var replay = BattleActionModel.PlayCard(state, serial.Steps.Single().Action!);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(serial.BestState!),
            "Returned aura plan did not reproduce its complete searched successor.");
        Console.WriteLine("ROOM-HEALTH-FOCUSED-SEARCH PASS: native three-source/full-health root, one complete expansion, selected 30 attack, serial/parallel identical path/state, zero unsupported, returned-plan replay and immutable parent. Deep search is separate.");
    }
}
