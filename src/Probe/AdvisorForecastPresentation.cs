using System;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AdvisorForecastPresentation
    {
        internal static bool IsTerminal(RoomOutcome outcome) =>
            outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;

        internal static string Describe(RoomOutcome outcome, int turns, int currentHp, int forecastHp)
        {
            int loss = Math.Max(0, currentHp - forecastHp);
            if (!IsTerminal(outcome))
                return "部分方案 · 仅预测 " + turns + " 回合\n窗口末预计薪火 " + forecastHp +
                    " 血 · 窗口内预计损失 " + loss + "\n整场战损尚未确定";
            string result = outcome == RoomOutcome.BattleWon ? "整场预计获胜" :
                outcome == RoomOutcome.PlayerDefeated ? "整场预计战败" : "预计僵持";
            return result + " · 薪火 " + currentHp + " → " + forecastHp + " 血 · 后续预计损失 " + loss;
        }

        internal static string Ability(string unit, string ability, int sourceRoom, int? currentCost) =>
            "【单位技能】" + unit + "：" + ability +
            (currentCost.HasValue ? " · 当前 " + currentCost + " 费" : "") +
            (sourceRoom >= 0 ? "\n来源：当前第 " + (sourceRoom + 1) + " 层的 " + unit : "");
    }
}
