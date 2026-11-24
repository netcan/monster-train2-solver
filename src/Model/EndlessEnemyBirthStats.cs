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

        public EndlessEnemyBirthStats(int initialAttack, int initialHealth, int attack, int health, IReadOnlyList<int> effectValues)
        {
            InitialAttack = initialAttack;
            InitialHealth = initialHealth;
            Attack = attack;
            Health = health;
            EffectValues = Array.AsReadOnly(effectValues.ToArray());
        }

        internal CombatUnit? Apply(CombatUnit actor)
        {
            if (actor.SpawnerCardId > 0 || !SupportedShape(actor) ||
                actor.BaseAttack != InitialAttack || actor.Health != InitialHealth || actor.MaxHealth != InitialHealth ||
                actor.Modifiers is not { AttackDamageAdded: 0, DamageBuff: 0 } modifiers ||
                modifiers.AttackDamage != InitialAttack || Attack < 0 || Health < 1 ||
                (actor.Triggers.FirstOrDefault()?.Effects.Count ?? 0) != EffectValues.Count ||
                EffectValues.Any(value => value < 0))
                return null;

            CombatUnit scaled = actor.WithRawAttack(Attack).WithHealthAndMaxHealth(Health, Health);
            if (actor.Triggers is not [var trigger]) return scaled;
            CombatEffect[] effects = trigger.Effects.Select((effect, index) => effect.WithActionValue(EffectValues[index])).ToArray();
            return scaled.WithTriggers(new[] { trigger.WithEffects(effects) });
        }

        internal static bool SupportedShape(CombatUnit actor) =>
            actor.IsBoss == false && actor.Statuses.Count == 0 &&
                (actor.Triggers.Count == 0 || SupportedTrigger(actor)) ||
            actor.IsBoss == true && actor.EndsBattleOnDeath && actor.BumpRules?.OuterBoss != true &&
                actor.Triggers.Count == 0 && actor.Statuses is [{ Id: "relentless", Stacks: 1 }];

        private static bool SupportedTrigger(CombatUnit actor) => actor.Triggers is [var trigger] &&
            trigger.Effects.All(effect => effect.Action is { Range: null, Statuses.Count: 0 } action &&
                action.Value == effect.Value && effect.DamageStatusMultiplier == null) &&
            (trigger switch
        {
            { Kind: "PostCombat", Effects: [
                { Type: "CardEffectDamage", Action: { Type: "Damage", Target: "Self" } },
                { Type: "CardEffectBuffDamage", Action: { Type: "BuffAttack", Target: "Self" } }] } => true,
            { Kind: "PostCombatHealing", Effects: [
                { Type: "CardEffectHeal", Action: { Type: "Heal", Target: "RoomHealTargets", AllowEnemy: true, AllowPlayer: false } }] } => true,
            _ => false
        });
    }
}
