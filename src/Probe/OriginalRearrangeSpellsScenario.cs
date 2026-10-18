using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRearrangeSpellsScenario
    {
        internal const string Name = "original-rearrange-spells";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Control(int suffix, string effectName, int value, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuRearrangeControl" + suffix; Set(data, "id", "ac110051-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                if (effectName == "CardEffectAdjustCorruption") Set(effect, "paramBool", true);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Control(1, "CardEffectGainEnergy", 20, Team.Type.None), health = Control(2, "CardEffectBuffMaxHealth", 1200, Team.Type.Heroes),
                corruption = Control(3, "CardEffectAdjustCorruption", 3, Team.Type.None),
                firstOnly = Control(4, "CardEffectFloorRearrange", 1, Team.Type.Heroes | Team.Type.Monsters);
            firstOnly.GetEffects()[0].Cheat_SetTargetMode(TargetMode.FrontInRoom);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(health.GetID()), Add(corruption.GetID()),
                Add("4a9b228e-d3d2-4de8-8cc5-f66fe20af14b"), Add("1450e56b-ca09-453a-937a-97bd44bcb3de"), Add("5ae81798-157e-4fcb-b4e6-822ff40843b0"),
                Add("ac87d3d4-c542-4950-878a-50cbc9a45919"), Add("aaaddead-a999-40ae-8327-a6f977645c04"), Add("1185bf27-1e2a-478a-af93-500a864ebad4"),
                Add("37f4e51b-3771-4a9a-aaae-910c3062ba64"), Add("1737fa0b-7f37-41e8-9183-ed9e6a391556"), Add("392dce9e-ad56-4834-82cc-053705610145"),
                Add("d35bfe1d-d05e-403a-b6ab-38ce30d3e2ea"), Add("11c9415e-3316-4e86-8a38-fe61f7e43cbd"), Add("11c9415e-3316-4e86-8a38-fe61f7e43cbd"), Add(firstOnly.GetID()) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-REARRANGE-SPELLS-PREPARED eleven unchanged originals; damage/status/attack chains, Corruption and positive/zero X.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int target = index is >= 6 and <= 10 || index == 16 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
                index == 17 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MageT1_Junker").Id :
                index is 11 or 13 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[1]).Id :
                index is 12 or 14 or 15 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            var action = new PlayCardAction(ids[index], index == 2 || index == 15 ? 1 : 0, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-REARRANGE-SPELLS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
