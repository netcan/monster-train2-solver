using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossOrdinaryAllyScenario
    {
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public int AnchorCardId { get; set; }
            public int CardId { get; set; }
            public string CardDataId { get; set; } = "";
            public string UpgradeKind { get; set; } = "";
            public CardPlayRule Definition { get; set; } = null!;
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
        }
        internal static string Kind => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_ORDINARY_ALLY") ?? "";
        internal static bool Enabled => Kind.Length > 0;
        internal static string UpgradeKind => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_ORDINARY_ALLY_UPGRADE") ?? "";
        internal static Record? Evidence { get; private set; }
        internal static bool Prepare()
        {
            if (!Enabled || Evidence != null) return false;
            AllGameManagers managers = AllGameManagers.Instance;
            if (!TrueFinalBossTurnProbe.Enabled || managers.GetSaveManager().PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Ordinary ally calibration requires isolated original TFB deployment.");
            CardManager cards = managers.GetCardManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            string id = Kind == "Steward" ? "38e70e6a-20f3-4e36-8d31-c5110be07f87" : Kind == "Warrior" ?
                "5de24977-6800-4015-a20e-9949e79e057c" : "e66178fd-b8e7-401c-b570-7832840ea902";
            var row = new Record { Kind = Kind, UpgradeKind = UpgradeKind, CardDataId = id, Before = trace.CaptureDecision(),
                AnchorCardId = trace.CardId(cards.GetHand().First(card => card.GetSpawnCharacterData()?.GetAssetKey() == "TrainStewardBig")) };
            CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(id)!;
            row.Definition = BattleActionProbe.Definition(data);
            CardState added = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            foreach (string kind in UpgradeKind == "Both" ? new[] { "Health", "Damage" } : UpgradeKind.Length == 0 ? Array.Empty<string>() : new[] { UpgradeKind })
            {
                var upgrade = new CardUpgradeState();
                upgrade.Setup(TrueFinalBossAllyUpgradeScenario.Definition(managers.GetSaveManager().GetAllGameData(), kind));
                added.ApplyPermanentUpgrade(upgrade, managers.GetSaveManager(), ignoreUpgradeAnimation: true);
            }
            row.CardId = trace.CardId(added);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            row.Actual = trace.CaptureDecision();
            Evidence = row;
            return true;
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            foreach (int id in new[] { Evidence!.AnchorCardId, Evidence.CardId })
                if (state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateActions(state, id, includeAbilities: false).SupportedActions
                        .First(action => action.RoomIndex == 0 && action.PlayerPosition == (id == Evidence.CardId ? 1 : 0));
            return null;
        }
    }
}
