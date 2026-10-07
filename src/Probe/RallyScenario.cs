using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class RallyScenario
    {
        internal sealed class Operation
        {
            public string Label { get; set; } = "";
            public int ActorId { get; set; }
            public int Amount { get; set; }
            public bool Remove { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? AfterApi { get; set; }
            public RoomCombatState? After { get; set; }
            public int QueueAfterApi { get; set; }
            public int QueueAfter { get; set; }
        }
        internal static readonly List<Operation> Operations = new List<Operation>();
        internal static bool Started, Completed;
        internal static string? Error, Label;
        private static CharacterData observer = null!;
        private static bool holding;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HordeStatusScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager(); CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            steward.GetTraits().RemoveAll(trait => trait.GetTraitStateName() is "CardTraitScalingUpgradeUnitHealth" or "CardTraitScalingUpgradeUnitAttack");
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(unit, unit.GetTriggers().Concat(Triggers()).ToList());
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss()))
                .First(character => !character.IsMiniboss());
            observer = UnityEngine.Object.Instantiate(ordinary); observer.name = "PojuRallyObserver";
            AccessTools.Field(typeof(GameData), "id").SetValue(observer, "c2f6ed7f-18ce-4070-b65f-7dd9f5190001");
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(observer, Triggers());
            AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(observer, Array.Empty<StatusEffectStackData>());
            log.LogInfo("RALLY-PREPARED ordinary/Horde summons, both-team and cross-room observers, once/visibility gates and last-spawned targets; original Boss/waves retained.");
        }
        private static List<CharacterTriggerData> Triggers()
        {
            var gold = Gold(5, false, true); var once = Gold(11, true, true); var visible = Gold(17, false, false);
            var guarded = Gold(19, false, true);
            AccessTools.Field(typeof(CharacterTriggerData), "requiredStatusEffects").SetValue(guarded,
                new List<StatusEffectStackData> { new StatusEffectStackData { statusId = "armor", count = 999 } });
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuRallySpawnArmor", "c2f6ed7f-18ce-4070-b65f-7dd9f5190002", 0, 0, 0, 0, "armor", 1);
            var effect = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.LastSpawnedCharacter);
            AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(effect, upgrade);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt1").SetValue(effect, (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            var buff = Gold(0, false, true); AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(buff, new List<CardEffectData> { effect });
            return new List<CharacterTriggerData> { gold, once, visible, guarded, buff };
        }
        private static CharacterTriggerData Gold(int amount, bool once, bool hidden)
        {
            var trigger = HealingScenario.HealGold(amount, once, hidden);
            AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, CharacterTriggerData.Trigger.CardMonsterPlayed); return trigger;
        }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers), log)); }
        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            while (true)
            {
                bool next;
                try { next = native.MoveNext(); } catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                if (!next) break; yield return native.Current;
            }
            (native as IDisposable)?.Dispose(); holding = false; Label = null; Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            RoomManager rooms = managers.GetRoomManager()!; CombatManager combat = managers.GetCombatManager()!;
            CardManager cards = managers.GetCardManager()!; FullBattleTrace trace = FullBattleTrace.Active!;
            yield return managers.GetHeroManager()!.SpawnHeroInRoom(observer, 0, null!);
            yield return managers.GetHeroManager()!.SpawnHeroInRoom(observer, 1, null!);
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            for (int i = 0; i < 2; i++)
            {
                Label = "paid-summon-" + i;
                int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (hand < 0) throw new InvalidOperationException("Rally fixture requires two initial Stewards.");
                CardState card = cards.GetHand()[hand]; var point = rooms.GetRoom(0).GetMonsterPoint(i);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Native Rally summon failed: " + error);
                while (!Ready()) yield return null;
            }
            var players = new List<CharacterState>(); var enemies = new List<CharacterState>();
            rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters); rooms.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                players[1].AddStatusEffect("silenced", 1, allowModification: false); yield return combat.RunTriggerQueue();
                yield return Apply("grow-first-player-by-two", players[0], 2);
                yield return Apply("zero-player-growth", players[0], 0);
                yield return Apply("remove-one-player-troop", players[0], 1, true);
                yield return Apply("first-enemy-horde", enemies[0], 1);
                yield return Apply("grow-enemy-by-two", enemies[0], 2);
                yield return enemies[0].DebuffMaxHP(9999, 0);
                enemies.Clear(); rooms.GetRoom(1).AddCharactersToList(enemies, Team.Type.Heroes);
                yield return enemies[0].DebuffMaxHP(9999, 0);
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; holding = false; Label = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            IEnumerator Apply(string label, CharacterState actor, int amount, bool remove = false)
            {
                Label = label; holding = true;
                var record = new Operation { Label = label, ActorId = trace.UnitId(actor), Amount = amount, Remove = remove, Before = trace.Capture(rooms.GetRoom(0)) };
                Operations.Add(record);
                if (remove) actor.RemoveStatusEffect("horde", amount); else actor.AddStatusEffect("horde", amount, allowModification: false);
                record.AfterApi = trace.Capture(rooms.GetRoom(0)); record.QueueAfterApi = QueueCount();
                yield return combat.RunTriggerQueue(); while (combat.IsRunningTriggerQueue) yield return null;
                record.After = trace.Capture(rooms.GetRoom(0)); record.QueueAfter = QueueCount(); holding = false; Label = null;
            }
            int QueueCount() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() && !combat.IsRunningTriggerQueue &&
                QueueCount() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) && !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        [HarmonyPatch(typeof(CombatManager), "ProcessEffectsQueue")]
        private static class BackgroundLoopPatch
        {
            private static bool Prefix(ref IEnumerator __result) { if (!holding) return true; __result = Hold(); return false; }
            private static IEnumerator Hold() { yield return null; }
        }
    }
}
