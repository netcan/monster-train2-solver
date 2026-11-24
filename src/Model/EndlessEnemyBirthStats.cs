using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EndlessEnemyBirthStats
    {
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

        internal CombatUnit? Apply(CombatUnit actor)
        {
            bool statusShape = IsStatusShape(actor);
            if (actor.SpawnerCardId > 0 || !SupportedShape(actor) || statusShape != (StatusScaling != null) ||
                actor.BaseAttack != InitialAttack || actor.Health != InitialHealth || actor.MaxHealth != InitialHealth ||
                actor.Modifiers is not { AttackDamageAdded: 0, DamageBuff: 0 } modifiers ||
                modifiers.AttackDamage != InitialAttack || Attack < 0 || Health < 1 ||
                (statusShape ? 0 : actor.Triggers.Sum(trigger => trigger.Effects.Count)) != EffectValues.Count ||
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
            actor.IsBoss == true && actor.EndsBattleOnDeath && actor.BumpRules?.OuterBoss != true &&
                actor.Triggers.Count == 0 && actor.Statuses is [{ Id: "relentless", Stacks: 1 }] ||
            IsStatusShape(actor);

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
