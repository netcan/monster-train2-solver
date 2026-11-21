using System;

namespace MonsterTrain2Poju.Model
{
    public static partial class RelicResourceModel
    {
        public static (int BaseMaxHealth, int Health, int ModifiedAttack) AdjustPyreForDeckChange(
            int baseMaxHealth, int maxHealth, int health, int modifiedAttack,
            int deckDelta, int healthPerCard, int attackPerCard)
        {
            int healthDelta = unchecked(deckDelta * healthPerCard);
            int newBaseMaximum = Math.Max(1, unchecked(baseMaxHealth + healthDelta));
            int newMaximum = unchecked(maxHealth - baseMaxHealth + newBaseMaximum);
            // A reduction only lowers the cap. Any raw HP above that cap is
            // invisible, and a later increase would still fill the new cap.
            int newHealth = Math.Min(newMaximum, Math.Max(0, unchecked(health + Math.Max(0, healthDelta))));
            int newAttack = Math.Max(0, unchecked(modifiedAttack + deckDelta * attackPerCard));
            return (newBaseMaximum, newHealth, newAttack);
        }
    }
}
