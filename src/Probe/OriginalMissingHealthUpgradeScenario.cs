using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMissingHealthUpgradeScenario
    {
        internal const string Name = "original-missing-health-upgrade";
        internal const string OriginalId = "80d43965-4baf-4b30-b4d6-e6bdef08f115";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? OriginalDefinition { get; private set; }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Original missing-health upgrades require isolated deployment.");
            CardData original = all.FindCardData(OriginalId)!; CharacterData originalUnit = original.GetSpawnCharacterData();
            OriginalDefinition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Character = originalUnit.GetAssetKey(),
                Attack = originalUnit.GetAttackDamage(), Health = originalUnit.GetHealth(), Size = originalUnit.GetSize(),
                Triggers = originalUnit.GetTriggers().Select(trigger => new { Kind = trigger.GetTrigger().ToString(),
                    Effects = trigger.GetEffects().Select(effect => {
                        var upgrade = new CardUpgradeState(); upgrade.Setup(effect.GetParamCardUpgradeData());
                        return new { Raw = RuleCatalogProbe.Effect(effect), Upgrade = CardModifierProbe.Upgrade(upgrade, false, filtersAlreadyApplied: true) };
                    }).ToArray() }).ToArray() };
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Control(int suffix, string type, int amount, bool targetless)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755")); data.name = "PojuMissingHealthUpgrade" + suffix;
                Set(data, "id", "fc231b77-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", amount); Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 10, true);
            var heal = Control(2, "CardEffectHeal", 9999, false);
            var damage = Control(3, "CardEffectDamage", 7, false);
            var laterDamage = Control(4, "CardEffectDamage", 5, false);
            var blank = Control(5, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            var created = new[] { Add(steward), Add(battery), Add(original), Add(original), Add(heal), Add(damage), Add(heal), Add(laterDamage) };
            foreach (var card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MISSING-HEALTH-UPGRADE-PREPARED unchanged Mournguards; independent Zeal missing HP, heal-to-full controls, repeated damage/growth and one accumulated source/unit record.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            int requiredTurn = index <= 0 ? 0 : index <= 4 ? 1 : index - 3;
            if (index < 0 || source.Spawn.Turn < requiredTurn) return null;
            int floor = index == 3 ? 2 : index >= 2 ? 1 : 0;
            int target = index >= 4 ? source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.SpawnerCardId == CardIds[2]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
        }
    }
}
