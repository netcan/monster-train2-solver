using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class HordeDeathScenario
    {
        internal sealed class Operation
        {
            public string Label { get; set; } = "";
            public string Kind { get; set; } = "";
            public int ActorId { get; set; }
            public int Amount { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public bool HasFinishedDying { get; set; }
            public bool IsBeingRemoved { get; set; }
            public bool IsSacrifice { get; set; }
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
            ((IList<CharacterTriggerData.Trigger>)managers.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers())
                .Remove(CharacterTriggerData.Trigger.PreCombat);
            log.LogInfo("HORDE-DEATH-PREPARED queued player sacrifice and repeated sacrifice from both-team death callbacks; original Boss/waves retained.");
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
                if (hand < 0) throw new InvalidOperationException("Horde death fixture requires two initial Stewards.");
                CardState card = cards.GetHand()[hand]; var point = rooms.GetRoom(0).GetMonsterPoint(i);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Native Horde death summon failed: " + error);
                while (!Ready()) yield return null;
            }
            rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                for (int i = 0; i < 3; i++) yield return managers.GetHeroManager()!.SpawnHeroInRoom(HarvestScenario.ObserverDefinition, 0, null!);
                rooms.GetRoom(0).AddCharactersToList(heroes, Team.Type.Heroes);
                yield return Install(players[0], CharacterTriggerData.Trigger.PreCombat, "c2f6ed7f-18ce-4070-b65f-7dd9f5160082");
                yield return Install(heroes[0], CharacterTriggerData.Trigger.OnDeath, "c2f6ed7f-18ce-4070-b65f-7dd9f5160083");
                yield return Install(players[1], CharacterTriggerData.Trigger.OnDeath, "c2f6ed7f-18ce-4070-b65f-7dd9f5160084");
                yield return Apply("queued-final-player", "PreCombat", players[0], 0);
                yield return Apply("dying-final-enemy", "DebuffHealth", heroes[0], 9999);
                yield return Apply("dying-final-player", "DebuffHealth", players[1], 9999);
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = null; Label = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);

            IEnumerator Install(CharacterState actor, CharacterTriggerData.Trigger kind, string id)
            {
                var upgrade = DynamicUpgradeScenario.Upgrade("PojuHordeDeath" + kind, id, 0, 0, 0, 0, "armor", 0);
                upgrade.GetStatusEffectUpgrades().Clear();
                var trigger = HealingScenario.HealGold(0, false, true);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, kind);
                var effect = new CardEffectData("CardEffectRemoveStatusEffect", null!, Team.Type.Heroes | Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.Self);
                AccessTools.Field(typeof(CardEffectData), "paramStatusEffects").SetValue(effect,
                    new[] { new StatusEffectStackData { statusId = "horde", count = 9999 } });
                AccessTools.Field(typeof(CardEffectData), "disallowStatusEffectStackModifiers").SetValue(effect, true);
                AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(trigger, new List<CardEffectData> { effect });
                upgrade.GetCharacterTriggerUpgrades().Add(trigger);
                var state = new CardUpgradeState(); state.Setup(upgrade); yield return actor.ApplyCardUpgrade(state, upgradeId: id);
            }
            IEnumerator Apply(string label, string kind, CharacterState actor, int amount)
            {
                var record = new Operation { Label = label, Kind = kind, ActorId = trace.UnitId(actor), Amount = amount,
                    Before = trace.Capture(rooms.GetRoom(0)) };
                Operations.Add(record); current = record; Label = label;
                if (kind == "PreCombat") yield return combat.QueueAndRunTrigger(actor, CharacterTriggerData.Trigger.PreCombat);
                else yield return actor.DebuffMaxHP(amount, 0);
                yield return combat.RunTriggerQueue();
                while (combat.IsRunningTriggerQueue) yield return null;
                record.QueueAfter = QueueCount(); record.After = trace.Capture(rooms.GetRoom(0));
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                {
                    record.AfterActor = trace.CaptureUnit(actor); record.HasFinishedDying = actor.HasFinishedDying;
                    record.IsBeingRemoved = actor.IsBeingRemoved(); record.IsSacrifice = actor.IsSacrifice;
                }
                current = null; Label = null;
            }
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
                    current.Dispatched.Add(new HarvestScenario.Dispatch { ActorId = FullBattleTrace.Active!.UnitId(__instance), Kind = trigger.ToString(),
                        DyingId = dyingCharacter == null ? 0 : FullBattleTrace.Active!.UnitId(dyingCharacter), TriggerCount = triggerCount });
            }
        }
    }
}
