using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OfferingTokenFrontierChecks
{
    internal static void Run(FixtureValue archive)
    {
        Require(archive.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            archive.GetProperty("OriginalOfferingTokenPrepared").GetBoolean(), "Expected the original 2.3.1 Offering Token witness.");
        var ids = archive.GetProperty("OriginalOfferingTokenCardIds").Deserialize<int[]>()!;
        var row = archive.GetProperty("Actions").EnumerateArray().Single(action =>
            action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]);
        var root = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var recorded = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var selection = recorded.RecursionSelections.Single();
        using var keys = new BattleStateKeys();
        string originalKey = keys.Key(root);
        var bothChoices = Candidates(root);
        Require(bothChoices.Keys.Order().SequenceEqual(new[] { ids[2], ids[3] }.Order()),
            "The exact in-cast hand must expose the existing card and the card drawn by Offering Token.");
        Require(keys.Key(bothChoices[selection.CardInstanceIds.Single()].Result.State!) == keys.Key(actual),
            "The recorded choice must reproduce the complete native successor and RNG.");
        int alternateId = selection.CardInstanceIds.Single() == ids[2] ? ids[3] : ids[2];
        VerifyChoice(root, bothChoices[alternateId], alternateId);

        // Keep the drawn card in the recorded draw pile. Move only the other
        // hand card to discard so the cast lists an initially absent choice.
        var context = root.Spawn.Train.Context!;
        var cards = context.Cards;
        var onlySourceInHand = new CardCycleState(cards.Hand.Where(card => card.InstanceId == ids[1]).ToArray(),
            cards.Draw, cards.Discard.Concat(cards.Hand.Where(card => card.InstanceId != ids[1])).ToArray(), cards.Rng,
            cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades,
            cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn);
        var spawn = root.Spawn;
        var train = CardSpellModel.WithContext(spawn.Train, context.WithCards(onlySourceInHand));
        var changedSpawn = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping,
            spawn.Rng, spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled,
            spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions,
            spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds, spawn.OuterBoss, spawn.QueuedOuterBoss,
            spawn.QueuedTrueFinalBoss, spawn.TrueFinalBoss);
        var changed = new BattleTurnState(changedSpawn, root.Energy, root.EnergyPerTurn, root.DrawPerTurn, root.ForgePoints,
            root.DragonsHoard, root.MoonPhase, root.RngStreams, root.OtherPiles, root.ExternalInteractions, root.PlayRules,
            root.BattlePreviewEnabled, root.UiRngIsolated, root.CanonicalDecisionReferences, root.CanonicalPhysicalReferences,
            root.SelectedRoom, root.MoonPhaseTrackingActive);
        string changedKey = keys.Key(changed);
        var onlyDrawnChoice = Candidates(changed);
        Require(onlyDrawnChoice.Count == 1 && onlyDrawnChoice.ContainsKey(ids[3]),
            "With only Offering Token initially in hand, its draw must still be offered for discard.");
        VerifyChoice(changed, onlyDrawnChoice[ids[3]], ids[3]);
        using var independent = new BattleStateKeys();
        Require(independent.Key(root) == originalKey && independent.Key(changed) == changedKey,
            "Exploring the in-cast chooser mutated a parent battle or RNG.");
        Console.WriteLine("OFFERING-TOKEN-FRONTIER PASS: original recorded successor and RNG, both in-cast discard choices and a model-only initial empty-hand control.");

        Dictionary<int, SupportedActionCandidate> Candidates(BattleTurnState state)
        {
            var frontier = BattleActionModel.EnumerateActions(state, ids[1], includeAbilities: false);
            Require(frontier.UnsupportedAttempts.Count == 0, "Offering Token candidate search produced unsupported attempts.");
            return frontier.SupportedCandidates.Where(item => item.Action.RoomIndex == recorded.RoomIndex).ToDictionary(
                item => item.Action.RecursionSelections.Single(choice => choice.EffectIndex == selection.EffectIndex).CardInstanceIds.Single());
        }

        void VerifyChoice(BattleTurnState state, SupportedActionCandidate candidate, int discarded)
        {
            var replay = BattleActionModel.PlayCard(state, candidate.Action);
            Require(replay.Supported && replay.Outcome == candidate.Result.Outcome &&
                keys.Key(replay.State!) == keys.Key(candidate.Result.State!) &&
                replay.State!.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == discarded) &&
                !replay.State.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == discarded),
                "Generated discard action differs from independent full-state and RNG replay.");
        }
    }

    private static void Require(bool success, string message)
    { if (!success) throw new InvalidOperationException(message); }
}
