using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PunishedChampionDefinitionChecks
{
    private const string Module = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
    private const string Darkshard = "SubtypesData_Darkshard";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static FixtureValue[] Items(FixtureValue row, string key) => row.GetProperty(key).EnumerateArray().ToArray();
    private static string? Text(FixtureValue row, string key) => row.GetProperty(key).GetString();

    internal static void Native(string catalogPath, string battlePath)
    {
        using var document = ModelJson.ReadFixture(catalogPath);
        var root = document.RootElement;
        OriginalHaloChecks.Catalog(catalogPath);
        Require(Text(root, "GameModuleMvid") == Module, "Unexpected original champion game assembly.");
        var cards = Items(root, "Cards").ToDictionary(card => Text(card, "Id")!);
        var champions = Items(root, "PunishedChampionUpgradeDefinitions");
        string[] championIds = { "c58f8d77-8578-4d97-bcf1-beb4cdbe2f27", "d10bb18c-49e9-4895-a1c5-e4ecebbf61a0" };
        Require(champions.Length == 2 && champions.Select(row => Text(row, "Id")).SequenceEqual(championIds),
            "Missing the two original Punished champion trees.");
        var upgrades = new Dictionary<string, FixtureValue>();
        int[][] damage = { new[] { 10, 25, 40, 15, 40, 90, 0, 15, 30 }, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 } };
        int[][] health = { new[] { 10, 30, 50, 5, 20, 30, 20, 40, 80 }, new[] { 20, 50, 100, 10, 30, 50, 10, 20, 60 } };
        int[] nativePathOrder = { 2, 3, 1 };
        for (int champion = 0; champion < champions.Length; champion++)
        {
            Require(Text(champions[champion], "Key") == "PunishedChamp" + (champion + 1), "Champion identity changed.");
            var paths = Items(champions[champion], "Paths");
            Require(paths.Length == 3, "Champion tree lost a path.");
            for (int path = 0; path < paths.Length; path++)
            {
                var ranks = Items(paths[path], "Upgrades");
                Require(paths[path].GetProperty("Index").GetInt32() == path && ranks.Length == 3, "Path index or rank count changed.");
                for (int rank = 0; rank < ranks.Length; rank++)
                {
                    var row = ranks[rank]; var rule = row.GetProperty("Rule");
                    string key = $"Punished{champion + 1}Path{nativePathOrder[path]}{(char)('a' + rank)}";
                    Require(Text(row, "Key") == key && row.GetProperty("Rank").GetInt32() == rank + 1 &&
                        Guid.TryParse(Text(row, "Id"), out _) && Text(row, "Id") == Text(rule, "DataId") &&
                        Text(rule, "AssetKey") == key && Items(row, "Replaces").Length == 0 &&
                        rule.GetProperty("Stats").GetProperty("Damage").GetInt32() == damage[champion][path * 3 + rank] &&
                        rule.GetProperty("Stats").GetProperty("Health").GetInt32() == health[champion][path * 3 + rank],
                        "Original upgrade identity, rank, replacement list or stats changed: " + key);
                    var ability = rule.GetProperty("AbilityUpgrade");
                    Require(ability.ValueKind == FixtureKind.Null ? row.GetProperty("Ability").ValueKind == FixtureKind.Null :
                        Text(ability.GetProperty("Definition"), "DataId") == Text(row, "Ability"),
                        "Upgrade ability projection lost its original identity: " + key);
                    upgrades.Add(key, row);
                }
            }
        }
        Require(upgrades.Values.Select(row => Text(row, "Id")).Distinct().Count() == 18, "Duplicate champion upgrades.");
        var roomUpgrades = upgrades.Values.Where(row => Items(row, "RoomModifiers").Length > 0).ToArray();
        Require(roomUpgrades.Length == 3, "Unexpected champion room modifier paths.");
        for (int rank = 0; rank < 3; rank++)
        {
            var row = upgrades["Punished1Path1" + (char)('a' + rank)];
            var modifier = Items(row, "RoomModifiers").Single();
            Require(Text(modifier, "Type") == "RoomStateFriendlyDamagePerOwnMissingHp" &&
                modifier.GetProperty("Int").GetInt32() == rank + 1 && modifier.GetProperty("Int2").GetInt32() == 1 &&
                !modifier.GetProperty("DisallowedInDeployment").GetBoolean() &&
                Items(row.GetProperty("Rule"), "ExternalInteractions").Select(item => item.GetString()).SequenceEqual(new[] { "Upgrade room modifiers" }),
                "Original room modifier or its explicit projection boundary changed.");
        }
        string[] abilities = { "2160ff0b-3d1f-4d77-97e0-26b5df37590e", "c0d006a1-ed15-45ff-bb51-7664a8c4f11c", "24daf0a7-40fd-4f15-b6d1-60522568cad4" };
        string[][] tokens = {
            new[] { "6c3eb0bb-4871-438d-9b5c-fcef1f94bf70", "c847d9b0-5af4-4076-9d99-111b81224ef5", "0127e3c2-b60d-4ac3-acf7-6395d44eb5ef" },
            new[] { "983495d0-11cb-49d7-9f3e-8531c6f18676", "841340c7-76e3-4336-b8a2-41f308a787fb", "491acbc6-c766-4ecf-8b10-6c51b5ae94e5" },
            new[] { "462f5b87-cace-447d-8818-3d4b7bc8eb7f", "c62f38bc-3b99-4f90-8d32-59a5588466e8", "4535073a-f484-4ecd-abb9-b83d6ddfcee1" }
        };
        for (int rank = 0; rank < 3; rank++)
        {
            Require(Text(upgrades["Punished1Path3" + (char)('a' + rank)], "Ability") == abilities[rank], "GrimFlight upgrade link changed.");
            var card = cards[abilities[rank]]; var effects = Items(card, "Effects");
            Require(card.GetProperty("IsUnitAbility").GetBoolean() && !card.GetProperty("AppearsInLogbook").GetBoolean() &&
                effects.Length == 2 && Text(effects[0], "Type") == "CardEffectUnitAbilityTeleport" &&
                Text(effects[1], "Type") == "CardEffectChooseMonsterToSpawnInBack" && Text(effects[1], "Target") == "Room" &&
                effects[1].GetProperty("Int").GetInt32() == 3 && Text(effects[1], "ParamSubtype") == Darkshard &&
                !effects[1].GetProperty("HasCardFilter").GetBoolean() &&
                Items(effects[1], "CardPool").Select(row => Text(row, "Id")).SequenceEqual(tokens[rank]) &&
                Items(card.GetProperty("Rule"), "ExternalInteractions").Length > 0,
                "Original teleport/three-choice sequence, pool, subtype limit or projection boundary changed.");
            foreach (string id in tokens[rank])
            {
                var token = cards[id]; var rule = token.GetProperty("Rule"); var unit = rule.GetProperty("SpawnUnit");
                Require(token.GetProperty("AppearsInLogbook").GetBoolean() && !token.GetProperty("Archived").GetBoolean() &&
                    Text(rule, "CostType") == "NonPlayable" && unit.GetProperty("Size").GetInt32() == 1 &&
                    unit.GetProperty("Health").GetInt32() == 10 && Items(unit, "Subtypes").Any(item => item.GetString() == Darkshard),
                    "Original Darkshard became a playable proxy or lost its unit definition.");
            }
        }
        using var baseline = ModelJson.ReadFixture(battlePath); var battle = baseline.RootElement;
        Require(Text(battle, "GameVersion") == "2.3.1" && Text(battle, "GameModuleMvid") == Module &&
            Text(battle, "Policy") == "no-cards" && battle.GetProperty("NativeWon").GetBoolean() &&
            battle.GetProperty("TerminalEffectsSettled").GetBoolean() && Items(battle, "Actions").Length == 0 && Items(battle, "Turns").Length == 7,
            "Catalog capture did not complete its independent baseline battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(battle.GetProperty(key).GetInt32() == 0, "Nonzero catalog baseline " + key);
        RoomCombatChecks.NativeRoomOnly(battlePath); TrainCombatChecks.Native(battle); RecordedBattleChecks.Native(battle);
        Console.WriteLine("PUNISHED-CHAMPION-DEFINITIONS PASS: 2 original champions, 18 upgrades, 3 original choice abilities/9 NonPlayable tokens, unchanged decision/RNG/frame and seven-turn baseline/16 branches. Definition audit only: three room-modifier upgrades and three summon-choice abilities remain guarded; no new runtime coverage claimed.");
    }
}
