using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TroopAuraState
    {
        public string Subtype { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public IReadOnlyList<int> Targets { get; }
        public TroopAuraState(string subtype, CardUpgradeModifier upgrade, IReadOnlyList<CombatStatus> statuses, IReadOnlyList<int> targets)
        { Subtype = subtype; Upgrade = upgrade; Statuses = Array.AsReadOnly(statuses.ToArray()); Targets = Array.AsReadOnly(targets.ToArray()); }
        internal TroopAuraState WithTargets(IEnumerable<int> targets) => new TroopAuraState(Subtype, Upgrade, Statuses, targets.ToArray());
    }
    internal static class TroopAuraModel
    {
        internal static string Key(int id) => "troop-aura:" + id;
        internal static bool Matches(CombatUnit unit, TroopAuraState rule) => unit.Team == CombatTeam.Player && !unit.IsPyre &&
            unit.Health > 0 && unit.DeathState?.IsDestroyed != true && unit.Subtypes.Contains(rule.Subtype);

        internal static EnchantmentCombatResult UpdateAll(EnchantmentCombatState source, IReadOnlyList<int> order)
        {
            TrainCombatState train = source.Train;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            string? error = null;
            foreach (int ownerId in order)
            {
                RoomCombatState? room = train.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == ownerId));
                CombatUnit? owner = room?.Units.Single(unit => unit.Id == ownerId);
                TroopAuraState? rule = owner?.Modifiers?.TroopAura;
                if (rule == null || owner!.Health <= 0 || owner.DeathState?.IsDestroyed == true) continue;
                if (source.EnchanterIds.Count > 0)
                { error = "Combined troop upgrades and enchantment source ordering require native calibration."; break; }
                var targets = room!.Units.Where(unit => Matches(unit, rule)).Select(unit => unit.Id).ToArray();
                var cache = rule.Targets.ToList();
                foreach (int id in rule.Targets.Reverse())
                {
                    CombatUnit? unit = train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
                    if (unit == null || unit.Health <= 0 || unit.DeathState?.IsDestroyed == true)
                    { if (!source.Preview) cache.Remove(id); continue; }
                    if (targets.Contains(id)) continue;
                    if (!ApplyAll(id, false)) break;
                    if (!source.Preview) cache.Remove(id);
                }
                if (error != null) break;
                foreach (int id in targets.Where(id => !rule.Targets.Contains(id)))
                {
                    if (!ApplyAll(id, true)) break;
                    if (!source.Preview) cache.Add(id);
                }
                if (error != null) break;
                RoomCombatState latest = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == ownerId));
                train = Replace(train, latest.WithUnits(latest.Units.Select(unit => unit.Id == ownerId ?
                    unit.WithModifiers(unit.Modifiers!.WithTroopAura(rule.WithTargets(cache))) : unit).ToArray()));

                bool ApplyAll(int id, bool add)
                {
                    foreach (CombatStatus status in rule.Statuses)
                    {
                        RoomCombatState targetRoom = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id));
                        RoomCombatResult changed = add ? StatusApplicationModel.ApplyRetained(targetRoom, id, status, 0) :
                            StatusRemovalModel.Remove(targetRoom, id, status.Id, status.Stacks);
                        if (!Accept(changed)) return false;
                    }
                    CombatUnit unit = train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
                    return Upgrade(id, unit.Status("horde")?.Stacks ?? 0, add);
                }
                bool Upgrade(int id, int count, bool add)
                {
                    CardUpgradeModifier upgrade = rule.Upgrade.WithRuntimeIdentity(Key(ownerId));
                    for (int i = 0; i < count; i++)
                    {
                        RoomCombatState targetRoom = train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id));
                        if (!Accept(UnitModifierModel.ApplyDirect(targetRoom, id, upgrade, !add, upgrade.DataId))) return false;
                    }
                    return true;
                }
                bool Accept(RoomCombatResult changed)
                {
                    if (!changed.Supported) { error = changed.UnsupportedReason; return false; }
                    train = Replace(train, changed.State!); callbacks.AddRange(changed.PendingCallbacks); return true;
                }
            }
            return new EnchantmentCombatResult(error == null ? new EnchantmentCombatState(train, source.RetainedUnits,
                source.EnchanterIds, source.AllowUpdates, source.Updating, source.Preview, source.TestRng) : null,
                Array.Empty<EnchantmentRequest>(), callbacks, error);
        }

        internal static RoomCombatResult TroopChanged(RoomCombatState source, CombatUnit target, string kind, int total, int delta)
        {
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            int targetId = target.Id;
            RoomCombatState room = source.Units.Any(unit => unit.Id == targetId) ? source : source.WithUnits(source.Units.Concat(new[] { target }).ToArray());
            CombatUnit retained = target;
            if (kind == "OnTroopAdded" && total == delta) return new RoomCombatResult(room, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            foreach (CombatUnit owner in source.Units.Where(unit => unit.Modifiers?.TroopAura != null))
            {
                TroopAuraState rule = owner.Modifiers!.TroopAura!;
                if (target.Team != CombatTeam.Player || target.IsPyre || !target.Subtypes.Contains(rule.Subtype)) continue;
                CardUpgradeModifier upgrade = rule.Upgrade.WithRuntimeIdentity(Key(owner.Id));
                for (int i = 0; i < delta; i++)
                {
                    if (!room.Units.Any(unit => unit.Id == targetId)) room = room.WithUnits(room.Units.Concat(new[] { retained }).ToArray());
                    RoomCombatResult changed = UnitModifierModel.ApplyDirect(room.WithContext(room.Context?.WithEnchantments(null)),
                        targetId, upgrade, kind == "OnTroopRemoved", upgrade.DataId);
                    if (!changed.Supported) return changed;
                    callbacks.AddRange(changed.PendingCallbacks);
                    retained = changed.State!.Units.Concat(changed.RetainedUnits).First(unit => unit.Id == targetId);
                    room = changed.State!.WithContext(changed.State.Context?.WithEnchantments(source.Context?.Enchantments));
                }
            }
            return new RoomCombatResult(EnchantmentWorldModel.Sync(room, new[] { retained }), RoomOutcome.Exchanged, 0,
                new List<CombatEvent>(), pendingCallbacks: callbacks, retainedUnits: new[] { retained });
        }
        private static TrainCombatState Replace(TrainCombatState source, RoomCombatState room) => new TrainCombatState(
            source.Rooms.Select(old => old.RoomIndex == room.RoomIndex ? room : old.WithContext(room.Context)).ToArray(),
            source.Movement, source.EnemySlotsPerRoom, room.Context);
    }
}
