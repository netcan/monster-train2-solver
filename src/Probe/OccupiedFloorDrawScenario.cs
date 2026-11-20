#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OccupiedFloorDrawScenario
    {
        internal static bool RelentlessVariant => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "relentless-draw";
        internal static bool Enabled => RelentlessVariant || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "occupied-floor-draw";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definition { get; private set; }
        private static SoulData? original;
        internal static object? CurrentDefinition => original == null ? null : Describe();
        internal static readonly List<object> Callbacks = new List<object>();
        private static object Describe() => new { Id = original!.GetID(), Key = original.name,
            Effects = original.GetEffects().Select(data => new { Type = data.GetEffectClassName(), Int = data.GetParamInt(), Int2 = data.GetParamInt2(), Conditions = data.GetEffectConditions().Count }).ToArray() };
        private static void Set(object target, string field, object value) => SpawnStatusScalingMechanismBatch.Set(target, field, value);
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Occupied draw source requires clean pre-battle acquisition.");
            original = SpawnStatusScalingMechanismBatch.Owners(save.GetAllGameData()).OfType<SoulData>().Single(data => data.name == (RelentlessVariant ? "WarborneT" + (Environment.GetEnvironmentVariable("MT2_PROBE_RELENTLESS_DRAW_TIER") ?? "2") : "WarborneT1"));
            Definition = Describe(); save.AddSelectedSoul(original);
            log.LogInfo("OCCUPIED-DRAW-ACQUIRED original " + original.name + " effects=" + string.Join(",", original.GetEffects().Select(effect => effect.GetEffectClassName())));
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var cards = managers.GetCardManager(); var all = save.GetAllGameData();
            if (original == null || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 || save.GetSelectedSouls().Single().GetRelicDataID() != original.GetID())
                throw new InvalidOperationException("Occupied draw requires original soul and isolated deployment.");
            if (RelentlessVariant)
            {
                var enemy = all.GetAllCharacterData().Single(data => data.name == "MageT1_Junker");
                var statuses = enemy.GetStartingStatusEffects().Where(status => status.statusId != "relentless").ToList();
                statuses.Add(new StatusEffectStackData { statusId = "relentless", count = 1 });
                Set(enemy, "startingStatusEffects", statuses.ToArray());
            }
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 80); Set(steward.GetSpawnCharacterData(), "health", 5000);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var damage = UnityEngine.Object.Instantiate(all.GetAllCardData().First(data => data.GetCardType() == CardType.Spell));
            damage.name = "PojuOccupiedDrawDamageControl"; Set(damage, "id", "fc119000-0000-4000-8000-000000001101");
            Set(damage, "cost", 0); Set(damage, "costType", CardData.CostType.Default); Set(damage, "targetless", true); Set(damage, "targetsRoom", true);
            Set(damage, "traits", new List<CardTraitData>()); Set(damage, "startingUpgrades", new List<CardUpgradeData>());
            Set(damage, "triggers", new List<CardTriggerEffectData>()); Set(damage, "effectTriggers", new List<CharacterTriggerData>());
            var hit = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes); hit.Cheat_SetTargetMode(TargetMode.Room); Set(hit, "paramInt", 200);
            Set(damage, "effects", new List<CardEffectData> { hit });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(damage);
            var controls = new List<CardData> { steward, steward, steward, damage };
            if (original.name == "WarborneT3")
            {
                var empty = UnityEngine.Object.Instantiate(damage); empty.name = "PojuWarborneCountControl";
                Set(empty, "id", "fc119000-0000-4000-8000-000000001102");
                var noop = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); noop.Cheat_SetTargetMode(TargetMode.Room); Set(noop, "paramInt", 1);
                Set(empty, "effects", new List<CardEffectData> { noop });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(empty);
                controls.AddRange(Enumerable.Repeat(empty, 6));
            }
            var added = controls.Select(data => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false)).ToArray();
            foreach (var card in added) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = added.Select(FullBattleTrace.Active!.CardId).ToArray(); Prepared = true;
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("OCCUPIED-DRAW-PREPARED three authored durable defenders and one authored enemy room-damage control.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, card => source.Spawn.Train.Context!.Statistics!.Value(card, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || source.Spawn.Turn < 1 && (original?.name == "WarborneT3" || index == 3)) return null;
            int room = index >= 4 ? 0 : index < 3 ? index : source.Spawn.Train.Rooms.FirstOrDefault(floor => floor.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0))?.RoomIndex ?? -1;
            return room < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source, CardIds[index]).FirstOrDefault(play => play.RoomIndex == room);
        }
        [HarmonyPatch]
        private static class CallbackWitnesses
        {
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                foreach (string method in new[] { "OnCharacterAdded", "OnCharacterKilled" })
                    yield return AccessTools.Method(typeof(RelicEffectAdditionalDrawForEnemyOccupiedFloors), method);
            }
            private static void Postfix(RelicEffectAdditionalDrawForEnemyOccupiedFloors __instance, CharacterState character, ref IEnumerator __result, System.Reflection.MethodBase __originalMethod)
            { if (Enabled && Prepared && !OccupiedFloorDrawMechanismBatch.Active) __result = Witness(__result, __instance, character, __originalMethod.Name); }
        }
        private static IEnumerator Witness(IEnumerator callback, RelicEffectAdditionalDrawForEnemyOccupiedFloors effect, CharacterState character, string kind)
        {
            float before = effect.Value; string phase = AllGameManagers.Instance!.GetCombatManager().GetCombatPhase().ToString(); string team = character.GetTeamType().ToString();
            while (callback.MoveNext()) yield return callback.Current;
            Callbacks.Add(new { Kind = kind, Phase = phase, Team = team, Before = before, After = effect.Value });
        }
    }
}
#endif
