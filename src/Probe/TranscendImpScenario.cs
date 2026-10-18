using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TranscendImpScenario
    {
        internal const string Name = "transcend-imp";
        internal const string DataId = "57c1d696-f48d-48a3-9ac5-e868f3f669fe";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] SourceCardIds { get; private set; } = Array.Empty<int>();
        internal static int RecallCardId { get; private set; }
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int CardId { get; set; }
            public int SelfId { get; set; }
            public int RoomIndex { get; set; }
            public string Kind { get; set; } = "";
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData all = save.GetAllGameData(); CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 60);
            Set(steward.GetSpawnCharacterData(), "health", 100); Set(steward.GetSpawnCharacterData(), "size", 1);
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuReplaySelf", "f15d0009-0000-4000-8000-000000000001", 3, 4, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear();
            var gold = new CardEffectData("CardEffectRewardGold", null!, Team.Type.None); gold.Cheat_SetTargetMode(TargetMode.Room); Set(gold, "paramInt", 2);
            var buff = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters); buff.Cheat_SetTargetMode(TargetMode.Self);
            Set(buff, "paramCardUpgradeData", upgrade); Set(buff, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            var trigger = new CharacterTriggerData(CharacterTriggerData.Trigger.OnSpawn, gold); Set(trigger, "effects", new List<CardEffectData> { gold, buff });
            Set(steward.GetSpawnCharacterData(), "triggers", new List<CharacterTriggerData> { trigger });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardIds = Enumerable.Range(0, 3).Select(_ => FullBattleTrace.Active!.CardId(cards.AddNewCard(all.FindCardData(DataId)!, CardPile.HandPile, false, false, animate: false))).ToArray();
            SourceCardIds = Enumerable.Range(0, 2).Select(_ => FullBattleTrace.Active!.CardId(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false))).ToArray();
            RecallCardId = FullBattleTrace.Active!.CardId(cards.AddNewCard(all.FindCardData(MagneticRelayScenario.DataId)!, CardPile.HandPile, false, false, animate: false));
            managers.GetPlayerManager().AddEnergy(12); Prepared = true;
            log.LogInfo("TRANSCEND-IMP-PREPARED three original Imps: empty history, ordered gold/self upgrades, retained history after original source recall; natural waves/boss retained.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] imps, int[] units, int recall)
        {
            CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
            bool Played(int id) => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") > 0;
            PlayCardAction? Play(int id, int room) => plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == room);
            if (!Played(imps[0])) return Play(imps[0], 1);
            foreach (int id in units) if (!Played(id)) return Play(id, 0);
            if (!Played(imps[1])) return Play(imps[1], 0);
            if (!Played(recall))
            {
                int target = source.Spawn.Train.Rooms.SelectMany(room => room.Units).First(unit => unit.SpawnerCardId == units[0]).Id;
                return plays.FirstOrDefault(play => play.CardInstanceId == recall && play.TargetUnitId == target);
            }
            if (!Played(imps[2])) return Play(imps[2], 0);
            return plays.FirstOrDefault(play => play.RoomIndex == 0 && units.Contains(play.CardInstanceId));
        }
        internal static BattleTriggerHistory CaptureHistory(AllGameManagers managers)
        {
            var rows = new List<BattleTriggerBatch>(); CombatManager combat = managers.GetCombatManager()!;
            foreach (Team.Type team in new[] { Team.Type.Monsters, Team.Type.Heroes })
            {
                var cache = (BattleTriggeredEvents.BattleTriggers)AccessTools.Method(typeof(CombatManager), "GetBattleTriggerEffects")
                    .Invoke(combat, new object[] { team, CharacterTriggerData.Trigger.OnSpawn });
                foreach (var batch in cache.battleTriggeredCardEffects)
                {
                    var interactions = new List<string>();
                    CombatEffect[] effects = batch.cardEffects.Select(effect =>
                    {
                        string type = effect.GetCardEffect().GetType().Name;
                        int value = type == "CardEffectRewardGold" ? (int)AccessTools.Field(typeof(CardEffectRewardGold), "unmodifiedGoldReward").GetValue(effect.GetCardEffect()) : effect.GetParamInt();
                        if (type == "CardEffectRewardGold" && (effect.GetParamBool() || effect.GetTargetMode() != TargetMode.Room || effect.GetTargetTeamType() != Team.Type.None))
                            type = "UncalibratedReplayGold";
                        return new CombatEffect(type, value, 0, "", effect.GetAdditionalParamInt(), Array.Empty<string>(), effect.GetParamBool2(),
                            unitUpgrade: UnitTriggerUpgradeProbe.Capture(effect, interactions), action: UnitTriggerActionProbe.Capture(effect));
                    }).ToArray();
                    if (interactions.Count > 0) throw new NotSupportedException(string.Join("; ", interactions));
                    rows.Add(new BattleTriggerBatch(team == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy, "OnSpawn",
                        batch.spawnerCard == null ? 0 : FullBattleTrace.Active!.CardId(batch.spawnerCard), effects));
                }
            }
            return new BattleTriggerHistory(new[] { "OnSpawn" }, rows);
        }
        [HarmonyPatch(typeof(CardEffectReplayTrigger), nameof(CardEffectReplayTrigger.ApplyEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new Record { CardId = trace.CardId(effect.GetParentCardState()), SelfId = trace.UnitId(parameters.selfTarget),
                RoomIndex = parameters.selectedRoom, Kind = effect.GetParamTrigger().ToString(), Before = trace.CaptureTrain() };
            Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureTrain(); }
        }
        private static void Set(object target, string field, object value) =>
            (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);
    }
}
