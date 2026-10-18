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
    internal static class OriginalFreezeReduceCostScenario
    {
        internal const string Name = "original-freeze-reduce-cost";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] UnitCards { get; private set; } = Array.Empty<int>();
        internal static int[] SpellCards { get; private set; } = Array.Empty<int>();
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static int step;
        private static bool playedFrozen;
        private static bool actionMark;
        internal static object Evidence => new { OwnerId = RelicFreezeHandUpgradeModel.DataId, UnitCards, SpellCards,
            Roles = new[] { "frozen-play-after-first-upgrade", "frozen-retained", "persistent-not-frozen", "ordinary-discard" } };
        internal sealed class TestRecord
        {
            public int Room { get; set; }
            public bool Result { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public int Room { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("FreezeReduceCost requires one pre-battle acquisition.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicFreezeHandUpgradeModel.DataId);
            RelicEffectData effect = owner.GetEffects().Single();
            if (owner.name != RelicFreezeHandUpgradeModel.AssetKey || !owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicFreezeHandUpgradeModel.Effect || !effect.GetParamBool() ||
                effect.GetEffectConditions().Count != 0 || !effect.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitFreeze" }) ||
                effect.GetParamCardUpgradeData().GetID() != RelicFreezeHandUpgradeModel.UpgradeId)
                throw new InvalidOperationException("Original FreezeReduceCost definition changed.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(steward, "cost", 0); save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(owner);
            CombatRelicState captured = RelicProbe.Capture(managers).Single(item => item.DataId == RelicFreezeHandUpgradeModel.DataId);
            string? error = RelicModel.Validate(new[] { captured });
            if (error != null) throw new InvalidOperationException(error + " " + JToken.FromObject(captured));
            Acquired = true;
            log.LogInfo("ORIGINAL-FREEZE-COST-ACQUIRED unchanged end-of-turn trait filter and -99 temporary cost upgrade.");
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected deployment setup.");
            CardManager cards = managers.GetCardManager()!; AllGameData data = managers.GetSaveManager().GetAllGameData();
            UnitCards = cards.GetHand().Where(card => card.GetCardType() == CardType.Monster).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (UnitCards.Length != 2) throw new InvalidOperationException("Missing controlled Stewards.");
            CardData template = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            var spells = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                CardData spell = UnityEngine.Object.Instantiate(template);
                spell.name = "PojuFreezeCost" + i; Set(spell, "id", "f1ee2e00-0000-4000-8000-00000000000" + i);
                Set(spell, "cost", i + 2); Set(spell, "costType", CardData.CostType.Default);
                Set(spell, "targetless", true); Set(spell, "targetsRoom", true);
                Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
                var traits = new List<CardTraitData>();
                if (i == 2) { var persistent = new CardTraitData(); persistent.Setup("CardTraitPersistent"); traits.Add(persistent); }
                Set(spell, "traits", traits); Set(spell, "effects", new List<CardEffectData>());
                Set(spell, "effectTriggers", new List<CharacterTriggerData>()); Set(spell, "triggers", new List<CardTriggerEffectData>());
                Set(spell, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
                CardState created = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false)!;
                if (i < 2) { var frozen = new CardTraitData(); frozen.Setup("CardTraitFreeze"); cards.AddTemporaryTraitToCard(created, frozen, true); }
                spells.Add(FullBattleTrace.Active!.CardId(created));
            }
            SpellCards = spells.ToArray(); FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int? id = step < 2 ? UnitCards[step] : source.Spawn.Turn >= 2 && !playedFrozen ? SpellCards[0] : (int?)null;
            if (!id.HasValue) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                play.CardInstanceId == id.Value && (step >= 2 || play.RoomIndex == 0));
            if (action != null) { if (step < 2) step++; else playedFrozen = true; actionMark = true; }
            return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark) return false; actionMark = false; return true; }
        private static bool Active => Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(RelicEffectApplyCardUpgradeToHandEndOfTurn), nameof(RelicEffectApplyCardUpgradeToHandEndOfTurn.TestEffectEndOfTurn))]
        private static class TestPatch
        {
            private static void Prefix(EndOfTurnRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active) return;
                var row = new TestRecord { Room = relicEffectParams.roomState.GetRoomIndex() }; __state = row; Tests.Add(row);
                try { row.Before = FullBattleTrace.Active!.CaptureTrain(); } catch (Exception error) { row.Error = error.ToString(); }
            }
            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (!__result || !JToken.DeepEquals(JToken.FromObject(__state.Before), JToken.FromObject(__state.Actual)))
                        __state.Difference = "Freeze-cost Test must return true without changing state/RNG."; }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectApplyCardUpgradeToHandEndOfTurn), nameof(RelicEffectApplyCardUpgradeToHandEndOfTurn.ApplyEffectEndOfTurn))]
        private static class ApplyPatch
        {
            private static void Postfix(EndOfTurnRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, relicEffectParams.roomState.GetRoomIndex()); }
        }
        private static IEnumerator Observe(IEnumerator native, int room)
        {
            var row = new ApplicationRecord { Room = room }; Applications.Add(row);
            try { row.Before = FullBattleTrace.Active!.CaptureTrain();
                TrainCombatResult predicted = RelicFreezeHandUpgradeModel.ApplySelected(row.Before, room, false);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason; }
            catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose();
                try { row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Freeze-cost hand upgrade/state/RNG differs."; }
                catch (Exception error) { row.Error = error.ToString(); }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
