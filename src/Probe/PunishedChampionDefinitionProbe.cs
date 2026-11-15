using System;
using System.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class PunishedChampionDefinitionProbe
    {
        internal static readonly string[] Champions = {
            "c58f8d77-8578-4d97-bcf1-beb4cdbe2f27", "d10bb18c-49e9-4895-a1c5-e4ecebbf61a0" };
        internal static CardUpgradeTreeData? Tree(AllGameData data, CardData champion) => data.GetAllClassDatas()
            .Select(clan => clan.FindUpgradeTreeForChampion(champion.GetSpawnCharacterData())).FirstOrDefault(tree => tree != null);
        internal static object[] Capture(AllGameData data) => Champions.Select(id => data.FindCardData(id)).Where(card => card != null)
            .Select(card => (object)new { Id = card!.GetID(), Key = card.name,
                Paths = (Tree(data, card) ?? throw new InvalidOperationException("Missing original champion upgrade tree."))
                    .GetUpgradeTrees().Select((path, index) => new { Index = index,
                        Upgrades = path.GetCardUpgrades().Select((upgrade, rank) => {
                            var state = new CardUpgradeState(); state.Setup(upgrade);
                            return new { Rank = rank + 1, Id = upgrade.GetID(), Key = upgrade.name,
                                Replaces = upgrade.GetUpgradesToRemove().Select(item => item.GetID()).ToArray(),
                                Rule = CardModifierProbe.Upgrade(state), Ability = upgrade.GetUnitAbilityUpgrade()?.GetID(),
                                RoomModifiers = upgrade.GetRoomModifierUpgrades().Select(RuleCatalogProbe.RoomModifier).ToArray(),
                                CardTriggers = upgrade.GetCardTriggerUpgrades().Select(CardUpgradeLifecycleProbe.TriggerDefinition).ToArray(),
                                Triggers = upgrade.GetCharacterTriggerUpgrades().Select(trigger => new {
                                    Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetEffects().Select(RuleCatalogProbe.Effect).ToArray()
                                }).ToArray() };
                        }).ToArray()
                    }).ToArray()
            }).ToArray();
    }
}
