using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EclipseChamberScenario
    {
        internal const string Name = "eclipse-chamber";
        internal const string DataId = "0fcb1ed0-49e2-465f-b8fa-444699348dfc";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public bool Characters { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 60);
            Set(steward.GetSpawnCharacterData(), "health", 200); Set(steward.GetSpawnCharacterData(), "size", 1);
            var ordinary = UnityEngine.Object.Instantiate(steward); ordinary.name = "PojuEclipseOrdinary";
            Set(ordinary, "id", "ec110010-0000-4000-8000-000000000001");
            var ordinaryUnit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData()); ordinaryUnit.name = "PojuEclipseOrdinaryUnit";
            Set(ordinaryUnit, "id", "ec110010-0000-4000-8000-000000000002"); Set(ordinaryUnit, "subtypeKeys", new List<string>());
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters); Set(spawn, "paramCharacterData", ordinaryUnit);
            Set(ordinary, "effects", new List<CardEffectData> { spawn });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(ordinary);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(ordinaryUnit);
            Set(steward.GetSpawnCharacterData(), "subtypeKeys", new List<string> { "SubtypesData_Silent" });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState first = Add(steward.GetID()), control = Add(steward.GetID());
            CardState ordinaryCard = Add(ordinary.GetID());
            CardState moonrise = Add(MoonPhaseCardScenario.MoonriseId), eclipse = Add(DataId), later = Add(steward.GetID());
            CardState replace = Add(RoomCapacityAttachmentScenario.CardDataId), second = Add(DataId);
            CardIds = new[] { first, control, ordinaryCard, moonrise, eclipse, later, replace, second }.Select(FullBattleTrace.Active!.CardId).ToArray();
            // Settle native activation before the first decision. This isolates
            // room mask behavior from the separate mid-battle activation reset.
            var setup = new Stack<IEnumerator>(); setup.Push(managers.GetPlayerManager().HandleMoonPhaseResetMidBattle());
            while (setup.Count > 0)
            {
                IEnumerator step = setup.Peek();
                if (!step.MoveNext()) { (step as IDisposable)?.Dispose(); setup.Pop(); }
                else if (step.Current is IEnumerator nested) setup.Push(nested);
                else if (step.Current != null) throw new InvalidOperationException("Moon activation setup requires an asynchronous wait.");
            }
            CardPlayRule rule = BattleActionProbe.Definition(all.FindCardData(DataId)!);
            if (rule.ExternalInteractions.Count > 0 || rule.Effects.Count != 2 || rule.Effects[0].RoomAttachment?.MoonPhase == null)
                throw new InvalidOperationException("Eclipse projection is incomplete: " + string.Join(";", rule.ExternalInteractions));
            log.LogInfo("ECLIPSE-CHAMBER-PREPARED original rooms and Moonrise, Silent/ordinary same-floor controls, outside-floor control and post-attachment birth.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            return null;
        }
        [HarmonyPatch(typeof(CardEffectUpdateMoonPhaseResource), nameof(CardEffectUpdateMoonPhaseResource.ApplyEffect))]
        private static class RefreshPatch
        {
            private static void Postfix(CardEffectState cardEffectState, ref IEnumerator __result)
            { if (Enabled && Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result, cardEffectState.GetParamBool()); }
        }
        private static IEnumerator Observe(IEnumerator native, bool characters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new Record { Characters = characters, Before = trace.CaptureTrain() }; Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureTrain(); }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
