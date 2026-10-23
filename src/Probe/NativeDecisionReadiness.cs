using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class NativeDecisionReadiness
    {
        internal static bool CardsDrawn(CombatManager combat)
        {
            if (combat.HaveCardsBeenDrawnForCurrentTurn()) return true;
            if (AllGameManagers.Instance.GetSaveManager().GetCurrentScenarioData()?.name !=
                "Level6AngelDuo_RallyAttackIncantHealth") return false;
            int skipped = (int)AccessTools.Field(typeof(CombatManager), "turnsRemainingWhenRelentlessTriggeredEarly").GetValue(combat);
            int waves = (int)AccessTools.Field(typeof(CombatManager), "totalWaves").GetValue(combat);
            int drawn = (int)AccessTools.Field(typeof(CombatManager), "turnCardsNotYetDrawn").GetValue(combat);
            return skipped > 0 && combat.GetTurnCount() == waves - 1 && combat.GetTurnCount() - skipped < drawn;
        }
    }
}
