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
    internal static class OriginalDecayDeathScenario
    {
        internal const string Name = "original-decay-death";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool UsesHeroTargets { get; private set; }
        internal static bool HeroTargetControlsCompleted { get; private set; }
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static bool observing, controls;
        private static ApplicationRecord? active;
        internal sealed class TestRecord
        {
            public CombatUnit? Actor { get; set; }
            public string Trigger { get; set; } = "";
            public int DamageType { get; set; }
            public bool Expected { get; set; }
            public bool Returned { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public CombatUnit Actor { get; set; } = null!;
            public CombatUnit? ActorAfter { get; set; }
            public int RoomIndex { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public List<string> Notifications { get; set; } = new List<string>();
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        private static bool Active => Prepared && (!UsesHeroTargets || !HeroTargetControlsCompleted) &&
            !observing && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Expected fresh pre-battle Decay-death acquisition.");
            RelicData original = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicDecayDeathModel.DataId);
            UsesHeroTargets = original.GetEffects().Single().GetParamCardEffectData().Single().GetParamInt() == 12;
            if (UsesHeroTargets)
            {
                AcquireOriginal();
                return;
            }
            CardData template = save.GetDeckState().Select(item => data.FindCardData(item.GetCardDataID()))
                .First(item => item.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            foreach (CharacterData definition in data.GetAllCharacterData()) Set(definition, "attackDamage", 0);
            CharacterData mage = data.GetAllCharacterData().Single(item => item.GetAssetKey() == "MageT1_Junker");
            Set(mage, "health", 3); Set(mage, "triggers", new List<CharacterTriggerData>());
            Set(mage, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "decay", count = 5 } });
            save.GetDeckState().Clear();
            for (int index = 0; index < RelicDecayDeathModel.CardIds.Length; index++)
            {
                string id = RelicDecayDeathModel.CardIds[index];
                CardData card = UnityEngine.Object.Instantiate(template);
                CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData());
                card.name = unit.name = "PojuDecayDeath" + index; Set(card, "id", id); Set(unit, "id", id);
                Set(unit, "health", index == 0 || index == 4 ? 3 : 100); Set(unit, "attackDamage", index == 5 ? 100 : 0); Set(unit, "size", 1);
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null!);
                Set(unit, "startingStatusEffects", index == 0 ? new[] {
                    new StatusEffectStackData { statusId = "decay", count = 5 }, new StatusEffectStackData { statusId = "regen", count = 2 },
                    new StatusEffectStackData { statusId = "spell weakness", count = 4 } } : index == 3 ? new[] {
                    new StatusEffectStackData { statusId = "untouchable", count = 1 } } : index == 4 ? new[] {
                    new StatusEffectStackData { statusId = "decay", count = 5 } } : Array.Empty<StatusEffectStackData>());
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                Set(spawn, "targetMode", TargetMode.Room); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn }); Set(card, "cost", 0);
                Set(card, "traits", new List<CardTraitData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            }
            AcquireOriginal();

            void AcquireOriginal()
            {
                save.AddRelic(original);
                string? error = RelicModel.Validate(RelicProbe.Capture(managers));
                if (error != null) throw new InvalidOperationException(error);
                Acquired = true;
                log.LogInfo("ORIGINAL-DECAY-DEATH-ACQUIRED target=" + (UsesHeroTargets ? "Heroes" : "SourceTeam"));
            }
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || !Acquired || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected Decay-death deployment.");
            CardManager cards = managers.GetCardManager()!;
            if (!UsesHeroTargets)
                foreach (CardState card in cards.GetAllCards(new List<CardState>())) if (!cards.GetHand().Contains(card)) cards.DrawSpecificCard(card);
            Prepared = true; FullBattleTrace.Active!.InvalidateCardRules(); Set(managers.GetCombatManager(), "combatStateChanged", true);
        }

        internal static IEnumerator RunHeroTargetControls(AllGameManagers managers)
        {
            if (!UsesHeroTargets || !Prepared || HeroTargetControlsCompleted)
                throw new InvalidOperationException("Expected prepared current Decay-death controls.");
            var rooms = managers.GetRoomManager();
            var heroes = managers.GetHeroManager();
            var monsters = managers.GetMonsterManager();
            var combat = managers.GetCombatManager();
            var data = managers.GetSaveManager().GetAllGameData();
            const string stewardId = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
            CharacterData steward = data.FindCardData(stewardId).GetSpawnCharacterData();
            CharacterData hero = data.GetAllCharacterData().Single(item => item.GetAssetKey() == "HeavyT1_Basic");
            if (Enumerable.Range(0, 3).Any(floor => InRoom(floor, Team.Type.Heroes | Team.Type.Monsters).Count != 0))
                throw new InvalidOperationException("Expected empty train floors before original Decay-death controls.");

            yield return heroes.SpawnHeroInRoom(hero, 0, null!);
            yield return heroes.SpawnHeroInRoom(hero, 0, null!);
            yield return heroes.SpawnHeroInRoom(hero, 0, null!);
            var initialHeroes = InRoom(0, Team.Type.Heroes);
            if (initialHeroes.Count != 3) throw new InvalidOperationException("Expected three original heroes on the source floor.");
            initialHeroes[2].AddStatusEffect("untouchable", 1);
            initialHeroes[1].AddStatusEffect("decay", 2);
            yield return heroes.SpawnHeroInRoom(hero, 1, null!);

            CharacterState? playerSource = null;
            yield return monsters.CreateMonsterState(steward, null!, 0, unit => playerSource = unit,
                SpawnMode.SelectedSlot, rooms.GetRoom(0).GetMonsterPoint(0), isCardless: true);
            yield return monsters.CreateMonsterState(steward, null!, 0, _ => { },
                SpawnMode.SelectedSlot, rooms.GetRoom(0).GetMonsterPoint(2), isCardless: true);
            yield return DamageSource(playerSource!);

            yield return heroes.SpawnHeroInRoom(hero, 0, null!);
            CharacterState heroSource = InRoom(0, Team.Type.Heroes).Single(unit => !initialHeroes.Contains(unit));
            yield return DamageSource(heroSource);

            CharacterState? alone = null;
            yield return monsters.CreateMonsterState(steward, null!, 2, unit => alone = unit,
                SpawnMode.SelectedSlot, rooms.GetRoom(2).GetMonsterPoint(0), isCardless: true);
            yield return DamageSource(alone!);

            // Remove surviving controls before taking the first recorded decision.
            var remaining = Enumerable.Range(0, 3).SelectMany(floor => InRoom(floor, Team.Type.Heroes | Team.Type.Monsters)).ToList();
            foreach (CharacterState unit in remaining) unit.SetHealth(0, unit.GetMaxHP());
            yield return combat.RemoveDeadCharacters(remaining, ignoreTriggers: true);
            if (Enumerable.Range(0, 3).Any(floor => InRoom(floor, Team.Type.Heroes | Team.Type.Monsters).Count != 0))
                throw new InvalidOperationException("Original Decay-death controls did not clear the train floors.");
            HeroTargetControlsCompleted = true;

            List<CharacterState> InRoom(int floor, Team.Type team)
            {
                var actors = new List<CharacterState>();
                rooms.GetRoom(floor).AddCharactersToList(actors, team);
                return actors;
            }

            IEnumerator DamageSource(CharacterState source)
            {
                source.SetHealth(3, source.GetMaxHP());
                source.AddStatusEffect("decay", 5);
                source.AddStatusEffect("regen", 2);
                source.AddStatusEffect("spell weakness", 4);
                var damage = new CombatManager.ApplyDamageToTargetParameters { damageType = Damage.Type.Decay, finalEffectInSequence = true };
                yield return combat.ApplyDamageToTarget(1, source, damage);
                yield return combat.ApplyDamageToTarget(5, source, damage);
                if (!source.IsDead) throw new InvalidOperationException("Original Decay-death control source survived native damage.");
                yield return combat.RemoveDeadCharacters();
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitDeath), nameof(RelicEffectAddEffectOnUnitDeath.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(CharacterTriggerRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active) return;
                observing = true; var row = new TestRecord(); __state = row; Tests.Add(row);
                try
                {
                    row.Actor = relicEffectParams.characterState == null ? null : FullBattleTrace.Active!.CaptureUnit(relicEffectParams.characterState);
                    row.Trigger = relicEffectParams.trigger.ToString(); row.DamageType = relicEffectParams.paramInt2;
                    row.Before = FullBattleTrace.Active!.CaptureTrain(); row.Expected = RelicDecayDeathModel.Test(row.Actor, row.Trigger, row.DamageType);
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return; observing = true;
                try
                {
                    __state.Returned = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__result != __state.Expected || !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Native Decay-death Test result or complete state differs.";
                }
                catch (Exception error) { __state.Error = error.ToString(); }
                finally { observing = false; }
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitDeath), nameof(RelicEffectAddEffectOnUnitDeath.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddEffectOnUnitDeath __instance, CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, __instance, relicEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, RelicEffectAddEffectOnUnitDeath effect, CharacterTriggerRelicEffectParams input)
        {
            var row = new ApplicationRecord(); Applications.Add(row); observing = true;
            try
            {
                row.Actor = FullBattleTrace.Active!.CaptureUnit(input.characterState); row.RoomIndex = input.characterState.GetCurrentRoomIndex();
                row.Before = FullBattleTrace.Active.CaptureTrain();
                RoomCombatState room = row.Before.Rooms.Single(item => item.RoomIndex == row.RoomIndex);
                RoomCombatResult predicted = RelicDecayDeathModel.Apply(room, row.Actor); row.UnsupportedReason = predicted.UnsupportedReason;
                if (predicted.Supported)
                    row.Predicted = CardSpellModel.WithContext(new TrainCombatState(row.Before.Rooms.Select(item => item.RoomIndex == row.RoomIndex ? predicted.State! : item).ToArray(),
                        row.Before.Movement, row.Before.EnemySlotsPerRoom, predicted.State!.Context), predicted.State.Context!);
                active = row;
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); observing = true;
                try
                {
                    row.ActorAfter = FullBattleTrace.Active!.CaptureUnit(input.characterState); row.Actual = FullBattleTrace.Active.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native Decay-death transfer full state differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
                finally { observing = false; active = null; }
            }
            if (!controls)
            {
                controls = true;
                foreach (var control in new[] {
                    new CharacterTriggerRelicEffectParams(new RelicEffectParams()) { characterState = input.characterState, trigger = CharacterTriggerData.Trigger.OnDeath, paramInt2 = 8 },
                    new CharacterTriggerRelicEffectParams(new RelicEffectParams()) { characterState = input.characterState, trigger = CharacterTriggerData.Trigger.OnHit, paramInt2 = 0 },
                    new CharacterTriggerRelicEffectParams(new RelicEffectParams()) { characterState = null!, trigger = CharacterTriggerData.Trigger.OnHit, paramInt2 = 8 } })
                    if (effect.TestCharacterTriggerEffect(control, AllGameManagers.Instance!.GetCoreManagers())) throw new InvalidOperationException("Decay-death negative control passed.");
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class TriggerPatch
        { private static void Prefix(RelicState triggeredRelic) { if (active != null && triggeredRelic.GetRelicDataID() == RelicDecayDeathModel.DataId) active.Notifications.Add("Triggered"); } }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
