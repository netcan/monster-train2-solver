using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicCardDamageStatusModel
    {
        internal const string Effect = RelicModel.AddStatusEffectOnDamage;
        internal const string DataId = "7cf3a2a3-afae-48fc-8721-365a5b056b8b";
        internal const string AssetKey = "CooldownOnDamage_DH";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0) return null;
            return relic.DataId == DataId && relic.AssetKey == AssetKey &&
                relic.EffectTypes.SequenceEqual(new[] { Effect }, System.StringComparer.Ordinal)
                ? null : "Damage-triggered status is modeled only for the exact CooldownOnDamage_DH owner.";
        }

        internal static TrainCombatResult ApplyOne(TrainCombatState source, int roomIndex,
            IEnumerable<int> damagedUnitIds, int relicIndex, int effectIndex)
        {
            if (source.Context == null) return Fail("Damage-triggered status requires shared battle context.");
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return Fail(error);
            if (relicIndex < 0 || relicIndex >= source.Context.Relics!.Count)
                return Fail("Missing damage-triggered status relic owner.");
            CombatRelicState owner = source.Context.Relics[relicIndex];
            if (effectIndex != 0 || effectIndex >= owner.EffectTypes.Count || owner.EffectTypes[effectIndex] != Effect ||
                owner.DataId != DataId || owner.AssetKey != AssetKey)
                return Fail("Damage-triggered status owner or effect order changed.");
            RoomCombatState? initial = source.Rooms.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (initial == null) return Fail("Damage-triggered status requires the selected room.");

            TrainCombatState current = source;
            foreach (int targetId in damagedUnitIds.Distinct())
            {
                RoomCombatState room = current.Rooms.Single(item => item.RoomIndex == roomIndex);
                CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == targetId);
                if (target?.Team != CombatTeam.Player || target.IsPyre == true) continue;
                if ((target.Status("cooldown")?.Stacks ?? 0) <= 1)
                    return Fail("Cooldown reaching zero and ability-ready callbacks are not calibrated.");
                CombatStatus? cooldown = current.Context!.StatusRules.SingleOrDefault(status => status.Id == "cooldown");
                if (cooldown == null) return Fail("Damage-triggered cooldown requires its native status definition.");
                RoomCombatResult applied = StatusApplicationModel.Apply(room, targetId, cooldown.WithStacks(-1));
                if (!applied.Supported) return Fail(applied.UnsupportedReason!);
                RoomCombatState changed = applied.State!;
                RoomCombatState[] rooms = current.Rooms.Select(item => item.RoomIndex == roomIndex ? changed :
                    new RoomCombatState(item.RoomIndex, item.Deployment, item.Units, item.ExternalInteractions,
                        changed.Context, item.Preview, item.ActiveCompanionBossIds)).ToArray();
                current = new TrainCombatState(rooms, current.Movement, current.EnemySlotsPerRoom, changed.Context);
                if (applied.PendingCallbacks.Count > 0)
                {
                    TrainCombatResult callbacks = TrainCombatModel.ApplyCharacterQueue(current,
                        applied.PendingCallbacks.ToList());
                    if (!callbacks.Supported) return Fail(callbacks.UnsupportedReason!);
                    current = callbacks.State!;
                }
            }
            return new TrainCombatResult(current, RoomOutcome.Exchanged, new List<RoomCombatResult>());
        }

        private static TrainCombatResult Fail(string reason) =>
            new TrainCombatResult(null, RoomOutcome.Unsupported, new List<RoomCombatResult>(), reason);
    }
}
