using System;

namespace MonsterTrain2Poju.Model
{
    public sealed class HordeBaseStats
    {
        public int Attack { get; }
        public int Health { get; }
        public HordeBaseStats(int attack, int health)
        { Attack = attack; Health = health; }
    }

    public sealed class HordeStats
    {
        // These are native raw fields, which can be signed or have HP above max HP.
        public int Attack { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public HordeStats(int attack, int health, int maxHealth)
        { Attack = attack; Health = health; MaxHealth = maxHealth; }
    }

    public sealed class HordeCasualties
    {
        public bool ShouldRemove { get; }
        public int RemovalCount { get; }
        public HordeCasualties(bool shouldRemove, int removalCount)
        { ShouldRemove = shouldRemove; RemovalCount = removalCount; }
    }

    public static class HordeStatModel
    {
        // StatusEffectHordeState's numerical step; callers supply the effective
        // stack delta after native addition/removal gates and stack caps.
        public static HordeStats Change(HordeStats current, HordeBaseStats definition, int stackDelta, int stacksTotal)
        {
            Validate(definition);
            bool added = stackDelta >= 0;
            int magnitude = Math.Abs(stackDelta);
            int attack = unchecked(definition.Attack * magnitude);
            int health = unchecked(definition.Health * magnitude);
            int hp, maxHp;
            if (added)
            {
                hp = health; maxHp = health;
                if (magnitude != stacksTotal)
                {
                    attack = unchecked(attack + current.Attack);
                    hp = unchecked(hp + current.Health);
                    maxHp = unchecked(maxHp + current.MaxHealth);
                }
            }
            else
            {
                attack = unchecked(current.Attack - attack);
                hp = Math.Max(0, Troops(current.Health, definition.Health) == stacksTotal ? current.Health : unchecked(current.Health - health));
                maxHp = Math.Max(0, Troops(current.MaxHealth, definition.Health) == stacksTotal ? current.MaxHealth : unchecked(current.MaxHealth - health));
            }
            // StateInformation setters independently cap each HP field; they do
            // not clamp damage, negative growth results, or HP against max HP.
            return new HordeStats(attack, Math.Min(99999, hp), Math.Min(99999, maxHp));
        }

        public static HordeCasualties Casualties(HordeStats current, HordeBaseStats definition, int stacks)
        {
            Validate(definition);
            bool shouldRemove = stacks > 1 && current.Health <= unchecked((stacks - 1) * definition.Health);
            int removed = Math.Max(0, unchecked(stacks - Troops(current.Health, definition.Health)));
            return new HordeCasualties(shouldRemove, removed);
        }

        private static int Troops(int health, int baseHealth) => Math.Max(1,
            unchecked((int)Math.Ceiling((float)health / (float)baseHealth)));
        private static void Validate(HordeBaseStats definition)
        {
            if (definition.Health <= 0) throw new ArgumentOutOfRangeException(nameof(definition), "Horde requires positive authored health.");
        }
    }
}
