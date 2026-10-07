using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class DyingHordeUpgradeScenario
    {
        internal static readonly List<HordeDeathScenario.Operation> Operations = new List<HordeDeathScenario.Operation>();
        internal static bool Started, Completed;
        internal static string? Error, Label;
        private static HordeDeathScenario.Operation? current;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HarvestScenario.Prepare(managers, log);
            CardData steward = managers.GetSaveManager().GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            steward.GetTraits().RemoveAll(trait => trait.GetTraitStateName() == "CardTraitScalingUpgradeUnitHealth");
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, managers.GetSaveManager());
            log.LogInfo("DYING-HORDE-UPGRADES-PREPARED dying HP casualties, failed additions/removals and retained source writeback; original Boss/waves retained.");
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
                if (hand < 0) throw new InvalidOperationException("Dying Horde fixture requires two initial Stewards.");
                var point = rooms.GetRoom(0).GetMonsterPoint(i); CardState card = cards.GetHand()[hand];
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Native dying Horde summon failed: " + error);
                while (!Ready()) yield return null;
            }
            rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                for (int i = 0; i < 3; i++) yield return managers.GetHeroManager()!.SpawnHeroInRoom(HarvestScenario.ObserverDefinition, 0, null!);
                rooms.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
                var hp = Upgrade(1, 2, -1, 4, 9);
                var unhealed = Upgrade(2, 2, 3, -1, 9);
                var removeHp = Upgrade(3, 2, 5, 4, 3);
                var removeUnhealed = Upgrade(4, 2, 0, 4, 3);
                var attributed = Upgrade(5, 2, 0, 0, 3);
                yield return Install(players[0], 11, Effect(hp), Effect(Upgrade(6, 1, 3, 0, 2)));
                yield return Install(enemies[0], 12, Effect(unhealed));
                yield return ApplyStartingUpgrade(players[1], removeHp);
                yield return Install(players[1], 13, Effect(removeHp, remove: true),
                    Effect(Upgrade(7, 1, 3, 0, 2), lifetime: UnitUpgradeLifetime.TemporaryUntilUnitDeath));
                yield return ApplyStartingUpgrade(enemies[1], removeUnhealed);
                yield return Install(enemies[1], 14, Effect(removeUnhealed, remove: true));
                yield return ApplyStartingUpgrade(enemies[2], attributed);
                yield return enemies[2].BuffMaxHP(40, triggerOnHeal: false, heal: false);
                object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(enemies[2]);
                ((Dictionary<string, int>)AccessTools.Field(primary.GetType(), "maxHpFromUpgrades").GetValue(primary))[attributed.GetID()] = 40;
                yield return Install(enemies[2], 15, Effect(attributed, remove: true));
                yield return Kill("dying-player-ordinary-hp", players[0]);
                yield return Kill("dying-enemy-unhealed-hp", enemies[0]);
                yield return Kill("dying-player-remove-hp", players[1]);
                yield return Kill("dying-enemy-remove-unhealed", enemies[1]);
                yield return Kill("dying-enemy-remove-attributed", enemies[2]);
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = null; Label = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);

            IEnumerator ApplyStartingUpgrade(CharacterState actor, CardUpgradeData data)
            {
                var state = State(data); yield return actor.ApplyCardUpgrade(state, upgradeId: data.GetID());
                if (actor.GetSpawnerCard() != null) actor.GetSpawnerCard().GetTemporaryCardStateModifiers().AddUpgrade(state);
            }
            IEnumerator Install(CharacterState actor, int id, params CardEffectData[] effects)
            {
                var upgrade = Upgrade(id, 0, 0, 0, 0); upgrade.GetStatusEffectUpgrades().Clear();
                var trigger = HealingScenario.HealGold(0, false, true);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, CharacterTriggerData.Trigger.OnDeath);
                AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(trigger, effects.ToList());
                upgrade.GetCharacterTriggerUpgrades().Add(trigger);
                yield return actor.ApplyCardUpgrade(State(upgrade), upgradeId: upgrade.GetID());
            }
            IEnumerator Kill(string label, CharacterState actor)
            {
                current = new HordeDeathScenario.Operation { Label = label, Kind = "DebuffHealth", ActorId = trace.UnitId(actor),
                    Amount = 9999, Before = trace.Capture(rooms.GetRoom(0)) }; Operations.Add(current); Label = label;
                yield return actor.DebuffMaxHP(current.Amount, 0);
                yield return combat.RunTriggerQueue();
                while (combat.IsRunningTriggerQueue) yield return null;
                current.QueueAfter = QueueCount(); current.After = trace.Capture(rooms.GetRoom(0));
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                {
                    current.AfterActor = trace.CaptureUnit(actor); current.HasFinishedDying = actor.HasFinishedDying;
                    current.IsBeingRemoved = actor.IsBeingRemoved(); current.IsSacrifice = actor.IsSacrifice;
                }
                current = null; Label = null;
            }
            int QueueCount() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && QueueCount() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        private static CardUpgradeData Upgrade(int id, int damage, int hp, int unhealed, int armor) => DynamicUpgradeScenario.Upgrade(
            "PojuDyingHordeUpgrade" + id, "c2f6ed7f-18ce-4070-b65f-7dd9f518" + id.ToString("D4"), damage, hp, 0, unhealed, "armor", armor);
        private static CardUpgradeState State(CardUpgradeData data) { var state = new CardUpgradeState(); state.Setup(data); return state; }
        private static CardEffectData Effect(CardUpgradeData upgrade, bool remove = false, UnitUpgradeLifetime lifetime = UnitUpgradeLifetime.Permanent)
        {
            var effect = new CardEffectData(remove ? "CardEffectRemoveTempUpgradeFromUnit" : "CardEffectAddCardUpgradeToUnits", null!, Team.Type.Heroes | Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self);
            AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(effect, upgrade);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt1").SetValue(effect, (int)lifetime);
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
