using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RevivalScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "revival";
        internal static bool Started, Completed;
        internal static string? Error, Label;
        internal sealed class Operation
        {
            public string Label { get; set; } = "";
            public string Kind { get; set; } = "";
            public int ActorId { get; set; }
            public int SourceCardId { get; set; }
            public int Amount { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public int QueueAfter { get; set; }
            public List<RevivalProbe.Callback> Dispatched { get; set; } = new List<RevivalProbe.Callback>();
        }
        internal static readonly List<Operation> Operations = new List<Operation>();
        private static Operation? current;
        private static CharacterData observer = null!;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CharacterData player = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!
                .GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(player, "health", 10); Set(player, "size", 1);
            Set(player, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "undying", count = 2 } });
            var triggers = Triggers();
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.Self); Set(damage, "paramInt", 9999);
            var turn = HealingScenario.HealGold(0, false, true); Set(turn, "trigger", CharacterTriggerData.Trigger.OnTurnBegin);
            var secondDamage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            secondDamage.Cheat_SetTargetMode(TargetMode.Self); Set(secondDamage, "paramInt", 9999);
            Set(turn, "effects", new List<CardEffectData> { damage, secondDamage }); triggers.Add(turn);
            Set(player, "triggers", triggers);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                    save, save.GetGeneratedBoss())).First(character => !character.IsMiniboss());
            observer = UnityEngine.Object.Instantiate(ordinary); observer.name = "PojuRevivalObserver";
            Set(observer, "id", "c2f6ed7f-18ce-4070-b65f-7dd9f5160088"); Set(observer, "health", 6);
            Set(observer, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "undying", count = 2 } });
            Set(observer, "triggers", Triggers());
            log.LogInfo("REVIVAL-PREPARED two-stack units, both-team harvest, once/silence/threshold callbacks and queued self damage; original Boss/waves unchanged.");
        }
        private static List<CharacterTriggerData> Triggers()
        {
            var list = new List<CharacterTriggerData>();
            foreach (var pair in new[] { (CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor, 5),
                (CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor, 10), (CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor, 15),
                (CharacterTriggerData.Trigger.OnReanimated, 30), (CharacterTriggerData.Trigger.OnDeath, 40) })
            {
                var trigger = HealingScenario.HealGold(pair.Item2, false, true); Set(trigger, "trigger", pair.Item1); list.Add(trigger);
            }
            var once = HealingScenario.HealGold(31, true, false); Set(once, "trigger", CharacterTriggerData.Trigger.OnReanimated); list.Add(once);
            var death = HealingScenario.HealGold(41, false, true); Set(death, "trigger", CharacterTriggerData.Trigger.OnDeath);
            Set(death, "triggerAtThreshold", 1); list.Add(death);
            return list;
        }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers), log)); }
        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            while (true)
            {
                bool next;
                try { next = native.MoveNext(); }
                catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                if (!next) break;
                yield return native.Current;
            }
            (native as IDisposable)?.Dispose(); current = null; Label = null;
            if (Error == null) Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            CombatManager combat = managers.GetCombatManager()!; FullBattleTrace trace = FullBattleTrace.Active!;
            var players = new List<CharacterState>(); var enemies = new List<CharacterState>();
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            for (int i = 0; i < 2; i++)
            {
                int index = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (index < 0) throw new InvalidOperationException("Revival fixture needs two initial Stewards.");
                CardState card = cards.GetHand()[index]; var point = rooms.GetRoom(0).GetMonsterPoint(i);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(index, point, ref error))
                    throw new InvalidOperationException("Native revival summon failed: " + error);
                while (managers.GetReplayManager().IsCardPlaying() || combat.IsRunningTriggerQueue ||
                    (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false)) yield return null;
            }
            rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                for (int i = 0; i < 2; i++) yield return managers.GetHeroManager()!.SpawnHeroInRoom(observer, 0, null!);
                rooms.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
                if (players.Count != 2 || enemies.Count != 2) throw new InvalidOperationException("Revival observers missing.");
                CardState source = players[1].GetSpawnerCard();
                yield return Apply("player-first-revival", "Damage", players[0], 9999);
                players[0].AddStatusEffect("silenced", 1, allowModification: false); yield return combat.RunTriggerQueue();
                yield return Apply("player-last-stack", "Damage", players[0], 9999);
                yield return Apply("player-direct-without-stack", "Revive", players[0], 0);
                yield return Apply("player-final-removal", "Damage", players[0], 9999);
                yield return Apply("enemy-first-revival", "Damage", enemies[0], 9999);
                yield return Apply("enemy-health-sacrifice-revival", "DebuffHealth", enemies[0], 9999);
                yield return Apply("enemy-final-removal", "Damage", enemies[0], 9999);
                players[1].AddStatusEffect("undying", 2, allowModification: false);
                players[1].SetHealth(50, 50);

                IEnumerator Apply(string label, string kind, CharacterState actor, int amount)
                {
                    var record = new Operation { Label = label, Kind = kind, ActorId = trace.UnitId(actor), Amount = amount,
                        SourceCardId = kind == "DebuffHealth" ? 0 : trace.CardId(source), Before = trace.Capture(rooms.GetRoom(0)) };
                    Operations.Add(record); current = record; Label = label;
                    if (kind == "Damage") yield return actor.ApplyDamage(amount, new CharacterState.ApplyDamageParams {
                        damageSourceCard = source, damageType = Damage.Type.Default }, managers.GetPlayerManager(), managers.GetCardStatistics());
                    else if (kind == "DebuffHealth") yield return actor.DebuffMaxHP(amount, 0);
                    else yield return actor.ReviveFromUndyingStatus(source);
                    yield return combat.RunTriggerQueue();
                    while (combat.IsRunningTriggerQueue) yield return null;
                    record.QueueAfter = ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
                    record.After = trace.Capture(rooms.GetRoom(0));
                    using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) record.AfterActor = trace.CaptureUnit(actor);
                    current = null; Label = null;
                }
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = null; Label = null; }
            Set(combat, "combatStateChanged", true);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, int triggerCount, bool fromRunningTriggerQueue)
            {
                if (current != null && fromRunningTriggerQueue && __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    current.Dispatched.Add(RevivalProbe.CaptureCallback(__instance, trigger, dyingCharacter, fireTriggersData, triggerCount));
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
