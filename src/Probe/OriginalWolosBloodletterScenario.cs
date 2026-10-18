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
    internal static class OriginalWolosBloodletterScenario
    {
        internal const string Name = "original-wolos-bloodletter";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] Cards { get; private set; } = Array.Empty<int>();
        internal static int Spell { get; private set; }
        internal static readonly List<Record> Records = new List<Record>();
        private static int step;
        private static bool mark;
        private static bool control;
        private static bool observing;
        private static Record? applying;
        internal static object Evidence => new { Cards, Spell, Roles = new[] { "front-fighter", "rear-fighter", "untouchable", "armor", "one-hp" } };
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool Control { get; set; }
            public int Room { get; set; }
            public bool? Returned { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public int[] CachedTargets { get; set; } = Array.Empty<int>();
            public int[] Notifications { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public bool Completed { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle) throw new InvalidOperationException("Expected pre-battle Bloodletter acquisition.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            save.GetDeckState().Clear();
            for (int i = 0; i < 5; i++)
            {
                CardData card = UnityEngine.Object.Instantiate(steward); CharacterData unit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData());
                card.name = "PojuBloodletterCard" + i; unit.name = "PojuBloodletterUnit" + i;
                Set(card, "id", "bc052360-0000-4000-8000-00000000000" + i); Set(unit, "id", "bc052361-0000-4000-8000-00000000000" + i);
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(TargetMode.Room); Set(spawn, "paramInt", 1); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn }); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(unit, "health", i == 4 ? 1 : i < 2 ? 80 : 8); Set(unit, "attackDamage", i < 2 ? 30 : 0); Set(unit, "size", 1);
                Set(unit, "unitAbility", null!); Set(unit, "triggers", new List<CharacterTriggerData>());
                Set(unit, "startingStatusEffects", i == 2 ? new[] { new StatusEffectStackData { statusId = "untouchable", count = 1 } } :
                    i == 3 ? new[] { new StatusEffectStackData { statusId = "armor", count = 1 } } : Array.Empty<StatusEffectStackData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            }
            save.AddRelic(data.GetAllCollectableRelicData().Single(relic => relic.GetID() == RelicEndTurnHealthModel.DataId));
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicEndTurnHealthModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-WOLOS-BLOODLETTER-ACQUIRED unchanged +12 Monster attack and one end-turn damage.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; AllGameData data = managers.GetSaveManager().GetAllGameData();
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected Bloodletter deployment setup.");
            Cards = Enumerable.Range(0, 5).Select(index => FullBattleTrace.Active!.CardId(cards.GetHand()
                .Single(card => card.GetCardDataID() == "bc052360-0000-4000-8000-00000000000" + index))).ToArray();
            CardData spell = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            spell.name = "PojuBloodletterSpell"; Set(spell, "id", "bc052360-0000-4000-8000-000000000005");
            Set(spell, "cost", 0); Set(spell, "traits", new List<CardTraitData>()); Set(spell, "effects", new List<CardEffectData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>()); Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>()); Set(spell, "targetless", true); Set(spell, "targetsRoom", true);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
            CardState added = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false)!;
            Spell = FullBattleTrace.Active!.CardId(added); Prepared = true; FullBattleTrace.Active.InvalidateCardRules();
            control = true;
            try
            {
                foreach (CardState card in cards.GetHand().ToArray())
                { managers.GetRelicManager().ApplyCardStateModifiers(card, false); managers.GetRelicManager().ApplyCardStateModifiers(card, true); }
                var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() == RelicEndTurnHealthModel.DataId)
                    .GetEffects().OfType<RelicEffectDamageOrHealUnitsEndOfTurn>().Single();
                for (int room = 0; room < 4; room++) effect.TestEffectEndOfTurn(new EndOfTurnRelicEffectParams(new RelicEffectParams
                    { relicManager = managers.GetRelicManager(), saveManager = managers.GetSaveManager(), screenManager = managers.GetScreenManager() })
                    { roomState = managers.GetRoomManager()!.GetRoom(room) }, managers.GetCoreManagers());
            }
            finally { control = false; }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step == 5) return null;
            int room = step < 2 ? 0 : step < 4 ? 1 : 2;
            PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.CardInstanceId == Cards[step] && action.RoomIndex == room);
            if (play == null) throw new InvalidOperationException("Missing Bloodletter deployment " + step);
            step++; mark = true; return play;
        }
        internal static bool IsScenarioAction(PlayCardAction action) { if (!mark) return false; mark = false; return true; }
        private static int[] Cached(RelicEffectDamageOrHealUnitsEndOfTurn effect) => ((List<CharacterState>)AccessTools.Field(effect.GetType(), "allTargets").GetValue(effect))
            .Select(FullBattleTrace.Active!.UnitId).ToArray();
        private static Record? Begin(string kind, RoomState room)
        {
            if (!Prepared || observing || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return null;
            var row = new Record { Kind = kind, Room = room.GetRoomIndex(), Control = control }; Records.Add(row); observing = true;
            try
            {
                row.Before = FullBattleTrace.Active.CaptureTrain(); row.Targets = RelicEndTurnHealthModel.Targets(row.Before.Rooms.Single(item => item.RoomIndex == row.Room));
                if (kind == "Test") row.Predicted = row.Before;
                else { TrainCombatResult predicted = RelicEndTurnHealthModel.ApplyCallback(row.Before, row.Room); row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason; }
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            return row;
        }
        private static void Finish(Record? row, RelicEffectDamageOrHealUnitsEndOfTurn effect, bool? result = null)
        {
            if (row == null) return; observing = true;
            try { row.Returned = result; row.CachedTargets = Cached(effect); row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                if (!row.Targets.SequenceEqual(row.CachedTargets) || result.HasValue && result != (row.Targets.Length > 0) ||
                    row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))) row.Difference = "Native Bloodletter targets/state/return differ."; }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
        }
        [HarmonyPatch(typeof(RelicEffectDamageOrHealUnitsEndOfTurn), nameof(RelicEffectDamageOrHealUnitsEndOfTurn.TestEffectEndOfTurn))]
        private static class TestPatch
        { private static void Prefix(EndOfTurnRelicEffectParams relicEffectParams, out Record? __state) => __state = Begin("Test", relicEffectParams.roomState);
            private static void Postfix(RelicEffectDamageOrHealUnitsEndOfTurn __instance, bool __result, Record? __state) => Finish(__state, __instance, __result); }
        [HarmonyPatch(typeof(RelicEffectDamageOrHealUnitsEndOfTurn), nameof(RelicEffectDamageOrHealUnitsEndOfTurn.ApplyEffectEndOfTurn))]
        private static class ApplyPatch
        { private static void Postfix(RelicEffectDamageOrHealUnitsEndOfTurn __instance, EndOfTurnRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, __instance, relicEffectParams.roomState); } }
        private static IEnumerator Observe(IEnumerator native, RelicEffectDamageOrHealUnitsEndOfTurn effect, RoomState room)
        {
            Record? row = Begin("Apply", room); applying = row;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); applying = null; Finish(row, effect); }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered), new[] { typeof(RelicState), typeof(IRelicEffect) })]
        private static class NotifyPatch
        { private static void Prefix(IRelicEffect triggeredEffect)
            { if (applying != null && triggeredEffect is RelicEffectDamageOrHealUnitsEndOfTurn)
                applying.Notifications = applying.Notifications.Concat(new[] { 1 }).ToArray(); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
