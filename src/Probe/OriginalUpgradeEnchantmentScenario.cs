using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUpgradeEnchantmentScenario
    {
        internal const string Name = "original-upgrade-enchantment";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object[] Definitions { get; private set; } = Array.Empty<object>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Upgrade enchantments require isolated deployment.");
            string[] nativeIds = { "6c3eb0bb-4871-438d-9b5c-fcef1f94bf70", "983495d0-11cb-49d7-9f3e-8531c6f18676", "462f5b87-cace-447d-8818-3d4b7bc8eb7f" };
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var originals = nativeIds.Select(id => all.FindCardData(id)!).ToArray();
            Definitions = originals.Select(card => (object)new { Id = card.GetID(), Key = card.name, CostType = card.GetCostType().ToString(),
                Character = card.GetSpawnCharacterData().GetAssetKey(), Attack = card.GetSpawnCharacterData().GetAttackDamage(),
                Health = card.GetSpawnCharacterData().GetHealth(), Size = card.GetSpawnCharacterData().GetSize(),
                Triggers = card.GetSpawnCharacterData().GetTriggers().Select(trigger => new { Kind = trigger.GetTrigger().ToString(),
                    Effects = trigger.GetEffects().Select(effect => { CardUpgradeModifier? upgrade = null;
                        if (effect.GetParamCardUpgradeData() != null) { var state = new CardUpgradeState(); state.Setup(effect.GetParamCardUpgradeData());
                            upgrade = CardModifierProbe.Upgrade(state, false, filtersAlreadyApplied: true); }
                        return new { Raw = RuleCatalogProbe.Effect(effect), Upgrade = upgrade }; }).ToArray() }).ToArray() }).ToArray();
            CardData Proxy(CardData original, int suffix)
            {
                var data = UnityEngine.Object.Instantiate(original); data.name = "PojuUpgradeEnchantment" + suffix;
                Set(data, "id", "fc231b79-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "costType", CardData.CostType.Default); definitions.Add(data); return data;
            }
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 20);
            CardData Control(int suffix, string type, int amount, bool targetless, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755")); data.name = "PojuUpgradeEnchantment" + suffix;
                Set(data, "id", "fc231b79-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter); Set(effect, "paramInt", status == null ? amount : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = amount } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(10, "CardEffectGainEnergy", 10, true);
            var mute = Control(11, "CardEffectAddStatusEffect", 1, false, "muted");
            var unmute = Control(12, "CardEffectRemoveStatusEffect", 99, false, "muted");
            var ascend = Control(13, "CardEffectBump", 1, false); var descend = Control(14, "CardEffectBump", -1, false);
            var kill = Control(15, "CardEffectDamage", 1000, false); var blank = Control(16, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing native enchantment control card: " + data.name);
            var created = new[] { Add(steward), Add(battery), Add(Proxy(originals[0], 1)), Add(Proxy(originals[1], 2)), Add(Proxy(originals[2], 3)),
                Add(mute), Add(unmute), Add(ascend), Add(descend), Add(kill), Add(kill), Add(kill) };
            foreach (var card in created.Skip(1)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-UPGRADE-ENCHANTMENT-PREPARED three unchanged BlindingDarkshard character definitions through playable control proxies; stacking/mute/movement/death/preview.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 1 && source.Spawn.Turn < (index == 11 ? 2 : 1)) return null;
            int floor = index == 8 ? 1 : 0;
            int sourceIndex = index is 5 or 6 or 9 ? 2 : index is 7 or 8 or 10 ? 3 : 4;
            int target = index >= 5 ? source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units
                .Single(unit => unit.SpawnerCardId == CardIds[sourceIndex]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
        }
    }
}
