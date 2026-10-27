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
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("UNDERLEGION-TROOP-UPGRADE PASS: natural champion birth/full successor/forecast/RNG; inherited health-per-troop aura/cache; eight immutable branches; nine changed-signature guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
