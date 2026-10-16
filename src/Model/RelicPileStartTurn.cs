using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RelicPileStartTurnModel
    {
        public const string Effect = RelicResourceModel.PileStartTurn;
        public const string DataId = "e5d7c9d0-c0a3-4b0c-97cf-6def6ad287e9";
        public const string AssetKey = "StartOfTurnAddFunguysPerDeckSize";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource)
        {
            RelicPileStartTurnRule? rule = resource.PileStartTurn;
            CardActionEffect? action = rule?.Effect;
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.DisallowedInPlacementPhase != true ||
                resource.ParamInt != 20 || resource.ParamInt2 != 0 || resource.ParamMinInt != 0 || resource.Amount != 20 ||
                !resource.CanApplyInPreviewMode || resource.RequiredMutatorId != null || resource.Conditions.Count != 0 || rule == null ||
                rule.Pile != "DeckPile" || rule.Threshold != 20 || !rule.ApplyAllRooms || rule.ExternalInteractions.Count != 0 ||
                action == null || action.Type != "SpawnOrStatus" || action.Target != "Room" || action.Value != 1 ||
                !action.AllowEnemy || action.AllowPlayer || action.DisallowStatusStackModifiers ||
                action.Statuses.Count != 1 || action.Statuses[0].Id != "horde" || action.Statuses[0].Stacks != 1 ||
                action.Filters?.Subtype != "SubtypesData_Horde" || action.SpawnFallbackUnit?.AssetKey != "MonsterFunguy")
                return "Missing or changed original StartOfTurnAddFunguysPerDeckSize payload.";
            if (owner.EffectTypes.Count(type => type == Effect) != 1)
                return "Original deck-scaling Funguy relic must have exactly one timing effect.";
            return null;
        }

        public static TrainSpellResult Apply(TrainCombatState source, BattlePlayRules? definitions,
            int selectedRoom = -1, bool placement = false, bool preview = false)
        {
            CombatContext? initial = source.Context;
            if (initial == null || initial.Relics == null || initial.Resources == null)
                return Unsupported("Deck-scaled relic requires captured relic and visible-deck state.");
            string? error = RelicModel.Validate(initial);
            if (error != null) return Unsupported(error);
            if (initial.Relics.Any(owner => owner.EffectTypes.Contains("RelicEffectExtraSpawnsFirstUnitPerTurn")))
                return Unsupported("Deck-scaled Funguy spawn-count relic interaction is not modeled.");

            TrainCombatState state = source;
            var events = new List<CombatEvent>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (CombatRelicState owner in initial.Relics)
            {
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                foreach (RelicResourceEffect resource in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    if (resource.Type != Effect) continue;
                    RelicPileStartTurnRule rule = resource.PileStartTurn!;
                    if (preview && !resource.CanApplyInPreviewMode) continue;
                    int applications = Applications(initial.Resources.VisibleDeckCount, rule.Threshold);
                    if (applications <= 0) continue;
                    if (definitions == null) return Unsupported("Deck-scaled relic requires native room play definitions.");
                    int[] roomIndices = rule.ApplyAllRooms
                        ? state.Rooms.OrderByDescending(room => room.RoomIndex).Select(room => room.RoomIndex).ToArray()
                        : selectedRoom >= 0 ? new[] { selectedRoom } :
                            initial.QueryFrame?.SelectedRoomIndex is int currentRoom && currentRoom >= 0
                                ? new[] { currentRoom } : Array.Empty<int>();
                    if (roomIndices.Length == 0) return Unsupported("Deck-scaled relic has no selected-room definition.");
                    foreach (int roomIndex in roomIndices)
                    {
                        RoomPlayRule? roomRule = definitions.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
                        RoomCombatState? room = state.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
                        if (roomRule == null || room == null) return Unsupported("Deck-scaled relic requires complete target-room definitions.");
                        bool eligibleHorde = room.Units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.IsBoss != true &&
                            unit.Subtypes.Contains(rule.Effect.Filters!.Subtype) &&
                            !(unit.Health <= 0 && unit.DeathState?.HasFinishedDying == true && unit.DeathState.IsBeingRemoved));
                        if (roomRule.Enabled && !roomRule.SummonBlocked && !roomRule.IsPyre && !eligibleHorde && state.Context?.SpawnPoints == null)
                            return Unsupported("Deck-scaled Funguy fallback requires captured physical spawn points.");
                        for (int count = 0; count < applications; count++)
                        {
                            TrainSpellResult applied = CardSpellModel.Apply(state, roomIndex, new[] { rule.Effect },
                                targetId: 0, sourceCardId: 0, definitions: definitions, otherPiles: state.Context?.OtherPiles);
                            if (!applied.Supported) return applied;
                            state = applied.State!;
                            events.AddRange(applied.Events);
                            if (applied.Outcome != RoomOutcome.Exchanged) outcome = applied.Outcome;
                            if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated) break;
                        }
                        if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated) break;
                    }
                    if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated) break;
                }
            }
            return new TrainSpellResult(state, outcome, events, otherPiles: state.Context?.OtherPiles, playRules: definitions);
        }

        internal static int Applications(int visibleDeckCount, int threshold) =>
            visibleDeckCount < 0 || threshold <= 0 ? 0 : visibleDeckCount / threshold;

        private static TrainSpellResult Unsupported(string reason) => new(null, RoomOutcome.Unsupported,
            Array.Empty<CombatEvent>(), reason);
    }
}
