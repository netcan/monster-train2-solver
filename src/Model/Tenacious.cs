namespace MonsterTrain2Poju.Model
{
    public sealed class TenaciousDamageResult
    {
        public int Damage { get; }
        public int Prevented { get; }
        public bool Triggered => Prevented > 0;
        internal TenaciousDamageResult(int damage, int prevented) { Damage = damage; Prevented = prevented; }
    }

    public static class TenaciousModel
    {
        // The character source takes precedence over a card or relic source.
        public static bool IsFriendlySource(CombatTeam target, CombatTeam? attacker, bool hasCard, bool hasRelic) =>
            attacker.HasValue ? attacker.Value == target : target == CombatTeam.Player && (hasCard || hasRelic);

        public static TenaciousDamageResult Adjust(int health, int damage, bool friendly, bool active = true) =>
            !active || !friendly || health <= 0 || damage < health
                ? new TenaciousDamageResult(damage, 0)
                : new TenaciousDamageResult(health - 1, damage - (health - 1));
    }
}
