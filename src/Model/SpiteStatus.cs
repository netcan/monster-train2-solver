namespace MonsterTrain2Poju.Model
{
    public static class SpiteStatusModel
    {
        // OnPreAttacking runs for every damage type before source-card traits,
        // shields and armor. Native tests the current actor HP on each hit.
        public static int Adjust(CombatTeam? attacker, int health, int maximum, int stacks,
            CombatTeam? target, bool targetAlive, int damage) =>
            attacker.HasValue && target.HasValue && targetAlive && attacker != target &&
            unchecked(health * 2) <= maximum && damage > 0
                ? unchecked(damage * unchecked(stacks + 1)) : damage;
    }
}
