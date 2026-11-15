#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class PyreDamageTakenScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "pyre-damage-taken";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static MutatorData? Original => original;
        private static MutatorData? original;
        private static object Describe(MutatorData data) => new { Id = data.GetID(), Key = data.name,
            Effects = data.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(),
                Conditions = effect.GetEffectConditions().Count }).ToArray() };
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager();
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Pyre damage scenario requires isolated deployment.");
            original = ((System.Collections.IEnumerable)AccessTools.Field(typeof(AllGameData), "mutatorDatas").GetValue(all))
                .Cast<MutatorData>().Single(data => data.name == "Echoes_PyreTakesMoreDamage");
            if (original.GetEffects().Count != 1 || original.GetEffects()[0].GetEffectClassName() != RelicResourceModel.ExtraPyreDamage ||
                original.GetEffects()[0].GetParamInt() != 3 || original.GetEffects()[0].GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original extra Pyre damage mutator changed.");
            Definition = Describe(original); save.AddMutator(original);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            Set(all.GetBalanceData(), "maxHandSize", 30);
            var template = all.FindCardData("3020f56f-71a5-4155-8bed-48ada7e7e0de")!;
            var ids = new List<int>();
            // Explicit controls: zero, positive, full/partial armor, full shield.
            var operations = new[] { (Damage: 0, Status: "", Count: 0), (1, "", 0), (0, "armor", 5),
                (3, "", 0), (5, "", 0), (0, "damage shield", 1), (7, "", 0) };
            for (int i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];
                var data = UnityEngine.Object.Instantiate(template); data.name = "PojuPyreDamageControl" + i;
                Set(data, "id", "fc231bb1-0000-4000-8001-" + (i + 1).ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", true); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(operation.Item2.Length == 0 ? "CardEffectDamage" : "CardEffectAddStatusEffect", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.Pyre); Set(effect, "paramInt", operation.Item1);
                if (operation.Item2.Length > 0) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = operation.Item2, count = operation.Item3 } });
                var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room);
                Set(data, "effects", new List<CardEffectData> { noop, effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
                var created = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); created.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                ids.Add(FullBattleTrace.Active!.CardId(created));
            }
            CardIds = ids.ToArray(); Prepared = true;
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("PYRE-DAMAGE-TAKEN-PREPARED unchanged +3 original mutator; seven authored defense controls; natural enemy attacks thereafter.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.Spawn.Turn < 1) return null;
            int id = CardIds.FirstOrDefault(card => source.Spawn.Train.Context!.Statistics!.Value(card, "TimesPlayed", "ThisBattle") == 0);
            return id == 0 ? null : BattleActionModel.EnumerateSupportedPlays(source, id).FirstOrDefault(play => play.RoomIndex == 0);
        }
    }
}
#endif
