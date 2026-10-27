using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnderlegionSecondTroopUpgradeChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CardCorruptionAttackUpgradeModel.Guard);
        Require(rows.Length == 1 && rows[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 7,
            "Wrong original second-tier champion birth boundary.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var upgrade = before.Spawn.Train.Context!.FindCard(7)!.Permanent.Upgrades.Single();
        Require(upgrade.DataId == CardUnderlegionTroopUpgradeModel.SecondDataId && CardUnderlegionTroopUpgradeModel.Known(upgrade),
            "Changed original second-tier upgrade.");
        var hero = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == 7);
        Require(hero.Id == 2 && hero.AssetKey == "MonsterChampionUnderlegion1" && hero.BaseAttack == 50 && hero.Health == 50 &&
            hero.Modifiers?.TroopAura is { Subtype: CardUnderlegionTroopUpgradeModel.NoneSubtype } aura &&
            aura.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.SecondAuraUpgradeId &&
            aura.Upgrade.AssetKey == CardUnderlegionTroopUpgradeModel.SecondAuraUpgradeKey && aura.Upgrade.Stats.Health == 2 &&
            aura.Targets.SequenceEqual([hero.Id]), "Original second-tier aura birth/cache differs.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Second-tier birth differs from complete native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Second-tier birth mutated its parent.");
        foreach (var change in new Action<JsonObject>[] {
            node => node["DataId"] = "unknown",
            node => node["AssetKey"] = "unknown",
            node => node["Stats"]!["Damage"] = 41,
            node => node["Stats"]!["Health"] = 36,
            node => node["CloneDamageBase"] = 20,
            node => node["Unique"] = true,
            node => node["RemoveOnDiscard"] = true,
            node => node["ExternalInteractions"]!.AsArray().Add("uncalibrated-room-upgrade"),
            node => node["TriggerUpgrades"]![0]!["Kind"] = "OnHit"
        }) {
            var node = JsonSerializer.SerializeToNode(upgrade, ModelJson.Options)!.AsObject(); change(node);
            var altered = node.Deserialize<CardUpgradeModifier>(ModelJson.Options)!;
            Require(!CardUnderlegionTroopUpgradeModel.Known(altered) &&
                CardModifierModel.UpgradeInteractions(altered).Contains(CardCorruptionAttackUpgradeModel.Guard),
                "Unknown second-tier room upgrade lost its guard.");
        }
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, CardCorruptionAttackUpgradeModel.Guard);
            UnderlegionTroopUpgradeChecks.CapturePair(source, second);
        }
        Console.WriteLine("UNDERLEGION-SECOND-TROOP-UPGRADE PASS: original50/50 champion birth, health2-per-stack aura/cache, complete native successor/forecast/RNG; four immutable branches/nine guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
