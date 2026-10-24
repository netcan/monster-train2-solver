using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonsterTrain2Poju.Probe
{
    internal static class AdvisorCardPresentation
    {
        // These are current native values, not the predicted values at a later
        // point in the plan. Rebuild them only when publishing/viewing advice.
        internal static string Describe(CardState card, int handIndex, int? cost)
        {
            string location = handIndex < 0 ? "当前不在手牌" : "当前手牌左起第 " + (handIndex + 1) + " 张";
            string energy = cost.HasValue ? " · 当前 " + (card.IsConsumeRemainingEnergyCostType() ? "X" : cost.Value.ToString()) + " 费" : "";
            string damage = card.GetCardType() == CardType.Spell ? "法强" : "攻击";
            var permanent = card.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => Upgrade(upgrade, damage)).ToArray();
            var temporary = card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(upgrade => Upgrade(upgrade, damage)).ToArray();
            return Plain(card.GetTitle()) + " · " + location + energy +
                "\n当前升级：" + (permanent.Length == 0 ? "无" : string.Join("、", permanent)) +
                (temporary.Length == 0 ? "" : "；临时：" + string.Join("、", temporary));
        }

        internal static string Upgrade(CardUpgradeState upgrade, string damage)
        {
            var stats = new List<string>();
            void Stat(string name, int value)
            { if (value != 0) stats.Add(name + (value > 0 ? "+" : "") + value); }
            Stat(damage, upgrade.GetAttackDamage());
            Stat("攻击", upgrade.GetAttackDamageBuff());
            Stat("生命", upgrade.GetAdditionalHP());
            Stat("费用", -upgrade.GetCostReduction());
            Stat("X费用", -upgrade.GetXCostReduction());
            Stat("治疗", upgrade.GetAdditionalHeal());
            Stat("容量", upgrade.GetAdditionalSize());
            string title = upgrade.GetUpgradeTitleKey().HasTranslation() ? Plain(upgrade.GetUpgradeTitle()) : "";
            if (title.Length == 0 && stats.Count == 0) title = "特殊效果";
            return title.Length == 0 ? string.Join("，", stats) :
                title + (stats.Count == 0 ? "" : "（" + string.Join("，", stats) + "）");
        }

        private static string Plain(string value) => Regex.Replace(value, "<[^>]*>", "").Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}
