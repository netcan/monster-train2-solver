using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPunishedUnitsScenario
    {
        internal const string Name = "original-punished-units";
        internal static readonly string[] Originals = {
            "ce6394f3-73b6-4fca-a9c4-b9999cb3f973", "5b16e14e-69e0-4500-985c-c3d62ef2d900",
            "f16b30a4-88f1-4da6-8f4d-87fcb9f86659", "15100a7a-6288-4b07-a726-2d6be7ec6e33",
            "a824b80b-4fac-4b20-9d2a-1790804e1232", "cb3c2f95-c556-47f7-86c3-86c773e9510e" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Original Punished units require isolated deployment.");
            Definitions = Originals.Select(id => all.FindCardData(id)!).Select(card => new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(),
                Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Rule = BattleActionProbe.Definition(card),
                Triggers = card.GetSpawnCharacterData().GetTriggers().Select(trigger => new {
                    Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetEffects().Select(RuleCatalogProbe.Effect).ToArray()
                }).ToArray() }).ToArray();
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData Control(int suffix, string type, int value, bool targetless = false, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuPunishedControl" + suffix;
                Set(data, "id", "fc231b85-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless && type != "CardEffectAdjustRoomCapacity" ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? value : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 30, true);
            var capacity = Control(2, "CardEffectAdjustRoomCapacity", 5, true);
            var damage3 = Control(3, "CardEffectDamage", 3); var heal = Control(4, "CardEffectHeal", 9999);
            var armor5 = Control(5, "CardEffectAddStatusEffect", 5, status: "armor");
            var damage5 = Control(6, "CardEffectDamage", 5); var zero = Control(7, "CardEffectDamage", 0);
            var shield = Control(8, "CardEffectAddStatusEffect", 1, status: "damage shield");
            var damage4 = Control(9, "CardEffectDamage", 4); var damage2 = Control(10, "CardEffectDamage", 2);
            var blank = Control(11, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing original-unit batch card: " + data.name);
            var originals = Originals.Select(id => all.FindCardData(id)!).ToArray();
            var created = new[] { Add(steward), Add(battery), Add(capacity), Add(originals[0]), Add(originals[1]), Add(originals[2]),
                Add(originals[3]), Add(originals[4]), Add(originals[5]), Add(damage3), Add(heal), Add(armor5), Add(damage5),
                Add(zero), Add(damage3), Add(damage3), Add(armor5), Add(damage5), Add(shield), Add(damage4), Add(zero),
                Add(heal), Add(damage2), Add(originals[3]), Add(damage3) };
            foreach (var card in created.Skip(1))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PUNISHED-UNITS-PREPARED six unchanged originals; damage history, Punish, permanent health, all-front attacks, Bloodshed and damaged-only PostCombat lifesteal.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index > 0 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 1 ? 2 : index is 8 or 11 or 12 or 13 or 14 or 23 ? 1 : 0;
            int targetCard = index is 9 or 10 ? 3 : index is 11 or 12 or 13 or 14 ? 8 :
                index is 15 or 16 or 17 or 18 or 19 or 20 ? 4 : index is 21 or 24 ? 5 : index == 22 ? 7 : -1;
            int target = targetCard < 0 ? 0 : source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units
                .Single(unit => unit.SpawnerCardId == CardIds[targetCard]).Id;
            int position = index == 0 || index == 8 || index == 23 ? 0 : index is >= 3 and <= 7 ? index - 3 : -1;
            var action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target && (position < 0 || play.PlayerPosition == position));
            if (action == null)
            {
                var result = BattleActionModel.PlayCard(source, new PlayCardAction(CardIds[index], floor, targetUnitId: target, playerPosition: position));
                throw new InvalidOperationException("Punished-unit planned input " + index + " rejected: " + result.Reason);
            }
            return action;
        }
    }
}
