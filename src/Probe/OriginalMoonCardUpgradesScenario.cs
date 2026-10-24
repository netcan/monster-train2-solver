using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoonCardUpgradesScenario
    {
        internal const string Name = "original-moon-card-upgrades";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool RoomMask => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_ROOM_MASK") == "1";
        internal static bool Lunacy => RoomMask && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_LUNACY") == "1";
        internal static bool CostRelic => Lunacy && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_COST_RELIC") == "1";
        internal static bool AttackRelic => Lunacy && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_ATTACK_RELIC") == "1";
        internal static bool HealingRelic => Lunacy && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_HEALING_RELIC") == "1";
        internal static bool PhaseHealRelic => Lunacy && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_PHASE_HEAL_RELIC") == "1";
        internal static bool PhaseEnergyRelic => Lunacy && Environment.GetEnvironmentVariable("MT2_PROBE_MOON_UPGRADE_PHASE_ENERGY_RELIC") == "1";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<HandSample> Hands = new List<HandSample>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal sealed class HandSample
        {
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30);
            // Durable enemies keep all ten offensive casts observable before combat.
            foreach (CharacterData character in all.GetAllCharacterData()) Set(character, "health", 500);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 2000); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            Set(managers.GetPlayerManager(), "isMoonPhaseTrackingActive", true);
            Set(managers.GetPlayerManager(), "currentMoonPhase", PlayerManager.MoonPhase.New);
            CardData Authored(int suffix, string effectType, int value)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(item => item.GetCardType() == CardType.Spell));
                data.name = "PojuMoonCardUpgradeControl" + suffix; Set(data, "id", "ac110035-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectType, null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Authored(1, "CardEffectGainEnergy", 20), newMoon = Authored(2, "CardEffectSetMoonPhase", 1), fullMoon = Authored(3, "CardEffectSetMoonPhase", 2);
            string[] originals = { "87e45427-921e-45d3-b4c4-9c156a83cedf", "753f20a2-c625-47b3-be4b-8600b673841e",
                "108b9153-0236-4ba2-ad20-d03a991bbd57", "cbf4d65d-afdb-4fd8-a7ce-216d58a6b486", "8757357b-c36f-476d-88de-b9a481d89fcb" };
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            var created = new List<CardState> { Add(steward.GetID()), Add(steward.GetID()), Add(originals[2]), Add(battery.GetID()), Add(newMoon.GetID()) };
            created.AddRange(originals.Select(Add)); created.Add(Add(fullMoon.GetID())); created.AddRange(originals.Select(Add));
            if (RoomMask) created.Add(Add(EclipseChamberScenario.DataId));
            if (Lunacy)
            {
                CardData refill = Authored(4, "CardEffectGainEnergy", 3);
                created.AddRange(new[] { Add("e9fa09e5-13bd-4826-a8e5-969332cbfbbf"), Add(refill.GetID()),
                    Add("e9fa09e5-13bd-4826-a8e5-969332cbfbbf"), Add("e9fa09e5-13bd-4826-a8e5-969332cbfbbf") });
            }
            if (CostRelic) created.AddRange(new[] { Add(OriginalCardTypeCostScenario.BlightId), Add(OriginalCardTypeCostScenario.JunkId) });
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardUpgradeData numeric = all.GetAllCardUpgradeData().First(upgrade => upgrade.GetBonusDamage() > 0 && upgrade.GetTraitDataUpgrades().Count == 0 &&
                upgrade.GetCardTriggerUpgrades().Count == 0 && upgrade.GetStatusEffectUpgrades().Count == 0);
            foreach (CardState card in new[] { created[7], created[13] })
            { var upgrade = new CardUpgradeState(); upgrade.Setup(numeric); card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true); card.ReapplyMagicPowerScalingFromTraitsToAllExistingUpgrades(managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MOON-CARD-UPGRADES-PREPARED five unchanged spell definitions at New/Full, hand callbacks, unplayed redraw/reset, positive magic power and full native chains.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Enumerable.Range(0, ids.Length).Where(i => i != 2 && source.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            if (RoomMask && index == 5 && source.Spawn.Train.Context!.Statistics!.Value(ids[16], "TimesPlayed", "ThisBattle") == 0) index = 16;
            CombatUnit? target = index is 5 or 11 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[1]) :
                index is 6 or 12 or 17 or 19 or 20 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]) : null;
            int roomIndex = index == 1 ? 1 : target == null ? 0 : source.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == target.Id)).RoomIndex;
            var action = new PlayCardAction(ids[index], roomIndex, playerPosition: index < 2 ? 0 : -1, targetUnitId: target?.Id ?? 0);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-MOON-CARD-UPGRADE-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.UpdateMoonPhasePlayerResource))]
        private static class HandPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result); }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; var row = new HandSample { Before = trace.CaptureContext() }; Hands.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureContext(); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
