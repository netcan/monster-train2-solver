namespace MonsterTrain2Poju.Model
{
    public static class PyreBaneModel
    {
        public static bool Targetable(CombatUnit unit, bool inCombat) => !inCombat || !Dormant(unit);
        public static bool ExcludedFromRoomMode(CombatUnit unit, string mode, bool inCombat = false) =>
            Dormant(unit) && (inCombat || mode is "FrontInRoom" or "RandomInRoom");
        private static bool Dormant(CombatUnit unit) => unit.Health == 1 &&
            unit.Status("pyrebane")?.Stacks > 0 && unit.Status("lifelink")?.Stacks > 0;
    }
}
