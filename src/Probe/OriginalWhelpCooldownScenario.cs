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
    internal static class OriginalWhelpCooldownScenario
    {
        internal const string Name = "original-whelp-spawn-cooldown";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] Cards { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        private static int step;
        private static bool mark;
        private static bool control;
        private static bool testedControls;
        private static bool observing;
        private static readonly List<Record> active = new List<Record>();
        internal static object Evidence => new { Cards, Subtype = RelicUnitSpawnEffectModel.Subtype,
            FirstTurn = 0, SecondTurn = 1, Cooldowns = new[] { 8, 1, 8, 8, 8, 8 },
            AbilityCarrier = 1, AvailableGold = 11, UnavailableGold = 7 };
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public string Trigger { get; set; } = "";
            public bool Control { get; set; }
            public int ActorId { get; set; }
            public bool? Returned { get; set; }
            public bool? ExpectedReturn { get; set; }
            public int Notifications { get; set; }
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
                throw new InvalidOperationException("Expected pre-battle Whelp cooldown acquisition.");
            CardData template = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CardData ability = UnityEngine.Object.Instantiate(save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetCardType() == CardType.Spell)!);
            ability.name = "PojuWhelpCooldownAbility"; Set(ability, "id", "bc052382-0000-4000-8000-000000000001");
            Set(ability, "isUnitAbility", true); Set(ability, "cooldownAtSpawn", 1); Set(ability, "cooldownAfterActivated", 3);
            Set(ability, "cost", 0); Set(ability, "costType", CardData.CostType.Default);
            Set(ability, "targetsRoom", true); Set(ability, "targetless", false);
            Set(ability, "traits", new List<CardTraitData>()); Set(ability, "startingUpgrades", new List<CardUpgradeData>());
            Set(ability, "triggers", new List<CardTriggerEffectData>()); Set(ability, "effectTriggers", new List<CharacterTriggerData>());
            var heal = new CardEffectData("CardEffectHeal", null!, Team.Type.Monsters);
            heal.Cheat_SetTargetMode(TargetMode.Self); Set(heal, "paramInt", 1);
            Set(ability, "effects", new List<CardEffectData> { heal });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(ability);
            var definitions = new List<CardData>();
            for (int index = 0; index < 6; index++)
            {
                CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData());
                unit.name = "PojuWhelpCooldownUnit" + index;
                Set(unit, "id", "bc052381-0000-4000-8000-00000000000" + index);
                Set(unit, "health", 100); Set(unit, "attackDamage", index < 2 ? 40 : 0); Set(unit, "size", 1);
                Set(unit, "subtypeKeys", index < 2 ? new List<string>() : new List<string> { RelicUnitSpawnEffectModel.Subtype });
                Set(unit, "startingStatusEffects", index == 1 ? Array.Empty<StatusEffectStackData>() :
                    new[] { new StatusEffectStackData { statusId = "cooldown", count = 8 } });
                var triggers = new List<CharacterTriggerData>();
                if (index == 1)
                    foreach (var callback in new[] { (CharacterTriggerData.Trigger.OnUnitAbilityAvailable, 11),
                        (CharacterTriggerData.Trigger.OnUnitAbilityUnavailable, 7) })
                    {
                        CharacterTriggerData trigger = HealingScenario.HealGold(callback.Item2, false, true);
                        Set(trigger, "trigger", callback.Item1); triggers.Add(trigger);
                    }
                Set(unit, "triggers", triggers); Set(unit, "unitAbility", index == 1 ? ability : null!);
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuWhelpCooldownCard" + index;
                Set(card, "id", "bc052380-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", 1); Set(effect, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card); definitions.Add(card);
            }
            save.GetDeckState().Clear();
            foreach (CardData card in definitions) save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(data.GetAllCollectableRelicData().Single(owner => owner.GetID() == RelicUnitSpawnEffectModel.DataId));
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicUnitSpawnEffectModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-WHELP-COOLDOWN-ACQUIRED unchanged owner, first-Whelp condition and Tower -2 cooldown effect; real ability with availability gold controls; unused Room trigger retained.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!;
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected Whelp cooldown deployment.");
            var instances = cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID().StartsWith("bc052380-", StringComparison.Ordinal))
                .OrderBy(card => card.GetCardDataID(), StringComparer.Ordinal).ToArray();
            if (instances.Length != 6) throw new InvalidOperationException("Missing six Whelp cooldown carriers.");
            foreach (CardState card in instances) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            Cards = instances.Select(FullBattleTrace.Active!.CardId).ToArray();
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules();
            Set(managers.GetCombatManager(), "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared) return null;
            if (step == 6) { Controls(); return null; }
            int turn = source.Spawn.Turn;
            if (step < 4 && turn != 0 || step >= 4 && turn != 1) return null;
            int room = step == 0 || step == 2 ? 0 : step == 4 ? 2 : 1;
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(source);
            PlayCardAction? action = frontier.SupportedActions.FirstOrDefault(play => play.CardInstanceId == Cards[step] && play.RoomIndex == room);
            if (action == null) throw new InvalidOperationException("Missing Whelp cooldown play " + step + " turn " + turn + ": " +
                string.Join("; ", frontier.UnsupportedAttempts.Where(item => item.Action?.CardInstanceId == Cards[step]).Select(item => item.Reason).Distinct()));
            step++; mark = true; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) { if (!mark) return false; mark = false; return true; }
        private static void Controls()
        {
            if (testedControls) return; testedControls = true; control = true;
            try
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                RelicEffectAddEffectOnUnitSpawn effect = managers.GetSaveManager().GetCollectedRelics()
                    .Single(owner => owner.GetRelicDataID() == RelicUnitSpawnEffectModel.DataId).GetEffects().OfType<RelicEffectAddEffectOnUnitSpawn>().Single();
                var characters = new List<CharacterState>();
                managers.GetRoomManager()!.GetRoom(0).AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
                CharacterState whelp = characters.Single(actor => actor.GetSpawnerCard()?.GetCardDataID() == "bc052380-0000-4000-8000-000000000002");
                CharacterState other = characters.Single(actor => actor.GetSpawnerCard()?.GetCardDataID() == "bc052380-0000-4000-8000-000000000000");
                foreach (var item in new[] { (whelp, CharacterTriggerData.Trigger.OnSpawn), (whelp, CharacterTriggerData.Trigger.OnDeath),
                    (other, CharacterTriggerData.Trigger.OnSpawn), ((CharacterState)null!, CharacterTriggerData.Trigger.OnSpawn) })
                    effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams())
                        { characterState = item.Item1, trigger = item.Item2 }, managers.GetCoreManagers());
                foreach (CharacterState enemy in characters.Where(actor => actor.GetTeamType() == Team.Type.Heroes).Take(1))
                    effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams())
                        { characterState = enemy, trigger = CharacterTriggerData.Trigger.OnSpawn }, managers.GetCoreManagers());
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
                    row.ExpectedReturn = RelicUnitSpawnEffectModel.Test(actor == null ? null : FullBattleTrace.Active.CaptureUnit(actor), trigger);
                    row.Predicted = row.Before;
                }
                else
                {
                    TrainCombatResult result = RelicUnitSpawnEffectModel.Apply(row.Before, row.ActorId, trigger, managed: kind == "Manager");
                    row.Predicted = result.State; row.UnsupportedReason = result.UnsupportedReason;
                }
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            active.Add(row); return row;
        }
        private static void Finish(Record? row, bool? result = null)
        {
            if (row == null) return; active.Remove(row); observing = true;
            try
            {
                row.Returned = result; row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                if (result != row.ExpectedReturn || row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                    row.Difference = "Native Whelp cooldown state or result differs.";
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitSpawn), nameof(RelicEffectAddEffectOnUnitSpawn.TestCharacterTriggerEffect))]
        private static class TestPatch
        { private static void Prefix(CharacterTriggerRelicEffectParams relicEffectParams, out Record? __state) => __state = Begin("Test", relicEffectParams.trigger.ToString(), relicEffectParams.characterState);
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result); }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitSpawn), nameof(RelicEffectAddEffectOnUnitSpawn.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        { private static void Postfix(CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result) =>
                __result = Observe(__result, "Apply", relicEffectParams.trigger.ToString(), relicEffectParams.characterState); }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyCharacterTriggerRelicEffect))]
        private static class ManagerPatch
        { private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState characterState, ref IEnumerator __result) =>
                __result = Observe(__result, "Manager", trigger.ToString(), characterState); }
        private static IEnumerator Observe(IEnumerator native, string kind, string trigger, CharacterState actor)
        { Record? row = Begin(kind, trigger, actor); try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); Finish(row); } }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered), new[] { typeof(RelicState), typeof(IRelicEffect) })]
        private static class NotifyPatch
        { private static void Prefix(IRelicEffect triggeredEffect) { if (triggeredEffect is RelicEffectAddEffectOnUnitSpawn)
                    foreach (Record row in active) row.Notifications++; } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
