using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDevotionUpgradeScenario
    {
        internal const string Name = "original-devotion-upgrade";
        internal const string OriginalId = "0bf610e9-b1a0-4f02-a75c-71baca900fd1";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? OriginalDefinition { get; private set; }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                DevotionProbe.Read(save, "GetDevotionLevel") != 0 || DevotionProbe.Read(save, "GetDevotionPoints") != 0)
                throw new InvalidOperationException("Original Devotion upgrades require isolated zero-Devotion deployment.");
            CardData original = all.FindCardData(OriginalId)!; CharacterData originalUnit = original.GetSpawnCharacterData();
            OriginalDefinition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Character = originalUnit.GetAssetKey(),
                Attack = originalUnit.GetAttackDamage(), Health = originalUnit.GetHealth(), Size = originalUnit.GetSize(),
                Triggers = originalUnit.GetTriggers().Select(trigger => new { Kind = trigger.GetTrigger().ToString(),
                    Effects = trigger.GetEffects().Select(effect => new { Raw = RuleCatalogProbe.Effect(effect),
                        UpgradeId = effect.GetParamCardUpgradeData()?.GetID(), UpgradeKey = effect.GetParamCardUpgradeData()?.name }).ToArray() }).ToArray() };
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Control(int suffix, string type, int amount)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755")); data.name = "PojuDevotionUpgrade" + suffix;
                Set(data, "id", "fc231b75-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", true); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room);
                Set(effect, "paramInt", amount); Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            int first = DevotionProbe.Read(all.GetBalanceData(), "GetMonolithDevotionFirstThreshold");
            int increment = DevotionProbe.Read(all.GetBalanceData(), "GetMonolithDevotionThresholdIncreasePerLevel");
            var battery = Control(1, "CardEffectGainEnergy", 10);
            var levelOne = Control(2, "CardEffectGainDevotionPoints", first);
            var levelTwo = Control(3, "CardEffectGainDevotionPoints", first + increment);
            var blank = Control(4, "CardEffectNULL", 0);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            var created = new[] { Add(original), Add(steward), Add(battery), Add(levelOne), Add(original), Add(levelTwo), Add(original) };
            foreach (var card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DEVOTION-UPGRADE-PREPARED unchanged DarkInitiate at levels zero, one and two; no retroactive upgrades.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 4 ? 2 : index == 6 ? 1 : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] && play.RoomIndex == floor);
        }
    }
}
