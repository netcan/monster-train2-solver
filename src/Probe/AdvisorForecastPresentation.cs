using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AdvisorForecastPresentation
    {
        internal static string SearchScope(bool wholeBattle, int horizon) =>
            wholeBattle ? "整场搜索" : "未来 " + horizon + " 回合";

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

        internal static string Choices(PlayCardAction action, Func<int, string> cardName, Func<string, string> summonName)
        {
            var cardChoices = action.RecursionSelections.Where(choice => choice.CardInstanceIds.Count != 0).Select(choice =>
                (choice.EffectIndex, Label: "选择 " + string.Join("、", choice.CardInstanceIds.Select(cardName))));
            var summonChoices = action.SummonSelections.Select(choice =>
                (choice.EffectIndex, Label: "选择 " + summonName(choice.CardDataId)));
            var draftChoices = action.MixSelections.OrderBy(choice => choice.EffectIndex).ThenBy(choice => choice.MixNumber)
                .Select((choice, index) => (choice.EffectIndex,
                    Label: "第 " + (index + 1) + " 次备选中选第 " + (choice.ChoiceIndex + 1) + " 项"));
            return string.Concat(cardChoices.Concat(summonChoices).Concat(draftChoices)
                .OrderBy(choice => choice.EffectIndex).Select(choice => " · " + choice.Label));
        }

        internal static string EndTurn(BattleSearchStep step) => "结束回合" +
            (step.RelentlessDecisions?.Count > 0 ? "；" + string.Join("；", step.RelentlessDecisions.Select(choice =>
                "第 " + (choice.RoomIndex + 1) + " 层第 " + choice.CompletedRounds + " 轮" + (choice.Continue ? "继续" : "放弃"))) : "");
    }
}
