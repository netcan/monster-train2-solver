using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnderlegionTroopUpgradeChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CardCorruptionAttackUpgradeModel.Guard);
        Require(rows.Length == 1 && rows[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 3,
            "Wrong natural champion birth boundary.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(3)!;
        var upgrade = card.Permanent.Upgrades.Single();
        Require(CardUnderlegionTroopUpgradeModel.Known(upgrade), "Changed original champion upgrade.");
        var hero = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == 3);
        Require(hero.AssetKey == "MonsterChampionUnderlegion1" && hero.Health == 20 && hero.BaseAttack == 30 &&
            hero.Modifiers?.TroopAura is { Subtype: CardUnderlegionTroopUpgradeModel.NoneSubtype } aura &&
            aura.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.AuraUpgradeId && aura.Upgrade.Stats.Health == 1 &&
            aura.Targets.SequenceEqual([hero.Id]), "Native inherited troop aura/cache differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Parallel.For(0, 8, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel champion birth changed full successor/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Champion birth mutated its parent.");
        foreach (var change in new Action<JsonObject>[] {
            node => node["DataId"] = "unknown",
            node => node["AssetKey"] = "unknown",
            node => node["Stats"]!["Damage"] = 21,
            node => node["Stats"]!["Health"] = 6,
            node => node["Statuses"] = JsonSerializer.SerializeToNode(new[] { new CombatStatus("armor", 1) }, ModelJson.Options),
            node => node["ExternalInteractions"]!.AsArray().Add("Uncalibrated upgrade"),
            node => node["Unique"] = true,
            node => node["RemoveOnDiscard"] = true,
            node => node["TriggerUpgrades"]![0]!["Kind"] = "OnHit"
        }) {
            var node = JsonSerializer.SerializeToNode(upgrade, ModelJson.Options)!.AsObject(); change(node);
            var altered = node.Deserialize<CardUpgradeModifier>(ModelJson.Options)!;
            Require(!CardUnderlegionTroopUpgradeModel.Known(altered) &&
                CardModifierModel.UpgradeInteractions(altered).Contains(CardCorruptionAttackUpgradeModel.Guard),
                "Changed champion signature lost its guard.");
        }
        if (replay is FixtureValue second) {
            NativeBranchObservationChecks.Replay(second, CardCorruptionAttackUpgradeModel.Guard);
            CapturePair(source, second);
        }
        Console.WriteLine("UNDERLEGION-TROOP-UPGRADE PASS: natural champion birth/full successor/forecast/RNG; inherited health-per-troop aura/cache; eight immutable branches; nine changed-signature guards; independentPair=" + replay.HasValue);
    }

    internal static void CapturePair(FixtureValue source, FixtureValue replay)
    {
        // The old recorder omitted the automatic world for an upgrade-owned aura.
        // First replay both full observations above. Then prove that this one
        // newly captured field is the only historical difference; runtime keys
        // and acceptance retain the entire new world without normalization.
        var oldRow = source.GetProperty("Rows").EnumerateArray().Single();
        var newRow = replay.GetProperty("Rows").EnumerateArray().Single();
        foreach (string field in new[] { "Before", "Actual" }) {
            var oldState = oldRow.GetProperty(field).Deserialize<BattleTurnState>()!;
            var newState = newRow.GetProperty(field).Deserialize<BattleTurnState>()!;
            Require(oldState.Spawn.Train.Context!.Enchantments == null &&
                oldState.Spawn.Train.Rooms.All(room => room.Context!.Enchantments == null) &&
                newState.Spawn.Train.Context!.Enchantments is { AutomaticLifecycle: true, EnchanterIds.Count: 0 },
                "Unexpected historical world capture boundary.");
            var node = JsonSerializer.SerializeToNode(newState, ModelJson.Options)!;
            var train = node["Spawn"]!["Train"]!;
            var contexts = new[] { train["Context"]! }.Concat(train["Rooms"]!.AsArray().Select(room => room!["Context"]!));
            foreach (var context in contexts) context["Enchantments"] = null;
            Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                NativeBranchObservationChecks.Key(oldState), "Native state changed beyond newly captured automatic world.");
        }
        var oldRequest = JsonSerializer.SerializeToNode(source.GetProperty("Request").ToObjectGraph())!;
        var newRequest = JsonSerializer.SerializeToNode(replay.GetProperty("Request").ToObjectGraph())!;
        foreach (string field in new[] { "Root", "FailedRoot" }) {
            // Requests built from typed states explicitly serialize optional
            // defaults that the native recorder omits. Bind the complete model
            // state rather than requiring identical archive object members.
            Require(Key(source.GetProperty("Request").GetProperty(field)) == Key(oldRow.GetProperty("Before")) &&
                Key(replay.GetProperty("Request").GetProperty(field)) == Key(newRow.GetProperty("Before")),
                "Birth request does not bind its own native root.");
            newRequest[field] = oldRequest[field]!.DeepClone();
        }
        Require(JsonNode.DeepEquals(oldRequest, newRequest) &&
            oldRow.GetProperty("Expected").ValueKind == FixtureKind.Null &&
            newRow.GetProperty("Expected").ValueKind == FixtureKind.Object &&
            newRow.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
            oldRow.GetProperty("Action").ContentEquals(newRow.GetProperty("Action")),
            "Independent capture changed request/input or erased the old rejection.");
        static string Key(FixtureValue value) => NativeBranchObservationChecks.Key(value.Deserialize<BattleTurnState>()!);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
