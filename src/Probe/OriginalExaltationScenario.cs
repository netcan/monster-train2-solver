using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalExaltationScenario
    {
        internal const string Name = "original-exaltation";
        internal const string OriginalId = "1a692e89-3212-4dd4-ba09-072d2ddade79";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Exaltation requires isolated deployment.");
            CardData original = all.FindCardData(OriginalId)!;
            Definition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                CostType = original.GetCostType().ToString(), Effects = original.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray() };
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { HealingScenario.HealGold(1, false, true) }).ToList());
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 24);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Control(int suffix, string type, int amount, bool targetless, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalId)); data.name = "PojuExaltation" + suffix;
                Set(data, "id", "fc231b80-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter); Set(effect, "paramInt", status == null ? amount : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = amount } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 20, true);
            var damage = Control(2, "CardEffectDamage", 7, false);
            var oneHealth = Control(3, "CardEffectSetHealth", 1, false);
            var blank = Control(4, "CardEffectNULL", 0, true);
            var enemyHealth = Control(5, "CardEffectSetHealth", 1, false);
            Set(enemyHealth.GetEffects()[0], "targetTeamType", Team.Type.Heroes);
            var immune = Control(6, "CardEffectAddStatusEffect", 1, false, "heal immunity");
            var cleanse = Control(7, "CardEffectRemoveStatusEffect", 99, false, "heal immunity");
            CardData Splash(int suffix, int damageAmount, bool ranged = false)
            {
                var data = Control(suffix, ExaltationModel.NativeEffect, damageAmount, false);
                var effect = data.GetEffects()[0]; Set(effect, "additionalParamInt", 20);
                if (ranged) { Set(effect, "useIntRange", true); Set(effect, "paramMinInt", 4); Set(effect, "paramMaxInt", 10); Set(effect, "paramMultiplier", 1.5f); }
                return data;
            }
            var zero = Splash(8, 0); var negative = Splash(9, -5); var range = Splash(10, 10, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Exaltation control card: " + data.name);
            var created = new[] { Add(steward), Add(steward), Add(steward), Add(battery), Add(damage), Add(original),
                Add(enemyHealth), Add(original), Add(oneHealth), Add(original), Add(original), Add(damage), Add(original),
                Add(damage), Add(original), Add(damage), Add(immune), Add(original), Add(cleanse), Add(zero), Add(negative), Add(range) };
            var magic = new CardUpgradeState(); magic.Setup(); magic.SetAttackDamage(5); magic.SetAdditionalHeal(123);
            created[14].ApplyPermanentUpgrade(magic, save, ignoreUpgradeAnimation: true);
            foreach (var card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-EXALTATION-PREPARED unchanged spell; player/enemy healing, mixed splash, exclusion, lethal bystander and isolated target controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int floor = index == 2 || index is 10 or 11 or 12 ? 2 : 0;
            int target = 0;
            if (index is 6 or 7) target = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units
                .Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id;
            else if (index >= 4) target = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units
                .Single(unit => unit.SpawnerCardId == CardIds[index == 8 ? 1 : floor == 2 ? 2 : 0]).Id;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
        }
    }
}
