using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class HarvestScenario
    {
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
            public List<Dispatch> Dispatched { get; set; } = new List<Dispatch>();
        }
        internal sealed class Dispatch
        {
            public int ActorId { get; set; }
            public string Kind { get; set; } = "";
            public int DyingId { get; set; }
            public int TriggerCount { get; set; }
        }
        internal static readonly List<Operation> Operations = new List<Operation>();
        internal static bool Started, Completed;
        internal static string? Error, Label;
        private static Operation? current;
        private static CharacterData enemy = null!;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HordeStatusScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CharacterData player = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!
                .GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(player, "triggers", player.GetTriggers().Concat(Triggers()).ToList());
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                    save, save.GetGeneratedBoss())).First(character => !character.IsMiniboss());
            enemy = UnityEngine.Object.Instantiate(ordinary); enemy.name = "PojuHarvestObserver";
            AccessTools.Field(typeof(GameData), "id").SetValue(enemy, "c2f6ed7f-18ce-4070-b65f-7dd9f5160080");
            Set(enemy, "triggers", Triggers());
            Set(enemy, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "horde", count = 2 } });
            log.LogInfo("HARVEST-PREPARED both-team observers, Horde, once/silence/required-dying gates and death children; original Boss/waves unchanged.");
        }
        private static List<CharacterTriggerData> Triggers()
        {
            var list = new List<CharacterTriggerData>();
            foreach (var pair in new[] { (CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor, 5),
                (CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor, 10), (CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor, 15) })
            {
                list.Add(Gold(pair.Item1, pair.Item2));
                list.Add(Gold(pair.Item1, 20, once: true));
                var armor = Gold(pair.Item1, 25);
                Set(armor, "requiredStatusEffectsForDyingCharacter", new List<StatusEffectStackData> { new StatusEffectStackData { statusId = "ARMOR", count = 999 } });
                list.Add(armor);
                var horde = Gold(pair.Item1, 30, ignore: false);
                Set(horde, "requiredStatusEffectsForDyingCharacter", new List<StatusEffectStackData> { new StatusEffectStackData { statusId = "horde", count = 999 } });
                list.Add(horde);
            }
            var death = Gold(CharacterTriggerData.Trigger.OnDeath, 40);
            var status = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.None);
            status.Cheat_SetTargetMode(TargetMode.Self);
            Set(status, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 1 } });
            Set(death, "effects", new List<CardEffectData> { status, death.GetEffects()[0] }); list.Add(death);
            list.Add(Gold(CharacterTriggerData.Trigger.OnArmorAdded, 35));
            return list;
        }
        private static CharacterTriggerData Gold(CharacterTriggerData.Trigger kind, int amount, bool once = false, bool ignore = true)
        {
            var trigger = HealingScenario.HealGold(amount, once, ignore); Set(trigger, "trigger", kind); return trigger;
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
            (native as IDisposable)?.Dispose(); current = null; Label = null; Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            CombatManager combat = managers.GetCombatManager()!; FullBattleTrace trace = FullBattleTrace.Active!;
            var players = new List<CharacterState>(); var heroes = new List<CharacterState>();
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            for (int i = 0; i < 2; i++)
            {
                int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (hand < 0) throw new InvalidOperationException("Harvest fixture needs two initial Stewards.");
                CardState card = cards.GetHand()[hand]; var point = rooms.GetRoom(0).GetMonsterPoint(i);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Native Harvest summon failed: " + error);
                while (!Ready()) yield return null;
                players.Clear(); rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            }
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                for (int i = 0; i < 3; i++) yield return managers.GetHeroManager()!.SpawnHeroInRoom(enemy, 0, null!);
                rooms.GetRoom(0).AddCharactersToList(heroes, Team.Type.Heroes);
                if (heroes.Count != 3) throw new InvalidOperationException("Native Harvest enemy observers missing.");
                CardState source = players[1].GetSpawnerCard();
                // Silence only blocks the authored visible Horde-condition rewards.
                players[1].AddStatusEffect("silenced", 1, allowModification: false); yield return combat.RunTriggerQueue();
                yield return Apply("enemy-horde-health-lethal", "DebuffHealth", heroes[0], 9999);
                yield return Apply("player-horde-health-lethal", "DebuffHealth", players[0], 9999);
                yield return Apply("enemy-horde-damage-lethal", "Damage", heroes[1], 9999);
                yield return Apply("player-horde-damage-lethal", "Damage", players[1], 9999);

                IEnumerator Apply(string label, string kind, CharacterState actor, int amount)
                {
                    var record = new Operation { Label = label, Kind = kind, ActorId = trace.UnitId(actor), Amount = amount,
                        SourceCardId = kind == "Damage" ? trace.CardId(source) : 0, Before = trace.Capture(rooms.GetRoom(0)) };
                    Operations.Add(record); current = record; Label = label;
                    if (kind == "Damage") yield return actor.ApplyDamage(amount, new CharacterState.ApplyDamageParams {
                        damageSourceCard = source, damageType = Damage.Type.Default }, managers.GetPlayerManager(), managers.GetCardStatistics());
                    else yield return actor.DebuffMaxHP(amount, 0);
                    yield return combat.RunTriggerQueue();
                    while (combat.IsRunningTriggerQueue) yield return null;
                    record.QueueAfter = QueueCount(); record.After = trace.Capture(rooms.GetRoom(0));
                    using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) record.AfterActor = trace.CaptureUnit(actor);
                    current = null; Label = null;
                }
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = null; Label = null; }
            Set(combat, "combatStateChanged", true);
            int QueueCount() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && QueueCount() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                int triggerCount, bool fromRunningTriggerQueue)
            {
                if (current != null && fromRunningTriggerQueue && __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    current.Dispatched.Add(new Dispatch { ActorId = FullBattleTrace.Active!.UnitId(__instance), Kind = trigger.ToString(),
                        DyingId = dyingCharacter == null ? 0 : FullBattleTrace.Active!.UnitId(dyingCharacter), TriggerCount = triggerCount });
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
