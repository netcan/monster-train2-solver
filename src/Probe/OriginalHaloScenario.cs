using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHaloScenario
    {
        internal const string Name = "original-halo";
        internal const string OriginalId = "cbfec4a4-00fe-406e-b52a-e35e6456ba7d";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Zealot's Halo requires isolated deployment.");
            var original = all.FindCardData(OriginalId)!; var upgradeData = original.GetEffects().Single().GetParamCardUpgradeData();
            var upgrade = new CardUpgradeState(); upgrade.Setup(upgradeData);
            Definition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                Effects = original.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(),
                Upgrade = CardModifierProbe.Upgrade(upgrade),
                Modifiers = upgradeData.GetRoomModifierUpgrades().Select(RuleCatalogProbe.RoomModifier).ToArray() };
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 32);
            CardData Host(int suffix, int equipmentLimit)
            {
                var data = UnityEngine.Object.Instantiate(steward); data.name = "PojuHaloHost" + suffix;
                Set(data, "id", "fc231b84-0000-4000-8000-" + suffix.ToString("D12"));
                var body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Body";
                Set(body, "attackDamage", 0); Set(body, "health", 30); Set(body, "equipmentLimit", equipmentLimit);
                var summon = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters);
                summon.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode()); Set(data, "effects", new List<CardEffectData> { summon });
                definitions.Add(data); return data;
            }
            CardData Control(int suffix, string type, int value, bool targetless, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuHaloControl" + suffix;
                Set(data, "id", "fc231b84-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? value : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(10, "CardEffectGainEnergy", 20, true);
            var damage7 = Control(11, "CardEffectDamage", 7, false); var heal3 = Control(12, "CardEffectHeal", 3, false);
            var armor = Control(13, "CardEffectAddStatusEffect", 5, false, "armor"); var damage5 = Control(14, "CardEffectDamage", 5, false);
            var damage3 = Control(15, "CardEffectDamage", 3, false); var ascend = Control(16, "CardEffectBump", 1, false);
            var descend = Control(17, "CardEffectBump", -1, false); var remove = Control(18, "CardEffectRemoveEquipment", 0, false);
            var damage4 = Control(19, "CardEffectDamage", 4, false); var healAll = Control(20, "CardEffectHeal", 9999, false);
            var kill = Control(21, "CardEffectDamage", 1000, false); var damage2 = Control(22, "CardEffectDamage", 2, false);
            var blank = Control(23, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Halo control card: " + data.name);
            var created = new[] { Add(steward), Add(Host(1, 2)), Add(Host(2, 1)), Add(original), Add(battery),
                Add(damage7), Add(heal3), Add(armor), Add(damage5), Add(original), Add(damage3), Add(ascend),
                Add(damage3), Add(descend), Add(remove), Add(damage4), Add(healAll), Add(original), Add(original),
                Add(damage5), Add(kill), Add(damage4), Add(healAll), Add(original), Add(damage2) };
            foreach (var card in created.Skip(3))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-HALO-PREPARED unchanged +10 HP/missing-health equipment; damage/healing/armor, stacking, movement, removal, death and capacity replacement controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 4 ? 2 : index == 13 ? 1 : 0;
            int target = index is 0 or 1 or 2 or 4 ? 0 : source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor)
                .Units.Single(unit => unit.SpawnerCardId == CardIds[index is 12 or 17 or 21 or 22 or 23 or 24 ? 2 : 1]).Id;
            var chosen = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
            if (chosen == null)
            {
                var rejected = BattleActionModel.PlayCard(source, new PlayCardAction(CardIds[index], floor, targetUnitId: target));
                throw new InvalidOperationException("Halo planned action " + index + " rejected: " + rejected.Reason);
            }
            return chosen;
        }
    }
}
