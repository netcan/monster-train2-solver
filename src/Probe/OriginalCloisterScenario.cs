using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCloisterScenario
    {
        internal const string Name = "original-cloister";
        internal const string OriginalId = "8e11f688-eb5f-48b5-98f6-cf40e60c842d";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Cloister requires isolated deployment.");
            var original = all.FindCardData(OriginalId)!;
            Definition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                Effects = original.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray() };
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 24);
            var ally = UnityEngine.Object.Instantiate(steward); ally.name = "PojuCloisterAlly";
            Set(ally, "id", "fc231b82-0000-4000-8000-000000000001");
            var body = UnityEngine.Object.Instantiate(anchor); body.name = "PojuCloisterAllyBody"; Set(body, "attackDamage", 0); Set(body, "health", 12);
            var summon = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters);
            summon.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode()); Set(ally, "effects", new List<CardEffectData> { summon }); definitions.Add(ally);
            CardData Control(int suffix, string type, int value, bool targetless, bool targetFriendly = false)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuCloisterControl" + suffix;
                Set(data, "id", "fc231b82-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : targetFriendly ? Team.Type.Monsters : Team.Type.Heroes);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(10, "CardEffectGainEnergy", 20, true); var ascend = Control(11, "CardEffectBump", 1, false);
            // Chained reflects damage taken by a friendly onto chained enemies.
            // Hitting the allied body kills the mage and consumes guard armor.
            var descend = Control(12, "CardEffectBump", -1, false); var damage = Control(13, "CardEffectDamage", 1, false, targetFriendly: true);
            var blank = Control(14, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Cloister control card: " + data.name);
            var created = new[] { Add(steward), Add(original), Add(original), Add(ally), Add(ally), Add(battery), Add(original),
                Add(ascend), Add(descend), Add(damage), Add(all.FindCardData(RoomCapacityAttachmentScenario.CardDataId)!),
                Add(ascend), Add(descend), Add(original), Add(ascend), Add(descend) };
            foreach (var card in created.Where((_, index) => index is not (0 or 3 or 4)))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CLOISTER-PREPARED unchanged room attachment; enemy spawn/ascend/descend, friendly entry, replacement/removal/reset and immediate chained damage controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 5 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 5 ? 2 : index is 2 or 4 or 8 or 12 or 15 ? 1 : 0;
            int target = index is 7 or 8 or 9 or 11 or 12 or 14 or 15 ? source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor)
                .Units.Single(unit => unit.AssetKey == (index == 9 ? "PojuCloisterAllyBody" : "ShieldGuardT1_BasicNonAttack")).Id : 0;
            var chosen = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
            if (chosen == null)
            {
                var attempted = new PlayCardAction(CardIds[index], floor, targetUnitId: target);
                var rejected = BattleActionModel.PlayCard(source, attempted);
                throw new InvalidOperationException("Cloister planned action " + index + " rejected: " + rejected.Reason);
            }
            return chosen;
        }
    }
}
