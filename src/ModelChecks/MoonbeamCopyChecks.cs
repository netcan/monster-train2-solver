using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonbeamCopyChecks
{
    internal const string DeploymentReason = "Copy-on-play requires no authored traits or one ordinary authored Consume, optionally with one ordinary temporary Freeze.";
    private const string LifecycleReason = "Moonbeam consume/copy/X-cost lifecycle requires independent native calibration.";

    internal static void Lifecycle(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, LifecycleReason);
        Require(rows.Length == 7 && rows.Skip(1).Take(4).All(row => row.GetProperty("IsEndTurn").GetBoolean()),
            "Missing natural four-turn draw and original/copy plays.");
        var before = rows[^2].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var copied = rows[^2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var original = before.Spawn.Train.Context!.FindCard(26)!;
        int copyId = before.Spawn.Train.Context.NextCardId;
        var copy = copied.Spawn.Train.Context!.FindCard(copyId)!;
        Require(before.Spawn.Turn == 4 && before.Energy == 4 && copied.Energy == 0 && actual.Energy == 0 &&
            original.DataId == CardCopyOnPlayModel.MoonbeamSpellId && copyId == 31 && copy.DataId == original.DataId &&
            copied.Spawn.Train.Context.NextCardId == 32 && actual.Spawn.Train.Context!.NextCardId == 32 &&
            copied.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == copyId) &&
            copied.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 26) &&
            !CardCopyOnPlayModel.Present(copy) && CardCopyOnPlayModel.SupportsTerminalPurge(copy) &&
            copy.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["ChaosScepter"]) &&
            copy.Temporary.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["CopyOnPlayTraitUpgrade"]) &&
            actual.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == copyId) &&
            !actual.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == copyId),
            "X payment, copy replacement, inherited magic power or consume/purge routing differs.");
        var priorEnemies = copied.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy)
            .Select(unit => new { unit.Id, unit.Health, Armor = unit.Status("armor")?.Stacks });
        var nextEnemies = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy)
            .Select(unit => new { unit.Id, unit.Health, Armor = unit.Status("armor")?.Stacks });
        Require(JsonSerializer.Serialize(priorEnemies) == JsonSerializer.Serialize(nextEnemies), "Zero-X copy changed enemy health/armor.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var action = rows[^2].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(copied) &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Copy play changed native successor/RNG or its immutable parent.");
        });
        if (replay.HasValue)
        {
            var other = NativeBranchObservationChecks.Replay(replay.Value, LifecycleReason);
            Require(source.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")) && other.Length == rows.Length,
                "Independent lifecycle request differs.");
            for (int index = 0; index < rows.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(other[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent lifecycle state/RNG differs at " + index);
        }
        Console.WriteLine("MOONBEAM-COPY-LIFECYCLE PASS:7 native inputs/full state/RNG, natural turn4 draw, X4 original, inherited +20 magic/Consume, " +
            "single replacement copy/X0 purge and four immutable branches; independentPair=" + replay.HasValue);
    }

    internal static void Deployment(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, DeploymentReason);
        Require(rows.Length == 1, "Expected original deployment rejection.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(26)!;
        Require(card.DataId == CardCopyOnPlayModel.MoonbeamSpellId && CardCopyOnPlayModel.Validate(card) == null &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["ChaosScepter", "CreateSpellCopyWithPurge"]),
            "Lost original Moonbeam consume/copy upgrades.");
        foreach (string field in new[] { "Int", "TrackedValue", "StatusEffectIds", "Removable", "DataId" })
        {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var authored = node["MaskDescriptor"]!["SourceTraits"]![0]!;
            if (field == "Int") authored[field] = 2;
            if (field == "TrackedValue") authored[field] = "SubtypeInDeck";
            if (field == "StatusEffectIds") authored[field] = JsonSerializer.SerializeToNode(new[] { "poison" });
            if (field == "Removable") node["MaskDescriptor"]!["BaseTraits"]![0]![field] = true;
            if (field == "DataId") node["MaskDescriptor"]!["Definition"]![field] = "uncalibrated";
            Require(CardCopyOnPlayModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) == DeploymentReason,
                "Uncalibrated Moonbeam signature accepted: " + field);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, DeploymentReason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOONBEAM-COPY-DEPLOYMENT PASS: original rejection/full native state/RNG, five signature rejection controls; independentPair=" +
            replay.HasValue + "; spell/copy execution requires separate native evidence.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
