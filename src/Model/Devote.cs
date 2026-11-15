using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Inputs are settled player decisions, like the other battle operations.
    // Native UI selection, in-flight card plays and preview copies are not decisions.
    public static class DevoteModel
    {
        public const string Operation = "Devote";
        public static BattleActionResult Apply(BattleTurnState source, PlayCardAction action)
        {
            if (action.Operation != Operation || action.CardInstanceId != 0 || action.PlayerPosition != -1 ||
                action.TargetUnitId != 0 || action.ActivatorUnitId != 0 || action.ActivatorRoomIndex.HasValue ||
                action.TargetSpawnPointIndex != -1 || action.RecursionSelections.Count != 0 || action.MixSelections.Count != 0)
                return Illegal("Devote has no card, target or selection payload.");
            if (source.PlayRules?.DevoteEnabled != true || source.Spawn.Train.Context == null)
                return Unsupported("Devote requires captured native operation support and battle context.");
            CombatContext context = source.Spawn.Train.Context;
            if (source.Spawn.Train.Rooms.Any(room => room.Preview) || context.QueryFrame?.RunningCombat == false ||
                context.EnergyState?.Phase != "MonsterTurn") return Illegal("Devote is unavailable outside the live player action phase.");
            if (source.Spawn.Train.Rooms.Any(room => room.Deployment)) return Illegal("Devote is unavailable during deployment.");
            if (source.Energy < 1) return Illegal("Insufficient energy.");
            if (source.PlayRules.Rooms.All(room => room.RoomIndex != action.RoomIndex || !room.Enabled))
                return Illegal("Devote selected room is unavailable.");
            string? error = DevotionModel.Validate(context) ?? EnergyModel.Validate(context) ??
                StatisticQueryFrame.ValidateDecision(source) ?? TrainCombatModel.Validate(source.Spawn.Train);
            if (error != null) return Unsupported(error);
            var interactions = BattleTurnModel.ActionInteractions(source).Concat(source.Spawn.ExternalInteractions).ToArray();
            if (interactions.Length > 0) return Unsupported(string.Join("; ", interactions));
            int[]? eligible = EligibleCards(context, out error);
            if (eligible == null) return Unsupported(error!);
            if (eligible.Length == 0) return Illegal("No discardable cards.");
            BattleTurnResult selection = BattleTurnModel.SelectRoom(source, action.RoomIndex);
            if (!selection.Supported) return Unsupported(selection.UnsupportedReason!);
            source = selection.State!; context = source.Spawn.Train.Context!;
            RngDraw choice = context.BattleRng.Range(0, eligible.Length);
            context = context.WithBattleRng(choice.State).WithQueryFrame(context.QueryFrame!.With(energy: source.Energy - 1));
            DevotionResult devotion = DevotionModel.AwardBeforeDevoteDiscard(context);
            if (!devotion.Supported) return Unsupported(devotion.UnsupportedReason!);
            TrainCombatState train = CardSpellModel.WithContext(source.Spawn.Train, devotion.Context!);
            HandRemovalResult Treasure(CombatContext frame, int cardId)
            {
                TrainSpellResult cast = IndirectSpellModel.Apply(CardSpellModel.WithContext(train, frame), cardId, source.PlayRules, action.RoomIndex);
                if (!cast.Supported) return new HandRemovalResult(null, null, Array.Empty<CardToken>(), 0, cast.UnsupportedReason);
                train = cast.State!;
                return new HandRemovalResult(train.Context, train.Context!.OtherPiles, Array.Empty<CardToken>(), 0);
            }
            HandRemovalResult discarded = HandRemovalModel.Apply(devotion.Context!, 0, 0, 0, source.PlayRules,
                source.OtherPiles, selectedCardIds: new[] { eligible[choice.Value] }, effectCounterType: null, triggeredByCard: false, onTreasure: Treasure);
            if (!discarded.Supported) return Unsupported(discarded.UnsupportedReason!);
            context = discarded.Context!;
            if (devotion.LeveledUp && (error = DevotionModel.ValidateLevelCallbacks(context)) != null) return Unsupported(error);
            train = CardSpellModel.WithContext(train, context);
            if (source.BattlePreviewEnabled)
            {
                TrainCombatResult preview = BattlePreviewModel.Refresh(train, source.PlayRules);
                if (!preview.Supported) return Unsupported(preview.UnsupportedReason!);
                train = preview.State!;
            }
            if (source.CanonicalPhysicalReferences) train = TrainCombatModel.CompleteFrameRemovals(train, Array.Empty<int>());
            else if (source.CanonicalDecisionReferences) train = TrainCombatModel.ProcessRemovals(train);
            context = train.Context!;
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                context.NextUnitId ?? old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled,
                old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions,
                old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss,
                old.QueuedTrueFinalBoss, old.TrueFinalBoss);
            return new BattleActionResult(new BattleTurnState(spawn, context.QueryFrame!.Energy!.Value,
                source.EnergyPerTurn, source.DrawPerTurn, context.QueryFrame.ForgePoints ?? source.ForgePoints,
                context.QueryFrame.DragonsHoard ?? source.DragonsHoard, source.MoonPhase,
                source.RngStreams.Select(stream => new BattleRngStream(stream.Name, stream.Seed,
                    stream.Name == "Battle" ? context.BattleRng : stream.Name == "CardDraw" ? context.Cards.Rng : stream.State)).ToArray(),
                context.OtherPiles ?? source.OtherPiles, BattleTurnModel.AfterActionInteractions(source, context),
                source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
                source.CanonicalPhysicalReferences, context.QueryFrame.SelectedRoomIndex ?? source.SelectedRoom,
                source.MoonPhaseTrackingActive));
        }
        public static int[]? EligibleCards(CombatContext source, out string? error)
        {
            error = null;
            var eligible = new System.Collections.Generic.List<int>();
            foreach (CardToken token in source.Cards.Hand)
            {
                CardInstanceState? card = source.FindCard(token.InstanceId);
                if (card?.MaskDescriptor == null || card.DataId != token.DataId)
                { error = "Devote requires matching hand cards and captured branch traits."; return null; }
                CardUpgradeMaskCard mask;
                try { mask = CardBranchMaskModel.Resolve(card); }
                catch (InvalidOperationException exception) { error = exception.Message; return null; }
                if (!mask.Traits.Contains("CardTraitPersistent") && !mask.Traits.Contains("CardTraitInfusion")) eligible.Add(token.InstanceId);
            }
            return eligible.ToArray();
        }
        private static BattleActionResult Illegal(string error) => new BattleActionResult(null, ActionRejection.Illegal, error);
        private static BattleActionResult Unsupported(string error) => new BattleActionResult(null, ActionRejection.Unsupported, error);
    }
}
