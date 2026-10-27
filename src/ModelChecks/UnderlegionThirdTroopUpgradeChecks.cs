using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnderlegionThirdTroopUpgradeChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CardCorruptionAttackUpgradeModel.Guard);
        Require(rows.Length == 1 && rows[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 9,
            "Wrong original third-tier champion birth boundary.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var upgrade = before.Spawn.Train.Context!.FindCard(9)!.Permanent.Upgrades.Single();
        Require(upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdDataId && CardUnderlegionTroopUpgradeModel.Known(upgrade),
            "Changed original third-tier upgrade.");
        var hero = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == 9);
        Require(hero.Id == 2 && hero.AssetKey == "MonsterChampionUnderlegion1" && hero.BaseAttack == 80 && hero.Health == 100 &&
            hero.Modifiers?.TroopAura is { Subtype: CardUnderlegionTroopUpgradeModel.NoneSubtype } aura &&
            aura.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId &&
            aura.Upgrade.AssetKey == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeKey && aura.Upgrade.Stats.Health == 3 &&
            aura.Targets.SequenceEqual([hero.Id]) && hero.Triggers[0].Effects.Single().Action is
                { Value: 3, Statuses: [{ Id: "horde", Stacks: 3 }] },
            "Original third-tier aura birth/cache or three-stack trigger definition differs.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Third-tier birth differs from complete native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Third-tier birth mutated its parent.");
        foreach (var change in new Action<JsonObject>[] {
            node => node["DataId"] = "unknown",
            node => node["AssetKey"] = "unknown",
            node => node["Stats"]!["Damage"] = 71,
            node => node["Stats"]!["Health"] = 86,
            node => node["CloneDamageBase"] = 40,
            node => node["Unique"] = true,
            node => node["RemoveOnDiscard"] = true,
            node => node["ExternalInteractions"]!.AsArray().Add("uncalibrated-room-upgrade"),
            node => node["TriggerUpgrades"]![0]!["Kind"] = "OnHit",
            node => node["TriggerUpgrades"]![0]!["Effects"]![0]!["Action"]!["Value"] = 2
        })
        {
            var node = JsonSerializer.SerializeToNode(upgrade, ModelJson.Options)!.AsObject(); change(node);
            var altered = node.Deserialize<CardUpgradeModifier>(ModelJson.Options)!;
            Require(!CardUnderlegionTroopUpgradeModel.Known(altered) &&
                CardModifierModel.UpgradeInteractions(altered).Contains(CardCorruptionAttackUpgradeModel.Guard),
                "Unknown third-tier room upgrade lost its guard.");
        }
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, CardCorruptionAttackUpgradeModel.Guard);
            UnderlegionTroopUpgradeChecks.CapturePair(source, second);
        }
        Console.WriteLine("UNDERLEGION-THIRD-TROOP-UPGRADE PASS: original80/100 champion birth, health3 aura/cache and authored three-stack trigger; full native successor/forecast/RNG; four immutable branches/ten guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
