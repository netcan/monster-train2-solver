using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomEnemyEntryRule
    {
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public bool DisallowedInDeployment { get; }
        public RoomEnemyEntryRule(IReadOnlyList<CombatStatus> statuses, bool disallowedInDeployment)
        { Statuses = Array.AsReadOnly(statuses.ToArray()); DisallowedInDeployment = disallowedInDeployment; }
    }

    public static class RoomEnemyEntryModel
    {
        public const string Type = "RoomStateAddStatusEffectOnEnemyEnter";
        public static string? Validate(RoomEnemyEntryRule rule) => rule.Statuses.Count != 1 ||
            rule.Statuses.Any(status => status.Id != "chained" || status.Stacks != 1)
            ? "Uncalibrated enemy room-entry status payload." : null;

        // Native attachment upgrades refresh inactive moon understanding on
        // entry and exit even when their only room effect is an entry status.
        // Keep that path separate from moon overrides and actor callbacks.
        internal static bool IsStatusOnly(RoomCapacityAttachmentState attachment) =>
            attachment is { AttachmentType: RoomCapacityModel.PrimaryAttachment or RoomCapacityModel.EmbeddedAttachment,
                CapacityDelta: 0, AffectsHeroes: true, AffectsMonsters: false,
                IgnoreBosses: false, SpellCostDelta: 0, EnergyPerTurnDelta: 0, MoonPhase: null,
                CharacterFilters.Count: 0, CharacterUpgrades.Count: 1, PostCombat: null, PreCombat: null,
                StatusStackBonuses: null, SpawnCopySubtype: null, SpellUpgrade: null, DeathEffect: null,
                Ability: null, CorruptionModifier: null, Echo: null, EnemyEntry.Count: 1 } &&
            Validate(attachment.EnemyEntry[0]) == null && !attachment.EnemyEntry[0].DisallowedInDeployment &&
            attachment.CharacterUpgrades[0] is { ExternalInteractions.Count: 0, Statuses.Count: 0,
                TriggerUpgrades: null, AbilityUpgrade: null, DamageBuff: 0, UnhealedHealth: 0,
                Stats: { Damage: 0, Health: 0, Cost: 0, Heal: 0, Size: 0, XCost: 0, EquipmentLimit: 0, UpgradeSlotCount: 0 } };

        public static RoomCombatResult Apply(RoomCombatState source, int unitId, string trigger)
        {
            if (trigger is not ("AfterSpawnEnchant" or "PostAscension" or "PostDescension")) return Unchanged(source);
            CombatUnit? unit = source.Units.FirstOrDefault(actor => actor.Id == unitId);
            if (unit == null || unit.Health <= 0 || unit.Team != CombatTeam.Enemy || unit.IsPyre || unit.Status("untouchable")?.Stacks > 0)
                return Unchanged(source);
            var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomCombatState state = source;
            foreach (RoomCapacityAttachmentState attachment in source.Context?.RoomCapacities?
                .FirstOrDefault(room => room.RoomIndex == source.RoomIndex)?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
            foreach (RoomEnemyEntryRule rule in attachment.EnemyEntry ?? Array.Empty<RoomEnemyEntryRule>())
            {
                string? error = Validate(rule);
                if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
                if (source.Deployment && rule.DisallowedInDeployment) continue;
                foreach (CombatStatus status in rule.Statuses)
                {
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, unitId, status, 0, fromRoomModifier: true);
                    if (!applied.Supported) return applied;
                    state = applied.State!; pending.AddRange(applied.PendingCallbacks);
                }
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: pending);
        }
        private static RoomCombatResult Unchanged(RoomCombatState source) =>
            new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
    }
}
