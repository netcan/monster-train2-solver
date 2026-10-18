using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalReactiveUnitsScenario
    {
        internal const string Name = "original-reactive-units";
        internal static readonly string[] Originals = { "fec68cb5-1d4f-4ae5-b01a-288a06cc9502", "107ecf70-99f8-47d1-b1e1-8627fff3f574",
            "49cfd4db-f1f1-4c3d-ab08-380e3fc0f6a6", "3d20e7fd-0c1f-478e-8845-4a11e27baca5", "545a9444-3773-4867-bfa1-e6032646552f",
            "4b7a0257-6f1b-4dde-8c97-b5ca5bd06fc7", "5259f045-a38e-4ff8-b6aa-c4efd942058f", "d4262a26-c3d6-4792-90f7-9ed5a1b93eb7",
            "b5b205da-0178-4859-b7d8-3a8d75cad180", "ecb7cd49-a997-43b6-9820-bf3c46db29d2" };
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
                data.name = "PojuReactiveUnitControl" + suffix; Set(data, "id", "ac110056-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", mode == TargetMode.Room); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData energy = Control(1, "CardEffectGainEnergy", 30, Team.Type.None, TargetMode.Room),
                capacity = Control(2, "CardEffectAdjustRoomCapacity", 30, Team.Type.Monsters, TargetMode.Room),
                attack = Control(3, "CardEffectAttackWithUnit", 0, Team.Type.Monsters, TargetMode.DropTargetCharacter),
                wound = Control(4, "CardEffectDamage", 1, Team.Type.Monsters, TargetMode.DropTargetCharacter),
                move = Control(5, "CardEffectBump", 1, Team.Type.Heroes, TargetMode.Room),
                incant = Control(6, "CardEffectGainEnergy", 0, Team.Type.None, TargetMode.Room);
            var attackBuff = new CardEffectData("CardEffectBuffDamage", null!, Team.Type.Monsters);
            attackBuff.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attackBuff, "paramInt", 1);
            attack.GetEffects().Insert(0, attackBuff);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(energy.GetID()), Add(capacity.GetID()), Add(capacity.GetID()) }
                .Concat(Originals.Select(Add)).Concat(new[] { Add(attack.GetID()), Add(wound.GetID()), Add(wound.GetID()), Add(wound.GetID()), Add(wound.GetID()),
                    Add(move.GetID()), Add(incant.GetID()), Add(incant.GetID()) }).ToArray();
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-REACTIVE-UNITS-PREPARED ten unchanged units; positive enemy-attributed hit, four selected damage callbacks and repeated enemy-present Incant.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int room = index < 3 ? index : index == 5 || index is >= 11 and <= 15 || index >= 22 ? 1 : 0;
            int position = index < 3 ? 0 : index == 6 ? 0 : index is >= 7 and <= 10 ? index - 5 : index is >= 11 and <= 15 ? index - 10 : -1;
            int target = index == 16 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[6]).Id :
                index is >= 17 and <= 20 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[index - 10]).Id : 0;
            var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-REACTIVE-UNITS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
