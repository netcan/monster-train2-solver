using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalAttackOverridesChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
        int host = index is 6 or 14 ? 0 : index is 7 or 13 or 15 or 27 or 28 ? 1 : 2;
        int target = index >= 6 && index != 9 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
        var action = new PlayCardAction(ids[index], index == 3 ? 1 : 0, playerPosition: index < 4 ? (index == 3 ? 0 : index) : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalAttackOverridesPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original attack override witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalAttackOverridesIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 29 && actions.Length == 29, "Missing override priority, raw buff, wound, maximum HP or removal controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int host) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[host]);
        foreach (int i in new[] { 6, 7, 10, 19 })
        {
            BattleTurnState before = Before(i), after = After(i);
            var card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(card.DataId == (i is 6 or 19 ? "17fa5800-9c48-41f5-be97-86db8d4c5f9e" : "d42625f2-9c06-45ff-a6d5-38ddc6ce478a") &&
                rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost &&
                Actor(after, i == 6 ? 0 : i == 7 ? 1 : 2).EquipmentCards!.Contains(ids[i]), "Original Irontongue/Equalizer identity, paid cost or equipment binding differs.");
        }
        Require(Actor(After(11), 2).Modifiers!.AttackDamage == 17 && Actor(After(11), 2).Modifiers!.DamageBuff == 100 &&
            Actor(After(12), 2).Status("buff")?.Stacks == 2, "Override capture must retain hidden raw attack and positive buff/status fields.");
        Require(Actor(Before(15), 1).Health < Actor(Before(15), 1).MaxHealth, "Equalizer control is not wounded.");
        Require(Actor(Before(18), 2).Status("armorattack")?.Stacks == 1 && (Actor(Before(18), 2).Status("armor")?.Stacks ?? 0) == 0 &&
            Actor(Before(18), 2).Status("equalizer")?.Stacks == 1, "Zero-armor priority control is missing.");
        var expected = new Dictionary<int, int> { [14] = 30, [15] = 100, [16] = 25, [18] = 0, [20] = 30, [22] = 0, [24] = 110, [26] = 121, [28] = 120 };
        foreach (var pair in expected)
        {
            BattleTurnState before = Before(pair.Key), after = After(pair.Key);
            int host = pair.Key == 14 ? 0 : pair.Key is 15 or 28 ? 1 : 2;
            CombatUnit actor = Actor(before, host);
            Require(actor.Attack == pair.Value, "Projected attack does not follow native fixed/armor/max HP priority.");
            foreach (CombatUnit enemy in before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy))
            {
                CombatUnit changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == enemy.Id);
                int damage = enemy.Health + (enemy.Status("armor")?.Stacks ?? 0) - changed.Health - (changed.Status("armor")?.Stacks ?? 0);
                bool selected = enemy.AssetKey == "ShieldGuardT1_BasicNonAttack" || actor.Status("sweep") != null;
                Require(damage == (selected ? pair.Value : 0), "Independent native victim HP/armor does not prove the expected attack or Sweep.");
            }
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Override attack changed the neighboring host.");
        }
        Require(Actor(After(21), 2).Status("fixedattack") == null && Actor(After(23), 2).Status("armorattack") == null &&
            Actor(After(25), 2).Status("equalizer") == null && Actor(After(25), 2).Modifiers!.DamageBuff == 100 &&
            Actor(After(27), 1).MaxHealth == 120, "Status removals lost hidden buffs or maximum HP growth did not update Equalizer.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-ATTACK-OVERRIDES PASS: unchanged Irontongue/Equalizer, native victim damage proves fixed > armor > maximum HP priority, zero armor, hidden buffs, removals, wounded/max HP growth and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
