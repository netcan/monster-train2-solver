using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicEndTurnHealthRule
    {
        public string SourceTeam { get; }
        public bool IsHeal { get; }
        public RelicEndTurnHealthRule(string sourceTeam, bool isHeal)
        { SourceTeam = sourceTeam; IsHeal = isHeal; }
    }

    internal static class RelicEndTurnHealthModel
    {
        internal const string Effect = "RelicEffectDamageOrHealUnitsEndOfTurn";
        internal const string DataId = "99814a66-c170-4b45-a466-6e164a125c59";
        internal const string AssetKey = "Inkbound_WolosBloodletter";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicCardAttackModifier? attack = owner.CardAttackModifiers?.SingleOrDefault();
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == true && owner.EffectTypes.SequenceEqual(new[] { Effect, RelicModel.ModifyCardAttack }) &&
                effect.EffectIndex == 0 && effect.ParamInt == 1 && effect.Amount == 1 && effect.ParamInt2 == 0 &&
                effect.Conditions.Count == 0 && effect.RequiredMutatorId == null && effect.CanApplyInPreviewMode &&
                effect.EndTurnHealth?.SourceTeam == "Monsters" && !effect.EndTurnHealth.IsHeal &&
                attack?.EffectIndex == 1 && attack.AttackModifier == 12 && attack.CardType == "Monster" &&
                attack.AttackMultiplier == 1 && attack.UnmodifiedAttackMultiplier == 0 && !attack.MultiplyByUpgradeCount &&
                !attack.MultiplyByRelicCount && !attack.ExcludeChampions && !attack.ExcludeStartRelics && attack.CardFilter == null
                ? null : "Missing or changed original Wolos Bloodletter definition.";
        }
        // Native Test includes dead or destroyed entries if the room still owns them;
        // Apply separately skips destroyed targets and the damage API skips dead ones.
        internal static int[] Targets(RoomCombatState room) => room.Units.Where(unit => unit.Team == CombatTeam.Player &&
            !unit.IsPyre && !(unit.Status("untouchable")?.Stacks > 0)).Select(unit => unit.Id).ToArray();
        internal static TrainCombatResult ApplyCallback(TrainCombatState source, int roomIndex)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return Fail(error);
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(relic => relic.DataId == DataId);
            if (owner == null) return new(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            TrainCombatState state = source; var results = new List<RoomCombatResult>();
            int[] ids = Targets(source.Rooms.Single(room => room.RoomIndex == roomIndex));
            foreach (int id in ids)
            {
                CombatUnit? target = state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
                if (target == null || target.DeathState?.IsDestroyed == true) continue;
                state = CardSpellModel.WithContext(state, state.Context!.WithQueryFrame(state.Context.QueryFrame?.With(selectedRoomIndex: roomIndex)));
                if (target.Health <= 0) continue;
                var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
                RoomCombatResult damaged = RoomCombatModel.ApplyRelicDamage(state.Rooms.Single(room => room.RoomIndex == roomIndex), id, 1, pending.Add);
                if (!damaged.Supported) return Fail(damaged.UnsupportedReason!);
                RelicDamageQueueModel.Import(ref state, damaged.State!); results.Add(damaged);
                // This effect does not call RemoveDeadCharacters. Spawner exhaustion
                // belongs to DoRoomCombat after all end-turn relic effects finish.
                RoomCombatModel.QueuedCharacterTrigger[] immediate = pending.Where(callback => !callback.DeferUntilRemoval).ToArray();
                if (immediate.Length > 0)
                {
                    UnitCloneResult drained = UnitCloneModel.Drain(new UnitCloneResult(state, 0, RoomOutcome.Exchanged, null,
                        Array.Empty<CombatEvent>(), Array.Empty<UnitCloneBoundary>(), immediate), deferPhysicalRemoval: true);
                    if (!drained.Supported) return Fail(drained.UnsupportedReason!);
                    state = drained.State!;
                }
            }
            return new(state, RoomOutcome.Exchanged, results);
        }
        private static TrainCombatResult Fail(string error) => new(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}
