using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Upgrade auras retain the actual descriptor given to each target. The preview map
    // copies those references; immutable descriptors can safely be shared across branches.
    internal static class EnchantmentUpgradeModel
    {
        internal static EnchantmentState Begin(EnchantmentState source, int sourceId, bool self,
            bool preview, IReadOnlyList<int> targets, IReadOnlyList<EnchantmentActor> actors)
        {
            var primary = source.PrimaryTargets.ToList(); var previews = source.PreviewTargets.ToList();
            bool sync = source.PreviewRequiresSync;
            if (preview && sync)
            {
                sync = false;
                foreach (var entry in primary) Set(previews, entry);
            }
            var map = preview ? previews : primary;
            foreach (int id in targets)
                if ((id != sourceId || self) && map.All(entry => entry.UnitId != id))
                    map.Add(new EnchantmentTarget(id, false));
            var enchanter = actors.Single(actor => actor.Id == sourceId);
            for (int i = 0; i < map.Count; i++)
            {
                var entry = map[i]; var target = actors.Single(actor => actor.Id == entry.UnitId);
                bool valid = !enchanter.Muted && !enchanter.Silenced && !enchanter.IsDormant &&
                    !enchanter.IsDestroyed && enchanter.IsAlive && !target.IsDestroyed && target.IsAlive &&
                    (target.Id != sourceId || self) && enchanter.RoomIndex == target.RoomIndex;
                map[i] = new EnchantmentTarget(entry.UnitId, entry.IsEnchanted, valid ? 1 : 2, entry.AppliedUpgrade);
            }
            return new EnchantmentState(primary, previews, sync);
        }

        internal static bool Skip(EnchantmentActor target, bool preview) => target.IsDestroyed ||
            !target.IsAlive && !target.Undying || target.Preview != preview;

        internal static EnchantmentTarget Entry(EnchantmentState state, int id, bool preview) =>
            (preview ? state.PreviewTargets : state.PrimaryTargets).Single(entry => entry.UnitId == id);

        internal static EnchantmentState Replace(EnchantmentState state, EnchantmentTarget entry, bool preview)
        {
            var map = (preview ? state.PreviewTargets : state.PrimaryTargets).ToList(); Set(map, entry);
            return new EnchantmentState(preview ? state.PrimaryTargets : map,
                preview ? map : state.PreviewTargets, state.PreviewRequiresSync);
        }

        private static void Set(List<EnchantmentTarget> map, EnchantmentTarget value)
        { int index = map.FindIndex(entry => entry.UnitId == value.UnitId); if (index < 0) map.Add(value); else map[index] = value; }
    }
}
