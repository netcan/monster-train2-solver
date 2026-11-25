using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EndlessEnemyBirthStats
    {
        internal const string CorruptionHorde = "HordeT2_DeathAddsCorruption";
        public int InitialAttack { get; }
        public int InitialHealth { get; }
        public int Attack { get; }
        public int Health { get; }
        public IReadOnlyList<int> EffectValues { get; }
        public RelicSpawnStatusScalingRule? StatusScaling { get; }
        public bool ShouldSerializeStatusScaling() => StatusScaling != null;

        public EndlessEnemyBirthStats(int initialAttack, int initialHealth, int attack, int health, IReadOnlyList<int> effectValues,
            RelicSpawnStatusScalingRule? statusScaling = null)
        {
            InitialAttack = initialAttack;
            InitialHealth = initialHealth;
            Attack = attack;
            Health = health;
            EffectValues = Array.AsReadOnly(effectValues.ToArray());
            StatusScaling = statusScaling;
        }

        internal CombatUnit? Apply(CombatUnit actor, RoomCombatState room)
        {
            bool statusShape = IsStatusShape(actor);
            bool outerBoss = actor.BumpRules?.OuterBoss == true;
            bool silenceAura = IsUnscaledSilenceAura(actor);
            bool witnessedAura = silenceAura && HasSettledTitanSkinAura(actor, room);
            if (actor.SpawnerCardId > 0 ||
                (outerBoss ? !OuterBossInstantiationModel.IsOriginalArkionPushBirth(actor) || actor.Statuses.Count != 0 :
                    !SupportedShape(actor) && !witnessedAura) ||
                statusShape != (StatusScaling != null) ||
                actor.BaseAttack != InitialAttack || actor.Health != InitialHealth || actor.MaxHealth != InitialHealth ||
                actor.Modifiers is not { AttackDamageAdded: 0, DamageBuff: 0 } modifiers ||
                modifiers.AttackDamage != InitialAttack || Attack < 0 || Health < 1 ||
                (statusShape || outerBoss || silenceAura ? 0 : actor.Triggers.Sum(trigger => trigger.Effects.Count)) != EffectValues.Count ||
                EffectValues.Any(value => value < 0))
                return null;

            CombatUnit scaled = actor.WithRawAttack(Attack).WithHealthAndMaxHealth(Health, Health);
            if (EffectValues.Count == 0) return scaled;
            int offset = 0;
            var triggers = new List<CombatTrigger>();
            foreach (CombatTrigger trigger in actor.Triggers)
            {
                CombatEffect[] effects = trigger.Effects.Select((effect, index) => effect.WithActionValue(EffectValues[offset + index])).ToArray();
                triggers.Add(trigger.WithEffects(effects));
                offset += effects.Length;
            }
            return scaled.WithTriggers(triggers);
        }

        internal static bool SupportedShape(CombatUnit actor) =>
            actor.IsBoss == false && actor.Statuses.Count == 0 &&
                (actor.Triggers.Count == 0 || SupportedTrigger(actor)) ||
            actor is { AssetKey: "HeavyT1_LoseHealthOnMove_Chosen", IsBoss: false,
                Statuses: [{ Id: "multistrike", Stacks: 1 }], Triggers: [{ Kind: "PostCombat" }] } &&
                SupportedTrigger(actor) ||
            actor.IsBoss == true && actor.EndsBattleOnDeath && actor.BumpRules?.OuterBoss != true &&
                actor.Triggers.Count == 0 && actor.Statuses is [{ Id: "relentless", Stacks: 1 }] ||
            IsStatusShape(actor) || actor.Statuses.Count == 0 && IsUnscaledSilenceAura(actor);

        internal static bool IsUnscaledSilenceAura(CombatUnit actor) => actor is
        {
            AssetKey: "EnchanterT3_Silence", Team: CombatTeam.Enemy, IsBoss: false,
            AbilityRules.AuthoredStartingStatuses: [],
            Triggers: [{ Kind: "AfterSpawnEnchant", Effects: [{
                Type: "CardEffectEnchant", Value: 1, Enchantment: {
                    Targeting: { Type: "AddStatus", Target: "Room", AllowPlayer: true, AllowEnemy: false, Range: null },
                    StatusPool: [{ Id: "silenced", Stacks: 1 }], HasParentCard: false, Upgrade: null
                }
            }] }]
        };

        // The room can already show the earlier carrier's aura when binding the captured
        // birth delta. The original callback changes only raw attack and maximum health.
        private static bool HasSettledTitanSkinAura(CombatUnit actor, RoomCombatState room) =>
            actor.Statuses is [{ Id: "titanskin", Stacks: 10 }] &&
            room.Units.Any(owner => OriginalTitanSkinAura(owner) is EnchantmentRule aura &&
                (room.Preview ? aura.State.PreviewTargets : aura.State.PrimaryTargets)
                    .Any(target => target.UnitId == actor.Id && target.IsEnchanted));

        internal static EnchantmentRule? OriginalTitanSkinAura(CombatUnit actor) => actor is
        {
            AssetKey: "ShieldGuardT3_Enchanter_Chosen", Team: CombatTeam.Enemy, Health: > 0,
            Triggers: [{ Kind: "AfterSpawnEnchant", Effects: [{
                Type: "CardEffectEnchant", Enchantment: { Bound: true,
                    Targeting: { Type: "AddStatus", Target: "Room", AllowEnemy: true, AllowPlayer: false, Range: null },
                    StatusPool: [{ Id: "titanskin", Stacks: 10 }], HasParentCard: false, Upgrade: null
                } aura
            }] }]
        } ? aura : null;

        internal static bool IsStatusShape(CombatUnit actor) => actor.IsBoss == false &&
            actor.Triggers is [var trigger] && (trigger switch
            {
                { Kind: "OnKill", Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus",
                    Target: "Self", Range: null, Statuses: [{ Id: "buff" }] } }] } =>
                    actor.Statuses.Count == 0 || actor.AbilityRules?.AuthoredStartingStatuses.Count == 0 &&
                    actor.Statuses is [{ Id: "titanskin" }],
                { Kind: "AfterSpawnEnchant", Effects: [{ Type: "CardEffectEnchant",
                    Enchantment: { StatusPool: [{ Id: "titanskin" }], HasParentCard: false, Upgrade: null } }] } =>
                    actor.Statuses is [{ Id: "armor" }, { Id: "titanskin" }],
                { Kind: "OnDeath", Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus",
                    Target: "FrontInRoom", AllowPlayer: true, AllowEnemy: false, Range: null,
                    Statuses: [{ Id: "corruption" }] } }] } =>
                    actor.AssetKey == CorruptionHorde && actor.Statuses.Count == 0,
                _ => false
            });

        private static bool SupportedTrigger(CombatUnit actor) =>
            actor.Triggers.SelectMany(trigger => trigger.Effects).All(effect => effect.Action is { Range: null, Statuses.Count: 0 } action &&
                action.Value == effect.Value && effect.DamageStatusMultiplier == null) &&
            (actor.Triggers switch
        {
            [{ Kind: "PostCombat", Effects: [
                { Type: "CardEffectDamage", Action: { Type: "Damage", Target: "Self" } },
                { Type: "CardEffectBuffDamage", Action: { Type: "BuffAttack", Target: "Self" } }] }] => true,
            [{ Kind: "PostCombatHealing", Effects: [
                { Type: "CardEffectHeal", Action: { Type: "Heal", Target: "RoomHealTargets", AllowEnemy: true, AllowPlayer: false } }] }] => true,
            [{ Kind: "CardSpellPlayed", Effects: [
                { Type: "CardEffectBuffDamage", Action: { Type: "BuffAttack", Target: "Tower", AllowEnemy: true, AllowPlayer: false } }] },
             { Kind: "PostCombatHealing", Effects: [
                { Type: "CardEffectHeal", Action: { Type: "Heal", Target: "RoomHealTargets", AllowEnemy: true, AllowPlayer: false } }] }] =>
                actor.AssetKey == "ClericT3_IncantAttack_Chosen",
            _ => false
        });
    }
}
