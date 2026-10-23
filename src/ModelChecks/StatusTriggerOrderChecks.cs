using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StatusTriggerOrderChecks
{
    internal static void Native(FixtureValue fixture, bool damage = false)
    {
        Require(fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("ContextUnchanged").GetBoolean() &&
            fixture.GetProperty("FrameBefore").ContentEquals(fixture.GetProperty("FrameAfter")) &&
            fixture.GetProperty("RngBefore").ContentEquals(fixture.GetProperty("RngAfter")), "Native sort calibration changed live state/RNG/frame.");
        var metadata = fixture.GetProperty("Metadata").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!);
        Require(metadata.Count + fixture.GetProperty("AbsentModelIds").GetArrayLength() == 68 &&
            metadata.Count == 66 && fixture.GetProperty("AbsentModelIds").EnumerateArray().Select(value => value.GetString())
                .OrderBy(value => value).SequenceEqual(new[] { "heal multiplier", "inedible" }) &&
            metadata.All(pair => StatusTriggerOrderModel.TriggerOrder(pair.Key) == pair.Value.GetProperty("Order").GetInt32()),
            "Native status trigger-order catalog differs from the model.");
        int rows = 0, changed = 0, titaniteFirst = 0, shieldFirst = 0;
        var seen = new HashSet<string>();
        foreach (FixtureValue row in fixture.GetProperty("Rows").EnumerateArray())
        {
            int count = row.GetProperty("Count").GetInt32(), sample = row.GetProperty("Sample").GetInt32();
            string? zero = row.GetProperty("ZeroStackId").GetString();
            var dictionary = row.GetProperty("DictionaryOrder").EnumerateArray().Select(value =>
                new CombatStatus(value.GetString()!, value.GetString() == zero ? 0 : 1)).ToArray();
            string[] actual = row.GetProperty("SortedOrder").EnumerateArray().Select(value => value.GetString()!).ToArray();
            string[] modeled = StatusTriggerOrderModel.Order(dictionary).Select(status => status.Id).ToArray();
            Require(count is >= 2 and <= 68 && sample is >= 0 and < 32 && dictionary.Length == count + (zero == null ? 0 : 1) &&
                actual.Length == count && actual.Distinct().Count() == count && modeled.SequenceEqual(actual) &&
                seen.Add($"{count}:{sample}"), "Native all-status runtime sort differs from the model.");
            string[] expectedStable = dictionary.Where(status => status.Stacks > 0).OrderBy(status =>
                StatusTriggerOrderModel.TriggerOrder(status.Id)).Select(status => status.Id).ToArray();
            if (!actual.SequenceEqual(expectedStable)) changed++;
            if (Array.IndexOf(actual, "titanite") < Array.IndexOf(actual, "damage shield")) titaniteFirst++; else shieldFirst++;
            rows++;
        }
        Require(rows == (metadata.Count - 1) * 32 && changed > 0 && titaniteFirst > 0 && shieldFirst > 0,
            "Missing large-list native ordering witnesses or controls.");
        if (damage) Damage(fixture, metadata);
        Console.WriteLine($"STATUS-TRIGGER-ORDER PASS: {rows} native dictionary/runtime sorts, 2–{metadata.Count} active statuses, zero-stack controls, all nonzero priorities and immutable native state/RNG/frame.");
    }

    private static void Damage(FixtureValue fixture, Dictionary<string, FixtureValue> metadata)
    {
        int rows = 0, reduced = 0, raw = 0;
        var seen = new HashSet<string>();
        foreach (FixtureValue row in fixture.GetProperty("DamageRows").EnumerateArray())
        {
            int count = row.GetProperty("Count").GetInt32(), sample = row.GetProperty("Sample").GetInt32();
            var registry = row.GetProperty("StatusOrder").EnumerateArray().Select(value => new CombatStatus(value.GetString()!, 1,
                metadata[value.GetString()!].GetProperty("ParamInt").GetInt32(), removeWhenTriggered: value.GetString() is "damage shield" or "armor")).ToArray();
            var target = new CombatUnit(1, "target", CombatTeam.Enemy, 0, 1000, 1000, false, false, false, registry, statusRegistry: registry);
            var actor = new CombatUnit(2, "attacker", CombatTeam.Player, 0, 1000, 1000, false, false, false,
                registry.Any(status => status.Id == "piercing") ? [new("piercing", 1, 1)] : []);
            var context = new CombatContext(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 8, 10,
                statistics: BattleStatistics.Empty());
            var room = new RoomCombatState(0, false, [target, actor], [], context);
            string immutable = System.Text.Json.JsonSerializer.Serialize(room, ModelJson.Options);
            var result = RoomCombatModel.ApplyCardDamage(room, 1, 21, attackerUnitId: 2);
            Require(result.Supported, result.UnsupportedReason ?? "Large-status damage unsupported.");
            int blocked = row.GetProperty("ShieldBlocked").GetInt32();
            CombatUnit after = result.State!.Units.Single(unit => unit.Id == 1);
            Require(after.Health == 1000 && after.Status("damage shield") == null && after.Status("titanite")?.Stacks == 1 &&
                result.State.Context!.Statistics!.LastAttackDamageDealt == blocked && row.GetProperty("ShieldDamage").GetInt32() == 0 &&
                System.Text.Json.JsonSerializer.Serialize(room, ModelJson.Options) == immutable && seen.Add($"{count}:{sample}"),
                "Large native shield damage/accounting/root differs from the complete model path.");
            if (blocked == 11) reduced++; else if (blocked == 21) raw++; else throw new InvalidOperationException("Unexpected native shield stage result.");
            if (count == 24 && sample == 0)
            {
                Parallel.For(0, 16, _ => {
                    var branch = RoomCombatModel.ApplyCardDamage(room, 1, 21, attackerUnitId: 2);
                    Require(branch.Supported && branch.State!.Context!.Statistics!.LastAttackDamageDealt == blocked &&
                        System.Text.Json.JsonSerializer.Serialize(room, ModelJson.Options) == immutable,
                        "Large-status parallel branch changed the shared root or native accounting.");
                });
                foreach (string? absent in new string?[] { null, "heal multiplier", "inedible" })
                {
                    var uncalibrated = registry.Select(status => status.Id == "dazed" && absent != null
                        ? new CombatStatus(absent, 1) : status).ToArray();
                    var rejectedTarget = new CombatUnit(1, "target", CombatTeam.Enemy, 0, 1000, 1000, false, false, false,
                        uncalibrated, statusRegistry: absent == null ? null : uncalibrated);
                    var rejected = RoomCombatModel.ApplyCardDamage(new RoomCombatState(0, false, [rejectedTarget, actor], [], context), 1, 21, attackerUnitId: 2);
                    Require(!rejected.Supported && rejected.UnsupportedReason!.Contains("calibrated native status list"),
                        "Missing status dictionary or absent native definition bypassed the large-list guard.");
                }
            }
            rows++;
        }
        Require(rows == 192 && reduced > 0 && raw > 0, "Missing native large-status damage controls.");
        Console.WriteLine($"LARGE-STATUS-DAMAGE PASS: {rows} native stages and complete model health, shield consumption, last-attack damage and immutable roots; both orders, sixteen parallel branches and rejected absent definitions.");
    }
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
