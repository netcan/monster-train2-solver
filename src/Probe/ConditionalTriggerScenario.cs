using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class ConditionalTriggerScenario
    {
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static string? CurrentLabel { get; private set; }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        { Started = true; managers.GetSaveManager().StartCoroutine(Protect(Run(managers, log), log)); }
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
            CardManager cards = managers.GetCardManager()!; RoomManager rooms = managers.GetRoomManager()!;
            CombatManager combat = managers.GetCombatManager()!;
            ((IList<CharacterTriggerData.Trigger>)managers.GetSaveManager().GetBalanceData()
                .GetDisallowedDeploymentPhaseCharacterTriggers()).Remove(CharacterTriggerData.Trigger.PreCombat);
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            var summoned = new List<CharacterState>();
            for (int index = 0; index < 2; index++)
            {
                int handIndex = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (handIndex < 0) throw new InvalidOperationException("Conditional trigger setup requires two initial Stewards.");
                // Each Steward occupies three capacity; use another real floor for
                // the second actor instead of altering the room or unit definitions.
                yield return rooms.GetRoomUI().SetSelectedRoom(index);
                CardState card = cards.GetHand()[handIndex]; var point = rooms.GetRoom(index).GetMonsterPoint(0);
                if (!cards.CanPlayHandCard(card, index, point, null, null, out var error) || !cards.PlayCard(handIndex, point, ref error))
                    throw new InvalidOperationException("Conditional trigger setup summon failed: " + error);
                while (managers.GetReplayManager().IsCardPlaying() || combat.IsRunningTriggerQueue ||
                    (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false)) yield return null;
                var monsters = new List<CharacterState>(); rooms.GetRoom(index).AddCharactersToList(monsters, Team.Type.Monsters);
                summoned.Add(monsters.Single(unit => unit.GetSpawnerCard() == card));
            }
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            CharacterState host = summoned[0], target = summoned[1];
            CardUpgradeData conditions = Upgrade("Conditions", 1, 6, 20);
            conditions.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, true, 11, ["ARMOR", "VaLoR"], []));
            conditions.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, 17, ["poison"], []));
            conditions.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, 23, [], ["ARMOR"]));
            yield return host.ApplyCardUpgrade(State(conditions), upgradeId: conditions.GetID());
            yield return Observe("self-missing-all");
            host.AddStatusEffect("armor", 1, allowModification: false); yield return combat.RunTriggerQueue();
            yield return Observe("self-missing-one");
            host.AddStatusEffect("valor", 1, allowModification: false); yield return combat.RunTriggerQueue();
            yield return Observe("self-present-case-insensitive");
            yield return Observe("dying-missing-status", target);
            target.AddStatusEffect("armor", 1, allowModification: false); yield return combat.RunTriggerQueue();
            yield return Observe("dying-present", target);
            target.RemoveStatusEffect("armor", -1, allowModification: false); yield return combat.RunTriggerQueue();
            yield return Observe("dying-zero-status", target);
            yield return Observe("dying-null-bypasses");

            host.RemoveStatusEffect("valor", -1, allowModification: false); yield return combat.RunTriggerQueue();
            CardUpgradeData samePhase = Upgrade("SamePhase", 2);
            CharacterTriggerData add = Trigger(CharacterTriggerData.Trigger.PreCombat, true, 0, [], []);
            Set(add, "effects", new List<CardEffectData> { Status(TargetMode.Self, Team.Type.Monsters, "valor") });
            samePhase.GetCharacterTriggerUpgrades().Add(add);
            samePhase.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.PreCombat, false, 35, ["VALOR"], []));
            yield return host.ApplyCardUpgrade(State(samePhase), upgradeId: samePhase.GetID());
            yield return Observe("same-phase-status-change");

            CardUpgradeData natural = Upgrade("NaturalSlay", 3);
            CharacterTriggerData pierce = Trigger(CharacterTriggerData.Trigger.PreCombat, false, 0, [], []);
            Set(pierce, "effects", new List<CardEffectData> {
                Status(TargetMode.Self, Team.Type.Monsters, "piercing"), Status(TargetMode.Room, Team.Type.Heroes, "piercing") });
            natural.GetCharacterTriggerUpgrades().Add(pierce);
            natural.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.OnKill, false, 7, ["PIERCING"], ["pIeRcInG"]));
            natural.GetCharacterTriggerUpgrades().Add(Trigger(CharacterTriggerData.Trigger.OnKill, false, 13, ["PIERCING"], ["armor"]));
            yield return host.ApplyCardUpgrade(State(natural), upgradeId: natural.GetID());
            Set(combat, "combatStateChanged", true);
            log.LogInfo("CONDITIONAL-TRIGGERS-PREPARED eight native presence/zero/case/null-target/current-phase cases and natural Slay conditions; original Boss/waves.");

            IEnumerator Observe(string label, CharacterState? dying = null)
            {
                CurrentLabel = label;
                yield return combat.QueueAndRunTrigger(host, CharacterTriggerData.Trigger.PreCombat, dying);
                CurrentLabel = null;
            }
        }
        private static CardUpgradeData Upgrade(string name, int index, int damage = 0, int health = 0)
        {
            var definition = DynamicUpgradeScenario.Upgrade("PojuConditional" + name,
                "2e1f6480-ff10-4000-8000-" + index.ToString("D12"), damage, health, 0, 0, "armor", 0);
            definition.GetStatusEffectUpgrades().Clear(); return definition;
        }
        private static CardUpgradeState State(CardUpgradeData definition) { var state = new CardUpgradeState(); state.Setup(definition); return state; }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, bool once, int gold, string[] self, string[] dying)
        {
            var trigger = HealingScenario.HealGold(gold, once, true); Set(trigger, "trigger", kind);
            Set(trigger, "requiredStatusEffects", self.Select(id => new StatusEffectStackData { statusId = id, count = 999 }).ToList());
            Set(trigger, "requiredStatusEffectsForDyingCharacter", dying.Select(id => new StatusEffectStackData { statusId = id, count = 999 }).ToList());
            return trigger;
        }
        private static CardEffectData Status(TargetMode target, Team.Type team, string id)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, team); effect.Cheat_SetTargetMode(target);
            Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = id, count = 1 } }); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
