using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class HordeUpgradeScenario
    {
        internal sealed class Operation
        {
            public string Label { get; set; } = "";
            public string Kind { get; set; } = "Direct";
            public int ActorId { get; set; }
            public bool Remove { get; set; }
            public string UpgradeId { get; set; } = "";
            public CardUpgradeModifier Upgrade { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? AfterApi { get; set; }
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public int QueueAfterApi { get; set; }
            public int QueueAfter { get; set; }
            public List<HarvestScenario.Dispatch> Dispatched { get; set; } = new List<HarvestScenario.Dispatch>();
        }
        internal static readonly List<Operation> Operations = new List<Operation>();
        internal static bool Started, Completed;
        internal static string? Error, Label;
        private static Operation? current;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HarvestScenario.Prepare(managers, log);
            // First Horde installation is distinct from growing an existing stack.
            AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(HarvestScenario.ObserverDefinition,
                Array.Empty<StatusEffectStackData>());
            ((IList<CharacterTriggerData.Trigger>)managers.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers())
                .Remove(CharacterTriggerData.Trigger.PreCombat);
            CardState[] rallies = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardType() == CardType.Spell && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (rallies.Length == 0) throw new InvalidOperationException("Horde upgrade fixture requires real Rally spells.");
            CardData rally = managers.GetSaveManager().GetAllGameData().FindCardData(rallies[0].GetCardDataID())!;
            CardEffectData[] original = rally.GetEffects().ToArray();
            var growth = Upgrade(10, 3, 3, 2, 3); var casualties = Upgrade(11, 2, -30, -25, 1);
            rally.GetEffects().Clear();
            rally.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", growth, TargetMode.DropTargetCharacter));
            rally.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", casualties, TargetMode.LastTargetedCharacters));
            rally.GetEffects().Add(Effect("CardEffectRemoveTempUpgradeFromUnit", casualties, TargetMode.LastTargetedCharacters));
            rally.GetEffects().Add(Effect("CardEffectRemoveTempUpgradeFromUnit", growth, TargetMode.LastTargetedCharacters));
            rally.GetEffects().AddRange(original);
            foreach (CardState card in rallies) card.Setup(rally, managers.GetSaveManager());
            log.LogInfo("HORDE-UPGRADES-PREPARED ordered HP/status changes, both teams and original Boss/waves.");
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
            yield return rooms.GetRoomUI().SetSelectedRoom(0);
            var players = new List<CharacterState>(); var enemies = new List<CharacterState>();
            for (int i = 0; i < 2; i++)
            {
                int hand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (hand < 0) throw new InvalidOperationException("Horde upgrade fixture requires two initial Stewards.");
                CardState card = cards.GetHand()[hand]; var point = rooms.GetRoom(0).GetMonsterPoint(i);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Native Horde upgrade summon failed: " + error);
                while (!Ready()) yield return null;
            }
            rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                yield return managers.GetHeroManager()!.SpawnHeroInRoom(HarvestScenario.ObserverDefinition, 0, null!);
                rooms.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
                CharacterState host = players[0], enemy = enemies.Single(unit => unit.GetSourceCharacterData() == HarvestScenario.ObserverDefinition);
                var grow = Upgrade(1, 3, 10, 0, 1);
                yield return Apply("health-before-growth", host, grow);
                yield return Apply("remove-health-before-troops", host, grow, true);
                var regrow = Upgrade(2, 0, 0, 0, 2);
                yield return Apply("grow-for-two-casualties", host, regrow);
                var debuff = Upgrade(3, 2, -30, -25, 1);
                var troop = HealingScenario.HealGold(7, false, true);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(troop, CharacterTriggerData.Trigger.OnTroopRemoved);
                debuff.GetCharacterTriggerUpgrades().Add(troop);
                yield return Apply("two-casualties-before-growth", host, debuff);
                yield return Apply("remove-negative-health-upgrade", host, debuff, true);
                var unhealed = Upgrade(4, 0, 0, 40, 0);
                yield return Apply("unhealed-maximum-add", host, unhealed);
                yield return Apply("unhealed-maximum-remove", host, unhealed, true);
                var keyed = Upgrade(14, 0, 0, 0, 0);
                yield return Apply("attributed-health-add", players[1], keyed);
                yield return players[1].BuffMaxHP(40, triggerOnHeal: false, heal: false);
                players[1].DebuffHP(25);
                object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(players[1]);
                ((Dictionary<string, int>)AccessTools.Field(primary.GetType(), "maxHpFromUpgrades").GetValue(primary))[keyed.GetID()] = 40;
                yield return Apply("attributed-health-casualty", players[1], keyed, true);
                var negative = Upgrade(5, 1, 0, 0, -1);
                yield return Apply("negative-troop-add", host, negative);
                yield return Apply("negative-troop-remove", host, negative, true);
                var zero = Upgrade(6, 1, 0, 0, 0);
                yield return Apply("zero-troop-upgrade", host, zero);
                var first = Upgrade(7, 9, 17, 13, 2);
                yield return Apply("first-horde-resets-upgraded-stats", enemy, first);
                // Removing all troops through an upgrade is the raw status API: no sacrifice.
                var final = Upgrade(8, 0, 0, 0, -9999);
                yield return Apply("raw-final-troops-upgrade", enemy, final);
                var lethal = Upgrade(9, 5, 0, -9999, 9);
                yield return Apply("unhealed-lethal-skips-status", players[1], lethal);
                var child = Upgrade(13, 0, -5, 0, 2);
                var install = Upgrade(12, 0, 0, 0, 0); install.GetStatusEffectUpgrades().Clear();
                var trigger = HealingScenario.HealGold(0, true, true);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, CharacterTriggerData.Trigger.PreCombat);
                AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(trigger,
                    new List<CardEffectData> { Effect("CardEffectAddTempCardUpgradeToUnits", child, TargetMode.Self) });
                install.GetCharacterTriggerUpgrades().Add(trigger);
                var installed = new CardUpgradeState(); installed.Setup(install);
                yield return host.ApplyCardUpgrade(installed, upgradeId: install.GetID());
                var queued = new Operation { Label = "queued-horde-upgrade", Kind = "Trigger", ActorId = trace.UnitId(host),
                    UpgradeId = child.GetID(), Upgrade = CardModifierProbe.Upgrade(State(child)), Before = trace.Capture(rooms.GetRoom(0)) };
                Operations.Add(queued); current = queued; Label = queued.Label;
                yield return combat.QueueAndRunTrigger(host, CharacterTriggerData.Trigger.PreCombat);
                queued.AfterApi = trace.Capture(rooms.GetRoom(0)); queued.QueueAfterApi = QueueCount();
                queued.After = queued.AfterApi; queued.QueueAfter = QueueCount(); queued.AfterActor = trace.CaptureUnit(host);
                current = null; Label = null;
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = null; Label = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);

            IEnumerator Apply(string label, CharacterState actor, CardUpgradeData definition, bool remove = false)
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup(definition);
                var record = new Operation { Label = label, ActorId = trace.UnitId(actor), Remove = remove,
                    UpgradeId = definition.GetID(), Upgrade = CardModifierProbe.Upgrade(upgrade), Before = trace.Capture(rooms.GetRoom(0)) };
                Operations.Add(record); current = record; Label = label;
                yield return remove ? actor.RemoveCardUpgrade(upgrade, record.UpgradeId) : actor.ApplyCardUpgrade(upgrade, upgradeId: record.UpgradeId);
                record.AfterApi = trace.Capture(rooms.GetRoom(0)); record.QueueAfterApi = QueueCount();
                yield return combat.RunTriggerQueue();
                while (combat.IsRunningTriggerQueue) yield return null;
                record.After = trace.Capture(rooms.GetRoom(0)); record.QueueAfter = QueueCount();
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) record.AfterActor = trace.CaptureUnit(actor);
                current = null; Label = null;
            }
            int QueueCount() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && QueueCount() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        private static CardUpgradeData Upgrade(int id, int damage, int health, int unhealed, int troops) => DynamicUpgradeScenario.Upgrade(
            "PojuHordeUpgrade" + id, "c2f6ed7f-18ce-4070-b65f-7dd9f517" + id.ToString("D4"), damage, health, 0, unhealed, "horde", troops);
        private static CardUpgradeState State(CardUpgradeData data) { var state = new CardUpgradeState(); state.Setup(data); return state; }
        private static CardEffectData Effect(string type, CardUpgradeData upgrade, TargetMode target)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(target);
            AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(effect, upgrade);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt1").SetValue(effect, (int)UnitUpgradeLifetime.TemporaryUntilEndOfBattle);
            return effect;
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                int triggerCount, bool fromRunningTriggerQueue)
            {
                if (current != null && fromRunningTriggerQueue && __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    current.Dispatched.Add(new HarvestScenario.Dispatch { ActorId = FullBattleTrace.Active!.UnitId(__instance), Kind = trigger.ToString(),
                        DyingId = dyingCharacter == null ? 0 : FullBattleTrace.Active!.UnitId(dyingCharacter), TriggerCount = triggerCount });
            }
        }
        // A normal card effect occupies ProcessEffectsQueue until its coroutine
        // returns. Standalone API calibration must hold the same background loop;
        // otherwise idle RemoveDeadCharacters drains our queue between HP steps.
        [HarmonyPatch(typeof(CombatManager), "ProcessEffectsQueue")]
        private static class BackgroundLoopPatch
        {
            private static bool Prefix(ref IEnumerator __result)
            {
                if (current == null) return true;
                __result = Hold(); return false;
            }
            private static IEnumerator Hold() { yield return null; }
        }
    }
}
