using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpareScenario
    {
        internal const string Name = "original-spare";
        internal const string PainId = "36077a72-c45f-464f-b652-d88af459cbdd";
        internal const string TrialId = "4fe8563d-aeb8-408a-bf4a-7ed76e8e0456";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object[] Definitions { get; private set; } = Array.Empty<object>();
        internal static readonly int[] Order = { 0, 1, 2, 3, 4, 5, 6, 5, 7, 8, 14, 9, 10, 11, 16, 12, 15, 13, 17 };
        private static ManualLogSource? logger;
        private static bool recallSubmitted;
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log; recallSubmitted = false;
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("OnSpare requires isolated deployment.");
            Definitions = new[] { PainId, TrialId }.Select(id => { var data = all.FindCardData(id)!;
                return (object)new { Id = id, Key = data.name, Cost = data.GetCost(), CostType = data.GetCostType().ToString(),
                    Effects = data.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(), Traits = data.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(),
                    Triggers = data.GetCardTriggers().Select(CardUpgradeLifecycleProbe.TriggerDefinition).ToArray(),
                    AssociatedCharacterTrigger = CardTriggerType.OnSpare.GetAssociatedCharacterTrigger()?.ToString() }; }).ToArray();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 24);
            CardData Victim(int suffix, int health, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(steward); data.name = "PojuSpareVictim" + suffix;
                Set(data, "id", "fc231b81-0000-4000-8000-" + suffix.ToString("D12"));
                var unit = UnityEngine.Object.Instantiate(anchor); unit.name = data.name + "Body";
                Set(unit, "attackDamage", 0); Set(unit, "health", health);
                Set(unit, "startingStatusEffects", status == null ? Array.Empty<StatusEffectStackData>() : new[] { new StatusEffectStackData { statusId = status, count = 1 } });
                var spawn = new CardEffectData("CardEffectSpawnMonster", unit, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode()); Set(data, "effects", new List<CardEffectData> { spawn });
                definitions.Add(data); return data;
            }
            CardData Control(int suffix, string type, int value, bool targetless, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(PainId)); data.name = "PojuSpareControl" + suffix;
                Set(data, "id", "fc231b81-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
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
            var recall = Control(11, "CardEffectRecursion", 1, true); var recursion = recall.GetEffects()[0];
            recursion.Cheat_SetTargetMode(TargetMode.Discard); Set(recursion, "targetCardSelectionMode", CardEffectData.CardSelectionMode.ChooseToHand);
            Set(recursion, "targetCardType", CardType.Spell);
            var armor = Control(12, "CardEffectAddStatusEffect", 20, false, "armor");
            var shield = Control(13, "CardEffectAddStatusEffect", 1, false, "damage shield");
            var oneHealth = Control(14, "CardEffectSetHealth", 1, false); var blank = Control(15, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing OnSpare control card: " + data.name);
            CardData pain = all.FindCardData(PainId)!, trial = all.FindCardData(TrialId)!;
            var created = new[] { Add(steward), Add(Victim(1, 50)), Add(Victim(2, 1, "undying")), Add(Victim(3, 1)), Add(battery),
                Add(pain), Add(recall), Add(pain), Add(pain), Add(pain), Add(trial), Add(trial), Add(trial), Add(trial), Add(armor), Add(shield), Add(oneHealth), Add(pain) };
            var zero = new CardUpgradeState(); zero.Setup(); zero.SetAttackDamage(-5);
            created[17].ApplyPermanentUpgrade(zero, save, ignoreUpgradeAnimation: true);
            foreach (var card in created.Skip(4)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SPARE-PREPARED unchanged Pain Tithe and Trial By Fire; repeated same-card growth, nonlethal/death/Undying/armor/shield/zero damage and team-gated energy.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int step;
            for (step = 0; step < Order.Length; step++)
                if (source.Spawn.Train.Context!.Statistics!.Value(CardIds[Order[step]], "TimesPlayed", "ThisBattle") <=
                    Order.Take(step).Count(index => index == Order[step])) break;
            if (step >= Order.Length || step >= 4 && source.Spawn.Turn < 1) return null;
            int floor = step is 0 or 4 or 13 ? 0 : step is 3 or 9 ? 2 : 1;
            int target = 0;
            if (step == 13) target = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id;
            else if (step >= 5 && step != 6)
            {
                int victim = step is 8 or 16 or 17 or 18 ? 2 : step == 9 ? 3 : 1;
                target = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.SpawnerCardId == CardIds[victim]).Id;
            }
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[Order[step]] &&
                play.RoomIndex == floor && play.TargetUnitId == target && (step != 6 || play.RecursionSelections.Any(selection =>
                    selection.EffectIndex == 0 && selection.CardInstanceIds.SequenceEqual(new[] { CardIds[5] }))));
        }

        [HarmonyPatch(typeof(DeckScreen), nameof(DeckScreen.AddDeckScreenCardStateChosenDelegate))]
        private static class RecallSelectionPatch
        {
            [HarmonyPostfix]
            private static void Postfix(DeckScreen __instance)
            {
                if (!Enabled || !Prepared || recallSubmitted ||
                    (DeckScreen.Mode)AccessTools.Field(typeof(DeckScreen), "mode").GetValue(__instance) != DeckScreen.Mode.CardEffectSelection)
                    return;
                IList infos = (IList)AccessTools.Field(typeof(DeckScreen), "cardInfos").GetValue(__instance);
                for (int index = 0; index < infos.Count; index++)
                {
                    var card = AccessTools.Field(infos[index].GetType(), "cardState").GetValue(infos[index]) as CardState;
                    if (card == null || FullBattleTrace.Active!.CardId(card) != CardIds[5]) continue;
                    recallSubmitted = true;
                    AccessTools.Method(typeof(DeckScreen), "AddSelectedIndex").Invoke(__instance, new object[] { index });
                    AccessTools.Method(typeof(DeckScreen), "ConfirmSelectedCard").Invoke(__instance, null);
                    logger!.LogInfo("ORIGINAL-SPARE-RECALL-AUTOSELECT card=" + CardIds[5] + " index=" + index);
                    return;
                }
                throw new InvalidOperationException("The grown Pain Tithe is absent from native recursion candidates.");
            }
        }
    }
}
