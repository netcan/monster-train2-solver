using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class StatusCallbackModel
    {
        internal static string? FilterPurifyQueue(CombatContext? context, List<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            for (int index = 0; index < queue.Count; index++)
            {
                string? error = Admit(context, queue[index], out RoomCombatModel.QueuedCharacterTrigger admitted);
                if (error != null) return error;
                queue[index] = admitted;
            }
            queue.RemoveAll(callback => callback.Admission == RoomCombatModel.CharacterTriggerAdmission.Rejected);
            return null;
        }
        internal static string? Admit(CombatContext? context, RoomCombatModel.QueuedCharacterTrigger callback,
            out RoomCombatModel.QueuedCharacterTrigger admitted)
        {
            admitted = callback;
            if (callback.Admission != RoomCombatModel.CharacterTriggerAdmission.Pending || callback.RemovalLifecycle ||
                HarvestModel.Stage(callback.Kind, out _, out _)) return null;
            bool blocked = false;
            if (callback.Unit.Status("purify")?.Stacks > 0)
            {
                if (context?.PurifyBlockedTriggers == null) return "Purify requires captured trigger queue restrictions.";
                blocked = context.PurifyBlockedTriggers.Contains(callback.Kind);
            }
            admitted = callback.WithAdmission(blocked ? RoomCombatModel.CharacterTriggerAdmission.Rejected : RoomCombatModel.CharacterTriggerAdmission.Accepted);
            return null;
        }
        internal static readonly string[] Kinds =
        { "OnStatusEffectChanged", "OnArmorAdded", "OnPyregelAdded", "OnValiant", "OnSilence", "OnSilenceLost", "OnNewStatusEffectAdded",
            "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable", "OnTroopAdded", "OnTroopRemoved" };

        internal static IReadOnlyList<CombatStatus> MergeStartingStatuses(IEnumerable<CombatStatus> authored, IEnumerable<CombatStatus> upgrades)
        {
            var merged = new List<CombatStatus>();
            foreach (CombatStatus status in authored.Concat(upgrades))
            {
                int index = merged.FindIndex(item => item.Id == status.Id);
                if (index < 0) merged.Add(status.WithStacks(System.Math.Max(0, status.Stacks)));
                else merged[index] = merged[index].WithStacks(System.Math.Max(0, unchecked(merged[index].Stacks + status.Stacks)));
            }
            return merged.Where(status => status.Stacks > 0).ToArray();
        }

        internal static string? Added(int room, CombatUnit before, CombatUnit after, string id,
            ICollection<RoomCombatModel.QueuedCharacterTrigger> queue, CombatStatus? appliedDefinition = null)
        {
            int old = before.RegisteredStatus(id)?.Stacks ?? 0;
            CombatStatus status = after.RegisteredStatus(id) ?? appliedDefinition!.WithStacks(0);
            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnStatusEffectChanged",
                paramInt: status.Stacks, paramInt2: status.Stacks - old, paramString: id));
            if (id == "horde") queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnTroopAdded",
                paramInt: status.Stacks, paramInt2: status.Stacks - old, paramString: ""));
            string? kind = id == "armor" ? "OnArmorAdded" : id == "pyregel" ? "OnPyregelAdded" :
                id == "valor" ? "OnValiant" : id == "silenced" ? "OnSilence" : null;
            if (kind != null) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, kind,
                paramInt: id == "valor" ? status.Stacks : 0, paramString: id == "valor" ? "" : null));
            if (id == "cooldown" && after.Ability?.HasAbility == true && old <= 0 && status.Stacks > 0)
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnUnitAbilityUnavailable"));
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
            ICollection<RoomCombatModel.QueuedCharacterTrigger> queue, System.Action<CombatUnit>? update = null,
            System.Action<RoomCombatState>? updateRoom = null)
        {
            // Native installs authored immunities after applying starting statuses.
            CombatUnit empty = new CombatUnit(template.Id, template.AssetKey, template.Team, template.BaseAttack, template.Health,
                template.MaxHealth, template.CanAttack, template.IsPyre, template.EndsBattleOnDeath, System.Array.Empty<CombatStatus>(), template.Triggers,
                template.SpawnerCardId, template.Size, System.Array.Empty<string>(), template.Subtypes, template.Modifiers, template.IsBoss,
                template.LastAttackerId, template.StatusRegistry == null ? null : System.Array.Empty<CombatStatus>(), template.EquipmentCards, template.NextTriggerId, template.Ability, template.StatusDictionary == null ? null : new StatusDictionaryState(System.Array.Empty<string>(), System.Array.Empty<int>()), template.AbilityRules, template.HordeDefinition, template.IsSpawning, template.SacrificeCardId, template.DeathState, bumpRules: template.BumpRules);
            RoomCombatState initializing = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units.Select(unit => unit.Id == template.Id ? empty : unit).ToArray(),
                System.Array.Empty<string>(), source.Context, source.Preview);
            CombatUnit current = empty;
            bool inRoom = true;
            foreach (CombatStatus status in applications)
            {
                if (status.Stacks <= 0) continue;
                RoomCombatState scope = inRoom ? initializing : new RoomCombatState(initializing.RoomIndex, initializing.Deployment,
                    initializing.Units.Concat(new[] { current }).ToArray(), initializing.ExternalInteractions, initializing.Context, initializing.Preview);
                RoomCombatResult added = StatusApplicationModel.ApplyRetained(scope, template.Id, status, 0, allowModification: false);
                if (!added.Supported) return added.UnsupportedReason;
                initializing = added.State!;
                current = initializing.Units.FirstOrDefault(unit => unit.Id == template.Id) ??
                    added.RetainedUnits.FirstOrDefault(unit => unit.Id == template.Id) ?? current;
                inRoom &= initializing.Units.Any(unit => unit.Id == template.Id);
                if (!inRoom) initializing = EnchantmentWorldModel.Sync(new RoomCombatState(initializing.RoomIndex, initializing.Deployment,
                    initializing.Units.Where(unit => unit.Id != template.Id).ToArray(), initializing.ExternalInteractions, initializing.Context, initializing.Preview), new[] { current });
                foreach (RoomCombatModel.QueuedCharacterTrigger callback in added.PendingCallbacks) queue.Add(callback);
            }
            bool horde = applications.Any(status => status.Id == "horde" && status.Stacks > 0);
            bool purify = applications.Any(status => status.Id == "purify" && status.Stacks > 0);
            if (horde || purify || initializing.Context?.Enchantments != null)
            {
                CombatUnit value = current;
                var initialized = new CombatUnit(value.Id, value.AssetKey, value.Team, value.BaseAttack, value.Health, value.MaxHealth,
                    value.CanAttack, value.IsPyre, value.EndsBattleOnDeath, value.Statuses, value.Triggers, value.SpawnerCardId, value.Size,
                    template.StatusImmunities, value.Subtypes, value.Modifiers, value.IsBoss, value.LastAttackerId, value.StatusRegistry,
                    value.EquipmentCards, value.NextTriggerId, value.Ability, value.StatusDictionary, value.AbilityRules, value.HordeDefinition, value.IsSpawning, value.SacrificeCardId, value.DeathState, bumpRules: value.BumpRules);
                if (horde || purify) update?.Invoke(initialized);
                if (initializing.Context?.Enchantments != null)
                {
                    initializing = EnchantmentWorldModel.Sync(new RoomCombatState(source.RoomIndex, source.Deployment,
                        initializing.Units.Select(unit => unit.Id == template.Id ? initialized : unit).ToArray(),
                        source.ExternalInteractions, initializing.Context, source.Preview), inRoom ? null : new[] { initialized });
                    updateRoom?.Invoke(initializing);
                }
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
            if (id == "horde") queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnTroopRemoved",
                paramInt: count, paramInt2: old - count, paramString: ""));
            if (id == "silenced" && count == 0)
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnSilenceLost"));
            if (id == "cooldown" && after.Ability?.HasAbility == true && old > 0 && count == 0)
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room, after, "OnUnitAbilityAvailable"));
        }
    }
}
