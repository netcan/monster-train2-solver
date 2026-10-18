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
    internal static class OriginalSpawnRetreatScenario
    {
        internal const string Name = "original-spawn-retreat";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] Cards { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static int step;
        private static bool mark;
        private static bool observing;
        private static bool controlsDone;
        private static bool earlyControlDone;
        private static bool control;
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public string Trigger { get; set; } = "";
            public int ActorId { get; set; }
            public bool Control { get; set; }
            public bool? Returned { get; set; }
            public bool? ExpectedReturn { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected pre-battle retreat acquisition.");
            CardData card = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .First(item => item?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = card.GetSpawnCharacterData();
            Set(unit, "health", 100); Set(unit, "attackDamage", 40); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "unitAbility", null!); Set(card, "cost", 0);
            CharacterData excludedCarrier = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data))
                .Single(item => item.GetAssetKey() == "MageT1_Junker");
            Set(excludedCarrier, "subtypeKeys", excludedCarrier.GetSubtypes().Select(subtype => subtype.Key)
                .Concat(new[] { RelicSpawnRetreatModel.ExcludedSubtypes[0] }).Distinct().ToList());
            save.GetDeckState().Clear();
            for (int index = 0; index < 2; index++) save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(data.GetAllCollectableRelicData().Single(owner => owner.GetID() == RelicSpawnRetreatModel.DataId));
            CombatRelicState capturedOwner = RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicSpawnRetreatModel.DataId);
            string? error = RelicModel.Validate(new[] { capturedOwner });
            if (error != null) throw new InvalidOperationException(error + " " + JToken.FromObject(new {
                capturedOwner.AssetKey, capturedOwner.IsCovenant, capturedOwner.DisallowedInPlacementPhase,
                capturedOwner.AfterSpawnChanceRules }));
            Acquired = true; log.LogInfo("ORIGINAL-SPAWN-RETREAT-ACQUIRED unchanged 100% owner; BackInRoom enemy to FrontSlot, four room flags, natural enemy waves.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected retreat deployment.");
            CardManager cards = managers.GetCardManager()!;
            CardState[] units = cards.GetAllCards(new List<CardState>()).OrderBy(FullBattleTrace.Active!.CardId).ToArray();
            if (units.Length != 2) throw new InvalidOperationException("Missing retreat control cards.");
            foreach (CardState card in units) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            Cards = units.Select(FullBattleTrace.Active!.CardId).ToArray(); Prepared = true;
            FullBattleTrace.Active!.InvalidateCardRules(); Set(managers.GetCombatManager(), "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step == 2) { Controls(); return null; }
            if (source.Spawn.Turn != 0) return null;
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(source);
            PlayCardAction? action = frontier.SupportedActions.FirstOrDefault(item => item.CardInstanceId == Cards[step] && item.RoomIndex == step);
            if (action == null) throw new InvalidOperationException("Missing retreat carrier play: " +
                string.Join("; ", frontier.UnsupportedAttempts.Select(item => item.Reason).Distinct()));
            step++; mark = true; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) { bool result = mark; mark = false; return result; }
        private static void Controls()
        {
            if (controlsDone) return;
            var characters = new List<CharacterState>();
            AllGameManagers managers = AllGameManagers.Instance!;
            managers.GetRoomManager()!.GetRoom(0).AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
            CharacterState? enemy = characters.FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Heroes &&
                !FullBattleTrace.Active!.CaptureUnit(actor).Subtypes.Any(RelicSpawnRetreatModel.ExcludedSubtypes.Contains));
            var upperRoom = new List<CharacterState>();
            managers.GetRoomManager()!.GetRoom(1).AddCharactersToList(upperRoom, Team.Type.Monsters);
            CharacterState? player = upperRoom.FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Monsters);
            if (enemy == null || player == null) return;
            controlsDone = true; control = true;
            try
            {
                RelicEffectChanceAddEffectAfterSpawn effect = managers.GetSaveManager().GetCollectedRelics()
                    .Single(owner => owner.GetRelicDataID() == RelicSpawnRetreatModel.DataId).GetEffects().OfType<RelicEffectChanceAddEffectAfterSpawn>().Single();
                foreach (var item in new[] { (enemy, CharacterTriggerData.Trigger.AfterSpawnEnchant),
                    (enemy, CharacterTriggerData.Trigger.OnSpawn), (player, CharacterTriggerData.Trigger.AfterSpawnEnchant) })
                    effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams())
                        { characterState = item.Item1, trigger = item.Item2 }, managers.GetCoreManagers());
            }
            finally { control = false; }
        }
        private static Record? Begin(string kind, string trigger, CharacterState? actor)
        {
            if (!Prepared || observing || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return null;
            observing = true; var row = new Record { Kind = kind, Trigger = trigger, Control = control }; Records.Add(row);
            try
            {
                row.ActorId = actor == null ? 0 : FullBattleTrace.Active!.UnitId(actor);
                row.Before = FullBattleTrace.Active!.CaptureTrain();
                if (kind == "Test")
                {
                    CombatUnit? unit = actor == null ? null : FullBattleTrace.Active.CaptureUnit(actor);
                    RoomCombatState? room = row.Before.Rooms.FirstOrDefault(item => item.Units.Any(value => value.Id == row.ActorId));
                    CombatRelicState owner = row.Before.Context!.Relics!.Single(item => item.DataId == RelicSpawnRetreatModel.DataId);
                    row.ExpectedReturn = RelicSpawnRetreatModel.Test(room, unit, trigger, owner); row.Predicted = row.Before;
                }
                else if (kind == "Reset") row.Predicted = CardSpellModel.WithContext(row.Before, RelicSpawnRetreatModel.Reset(row.Before.Context!));
                else
                {
                    TrainCombatResult result = RelicSpawnRetreatModel.Apply(row.Before, row.ActorId, trigger, false);
                    row.Predicted = result.State; row.UnsupportedReason = result.UnsupportedReason;
                }
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            return row;
        }
        private static void Finish(Record? row, bool? returned = null)
        {
            if (row == null) return; observing = true;
            try
            {
                row.Returned = returned; row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                if (returned != row.ExpectedReturn || row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                    row.Difference = "Native retreat state or result differs.";
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
        }
        private static bool Observed(RelicEffectChanceAddEffectAfterSpawn effect) =>
            ((RelicState)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicState").GetValue(effect)).GetRelicDataID() == RelicSpawnRetreatModel.DataId;
        private static void EarlyControl(RelicEffectChanceAddEffectAfterSpawn effect, CharacterTriggerRelicEffectParams parameters)
        {
            if (!Prepared || control || earlyControlDone || parameters.trigger != CharacterTriggerData.Trigger.AfterSpawnEnchant ||
                parameters.characterState.GetTeamType() != Team.Type.Heroes) return;
            CombatUnit actor = FullBattleTrace.Active!.CaptureUnit(parameters.characterState);
            int room = parameters.characterState.GetCurrentRoomIndex();
            if (actor.Subtypes.Any(RelicSpawnRetreatModel.ExcludedSubtypes.Contains) ||
                ((bool[])AccessTools.Field(effect.GetType(), "_triggeredInRoom").GetValue(effect))[room]) return;
            earlyControlDone = true; control = true;
            try
            {
                effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams())
                    { characterState = parameters.characterState, trigger = CharacterTriggerData.Trigger.OnSpawn }, AllGameManagers.Instance!.GetCoreManagers());
                var heroes = new List<CharacterState>();
                parameters.characterState.GetCurrentRoom().AddCharactersToList(heroes, Team.Type.Heroes);
                CharacterState excluded = heroes.First(hero => FullBattleTrace.Active.CaptureUnit(hero).Subtypes
                    .Any(RelicSpawnRetreatModel.ExcludedSubtypes.Contains));
                effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams())
                    { characterState = excluded, trigger = CharacterTriggerData.Trigger.AfterSpawnEnchant }, AllGameManagers.Instance!.GetCoreManagers());
            }
            finally { control = false; }
        }
        [HarmonyPatch(typeof(RelicEffectChanceAddEffectAfterSpawn), nameof(RelicEffectChanceAddEffectAfterSpawn.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectChanceAddEffectAfterSpawn __instance, CharacterTriggerRelicEffectParams relicEffectParams, out Record? __state)
            {
                __state = null; if (!Observed(__instance)) return;
                EarlyControl(__instance, relicEffectParams);
                __state = Begin("Test", relicEffectParams.trigger.ToString(), relicEffectParams.characterState);
            }
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result);
        }
        [HarmonyPatch(typeof(RelicEffectChanceAddEffectAfterSpawn), nameof(RelicEffectChanceAddEffectAfterSpawn.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectChanceAddEffectAfterSpawn __instance, CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Observed(__instance)) __result = Observe(__result, "Apply", relicEffectParams.trigger.ToString(), relicEffectParams.characterState); }
        }
        [HarmonyPatch(typeof(RelicEffectChanceAddEffectAfterSpawn), nameof(RelicEffectChanceAddEffectAfterSpawn.ApplyEffectEndOfCombat))]
        private static class ResetPatch
        {
            private static void Postfix(RelicEffectChanceAddEffectAfterSpawn __instance, ref IEnumerator __result)
            { if (Observed(__instance)) __result = Observe(__result, "Reset", "EndOfCombat", null); }
        }
        private static IEnumerator Observe(IEnumerator native, string kind, string trigger, CharacterState? actor)
        { Record? row = Begin(kind, trigger, actor); try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); Finish(row); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
