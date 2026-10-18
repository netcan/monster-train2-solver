using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBaseChampionsScenario
    {
        internal const string Name = "original-base-champions";
        internal static readonly string[] Originals = { "0422f6ca-896c-4676-a01f-71d8d4b232fb", "0c43beba-20d6-4917-a624-e0c63a21a93a",
            "15e25a50-5235-4c45-803b-cada23022671", "2cbf61fc-f26b-4a57-a738-79589586dba7", "2e64ddf0-fad1-4bf2-80d2-68fd928ecd28",
            "31f56ada-fd59-4012-93be-88e69e55ba92", "35fcb075-2aeb-4a51-92f9-e3d198784906", "41600212-9122-46e2-a607-545a86830574",
            "49b6e610-56a8-4b97-990d-d9a2a093b266", "7052d604-bf4b-49cb-a1a9-1a0c47033c50", "7cff7749-0803-402c-8a9a-965222c24b4b",
            "8b6c19ce-c8d0-4a77-b33a-e490d0d1b282", "b0def3ad-c519-4ef7-b2d0-4af0467806b9", "b478b414-8d3f-4007-993f-efdf3640bbfb",
            "bd2fe27a-e8d9-4f1b-ae3c-549f1428e441", "d14a50f3-728d-43e1-87f0-ef1b013f6697", "d2a5feba-4ecd-4d5d-a206-ec2ebfb96667",
            "dfc5f5f4-e328-44cd-b4f3-758cbc1d5fe2" };
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
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 0); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData capacity = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            capacity.name = "PojuBaseChampionCapacity"; Set(capacity, "id", "ac110055-0000-4000-8000-000000000001");
            Set(capacity, "cost", 0); Set(capacity, "costType", CardData.CostType.Default); Set(capacity, "targetless", true); Set(capacity, "targetsRoom", true);
            Set(capacity, "traits", new List<CardTraitData>()); Set(capacity, "startingUpgrades", new List<CardUpgradeData>());
            Set(capacity, "triggers", new List<CardTriggerEffectData>()); Set(capacity, "effectTriggers", new List<CharacterTriggerData>());
            var effect = new CardEffectData("CardEffectAdjustRoomCapacity", null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", 30);
            Set(capacity, "effects", new List<CardEffectData> { effect });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(capacity);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(capacity.GetID()), Add(capacity.GetID()), Add(capacity.GetID()) }
                .Concat(Originals.Select(Add)).ToArray();
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-BASE-CHAMPIONS-PREPARED eighteen unchanged base definitions, no champion path upgrades; three zero-attack tanks and six champions per floor.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int room = index < 3 ? index : index < 6 ? index - 3 : (index - 6) / 6;
            var action = new PlayCardAction(ids[index], room, playerPosition: index < 3 ? 0 : index < 6 ? -1 : (index - 6) % 6 + 1);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-BASE-CHAMPIONS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
