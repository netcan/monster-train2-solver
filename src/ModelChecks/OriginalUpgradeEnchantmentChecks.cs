using MonsterTrain2Poju.Model;

internal static class OriginalUpgradeEnchantmentChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalUpgradeEnchantmentPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete upgrade enchantment battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        Require(root.GetProperty("DeathDissolveSettlementEnabled").GetBoolean() &&
            root.GetProperty("CardAnimationSettlement").GetProperty("Enabled").GetBoolean(), "Missing explicit native settlement protocol.");
        var settlements = root.GetProperty("DeathDissolveSettlements").EnumerateArray().ToArray();
        Require(settlements.Length > 0 && settlements.All(row => row.GetProperty("Completed").GetBoolean() &&
            row.GetProperty("PendingAfter").GetInt32() == 0 && row.GetProperty("Error").ValueKind == MonsterTrain2Poju.Fixtures.FixtureKind.Null),
            "Native dissolve callbacks did not settle before the existing removal flush.");
        var definitions = root.GetProperty("OriginalUpgradeEnchantmentDefinitions").EnumerateArray().ToArray();
        string[] originalIds = { "6c3eb0bb-4871-438d-9b5c-fcef1f94bf70", "983495d0-11cb-49d7-9f3e-8531c6f18676", "462f5b87-cace-447d-8818-3d4b7bc8eb7f" };
        Require(definitions.Length == 3, "Missing three original character definitions.");
        for (int index = 0; index < 3; index++)
        {
            var definition = definitions[index];
            Require(definition.GetProperty("Id").GetString() == originalIds[index] && definition.GetProperty("CostType").GetString() == "NonPlayable" &&
                definition.GetProperty("Attack").GetInt32() == 0 && definition.GetProperty("Health").GetInt32() == 10 &&
                definition.GetProperty("Size").GetInt32() == 1, "Original Darkshard unit or nonplayable card definition changed.");
            var trigger = definition.GetProperty("Triggers").EnumerateArray().Single(row => row.GetProperty("Kind").GetString() == "AfterSpawnEnchant");
            var effect = trigger.GetProperty("Effects").EnumerateArray().Single(); var raw = effect.GetProperty("Raw");
            Require(raw.GetProperty("Type").GetString() == "CardEffectEnchantCardUpgrade" && raw.GetProperty("Target").GetString() == "Room" &&
                raw.GetProperty("Team").GetString() == "Heroes" && effect.GetProperty("Upgrade").GetProperty("Stats").GetProperty("Damage").GetInt32() == -(3 + 2 * index),
                "Original upgrade enchantment targeting or reduction changed.");
        }
        int[] ids = root.GetProperty("OriginalUpgradeEnchantmentIds").Deserialize<int[]>()!;
        var rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 12 && ids.Distinct().Count() == 12 && rows.Length == 12 && rows.Select(row =>
            row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing twelve ordered native controls.");
        BattleTurnState Before(int index) => rows[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int cardId) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        CombatUnit[] Enemies(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        var enemies = Enemies(Before(2)); Require(enemies.Length >= 2, "Missing multiple native aura targets.");
        int[] buffs = { -3, -8, -15, -12, -15, -10, -15, -12, -7 };
        int[] counts = { 1, 2, 3, 2, 3, 2, 3, 2, 1 };
        for (int index = 2; index <= 10; index++)
        {
            var after = After(index);
            foreach (var original in enemies)
            {
                var target = Enemies(after).Single(unit => unit.Id == original.Id);
                Require(target.Modifiers!.AttackDamage == original.Modifiers!.AttackDamage && target.Modifiers.AttackDamageAdded == original.Modifiers.AttackDamageAdded &&
                    target.Modifiers.DamageBuff == buffs[index - 2] && target.Modifiers.Upgrades.Count == counts[index - 2] &&
                    target.Modifiers.Upgrades.All(upgrade => upgrade.Stats.Damage == 0 && upgrade.DamageBuff is -3 or -5 or -7),
                    "Aura did not preserve base attack or exact per-source reduction records at input " + index);
            }
        }
        for (int index = 0; index < 3; index++)
        {
            var state = After(4); var actor = Actor(state, ids[2 + index]);
            var rule = actor.Triggers.Single(trigger => trigger.Kind == "AfterSpawnEnchant").Effects.Single().Enchantment!;
            Require(actor.AssetKey == definitions[index].GetProperty("Character").GetString() && actor.Modifiers!.Upgrades.Count == 0 &&
                rule.Bound && rule.Upgrade!.Stats.Damage == -(3 + 2 * index) && rule.State.PrimaryTargets.Count == enemies.Length &&
                rule.State.PrimaryTargets.All(entry => entry.IsEnchanted && entry.NextAction == 0 && entry.AppliedUpgrade is { Stats.Damage: 0 } applied &&
                    applied.DamageBuff == -(3 + 2 * index)), "Missing retained runtime upgrade descriptor for original source " + index);
            var card = state.Spawn.Train.Context!.FindCard(ids[2 + index])!;
            Require(card.DataId != originalIds[index] && card.Permanent.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 0,
                "Control proxy misrepresented an original playable card or aura wrote back to its source card.");
        }
        Require(Actor(After(5), ids[2]).Status("muted")?.Stacks == 1 && Actor(After(6), ids[2]).Status("muted") == null &&
            After(7).Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Any(unit => unit.SpawnerCardId == ids[3]) &&
            After(8).Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.SpawnerCardId == ids[3]),
            "Missing seal/reset or actual floor movement controls.");
        Require(Before(11).Spawn.Turn == 2 && Enemies(Before(11)).Any(unit => unit.Modifiers!.DamageBuff == -7) &&
            Enemies(After(11)).All(unit => unit.Modifiers!.DamageBuff == 0 && unit.Modifiers.Upgrades.Count == 0),
            "Aura failed to reach new-wave enemies or restore them on source death.");
        var retained = Actor(After(4), ids[2]).Triggers.Single(trigger => trigger.Kind == "AfterSpawnEnchant")
            .Effects.Single().Enchantment!.State.PrimaryTargets.First();
        using (var keys = new BattleStateKeys())
            Require(keys.SnapshotKey(retained) != keys.SnapshotKey(new EnchantmentTarget(retained.UnitId, retained.IsEnchanted, retained.NextAction)),
                "Search pruning ignored the retained applied-upgrade descriptor.");
        foreach (var row in rows)
            Require(!BattleActionModel.EnumerateActions(row.GetProperty("Before").Deserialize<BattleTurnState>()!).HasUnsupportedAttempts,
                "Recorded enchantment decision has unsupported candidates.");
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.FrontierDiagnostics(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-UPGRADE-ENCHANTMENT PASS: three unchanged native Darkshard units through control proxies, independent buff-channel records, stacking/seal/reset/movement/death/new wave/preview, full chain/16 branches and serial/parallel search.");
    }
}
