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
    internal static class TriggerMutationScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int UnitId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static void Start(AllGameManagers managers, ManualLogSource log, bool detachedDraw = false)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers, log, detachedDraw), log)); }
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
        private static IEnumerator Run(AllGameManagers managers, ManualLogSource log, bool detachedDraw)
        {
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            FullBattleTrace trace = FullBattleTrace.Active!;
            // These are deployment-turn API tests. Author the timing definition too,
            // so the native queue and captured model both permit PreCombat here.
            ((IList<CharacterTriggerData.Trigger>)managers.GetSaveManager().GetBalanceData()
                .GetDisallowedDeploymentPhaseCharacterTriggers()).Remove(CharacterTriggerData.Trigger.PreCombat);
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            int handIndex = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            if (handIndex < 0) throw new InvalidOperationException("Trigger mutation setup requires an initial Steward.");
            CardState source = cards.GetHand()[handIndex]; var point = rooms.GetRoom(0).GetMonsterPoint(0);
            if (!cards.CanPlayHandCard(source, 0, point, null, null, out var error) || !cards.PlayCard(handIndex, point, ref error))
                throw new InvalidOperationException("Trigger mutation setup summon failed: " + error);
            while (managers.GetReplayManager().IsCardPlaying() || managers.GetCombatManager()!.IsRunningTriggerQueue ||
                (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false)) yield return null;
            var monsters = new List<CharacterState>(); rooms.GetRoom(0).AddCharactersToList(monsters, Team.Type.Monsters);
            CharacterState host = monsters.Single(unit => unit.GetSpawnerCard() == source);

            if (detachedDraw)
            {
                yield return PrepareDetachedDraw();
                Set(managers.GetCombatManager()!, "combatStateChanged", true);
                log.LogInfo("DETACHED-BONUS-DRAW-PREPARED shifted, self-removed and re-added effect states; pending callbacks retained for the next actual hand.");
                yield break;
            }

            CardUpgradeData child = Upgrade("Child", 1);
            child.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Gold(7)));
            CardUpgradeData parent = Upgrade("Append", 2);
            parent.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, true, Effect(child, false), Gold(11)));
            yield return host.ApplyCardUpgrade(State(parent), upgradeId: parent.GetID());
            yield return Observe("append-same-phase");
            yield return host.RemoveCardUpgrade(State(parent), parent.GetID());
            yield return host.RemoveCardUpgrade(State(child), child.GetID());

            CardUpgradeData previous = Upgrade("Previous", 3);
            previous.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.OnHeal, false, Gold(1)));
            CardUpgradeData shift = Upgrade("Shift", 4);
            shift.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Effect(previous, true), Gold(11)));
            shift.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Gold(25)));
            shift.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Gold(35)));
            yield return host.ApplyCardUpgrade(State(previous), upgradeId: previous.GetID());
            yield return host.ApplyCardUpgrade(State(shift), upgradeId: shift.GetID());
            yield return Observe("remove-earlier-skips-next");
            yield return host.RemoveCardUpgrade(State(shift), shift.GetID());

            CardUpgradeData removal = Upgrade("SelfRemovalDescriptor", 5);
            removal.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Gold(0)));
            CardUpgradeData self = DynamicUpgradeScenario.Upgrade("PojuSelfRemoval", removal.GetID(), 0, 0, 0, 0, "armor", 0);
            self.GetStatusEffectUpgrades().Clear();
            self.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Effect(removal, true), Gold(11)));
            self.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Gold(45)));
            yield return host.ApplyCardUpgrade(State(self), upgradeId: self.GetID());
            yield return Observe("self-removal-finishes-effects");
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("TRIGGER-MUTATION-PREPARED three native retained-trigger/list-index cases; original Boss/waves.");

            IEnumerator Observe(string label)
            {
                var record = new Record { Label = label, UnitId = trace.UnitId(host), Before = trace.Capture(rooms.GetRoom(0)) };
                Records.Add(record);
                var predicted = RoomCombatModel.ApplyPreCombat(record.Before, record.UnitId);
                record.UnsupportedReason = predicted.UnsupportedReason;
                yield return managers.GetCombatManager()!.QueueAndRunTrigger(host, CharacterTriggerData.Trigger.PreCombat);
                record.After = trace.Capture(rooms.GetRoom(0));
                if (predicted.Supported && !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)))
                    record.Difference = "Retained trigger/list iteration differs";
                log.LogInfo("TRIGGER-MUTATION-" + (predicted.Supported && record.Difference == null ? "MATCH " : "MISMATCH ") + label);
            }

            IEnumerator PrepareDetachedDraw()
            {
                CardUpgradeData a = DynamicUpgradeScenario.Upgrade("PojuDetachedDrawA", "d816ed10-77bb-4000-8000-000000000001", 1, 1, 0, 0, "armor", 0);
                CardUpgradeData b = DynamicUpgradeScenario.Upgrade("PojuDetachedDrawB", "d816ed10-77bb-4000-8000-000000000002", 2, 0, 0, 0, "armor", 0);
                CardUpgradeData previous = Upgrade("DrawPrevious", 6);
                previous.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, true, Next(0, null), Next(2, a)));
                CardUpgradeData shift = Upgrade("DrawShift", 7);
                shift.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, true, Effect(previous, true), Next(1, b)));
                yield return host.ApplyCardUpgrade(State(previous), upgradeId: previous.GetID());
                yield return host.ApplyCardUpgrade(State(shift), upgradeId: shift.GetID());
                yield return Observe("shift-after-pending-draw");
                yield return host.RemoveCardUpgrade(State(shift), shift.GetID());

                CardUpgradeData removal = Upgrade("DrawSelfDescriptor", 8);
                removal.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Next(0, null)));
                CardUpgradeData self = DynamicUpgradeScenario.Upgrade("PojuDrawSelf", removal.GetID(), 0, 0, 0, 0, "armor", 0);
                self.GetStatusEffectUpgrades().Clear();
                self.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Effect(removal, true), Next(3, a)));
                self.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, Next(9, b)));
                yield return host.ApplyCardUpgrade(State(self), upgradeId: self.GetID());
                yield return Observe("self-removed-pending-draw");

                CardUpgradeData repeat = Upgrade("DrawReadded", 9);
                repeat.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, true, Next(1, b)));
                for (int index = 0; index < 2; index++)
                {
                    yield return host.ApplyCardUpgrade(State(repeat), upgradeId: repeat.GetID());
                    yield return Observe(index == 0 ? "readded-first-draw" : "readded-second-draw");
                    yield return host.RemoveCardUpgrade(State(repeat), repeat.GetID());
                }
            }
        }
        private static CardUpgradeData Upgrade(string name, int index)
        {
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuMutation" + name, "d816ed10-77aa-4000-8000-" + index.ToString("D12"), 0, 0, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear(); return upgrade;
        }
        private static CardUpgradeState State(CardUpgradeData definition) { var state = new CardUpgradeState(); state.Setup(definition); return state; }
        private static CardEffectData Gold(int amount) => HealingScenario.HealGold(amount, false, true).GetEffects()[0];
        private static CardEffectData Next(int amount, CardUpgradeData? upgrade)
        {
            var effect = new CardEffectData("CardEffectDrawAdditionalNextTurn", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramInt", amount);
            Set(effect, "paramCardUpgradeData", upgrade!); return effect;
        }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, bool once, params CardEffectData[] effects)
        { var trigger = HealingScenario.HealGold(0, once, true); Set(trigger, "trigger", kind); Set(trigger, "effects", effects.ToList()); return trigger; }
        private static CardEffectData Effect(CardUpgradeData upgrade, bool remove)
        {
            var effect = new CardEffectData(remove ? "CardEffectRemoveTempUpgradeFromUnit" : "CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
