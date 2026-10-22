using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TitaniteCombinationChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("ContextUnchanged").GetBoolean() &&
            fixture.GetProperty("FrameBefore").ContentEquals(fixture.GetProperty("FrameAfter")) &&
            fixture.GetProperty("RngBefore").ContentEquals(fixture.GetProperty("RngAfter")), "Native decision/RNG/frame changed.");
        var metadata = fixture.GetProperty("Metadata").EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!);
        Require(metadata["titanite"].GetProperty("Stage").GetString() == "OnPreAttackedDamageShield" &&
            metadata["damage shield"].GetProperty("Stage").GetString() == "OnPreAttackedDamageShield" &&
            metadata["titanite"].GetProperty("Order").GetInt32() == 0 &&
            metadata["damage shield"].GetProperty("Order").GetInt32() == 0, "Unexpected native shield stage/order.");
        var sortControls = fixture.GetProperty("SortControls").EnumerateArray().ToArray();
        Require(sortControls.Select(control => control.GetProperty("Count").GetInt32()).SequenceEqual(new[] { 2, 5, 8, 16, 17, 32 }),
            "Incomplete native sorting controls.");
        foreach (var control in sortControls)
        {
            int count = control.GetProperty("Count").GetInt32();
            int[] order = control.GetProperty("Order").EnumerateArray().Select(value => value.GetInt32()).ToArray();
            Require(order.Length == count && order.Distinct().Count() == count &&
                (count > 16 || order.SequenceEqual(Enumerable.Range(0, count))), "Native small-list status sort changed.");
        }
        var seen = new HashSet<string>(); int orderWitnesses = 0, unitPiercingWitnesses = 0;
        foreach (var row in fixture.GetProperty("Rows").EnumerateArray())
        {
            string source = row.GetProperty("Source").GetString()!;
            int damage = row.GetProperty("Damage").GetInt32();
            int Count(string id) => row.GetProperty(id switch { "titanite" => "Titanite", "damage shield" => "Shield", "titanskin" => "Skin", "armor" => "Armor", _ => throw new InvalidOperationException("Unexpected defensive status: " + id) }).GetInt32();
            string[] order = row.GetProperty("StatusOrder").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var registry = order.Select(id => new CombatStatus(id, id == "piercing" ? 1 : Count(id),
                metadata[id].GetProperty("ParamInt").GetInt32(), removeWhenTriggered: id is "damage shield" or "armor")).ToArray();
            var target = new CombatUnit(1, "target", CombatTeam.Enemy, 0, 1000, 1000, false, false, false,
                registry.Where(status => status.Id != "piercing").ToArray(), statusRegistry: registry.Where(status => status.Id != "piercing").ToArray());
            var actor = new CombatUnit(2, "attacker", CombatTeam.Player, 0, 1000, 1000, false, false, false,
                source == "unit-piercing" ? [new("piercing", 1, 1)] : []);
            CardMaskDefinition definition = new("damage", "Spell", "Common", false, false, false,
                [], [], [], [], null, false, 0, 0, false, false, false);
            var descriptor = new CardMaskDescriptor(definition, 0, source == "card-piercing"
                ? [new("CardTraitIgnoreArmor", "CardTraitIgnoreArmor", 0, 0, true, 0)] : [], []);
            var card = new CardInstanceState(7, "damage", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: descriptor);
            var context = new CombatContext(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 8, 10,
                statistics: BattleStatistics.Empty(), cardRegistry: [card]);
            var room = new RoomCombatState(0, false, [target, actor], [], context);
            string immutable = System.Text.Json.JsonSerializer.Serialize(room, ModelJson.Options);
            var result = RoomCombatModel.ApplyCardDamage(room, 1, damage, source.StartsWith("card", StringComparison.Ordinal) ? 7 : 0,
                attackerUnitId: source.StartsWith("unit", StringComparison.Ordinal) ? 2 : 0);
            Require(result.Supported, result.UnsupportedReason ?? "Unsupported combined damage.");
            CombatUnit after = result.State!.Units.Single(unit => unit.Id == 1);
            int actual = row.GetProperty("Actual").GetInt32();
            Require(after.Health == 1000 - actual && System.Text.Json.JsonSerializer.Serialize(room, ModelJson.Options) == immutable,
                $"Native combined damage/root mismatch: {source}, raw={damage}, order={string.Join(",", order)}.");
            if (source.StartsWith("unit", StringComparison.Ordinal))
                Require(result.State.Context!.Statistics!.LastAttackDamageDealt == actual + row.GetProperty("Blocked").GetInt32(),
                    "Shield order changed native last-attack damage accounting.");
            Require(after.Status("titanite")?.Stacks == Count("titanite") &&
                (after.Status("titanskin")?.Stacks ?? 0) == Count("titanskin"), "Passive defensive stacks were consumed.");
            int armorSpent = row.GetProperty("SkinDamage").GetInt32() - actual;
            Require((after.Status("armor")?.Stacks ?? 0) == Count("armor") - armorSpent &&
                (after.Status("damage shield")?.Stacks ?? 0) == Count("damage shield") - (row.GetProperty("ShieldBlocked").GetInt32() > 0 ? 1 : 0),
                "Modeled shield/armor consumption differs from the native damage-stage outputs.");
            if (row.GetProperty("ShieldBlocked").GetInt32() > 0 && !row.GetProperty("Reverse").GetBoolean() && damage > 1) orderWitnesses++;
            if (source == "unit-piercing" && actual < damage && Count("damage shield") == 0) unitPiercingWitnesses++;
            Require(seen.Add($"{row.GetProperty("Reverse").GetBoolean()}:{Count("titanite")}:{Count("damage shield")}:{Count("titanskin")}:{Count("armor")}:{source}:{damage}"), "Duplicate native matrix row.");
        }
        Require(seen.Count == 1536 && orderWitnesses > 0 && unitPiercingWitnesses > 0, "Incomplete native combination matrix.");
        TitanSkinChecks.Run();
        Console.WriteLine("TITANITE-COMBINATIONS PASS: 1536 native stage operations, both shield orders, stack counts, Titanskin/armor and card/unit piercing; full model damage/accounting and immutable roots match.");
    }
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
