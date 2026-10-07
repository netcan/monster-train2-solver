using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using ShinyShoe.Loading;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityLifecycleScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int UnitId { get; set; }
            public bool Remove { get; set; }
            public AbilityChangeRule Rule { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static CardData original = null!, zero = null!, equipment = null!;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            var originalCounts = owned.Select(card => managers.GetSaveManager().GetAllGameData().FindCardData(card.GetCardDataID())!)
                .Distinct().ToDictionary(card => card, card => card.GetEffects().Count);
            AbilityCooldownScenario.Prepare(managers, log, cacheScenario: true);
            // This fixture exercises lifecycle APIs. Keep ordinary spell effects intact.
            foreach (var item in originalCounts.Where(item => item.Key.GetEffects().Count != item.Value))
            {
                item.Key.GetEffects().RemoveRange(item.Value, item.Key.GetEffects().Count - item.Value);
                foreach (CardState card in owned.Where(card => card.GetCardDataID() == item.Key.GetID())) card.Setup(item.Key, managers.GetSaveManager());
            }
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            original = data.FindCardData("c2f6ed7f-18ce-4070-b65f-7dd9f5160063")!;
            zero = Clone("PojuAbilityZeroSpawn", "c2f6ed7f-18ce-4070-b65f-7dd9f5160068", 5, 0);
            equipment = Clone("PojuAbilityEquipment", "c2f6ed7f-18ce-4070-b65f-7dd9f5160069", 7, 4);
            SaveManager save = managers.GetSaveManager();
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                    save, save.GetGeneratedBoss())).First(character => !character.IsMiniboss() && !character.IsOuterTrainBoss());
            Set(ordinary, "unitAbility", original);
            CardData Clone(string name, string id, int cooldown, int spawn)
            {
                CardData result = UnityEngine.Object.Instantiate(original); result.name = name;
                AccessTools.Field(typeof(GameData), "id").SetValue(result, id);
                Set(result, "cooldownAfterActivated", cooldown); Set(result, "cooldownAtSpawn", spawn);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(result);
                return result;
            }
            log.LogInfo("ABILITY-LIFECYCLE-PREPARED raw definitions, zero-spawn and equipment restoration; original Boss and waves.");
        }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers, log), log));
        }
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
            (native as IDisposable)?.Dispose(); Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            RoomManager rooms = managers.GetRoomManager()!;
            FullBattleTrace trace = FullBattleTrace.Active!;
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int index = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            if (index < 0) throw new InvalidOperationException("Ability lifecycle requires an initial Steward.");
            CardState source = cards.GetHand()[index]; var drop = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(source, 0, drop, null, null, out var error) || !cards.PlayCard(index, drop, ref error))
                throw new InvalidOperationException("Native lifecycle summon rejected: " + error);
            while (!Ready(managers)) yield return null;
            var monsters = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(monsters, Team.Type.Monsters);
            CharacterState unit = monsters.Single(item => item.GetSpawnerCard() == source);
            yield return Apply("remove-base", null, remove: true);
            yield return Apply("remove-missing-noop", null, remove: true);
            yield return Apply("null-definition-noop", null);
            yield return Apply("ordinary-card-noop", managers.GetSaveManager().GetAllGameData().FindCardData(source.GetCardDataID()));
            yield return Apply("assign-base", original);
            yield return Apply("reassign-base", original);
            yield return Apply("zero-spawn", zero);
            yield return Apply("equipment-over-zero", equipment, fromEquipment: true);
            yield return Apply("equipment-replace-keeps-original", original, fromEquipment: true);
            yield return Apply("restore-zero-with-activation-cooldown", null, remove: true);
            yield return Apply("equipment-again", equipment, fromEquipment: true);
            yield return Apply("direct-replace-clears-original", original);
            yield return Apply("equipment-before-permanent", equipment, fromEquipment: true);
            yield return Apply("permanent-equipment-restores-original", null, remove: true, permanent: true);
            Set(unit.GetSourceCharacterData(), "preventAbilitiesFromEquipment", true);
            yield return Apply("equipment-restricted-noop", equipment, fromEquipment: true);
            Set(unit.GetSourceCharacterData(), "preventAbilitiesFromEquipment", false);
            // A modified runtime duration must not become the remembered raw definition.
            unit.SetUnitAbilityCooldown(9);
            yield return Apply("equipment-over-modified-duration", equipment, fromEquipment: true);
            yield return Apply("restore-raw-activation-cooldown", null, remove: true);
            yield return Apply("permanent-base", null, remove: true, permanent: true);
            yield return Apply("permanent-missing-noop", null, remove: true, permanent: true);
            yield return Apply("explicit-assign-disabled", original);
            yield return Apply("duplicate-permanent-base", null, remove: true, permanent: true);

            IEnumerator Apply(string label, CardData? definition, bool remove = false, bool fromEquipment = false, bool permanent = false)
            {
                var record = new Record { Label = label, UnitId = trace.UnitId(unit), Remove = remove,
                    Rule = AbilityLifecycleProbe.Change(definition, fromEquipment, permanent), Before = trace.Capture(rooms.GetRoom(0)) };
                Records.Add(record);
                RoomCombatResult predicted = AbilityLifecycleModel.Apply(record.Before, record.UnitId, record.Rule, remove);
                record.UnsupportedReason = predicted.UnsupportedReason;
                yield return remove ? unit.RemoveUnitAbility(permanent) : unit.SetUnitAbility(definition!, fromEquipment);
                while (!Ready(managers)) yield return null;
                record.After = trace.Capture(rooms.GetRoom(0));
                if (predicted.Supported && !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)))
                    record.Difference = "Native ability lifecycle room/context differs";
                log.LogInfo("ABILITY-LIFECYCLE-" + (record.Difference == null && predicted.Supported ? "MATCH" : "MISMATCH") + " " + label + " " + record.UnsupportedReason + " " + record.Difference);
            }
        }
        private static bool Ready(AllGameManagers managers) => !LoadingScreen.IsWorking() && !managers.GetSaveManager().PreviewMode &&
            !managers.GetReplayManager().IsCardPlaying() && !managers.GetCombatManager()!.IsRunningTriggerQueue &&
            ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(managers.GetCombatManager())).Count == 0 &&
            !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
            !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(managers.GetCombatManager());
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
