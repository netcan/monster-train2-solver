using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFriendlyDamageStatusScenario
    {
        internal const string Name = "original-friendly-damage-status";
        internal const string OriginalId = "67678536-8a3b-4eb5-8c3f-50137c36f038";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? OriginalDefinition { get; private set; }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Original friendly-damage statuses require isolated deployment.");
            CardData original = all.FindCardData(OriginalId)!; CharacterData originalUnit = original.GetSpawnCharacterData();
            OriginalDefinition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Character = originalUnit.GetAssetKey(),
                Attack = originalUnit.GetAttackDamage(), Health = originalUnit.GetHealth(), Size = originalUnit.GetSize(),
                Triggers = originalUnit.GetTriggers().Select(trigger => new { Kind = trigger.GetTrigger().ToString(),
                    Effects = trigger.GetEffects().Select(effect => new { Raw = RuleCatalogProbe.Effect(effect),
                        CopyModifiers = effect.GetCopyModifiersFromSource(), MainSubtypeFilter = effect.GetFilterBasedOnMainSubClass() }).ToArray() }).ToArray() };
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 20);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Control(int suffix, string type, int amount, bool targetless, string? statusId = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755")); data.name = "PojuFriendlyDamageStatus" + suffix;
                Set(data, "id", "fc231b78-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", statusId == null ? amount : 0);
                if (statusId != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = statusId, count = amount } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 10, true);
            var damage = Control(2, "CardEffectDamage", 7, false);
            var armor = Control(3, "CardEffectAddStatusEffect", 20, false, "armor");
            var blocked = Control(4, "CardEffectDamage", 11, false);
            var partial = Control(5, "CardEffectDamage", 14, false);
            var zero = Control(6, "CardEffectDamage", 0, false);
            var shield = Control(7, "CardEffectAddStatusEffect", 1, false, "damage shield");
            var shielded = Control(8, "CardEffectDamage", 13, false);
            var other = Control(9, "CardEffectDamage", 4, false);
            var tenacious = Control(10, "CardEffectAddStatusEffect", 1, false, "tenacious");
            var lethal = Control(11, "CardEffectDamage", 1000, false);
            var blank = Control(12, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing native control hand card: " + data.name);
            var created = new[] { Add(steward), Add(battery), Add(original), Add(original), Add(damage), Add(armor),
                Add(blocked), Add(partial), Add(zero), Add(shield), Add(shielded), Add(other), Add(tenacious), Add(lethal) };
            foreach (var card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-FRIENDLY-DAMAGE-STATUS-PREPARED unchanged ScourgedZealots; direct/full-armor/partial-armor/zero/shield/Tenacious hits and independent victim caches.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 1 && source.Spawn.Turn < 1) return null;
            int floor = index is 3 or 11 ? 2 : index >= 2 ? 1 : 0;
            int target = index >= 4 ? source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.SpawnerCardId == CardIds[floor == 1 ? 2 : 3]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
        }
    }
}
