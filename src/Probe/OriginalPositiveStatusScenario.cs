using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPositiveStatusScenario
    {
        internal const string Name = "original-positive-status";
        internal const string CardId = "f213cc62-90f4-4bc8-a96f-0b191a7ec7ef";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool QueriesUnchanged { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal sealed class EffectSample
        {
            public int CardId { get; set; }
            public int TargetId { get; set; }
            public string Effect { get; set; } = "";
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<EffectSample> Effects = new List<EffectSample>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CardData blank = UnityEngine.Object.Instantiate(steward); blank.name = "PojuPositiveStatusBlank";
            Set(blank, "id", "ac110032-0000-4000-8000-000000000001");
            CharacterData blankUnit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData());
            Set(blankUnit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            // Give the blank clone an independent spawn effect and character.
            var spawn = new CardEffectData("CardEffectSpawnMonster", blankUnit, Team.Type.Monsters); spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(blank, "effects", new List<CardEffectData> { spawn }); Set(blank, "cost", 0);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(blank);
            CharacterData unit = steward.GetSpawnCharacterData();
            foreach (CharacterData character in new[] { unit, blankUnit }) { Set(character, "attackDamage", 60); Set(character, "health", 500); Set(character, "size", 1); }
            Set(steward, "cost", 0);
            var statuses = new[] { new StatusEffectStackData { statusId = "buff", count = 3 }, new StatusEffectStackData { statusId = "regen", count = 2 },
                new StatusEffectStackData { statusId = "armor", count = 4 }, new StatusEffectStackData { statusId = "debuff", count = 1 } };
            Set(unit, "startingStatusEffects", statuses);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Single(character => character.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 400); Set(guard, "startingStatusEffects", statuses);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuPositiveStatusBattery"; Set(battery, "id", "ac110032-0000-4000-8000-000000000002");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 3);
            Set(battery, "effects", new List<CardEffectData> { energy });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(blank.GetID()), Add(steward.GetID()), Add(SoulSiphonScenario.CardId), Add(CardId),
                Add(battery.GetID()), Add(SoulSiphonScenario.CardId), Add(CardId), Add(SoulSiphonScenario.CardId), Add(CardId) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            RoomManager rooms = managers.GetRoomManager()!; RoomState room = rooms.GetRoom(0);
            object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(room), preview = AccessTools.Field(typeof(RoomState), "previewCorruptionStats").GetValue(room);
            Set(stats, "maxCorruption", 6); Set(preview, "maxCorruption", 6);
            int selected = rooms.GetSelectedRoom(); RoomUI ui = rooms.GetRoomUI(); int current = room.GetCurrentNonPreviewCorruption(), previewCurrent = room.GetCurrentPreviewCorruption();
            FullBattleTrace trace = FullBattleTrace.Active!; JToken before = JToken.FromObject(trace.CaptureDecision()); JObject rng = RngStates(); int frame = UnityEngine.Time.frameCount;
            try
            {
                Set(ui, "selectedRoom", 0); CardTraitCorruptRestricted trait = created[4].GetTraitStates().OfType<CardTraitCorruptRestricted>().Single();
                foreach (int value in new[] { 0, 4, 5, 6 })
                {
                    Set(stats, "currentCorruption", value); Set(preview, "currentCorruption", value);
                    BattleTurnState state = trace.CaptureDecision();
                    Queries.Add(new { Before = state, CardId = CardIds[4], Corruption = value,
                        FromHand = trait.GetIsPlayableFromHand(cards, rooms, 1, out _), FromPlay = trait.GetIsPlayableFromPlay(cards, rooms, 1, out _) });
                }
            }
            finally { Set(stats, "currentCorruption", current); Set(preview, "currentCorruption", previewCurrent); Set(ui, "selectedRoom", selected); }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) && JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Positive status legality queries changed decision/RNG/frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-POSITIVE-STATUS-PREPARED original Symphony: friendly/enemy/no-positive targets, two-stage armor, negative/floor controls and five corruption.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int target = index is 4 or 9 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[index == 4 ? 0 : 1]).Id :
                index == 7 ? source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)?.Id ?? 0 : 0;
            if (index == 7 && target == 0) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 2 ? 1 : 0) && play.TargetUnitId == target);
        }
        [HarmonyPatch(typeof(CardEffectMultiplyAllPositiveStatusEffects), nameof(CardEffectMultiplyAllPositiveStatusEffects.ApplyEffect))]
        private static class PositivePatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result) { Wrap(cardEffectParams, "MultiplyAllPositiveStatusEffects", ref __result); }
        }
        [HarmonyPatch(typeof(CardEffectMultiplyStatusEffect), nameof(CardEffectMultiplyStatusEffect.ApplyEffect))]
        private static class ArmorPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result) { Wrap(cardEffectParams, "MultiplyStatus", ref __result); }
        }
        private static void Wrap(CardEffectParams parameters, string effect, ref IEnumerator result)
        {
            if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && parameters.playedCard?.GetCardDataID() == CardId)
                result = Observe(result, parameters, effect);
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectParams parameters, string effect)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new EffectSample { CardId = trace.CardId(parameters.playedCard), TargetId = parameters.targets.Count == 0 ? 0 : trace.CaptureUnit(parameters.targets[0]).Id,
                Effect = effect, Before = trace.CaptureCanonicalTrain() }; Effects.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureCanonicalTrain(); }
        }
        private static JObject RngStates()
        {
            var result = new JObject(); foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id))); return result;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
