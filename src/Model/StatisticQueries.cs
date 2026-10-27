using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Type/subtype masks come from immutable card definitions. Scaling traits do not set cardFilter.
    public sealed class CardStatisticQuery
    {
        public string Type { get; }
        public string Duration { get; }
        public IReadOnlyList<string>? TypeMatchDataIds { get; }
        public IReadOnlyList<string>? SubtypeMatchDataIds { get; }
        public bool SubtypeIsNone { get; }
        public string? Subtype { get; }
        public string? SpecificCardDataId { get; }
        public int? SourceRawCost { get; }
        public bool VariableCost { get; }
        public string TargetTeam { get; }
        public bool CurrentCostCardModifiersOnly { get; }
        public bool CurrentCostSourceIsSpell { get; }
        public IReadOnlyList<string>? StatusIds { get; }
        public CardStatisticQuery(string type, string duration = "ThisTurn", IReadOnlyList<string>? typeMatchDataIds = null,
            IReadOnlyList<string>? subtypeMatchDataIds = null, bool subtypeIsNone = true, string? subtype = null,
            string? specificCardDataId = null, int? sourceRawCost = null, bool variableCost = false, string targetTeam = "None",
            bool currentCostCardModifiersOnly = false, bool currentCostSourceIsSpell = false,
            IReadOnlyList<string>? statusIds = null)
        { Type = type; Duration = duration; TypeMatchDataIds = Freeze(typeMatchDataIds); SubtypeMatchDataIds = Freeze(subtypeMatchDataIds);
            SubtypeIsNone = subtypeIsNone; Subtype = subtype; SpecificCardDataId = specificCardDataId;
            SourceRawCost = sourceRawCost; VariableCost = variableCost; TargetTeam = targetTeam;
            CurrentCostCardModifiersOnly = currentCostCardModifiersOnly; CurrentCostSourceIsSpell = currentCostSourceIsSpell;
            StatusIds = statusIds == null ? null : Array.AsReadOnly(statusIds.ToArray()); }
        private static IReadOnlyList<string>? Freeze(IReadOnlyList<string>? source) => source == null ? null :
            Array.AsReadOnly(source.Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    // Dynamic inputs must describe the current query boundary, rather than a past root snapshot.
    public sealed class StatisticQueryFrame
    {
        public int? Energy { get; }
        public bool? RunningCombat { get; }
        public int? Turn { get; }
        public int? ForgePoints { get; }
        public int? DragonsHoard { get; }
        public int? MoonPhase { get; }
        public bool? MoonPhaseTrackingActive { get; }
        public bool? ForgeTrackingActive { get; }
        public bool? ForgeToggleActive { get; }
        // Native PyreHeartResurrection is a consumed/not-allowed flag (0 or 1), not a tally.
        public int? PyreResurrectionCount { get; }
        public int? GrosMichelCombatRoundCount { get; }
        public bool ActiveBattle { get; }
        public int? SelectedRoomIndex { get; }
        public ForcedRelentlessCounters? ForcedRelentless { get; }
        // Internal preview input. Native heal previews read live SaveData HP,
        // even after an earlier preview room has changed the Pyre copy.
        internal int? PreviewTowerHealth { get; private set; }
        public StatisticQueryFrame(int? energy = null, bool? runningCombat = null, int? turn = null, int? forgePoints = null,
            int? dragonsHoard = null, int? moonPhase = null, int? pyreResurrectionCount = null, bool activeBattle = true,
            int? selectedRoomIndex = null, bool? moonPhaseTrackingActive = null,
            bool? forgeTrackingActive = null, bool? forgeToggleActive = null, int? grosMichelCombatRoundCount = null,
            ForcedRelentlessCounters? forcedRelentless = null)
        { Energy = energy; RunningCombat = runningCombat; Turn = turn; ForgePoints = forgePoints;
            DragonsHoard = dragonsHoard; MoonPhase = moonPhase; PyreResurrectionCount = pyreResurrectionCount; ActiveBattle = activeBattle;
            SelectedRoomIndex = selectedRoomIndex; MoonPhaseTrackingActive = moonPhaseTrackingActive;
            ForgeTrackingActive = forgeTrackingActive; ForgeToggleActive = forgeToggleActive;
            GrosMichelCombatRoundCount = grosMichelCombatRoundCount; ForcedRelentless = forcedRelentless; }
        public StatisticQueryFrame With(int? energy = null, bool? runningCombat = null, int? turn = null,
            int? forgePoints = null, int? dragonsHoard = null, int? moonPhase = null,
            int? pyreResurrectionCount = null, bool? activeBattle = null, int? selectedRoomIndex = null,
            bool? moonPhaseTrackingActive = null, bool? forgeTrackingActive = null,
            bool? forgeToggleActive = null, int? grosMichelCombatRoundCount = null,
            ForcedRelentlessCounters? forcedRelentless = null) => new StatisticQueryFrame(
                energy ?? Energy, runningCombat ?? RunningCombat, turn ?? Turn, forgePoints ?? ForgePoints,
                dragonsHoard ?? DragonsHoard, moonPhase ?? MoonPhase, pyreResurrectionCount ?? PyreResurrectionCount,
                activeBattle ?? ActiveBattle, selectedRoomIndex ?? SelectedRoomIndex,
                moonPhaseTrackingActive ?? MoonPhaseTrackingActive, forgeTrackingActive ?? ForgeTrackingActive,
                forgeToggleActive ?? ForgeToggleActive, grosMichelCombatRoundCount ?? GrosMichelCombatRoundCount,
                forcedRelentless ?? ForcedRelentless) { PreviewTowerHealth = PreviewTowerHealth };

        internal StatisticQueryFrame WithPreviewTowerHealth(int? health)
        {
            var changed = With();
            changed.PreviewTowerHealth = health;
            return changed;
        }

        internal static string? ValidateDecision(BattleTurnState state)
        {
            CombatContext? context = state.Spawn.Train.Context;
            if (context?.EnergyState != null)
            {
                string? energyError = EnergyModel.Validate(context);
                if (energyError != null) return energyError;
                if (context.EnergyState.Phase != "MonsterTurn" || !context.EnergyState.PyreAlive)
                    return "Energy state does not describe an active player decision.";
            }
            StatisticQueryFrame? frame = context?.QueryFrame;
            if (frame == null) return null; // Old captures retain their explicitly incomplete inputs.
            if (frame.ForcedRelentless != null && (frame.ForcedRelentless.WaveCount != state.Spawn.Waves.Count ||
                frame.ForcedRelentless.RemainingTurns < 0))
                return "Forced-Relentless counters disagree with the native spawn definition.";
            int moon = state.MoonPhase == "New" ? 1 : state.MoonPhase == "Full" ? 2 : 0;
            if (moon == 0 || frame.Energy != state.Energy || frame.Turn != state.Spawn.Turn ||
                frame.ForgePoints != state.ForgePoints || frame.DragonsHoard != state.DragonsHoard || frame.MoonPhase != moon ||
                frame.MoonPhaseTrackingActive.HasValue && state.MoonPhaseTrackingActive.HasValue &&
                frame.MoonPhaseTrackingActive != state.MoonPhaseTrackingActive)
                return "Dynamic statistic inputs disagree with the current decision state.";
            if (frame.RunningCombat != true || !frame.ActiveBattle || frame.PyreResurrectionCount == null)
                return "Dynamic statistic inputs do not describe an active player decision.";
            return null;
        }
    }

    public sealed class StatisticQueryResult
    {
        public CombatContext? Context { get; }
        public int Value { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal StatisticQueryResult(CombatContext? context, int value, string? unsupportedReason = null)
        { Context = context; Value = value; UnsupportedReason = unsupportedReason; }
    }

    public static class StatisticQueryModel
    {
        public static StatisticQueryResult Evaluate(CombatContext source, CardStatisticQuery query, int sourceCardId = 0,
            StatisticQueryFrame? frame = null, RoomCombatState? selectedRoom = null)
        {
            frame = frame ?? source.QueryFrame;
            if (source.Statistics == null || source.OtherPiles == null)
                return Unsupported("Statistic queries require counters and complete card piles.");
            if (query.Duration != "ThisTurn" && query.Duration != "PreviousTurn" && query.Duration != "ThisBattle")
                return Unsupported("Unmodeled statistic duration " + query.Duration);
            CardToken[] owned = source.Cards.Hand.Concat(source.Cards.Draw).Concat(source.Cards.Discard)
                .Concat(source.OtherPiles.SelectMany(pile => pile.Cards)).GroupBy(card => card.InstanceId).Select(group => group.First()).ToArray();
            int[] ids;
            if (owned.Length == 0)
            {
                if (source.Statistics.DeckCards == null) return Unsupported("Empty-pile statistics require permanent deck membership.");
                ids = source.Statistics.DeckCards.ToArray();
            }
            else ids = owned.Select(card => card.InstanceId).ToArray();
            CombatContext context = source.WithStatistics(source.Statistics.RefreshOwnedCards(ids));
            BattleStatistics statistics = context.Statistics!;
            string? dataError = null;
            string? DataId(int id)
            {
                string? data = owned.FirstOrDefault(card => card.InstanceId == id)?.DataId ?? context.FindCard(id)?.DataId;
                if (data == null) dataError = "Missing card definition identity for statistic member " + id;
                return data;
            }
            bool TypeMatches(int id) => query.TypeMatchDataIds == null || query.TypeMatchDataIds.Count > 0 && query.TypeMatchDataIds.Contains(DataId(id)!);
            bool SubtypeMatches(int id) => query.SubtypeMatchDataIds?.Count > 0 && query.SubtypeMatchDataIds.Contains(DataId(id)!);
            int[] Pile(string name) => source.OtherPiles.Where(pile => pile.Name == name).SelectMany(pile => pile.Cards)
                .Select(card => card.InstanceId).ToArray();
            StatisticQueryResult Finish(int value) => dataError == null ? new StatisticQueryResult(context, value) : Unsupported(dataError);
            int Local(string type) => ids.Contains(sourceCardId) ? statistics.Value(sourceCardId, type, query.Duration) : 0;
            int Sum(IEnumerable<int> values) { int total = 0; foreach (int value in values) total = unchecked(total + value); return total; }
            StatisticQueryResult Scalar(int? value) => value.HasValue ? Finish(value.Value) : Unsupported("Missing current query input for " + query.Type);

            string[] pileTypes = { "TypeInDeck", "TypeInDiscardPile", "TypeInExhaustPile", "TypeInDrawPile", "TypeInEatenPile",
                "SubtypeInDeck", "SubtypeInDiscardPile", "SubtypeInExhaustPile", "SubtypeInDrawPile", "SubtypeInEatenPile" };
            if (pileTypes.Contains(query.Type))
            {
                bool subtype = query.Type.StartsWith("Subtype", StringComparison.Ordinal);
                IEnumerable<int> members = ids.Where(subtype ? (Func<int, bool>)SubtypeMatches : TypeMatches);
                if (query.Type.EndsWith("InDeck", StringComparison.Ordinal))
                {
                    // Unlike TypeInDeck, native SubtypeInDeck includes exhausted/eaten/purged cards.
                    if (!subtype) { var excluded = new HashSet<int>(Pile("Exhausted").Concat(Pile("Eaten")).Concat(Pile("Purged")));
                        members = members.Where(id => !excluded.Contains(id)); }
                }
                else
                {
                    int[] located = query.Type.EndsWith("DrawPile", StringComparison.Ordinal) ? source.Cards.Draw.Select(card => card.InstanceId).ToArray() :
                        query.Type.EndsWith("DiscardPile", StringComparison.Ordinal) ? source.Cards.Discard.Select(card => card.InstanceId).ToArray() :
                        Pile(query.Type.EndsWith("ExhaustPile", StringComparison.Ordinal) ? "Exhausted" : "Eaten");
                    members = members.Where(located.Contains);
                }
                return Finish(members.Count());
            }
            bool previous = query.Duration == "PreviousTurn";
            IReadOnlyList<StatisticCount> floors = query.Duration == "ThisBattle" ? statistics.SpawnedThisBattlePerFloor : statistics.SpawnedThisTurnPerFloor;
            if (!previous && query.Type == "AnyMonsterSpawned") return Finish(Sum(floors.Select(count => count.Value)));
            if (!previous && query.Type == "AnyMonsterSpawnedTopFloor") return Finish(floors.FirstOrDefault(count => count.Key == "2")?.Value ?? 0);
            if (!previous && query.Type == "AnyMonsterDeath") return Finish(query.Duration == "ThisBattle" ? statistics.MonstersDeadThisBattle : statistics.MonstersDeadThisTurn);
            if (query.Type == "MonsterSubtypePlayed")
            {
                if (query.Subtype == null) return Unsupported("Monster subtype spawn queries require a subtype key.");
                IReadOnlyList<StatisticCount> counts = query.Duration == "ThisBattle" ? statistics.SubtypesSpawnedThisBattle : statistics.SubtypesSpawnedThisTurn;
                return Finish(previous ? 0 : counts.FirstOrDefault(count => count.Key == query.Subtype)?.Value ?? 0);
            }
            string? counter = query.Type == "AnyHeroKilled" ? "HeroesKilled" : query.Type == "AnyMonsterDeath" ? "SpawnedMonsterDeaths" :
                query.Type == "AnyDiscarded" ? "TimesDiscarded" : query.Type == "AnyCardPlayed" ? "TimesPlayed" :
                query.Type == "AnyCardDrawn" ? "TimesDrawn" : query.Type == "AnyExhausted" ? "TimesExhausted" : null;
            if (counter != null) return Finish(Sum(ids.Where(id => TypeMatches(id) && (query.SubtypeIsNone || SubtypeMatches(id)))
                .Select(id => statistics.Value(id, counter, query.Duration))));
            switch (query.Type)
            {
                case "PlayedCost": case "UnmodifiedPlayedCost":
                    if (sourceCardId <= 0) return Unsupported("Played-cost queries require a source card.");
                    CardPlayedCost? paid = statistics.PlayedCosts.FirstOrDefault(cost => cost.CardId == sourceCardId);
                    bool variableActive = query.VariableCost && frame?.ActiveBattle != false;
                    if (paid == null && variableActive && frame?.Energy == null) return Unsupported("Variable-cost fallback requires current energy.");
                    int amount = paid?.Cost ?? (variableActive ? frame!.Energy!.Value : 0);
                    if (query.Type == "UnmodifiedPlayedCost") return Finish(paid == null ? Math.Max(0, amount) : amount);
                    if (paid != null || variableActive)
                    {
                        CardInstanceState? instance = context.FindCard(sourceCardId);
                        if (instance == null || query.SourceRawCost == null) return Unsupported("Modified played cost requires source cost and card modifiers.");
                        try { amount = unchecked(amount + CardModifierModel.UpgradedStat(query.SourceRawCost.Value, "XCost", true,
                            instance.Permanent, instance.Temporary)); }
                        catch (OverflowException) { return Unsupported("Played-cost modifiers exceed the modeled numeric domain."); }
                        if (variableActive && query.CurrentCostSourceIsSpell && frame?.SelectedRoomIndex is int playedRoom)
                            amount = unchecked(amount + RoomCapacityModel.SpellPlayedCostModifier(context, playedRoom));
                    }
                    return Finish(Math.Max(0, amount));
                case "CurrentCost":
                    if (!query.CurrentCostCardModifiersOnly)
                        return Unsupported("Current cost has unmodeled trait, hand, room or card-effect modifiers.");
                    if (query.SourceRawCost == null || sourceCardId <= 0)
                        return Unsupported("Current cost requires a captured source card and base cost.");
                    CardInstanceState? costCard = context.FindCard(sourceCardId);
                    if (costCard == null) return Unsupported("Current cost requires captured card modifiers.");
                    try
                    {
                        int cost = CardModifierModel.UpgradedStat(query.SourceRawCost.Value, "Cost", true,
                            costCard.Permanent, costCard.Temporary);
                        if (query.CurrentCostSourceIsSpell && frame?.SelectedRoomIndex is int costSelectedRoom)
                            cost = (int)Math.Clamp((long)cost + RoomCapacityModel.SpellCostDelta(context, costSelectedRoom), 0L, 99L);
                        return Finish(cost);
                    }
                    catch (OverflowException) { return Unsupported("Current cost modifiers exceed the modeled numeric domain."); }
                case "Gold": return frame?.RunningCombat.HasValue == true ? Finish(frame.RunningCombat.Value ? statistics.GoldStartOfThisTurn : context.Gold) :
                    Unsupported("Gold queries require the current combat-loop state.");
                case "TurnCount": return Scalar(frame?.Turn);
                case "ForgePoints": return Scalar(frame?.ForgePoints);
                case "DragonsHoardAmount": return Scalar(frame?.DragonsHoard);
                case "MoonPhase": return Scalar(frame?.MoonPhase);
                case "PyreHeartResurrection": return Scalar(frame?.PyreResurrectionCount);
                case "CorruptionInTargetRoom":
                    if (frame?.SelectedRoomIndex is not int corruptionRoom)
                        return Unsupported("Corruption queries require the current selected room.");
                    if (context.RoomCapacities == null)
                        return Unsupported("Corruption queries require captured room state.");
                    RoomCapacityState? corrupted = context.RoomCapacities.FirstOrDefault(item => item.RoomIndex == corruptionRoom);
                    return corrupted?.Corruption is int corruption ? Finish(corruption) :
                        Unsupported("Corruption queries require the selected room's current corruption.");
                case "StatusEffectCountInTargetRoom":
                    if (query.StatusIds == null || query.StatusIds.Count == 0)
                        return Unsupported("Room-status queries require the captured native status filter.");
                    if (frame?.SelectedRoomIndex == null || selectedRoom == null || selectedRoom.RoomIndex != frame.SelectedRoomIndex)
                        return Unsupported("Room-status queries require live units from the current selected room.");
                    if (query.TargetTeam != "None" && query.TargetTeam != "Heroes" && query.TargetTeam != "Monsters" && query.TargetTeam != "Heroes, Monsters")
                        return Unsupported("Unmodeled room-status query team.");
                    bool heroes = query.TargetTeam == "Heroes" || query.TargetTeam == "Heroes, Monsters";
                    bool monsters = query.TargetTeam == "Monsters" || query.TargetTeam == "Heroes, Monsters";
                    return Finish(Sum(selectedRoom.Units.Where(unit => unit.Health > 0 &&
                        (unit.Team == CombatTeam.Enemy ? heroes : monsters)).SelectMany(unit => query.StatusIds.Select(id => Math.Max(0, unit.Status(id)?.Stacks ?? 0)))));
                case "CapacityOfTargetRoom":
                    if (frame?.SelectedRoomIndex is not int capacityRoomIndex)
                        return Unsupported("Capacity queries require the current selected room.");
                    if (context.RoomCapacities == null)
                        return Unsupported("Capacity queries require captured room capacities.");
                    RoomCapacityState? room = context.RoomCapacities.FirstOrDefault(item => item.RoomIndex == capacityRoomIndex);
                    if (room == null) return Finish(0);
                    // Native RoomState selects the enemy group only for the exact Heroes flag.
                    return Finish(query.TargetTeam == "Heroes" ? room.EnemyMaximum : room.PlayerMaximum);
                case "LastAttackDamageDealt": return Finish(statistics.LastAttackDamageDealt);
                case "EnergyRemainingEndOfTurn": return Finish(statistics.EnergyRemainingEndOfTurn);
                case "AnyCharacter": return Finish(1);
                case "NumSpecificCardsInDeck":
                    if (query.SpecificCardDataId == null) return Finish(Local(query.Type));
                    if (statistics.DeckCards == null) return Unsupported("Specific-card queries require permanent deck membership.");
                    return Finish(statistics.DeckCards.Count(id => DataId(id) == query.SpecificCardDataId));
                case "HeroesKilled": case "SpawnedMonsterDeaths": case "TimesDiscarded": case "TimesPlayed": case "TimesDrawn": case "TimesExhausted":
                case "LastSacrificedMonsterStats": case "AnyStatusEffectStacksAdded": case "AnyStatusEffectStacksRemoved":
                case "AnyMonsterSpawned": case "AnyMonsterSpawnedTopFloor": return Finish(Local(query.Type));
                default: return Unsupported("Unmodeled statistic query " + query.Type);
            }
        }
        private static StatisticQueryResult Unsupported(string reason) => new StatisticQueryResult(null, 0, reason);
    }
}
