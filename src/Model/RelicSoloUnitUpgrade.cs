using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicSoloUnitUpgrade
    {
        public int EffectIndex { get; }
        public bool SourceMonsters { get; }
        public string? SubtypeKey { get; }
        public bool SubtypeIsNone { get; }
        public bool Removable { get; }
        public int ConditionCount { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public IReadOnlyList<int> TrackedUnitIds { get; }

        public RelicSoloUnitUpgrade(int effectIndex, bool sourceMonsters, string? subtypeKey, bool subtypeIsNone,
            bool removable, int conditionCount, CardUpgradeModifier? upgrade, IReadOnlyList<int> trackedUnitIds)
        {
            EffectIndex = effectIndex; SourceMonsters = sourceMonsters; SubtypeKey = subtypeKey;
            SubtypeIsNone = subtypeIsNone; Removable = removable; ConditionCount = conditionCount;
            Upgrade = upgrade; TrackedUnitIds = Array.AsReadOnly(trackedUnitIds.ToArray());
        }

        internal RelicSoloUnitUpgrade WithTrackedUnitIds(IReadOnlyList<int> ids) =>
            new RelicSoloUnitUpgrade(EffectIndex, SourceMonsters, SubtypeKey, SubtypeIsNone,
                Removable, ConditionCount, Upgrade, ids);
    }

    internal sealed class RelicSoloUnitUpgradeResult
    {
        internal TrainCombatState? State { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => State != null;
        internal RelicSoloUnitUpgradeResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    internal static class RelicSoloUnitUpgradeModel
    {
        internal const string Effect = "RelicEffectAddTempSoloUnitUpgradeStartOfTurn";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.SoloUnitUpgrades?.Count > 0 ? "Unexpected relic solo-unit upgrade definitions." : null;
            if (relic.SoloUnitUpgrades == null || !indices.SequenceEqual(relic.SoloUnitUpgrades.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic solo-unit upgrade definitions.";
            foreach (RelicSoloUnitUpgrade rule in relic.SoloUnitUpgrades)
            {
                if (!rule.SourceMonsters)
                    return "Unmodeled non-monster solo-unit source team.";
                if (rule.ConditionCount != 0 || !rule.SubtypeIsNone && string.IsNullOrEmpty(rule.SubtypeKey))
                    return "Unmodeled relic solo-unit upgrade filter.";
                if (rule.TrackedUnitIds.Count != 4 || rule.TrackedUnitIds.Any(id => id < 0))
                    return "Malformed native relic solo-unit tracking state.";
                if (rule.Upgrade != null && (string.IsNullOrEmpty(rule.Upgrade.DataId) || rule.Upgrade.ExternalInteractions.Count > 0))
                    return "Unmodeled relic solo-unit upgrade payload.";
            }
            return null;
        }

        internal static RelicSoloUnitUpgradeResult Apply(TrainCombatState source)
        {
            CombatContext? context = source.Context;
            if (context?.Relics == null || context.Relics.All(relic => !relic.EffectTypes.Contains(Effect)))
                return new RelicSoloUnitUpgradeResult(source);
            string? validation = RelicModel.Validate(context);
            if (validation != null) return Unsupported(validation);
            int upgradableRoomCount = source.Rooms.Count;
            if (upgradableRoomCount != 4) return Unsupported("Solo-unit relic expects four upgradable rooms.");

            TrainCombatState state = source;
            for (int relicIndex = 0; relicIndex < state.Context!.Relics!.Count; relicIndex++)
            {
                CombatRelicState relic = state.Context.Relics[relicIndex];
                var rules = relic.SoloUnitUpgrades?.ToArray() ?? Array.Empty<RelicSoloUnitUpgrade>();
                if (rules.Length == 0) continue;
                foreach (RelicSoloUnitUpgrade rule in rules)
                {
                    int[] tracked = rule.TrackedUnitIds.ToArray();
                    for (int roomIndex = 0; roomIndex < upgradableRoomCount; roomIndex++)
                    {
                        CombatUnit[] targets = rule.SourceMonsters
                            ? state.Rooms[roomIndex].Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0).ToArray()
                            : Array.Empty<CombatUnit>();
                        int previous = tracked[roomIndex];
                        if (targets.Length == 1)
                        {
                            CombatUnit target = targets[0];
                            if (previous != 0 && previous != target.Id)
                            {
                                if (rule.Removable && rule.Upgrade != null)
                                {
                                    var removed = ApplyUpgrade(state, previous, rule.Upgrade, remove: true);
                                    if (!removed.Supported) return Unsupported(removed.UnsupportedReason!);
                                    state = removed.State!;
                                }
                                tracked[roomIndex] = target.Id;
                            }
                            else if (previous == 0) tracked[roomIndex] = target.Id;
                            if (rule.Upgrade != null)
                            {
                                var added = ApplyUpgrade(state, target.Id, rule.Upgrade, remove: false);
                                if (!added.Supported) return Unsupported(added.UnsupportedReason!);
                                state = added.State!;
                            }
                        }
                        else
                        {
                            if (previous != 0 && rule.Removable && rule.Upgrade != null)
                            {
                                var removed = ApplyUpgrade(state, previous, rule.Upgrade, remove: true);
                                if (!removed.Supported) return Unsupported(removed.UnsupportedReason!);
                                state = removed.State!;
                            }
                            tracked[roomIndex] = 0;
                        }
                    }
                    CombatRelicState[] relics = state.Context!.Relics!.ToArray();
                    int currentIndex = relicIndex;
                    if (currentIndex >= relics.Length || relics[currentIndex].DataId != relic.DataId || relics[currentIndex].AssetKey != relic.AssetKey)
                        return Unsupported("Solo-unit relic order changed during turn resolution.");
                    RelicSoloUnitUpgrade[] updatedRules = (relics[currentIndex].SoloUnitUpgrades ?? Array.Empty<RelicSoloUnitUpgrade>())
                        .Select(item => item.EffectIndex == rule.EffectIndex ? item.WithTrackedUnitIds(tracked) : item).ToArray();
                    relics[currentIndex] = relics[currentIndex].WithSoloUnitUpgrades(updatedRules);
                    state = WithContext(state, state.Context.WithRelics(relics));
                }
            }
            return new RelicSoloUnitUpgradeResult(state);
        }

        private static RelicSoloUnitUpgradeResult ApplyUpgrade(TrainCombatState source, int targetId,
            CardUpgradeModifier upgrade, bool remove)
        {
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == targetId));
            if (room == null) return Unsupported("Tracked solo unit is no longer in an upgradable room.");
            CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target?.Health <= 0) return new RelicSoloUnitUpgradeResult(source);
            RoomCombatResult applied = UnitModifierModel.Apply(room.WithContext(source.Context), targetId, upgrade,
                "TemporaryUntilEndOfBattle", remove, sourceCardId: 0);
            if (!applied.Supported) return Unsupported(applied.UnsupportedReason!);
            CombatContext? context = applied.State!.Context ?? source.Context;
            RoomCombatState[] rooms = source.Rooms.Select(item => item.RoomIndex == room.RoomIndex
                ? applied.State!.WithContext(context) : item.WithContext(context)).ToArray();
            return new RelicSoloUnitUpgradeResult(new TrainCombatState(rooms, source.Movement,
                source.EnemySlotsPerRoom, context));
        }

        private static TrainCombatState WithContext(TrainCombatState source, CombatContext context) =>
            new TrainCombatState(source.Rooms.Select(room => room.WithContext(context)).ToArray(), source.Movement,
                source.EnemySlotsPerRoom, context);

        private static RelicSoloUnitUpgradeResult Unsupported(string reason) => new RelicSoloUnitUpgradeResult(null, reason);
    }
}
