using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HallowedHallsDraftRelicChecks
{
    private const string Spell = "c6484604-b077-43ce-84a4-0179d2f36352";
    private const string UnknownCallbacks = "Room recursion with relic birth and discard callbacks requires native calibration.";

    internal static void Run(FixtureValue archive, FixtureValue? donor)
    {
        Require(archive.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected a pinned original Hallowed Halls recording.");
        var cast = archive.GetProperty("Actions").EnumerateArray().Select(row => (
            Before: row.GetProperty("Before").Deserialize<BattleTurnState>()!,
            Action: row.GetProperty("Action").Deserialize<PlayCardAction>()!,
            Row: row)).Single(row => row.Before.Spawn.Train.Context!.FindCard(row.Action.CardInstanceId)?.DataId == Spell);
        var actual = cast.Row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parentKey = keys.Key(cast.Before);
        var before = cast.Before;
        if (donor.HasValue)
        {
            Require(donor.Value.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
                "Expected the original draft owner from the same installed module.");
            var original = donor.Value.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var owners = original.Spawn.Train.Context!.Relics!;
            var allied = owners.Single(RelicModel.IsOriginalAlliedDraftOwner);
            Require(!before.Spawn.Train.Context!.Relics!.Any(RelicModel.IsOriginalAlliedDraftOwner),
                "The composite control requires the original Hallowed Halls recording without this owner.");
            before = AddOwner(before, allied);
            actual = AddOwner(actual, allied);
            var active = owners.Single(relic => relic.AssetKey == "CarriageJack");
            var unsupported = BattleActionModel.PlayCard(AddOwner(before, active), cast.Action);
            Require(unsupported.Rejection == ActionRejection.Unsupported && unsupported.Reason == UnknownCallbacks,
                "A projected combat-active owner must keep the callback guard.");
        }

        Require(before.Spawn.Train.Context!.Relics!.Count(RelicModel.IsOriginalAlliedDraftOwner) == 1,
            "Missing the exact original draft-only owner.");
        string beforeKey = keys.Key(before);
        string expectedKey = keys.Key(actual);
        var direct = BattleActionModel.PlayCard(before, cast.Action);
        var frontier = BattleActionModel.EnumerateActions(before, cast.Action.CardInstanceId, includeAbilities: false);
        var selected = frontier.SupportedCandidates.SingleOrDefault(candidate => candidate.Action.RoomIndex == cast.Action.RoomIndex);
        Require(direct.Supported && keys.Key(direct.State!) == expectedKey &&
            frontier.UnsupportedAttempts.Count == 0 && selected != null && keys.Key(selected.Result.State!) == expectedKey,
            "The exact paid spell and candidate frontier must match the Hallowed Halls successor and RNG.");
        using var independent = new BattleStateKeys();
        Require(independent.Key(before) == beforeKey && independent.Key(cast.Before) == parentKey,
            "Hallowed Halls replay changed its parent state.");
        Console.WriteLine(donor.HasValue
            ? "HALLOWED-HALLS-DRAFT-RELIC-COMPOSITE PASS: identical battle successor with original draft owner, active owner rejected."
            : "HALLOWED-HALLS-DRAFT-RELIC-NATIVE PASS: exact native play, candidate, and successor with the original draft owner.");
    }

    private static BattleTurnState AddOwner(BattleTurnState state, CombatRelicState owner)
    {
        var spawn = state.Spawn;
        var context = spawn.Train.Context!;
        var train = CardSpellModel.WithContext(spawn.Train, context.WithRelics(context.Relics!.Append(owner).ToArray()));
        var changedSpawn = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping,
            spawn.Rng, spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled,
            spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions,
            spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds, spawn.OuterBoss, spawn.QueuedOuterBoss,
            spawn.QueuedTrueFinalBoss, spawn.TrueFinalBoss);
        return new BattleTurnState(changedSpawn, state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints,
            state.DragonsHoard, state.MoonPhase, state.RngStreams, state.OtherPiles, state.ExternalInteractions, state.PlayRules,
            state.BattlePreviewEnabled, state.UiRngIsolated, state.CanonicalDecisionReferences, state.CanonicalPhysicalReferences,
            state.SelectedRoom, state.MoonPhaseTrackingActive);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
