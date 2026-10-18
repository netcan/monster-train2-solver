using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusCompositionsScenario
    {
        internal const string Name = "original-status-compositions";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly string[] Originals = {
            "537f6adf-0fd2-4c6b-88a8-e6cefeb52a7e", // Benediction
            "818357bd-4753-4079-a668-96e9dbd01094", // Divinity
            "82684961-63e8-4a64-8da4-5c20120243a9", // Full Throttle
            "2260a35e-0570-4e22-a3e0-4c39acfeb933", // Ensnare
            "a7207665-b6d1-4cd1-997e-8a7bb789c766", // Energy Siphon
            "2ffb1e93-357a-4d82-800a-f41524766981", // Acid Shower
            "c65e6eec-50c7-4ca1-b135-dfbe46e61213"  // Dragon's Breath
        };
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Single(character => character.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuStatusCompositionsBattery"; Set(battery, "id", "ac110037-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 20);
            Set(battery, "effects", new List<CardEffectData> { energy });
            CardData blank = UnityEngine.Object.Instantiate(battery); blank.name = "PojuStatusCompositionsDrawControl"; Set(blank, "id", "ac110037-0000-4000-8000-000000000002");
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
            Set(blank, "effects", new List<CardEffectData> { noop });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).AddRange(new[] { battery, blank });
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()) }.Concat(Originals.Select(Add)).ToArray();
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STATUS-COMPOSITIONS-PREPARED seven unchanged original spells, friendly Valor/shield/Burst, enemy Rooted/Spell Weakness/Unstable/Pyregel, ordered damage and future draw.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int target = index == 4 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id :
                index is 6 or 7 ? source.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).OrderByDescending(unit => unit.Health).First().Id : 0;
            var action = new PlayCardAction(ids[index], index == 1 ? 1 : 0, playerPosition: index < 2 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-STATUS-COMPOSITION-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
