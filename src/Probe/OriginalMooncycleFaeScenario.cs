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
    internal static class OriginalMooncycleFaeScenario
    {
        internal const string Name = "original-mooncycle-fae";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] UnitCards { get; private set; } = Array.Empty<int>();
        internal static int SpellCard { get; private set; }
        internal static readonly List<TestRecord> Tests = new();
        internal static readonly List<ApplicationRecord> Applications = new();
        private static int step;
        private static bool actionMark, emptyControl;
        internal static object Evidence => new { OwnerId = RelicMooncycleRoomEffectModel.DataId, UnitCards, SpellCard,
            Roles = new[] { "Fae-floor-0", "Fae-floor-1", "non-Fae-floor-0" } };
        internal sealed class TestRecord
        {
            public bool EmptyControl { get; set; }
            public bool Result { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public int Turn { get; set; }
            public int Phase { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Mooncycle Fae requires pre-battle acquisition.");
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicMooncycleRoomEffectModel.DataId);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CardData fae = data.FindCardData("d10e2dac-fa58-4509-b0ee-112b3bf78800") ??
                throw new InvalidOperationException("Original Fae carrier is missing.");
            if (!fae.GetSpawnCharacterData()!.GetSubtypes().Any(subtype => subtype.Key == RelicMooncycleRoomEffectModel.FaeSubtype))
                throw new InvalidOperationException("Original Fae carrier subtype changed.");
            foreach (CardData card in new[] { fae, steward })
            {
                CharacterData unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "unitAbility", null!);
                Set(card, "cost", 0);
            }
            Set(fae, "traits", new List<CardTraitData>());
            // Author the carrier as a banner unit so it enters the initial deployment hand.
            // The relic's unchanged Fae filter still sees its original Fae subtype.
            Set(fae.GetSpawnCharacterData()!, "subtypeKeys", fae.GetSpawnCharacterData()!.GetSubtypes()
                .Select(subtype => subtype.Key).Concat(new[] { "SubtypesData_BannerUnit" }).Distinct().ToList());
            save.GetDeckState().Clear();
            foreach (CardData card in new[] { fae, fae, steward }) save.AddCardToDeck(card, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(owner);
            string? error = RelicModel.Validate(new[] { RelicProbe.Capture(managers).Single(item => item.DataId == RelicMooncycleRoomEffectModel.DataId) });
            if (error != null) throw new InvalidOperationException(error);
            Acquired = true; log.LogInfo("ORIGINAL-MOONCYCLE-FAE-ACQUIRED unchanged all-room +5 upgrade and Conduit.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment preparation.");
            var cards = managers.GetCardManager()!.GetHand();
            UnitCards = cards.Where(card => card.GetSpawnCharacterData()?.GetSubtypes().Any(subtype => subtype.Key == RelicMooncycleRoomEffectModel.FaeSubtype) == true)
                .Concat(cards.Where(card => card.GetSpawnCharacterData()?.GetSubtypes().Any(subtype => subtype.Key == RelicMooncycleRoomEffectModel.FaeSubtype) != true))
                .Select(FullBattleTrace.Active!.CardId).ToArray();
            if (UnitCards.Length != 3) throw new InvalidOperationException("Missing controlled Fae/non-Fae cards; hand=" + string.Join(",", cards.Select(card => card.GetCardDataID())));
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData spell = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            spell.name = "PojuMooncycleFaePersistentSpell"; Set(spell, "id", "bc057002-0000-4000-8000-000000000001");
            Set(spell, "cost", 99); Set(spell, "targetless", true); Set(spell, "targetsRoom", true); Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
            Set(spell, "effects", new List<CardEffectData>()); Set(spell, "triggers", new List<CardTriggerEffectData>());
            Set(spell, "effectTriggers", new List<CharacterTriggerData>()); Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            var persistent = new CardTraitData(); persistent.Setup("CardTraitPersistent"); Set(spell, "traits", new List<CardTraitData> { persistent });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
            SpellCard = FullBattleTrace.Active!.CardId(managers.GetCardManager()!.AddNewCard(spell, CardPile.HandPile, false, false, animate: false));
            Set(managers.GetPlayerManager(), "isMoonPhaseTrackingActive", true);
            Prepared = true;
            var owner = managers.GetSaveManager().GetCollectedRelics().Single(item => item.GetRelicDataID() == RelicMooncycleRoomEffectModel.DataId);
            var effect = owner.GetEffects().OfType<RelicEffectAddEffectOnMooncycle>().Single();
            emptyControl = true;
            try { effect.TestEffectMoonPhase(new RelicEffectParams { relicManager = managers.GetRelicManager() }, managers.GetCoreManagers()); }
            finally { emptyControl = false; }
            FullBattleTrace.Active!.InvalidateCardRules();
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 3 || step == 2 && source.Spawn.Turn == 0) return null;
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(source);
            PlayCardAction? action = frontier.SupportedActions.FirstOrDefault(play => play.CardInstanceId == UnitCards[step] && play.RoomIndex == (step == 1 ? 1 : 0));
            if (action == null) throw new InvalidOperationException("Missing controlled mooncycle play at step " + step + ": " +
                string.Join("; ", frontier.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()));
            if (action != null) { step++; actionMark = true; } return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark) return false; actionMark = false; return true; }
        private static bool Active(RelicEffectAddEffectOnMooncycle effect) => Prepared && FullBattleTrace.Active != null &&
            effect.SourceRelicState?.GetRelicDataID() == RelicMooncycleRoomEffectModel.DataId && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(RelicEffectAddEffectOnMooncycle), nameof(RelicEffectAddEffectOnMooncycle.TestEffectMoonPhase))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectAddEffectOnMooncycle __instance, out TestRecord? __state)
            {
                __state = null; if (!Active(__instance)) return;
                var row = new TestRecord { EmptyControl = emptyControl }; __state = row; Tests.Add(row);
                try { row.Before = FullBattleTrace.Active!.CaptureTrain(); } catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (!JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual))) __state.Difference = "Mooncycle Test changed state/RNG."; }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnMooncycle), nameof(RelicEffectAddEffectOnMooncycle.ApplyEffectMoonPhase))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddEffectOnMooncycle __instance, ref IEnumerator __result)
            { if (Active(__instance)) __result = Observe(__result); }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try { row.Before = FullBattleTrace.Active!.CaptureTrain(); row.Phase = (int)AllGameManagers.Instance!.GetPlayerManager()!.CurrentMoonPhase;
                row.Turn = AllGameManagers.Instance.GetCombatManager()!.GetTurnCount();
                var predicted = RelicMooncycleRoomEffectModel.Apply(row.Before);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason; }
            catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose();
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Mooncycle complete state/RNG differs."; }
                catch (Exception error) { row.Error = error.ToString(); } }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
