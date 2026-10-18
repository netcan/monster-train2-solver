using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpellCompositionsScenario
    {
        internal const string Name = "original-spell-compositions";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly string[] Originals = {
            "a6c8ce76-3c82-47ca-a6e0-8a20e84558dd", // Flash Freeze
            "6a6c4a72-3e32-4995-8070-68d9240de0d5", // Dark Deal
            "4bf98c5f-73c5-42ed-af98-9dc8cf77c1ac", // Engraft
            "d5dd7d58-dd1b-4c46-9308-77179c39c915", // Glimmer
            "25787bff-d2fb-4d9a-a636-d96a5863d559", // Focused Growth
            "f7839c43-c139-4671-9f53-9347673e3b0b", // Fae's Blessing
            "64beabe3-0260-44e2-a47a-b07192b52f25"  // Enflame
        };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Single(character => character.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuSpellCompositionsBattery"; Set(battery, "id", "ac110033-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 10);
            Set(battery, "effects", new List<CardEffectData> { energy });
            CardData blank = UnityEngine.Object.Instantiate(battery); blank.name = "PojuSpellCompositionsDrawControl"; Set(blank, "id", "ac110033-0000-4000-8000-000000000002");
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramBool2", true);
            Set(blank, "effects", new List<CardEffectData> { noop });
            List<CardData> definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all); definitions.Add(battery); definitions.Add(blank);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()) }.Concat(Originals.Select(Add)).ToArray();
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SPELL-COMPOSITIONS-PREPARED seven unchanged original spells, positive damage/heal/overheal/status compositions, floor controls and future draw.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int target = index >= 3 && index != 6 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
