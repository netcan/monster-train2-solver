using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusUnitsScenario
    {
        internal const string Name = "original-status-units";
        internal static readonly string[] Originals = { "36aa1f24-a48e-41bf-b30a-7c98b250261a", "5de24977-6800-4015-a20e-9949e79e057c",
            "e66178fd-b8e7-401c-b570-7832840ea902", "b53dd72c-141c-49f2-abae-6505f803773a", "c3c595d2-898d-445c-8f3c-d06d6c120bbd",
            "3d32e031-9796-4eaf-84c3-a30b48db22d4", "3d81c731-8315-40ae-b731-4f2ed80df572", "63a48bad-07da-4687-bfef-2f7c6653af5e",
            "a5237619-8de3-46d2-8c52-e0d22abbc008", "c493d7e0-6731-4d8a-9ae1-56e44febb891", "4dd2be98-c73c-4f96-865f-6c24cc42e91d" };
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
            CardData Control(int suffix, string effectName, int value, Team.Type team, TargetMode mode)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuStatusUnitControl" + suffix; Set(data, "id", "ac110054-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", mode == TargetMode.Room); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData energy = Control(1, "CardEffectGainEnergy", 30, Team.Type.None, TargetMode.Room),
                capacity = Control(2, "CardEffectAdjustRoomCapacity", 30, Team.Type.Monsters, TargetMode.Room),
                hit = Control(3, "CardEffectDamage", 54, Team.Type.Monsters, TargetMode.DropTargetCharacter);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(energy.GetID()),
                Add(capacity.GetID()), Add(capacity.GetID()), Add(capacity.GetID()) }.Concat(Originals.Select(Add))
                .Concat(new[] { Add(hit.GetID()), Add(hit.GetID()), Add(hit.GetID()) }).ToArray();
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STATUS-UNITS-PREPARED eleven unchanged units, three zero-attack tanks, expanded capacity and positive Steelguard/Undying damage controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int room = index < 3 ? index : index is >= 4 and <= 6 ? index - 4 : index is >= 11 and <= 13 ? 1 : index is >= 14 and <= 17 ? 2 : index == 20 ? 1 : 0;
            int position = index < 3 ? 0 : index is >= 7 and <= 10 ? index - 6 : index is >= 11 and <= 13 ? index - 10 : index is >= 14 and <= 17 ? index - 13 : -1;
            int target = index >= 18 ? source.Spawn.Train.Rooms[room].Units.Single(unit => unit.SpawnerCardId == ids[index == 20 ? 13 : 10]).Id : 0;
            var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-STATUS-UNITS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
