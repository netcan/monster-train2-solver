using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossAllyUpgradeScenario
    {
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public int CardId { get; set; }
            public string UpgradeName { get; set; } = "";
            public string UpgradeDataId { get; set; } = "";
            public CardUpgradeModifier Upgrade { get; set; } = null!;
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
        }
        internal static string Kind => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_ALLY_UPGRADE") ?? "";
        internal static bool Enabled => Kind.Length > 0;
        internal static Record? Evidence { get; private set; }
        internal static bool Prepare()
        {
            if (!Enabled || Evidence != null) return false;
            AllGameManagers managers = AllGameManagers.Instance;
            SaveManager save = managers.GetSaveManager();
            if (!TrueFinalBossTurnProbe.Enabled || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("TFB ally upgrade requires isolated original deployment.");
            CardState card = managers.GetCardManager().GetHand().First(item =>
                item.GetSpawnCharacterData()?.GetAssetKey() == "TrainStewardBig");
            CardUpgradeData data = Definition(save.GetAllGameData(), Kind);
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new Record { Kind = Kind, CardId = trace.CardId(card), UpgradeName = data.name,
                UpgradeDataId = data.GetID(), Before = trace.CaptureDecision() };
            var upgrade = new CardUpgradeState(); upgrade.Setup(data);
            card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            row.Actual = trace.CaptureDecision();
            row.Upgrade = row.Actual.Spawn.Train.Context!.FindCard(row.CardId)!.Permanent.Upgrades.Single();
            Evidence = row;
            return true;
        }
        internal static CardUpgradeData Definition(AllGameData all, string kind) => all.GetAllCardUpgradeData().Single(item =>
            item.GetID() == (kind == "Health" ? "32c1b067-d47d-4841-b166-1a227378a937" : "1f0a08f2-ab63-442e-a089-a1063bf7e7aa") &&
            item.name == (kind == "Health" ? "BlessingBonusHealth" : "EnhanceAttackOnConsume") &&
            item.GetBonusHP() == (kind == "Health" ? 20 : 0) && item.GetBonusDamage() == (kind == "Damage" ? 10 : 0) &&
            item.GetCostReduction() == 0 && item.GetXCostReduction() == 0 && item.GetBonusHeal() == 0 && item.GetBonusSize() == 0 &&
            item.GetBonusEquipment() == 0 && item.GetBonusUpgradeSlotCount() == 0 && item.GetStatusEffectUpgrades().Count == 0 &&
            item.GetTraitDataUpgrades().Count == 0 && item.GetRemoveTraitUpgrades().Count == 0 && item.GetCharacterTriggerUpgrades().Count == 0 &&
            item.GetCardTriggerUpgrades().Count == 0 && item.GetRoomModifierUpgrades().Count == 0 && item.GetUpgradesToRemove().Count == 0 &&
            item.GetUnitAbilityUpgrade() == null && item.GetRoomAbilityUpgrade() == null && !item.GetUpgradeWillBeScaledByNonMagicPowerTrait());
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (state.Spawn.Train.Context!.Statistics!.Value(Evidence!.CardId, "TimesPlayed", "ThisBattle") > 0) return null;
            return BattleActionModel.EnumerateActions(state, Evidence.CardId, includeAbilities: false).SupportedActions
                .First(action => action.RoomIndex == 0 && action.PlayerPosition == 0);
        }
    }
}
