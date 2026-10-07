using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class HordeRemovalScenario
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public bool Effect { get; set; }
            public bool Trigger { get; set; }
            public string StatusId { get; set; } = "horde";
            public int Amount { get; set; }
            public int ActorId { get; set; }
            public int SourceCardId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public RoomCombatState? AfterDrain { get; set; }
            public CombatUnit? AfterDrainActor { get; set; }
            public int QueueBeforeDrain { get; set; }
            public int QueueAfterDrain { get; set; }
            public bool IsSacrifice { get; set; }
            public List<HarvestScenario.Dispatch> Dispatched { get; set; } = new List<HarvestScenario.Dispatch>();
        }
        internal sealed class Cast
        {
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction Action { get; set; } = null!;
            public BattleTurnState? After { get; set; }
        }
        internal static readonly List<Cast> Casts = new List<Cast>();
        internal static readonly List<Record> Records = new List<Record>();
        internal static bool Started, Completed;
        internal static string? Error, Label;
        private static Record? current;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HarvestScenario.Prepare(managers, log);
            var save = managers.GetSaveManager();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>());
            var removal = Removal(TargetMode.Room, 9999, "horde");
            var heal = new CardEffectData("CardEffectHeal", null!, Team.Type.Monsters); heal.Cheat_SetTargetMode(TargetMode.Room);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(heal, 1);
            foreach (string id in owned.Where(card => card.GetCardType() == CardType.Spell).Select(card => card.GetCardDataID()).Distinct())
            {
                var spell = save.GetAllGameData().FindCardData(id)!;
                AccessTools.Field(typeof(CardData), "effects").SetValue(spell, new List<CardEffectData> { removal, heal });
                foreach (var card in owned.Where(card => card.GetCardDataID() == id)) card.Setup(spell, save);
            }
            log.LogInfo("HORDE-REMOVAL-PREPARED direct API versus actual removal effects, both teams, signed/zero/partial/missing/final cases.");
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
                if (hand < 0) throw new InvalidOperationException("Horde removal fixture needs two initial Stewards.");
                CardState card = cards.GetHand()[hand]; var point = rooms.GetRoom(0).GetMonsterPoint(i);
                if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) || !cards.PlayCard(hand, point, ref error))
                    throw new InvalidOperationException("Native Horde removal summon failed: " + error);
                while (!Ready()) yield return null;
            }
            rooms.GetRoom(0).AddCharactersToList(players, Team.Type.Monsters);
            CardState source = players[1].GetSpawnerCard();
            bool previous = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                for (int i = 0; i < 4; i++) yield return managers.GetHeroManager()!.SpawnHeroInRoom(HarvestScenario.ObserverDefinition, 0, null!);
                rooms.GetRoom(0).AddCharactersToList(heroes, Team.Type.Heroes);
                if (heroes.Count != 4) throw new InvalidOperationException("Native Horde removal enemy observers missing.");
                yield return Apply("effect-zero", heroes[2], 0, true);
                yield return Apply("effect-negative", heroes[2], -2, true);
                yield return Apply("effect-partial", heroes[2], 1, true);
                yield return Apply("effect-missing", heroes[2], 1, true, "regen");
                yield return Apply("api-final-player", players[0], -1, false);
                yield return Apply("effect-final-player", players[1], 9999, true);
                yield return Apply("api-final-enemy", heroes[0], -1, false);
                yield return Apply("effect-final-enemy", heroes[1], -1, true);
                var upgrade = DynamicUpgradeScenario.Upgrade("PojuHordeRemovalTrigger", "c2f6ed7f-18ce-4070-b65f-7dd9f5160081", 0, 0, 0, 0, "armor", 0);
                upgrade.GetStatusEffectUpgrades().Clear();
                var trigger = HealingScenario.HealGold(0, false, true);
                AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, CharacterTriggerData.Trigger.PreCombat);
                AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(trigger, new List<CardEffectData> { Removal(TargetMode.Self, 9999, "horde") });
                upgrade.GetCharacterTriggerUpgrades().Add(trigger);
                var upgradeState = new CardUpgradeState(); upgradeState.Setup(upgrade); yield return heroes[2].ApplyCardUpgrade(upgradeState, upgradeId: upgrade.GetID());
                ((IList<CharacterTriggerData.Trigger>)managers.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers())
                    .Remove(CharacterTriggerData.Trigger.PreCombat);
                yield return Apply("trigger-final-enemy", heroes[2], 9999, false, trigger: true);
                // A further real summon makes the paid removal exercise both teams.
                int summonHand = cards.GetHand().FindIndex(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
                if (summonHand < 0)
                {
                    CardState drawn = cards.AddNewCard(managers.GetSaveManager().GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!,
                        CardPile.HandPile, fromRelic: false, permanent: false, animate: false);
                    while (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) yield return null;
                    summonHand = cards.GetHand().FindIndex(card => card == drawn);
                }
                var summonPoint = rooms.GetRoom(0).GetFirstEmptyMonsterPoint();
                if (!cards.CanPlayHandCard(cards.GetHand()[summonHand], 0, summonPoint, null, null, out var summonError) ||
                    !cards.PlayCard(summonHand, summonPoint, ref summonError)) throw new InvalidOperationException("Native paid-removal summon failed: " + summonError);
                while (managers.GetReplayManager().IsCardPlaying() || combat.IsRunningTriggerQueue || QueueCount() != 0 ||
                    (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false)) yield return null;
                // Exercise a real paid spell, including its later healing effect and final card callbacks.
                int spellHand = cards.GetHand().FindIndex(card => card.GetCardType() == CardType.Spell);
                if (spellHand < 0)
                {
                    CardState drawn = cards.GetDrawPile().First(card => card.GetCardType() == CardType.Spell);
                    if (!cards.DrawSpecificCard(drawn)) throw new InvalidOperationException("Native setup spell draw failed.");
                    while (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) yield return null;
                    spellHand = cards.GetHand().FindIndex(card => card == drawn);
                }
                if (spellHand < 0) throw new InvalidOperationException("Horde removal requires a real initial removal spell.");
                var cast = new Cast { Before = trace.CaptureDecision(), Action = new PlayCardAction(trace.CardId(cards.GetHand()[spellHand]), 0) };
                Casts.Add(cast);
                if (!cards.CanPlayHandCard(cards.GetHand()[spellHand], 0, null, null, null, out var castError) || !cards.PlayCard(spellHand, null, ref castError))
                    throw new InvalidOperationException("Native Horde removal spell failed: " + castError);
                while (managers.GetReplayManager().IsCardPlaying() || combat.IsRunningTriggerQueue || QueueCount() != 0 ||
                    (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false)) yield return null;
                cast.After = trace.CaptureDecision();

                IEnumerator Apply(string label, CharacterState actor, int amount, bool effect, string id = "horde", bool trigger = false)
                {
                    var record = new Record { Label = label, Effect = effect, Trigger = trigger, StatusId = id, Amount = amount, ActorId = trace.UnitId(actor),
                        SourceCardId = trigger ? 0 : trace.CardId(source), Before = trace.Capture(rooms.GetRoom(0)) };
                    Records.Add(record); current = record; Label = label;
                    if (trigger) yield return combat.QueueAndRunTrigger(actor, CharacterTriggerData.Trigger.PreCombat);
                    else if (effect)
                    {
                        var data = Removal(TargetMode.DropTargetCharacter, amount, id);
                        var state = new CardEffectState(); state.Setup(data, source, managers.GetSaveManager());
                        var parameters = new CardEffectParams { playedCard = source, selectedRoom = 0, targets = new List<CharacterState> { actor } };
                        // Forward this enumerator directly so an effect that yields nothing
                        // is captured before Unity can run unrelated queue work next frame.
                        IEnumerator application = state.GetCardEffect().ApplyEffect(state, parameters, managers.GetCoreManagers(), managers.GetSystemManagers());
                        while (application.MoveNext()) yield return application.Current;
                        (application as IDisposable)?.Dispose();
                    }
                    else actor.RemoveStatusEffect(id, amount, new CharacterState.RemoveStatusEffectParams { sourceCardState = source }, allowModification: false);
                    record.After = trace.Capture(rooms.GetRoom(0)); record.AfterActor = Unit(actor);
                    record.IsSacrifice = actor.IsSacrifice;
                    record.QueueBeforeDrain = QueueCount();
                    yield return combat.RunTriggerQueue();
                    while (combat.IsRunningTriggerQueue) yield return null;
                    record.QueueAfterDrain = QueueCount(); record.AfterDrain = trace.Capture(rooms.GetRoom(0)); record.AfterDrainActor = Unit(actor);
                    current = null; Label = null;
                }
            }
            finally { combat.SuppressCombatPreviewUpdates = previous; current = null; Label = null; }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            CombatUnit Unit(CharacterState actor)
            { using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) return trace.CaptureUnit(actor); }
            int QueueCount() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
            bool Ready() => !managers.GetSaveManager().PreviewMode && !managers.GetReplayManager().IsCardPlaying() &&
                !combat.IsRunningTriggerQueue && QueueCount() == 0 && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false) &&
                !(bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat);
        }
        private static CardEffectData Removal(TargetMode target, int amount, string id)
        {
            var data = new CardEffectData("CardEffectRemoveStatusEffect", null!, Team.Type.Heroes | Team.Type.Monsters);
            data.Cheat_SetTargetMode(target);
            AccessTools.Field(typeof(CardEffectData), "paramStatusEffects").SetValue(data, new[] { new StatusEffectStackData { statusId = id, count = amount } });
            AccessTools.Field(typeof(CardEffectData), "disallowStatusEffectStackModifiers").SetValue(data, true); return data;
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
