using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRoomStatusDamageScenario
    {
        internal const string Name = "original-room-status-damage";
        internal const string RamId = "fc525983-11ed-4612-997e-b53b07b52d48";
        internal const string LashId = "e5651931-70a9-422e-8780-263ea77e4c28";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CharacterData enemy in ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Where(character => character.name == "MageT1_Junker" || character.name == "ShieldGuardT1_BasicNonAttack"))
            {
                Set(enemy, "health", 400); Set(enemy, "ascendsTrainAutomatically", false);
                Set(enemy, "startingStatusEffects", enemy.GetStartingStatusEffects().Where(status => status.statusId != "spikes")
                    .Concat(new[] { new StatusEffectStackData { statusId = "spikes", count = 7 } }).ToArray());
            }
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            Set(save.GetBalanceData(), "maxHandSize", 14);
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuRoomStatusBattery"; Set(battery, "id", "ac110016-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room); Set(gain, "paramInt", 20);
            Set(battery, "effects", new List<CardEffectData> { gain });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("Room-status scenario could not add " + id);
            const string branding = "f381b99b-518d-4818-b182-44d8f04110be", potion = "ec0980cf-9f88-4d38-9704-b89042f2f625";
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(branding), Add(potion),
                Add(battery.GetID()), Add(RamId), Add(LashId), Add(branding), Add(branding), Add(potion), Add(potion), Add(RamId), Add(LashId) };
            foreach (CardState card in created.Skip(3))
            {
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
            }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ROOM-STATUS-DAMAGE-PREPARED unchanged Battering Ram/Bramble Lash, live two-unit armor/spikes sums, enemy/outside-floor and zero controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    if (index >= 5 && source.Spawn.Turn < 1) return null;
                    int owner = index is 3 or 4 ? 2 : index is 8 or 10 ? 0 : index is 9 or 11 ? 1 : -1;
                    int target = owner >= 0 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]).Id : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                        play.RoomIndex == (index is 2 or 3 or 4 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
