using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SurgicalStrikeSubtypeChecks
{
    private const string Strike = "e48d1711-fdff-41d4-b2a0-bc8f8b2bf8dd";
    private const string Dreg = "63a48bad-07da-4687-bfef-2f7c6653af5e";
    private const string Fungus = "SubtypesData_FunGuy";

    private sealed record OnKillCall(int Before, int After);
    private sealed record KillRow(string Kind, BattleTurnState Before, BattleTurnState Actual, int CardId, int TargetId,
        string[] RawSubtypes, bool EffectiveFungus, int[] Copies, OnKillCall[] Calls);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalSurgicalStrikeAllSubtypes" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == OriginalAllSubtypesModel.OwnerId && root.GetProperty("SourceId").GetString() == Strike,
            "Wrong pinned original Surgical Strike witness.");
        KillRow[] rows = root.GetProperty("Rows").Deserialize<KillRow[]>()!;
        Require(rows.Select(row => row.Kind).SequenceEqual(["BaselineFriendly", "ActiveFriendly", "ActiveFoe"]),
            "Missing original native execution controls.");
        using var keys = new BattleStateKeys();
        foreach (KillRow row in rows)
        {
            CombatContext before = row.Before.Spawn.Train.Context!;
            CombatContext actual = row.Actual.Spawn.Train.Context!;
            CombatUnit[] units = row.Before.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            CombatUnit target = units.Single(unit => unit.Id == row.TargetId);
            CardInstanceState card = before.FindCard(row.CardId)!;
            bool active = row.Kind != "BaselineFriendly";
            bool suppressed = row.Kind == "ActiveFriendly";
            int copies = suppressed ? 0 : 1;
            Require(row.Before.Spawn.Turn == 1 && row.Actual.Spawn.Turn == 1 && row.Before.Energy - row.Actual.Energy == 1 &&
                before.Cards.Hand.Any(token => token.InstanceId == row.CardId && token.DataId == Strike) &&
                actual.Cards.Discard.Any(token => token.InstanceId == row.CardId) && card.DataId == Strike &&
                target.Health is > 0 and <= 4 && !target.Subtypes.Contains(Fungus) && target.Subtypes.SequenceEqual(row.RawSubtypes) &&
                row.Actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != row.TargetId) &&
                (row.Kind == "ActiveFoe" ? target.Team == CombatTeam.Enemy : target.Team == CombatTeam.Player && before.FindCard(target.SpawnerCardId)?.DataId == Dreg) &&
                OriginalAllSubtypesModel.Active(before, deployment: false) == active && OriginalAllSubtypesModel.Active(actual, deployment: false) == active &&
                RelicModel.Validate(before) == null && RelicModel.Validate(actual) == null && row.EffectiveFungus == suppressed &&
                row.Calls is [var call] && call.Before == 1 && call.After == copies,
                "Original card, paid kill, registered mutator or native trigger count differs at " + row.Kind);
            Require(row.Copies.Length == copies && actual.Cards.Hand.Where(token => token.InstanceId >= before.NextCardId && token.DataId == Strike)
                .Select(token => token.InstanceId).SequenceEqual(row.Copies), "Native generated copies differ at " + row.Kind);

            string immutable = keys.Key(row.Before);
            CombatContext replay = CardKillTriggerModel.Apply(before, card, preview: false, deployment: false, attackerUnitId: 0,
                target, units, _ => throw new InvalidOperationException("Unexpected gold refresh for Surgical Strike."),
                (_, _) => throw new InvalidOperationException("Unexpected queued effect for Surgical Strike."), out string? reason);
            Require(reason == null && replay.NextCardId == actual.NextCardId && replay.Cards.Hand
                .Where(token => token.InstanceId >= before.NextCardId && token.DataId == Strike).Select(token => token.InstanceId).SequenceEqual(row.Copies) &&
                keys.Key(row.Before) == immutable, "Model OnKill copy or immutable root differs at " + row.Kind + ": " + reason);
            foreach (int childId in row.Copies)
            {
                CardInstanceState child = replay.FindCard(childId)!;
                CardInstanceState observed = actual.FindCard(childId)!;
                Require(child.DataId == observed.DataId && child.Temporary.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(
                    observed.Temporary.Upgrades.Select(upgrade => upgrade.AssetKey)) && child.Temporary.Upgrades.Count == 1 &&
                    CardBranchMaskModel.Resolve(child).Traits.Contains("CardTraitEphemeral") && CardBranchMaskModel.Resolve(observed).Traits.Contains("CardTraitEphemeral"),
                    "Copied-card upgrade or Ephemeral trait differs at " + row.Kind);
            }
        }
        Console.WriteLine("ORIGINAL-SURGICAL-STRIKE-SUBTYPES PASS: three native paid kills, original friendly subtype suppression, enemy control and exact generated copy IDs.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
