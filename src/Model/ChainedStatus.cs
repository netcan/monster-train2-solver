namespace MonsterTrain2Poju.Model
{
    // Native status admission and the unmodified secondary-damage request.
    // The battle damage executor must separately handle its queue/lifecycle.
    public static class ChainedStatusModel
    {
        public const int DamageType = 12;
        public static bool Test(CombatTeam? holderTeam, bool deadAndUnrevivable, int stacks,
            CombatTeam? attackedTeam, int damage, int damageType) => damageType != DamageType && damage > 0 &&
            attackedTeam == CombatTeam.Player && holderTeam == CombatTeam.Enemy && !deadAndUnrevivable && stacks > 0;
        public static int Damage(int damage, int stacks) => unchecked(damage * stacks + 1) / 2;
        public static int Magnitude(int stacks) => unchecked(stacks * 50);
    }
}
