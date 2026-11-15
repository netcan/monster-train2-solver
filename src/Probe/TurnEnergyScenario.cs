#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TurnEnergyScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "turn-energy";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definition { get; private set; }
        private static SoulData[] originals = Array.Empty<SoulData>();
        internal static object? CurrentDefinition => originals.Length == 0 ? null : Describe();
        private static object Describe() => originals.Select(data => new { Id = data.GetID(), Key = data.name,
            Effects = data.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Initial = effect.GetParamInt(), Reduction = effect.GetParamInt2(),
                Deployment = effect.GetParamBool(), Bonus = effect.GetParamMinInt(), Threshold = effect.GetParamMaxInt(), Conditions = effect.GetEffectConditions().Count }).ToArray() }).ToArray();
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (originals.Length > 0) return;
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Turn energy originals require clean pre-battle acquisition.");
            originals = new[] { all.FindSoulData("fa66e79e-800e-4753-afe6-de333ef678e4"), all.FindSoulData("75b688f5-daa0-487b-bf02-81d5ff014135") };
            if (originals.Any(data => data == null) || originals[0].name != "SolarisT1" || originals[1].name != "WormgutT3")
                throw new InvalidOperationException("Original Solaris/Wormgut sources changed.");
            Definition = Describe(); foreach (var data in originals) save.AddSelectedSoul(data);
            var capacity = all.GetAllCollectableRelicData().Single(data => data.GetID() == "aab4971f-05c6-430a-8a6a-dfb654a41b0c");
            if (capacity.name != "SilverIncreasedCapacity" || capacity.GetEffects().Count != 1 || capacity.GetEffects()[0].GetEffectClassName() != RelicResourceModel.FloorCapacity || capacity.GetEffects()[0].GetParamInt() != 2)
                throw new InvalidOperationException("Original capacity companion changed.");
            save.AddRelic(capacity);
            log.LogInfo("TURN-ENERGY-ACQUIRED unchanged SolarisT1/WormgutT3 souls and SilverIncreasedCapacity before native room setup.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var cards = managers.GetCardManager(); var all = save.GetAllGameData();
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Dynamic turn energy requires isolated deployment.");
            if (originals.Length != 2 || !save.GetSelectedSouls().Select(soul => soul.GetRelicDataID()).OrderBy(id => id).SequenceEqual(originals.Select(data => data.GetID()).OrderBy(id => id)))
                throw new InvalidOperationException("Missing original energy soul roster.");
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            AccessTools.Field(typeof(CardData), "cost").SetValue(steward, 0);
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 80);
            AccessTools.Field(typeof(CharacterData), "health").SetValue(steward.GetSpawnCharacterData(), 5000);
            AccessTools.Field(typeof(CharacterData), "size").SetValue(steward.GetSpawnCharacterData(), 6);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var ids = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                var card = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
                if (i == 3)
                {
                    var smaller = new CardUpgradeState(); smaller.Setup(); smaller.SetAdditionalSize(-4);
                    card.ApplyPermanentUpgrade(smaller, save, ignoreUpgradeAnimation: true);
                }
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                ids.Add(FullBattleTrace.Active!.CardId(card));
            }
            CardIds = ids.ToArray(); Prepared = true;
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("TURN-ENERGY-PREPARED original souls/capacity; four authored size 6/6/6/2 durable unit controls; native size threshold twenty.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, card => source.Spawn.Train.Context!.Statistics!.Value(card, "TimesPlayed", "ThisBattle") == 0);
            return index < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source, CardIds[index]).FirstOrDefault(play => play.RoomIndex == (index == 3 ? 0 : index));
        }
    }
}
#endif
