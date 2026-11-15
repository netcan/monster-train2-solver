using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitDamageHistory
    {
        public int DamageTaken { get; }
        public int DamageTakenBeforeArmor { get; }
        public int ArmorBlockedOnHit { get; }
        public int LethalDamagePreventedOnHit { get; }
        public UnitDamageHistory(int damageTaken = 0, int damageTakenBeforeArmor = 0,
            int armorBlockedOnHit = 0, int lethalDamagePreventedOnHit = 0)
        {
            DamageTaken = damageTaken; DamageTakenBeforeArmor = damageTakenBeforeArmor;
            ArmorBlockedOnHit = armorBlockedOnHit; LethalDamagePreventedOnHit = lethalDamagePreventedOnHit;
        }
        internal UnitDamageHistory WithModifiers(int armor, int lethal) =>
            new UnitDamageHistory(DamageTaken, DamageTakenBeforeArmor, armor, lethal);
        internal UnitDamageHistory WithTaken(int damage) => new UnitDamageHistory(damage,
            unchecked(damage + ArmorBlockedOnHit + LethalDamagePreventedOnHit), ArmorBlockedOnHit, LethalDamagePreventedOnHit);
        internal static UnitDamageHistory? AtBirth(CombatUnit template) => template.DamageHistory != null ||
            template.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Type is
                FriendlyDamageStatusModel.NativeEffect or OwnerDamageReactionModel.NativeEffect))
                ? new UnitDamageHistory() : null;
    }

    public static class FriendlyDamageStatusModel
    {
        public const string NativeEffect = "CardEffectAddStatusEffectPerFriendlyDamageTaken";
    }

    public static class OwnerDamageReactionModel
    {
        public const string NativeEffect = "CardEffectDamagePerOwnerDamageTaken";
    }
}
