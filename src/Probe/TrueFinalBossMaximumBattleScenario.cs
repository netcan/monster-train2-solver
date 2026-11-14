using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossMaximumBattleScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_BATTLE") == "1";
        internal static string SacrificeTarget => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_SACRIFICE") ?? "";
        internal static bool Sacrifice => SacrificeTarget.Length > 0;
        private static bool TerminalCommand => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_COMMAND") == "1";
        private static bool RetainMaximum => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_SACRIFICE_RETAIN") == "1";
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static int terminalCommandCardId;
        private static BattleTurnState? before, actual;
        private static readonly List<object> terminalHarvest = new List<object>();
        private static readonly List<object> terminalSacrifices = new List<object>();
        private static readonly List<object> terminalRemovals = new List<object>();
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step,
            AuthoredCardsAndDrawOrder = true, OriginalBossDefinitionsAndBirthStats = true, Before = before, Actual = actual,
            TerminalHarvest = terminalHarvest, SacrificeTarget, RetainMaximum, TerminalCommand, TerminalCommandCardId = terminalCommandCardId,
            TerminalSacrifices = terminalSacrifices, TerminalRemovals = terminalRemovals };
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static bool Prepare()
        {
            if (!Enabled || Prepared) return false;
            AllGameManagers managers = AllGameManagers.Instance!;
            if (managers.GetCombatManager().GetTurnCount() != 1) return false;
            if (!TrueFinalBossTurnProbe.Enabled || managers.GetSaveManager().PreviewMode || !ConditionalActivationProbe.HasVanguard)
                throw new InvalidOperationException("Maximum-health paid controls require the isolated live 2.3.1 TFB route.");
            var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            before = FullBattleTrace.Active!.CaptureDecision();
            string[] types = { "CardEffectDebuffMaxHealth", "CardEffectBuffMaxHealth", "CardEffectDebuffMaxHealth", "CardEffectDebuffMaxHealth",
                "CardEffectDebuffMaxHealth", "CardEffectDebuffMaxHealth", "CardEffectDebuffMaxHealth", "CardEffectDebuffMaxHealth",
                "CardEffectDebuffMaxHealth", "CardEffectDebuffMaxHealth" };
            int[] amounts = { 99999, 9, 2, 99999, 99999, 99999, 99999, 99999, 99999, 7 };
            if (Sacrifice)
            {
                types = types.Concat(new[] { "CardEffectDebuffMaxHealth" }).ToArray();
                amounts = amounts.Concat(new[] { 1 }).ToArray();
                if (RetainMaximum)
                    for (int index = 3; index < 10; index++) types[index] = "CardEffectDamage";
            }
            CardData[] data = types.Select((type, index) =>
            {
                var card = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId)); card.name = "PojuLinkedMaximumInput" + index;
                Set(card, "id", "fc231b61-0000-4000-8000-" + (index + 1).ToString("D12")); Set(card, "cost", index is 0 or 3 or 4 or 10 ? 1 : 0);
                var effect = new CardEffectData(type, null!, Team.Type.Heroes); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(effect, "paramInt", amounts[index]);
                if (type == "CardEffectBuffMaxHealth") Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle);
                Set(card, "effects", new List<CardEffectData> { effect }); Set(card, "traits", new List<CardTraitData>());
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>()); Set(card, "targetless", false); Set(card, "targetsRoom", true);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card); return card;
            }).ToArray();
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear(); cards = new int[data.Length];
            CardState Add(int index)
            {
                CardState card = manager.AddNewCard(data[index], CardPile.HandPile, false, false, animate: false) ??
                    throw new InvalidOperationException("Missing linked maximum-health input " + index);
                cards[index] = FullBattleTrace.Active!.CardId(card); return card;
            }
            int opening = Sacrifice ? 4 : 3;
            for (int index = 0; index < opening; index++) Add(index);
            for (int index = data.Length - 1; index >= opening; index--) manager.MoveCardFromHandToTopOfDeck(Add(index));
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            actual = FullBattleTrace.Active!.CaptureDecision();
            using (var document = NativeFixtureCapture.Capture(new { Before = before, Actual = actual }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-maximum-preparation.mt2f")))
                document.Write(output);
            return true;
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int opening = Sacrifice ? 4 : 3;
            if (!Prepared || step == cards.Length || state.Spawn.Turn < (step < opening ? 1 : step < opening + 4 ? 2 : 3)) return null;
            if (!state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cards[step]))
                return null;
            if (step == 10 && TerminalCommand)
            {
                terminalCommandCardId = state.Spawn.Train.Context.Cards.Hand.Single(card => card.DataId == "9f9afce5-44fe-40ff-aee5-1df851b30263").InstanceId;
                var frontier = BattleActionModel.EnumerateActions(state, terminalCommandCardId, includeAbilities: false);
                PlayCardAction? command = frontier.SupportedActions.FirstOrDefault() ?? frontier.UnsupportedAttempts
                    .FirstOrDefault(attempt => attempt.Reason == "Terminal routing for this card effect/destination is not validated.")?.Action;
                if (command == null) throw new InvalidOperationException("Missing native original TFB Pyre command input.");
                step++; return command;
            }
            string asset = step == 3 ? "Boss_Level9_Savagery_V2" : step is >= 4 and <= 8 ? "Boss_Level9_Dominion_V2" :
                step is 1 or 9 ? "Boss_Level9_Entropy_Mid_V2" : step == 2 ? "Boss_Level9_Entropy_Bot_V2" : "Boss_Level9_Entropy_Top_V2";
            if (step == 10) asset = "Boss_Level9_" + (SacrificeTarget == "Dominion" || SacrificeTarget == "Savagery" ? SacrificeTarget :
                "Entropy_" + (SacrificeTarget == "EntropyBot" ? "Bot" : SacrificeTarget == "EntropyMid" ? "Mid" : "Top")) + "_V2";
            RoomCombatState room = state.Spawn.Train.Rooms.Single(floor => floor.Units.Any(unit => unit.AssetKey == asset));
            int target = room.Units.Single(unit => unit.AssetKey == asset).Id;
            var action = new PlayCardAction(cards[step], room.RoomIndex, targetUnitId: target);
            // These declared inputs use native legality as the calibration
            // oracle; model rejections must be recorded, not stop observation.
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId) ||
            terminalCommandCardId > 0 && action.CardInstanceId == terminalCommandCardId;
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.Sacrifice))]
        private static class SacrificePatch
        {
            private static void Postfix(CharacterState __instance, CardState damageSourceCard, ref IEnumerator __result)
            {
                if (Enabled && Sacrifice && Prepared && step == cards.Length && __instance.IsTrueFinalBoss())
                    __result = Observe(__instance, damageSourceCard, __result);
            }
            private static IEnumerator Observe(CharacterState actor, CardState card, IEnumerator operation)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                CombatUnit root = trace.CaptureUnit(actor);
                CombatUnit[] actors = AllGameManagers.Instance.GetHeroManager().GetCharactersReadOnly()
                    .Where(unit => unit.IsTrueFinalBoss()).Select(unit => trace.CaptureUnit(unit)).ToArray();
                yield return operation;
                terminalSacrifices.Add(new { Root = root, Actors = actors, SourceCardId = card == null ? 0 : trace.CardId(card),
                    NativeReturned = true, IsSacrifice = actor.IsSacrifice, Destroyed = actor.IsDestroyed,
                    AllBossesDead = AllGameManagers.Instance.GetCombatManager().AllScenarioBossesDead });
            }
        }
        [HarmonyPatch(typeof(CharacterState), "RemoveCharacterInner")]
        private static class RemovalPatch
        {
            private static void Prefix(CharacterState __instance, List<CharacterState> activeList, bool death, bool vanishUnit, bool isLastDeathInSet)
            {
                if (Enabled && Sacrifice && Prepared && step == cards.Length && __instance.IsTrueFinalBoss())
                    terminalRemovals.Add(new { Id = FullBattleTrace.Active!.UnitId(__instance), Room = __instance.GetCurrentRoomIndex(),
                        ActiveBefore = activeList.Select(unit => new TrueFinalBossRemovalActor(FullBattleTrace.Active!.UnitId(unit),
                            unit.GetCurrentRoomIndex(), unit.IsTrueFinalBoss())).ToArray(), Death = death, Vanish = vanishUnit, LastDeath = isLastDeathInSet });
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class HarvestPatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, int triggerCount)
            {
                if (!Enabled || !Prepared || !fromRunningTriggerQueue || AllGameManagers.Instance.GetSaveManager().PreviewMode ||
                    !AllGameManagers.Instance.GetCombatManager().AllScenarioBossesDead ||
                    trigger.ToString() is not ("OnAnyHeroDeathOnFloor" or "OnAnyUnitDeathOnFloor")) return;
                FullBattleTrace trace = FullBattleTrace.Active!; CombatUnit unit = trace.CaptureUnit(__instance);
                if (!unit.Triggers.Any(item => item.Kind == trigger.ToString())) return;
                terminalHarvest.Add(new { ActorId = trace.UnitId(__instance), Kind = trigger.ToString(), Count = triggerCount,
                    DyingId = dyingCharacter == null ? 0 : trace.UnitId(dyingCharacter),
                    DyingHp = dyingCharacter?.GetHP(), Before = unit });
            }
        }
    }
}
