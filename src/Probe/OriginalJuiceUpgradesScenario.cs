using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalJuiceUpgradesScenario
    {
        internal const string Name = "original-juice-upgrades";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static object? CurrentDefinitions => all == null ? null : CaptureDefinitions();
        private static AllGameData? all;
        private static CardUpgradeData[] upgrades = Array.Empty<CardUpgradeData>();
        private static CardData[] spells = Array.Empty<CardData>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static object CaptureDefinitions() => new {
            Spells = spells.Select(card => BattleActionProbe.Definition(card)).ToArray(),
            Upgrades = upgrades.Select(data => { var state = new CardUpgradeState(); state.Setup(data);
                return CardModifierProbe.Upgrade(state); }).ToArray()
        };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Juice upgrade comparison requires isolated deployment.");
            upgrades = all.GetAllCardUpgradeData().Where(upgrade => upgrade.GetTraitDataUpgrades()
                .Any(trait => CardJuiceModel.IsTrait(trait.GetTraitStateName()) && trait.GetParamInt() == 0))
                .OrderBy(upgrade => upgrade.name, StringComparer.Ordinal).ToArray();
            if (!upgrades.Select(upgrade => upgrade.name).SequenceEqual(new[] { "MollisT1_Upgrade", "MollisT2_Upgrade", "MollisT3_Upgrade" }))
                throw new InvalidOperationException("Original Juice soul upgrade family differs.");
            spells = new[] { "3020f56f-71a5-4155-8bed-48ada7e7e0de", "f9f20326-dccb-44af-894b-467534afab4a" }
                .Select(id => all.FindCardData(id)!).ToArray();
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 60);
            Set(steward.GetSpawnCharacterData(), "health", 5000); Set(steward.GetSpawnCharacterData(), "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 24);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            var battery = UnityEngine.Object.Instantiate(spells[1]); battery.name = "PojuJuiceEnergyControl";
            Set(battery, "id", "fc231ba1-0000-4000-8000-000000000001"); Set(battery, "cost", 0);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "triggers", new List<CardTriggerEffectData>());
            Set(battery, "effectTriggers", new List<CharacterTriggerData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room);
            Set(energy, "paramInt", 40); Set(battery, "effects", new List<CardEffectData> { energy });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            var created = new List<CardState> { Add(steward), Add(steward), Add(battery) };
            foreach (var upgrade in upgrades)
                foreach (var data in new[] { spells[0], spells[1], spells[1] })
                {
                    var card = Add(data); var state = new CardUpgradeState(); state.Setup(upgrade);
                    card.ApplyPermanentUpgrade(state, save, ignoreUpgradeAnimation: true);
                    var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                    created.Add(card);
                }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray(); Definitions = CaptureDefinitions();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-JUICE-UPGRADES-PREPARED three discovered soul upgrades, two unchanged status spells, friendly/enemy cast callbacks.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int room = index == 1 ? 2 : 0, target = 0;
            if (index >= 3)
                target = (index - 3) % 3 == 2
                    ? source.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy).Id
                    : source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == CardIds[0]).Id;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                play.CardInstanceId == CardIds[index] && play.RoomIndex == room && play.TargetUnitId == target);
        }
    }
}
