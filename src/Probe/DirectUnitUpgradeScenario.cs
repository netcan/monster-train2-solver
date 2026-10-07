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
    internal static class DirectUnitUpgradeScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CardUpgradeModifier Upgrade { get; set; } = null!;
            public int UnitId { get; set; }
            public bool Remove { get; set; }
            public string UpgradeId { get; set; } = "";
            public int? AnonymousRemovalIndex { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }

        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            Started = true;
            managers.GetSaveManager().StartCoroutine(Protect(Run(managers, log), log));
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
            (native as IDisposable)?.Dispose();
            Completed = true;
        }
        private static IEnumerator Run(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            RoomManager rooms = managers.GetRoomManager()!;
            FullBattleTrace trace = FullBattleTrace.Active!;
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int index = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            if (index < 0) throw new InvalidOperationException("Direct upgrade setup requires an initial Steward.");
            CardState source = cards.GetHand()[index];
            var drop = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(source, 0, drop, null, null, out var error) || !cards.PlayCard(index, drop, ref error))
                throw new InvalidOperationException("Native direct-upgrade setup summon rejected: " + error);
            while (!Ready(managers)) yield return null;
            var monsters = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(monsters, Team.Type.Monsters);
            CharacterState unit = monsters.Single(item => item.GetSpawnerCard() == source);
            // Clone exclusion belongs to card effects, not CharacterState.ApplyCardUpgrade.
            unit.SetIsClone();
            CardUpgradeData repeated = Definition("Repeated", "32df4140-5700-4000-8000-000000000001", 2, 4, 3, "armor", 2);
            CardUpgradeState a = State(repeated);
            yield return Apply("duplicate-first", a);
            CardUpgradeData changed = Definition("RepeatedDifferentStats", repeated.GetID(), 7, 2, 0, "armor", 5);
            yield return Apply("duplicate-second", State(changed));
            CardUpgradeData caller = Definition("CallerStats", repeated.GetID(), 4, 1, 0, "armor", 1);
            yield return Apply("remove-first-with-caller-stats", State(caller), true);
            yield return Apply("remove-second", State(repeated), true);
            yield return Apply("remove-missing-noop", State(repeated), true);

            CardUpgradeState anonymous = new CardUpgradeState(); anonymous.Setup();
            anonymous.SetAttackDamage(3); anonymous.SetAdditionalHP(4);
            yield return Apply("anonymous-add", anonymous);
            CardUpgradeState fresh = new CardUpgradeState(); fresh.Setup(); fresh.SetAttackDamage(3); fresh.SetAdditionalHP(4);
            yield return Apply("anonymous-fresh-removal", fresh, true);
            yield return Apply("anonymous-same-object-removal", anonymous, true);

            CardUpgradeData unique = Definition("Unique", "32df4140-5700-4000-8000-000000000002", 2, 3, 0, "armor", 1);
            Set(unique, "isUnique", true);
            yield return Apply("unique-add", State(unique));
            yield return Apply("unique-noop", State(unique));
            yield return Apply("unique-remove", State(unique), true);
            CardUpgradeData clone = Definition("ExcludedClone", "32df4140-5700-4000-8000-000000000003", 1, 2, 0, "armor", 0);
            Set(clone, "excludeFromClones", true);
            yield return Apply("clone-direct-add", State(clone));
            yield return Apply("clone-direct-remove", State(clone), true);
            CardUpgradeData oversized = Definition("Restricted", "32df4140-5700-4000-8000-000000000004", 1, 1, 0, "armor", 0);
            Set(oversized, "bonusSize", 99); Set(oversized, "restrictSizeToRoomCapacity", true);
            yield return Apply("capacity-rejected", State(oversized));

            CardUpgradeData cap = Definition("EquipmentLimit", "32df4140-5700-4000-8000-000000000005", 0, 0, 0, "armor", 0);
            Set(cap, "bonusEquipment", 6);
            yield return Apply("equipment-limit-cap", State(cap));
            yield return Apply("equipment-limit-remove", State(cap), true);
            CardUpgradeData negative = Definition("Negative", "32df4140-5700-4000-8000-000000000006", -20, -3, -2, "armor", -2);
            yield return Apply("negative-add", State(negative));
            yield return Apply("negative-remove", State(negative), true);

            CardUpgradeData keyed = Definition("KeyedHealth", "32df4140-5700-4000-8000-000000000007", 1, 2, 0, "armor", 0);
            yield return Apply("keyed-add", State(keyed), upgradeId: "equipment-test-key");
            // An equipment trigger may leave additional maximum HP attributed to its upgrade ID.
            yield return unit.BuffMaxHP(4, triggerOnHeal: false, heal: false);
            object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            ((Dictionary<string, int>)AccessTools.Field(primary.GetType(), "maxHpFromUpgrades").GetValue(primary))["equipment-test-key"] = 4;
            yield return Apply("keyed-remove", State(keyed), true, "equipment-test-key");

            CardUpgradeData lethal = Definition("LethalPartial", "32df4140-5700-4000-8000-000000000008", 2, -9999, 0, "armor", 9);
            yield return Apply("lethal-partial-add", State(lethal));
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("DIRECT-UPGRADES-PREPARED " + Records.Count + " native API transitions; original Boss/waves retained.");

            IEnumerator Apply(string label, CardUpgradeState upgrade, bool remove = false, string upgradeId = "")
            {
                var applied = unit.GetAppliedCardUpgrades().ToList();
                int anonymousIndex = applied.FindIndex(item => ReferenceEquals(item, upgrade));
                var record = new Record { Label = label, UnitId = trace.UnitId(unit), Before = trace.Capture(rooms.GetRoom(0)),
                    Upgrade = CardModifierProbe.Upgrade(upgrade), Remove = remove, UpgradeId = upgradeId,
                    AnonymousRemovalIndex = remove && upgrade.GetCardUpgradeDataId().Length == 0 && anonymousIndex >= 0 ? (int?)anonymousIndex : null };
                Records.Add(record);
                var predicted = UnitModifierModel.ApplyDirect(record.Before, record.UnitId, record.Upgrade, remove, upgradeId, record.AnonymousRemovalIndex);
                record.UnsupportedReason = predicted.UnsupportedReason;
                yield return remove ? unit.RemoveCardUpgrade(upgrade, upgradeId) : unit.ApplyCardUpgrade(upgrade, upgradeId: upgradeId);
                record.After = trace.Capture(rooms.GetRoom(0));
                if (predicted.Supported && !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)))
                    record.Difference = "Direct unit API room/context differs";
                log.LogInfo("DIRECT-UPGRADE-" + (record.Difference == null && predicted.Supported ? "MATCH" : "MISMATCH") + " " + label + " " + record.UnsupportedReason);
            }
        }
        private static bool Ready(AllGameManagers managers) => !LoadingScreen.IsWorking() && !managers.GetSaveManager().PreviewMode &&
            !managers.GetReplayManager().IsCardPlaying() && !managers.GetCombatManager()!.IsRunningTriggerQueue &&
            !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
            !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(managers.GetCombatManager());
        private static CardUpgradeData Definition(string name, string id, int damage, int health, int unhealed, string status, int stacks)
            => DynamicUpgradeScenario.Upgrade("PojuDirect" + name, id, damage, health, 0, unhealed, status, stacks);
        private static CardUpgradeState State(CardUpgradeData data) { var state = new CardUpgradeState(); state.Setup(data); return state; }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
