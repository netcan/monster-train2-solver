using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TitaniteWeaknessChecks
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
            new[] { "melee weakness", "spell weakness" }.All(id =>
                metadata[id].GetProperty("Stage").GetString() == "OnPreAttacked") &&
            metadata["melee weakness"].GetProperty("RemoveWhenTriggered").GetBoolean() &&
            !metadata["spell weakness"].GetProperty("RemoveWhenTriggered").GetBoolean() &&
            metadata["spell weakness"].GetProperty("RemoveAfterCardPlayed").GetBoolean() &&
            metadata["titanskin"].GetProperty("Stage").GetString() == "OnPreAttackedTitanSkin" &&
            metadata["armor"].GetProperty("Stage").GetString() == "OnPreAttackedArmor",
            "Native shield/weakness/skin/armor stages changed.");
        var seen = new HashSet<string>(); int oddWitnesses = 0, shieldControls = 0, piercingControls = 0;
        foreach (var row in fixture.GetProperty("Rows").EnumerateArray())
        {
            string source = row.GetProperty("Source").GetString()!;
            string defense = row.GetProperty("Defense").GetString()!;
            string weakness = row.GetProperty("Weakness").GetString()!;
            bool unit = source.StartsWith("unit", StringComparison.Ordinal);
            int raw = row.GetProperty("Damage").GetInt32();
            int titanite = row.GetProperty("Titanite").GetInt32(), shield = row.GetProperty("Shield").GetInt32();
            int Count(string id) => id switch
            {
                "titanite" => titanite, "damage shield" => shield,
                "armor" => defense == "armor" ? 3 : 0, "titanskin" => defense == "titanskin" ? 2 : 0,
                _ when id == weakness => 1, "piercing" => 1,
                _ => throw new InvalidOperationException("Unexpected status: " + id)
            };
            var statuses = row.GetProperty("StatusOrder").EnumerateArray().Select(value => value.GetString()!)
                .Where(id => id != "piercing").Select(id => new CombatStatus(id, Count(id),
                    metadata[id].GetProperty("ParamInt").GetInt32(),
                    removeWhenTriggered: metadata[id].GetProperty("RemoveWhenTriggered").GetBoolean(),
                    removeWhenTriggeredAfterCardPlayed: metadata[id].GetProperty("RemoveAfterCardPlayed").GetBoolean())).ToArray();
            var target = new CombatUnit(1, "target", CombatTeam.Enemy, 0, 1000, 1000, false, false, false,
                statuses, statusRegistry: statuses);
            var actor = new CombatUnit(2, "attacker", CombatTeam.Player, raw, 1000, 1000, true, false, false,
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
            var result = unit ? RoomCombatModel.ApplyUnitTurn(room, 2) : RoomCombatModel.ApplyCardDamage(room, 1, raw, 7);
            Require(result.Supported, result.UnsupportedReason ?? "Unsupported weakness damage.");
            CombatUnit after = result.State!.Units.Single(value => value.Id == 1);
            int actual = row.GetProperty("Actual").GetInt32();
            int triggeredWeakness = row.GetProperty("WeaknessDamage").GetInt32() != row.GetProperty("ShieldDamage").GetInt32()
                ? row.GetProperty("WeaknessCount").GetInt32() : 0;
            Require(after.Health == 1000 - actual, $"Native damage differs: {source}, raw={raw}, Titanite={titanite}, shield={shield}, defense={defense}: native={actual}, model={1000 - after.Health}.");
            Require((after.Status(weakness)?.Stacks ?? 0) == 1 - (metadata[weakness].GetProperty("RemoveWhenTriggered").GetBoolean()
                    ? triggeredWeakness : 0) &&
                (after.Status(weakness)?.MarkedForRemovalAfterCardPlayed == true) ==
                    (triggeredWeakness > 0 && metadata[weakness].GetProperty("RemoveAfterCardPlayed").GetBoolean()) &&
                (after.Status("damage shield")?.Stacks ?? 0) == shield - (row.GetProperty("ShieldBlocked").GetInt32() > 0 ? 1 : 0) &&
                (after.Status("armor")?.Stacks ?? 0) == Count("armor") - row.GetProperty("ArmorSpent").GetInt32() &&
                (after.Status("titanskin")?.Stacks ?? 0) == Count("titanskin") &&
                (after.Status("titanite")?.Stacks ?? 0) == titanite,
                $"Native status consumption differs: {source}, raw={raw}, Titanite={titanite}, shield={shield}, defense={defense}.");
            Require(System.Text.Json.JsonSerializer.Serialize(room, ModelJson.Options) == immutable, "Damage model mutated its root.");
            if (source == "unit" && raw == 5 && titanite == 1 && shield == 0 && defense == "none")
            {
                Require(row.GetProperty("ShieldDamage").GetInt32() == 3 &&
                    row.GetProperty("WeaknessDamage").GetInt32() == 6 && actual == 6,
                    "Missing native odd-damage rounding before weakness witness.");
                oddWitnesses++;
            }
            if (shield > 0 && row.GetProperty("ShieldBlocked").GetInt32() > 0) shieldControls++;
            if (source == "card-piercing" && titanite > 0 && raw > 0 && actual == raw * 2) piercingControls++;
            Require(seen.Add($"{titanite}:{shield}:{defense}:{source}:{raw}"), "Duplicate weakness calibration row.");
        }
        Require(seen.Count == 144 && oddWitnesses == 1 && shieldControls > 0 && piercingControls > 0, "Incomplete weakness matrix.");
        Console.WriteLine($"TITANITE-WEAKNESS PASS rows={seen.Count}: native stage order, odd/even rounding, shield/piercing, skin/armor, status consumption and immutable roots.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
