using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HallowedHallsDraftRelicChecks
{
    private const string Spell = "c6484604-b077-43ce-84a4-0179d2f36352";
    private const string RailSpike = "a6a701f6-cb73-4880-b9b3-ed1abb3b23d4";
    private const string UnknownCallbacks = "Room recursion with relic birth and discard callbacks requires native calibration.";
    private const string UnknownDiscardDraw = "Scaled discard draw with OnCast effects or relic owners requires native calibration.";

    internal static void Run(FixtureValue archive, FixtureValue? donor)
    {
        Require(archive.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected a pinned original Hallowed Halls recording.");
        var casts = archive.GetProperty("Actions").EnumerateArray().Select(row => new RecordedCast(
            row.GetProperty("Before").Deserialize<BattleTurnState>()!,
            row.GetProperty("Action").Deserialize<PlayCardAction>()!,
            row.GetProperty("Actual").Deserialize<BattleTurnState>()!)).ToArray();
        var owners = casts.First().Before.Spawn.Train.Context!.Relics!;
        if (owners.Any(RelicModel.IsOriginalAlliedDraftOwner) && owners.Any(RelicModel.IsOriginalSpellUpgradeSlotOwner))
        {
            VerifyCampaignOwners(archive, casts, donor);
            return;
        }
        var cast = casts.Single(row => row.DataId == Spell);
        var actual = cast.Actual;
        using var keys = new BattleStateKeys();
        string parentKey = keys.Key(cast.Before);
        var before = cast.Before;
        if (donor.HasValue)
        {
            var donorOwners = DonorOwners(donor.Value);
            var allied = donorOwners.Single(RelicModel.IsOriginalAlliedDraftOwner);
            Require(!before.Spawn.Train.Context!.Relics!.Any(RelicModel.IsOriginalAlliedDraftOwner),
                "The composite control requires the original Hallowed Halls recording without this owner.");
            before = AddOwner(before, allied);
            actual = AddOwner(actual, allied);
            VerifyActiveOwner(before, cast.Action, donorOwners, UnknownCallbacks);
        }

        Require(before.Spawn.Train.Context!.Relics!.Count(RelicModel.IsOriginalAlliedDraftOwner) == 1,
            "Missing the exact original draft-only owner.");
        string beforeKey = keys.Key(before);
        VerifyCast(before, cast.Action, actual, keys, "Hallowed Halls");
        using var independent = new BattleStateKeys();
        Require(independent.Key(before) == beforeKey && independent.Key(cast.Before) == parentKey,
            "Hallowed Halls replay changed its parent state.");
        Console.WriteLine(donor.HasValue
            ? "HALLOWED-HALLS-DRAFT-RELIC-COMPOSITE PASS: identical battle successor with original draft owner, active owner rejected."
            : "HALLOWED-HALLS-DRAFT-RELIC-NATIVE PASS: exact native play, candidate, and successor with the original draft owner.");
    }

    private sealed record RecordedCast(BattleTurnState Before, PlayCardAction Action, BattleTurnState Actual)
    {
        internal string? DataId => Before.Spawn.Train.Context!.FindCard(Action.CardInstanceId)?.DataId;
    }

    private static void VerifyCampaignOwners(FixtureValue archive, RecordedCast[] casts, FixtureValue? donor)
    {
        Require(donor.HasValue, "The paired campaign owners require the original active-owner guard control.");
        var donorOwners = DonorOwners(donor!.Value);
        const string prefix = "original-isolated-spells:";
        string scenario = archive.GetProperty("ModifierScenario").GetString()!;
        Require(scenario.StartsWith(prefix, StringComparison.Ordinal), "Expected the fixed original isolated spell batch.");
        var scheduled = scenario[prefix.Length..].Split(',');
        Require(scheduled.Length is 1 or 2 && scheduled.All(id => id is Spell or RailSpike), "Unexpected original paid spell cohort.");
        var recorded = casts.Where(cast => cast.DataId is Spell or RailSpike).ToArray();
        Require(recorded.Select(cast => cast.DataId).SequenceEqual(scheduled), "Missing a scheduled original paid spell witness.");
        using var keys = new BattleStateKeys();
        foreach (var cast in recorded)
        {
            foreach (var state in new[] { cast.Before, cast.Actual })
            {
                var owners = state.Spawn.Train.Context!.Relics!.Where(relic => relic.EffectTypes.Count > 0).ToArray();
                Require(owners.Length == 2 && owners.Count(RelicModel.IsOriginalAlliedDraftOwner) == 1 &&
                    owners.Count(RelicModel.IsOriginalSpellUpgradeSlotOwner) == 1, "Expected both original campaign owners at each native boundary.");
            }

            bool rail = cast.DataId == RailSpike;
            if (rail) VerifyRailDraw(archive, cast);
            string parentKey = keys.Key(cast.Before);
            VerifyCast(cast.Before, cast.Action, cast.Actual, keys, rail ? "Awoken's Rail Spike" : "Hallowed Halls");
            VerifyActiveOwner(cast.Before, cast.Action, donorOwners, rail ? UnknownDiscardDraw : UnknownCallbacks);
            using var independent = new BattleStateKeys();
            Require(independent.Key(cast.Before) == parentKey, "Inspecting the native spell frontier changed its parent state.");
        }
        RecordedBattleChecks.Native(archive);
        Console.WriteLine("ORIGINAL-CAMPAIGN-RELICS-SPELLS PASS: " + recorded.Length +
            " native paid spells, complete successors, candidate frontiers and active-owner rejection.");
    }

    private static void VerifyRailDraw(FixtureValue archive, RecordedCast cast)
    {
        int paid = cast.Before.Energy - cast.Actual.Energy;
        Require(paid > 0 && cast.Before.Spawn.Train.Context!.Cards.DeckDrawDisabledThisTurn == false &&
            cast.Actual.Spawn.Train.Context!.Cards.DeckDrawDisabledThisTurn == true &&
            cast.Actual.OtherPiles.Any(pile => pile.Name == "Exhausted" && pile.Cards.Any(card => card.InstanceId == cast.Action.CardInstanceId)) &&
            archive.GetProperty("CardCycles").EnumerateArray().Any(cycle =>
                cycle.GetProperty("Kind").GetString() == "SpellDraw" &&
                cycle.GetProperty("PlayedCardId").GetInt32() == cast.Action.CardInstanceId &&
                cycle.GetProperty("HandSize").GetInt32() == paid && cycle.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Expected the original paid Rail Spike draw and Consume boundary.");
    }

    private static IReadOnlyList<CombatRelicState> DonorOwners(FixtureValue donor)
    {
        Require(donor.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected the original control owners from the same installed module.");
        var before = donor.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        return before.Spawn.Train.Context!.Relics!;
    }

    private static void VerifyActiveOwner(BattleTurnState before, PlayCardAction action, IReadOnlyList<CombatRelicState> donors, string reason)
    {
        var active = donors.Single(relic => relic.AssetKey == "CarriageJack");
        var unsupported = BattleActionModel.PlayCard(AddOwner(before, active), action);
        Require(unsupported.Rejection == ActionRejection.Unsupported && unsupported.Reason == reason,
            "A projected combat-active owner must keep the callback guard.");
    }

    private static void VerifyCast(BattleTurnState before, PlayCardAction action, BattleTurnState actual,
        BattleStateKeys keys, string spell)
    {
        string expectedKey = keys.Key(actual);
        var direct = BattleActionModel.PlayCard(before, action);
        var frontier = BattleActionModel.EnumerateActions(before, action.CardInstanceId, includeAbilities: false);
        var selected = frontier.SupportedCandidates.SingleOrDefault(candidate => candidate.Action.RoomIndex == action.RoomIndex);
        Require(direct.Supported && keys.Key(direct.State!) == expectedKey &&
            frontier.UnsupportedAttempts.Count == 0 && selected != null && keys.Key(selected.Result.State!) == expectedKey,
            "The exact paid " + spell + " and candidate frontier must match the native successor and RNG.");
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
