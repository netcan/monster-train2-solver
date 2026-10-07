using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class StatusCallbackModel
    {
        internal static readonly string[] Kinds =
        { "OnStatusEffectChanged", "OnArmorAdded", "OnPyregelAdded", "OnValiant", "OnSilence", "OnSilenceLost", "OnNewStatusEffectAdded" };

        internal static string? Added(int room, CombatUnit before, CombatUnit after, string id,
            ICollection<RoomCombatModel.QueuedCharacterTrigger> queue, CombatStatus? appliedDefinition = null)
        {
            int old = before.RegisteredStatus(id)?.Stacks ?? 0;
            CombatStatus status = after.RegisteredStatus(id) ?? appliedDefinition!.WithStacks(0);
            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnStatusEffectChanged",
                paramInt: status.Stacks, paramInt2: status.Stacks - old, paramString: id));
            string? kind = id == "armor" ? "OnArmorAdded" : id == "pyregel" ? "OnPyregelAdded" :
                id == "valor" ? "OnValiant" : id == "silenced" ? "OnSilence" : null;
            if (kind != null) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, kind,
                paramInt: id == "valor" ? status.Stacks : 0, paramString: id == "valor" ? "" : null));
            if (old != 0) return null;
            if (!status.Hidden.HasValue || status.DisplayCategory == null)
                return after.Triggers.Any(trigger => trigger.Kind == "OnNewStatusEffectAdded")
                    ? "New-status callbacks require captured visibility/category definitions." : null;
            if (status.Hidden == true || status.DisplayCategory == "Persistent") return null;
            int? visible = after.CountUniqueVisibleStatuses();
            if (!visible.HasValue)
                return after.Triggers.Any(trigger => trigger.Kind == "OnNewStatusEffectAdded")
                    ? "New-status callbacks require the complete native status dictionary." : null;
            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnNewStatusEffectAdded", paramInt: visible.Value, paramString: ""));
            return null;
        }

        internal static string? Initialize(RoomCombatState source, CombatUnit template, IReadOnlyList<CombatStatus> applications,
            ICollection<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            // Native installs authored immunities after applying starting statuses.
            CombatUnit empty = new CombatUnit(template.Id, template.AssetKey, template.Team, template.BaseAttack, template.Health,
                template.MaxHealth, template.CanAttack, template.IsPyre, template.EndsBattleOnDeath, System.Array.Empty<CombatStatus>(), template.Triggers,
                template.SpawnerCardId, template.Size, System.Array.Empty<string>(), template.Subtypes, template.Modifiers, template.IsBoss,
                template.LastAttackerId, template.StatusRegistry == null ? null : System.Array.Empty<CombatStatus>());
            RoomCombatState initializing = new RoomCombatState(source.RoomIndex, source.Deployment, new[] { empty },
                System.Array.Empty<string>(), source.Context, source.Preview);
            foreach (CombatStatus status in applications)
            {
                RoomCombatResult added = StatusApplicationModel.ApplyRetained(initializing, template.Id, status, 0, allowModification: false);
                if (!added.Supported) return added.UnsupportedReason;
                initializing = added.State!;
                foreach (RoomCombatModel.QueuedCharacterTrigger callback in added.PendingCallbacks) queue.Add(callback);
            }
            return null;
        }

        internal static void Removed(int room, CombatUnit before, CombatUnit after, string id,
            ICollection<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            int old = before.RegisteredStatus(id)?.Stacks ?? 0;
            int count = after.RegisteredStatus(id)?.Stacks ?? 0;
            if (old <= count) return;
            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnStatusEffectChanged",
                paramInt: count, paramInt2: count - old, paramString: id));
            if (id == "silenced" && count == 0)
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnSilenceLost"));
        }
    }
}
